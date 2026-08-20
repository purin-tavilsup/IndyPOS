# CloudApi containerization (Epic I, task I0)

**Date:** 2026-08-20
**Status:** Approved (Pond, 2026-08-20)
**Epic:** I (Cloud infrastructure) — task I0, prerequisite for I1–I3
**Branches:** `chore/bump-nokpirab-1.1.0` (PR 0), `fix/reject-default-jwt-secret` (PR 1),
`feat/cloudapi-migrate-verb` (PR 2), `feat/cloudapi-docker` (PR 3)

---

## Why now

Epic 2 is closed and Epic I is next. Tasks I1–I3 provision a DigitalOcean Droplet, a managed
PostgreSQL cluster and then deploy CloudApi onto them — real money, ~$27/mo, and a machine that
cannot be inspected as conveniently as this box. I0 exists so the deployment artefact is built and
proven *before* any of that is paid for.

`docs/architecture/IndyPOS_Production_Infrastructure_Guide.md` already fixes the target: a Droplet
running Docker, an external managed PostgreSQL, a private VPC, port 443 public. What is missing is
the container itself.

Reading the code to size the task surfaced three defects that a production container hits on its
first boot. They are in scope, because none of them can be discovered from a dev machine running
Aspire, and all three are load-bearing for I3.

---

## What exists today

| Thing | State |
|---|---|
| `docker-compose.yml` (repo root) | Dev convenience for **StoreHub**: `postgres:16-alpine` + pgadmin, `POSTGRES_DB: indypos_storehub`. No CloudApi service, no image build. |
| Dockerfile | None, anywhere in the repo. |
| `.dockerignore` | None. |
| `src/IndyPOS.CloudApi/appsettings*.json` | **None at all.** Every setting comes from environment or Aspire injection. |
| Connection string key | `builder.AddNpgsqlDbContext<CloudDbContext>("cloud-db")` — `Program.cs:34`. Aspire supplies it in dev via `AppHost`'s `postgres.AddDatabase("cloud-db")`; in a plain container it must arrive as `ConnectionStrings__cloud-db`. |
| OpenIddict signing key | Solved. `INDYPOS_RSA_SIGNING_KEY` (base64 PEM, PKCS#1 or #8, ≥2048 bits) is read in `OpenIddictExtensions.AddOpenIddictServer`, and `scripts/generate-rsa-key.ps1` already generates one. Absent, it falls back to *ephemeral* dev certificates — every restart invalidates every issued token. |
| `Nokpirab` package | Restores from nuget.org (`~/.nuget/packages/nokpirab/1.0.0/.nupkg.metadata` records `"source": "https://api.nuget.org/v3/index.json"`), and the flat-container index lists both **1.0.0 and 1.1.0** as published. No private feed, so no credentials in the build. `IndyPOS.Application` pins 1.0.0; PR 0 moves it to 1.1.0. |

---

## The three defects

### Defect I0-A — production has no schema path

`Program.cs:109-112` creates the schema only under Development:

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
    await db.Database.EnsureCreatedAsync();
}
```

`CloudDbContext` has **no `Migrations/` folder** — the only migrations in the repo are StoreHub's, at
`src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations`. So a container started with
`ASPNETCORE_ENVIRONMENT=Production` against a fresh managed PostgreSQL comes up with an empty
database: every endpoint that touches `CloudDbContext` fails, and `/oauth/token` cannot work at all
because OpenIddict's own EF tables are missing too.

`EnsureCreatedAsync` is not the fix. It cannot upgrade an existing database, which makes every
subsequent release a manual DDL exercise — and it would put CloudApi outside the forward-only
migration gate that `CLAUDE.md` and `docs/operations/upgrade-procedure.md` impose on StoreHub.

### Defect I0-B — the default JWT secret admits an admin

`LocalTokenOptions.SecretKey` (`src/IndyPOS.Application/Common/Models/LocalTokenOptions.cs:14`)
defaults to the literal `IndyPOS-StoreHub-Local-Auth-Secret-Key-2026`. `Program.cs:61-62` falls back
to that default whenever the `LocalToken` section is absent — and CloudApi has no appsettings file,
so "absent" is the *normal* case:

```csharp
var localTokenOptions = builder.Configuration.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>()
    ?? new LocalTokenOptions();
