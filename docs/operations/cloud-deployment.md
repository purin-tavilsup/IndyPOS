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

## Provision and deploy on DigitalOcean (Epic I: I1–I3)

Prerequisites on the dev box: Docker, `doctl` (`doctl auth init`).

### 0. Domain (Cloudflare)

Register `indypos.com` via **Cloudflare Registrar** (the zone lands on Cloudflare automatically).
Create an API token, resources limited to the `indypos.com` zone, with permissions
**Zone → Zone → Read** and **Zone → DNS → Edit** — Caddy needs `Zone:Read` to resolve the zone id
for the `_acme-challenge` record and `DNS:Edit` to write it.

### I1 — Droplet

Create a VPC in `sgp1`, then a Droplet inside it:

| Component | Recommendation |
|---|---|
| Droplet type | Basic — Premium AMD |
| CPU / Memory / Disk | 1 vCPU / 2 GB / 50 GB SSD |
| Runtime | Docker (DO Docker Marketplace image) |
| Region | `sgp1` (Singapore) |
| Backups | Enabled |

Cloud firewall: allow **22** from your admin IP, allow **443** from Cloudflare's published IPv4 + IPv6
ranges, deny everything else. No inbound 80, 8080, or 5432.

### I2 — Managed PostgreSQL

Create DO Managed PostgreSQL, smallest production tier, `sgp1`, in the same VPC. Restrict its trusted
sources to the Droplet. Create the database and copy the **private** connection string — put it in
`.env` as `CLOUD_DB_CONNECTION`; `compose.prod.yaml` maps it into the container as
`ConnectionStrings__cloud-db` (the hyphen matters — it maps to `AddNpgsqlDbContext<CloudDbContext>("cloud-db")`).

### Registry + image

Create a DO Container Registry (Starter tier). On the **dev box**, `doctl registry login` (read-write
— it builds and pushes), then build and push:

```powershell
pwsh -File scripts/cloud/push-image.ps1 -Registry <registry-name>
```

