# Cloud deployment runbook — platform refresh (2026-10-23)

> **Status: transition / not yet a production-ready copy-paste procedure.** This section supersedes conflicting instructions below. The historical I1–I3 procedure is retained for implementation reference, **not** as the current production deployment contract. Do not deploy until the Auth/Web implementation, production compose, configuration, and verification steps are reconciled with the approved design.

## Architecture diagrams

- [DigitalOcean cloud infrastructure and service relationships](../diagrams/cloud-infrastructure.md)
- [GitHub Actions → DigitalOcean CI/CD and deployment relationships](../diagrams/cloud-cicd-deployment.md)

Both diagrams describe the **approved target** and **recommended workflow**, not a completed deployment.

## Source of truth and deployment gates

- Approved platform/identity design: `docs/superpowers/specs/2026-10-21-cloud-platform-and-identity-design.md` (approved 2026-10-22).
- Existing single-API deployment: `deploy/cloud/compose.prod.yaml` and `deploy/cloud/.env.example` (**legacy shape** until updated).
- Deployment order: sign-in spike (Claude proven; ChatGPT not yet tested) → Auth + Web admin implementation → infrastructure provisioning/deploy → store rollout. The spike is **not** production Auth.
- **Stop/go gate:** verify on the target commit that Auth and Web are implemented, tests pass, the compose stack actually includes the new services and migrations, and the configuration names below match deployed code. Do not assume an approved design is already implemented.
- Record the target Git SHA, operator, deployment time, DB backup/restore drill result, and tested rollback procedure before going live.

## Target production topology

```text
StoreHub (offline-first) --HTTPS--> Cloudflare proxy --HTTPS--> Caddy (Droplet :443)
                                                        |-- indypos-api.exconeer.com  -> CloudApi
                                                        |-- indypos-auth.exconeer.com -> Cloud.Auth
                                                        |-- indypos.exconeer.com      -> Cloud.Web
                                                        '-- indypos-mcp.exconeer.com  -> future Cloud.Mcp (NOT deployed now)

Droplet: Docker Engine + Docker Compose, independent service containers
DO Managed PostgreSQL (same sgp1 VPC): separate CloudApi and Auth databases,
separate least-privilege database users. No direct public DB access.
```

- Domain: **`exconeer.com`**, registered through Cloudflare Registrar **only after confirming availability and completing purchase**. The design's wording is an intended purchase, not proof of registration.
- Flat subdomains are intentional: Cloudflare's standard edge certificate does not automatically cover nested names such as `auth.indypos.exconeer.com`.
- Cloudflare proxy enabled for each public host; SSL/TLS **Full (strict)**. Caddy terminates HTTPS at origin with publicly trusted certificates using Cloudflare DNS-01.
- Caddy needs a zone-scoped API token with Zone:Read and DNS:Edit; do not share it with API/Auth/Web containers. Keep `/data` persistent for ACME state.
- Public ingress: TCP 443 from current Cloudflare IPv4/IPv6 ranges; SSH 22 only from a restricted admin source; no public 80, 8080, 5432, or direct application container ports. Maintain the Cloudflare allowlist as published ranges change.
- Protect internal service calls; do not treat Docker's bridge network as an authorization boundary. Only explicitly intended routes should be reachable through Caddy.
- Use separate Compose projects/networks for unrelated products (e.g. future Workforce SaaS). Do not put unrelated services on the IndyPOS application network.

## Infrastructure sizing and provisioning

| Item | Initial recommendation | Validation |
|---|---|---|
| Region | DigitalOcean `sgp1` | Same region/VPC for Droplet and database |
| Droplet | Basic Premium AMD, **2 vCPU / 4 GiB RAM** as initial planning size | Benchmark all intended containers; adjust for actual memory/CPU, not store count alone |
| Container orchestration | Docker Compose (single host) | No Kubernetes required for initial offline-first deployment |
| Database | DO Managed PostgreSQL, separate Auth and business databases/roles | Confirm plan, connection limits, backups, PITR/restore options |
| Backups | Enable Droplet backups and managed DB backups | Perform a documented **restore drill before real data** |
| Monitoring | DO host metrics, external HTTPS uptime, container logs/health | Alert on 5xx, DB readiness, disk, memory, sync backlog and cert renewal |
| Registry | Existing DO Container Registry, immutable tags | Ensure read-only pull credentials and tag retention/GC |

The old 1 vCPU / 2 GB sizing and the expected **13 tables** below are historical estimates and acceptance criteria. Recalculate schema/table expectations from current migrations (including the separate Auth DB) before acceptance.