```

That key validates the `StoreHubJwt` scheme, which is the sole guard on the `SystemAdminOnly` and
`CanManageUsers` policies — i.e. on `POST /admin/stores/register` and all of `/admin/users`. Anyone
holding this repo can mint a token that registers stores and creates users on a public Droplet.

The fix is a startup refusal, not a new default: outside Development, the host must fail fast if the
configured key is still the built-in one. Fail fast, fail clearly — the same posture the codebase
already takes at other boundaries.

**Both hosts get the guard** (Pond, 2026-08-20). StoreHub reaches the same default through the same
fallback at `StoreHub/Program.cs:130-131`, and its committed `appsettings.json` carries no
`LocalToken` section either.

**On an installed till the guard is a no-op, and that is verified, not assumed.** The installer
generates the key per install — `DatabaseSetup.GenerateJwtSecret()` (`:145-150`) fills 64
cryptographically random bytes — DPAPI-protects it via
`SecretProtector.Protect("LocalToken:SecretKey", …)` (`:456`), writes it into StoreHub's
`appsettings.json` and ACLs the file. `ConfigSnapshot` exists precisely because package extraction
overwrites that file, so an in-place upgrade restores the real secret rather than reverting to the
template. StoreHub decrypts it at `Program.cs:58-60` before any consumer reads it.

Where the guard *does* fire, firing is the correct outcome: a fresh install whose `DatabaseSetup`
step failed, or an upgrade whose snapshot restore failed, would otherwise start a service
authenticating admin callers with a key published in a public repo.

### Which environments the guard trusts

⚠️ `StoreHubWebApplicationFactory.cs:35` calls `UseEnvironment("Testing")`, and supplies no
`LocalToken` configuration. So `IsDevelopment()` is **false** across the whole StoreHub integration
suite, and a naive `!IsDevelopment()` guard fails ~97 tests on contact.

**Decision: fail closed — Development is the only exemption** (Pond, 2026-08-20). Every other
environment name must supply a real key, including `Testing`, `Staging`, and any typo. The
alternative, guarding on `IsProduction()` alone, leaves a mistyped `ASPNETCORE_ENVIRONMENT` running
the known key on a public Droplet — silently, which is the failure mode this whole defect is about.

The cost is one line in `StoreHubWebApplicationFactory` injecting a per-run secret into the test
host's configuration. That is a fixture change, not a production concession.

### Defect I0-C — `/health/ready` is mapped twice

`app.MapDefaultEndpoints()` (`Program.cs:115`) maps `/health/ready` inside
`ServiceDefaults/Extensions.cs:86`. `Program.cs:183` then maps it again:

```csharp
app.MapGet("/health/ready", async (CloudDbContext db) => { ... });
```

Two endpoints, one route template, overlapping methods. Whether that throws
`AmbiguousMatchException` on request or silently resolves one way is **not asserted here** — it is
listed as an observation to make while the stack is running, per the standing rule that these get
executed rather than reasoned about.

Related and separate: `AddDefaultHealthChecks` registers exactly one check, `self`, tagged `live`
(`Extensions.cs:71`). The `/health/ready` mapping filters on the `ready` tag, so unless Aspire's
Npgsql component contributes a `ready`-tagged check, the readiness probe reports healthy with
nothing behind it. Also to be observed, not assumed.

**Consequence for this design:** the container's `HEALTHCHECK` targets `/health/live`. It is the
cheap probe by design, and it is unambiguously mapped once.

---

## Design

Four PRs. The dependency bump lands first because it moves the CQRS backbone every other project
compiles against. The auth fix comes next — it spans both hosts and is the one change here with a
security consequence, so it is reviewed on its own rather than buried in packaging. Then the schema
path, then the packaging that depends on both.

### PR 0 — `chore/bump-nokpirab-1.1.0`

`IndyPOS.Application` pins `Nokpirab 1.0.0`. Move it to **1.1.0**, the latest published version.

This is deliberately its own PR. `Nokpirab` supplies `ICommandHandler`/`IQueryHandler` — the
abstractions every use case, StoreHub endpoint, CloudApi endpoint and WinForms caller is written
against — so a breaking change between 1.0.0 and 1.1.0 would surface as a compile error across the
whole solution rather than in one place. Isolating it means the blast radius is legible, and the
packaging PR is not the thing that broke the build.

Verification is the full suite, not a targeted one: `dotnet build`, then `dotnet test` with Docker up
and the real store databases present. The baseline to hold is **637 total, 636 pass, 1 skip**
(`CLAUDE.md`, measured 2026-08-19), plus the installer's separate `231 → 223 pass / 8 skip`. Any
behavioural difference in the bump shows up as a delta against those numbers.

If 1.1.0 turns out to carry a breaking API change, this PR grows to include the call-site updates and
gets re-reviewed on its own merit — it does not silently expand into the PRs that follow.

### PR 1 — `fix/reject-default-jwt-secret`

Closes defect I0-B on both hosts.

**1. The predicate**, next to the options class in `IndyPOS.Application/Common/Models`, so it is
unit-testable without hosting: does this `LocalTokenOptions` still carry the built-in default
`SecretKey`? The default literal moves behind a named constant that both the property initialiser and
the predicate read, so they cannot drift apart.

**2. The refusal**, in both `CloudApi/Program.cs` and `StoreHub/Program.cs`, immediately after the
options are bound and before `AddAuthentication` wires the key in. Throws when the environment is
anything other than Development, with a message naming `LocalToken__SecretKey` and — for StoreHub —
pointing at the installer step that normally provides it. A specific exception type, not `Exception`.

**3. The fixture change:** `StoreHubWebApplicationFactory` injects a per-run secret, because it hosts
as `Testing` and the guard is fail-closed.

**Tests.** Predicate unit tests in `IndyPOS.Application.Tests` — default → true, configured → false,
and the empty/whitespace cases, red first. The suite baselines below are the regression evidence that
the refusal did not catch anything it should not have; the StoreHub integration suite in particular
either stays at its current count or the fixture change is wrong.

**Verify by running:** start StoreHub with `ASPNETCORE_ENVIRONMENT=Production` and no `LocalToken`
section, and read the failure. Then confirm Aspire's dev loop is untouched.

### PR 2 — `feat/cloudapi-migrate-verb`

**1. Initial EF migration for `CloudDbContext`**, in `src/IndyPOS.CloudApi/Infrastructure/Migrations/`.
Migrations live beside their context: StoreHub's are in `IndyPOS.Infrastructure` because
`StoreHubDbContext` is, and `CloudDbContext` is in CloudApi. Requires
`Microsoft.EntityFrameworkCore.Design` on the csproj.

The snapshot covers the nine `DbSet`s on `CloudDbContext` **and the OpenIddict EF tables** —
`OnModelCreating` calls `UseOpenIddict()`, so applications, authorizations, scopes and tokens are
part of the same model and the same migration.

**2. A `migrate` verb, shaped like StoreHub's.** After `builder.Build()`, before any endpoint is
mapped:

```csharp
else if (Array.Exists(args, a => string.Equals(a, "migrate", StringComparison.OrdinalIgnoreCase)))
{
    await app.MigrateCloudDatabaseAsync();
    return;
}
```

Schema work stays off the normal start path. StoreHub does this because doing it inline blocked the
service host's "Running" signal past the SCM's 30 s timeout on a fresh database (error 1053,
`StoreHub/Program.cs:182-196`). CloudApi has no SCM, but the same separation buys something else: a
one-shot container that either succeeds or fails *before* the API is allowed to start, which is what
`depends_on: service_completed_successfully` needs.

Development behaviour is unchanged — `EnsureCreatedAsync` stays for Aspire's fast loop.

**Verify by running:** `dotnet run --project src/IndyPOS.CloudApi -- migrate` against a throwaway
`docker run postgres:16-alpine`, then inspect the created tables — OpenIddict's included.

### PR 3 — `feat/cloudapi-docker`

**Location: `deploy/cloud/`, not the repo root.**

🚨 Docker Compose prefers `compose.yaml` over `docker-compose.yml` when both are present. A new
`compose.yaml` at the root would therefore make the existing StoreHub dev stack stop responding to a
bare `docker compose up` — silently, with no error to read. A subdirectory removes the precedence
question entirely, at the cost of `-f deploy/cloud/compose.yaml` on the command line.

| File | Purpose |
|---|---|
| `src/IndyPOS.CloudApi/Dockerfile` | Multi-stage image. **Build context is the repo root** — the project references `IndyPOS.Application`, `IndyPOS.Domain` and `IndyPOS.ServiceDefaults`, and inherits `Directory.Build.props`. Copy csproj/props first, restore, then copy the rest, so a source-only edit does not re-restore. |
| `.dockerignore` (root) | `bin/`, `obj/`, `.git/`, `.claude/`, `publish/`, `tests/`. Without it the build context includes the committed-adjacent `bin/Debug/net10.0` trees, which are large and useless to the image. |
| `deploy/cloud/compose.yaml` | Local full stack: `postgres:16-alpine` on its own volume → `cloud-api-migrate` one-shot → `cloud-api`. |
| `deploy/cloud/compose.prod.yaml` | Droplet deployment unit: the migrate one-shot and `cloud-api` only. No database service — that is the managed cluster. |
| `deploy/cloud/.env.example` | The variables that must be set, with the generator command for the RSA key. |
| `docs/operations/cloud-deployment.md` | Short runbook: bring it up locally, bring it up on the Droplet, where TLS plugs in. The full guide stays task I8. |

**Base images.** `mcr.microsoft.com/dotnet/sdk:10.0` to build, `mcr.microsoft.com/dotnet/aspnet:10.0`
to run. **Debian, not Alpine**: this system converts legacy timestamps to and from Asia/Bangkok and
handles Thai product names throughout, and the Alpine images ship neither ICU nor tzdata without
extra packages. The Debian runtime image has both. Chiseled variants are rejected for the same reason
`curl` is needed below — no shell, no probe binary.

**Runtime shape.** `USER $APP_UID` (the `aspnet` images define a non-root app user),
`ASPNETCORE_HTTP_PORTS=8080`, no HTTPS inside the container, and
`ENTRYPOINT ["dotnet", "IndyPOS.CloudApi.dll"]` so `migrate` can be passed as a command rather than
needing a second image. `curl` is installed in the final stage — roughly 1.5 MB — purely so
`HEALTHCHECK` and `depends_on: condition: service_healthy` have a probe to run; the aspnet image
ships neither curl nor wget. The probe is `GET /health/live`.

**Local stack runs as `Production`.** `deploy/cloud/compose.yaml` sets
`ASPNETCORE_ENVIRONMENT=Production` deliberately, supplying dev-only secrets from `.env.example`.
Running it as Development would exercise `EnsureCreatedAsync` and the dev signing certificates — i.e.
it would verify the one path that is *not* being shipped. Running it as Production means the local
verification covers the migration one-shot, the RSA key load, and the secret guard.

**TLS is a marked seam, not a stub.** No domain is registered yet, so `compose.prod.yaml` publishes
CloudApi on `127.0.0.1:8080` only, with a comment block stating exactly what a terminator must do
(forward to `cloud-api:8080` on the internal network, set `X-Forwarded-*`). Nothing half-built ships;
adding Caddy later is a ~10-line diff, and the firewall rules in the infrastructure guide already
assume 443 is the only public port.

**Operational settings** on `compose.prod.yaml`: `restart: unless-stopped`, and json-file logging
capped by size and file count — the Droplet has a 50 GB disk and an unbounded container log is a slow
outage.

---

## Configuration contract

What the container requires, and where each value comes from:

| Variable | Required | Source |
|---|---|---|
| `ConnectionStrings__cloud-db` | yes | Managed PostgreSQL connection string. Note the hyphen — the key must match `AddNpgsqlDbContext<CloudDbContext>("cloud-db")` at `Program.cs:34`. |
| `INDYPOS_RSA_SIGNING_KEY` | yes in production | `scripts/generate-rsa-key.ps1`. Absent → ephemeral certificates and tokens that die on restart. |
| `LocalToken__SecretKey` | yes in production | Generated per deployment. Startup now refuses the built-in default (defect I0-B). |
| `ASPNETCORE_ENVIRONMENT` | yes | `Production` on the Droplet, and also in the local stack, for the reason above. |
| `ASPNETCORE_HTTP_PORTS` | no | Defaults to 8080 in the image. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | no | `ServiceDefaults` wires the OTLP exporter only when this is set (`Extensions.cs:59-64`), so leaving it unset is a supported configuration. |

`.env` is gitignored; `.env.example` is committed and contains no real secret.

---

## Testing

**Automated.** `Application.Tests` gains the default-secret predicate tests (red first). No new test
project, and no new suite that needs Docker — the existing Docker-dependent count in `CLAUDE.md`
stays accurate.

**The suite baselines are the regression evidence**, and two of these four PRs can move them, so each
is re-measured rather than assumed: **637 total, 636 pass, 1 skip** for the solution (Docker up, real
store databases present), and **231 → 223 pass / 8 skip** for the installer, which `IndyPOS.sln`
excludes and `dotnet test` therefore never touches. PR 0 can move any of them; PR 1 specifically must
leave the **104** StoreHub integration tests where they are — that suite hosts as `Testing`, so it is
the one the fail-closed guard is most likely to disturb.

**By running, which is where this class of defect actually surfaces.** The console output and the
container's behaviour have no automated coverage, and Epic 2 established that nearly every real
defect was found by running the tool rather than by reading tests.

1. `dotnet run --project src/IndyPOS.CloudApi -- migrate` against a throwaway
   `docker run postgres:16-alpine`, then inspect the created tables — including OpenIddict's.
2. `docker build` from the repo root. First build proves the `Nokpirab` restore inside the container.
3. `docker compose -f deploy/cloud/compose.yaml up` — assert the migrate one-shot exits 0 and the API
   only then starts, and that the container reports healthy.
4. `curl` `/`, `/health/live`, `/health/ready`, `/sync/status`.
5. Drive a real client-credentials exchange against `/oauth/token` and call one authorized endpoint
   with the token. This is the check that proves the RSA key path and the OpenIddict tables together.
6. Start **both** hosts outside Development with no `LocalToken` section and confirm each refuses to
   boot, naming the variable to set. Then confirm Aspire's dev loop still starts clean.

**Observations to record while it is up** (open questions, not assumptions): whether
`GET /health/ready` throws `AmbiguousMatchException`; and whether any `ready`-tagged check exists
behind that route. Both feed follow-up work, not this design.

---

## Out of scope

- **Provisioning anything.** No Droplet, no managed database, no DNS. That is I1–I3.
- **TLS termination.** A seam, deliberately, until a domain exists.
- **Rotating an already-installed till's JWT secret.** The guard rejects the *default*; it does not
  re-key a store that already has a real one. No store runs v4 yet, so there is nothing to rotate.
- **The full cloud deployment guide** (task I8). PR 3 adds only what is needed to run what it ships.
- **Multi-instance concerns** — load balancer, sticky anything, `EventProcessor` running in two
  containers at once. Phase 4 of the infrastructure guide, and the one-shot migrate pattern is
  already the right shape for it.

---

## Rejected alternatives

**`EnsureCreatedAsync` in production.** No upgrade path, and it would exempt CloudApi from the
forward-only migration gate. Rejected.

**Migrating on API startup.** Simpler compose, but it races when more than one container starts, and
it repeats the mistake StoreHub already corrected. Rejected in favour of the one-shot.

**A single combined PR.** Mixes an auth-behaviour change with packaging. Reviewable separately, so
separated.

**Guarding on `IsProduction()` only.** Costs no test changes, but leaves `Staging` and every
mistyped `ASPNETCORE_ENVIRONMENT` authenticating admin callers with a key that is committed to a
public repo. Rejected in favour of failing closed.

**Changing the default `SecretKey` to something unguessable.** Moves the problem rather than solving
it: the value would still be in the repo, and every install would still share one key. Rejected.

**Alpine or chiseled base images.** Smaller, but Alpine needs ICU and tzdata added back for Thai text
and Bangkok conversion, and chiseled has no shell for a health probe. The size saving does not pay for
either.

**Compose files at the repo root.** Would shadow the existing StoreHub dev stack through Compose's
filename precedence. Rejected.