It prints the `CLOUDAPI_IMAGE=...` line for `.env`. The script refuses to push from a dirty working
tree (the image would not match its tag's commit) and tags each image `yyyyMMddTHHmmssZ-<sha>`, so a
rebuild never overwrites an existing tag.

The **Droplet only pulls**, so give it a **read-only** credential — not the read-write dev login.
DO registry credentials expire, and an expired one makes a later `docker compose pull` fail with
`unauthorized`. Two workable strategies:

- **Long-lived read-only docker config (simplest):** on the dev box, generate a read-only,
  long-expiry credential and copy it into the Droplet's `~/.docker/config.json`:
  ```bash
  doctl registry docker-config --read-only --expiry-seconds 15552000 > docker-config.json  # ~180d
  ```
  Note the expiry and diarise re-issuing before it lapses.
- **Re-auth per deploy:** run `doctl registry login --read-only` (add `--expiry-seconds` to bound it)
  on the Droplet as the first step of every deploy, so each pull uses a fresh credential.

(Confirm the exact `doctl` flags against your installed version; the invariant is *read-only on the
Droplet, with a credential that is either long-lived-and-diarised or refreshed each deploy*.)

### Cloudflare DNS

Add an **A record** `api` → the Droplet's public IP, **proxied** (orange cloud). Set the zone's
SSL/TLS mode to **Full (strict)**.

### Secrets → `.env` on the box

`cp deploy/cloud/.env.example deploy/cloud/.env` and fill every value:

| Key | Source |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | fixed `Production` |
| `LocalToken__SecretKey` | `openssl rand -base64 64` |
| `INDYPOS_RSA_SIGNING_KEY` | `scripts/generate-rsa-key.ps1` |
| `CLOUD_DB_CONNECTION` | the I2 private connection string (mapped into the container as `ConnectionStrings__cloud-db`) |
| `CLOUDAPI_IMAGE` | `push-image.ps1` output |
| `CLOUDAPI_DOMAIN` | `api.indypos.com` |
| `CLOUDFLARE_API_TOKEN` | the scoped Cloudflare token (`Zone:Read` + `DNS:Edit`, `indypos.com` only) |

### I3 — Deploy

```bash
cd deploy/cloud
docker compose -f compose.prod.yaml pull          # cloud-api from the registry
docker compose -f compose.prod.yaml build caddy   # thin custom Caddy image, once
docker compose -f compose.prod.yaml up -d
```

The migrate one-shot runs first and must exit 0; the API will not start otherwise.

### Registry retention (after the deploy verifies)

DO Container Registry garbage collection only reaps **unreferenced** manifests, so every immutable
timestamp tag stays referenced and counts against the 500 MiB Starter cap forever. Once the new
release is verified healthy, delete old release tags down to a small rollback window, **then** GC:

```bash
# Keep latest + the current tag + the previous 2-3; delete older ones.
doctl registry repository list-tags indypos-cloudapi
doctl registry repository delete-tag indypos-cloudapi <old-tag>        # repeat per obsolete tag
doctl registry garbage-collection start --include-untagged-manifests
```

(Confirm the exact `doctl` flags against your installed version; the sequence — delete obsolete
release tags/manifests, then GC with untagged cleanup — is the invariant.)

### Verify (acceptance)

1. `docker compose -f compose.prod.yaml ps` — migrate `Exited (0)`, `cloud-api` and `caddy` up.
2. The managed database has the 13 expected tables.
3. Caddy's log (`docker compose logs caddy`) shows a certificate obtained for `api.indypos.com`.
4. `curl https://api.indypos.com/health/live` returns `200`.
5. Register a store, then `POST https://api.indypos.com/oauth/token` returns `200` with a token.
6. A direct request to the Droplet IP on 443 from a non-Cloudflare address is refused; 8080 is
   unreachable off the box.

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

TLS is terminated by the `caddy` service in `compose.prod.yaml`, which obtains a publicly-trusted
certificate (Let's Encrypt, with ZeroSSL as Caddy's automatic fallback) via the Cloudflare DNS-01
challenge and reverse-proxies plain HTTP to
`cloud-api:8080` over the private `appnet` network. `cloud-api` publishes no host port. Cloudflare
sits in front in Full (strict) mode, so traffic is encrypted till→Cloudflare and Cloudflare→Caddy.

**The token endpoint requires HTTPS, and the container serves HTTP — this is now configured, not
broken.** OpenIddict rejects plain-HTTP token requests with `400` and
`error_uri: https://documentation.openiddict.com/errors/ID2083`. It decides whether a request "is
HTTPS" from `HttpContext.Request.IsHttps`, which reflects the connection the *container* sees — not
the connection the client made to the terminator. So terminating TLS upstream does **not**, by
itself, satisfy the check.

Both compose files therefore declare `OpenIddict__TlsTerminatedUpstream: "true"`, which makes
`AddOpenIddictServer` call `DisableTransportSecurityRequirement()`
(`src/IndyPOS.CloudApi/Infrastructure/Auth/OpenIddictExtensions.cs`).

**It defaults to `false` in code.** An unconfigured host — the image run without these compose files —
stays strict and refuses to issue tokens over cleartext rather than doing it silently. That is the
same fail-closed posture as the JWT signing-key guard.

⚠️ **Turning it on is a statement about your topology, and it is only true if you keep it true.** With
the flag on, this container will issue access tokens to anything that can reach it over plain HTTP.
That is safe only while the container is unreachable except through a TLS terminator — in
`compose.prod.yaml`, `cloud-api` publishes no host port and sits on the private `appnet` network,
reachable only through the `caddy` service, for exactly this reason. If you ever publish port 8080 on
a public interface, or put the container on a shared network, set the flag back to `false` first.

Verified by controlled comparison against the same image, same request, only the flag differing:

| `OpenIddict__TlsTerminatedUpstream` | Response to `POST /oauth/token` over plain HTTP |
|---|---|
| `false` (code default) | `400 invalid_request` — "This server only accepts HTTPS requests." (ID2083) |
| `true` (both compose files) | past the transport gate — a registered store now receives `200` with an access token (defect I0-E fixed 2026-09-17) |

The alternative — `UseForwardedHeaders()` trusting the terminator's `X-Forwarded-Proto` — was
considered and rejected for now. Container bridge addresses are dynamic, so it would require clearing
`KnownNetworks`/`KnownProxies`, i.e. trusting that header from anyone who can reach the container.
That is the same practical exposure as the flag above, with more moving parts. It becomes the stronger
option once a terminator exists at a known address, and is worth revisiting then.

**Store-to-cloud authentication now works end to end (defect I0-E, fixed 2026-09-17).** Registration
writes exactly one client registry: `RegisterStoreHandler` creates the OpenIddict application (via
`IStoreClientCredentialStore`) *and* the `StoreConfigs` row in a single transaction, so the
`client_id` OpenIddict's server pipeline validates against `OpenIddictApplications` is always present.
`CloudStoreConfig` no longer stores a `ClientSecretHash` — OpenIddict owns the secret — and
`TokenController` no longer re-verifies it, keeping only its `IsActive` gate. Verified against real
PostgreSQL: register → `POST /oauth/token` returns `200` with a persisted access token (was
`401 invalid_client`).

⚠️ **The transaction must run through the Npgsql retrying execution strategy.** Aspire's
`AddNpgsqlDbContext` enables retry-on-failure, under which a bare `BeginTransactionAsync` throws
`InvalidOperationException`. `RegisterStoreHandler` wraps the write in
`Database.CreateExecutionStrategy().ExecuteAsync(...)`; do not unwrap it. The InMemory unit tests
cannot catch a regression here (no retry strategy) — only a real-PostgreSQL run can.

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

**Never `docker compose down -v` in production.** `-v` deletes the named `caddy_data` volume, wiping
Caddy's ACME account and issued certificate — a plain `docker compose down` leaves named volumes
intact. Only pass `-v` when you intend to discard Caddy's TLS state.

**The 443 firewall allowlist is recurring maintenance.** Cloudflare's published IPv4/IPv6 ranges
change over time. Periodically compare the DO cloud firewall's 443 rule against Cloudflare's current
published ranges and update it if they drift, or direct clients could be blocked / the origin could
become reachable off-Cloudflare. Automating the sync is out of scope for I1–I3.

## Follow-ups (post-I0-E, from the 2026-09-17 review)

Surfaced by the independent review that closed I0-E. None block correctness; captured here because this
repo tracks work in docs, not GitHub issues.

1. **No automated test proves a *wrong* client secret is rejected end to end.** The property is correct
   — the store's OpenIddict application is `Confidential`, so the server pipeline authenticates the
   secret before `TokenController` runs — but it rests on that config plus the manual E2E, with no
   regression cover. A `WebApplicationFactory` test hitting the real `/oauth/token` with a valid client
   and a wrong secret, asserting the request fails, would pin it. A misconfiguration (e.g. the client
   registered as `Public`, or a missing permission) could silently break the security property today
   with no test failing.
2. **`IsActive` gates token *issuance* only.** Deactivating a store blocks new tokens, but an
   already-issued access token stays valid until it expires (15 min). Acceptable for 15-minute tokens;
   documented so it is a known bound, not a surprise.
3. **Endpoint authorization ignores scopes (pre-existing, not introduced by I0-E).** `/sync/*` and
   `/master/*` use plain `[Authorize]` (authenticated-only) with no scope requirement, and every store
   is granted both `sync.write` and `master.read`, so the scope separation is currently decorative.
   Worth enforcing per-endpoint scopes when the sync/master split needs to mean something.
