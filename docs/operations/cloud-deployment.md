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
- `/health/ready` — **mapped twice** and effectively broken. `MapDefaultEndpoints()` registers
  `MapHealthChecks("/health/ready", …)` filtered to checks tagged `"ready"`; `Program.cs` separately
  registers `MapGet("/health/ready", …)` that does a real `CanConnectAsync()` against the database.
  Verified against a running container: **no `AmbiguousMatchException`.** ASP.NET Core's routing
  prefers the endpoint carrying explicit `HttpMethodMetadata` (the `MapGet`, GET-only) over the one
  with none (`MapHealthChecks` maps via a generic `Map`, no method constraint), so the `Program.cs`
  handler silently wins every request — confirmed by the response body
  (`{"status":"healthy","database":"connected"}`, not the plain-text `Healthy` that `/health/live`
  returns) and by the server log, which reads `Executing endpoint 'HTTP: GET /health/ready'` rather
  than `'Health checks'`. The `MapHealthChecks` registration behind it is dead code.
  Separately: even if it were reached, it would report a false "ready" — `AddDefaultHealthChecks`
  registers only a `self` check tagged `"live"`; **no check anywhere is tagged `"ready"`**, so the
  predicate matches zero checks and the health-check middleware's convention is to report `Healthy`
  on an empty check set. Do not rely on `/health/ready` for anything until this is resolved as its
  own defect — packaging is not the place to fix it.

## TLS

Nothing in these files terminates TLS, and no domain is registered yet, so `compose.prod.yaml` binds
the API to `127.0.0.1:8080`. See the marked seam in that file for what a terminator must do.

**Known limitation — the token endpoint needs HTTPS, and the container only serves HTTP.**
OpenIddict's token endpoint (and every other OpenIddict endpoint — `/oauth/token`,
`/.well-known/openid-configuration`, `/.well-known/jwks`) rejects plain HTTP with `400` and
`error_uri: https://documentation.openiddict.com/errors/ID2083`, because
`DisableTransportSecurityRequirement()` is never called. TLS is terminated upstream of this
container (see the seam above), so a client talking to the container directly over its published
8080 cannot obtain a token — it must go through the terminator once one exists. This was verified
against the containerized API with a real RSA signing key configured (`INDYPOS_RSA_SIGNING_KEY`),
ruling out the ephemeral-development-certificate fallback as the cause. Do **not** work around this
by calling `DisableTransportSecurityRequirement()`, adding forwarded-headers middleware, or
generating a certificate inside the container — enabling plain-HTTP token issuance is a
security-design decision for the repository owner, not a packaging fix.

## Schema changes

Forward-only, same gate as StoreHub: additive, nullable or defaulted, no renames or drops. See
`docs/operations/upgrade-procedure.md`. Generate migrations with:

```bash
dotnet ef migrations add <Name> --project src/IndyPOS.CloudApi --output-dir Infrastructure/Migrations
```

Never hand-write one.
