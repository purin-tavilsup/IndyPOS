# Cloud deploy I1–I3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the CloudApi prod compose stack into a registry-pulled, Caddy-fronted deployment so `IndyPOS.CloudApi` can run on a DigitalOcean Droplet behind Cloudflare at `https://api.indypos.com`, and rewrite the runbook that provisions it.

**Architecture:** The prod stack stops building the image on the box and pulls it from DO Container Registry instead. A new Caddy service becomes the only container that publishes a host port (443); it terminates TLS with a Let's Encrypt certificate obtained via the Cloudflare DNS-01 challenge, and reverse-proxies plain HTTP to `cloud-api:8080` on a private Docker network. The database is DO Managed PostgreSQL, external to the box. This plan changes **no application code** — only `deploy/cloud/` config, one helper script, and one operations doc.

**Tech Stack:** Docker Compose, Caddy 2 (custom build with `caddy-dns/cloudflare`), DigitalOcean (Droplet, Managed PostgreSQL, Container Registry), Cloudflare (DNS, proxy, edge TLS), PowerShell 7.

**Spec:** `docs/superpowers/specs/2026-09-19-cloud-deploy-i1-i3-design.md`

## Global Constraints

- **Public repo — never commit a real secret.** `deploy/cloud/.env` is gitignored; only `.env.example` (no real values) is tracked. Any scratch `.env` created for a local check must be deleted after.
- **No application code changes.** Only `deploy/cloud/*`, `scripts/cloud/*`, and `docs/operations/cloud-deployment.md`.
- **The image is built on the dev box, not the Droplet** — only the finished CloudApi image is pulled on the box. The one exception is the thin Caddy image, built on the box because the free registry tier holds a single repo (reserved for CloudApi).
- **`OpenIddict__TlsTerminatedUpstream: "true"` stays** in `compose.prod.yaml` — TLS terminates at Caddy, so this remains valid.
- **ACME uses DNS-01 only.** Port 80 stays closed and 443 is Cloudflare-only, so issuance/renewal must not require an origin HTTP challenge endpoint or bypassing the proxy. DNS-01 works unattended behind the proxy (TLS-ALPN-01 cannot work behind it at all; HTTP-01 would need port 80 open, which we refuse).
- **Region `sgp1`.** Managed PostgreSQL from day one (no DB on the box).

---

### Task 1: Registry image delivery

Stop building the CloudApi image on the Droplet; build it on the dev box, push it to DO Container Registry, and have the prod stack pull it via `${CLOUDAPI_IMAGE}`.

**Files:**
- Create: `scripts/cloud/push-image.ps1`
- Modify: `deploy/cloud/compose.prod.yaml:6-10` (the `x-cloudapi` anchor)
- Modify: `deploy/cloud/.env.example` (add `CLOUDAPI_IMAGE`)

**Interfaces:**
- Produces: `CLOUDAPI_IMAGE` env var (registry ref + tag), consumed by both `cloud-api` and `cloud-api-migrate` via the shared anchor. Task 2 consumes the same anchor.

- [ ] **Step 1: Create the push script**

`scripts/cloud/push-image.ps1`:

```powershell
#requires -Version 7
<#
.SYNOPSIS
Build the CloudApi image on this dev box and push it to DO Container Registry.
Run `doctl registry login` first. Prints the CLOUDAPI_IMAGE line to paste into .env.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Registry,                              # DO registry name
    [string] $Tag,                                                          # default: yyyyMMdd-<short sha>
    [switch] $SkipPush                                                      # build+tag only (local check)
)
$ErrorActionPreference = 'Stop'

$repoRoot  = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $Tag) {
    # Immutable, traceable to source: date + short commit sha.
    $sha = (git -C $repoRoot rev-parse --short HEAD).Trim()
    $Tag = "{0}-{1}" -f (Get-Date -AsUTC -Format 'yyyyMMdd'), $sha
}
$imageBase = "registry.digitalocean.com/$Registry/indypos-cloudapi"
$ref       = "${imageBase}:$Tag"

docker build -f "$repoRoot/src/IndyPOS.CloudApi/Dockerfile" -t $ref -t "${imageBase}:latest" $repoRoot
if ($LASTEXITCODE -ne 0) { throw "docker build failed (exit $LASTEXITCODE)" }
if (-not $SkipPush) {
    docker push $ref
    if ($LASTEXITCODE -ne 0) { throw "docker push failed (exit $LASTEXITCODE)" }
    docker push "${imageBase}:latest"
    if ($LASTEXITCODE -ne 0) { throw "docker push failed (exit $LASTEXITCODE)" }
}

Write-Host ""
Write-Host "CLOUDAPI_IMAGE=$ref"
```