## Preflight checklist (must pass)

- [ ] Confirm `exconeer.com` registration, Cloudflare zone, scoped DNS token and proxy/TLS settings.
- [ ] Confirm Auth, Cloud.Web, and CloudApi implementations are merged and integration tests pass.
- [ ] Update `deploy/cloud/compose.prod.yaml`, Caddyfile, Docker build/push scripts and `.env.example` to the **three-service** topology; validate with `docker compose -f compose.prod.yaml config`.
- [ ] Publish immutable, traceable images for **each** application and use read-only registry credentials on Droplet; avoid mutable `latest` in production.
- [ ] Provision separate Auth and CloudApi databases, unique roles and restricted trusted sources; use private connection strings and TLS to PostgreSQL.
- [ ] Configure production issuer, external callback/redirect URIs, Google OAuth credentials, initial-admin Google email, token-signing keys, inter-service authentication and per-service secrets. Never commit real secrets or print them in logs.
- [ ] Restrict public host/port exposure; verify only Caddy publishes 443 and app containers have no host ports. Verify forwarded HTTPS scheme and issuer consistency in Auth.
- [ ] Confirm migration execution/order for **both databases** and define failure recovery. A failed one-shot migration requires operator intervention; never blindly retry a destructive migration.
- [ ] Document and perform managed DB restore drill; verify data recovery and retained backups before onboarding a real store.
- [ ] Validate offline POS sales continue when CloudApi/Auth/DNS are unavailable; queued sync eventually resumes without duplicate transactions.

## Deployment sequence (operator checklist; commands only after implementation gate)

1. Provision DO VPC, Droplet, firewall, managed PostgreSQL and two isolated databases/roles; enable backups and monitoring.
2. Configure Cloudflare flat DNS records `indypos`, `indypos-auth`, `indypos-api` to the Droplet public IP, proxied; set Full (strict).
3. Deploy scoped secret configuration on the Droplet (permissions `0600`), including the Caddy DNS token only in Caddy. Replace old `indypos.com` values everywhere, including StoreHub cloud/auth URLs.
4. Build/push immutable Auth, API and Web images. Verify provenance, digest and registry pull access.
5. Validate final compose/Caddy config, then execute documented one-shot database migrations for Auth and CloudApi. Check migration exit codes and schema expectations.
6. Start the services with `docker compose -f compose.prod.yaml up -d` **only after the compose file has been updated**. Inspect `docker compose -f compose.prod.yaml ps` and logs.
7. Confirm DNS, Caddy certificates, HTTPS issuer/redirects, API liveness **and** readiness, Auth discovery/PKCE/Google sign-in, and Cloud.Web bootstrap admin.
8. Through Cloud.Web, register a test store, confirm one-time credential display and store client-credentials token from **Auth**; test authorized sync and manager store isolation.
9. Simulate Cloud/API/Auth outage and recovery with StoreHub; verify offline sales, bounded retries, eventual sync, idempotency and no data loss.
10. Record successful restore drill, external uptime alert, rollback procedure and deploy SHA. Then onboard stores gradually.

### Required security and acceptance checks

- [ ] `https://indypos-api.exconeer.com/health/live` and `/health/ready` work as implemented; readiness verifies DB, not just process. Use the final app's documented endpoints for Auth/Web (do not invent paths).
- [ ] Cloud.Auth is the **sole issuer** for both human and store tokens; CloudApi only validates JWTs, audiences, roles and per-request store assignments.
- [ ] Bootstrap admin signs in via Google; invite links are single-use, expire in 2 days, and uninvited sign-ins fail.
- [ ] Manager cannot access unassigned stores through any CloudApi endpoint; deactivated stores cannot obtain new tokens.
- [ ] Token endpoints and OAuth metadata report correct external HTTPS scheme and issuer; no direct cleartext path to token issuance.
- [ ] Cloudflare bypass/direct-origin access is blocked by firewall; app and database ports remain private.
- [ ] Backups restore successfully; secrets are not committed; log retention, image cleanup and certificate renewal are operational.

## Rollback and outage response

- **App failure:** retain previous immutable image digests and compose configuration. Roll back application images only when compatible with the **forward-only** DB schema; never assume database down-migrations are safe.
- **Migration failure:** stop rollout, inspect migration logs and DB reachability, confirm DB state and backup, fix cause, then rerun the migration explicitly. Do not run `docker compose down -v` in production.
- **Droplet outage:** POS remains offline-first; restore Droplet/service from infrastructure configuration and verify database connectivity, then monitor queued sync catch-up.
- **Database incident:** use DO managed DB recovery procedure and tested restore point; reconcile sync/idempotency after recovery.
- **Auth outage:** existing offline tills keep selling; new token issuance, web sign-in and cloud-only operations may be unavailable. Alert and restore Auth independently.
- **Security incident:** revoke/rotate compromised credentials and signing material using a planned key-rollover process; disable affected store credentials in Auth.

