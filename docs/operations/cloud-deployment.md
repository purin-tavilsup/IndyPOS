# CloudApi deployment

Covers the container and the two compose stacks in `deploy/cloud/`. Provisioning the Droplet and the
managed database is Epic I tasks I1–I3; the full operations guide is task I8.

## Run it locally

```bash
cd deploy/cloud
docker compose up --build
```

Brings up PostgreSQL on 5433 (5432 belongs to the StoreHub dev stack at the repo root), applies EF
migrations in a one-shot container, then starts the API on <http://localhost:8080>.

The stack runs as `Production` on purpose — that is the path that ships. There is no Scalar/OpenAPI
UI as a result; for that, use the Aspire dev loop instead.

## Run it on the Droplet

1. Copy the repository to the box, or build the image elsewhere and push it to a registry.
2. `cp deploy/cloud/.env.example deploy/cloud/.env` and fill in every value.
   - `ConnectionStrings__cloud-db` — the managed cluster's string. The hyphen matters.
   - `LocalToken__SecretKey` — `openssl rand -base64 64`. The API refuses to start without it.
   - `INDYPOS_RSA_SIGNING_KEY` — from `scripts/generate-rsa-key.ps1`. Without it, every restart
     invalidates every issued token.
3. `cd deploy/cloud && docker compose -f compose.prod.yaml up -d --build`

The migrate one-shot runs first and must exit 0; the API will not start otherwise.

## Health

- `/health/live` — process up. This is what the container's `HEALTHCHECK` probes. Served by
  `MapHealthChecks` (`ServiceDefaults/Extensions.cs`), predicate `"live"`, and it is the only
  endpoint of the two that is actually reachable as designed.
- `/health/ready` — **mapped twice in CloudApi, and the second registration is dead code.**
  `MapDefaultEndpoints()` (`ServiceDefaults/Extensions.cs`) registers
  `MapHealthChecks("/health/ready", …)` filtered to checks tagged `"ready"`; `CloudApi/Program.cs`
  separately registers `MapGet("/health/ready", …)` that does a real `CanConnectAsync()` against the
  database. Verified against a running container: **no `AmbiguousMatchException`.** ASP.NET Core's
  routing prefers the endpoint carrying explicit `HttpMethodMetadata` (the `MapGet`, GET-only) over
  the one with none (`MapHealthChecks` maps via a generic `Map`, no method constraint), so the
  `Program.cs` handler silently wins every request — confirmed by the response body
  (`{"status":"healthy","database":"connected"}`, not the plain-text `Healthy` that `/health/live`
  returns) and by the server log, which reads `Executing endpoint 'HTTP: GET /health/ready'` rather
  than `'Health checks'`.

  **This is a CloudApi-only observation.** CloudApi's `AddServiceDefaults()` registers no check
  tagged `"ready"` — `AddDefaultHealthChecks` only ever registers a `self` check tagged `"live"`, and
  nothing in `CloudApi/Program.cs` adds a `"ready"`-tagged one — so even the dead `MapHealthChecks`
  registration would have reported a false "ready" (an empty check set reports `Healthy` by
  middleware convention) had it ever been reached. That is why the container's `HEALTHCHECK` probes
  `/health/live` instead. Do not rely on CloudApi's `/health/ready` for anything until this is
  resolved as its own defect — packaging is not the place to fix it.

  **⚠️ This does NOT generalize to StoreHub — do not "clean up" its `/health/ready` as dead code.**
  `src/IndyPOS.StoreHub/Program.cs` (around line 79) registers a real check:
  `AddDbContextCheck<StoreHubDbContext>("storehub-db", tags: ["ready"])`, so StoreHub's
  `MapHealthChecks("/health/ready", …)` mapping is genuine and does exercise the database. It is also
  **load-bearing for the installer**: `installer/IndyPOS.Bootstrapper/Installers/HealthProbe.cs`
  polls it to decide whether an upgrade (or a post-rollback restart) actually came back up, and
  `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs`'s `IsHealthyAsync` polls the
  same endpoint at runtime. Removing or "fixing" StoreHub's `/health/ready` on the strength of the
  CloudApi finding above would break the installer's rollback gate.

## TLS

Nothing in these files terminates TLS, and no domain is registered yet, so `compose.prod.yaml` binds
the API to `127.0.0.1:8080`. See the marked seam in that file for what a terminator must do.