- [ ] **Step 2: Run the script (build + tag only) to verify it works**

Run: `pwsh -File scripts/cloud/push-image.ps1 -Registry example -Tag localtest -SkipPush`
Expected: Docker builds the CloudApi image, tags it `registry.digitalocean.com/example/indypos-cloudapi:localtest` and `:latest`, and the last line prints `CLOUDAPI_IMAGE=registry.digitalocean.com/example/indypos-cloudapi:localtest`. (Before Step 1 this command fails with "file not found" — that is the RED state.)

- [ ] **Step 3: Point the compose anchor at the registry image**

In `deploy/cloud/compose.prod.yaml`, replace the anchor (current lines 6-10):

```yaml
x-cloudapi: &cloudapi
  build:
    context: ../..
    dockerfile: src/IndyPOS.CloudApi/Dockerfile
  image: indypos-cloudapi:prod
```

with:

```yaml
x-cloudapi: &cloudapi
  image: ${CLOUDAPI_IMAGE}
```

Also update the header comment block at the top of the file (lines 1-3): the box no longer builds the API, so change `docker compose -f compose.prod.yaml up -d --build` to `docker compose -f compose.prod.yaml pull` then `... up -d`.

- [ ] **Step 4: Document the key in `.env.example`**

Append to `deploy/cloud/.env.example`:

```dotenv
# The CloudApi image in DO Container Registry, including tag. scripts/cloud/push-image.ps1 prints the
# exact value after a push. Both cloud-api and the migrate one-shot pull this image.
CLOUDAPI_IMAGE=registry.digitalocean.com/<registry-name>/indypos-cloudapi:<tag>
```

- [ ] **Step 5: Verify the stack still parses with the image variable**

```bash
cd deploy/cloud
cp .env.example .env                 # scratch; .env is gitignored
# fill dummy values so `config` resolves:
#   CLOUDAPI_IMAGE=registry.digitalocean.com/example/indypos-cloudapi:localtest
#   LocalToken__SecretKey=dummy  INDYPOS_RSA_SIGNING_KEY=dummy  CLOUD_DB_CONNECTION=dummy
docker compose -f compose.prod.yaml config
rm .env                              # MUST delete the scratch .env
```

Expected: prints resolved config with `image: registry.digitalocean.com/example/indypos-cloudapi:localtest` on both `cloud-api` and `cloud-api-migrate`, and **no `build:` key** anywhere.

- [ ] **Step 6: Commit**

```bash
git add scripts/cloud/push-image.ps1 deploy/cloud/compose.prod.yaml deploy/cloud/.env.example
git commit -m "feat(cloud): pull CloudApi image from DO registry in prod stack"
```

---

### Task 2: Caddy TLS terminator

Add a Caddy reverse proxy that terminates TLS with a Let's Encrypt cert (DNS-01 via Cloudflare) and proxies to an internal-only `cloud-api`. Remove the loopback port mapping.

**Files:**
- Create: `deploy/cloud/Caddy.Dockerfile`
- Create: `deploy/cloud/Caddyfile`
- Modify: `deploy/cloud/compose.prod.yaml` (add `caddy` service, `appnet` network, volumes; remove `cloud-api` ports + seam comment)
- Modify: `deploy/cloud/.env.example` (add `CLOUDAPI_DOMAIN`, `CLOUDFLARE_API_TOKEN`)

**Interfaces:**
- Consumes: `${CLOUDAPI_IMAGE}` from Task 1; `CLOUDAPI_DOMAIN` and `CLOUDFLARE_API_TOKEN` from the environment.
- Produces: the `caddy` service (publishes 443), the internal `appnet` network, and named volumes `caddy_data` / `caddy_config`.

- [ ] **Step 1: Create the custom Caddy build**

First look up the current stable versions and pin them (the build runs on the production Droplet, so
a floating tag could yield a different binary with no repo change):
- Caddy release: latest stable `2.x.y` from https://hub.docker.com/_/caddy/tags
- Plugin: latest release tag of https://github.com/caddy-dns/cloudflare

`deploy/cloud/Caddy.Dockerfile` (replace each `<...>` with the versions you looked up):

