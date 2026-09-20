# Cloud deploy — Epic I tasks I1–I3 (design)

Status: Draft for review (Pond)
Date: 2026-09-19
Author: Pond (with Claude)
Supersedes the "TLS terminator seam" TODO in `deploy/cloud/compose.prod.yaml:37-48`.

Rev 2 (2026-09-19): folded in external review — Cloudflare token needs `Zone:Read` + `DNS:Edit`;
registry needs explicit tag retention before GC (GC alone will not reap still-tagged manifests);
DNS-01 rationale reworded; Caddy version pinning; IP-range refresh + `down -v` operational notes;
Cloudflare-removal portability steps; image tags carry a git sha. Caddy container health check
noted as a deferred optional.

## Goal

Stand up `IndyPOS.CloudApi` in production on DigitalOcean so store tills can reach it over
HTTPS at a stable address, and produce the exact runbook + config to do it repeatably.

- **I1** — provision the DigitalOcean Droplet (Singapore).
- **I2** — provision DO Managed PostgreSQL.
- **I3** — deploy CloudApi behind Cloudflare + Caddy, serving `https://api.indypos.com`.

**Division of labour:** Claude writes the config and the runbook. Pond executes the
provisioning against his DigitalOcean and Cloudflare accounts (creating a Droplet, a managed
database, a registry, and registering a domain cannot be done from the dev box). Claude
validates the repo changes locally before Pond deploys.

## Context

Task **I0** is done and merged: the CloudApi Docker image, the two compose stacks, `.env.example`,
the runbook `docs/operations/cloud-deployment.md`, an initial EF migration (13 tables including
OpenIddict's), a `migrate` verb, and a design-time factory all exist on `development`. I0-E (the
client registry) is closed — store-to-cloud auth works end to end against a local stack. So the
deployment artefact is already proven; I1–I3 put it on paid infrastructure.