## Recommended CI/CD and release operations (planned; not yet implemented)

**Deployment approach:** GitHub Actions → DigitalOcean Container Registry → SSH-triggered Docker Compose on the Droplet. This is the recommended target workflow, **not a claim that the repository currently contains a working production pipeline**. Preserve the existing `push-image.ps1` manual procedure until the pipeline is implemented and validated.

### Release policy

1. Every PR: build, run automated tests and security checks; do not expose production secrets to PR workflows.
2. On a selected release commit/tag: build **separate Auth, CloudApi and Web images**, tag immutably with commit SHA (and optionally timestamp), push to DO Container Registry, record their image digests.
3. Deploy production only through a protected GitHub Environment with **required reviewer approval**. Do not automatically deploy every merge to `development`.
4. After approval, an authorized GitHub Actions job connects to the Droplet using a **restricted deploy SSH identity** and invokes a small, audited deploy script on the host. Prefer a GitHub-hosted runner initially; a self-hosted runner adds production-host execution and maintenance risks.
5. The Droplet pulls images using **read-only** registry credentials. Avoid copying registry push credentials or long-lived cloud API tokens into production. Rotate pull credentials before expiry; prefer short-lived credentials refreshed by a controlled mechanism where feasible.
6. On the Droplet, write an immutable **release manifest** recording git SHA, image digests for each service, config version, migration identifiers, timestamp and operator. Retain the prior manifest for rollback.
7. Deploy and verify in the order below. Report success/failure to the GitHub Environment; failed verification must not be reported as a successful deployment.

### Deployment procedure (to automate)

```text
GitHub release / manual dispatch
  → CI tests + immutable image build/push
  → production environment approval
  → SSH to Droplet: acquire exclusive deployment lock
  → preflight: config, registry pull, disk, DB connectivity, backup status
  → record previous release manifest
  → run explicit one-shot DB migrations (Auth and CloudApi)
  → start/update Compose services with pinned image digests
  → check HTTPS endpoints, OAuth issuer/login, API readiness and store token flow
  → smoke-test sync; watch error rates and sync backlog
  → mark release healthy and retain rollback metadata
```

**Operational guardrails:**

- Serialize deployments with a lock (GitHub Actions concurrency plus host-side lock) so migrations and service updates cannot race.
- Run migrations **inside the Droplet/VPC**, never directly from a public GitHub-hosted runner to the private managed database. Require migrations to exit 0 before dependent services start; inspect partial schema state on failure.
- Use **expand-and-contract** database changes. Take/verify backups before high-risk migrations, but **do not rely on restoring a database as an ordinary app rollback**. A prior application image must remain compatible with the upgraded schema.
- Keep production secrets in restricted server-side files or a suitable secret manager. Set restrictive file permissions, mask CI logs, separate per-service secrets and use scoped Cloudflare DNS credentials only for Caddy. Do not put secrets in image layers, workflow output, release manifests or Git.
- Ensure Auth's signing keys persist across container replacement and have a documented key rotation/revocation strategy; do not regenerate signing keys on every deploy. Verify key storage and recovery during restore drill.
- Validate `docker compose -f compose.prod.yaml config` without printing interpolated secrets into logs; use `config --quiet` where supported. Pin image digests for repeatability.
- Maintain external uptime probes and alerts for API/Auth/Web, DB readiness, host disk/RAM/CPU, certificate renewal and StoreHub sync backlog. Separate liveness from readiness: DB failure should not cause an endless process-restart loop.
- **Single-host downtime is acceptable for the pilot** because tills continue offline, but document the interruption to cloud-only login, dashboard, approvals and remote access. Schedule risky releases during low usage and use a short smoke-test window.
- Keep application logs bounded (existing Docker log rotation), retain deploy audit records and prune obsolete registry tags only after verifying the new release and retaining rollback images.

### Failure handling and rollback

| Failure | Response |
|---|---|
| Build/tests fail | Do not publish/deploy release |
| Image pull or preflight fails | Leave running release unchanged; fix credentials/network/config |
| Migration fails | Stop rollout; inspect DB state and logs; restore only via incident recovery plan |
| New service fails readiness/smoke tests | Restore **previous pinned application images/config**, only if schema-compatible; verify Auth issuer and store sync |
| Signing-key or secret loss | Treat as a security/recovery incident; recover from protected backups or execute planned rotation |
| Host/database outage | Keep tills selling offline; recover infrastructure and validate sync catch-up/idempotency |