```dockerfile
# The stock caddy image does not bundle the Cloudflare DNS plugin needed for the DNS-01 challenge.
# Versions are pinned for a reproducible rebuild.
FROM caddy:<x.y.z>-builder AS builder
RUN xcaddy build v<x.y.z> --with github.com/caddy-dns/cloudflare@<vA.B.C>

FROM caddy:<x.y.z>
COPY --from=builder /usr/bin/caddy /usr/bin/caddy
```

- [ ] **Step 2: Create the Caddyfile**

`deploy/cloud/Caddyfile`:

```caddyfile
{$CLOUDAPI_DOMAIN} {
	reverse_proxy cloud-api:8080
	tls {
		dns cloudflare {$CLOUDFLARE_API_TOKEN}
	}
}
```

- [ ] **Step 3: Build the Caddy image**

Run: `cd deploy/cloud && docker build -f Caddy.Dockerfile -t indypos-caddy:prod .`
Expected: builds successfully. (Before Step 1 this fails — RED.)

- [ ] **Step 4: Validate the Caddyfile and confirm the plugin is compiled in**

```bash
cd deploy/cloud
docker run --rm \
  -e CLOUDAPI_DOMAIN=api.example.com -e CLOUDFLARE_API_TOKEN=dummy \
  -v "$(pwd)/Caddyfile:/etc/caddy/Caddyfile:ro" \
  indypos-caddy:prod caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile
```

Expected: `Valid configuration`. If the Cloudflare plugin were missing, the `dns cloudflare` line would fail with an unknown-module error — so this step doubles as proof the custom build worked.

- [ ] **Step 5: Rewrite `compose.prod.yaml` to the final form**

Replace the entire file with:

```yaml
# Droplet deployment unit. On the box:
#   docker compose -f compose.prod.yaml pull          # cloud-api from the registry
#   docker compose -f compose.prod.yaml build caddy   # thin custom Caddy image, once
#   docker compose -f compose.prod.yaml up -d
# Requires a .env alongside this file (see .env.example). Never commit that file.
name: indypos-cloud

x-cloudapi: &cloudapi
  image: ${CLOUDAPI_IMAGE}
  # Map only the application variables -- NOT `env_file: .env`, which would also inject
  # CLOUDFLARE_API_TOKEN (Caddy-only) into the app containers.
  environment:
    ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
    LocalToken__SecretKey: ${LocalToken__SecretKey}
    INDYPOS_RSA_SIGNING_KEY: ${INDYPOS_RSA_SIGNING_KEY}
    # CLOUD_DB_CONNECTION is hyphen-free so it interpolates; the container still gets the
    # correctly-hyphenated ConnectionStrings__cloud-db key (left-hand side is literal YAML).
    ConnectionStrings__cloud-db: ${CLOUD_DB_CONNECTION}
    # OpenIddict rejects token requests that did not arrive over HTTPS (ID2083). TLS terminates at
    # Caddy in front of this container, so the requirement is relaxed here. Defaults to false in
    # code -- an unconfigured host stays strict and refuses to issue tokens over cleartext.
    OpenIddict__TlsTerminatedUpstream: "true"
  logging:
    driver: json-file
    options:
      # The Droplet has a 50 GB disk. An uncapped container log is a slow outage.
      max-size: "10m"
      max-file: "5"

services:
  # One-shot. Must exit 0 before the API is allowed to start.
  cloud-api-migrate:
    <<: *cloudapi
    container_name: indypos-cloud-api-migrate
    command: ["migrate"]
    restart: "no"
    networks: [appnet]

  cloud-api:
    <<: *cloudapi
    container_name: indypos-cloud-api
    restart: unless-stopped
    # No published host port: reachable only by Caddy over the private appnet network below.
    networks: [appnet]
    depends_on:
      cloud-api-migrate:
        condition: service_completed_successfully

  # TLS terminator + reverse proxy. The only container that publishes a host port.
  # Obtains a publicly-trusted Let's Encrypt cert via the Cloudflare DNS-01 challenge.
  caddy:
    build:
      context: .
      dockerfile: Caddy.Dockerfile
    image: indypos-caddy:prod
    container_name: indypos-caddy
    restart: unless-stopped
    ports:
      - "443:443"
    environment:
      CLOUDAPI_DOMAIN: ${CLOUDAPI_DOMAIN}
      CLOUDFLARE_API_TOKEN: ${CLOUDFLARE_API_TOKEN}
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      # caddy_data holds the ACME account + issued certs. It MUST persist across restarts,
      # or repeated re-issuance can hit Let's Encrypt rate limits.
      - caddy_data:/data
      - caddy_config:/config
    depends_on:
      - cloud-api
    networks: [appnet]

networks:
  appnet:
    driver: bridge

volumes:
  caddy_data:
  caddy_config:
```