Of the "three owed decisions" before I3: TLS-at-the-token-endpoint (PR #89) and the OpenApi
advisories (PR #90/#91) are resolved. The remaining one is a procedural note about stacked-PR merge
order and does not affect this work.

The sizing, region, firewall ports, cost, and rejected alternatives are already fixed in
`docs/architecture/IndyPOS_Production_Infrastructure_Guide.md` — this spec does not revisit them. It
only fills in the edge (Cloudflare + Caddy), the image-delivery path (DO Container Registry), and the
step-by-step runbook.

## Locked decisions

- **Provider:** DigitalOcean, region `sgp1` (Singapore). Droplet Basic Premium AMD, 1 vCPU / 2 GB /
  50 GB. Managed PostgreSQL smallest production tier. Private VPC. ≈ $27/mo.
- **Edge:** Cloudflare in front of DO — DNS, proxy, edge TLS, DDoS. Domain `indypos.com` registered
  via **Cloudflare Registrar** (no GoDaddy). This epic wires `api.indypos.com` only; `www`, `admin`,
  `dev-api` are accommodated by the same zone later.
- **Origin TLS:** **Caddy** on the Droplet with a **publicly-trusted Let's Encrypt** certificate
  (not a Cloudflare Origin CA cert) — chosen for portability: the origin stays reachable and valid if
  Cloudflare is ever removed. Cloudflare SSL mode **Full (strict)**.
- **ACME challenge:** **DNS-01 via the Cloudflare DNS plugin.** DNS-01 is used deliberately so
  certificate issuance and renewal never depend on an origin HTTP challenge endpoint: port 80 stays
  closed, 443 stays restricted to Cloudflare, renewal runs unattended, and the Cloudflare proxy can
  stay enabled throughout. (TLS-ALPN-01 additionally cannot work behind the proxy at all — Cloudflare
  terminates the TLS handshake, so the challenge never reaches the origin. HTTP-01 could be coaxed to
  work if port 80 were open and reachable, but we keep it closed by design.) DNS-01 also opens the
  door to a wildcard cert later. It needs the custom Caddy build (§3) and a scoped Cloudflare API
  token.
- **Image delivery:** DO Container Registry, **Starter (free)** tier, one repo. Build the CloudApi
  image on the dev box, push an **immutable tag** (date + short git sha, for traceability to source),
  pull that exact tag on the Droplet. Because GC only reaps *unreferenced* manifests, staying under
  the 500 MiB cap requires **explicitly deleting old release tags** down to a small rollback window,
  *then* running GC — see the runbook. The Caddy image is built on the box (a small Go build) because
  the free tier holds only one repo.
- **Database:** DO Managed PostgreSQL from day one (not self-hosted on the box — the infra guide
  rejected that).
- **Cloudflare Tunnel: rejected.** It hides the origin IP and needs no cert, but makes Cloudflare
  load-bearing for reachability. Keeping Cloudflare replaceable is worth more here.

## Target architecture

```text
POS tills
    |
    |  HTTPS  (Cloudflare Universal cert)
    v
Cloudflare edge  (DNS + proxy + DDoS)
    |
    |  HTTPS  (Full strict; origin Let's Encrypt cert)
    v
DigitalOcean Droplet (sgp1, in VPC)
    |
    +-- Caddy            :443 published  (only Cloudflare IP ranges allowed)
    |       |   reverse_proxy, sets X-Forwarded-Proto / -For
    |       |   HTTP over the private Docker network
    |       v
    +-- cloud-api        :8080  (NO published host port)
    +-- cloud-api-migrate        one-shot, runs then exits
            |
            |  private VPC :5432
            v
    DO Managed PostgreSQL  (trusted source = the Droplet only)

Image source:
    dev box  --build+push-->  DO Container Registry  --pull-->  Droplet (cloud-api only)
```

Caddy is the only container that publishes a host port. `cloud-api` is reachable solely on the
private Docker network, so port 8080 can never be hit from the internet. `OpenIddict__TlsTerminatedUpstream=true`
stays valid and honest: Caddy terminates TLS on the box and forwards plain HTTP to the container.

**Design principle:** the Droplet holds only stateless-ish app/web workloads — Caddy, CloudApi, and
later the marketing/admin websites (`Website 1`, `Website 2`) fronted by the same Caddy via extra
site blocks. All durable state lives in DO Managed PostgreSQL, reached over the private VPC. Nothing
on the Droplet is a source of truth, so the box stays disposable and re-homeable.

## Repo / config changes

No application code changes — deploy-only. Files under `deploy/cloud/` and one script.

### 1. `deploy/cloud/compose.prod.yaml` (edit)

- **`x-cloudapi` anchor:** remove the `build:` block; change `image:` to `${CLOUDAPI_IMAGE}`. Do
  **not** keep `env_file: .env` — it would inject `CLOUDFLARE_API_TOKEN` (Caddy-only) into the app
  containers; map only the application variables explicitly instead. Keep the
  `OpenIddict__TlsTerminatedUpstream: "true"` environment entry and the log caps.
- **`cloud-api` service:** delete the "TLS TERMINATOR SEAM" comment and the
  `ports: ["127.0.0.1:8080:8080"]` mapping. Attach it to an internal `appnet` network only. Keep the
  `depends_on` gate on `cloud-api-migrate`.
- **`cloud-api-migrate`:** unchanged except it also joins `appnet` (it needs DB egress, which any
  network provides).
- **New `caddy` service:**
  - `build: { context: ., dockerfile: Caddy.Dockerfile }` (built on the box; see §3).
  - `ports: ["443:443"]` (no port 80 — DNS-01 needs no inbound challenge port).
  - `environment:` only `CLOUDAPI_DOMAIN` and `CLOUDFLARE_API_TOKEN` (substituted from `.env`), so
    Caddy's process gets just the two values it needs, not the API's secrets.
  - `volumes:` a named `caddy_data` volume for `/data` (**must persist** cert + ACME account across
    restarts, or we risk Let's Encrypt rate limits) and `caddy_config` for `/config`; mount the
    `Caddyfile` read-only.
  - `depends_on: cloud-api`, `restart: unless-stopped`, on `appnet`.
- **Networks:** define an internal `appnet`. **Volumes:** define `caddy_data`, `caddy_config`.

### 2. `deploy/cloud/Caddyfile` (new)

```caddyfile
{$CLOUDAPI_DOMAIN} {
    reverse_proxy cloud-api:8080
    tls {
        dns cloudflare {$CLOUDFLARE_API_TOKEN}
    }
}
```

Caddy substitutes the `{$VAR}` values from the environment at load. Caddy sets
`X-Forwarded-Proto`/`X-Forwarded-For` on the proxied request by default.

### 3. `deploy/cloud/Caddy.Dockerfile` (new)

```dockerfile
# Pin both the Caddy release and the plugin version for a reproducible rebuild — see note below.
FROM caddy:<pinned>-builder AS builder
RUN xcaddy build v<pinned> --with github.com/caddy-dns/cloudflare@<pinned>

FROM caddy:<pinned>
COPY --from=builder /usr/bin/caddy /usr/bin/caddy
```

The stock `caddy` image does not bundle the Cloudflare DNS plugin, so we build a thin custom image.
It is built on the Droplet (a fast Go build), not pushed to the registry, because the free registry
tier holds only one repository and that is reserved for the CloudApi image.

**Pin the versions** (review item): `caddy:2-builder` / `caddy:2` and an unpinned plugin float, so a
future rebuild on the box could produce a different binary with no repo change — worse because the
build happens on the production Droplet. Pin the Caddy release (minor/patch) and the plugin version.
The exact current-stable version strings are set at implementation time (verified by the build
succeeding), not guessed here.

### 4. `deploy/cloud/.env.example` (edit — add three keys)

```dotenv
# The CloudApi image in DO Container Registry, including tag. push-image.ps1 prints the exact value.
CLOUDAPI_IMAGE=registry.digitalocean.com/<registry-name>/indypos-cloudapi:<tag>

# The public hostname Caddy serves and requests a certificate for.
CLOUDAPI_DOMAIN=api.indypos.com

# Cloudflare API token scoped to the indypos.com zone ONLY. Required permissions:
#   Zone -> Zone -> Read   (the plugin lists zones to resolve the zone id for _acme-challenge)
#   Zone -> DNS  -> Edit   (write the challenge TXT record)
# Used by Caddy for DNS-01 ACME issuance and renewal. Left blank on purpose (public repo); fill it
# in .env on the box.
CLOUDFLARE_API_TOKEN=
```

The existing `LocalToken__SecretKey` and `INDYPOS_RSA_SIGNING_KEY` entries are unchanged. The
connection string moves to a hyphen-free `CLOUD_DB_CONNECTION` key (Compose reads `${NAME-x}` as a
default expression, so `${ConnectionStrings__cloud-db}` cannot interpolate); `compose.prod.yaml` maps
it into the container as `ConnectionStrings__cloud-db`.

### 5. `scripts/cloud/push-image.ps1` (new)

A small Windows helper: build `src/IndyPOS.CloudApi/Dockerfile` from the repo root, tag it
`registry.digitalocean.com/<registry>/indypos-cloudapi:<tag>` (and `:latest`), and `docker push` both.
Parameters: `-Registry`, `-Tag` (default `yyyyMMddTHHmmssZ-<short git sha>` — the UTC timestamp keeps a
rebuild of the same commit from overwriting a tag), and `-SkipPush` (local build+tag only). It refuses
to push from a dirty working tree (the image would not match its tagged commit), checks `doctl` is
authenticated before pushing, and prints the full `CLOUDAPI_IMAGE` line to paste into `.env`.

## Configuration contract (`.env` on the Droplet)

| Key | Source | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | fixed `Production` | already in the template |
| `LocalToken__SecretKey` | `openssl rand -base64 64` | host refuses to start on blank/default outside Development |
| `INDYPOS_RSA_SIGNING_KEY` | `scripts/generate-rsa-key.ps1` | blank ⇒ ephemeral keys ⇒ every restart invalidates all tokens |
| `CLOUD_DB_CONNECTION` | I2 managed-PG **private** connection string | mapped into the container as `ConnectionStrings__cloud-db` (matches `Program.cs:34`) |
| `CLOUDAPI_IMAGE` | `push-image.ps1` output | registry ref + tag |
| `CLOUDAPI_DOMAIN` | fixed `api.indypos.com` | Caddy site + cert subject |
| `CLOUDFLARE_API_TOKEN` | Cloudflare dashboard | `Zone:Read` + `DNS:Edit`, `indypos.com` zone only |

`.env` is gitignored and lives only on the box. This repo is public — no real secret is ever
committed.

## Runbook (rewrite of `docs/operations/cloud-deployment.md`)

Ordered, copy-pasteable. Prerequisites on the dev box: Docker, `doctl` (`doctl auth init`).

1. **Domain (Cloudflare):** register `indypos.com` via Cloudflare Registrar; the zone lands on
   Cloudflare automatically. Create the scoped API token — permissions `Zone:Read` + `DNS:Edit`,
   resources limited to the `indypos.com` zone.
2. **I1 — Droplet:** create the VPC in `sgp1` → create the Droplet to the spec below (Docker-from-
   Marketplace image, in the VPC, SSH key) → cloud firewall: allow **22** from the admin IP, allow
   **443** from the published **Cloudflare IPv4 + IPv6 ranges**, deny everything else. (No inbound 80.)

   | Component | Recommendation |
   |---|---|
   | Droplet type | Basic — Premium AMD |
   | CPU | 1 vCPU |
   | Memory | 2 GB RAM |
   | Disk | 50 GB SSD |
   | Runtime | Docker (DO Docker Marketplace image) |
   | Region | `sgp1` (Singapore) |
   | Network | inside the private VPC (same as managed PG) |
   | Backups | Enabled |

   Source: `docs/architecture/IndyPOS_Production_Infrastructure_Guide.md` (lines 57–69) and
   `PLAN.md:572`. Budgeted ~$12/mo; the Premium AMD tier typically runs a dollar or two above the base
   2 GB Droplet — confirm the exact figure at checkout. 2 GB is comfortable because the heavy .NET SDK
   build happens on the dev box (only the finished CloudApi image is pulled); the box builds nothing
   larger than the thin Caddy image.
3. **I2 — Managed PostgreSQL:** smallest prod tier, `sgp1`, same VPC → restrict trusted sources to the
   Droplet → create the database → copy the **private** connection string.
4. **Registry:** create the Starter registry → `doctl registry login` on the dev box and on the
   Droplet → run `push-image.ps1`.
5. **Cloudflare DNS:** add an **A record** `api` → the Droplet's public IP, **proxied** (orange
   cloud). Set SSL/TLS mode to **Full (strict)**.
6. **Secrets → `.env` on the box:** fill all seven keys from the contract above.
7. **I3 — Deploy:** copy `deploy/cloud/` to the box → `docker compose -f compose.prod.yaml pull` (pulls
   cloud-api) and `... build caddy` (builds the Caddy image once) → `... up -d` → watch
   `cloud-api-migrate` exit 0. **Re-run `up -d` once** if the managed DB was briefly unreachable at
   start — the one-shot has `restart: "no"` and is not gated on DB readiness.
8. **Retention + prune:** after the deploy verifies healthy, delete obsolete release tags/manifests
   so only `latest`, the current tag, and the previous 2–3 remain, **then** run garbage collection
   (`doctl registry garbage-collection start`). GC alone does **not** remove still-tagged release
   manifests, so without the delete step the timestamp tags accumulate until the 500 MiB Starter cap
   is hit.

## Firewall

```text
22/tcp    allow admin IP(s) only
443/tcp   allow Cloudflare IPv4 ranges
          allow Cloudflare IPv6 ranges
          deny  everything else
(no 80, no 8080, no 5432 inbound from the internet)
```

Locking 443 to Cloudflare's ranges stops anyone bypassing the proxy to hit the origin directly.
SSH stays public-to-admin-IP for now; Tailscale/WireGuard to remove public SSH is a future hardening
step, out of scope here.

**Operational maintenance (recurring):** Cloudflare's published IPv4/IPv6 ranges can change, so the
443 allowlist is a standing checklist item, not a one-off — periodically compare the DO cloud
firewall against Cloudflare's current published ranges and update it if they drift. Automating this
sync stays out of scope for I1–I3.

## Acceptance criteria

I1–I3 are done when all hold:

1. `docker compose -f compose.prod.yaml ps` shows `cloud-api-migrate` exited 0, `cloud-api` and
   `caddy` up.
2. The managed database has the 13 expected tables.
3. Caddy's log shows a certificate obtained for `api.indypos.com` via the DNS-01 challenge.
4. From an admin machine: `curl https://api.indypos.com/health/live` returns **200** (through
   Cloudflare → Caddy → cloud-api).
5. A store can be registered and `POST https://api.indypos.com/oauth/token` returns **200** with a
   persisted access token.
6. A direct request to the Droplet's IP on 443 from a non-Cloudflare address is refused by the
   firewall; port 8080 is not reachable from anywhere off the box.

## Testing

Provisioning steps are verified by the acceptance checks above (there is nothing to unit-test in
"create a Droplet"). Before Pond deploys, Claude validates the repo changes locally:

- `docker compose -f deploy/cloud/compose.prod.yaml config` parses clean with a dummy `.env`.
- A local dry-run: build the CloudApi image, stand up the prod compose against a throwaway local
  Postgres with a stub `CLOUDAPI_IMAGE`, and confirm migrate → cloud-api come up healthy. (Caddy's
  real cert path can't be exercised locally without the domain; its config is validated with
  `caddy validate`.)

## Portability — removing Cloudflare later

The only address a till knows is `https://api.indypos.com`, and the origin holds a publicly-trusted
cert (not a Cloudflare-only Origin CA cert), so Cloudflare can be dropped without touching tills or
app config. The migration would be:

1. Change the DO firewall to allow the intended direct clients on 443 (instead of Cloudflare ranges).
2. Move authoritative DNS elsewhere, or disable the Cloudflare proxy (grey-cloud), so `api.indypos.com`
   resolves straight to the origin.
3. Keep Caddy and its existing public TLS config — the cert stays valid; only the ACME challenge
   plumbing changes (DNS-01 still works wherever DNS is hosted, or switch Caddy to HTTP-01 once 80 is
   reachable).
4. CloudApi is unchanged.

No application-level Cloudflare dependency is introduced.

## Out of scope

I4 (SyncWorker against the real CloudApi) and I5–I8; the `www` / `admin` / `dev-api` subdomains and
the `Website 1` / `Website 2` containers (they slot behind the same Caddy via extra site blocks
later); a Caddy container-level health check (external `/health/live` already proves the full request
path, so this is a deferred nicety); Tailscale/WireGuard SSH hardening; Redis / background-worker
containers; automating the Cloudflare-IP-range firewall refresh. Each is a later, additive step.

## Risks and caveats

- **Migrate not gated on DB readiness** — if the managed cluster is momentarily unreachable at `up`
  time, the one-shot exits non-zero and `cloud-api` never starts; re-run `up -d` once. (Known I0
  behaviour; a future `depends_on`-style health gate could remove the manual step.)
- **Caddy cert persistence** — the `caddy_data` volume must survive restarts, or repeated re-issuance
  can hit Let's Encrypt rate limits. Named volume, not an anonymous one. **Never `docker compose down
  -v` in production** — that deletes named volumes, wiping Caddy's ACME account and certs; plain
  `down` leaves them intact.
- **Cloudflare IP ranges drift** — the 443 firewall rule needs periodic refreshing against
  Cloudflare's published list.
- **API token blast radius** — the Cloudflare token is scoped to DNS:Edit on the one zone and lives
  only in the box's gitignored `.env`.
- **DNS propagation** — the A record and the Registrar's nameserver activation can take time before
  the first deploy verifies; expect a wait on step 5.
```