### Milestone plan

- **M1 Infrastructure ready:** purchased domain and DNS, sgp1 VPC/Droplet/firewall, managed PostgreSQL, Caddy and TLS.
- **M2 Deployment ready:** production three-service Compose/Caddy/env, CI image pipeline, protected manual approval, SSH deploy script, migrations, immutable release manifests, rollback and monitoring.
- **M3 Production ready:** Auth/Web implementation, admin bootstrap, store registration, backup **restore drill**, security/E2E tests, offline-outage test and monitored pilot rollout.

**No Kubernetes, Redis or queue broker required for the initial single-Droplet pilot.** Revisit multi-host orchestration/HA only when uptime objectives, workload, or independent scaling requirements justify the complexity.

## Outstanding implementation tasks

1. Implement and verify Auth + Cloud.Web admin slice from approved design.
2. Update production Compose/Caddyfile and build pipeline for new services; keep migration jobs isolated and idempotent.
3. Update `.env.example`, domain references, StoreHub Auth/API endpoints, OAuth callback configuration and deployment scripts.
4. Add production-grade tests for authorization boundaries, store token rejection, registration and recovery.
5. Replace historical single-API acceptance criteria and table counts; run backup/restore and outage drills.

---

## Historical I1–I3 runbook (superseded where conflicting)

The following section documents the **old CloudApi-only** stack and `indypos.com` assumptions. It is retained for technical reference while the deployment assets are migrated. **Do not execute its provisioning/deploy steps verbatim against the new architecture.**

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

The **Droplet only pulls**, so give it a read-only credential rather than the read-write dev login.
`doctl registry docker-config` already produces a **read-only** credential (that is its default),
while `doctl registry login` credentials expire after **30 days** by default — and an expired one
makes a later `docker compose pull` fail with `unauthorized`. Two workable strategies:

- **Long-lived docker config (simplest):** on the dev box, generate a read-only credential with an
  explicit expiry and copy it into the Droplet's `~/.docker/config.json`:
  ```bash
  doctl registry docker-config --expiry-seconds 15552000 > docker-config.json  # ~180d, read-only
  ```
  Note the expiry and diarise re-issuing before it lapses.
- **Re-auth per deploy:** run `doctl registry login --read-only=true --expiry-seconds <n>` on the
  Droplet as the first step of every deploy, so each pull uses a fresh, bounded credential.

(Confirm the exact flags against your installed `doctl`; the invariant is *read-only on the Droplet,
with a credential that is either long-lived-and-diarised or refreshed each deploy*.)

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
- `/health/ready` — readiness: one `SELECT 1` against `cloud-db`, registered by
  `AddDatabaseReadinessCheck("cloud-db")` (`ServiceDefaults/DatabaseReadinessCheck.cs`). It answers
  the plain text `Healthy` (200) or `Unhealthy` (503), and a hanging database answers within about
  3 seconds. The container's `HEALTHCHECK` still probes `/health/live`: it is the cheap probe, and a
  database outage should not get the process restarted.

  **History (defect I0-C, resolved 2026-10):** until then CloudApi mapped `/health/ready` twice, once
  through `MapDefaultEndpoints()` and once through a hand-rolled `MapGet` that answered
  `{"status":"healthy","database":"connected"}` and returned `ex.Message` on failure. Against a
  running container routing always picked the `MapGet`, and the tag-filtered mapping had no
  `"ready"` check behind it at all. Both are gone; a route-table test pins one mapping.

  StoreHub's `/health/ready` works the same way and is **load-bearing for the installer**:
  `HealthProbe.cs` polls it to decide whether an upgrade (or a post-rollback restart) came back, and
  the till's `IsHealthyAsync` polls it at runtime.

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

**Dev databases now migrate (2026-10).** CloudApi and StoreHub used to provision their Aspire dev
databases with `EnsureCreatedAsync()`, which never adds a table or column to an existing database, so a
dev volume went stale after every schema change (missing `OpenIddict*` tables, `/oauth/token` failing
with a 500; StoreHub crashing while seeding). Development now applies the EF migrations on start, on
fresh database names (`cloud`, `storehub-<store>`). An old `cloud-db` or `storehub-db` database left in
the `indypos-postgres-data` volume is no longer used and can be dropped.

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