- [ ] **Step 6: Document the two new keys in `.env.example`**

Append to `deploy/cloud/.env.example`:

```dotenv
# The public hostname Caddy serves and requests a certificate for.
CLOUDAPI_DOMAIN=api.indypos.com

# Cloudflare API token scoped to the indypos.com zone ONLY. Required permissions:
#   Zone -> Zone -> Read   (the plugin lists zones to resolve the zone id for _acme-challenge)
#   Zone -> DNS  -> Edit   (write the challenge TXT record)
# Caddy uses it for DNS-01 ACME issuance and renewal. Left blank on purpose (public repo); fill it
# in .env on the box.
CLOUDFLARE_API_TOKEN=
```

- [ ] **Step 7: Verify the full stack parses**

```bash
cd deploy/cloud
cp .env.example .env                 # scratch; fill CLOUDAPI_IMAGE, CLOUDAPI_DOMAIN, and dummy secrets
docker compose -f compose.prod.yaml config
rm .env                              # MUST delete the scratch .env
```

Expected: the `caddy` service publishes `443:443`; `cloud-api` has **no `ports:`**; the `appnet` network and `caddy_data`/`caddy_config` volumes are present.

- [ ] **Step 8 (recommended): Local end-to-end dry-run**

Prove migrate → cloud-api come up against a throwaway Postgres (the real cert path needs the domain, so Caddy is not started here):

```bash
cd deploy/cloud
docker network create indypos-cloud_appnet 2>/dev/null || true
docker run -d --name dryrun-pg --network indypos-cloud_appnet \
  -e POSTGRES_PASSWORD=pw -e POSTGRES_DB=cloud postgres:16-alpine
# scratch .env: CLOUDAPI_IMAGE=<the :localtest tag from Task 1>, real dummy secrets, and
#   CLOUD_DB_CONNECTION=Host=dryrun-pg;Database=cloud;Username=postgres;Password=pw
docker compose -f compose.prod.yaml up -d cloud-api-migrate cloud-api
docker compose -f compose.prod.yaml ps        # migrate Exited(0), cloud-api Up
docker compose -f compose.prod.yaml down
docker rm -f dryrun-pg
rm .env
```

Expected: `cloud-api-migrate` exits 0, `cloud-api` reports healthy, and `curl localhost:8080` from the host is refused (no published port).

- [ ] **Step 9: Commit**

```bash
git add deploy/cloud/Caddy.Dockerfile deploy/cloud/Caddyfile deploy/cloud/compose.prod.yaml deploy/cloud/.env.example
git commit -m "feat(cloud): add Caddy TLS terminator with Cloudflare DNS-01"
```

---

### Task 3: Rewrite the deployment runbook

Replace the manual "Run it on the Droplet" steps with the full provisioning + deploy runbook, and update the TLS section to describe Caddy + Cloudflare. Preserve the Health, Schema, Operational-notes, and Follow-ups sections unchanged.

**Files:**
- Modify: `docs/operations/cloud-deployment.md`

- [ ] **Step 1: Replace the "Run it on the Droplet" section**

Replace the section currently at lines 19-29 (`## Run it on the Droplet` … the migrate note) with:

````markdown
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

Create a DO Container Registry (Starter tier). `doctl registry login` on the dev box and on the
Droplet, then build and push:

```powershell
pwsh -File scripts/cloud/push-image.ps1 -Registry <registry-name>
```

It prints the `CLOUDAPI_IMAGE=...` line for `.env`.

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
````

- [ ] **Step 2: Update the "Run it locally" note (unchanged stack, clarify only)**

Leave `## Run it locally` as-is — the local `compose.yaml` still builds and is unchanged.

- [ ] **Step 3: Update the TLS section opening**

Replace the first paragraph of `## TLS` (lines 66-69, the "Nothing … terminates TLS … binds the API to `127.0.0.1:8080`" text) with:

```markdown
TLS is terminated by the `caddy` service in `compose.prod.yaml`, which obtains a publicly-trusted
Let's Encrypt certificate via the Cloudflare DNS-01 challenge and reverse-proxies plain HTTP to
`cloud-api:8080` over the private `appnet` network. `cloud-api` publishes no host port. Cloudflare
sits in front in Full (strict) mode, so traffic is encrypted till→Cloudflare and Cloudflare→Caddy.
```

Keep the rest of the TLS section (the `OpenIddict__TlsTerminatedUpstream` explanation, the comparison
table, the `UseForwardedHeaders` rationale, and the I0-E paragraph) unchanged — it is still accurate.

- [ ] **Step 4: Add two bullets to the "Operational notes" section**

Append to the existing `## Operational notes` section (keep everything already there):

```markdown
**Never `docker compose down -v` in production.** `-v` deletes the named `caddy_data` volume, wiping
Caddy's ACME account and issued certificate — a plain `docker compose down` leaves named volumes
intact. Only pass `-v` when you intend to discard Caddy's TLS state.

**The 443 firewall allowlist is recurring maintenance.** Cloudflare's published IPv4/IPv6 ranges
change over time. Periodically compare the DO cloud firewall's 443 rule against Cloudflare's current
published ranges and update it if they drift, or direct clients could be blocked / the origin could
become reachable off-Cloudflare. Automating the sync is out of scope for I1–I3.
```

- [ ] **Step 5: Verify no stale instructions remain**

```bash
grep -n "127.0.0.1:8080" docs/operations/cloud-deployment.md   # expect: no matches
grep -n "up -d --build"  docs/operations/cloud-deployment.md   # expect: no matches (prod uses pull + build caddy)
grep -n "no domain is registered" docs/operations/cloud-deployment.md   # expect: no matches
grep -n "CLOUDAPI_DOMAIN\|CLOUDFLARE_API_TOKEN\|push-image" docs/operations/cloud-deployment.md  # expect: present
```

Confirm the "Operational notes" section (stale Aspire volume; migrate-does-not-self-heal) and the
"Follow-ups" section are still present and unchanged.

- [ ] **Step 6: Commit**

```bash
git add docs/operations/cloud-deployment.md
git commit -m "docs(cloud): rewrite deploy runbook for registry + Caddy + Cloudflare"
```

---

## Self-Review

**Spec coverage:**
- DOCR image delivery + `push-image.ps1` → Task 1. ✅
- Caddy service, `Caddyfile`, `Caddy.Dockerfile`, internal network, cert-volume persistence, remove loopback port → Task 2. ✅
- Three new `.env` keys (`CLOUDAPI_IMAGE` in Task 1; `CLOUDAPI_DOMAIN`, `CLOUDFLARE_API_TOKEN` in Task 2). ✅
- DNS-01 via Cloudflare plugin (custom build) → Task 2 Steps 1, 4. ✅
- Runbook (domain → I1 → I2 → registry → DNS → secrets → I3 → retention → verify), Droplet spec table, firewall, config contract, acceptance, operational notes → Task 3. ✅
- `OpenIddict__TlsTerminatedUpstream` retained → Task 2 Step 5 (anchor). ✅
- Provisioning steps (create Droplet / managed PG / registry / A record) are **operator actions in the runbook**, executed by Pond, not code tasks — by design (spec "division of labour").

**Review fold-in (Rev 2, 2026-09-19):** Cloudflare token now `Zone:Read` + `DNS:Edit` (Task 2 Step 6, Task 3 Step 1); registry retention step added before GC (Task 3 I3 section) — GC alone does not reap still-tagged manifests; Caddy versions pinned (Task 2 Step 1); DNS-01 rationale reworded (Global Constraints); image tag carries a git sha (Task 1 Step 1); `down -v` + IP-range operational notes (Task 3 Step 4). Deferred per reviewer: Caddy container health check (external `/health/live` already proves the path).

**Placeholder scan:** `<registry-name>` / `<tag>` are operator-fill config values, not plan TODOs. No "TBD"/"implement later"/vague-error-handling steps.

**Type consistency:** `CLOUDAPI_IMAGE`, `CLOUDAPI_DOMAIN`, `CLOUDFLARE_API_TOKEN`, service names (`cloud-api`, `cloud-api-migrate`, `caddy`), network (`appnet`), volumes (`caddy_data`, `caddy_config`), and image tags (`indypos-caddy:prod`) are spelled identically across Tasks 1–3 and the compose file.
```