**Known limitation — the token endpoint needs HTTPS, and the container only serves HTTP.**
OpenIddict's token endpoint (and every other OpenIddict endpoint — `/oauth/token`,
`/.well-known/openid-configuration`, `/.well-known/jwks`) rejects plain HTTP with `400` and
`error_uri: https://documentation.openiddict.com/errors/ID2083`, because
`DisableTransportSecurityRequirement()` is never called. This was verified against the containerized
API with a real RSA signing key configured (`INDYPOS_RSA_SIGNING_KEY`), ruling out the
ephemeral-development-certificate fallback as the cause.

**Putting a TLS terminator in front does NOT resolve this by itself.** OpenIddict decides whether the
request "is HTTPS" from `HttpContext.Request.IsHttps`, which reflects the connection the *container*
sees — not the connection the client made to the terminator. With TLS terminated upstream and no
forwarded-headers handling in this app, the container still sees a plain `http` request from the
terminator and still rejects it with ID2083. Resolving this needs one of two things, **and the choice
is deliberately deferred to the repository owner as a security decision, not made here**:

- Call `DisableTransportSecurityRequirement()` — accepts plain HTTP into the container outright, or
- Configure `UseForwardedHeaders()` so the app trusts the terminator's `X-Forwarded-Proto` header,
  which requires the terminator to be configured to set that header and the app to trust it only
  from the terminator's address.

Do **not** add either mechanism to this repository's C# yet, and do not work around ID2083 by
generating a certificate inside the container — a terminator is necessary infrastructure either way,
but is not sufficient on its own, and picking between the two options above is left to the repository
owner.

**Known limitation — store-to-cloud authentication does not work end to end yet, independent of the
TLS issue above.** Store registration (`RegisterStoreHandler`) writes OAuth2 client credentials —
a generated `ClientId` and a BCrypt-hashed `ClientSecret` — to this application's own `StoreConfigs`
table, and `TokenController`'s `/oauth/token` handler verifies an incoming `client_id`/`client_secret`
against that same table. But `AddOpenIddictServer` configures OpenIddict's core store
(`OpenIddictExtensions.cs`) to use EF Core against `CloudDbContext`, which gives OpenIddict its own
`OpenIddictApplications` table — and OpenIddict's server pipeline validates the incoming `client_id`
against *that* table before the request ever reaches `TokenController`. Nothing in `src/` ever
creates a row in `OpenIddictApplications`, so every token request is rejected with
`401 invalid_client` regardless of whether the store registered successfully against `StoreConfigs`.
Closing this belongs to Epic I task I4 (SyncWorker against the real CloudApi); until then, treat
store-to-cloud authentication as not working end to end.

## Schema changes

Forward-only, same gate as StoreHub: additive, nullable or defaulted, no renames or drops. See
`docs/operations/upgrade-procedure.md`. Generate migrations with:

```bash
dotnet ef migrations add <Name> --project src/IndyPOS.CloudApi --output-dir Infrastructure/Migrations
```

Never hand-write one.

## Operational notes

**Stale Aspire dev volume won't gain new tables.** `src/IndyPOS.AppHost/Program.cs` mounts the
`cloud-db` Postgres with `WithDataVolume("indypos-postgres-data")`, and CloudApi's dev startup path
provisions the schema with `EnsureCreatedAsync()` (`Program.cs`), which is a no-op once the database
already has any tables. So an Aspire dev `cloud-db` volume created before the OpenIddict entities
existed will NOT gain the four new `OpenIddict*` tables on a later run, and `/oauth/token` (and
anything else touching those tables) will keep failing with a 500 there until the volume is dropped.
To fix it: stop the AppHost, then remove the named volume (`docker volume rm indypos-postgres-data`,
or find its actual name with `docker volume ls` if Aspire suffixed it) and restart the AppHost so the
container is recreated empty and `EnsureCreatedAsync()` builds the full schema fresh.

**A migrate failure on the managed cluster does not self-heal.** `compose.prod.yaml`'s
`cloud-api-migrate` one-shot has `restart: "no"` and nothing gates it on the external managed
database actually being reachable first. If the managed cluster is briefly unavailable when
`docker compose -f compose.prod.yaml up -d` runs, the migrate one-shot exits non-zero, and the
`cloud-api` service — which depends on `cloud-api-migrate` completing successfully — never starts.
Compose does not retry this on its own. Once the database is confirmed reachable again, re-run
`docker compose -f compose.prod.yaml up -d` from the Droplet to retry the migration and bring the
API up.
