# Epic 3 — Store Rollout Plan (v3 xcopy → v4)

> **Status:** Ready to execute (team-reviewed 2026-09-20) · **Authored:** 2026-09-20
> Execution plan for the already-scoped Epic 3 (`PLAN.md:440-509`). The installer, migration CLI, and
> ops docs already exist — this plan adds the missing layer: **sequencing across three stores**, a
> **dev-machine dress rehearsal**, and the **cutover/rollback hardening** from team review.

---

## 1. Scope & assumptions

**In scope**
- A repeatable procedure to move each store from the v3 manual-xcopy app to the v4 installer-based app
  (StoreHub Windows service + PostgreSQL + migrated history).
- A **Phase A rehearsal** on the dev machine that runs all three stores one-by-one against their real
  legacy `Store.db` files, using the **full installer** (closest to the real in-store experience).
- A **Phase B production rollout**, store-by-store, after business hours.

**Out of scope (this plan)**
- **Cloud sync mechanics** — stores still *run* fully standalone (`PLAN.md:22`, ADR-001
  offline-first): a cloud outage never stops a till. But sync is the core of v4 (off-site audit,
  dashboards, the future MCP server for AI agents), so Phase B carries one cloud gate — see below.

**Phase B prerequisites added 2026-09-26**
- **The cash-flow persistence feature must land before Phase B**
  (`docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md` §12): each store's
  v4 DB gets the cash tables from day one, and no store runs v4 on the retired JSON + Drive CSV path.
  Phase A does **not** wait for it.
- **An invoice-void feature must exist before Phase B** (decided 2026-09-26): step 9's test sale
  from each terminal must be reversible, or it stays in the store's money totals. Needs its own
  spec (it also starts the "admin sale corrections" item in the cash spec §11: void reverses stock,
  keeps the original, records who/when, and needs a cloud event).
- **The one-shot history push must be proven at scale in Phase A-2** — including GeneralHardware's
  ~140k invoices — because Phase B pushes every store's history during its single migration run.
- **The cloud is deployed (with the new cash event handlers) before the first store goes live**,
  because `EventProcessor` currently drops unknown event types. Phase A-2 should cover cash sync
  end to end.
- **Installer / migrator code changes** — the machinery is complete and tested; this is procedure and
  sequencing only.

**Rehearsal vs production — the scoping rule that governs this plan**
- **Phase A uses a static test copy** of each store's DB. v3 is **not** selling during the rehearsal,
  so nothing diverges. Therefore the production-only hardening steps — **FREEZE, transactionally-safe
  backup, immutable migration source, and the live-v3 fallback model** — **do not apply to Phase A**.
- **Phase B is a live cutover.** Every one of those steps applies, because a real v3 is selling right
  up to the freeze.

**Key facts that shaped this plan**
- Every store is a **Fresh install**: v4 installs *alongside* the v3 xcopy; the live v3 `Store.db` is
  only ever **read**, never modified.
- The **SQLite→Postgres data migration is a separate manual step** (`MigrationTool` CLI), run *after*
  the installer. The installer itself only runs EF **schema** migration + admin seed.
- The installer is **one-machine-per-run** — no fan-out. Each Store Hub PC is visited individually
  (`upgrade-procedure.md:237`).

## 2. Store inventory & order

Order: **smallest/safest first, hardest last** — prove the pipeline on a trivial dataset before the
large, error-prone store.

| Order | Store (internal) | Real-world name | `--store-type` | Volume | Notes |
|-------|------------------|-----------------|----------------|--------|-------|
| 1 | MimyShop | — | `MimyShop` | 15 invoices | Trivial — proves the pipeline end-to-end |
| 2 | MimyMart | — | `Minimart` | ~97k invoices | Realistic mid volume |
| 3 | GeneralHardware | **Rungrat** | `GeneralHardware` | ~140k invoices, ฿12.4M | Largest; **known permanent verify ✘** |

> **⚠️ `--store-type` mapping trap:** MimyMart's enum value is `Minimart`, **not** `MimyMart`.

## 3. Two hard rules (apply in BOTH phases)

**Rule 1 — MIGRATE runs exactly once, and failure means "reset, don't retry."**
The CLI exits only `0` or `1`, and a non-zero exit does **not** mean "run it again" — it can mean
*"rows were written; do NOT re-run."* The migrator **refuses** a second run against an already-migrated
target (guards against doubling turnover). So:

```
dry-run  ──► clean? ──► migrate (once)
                          │
                 ┌── exit 0 ──► VERIFY
                 │
                 └── exit non-zero ──► STOP · inspect migrate log · do NOT re-run
                                        │
                                   retry approved?
                                        │
                            cleanup-v4.ps1  (reset v4 target to Fresh)
                                        │
                                   confirm Fresh · dry-run again · migrate once
```
Never re-run the migrator against a partially-migrated PostgreSQL target. Only a full **reset to
Fresh** makes a retry safe.

**Rule 2 — GeneralHardware's expected `verify` ✘ is an EXACT match, not "any exit 1."**
Accept **only** this known mismatch (pre-accepted 2026-08-19):

```
Payments (no invoice)  =  2 rows  =  ฿1,000
```
Everything else must be clean. **Any other mismatch stops the rollout** — a new ✘ is a real failure.

## 4. Phase A — Rehearsal (dev machine)

Phase A has two stages: **A-1** proves the offline pipeline for all three stores; **A-2** turns the
Cloud API on and proves the sync path end-to-end. **A-2 is gated — run it when we're ready**, after
A-1 is green.

### 4.1 Phase A-1 — Offline pipeline (no cloud), all three one-by-one

**Goal:** prove the multi-store loop and each store's real data *before* touching a real till.
**Not a live cutover** — no FREEZE, no safe-backup ceremony (source is already a static copy).

**Pre-flight**
- Confirm the installer's silent Postgres 18 step **tolerates a pre-existing PostgreSQL** on the dev
  box (port 5432 reuse) — a pre-existing instance is acceptable per decision.
- Have the three real legacy DBs ready
  (`.planning/indypos-overhaul/sqlite_database/{MimyShop,MimyMart,GeneralHardware}/Store.db`).
- **No Cloud API needed.** Run the migration (root command) and `verify` **without** `--cloud-api` / `--client-id` /
  `--client-secret` — sync only fires when those are set (`Program.cs:163`) and has no effect on the
  exit code (`Program.cs:177`). The whole pipeline runs offline on local StoreHub + local Postgres.

**The loop** — for each store in order (MimyShop → MimyMart → GeneralHardware):

```
INSTALL  ──►  DRY-RUN  ──►  MIGRATE(once)  ──►  VERIFY  ──►  SMOKE
   (against a static copy of the store's Store.db)
record metrics (below)
scripts/cleanup-v4.ps1     # Assert-V4Path guard; resets to Fresh for the next store
```
- **Why cleanup between stores:** one machine holds one v4 store identity at a time. Without the reset,
  store #2's install detects store #1's manifest and classifies as **Upgrade/Unusable**, not Fresh.
- **Failure handling:** Rule 1 applies — a failed migrate means `cleanup-v4.ps1` → Fresh → retry once,
  never a bare re-run.

**Metrics to capture per store** (sets realistic production expectations):
installer duration · migration duration · verify duration · peak memory · source SQLite size ·
resulting PostgreSQL size · migrated invoice count · smoke result · cleanup duration.
Baseline to beat: GeneralHardware ~1.2 min migrate / ~724 MB peak.

**Exit criteria (green light for Phase B):** all three complete install → migrate → verify → smoke,
with GeneralHardware's exact ฿1,000 ✘ (Rule 2) as the **only** accepted verify failure.

### 4.2 Phase A-2 — End-to-end WITH Cloud API (gated: when ready)

**Goal:** prove the store→cloud sync path end-to-end. This is the first real exercise of that path
against a live CloudApi (runtime task I4 is not yet verified E2E), so treat A-2 as *finding* issues,
not just confirming.

**Two distinct cloud mechanisms to test:**
1. **Migration-time push** — run `IndyPOS.MigrationTool` (migration is the **root** command; there
   is no `migrate` sub-command) **with** `--cloud-api <url> --client-id <id> --client-secret <secret>`.
   On success (not dry-run) it calls `SyncToCloudAsync()` (`Program.cs:163-171`) and pushes migrated
   history to the cloud in one shot. **This cannot be a re-run** of an A-1 migration: Rule 1 — the
   migrator refuses a second run against a migrated target. Start from `cleanup-v4.ps1` → Fresh →
   install → migrate **once, with the cloud flags**.
2. **Runtime sync** — StoreHub's `SyncWorker` drains the outbox via `HttpCloudSyncClient` during
   normal operation (sales rung *after* go-live). A fresh install **turns it off**: the installer
   writes `CloudApi.ClientId = ""` (so `AddStoreHubServices` picks `StubCloudSyncClient`) and
   `SyncWorker.Enabled = false` (so the worker exits). To turn it on: set `CloudApi.ClientId` /
   `ClientSecret` and `SyncWorker.Enabled = true` in StoreHub's `appsettings.json`, then restart the
   service. ⚠️ **Blocked by a code bug (found 2026-09-26):** StoreHub never reads `CloudApi.BaseUrl`
   — `ConfigureServices.cs:105-107` takes the base URL only from Aspire's
   `services:cloud-api:*` keys, else `https+http://cloud-api`, which does not resolve on a store
   PC. Fix and RED-test that before A-2.

**Prerequisites**
- A registered store client → `client_id` / `client_secret` (the I0-E client registry; OAuth2
  client-credentials, working E2E since 2026-09-17).
- A CloudApi target. Local container is simplest; the deployed DO instance also works since **cloud
  test data is disposable** (reset/clean afterward).

**Scope:** MimyShop first to prove both paths, then **GeneralHardware's ~140k invoices** for the
history push — Phase B pushes every store's history in its one migration run, so the push must be
proven at the largest store's scale first.

**Verify:** migrated rows land in the cloud Postgres; a post-go-live sale flows outbox → SyncWorker →
cloud (after the runtime-sync config step above); the OAuth2 token exchange succeeds.

**Cleanup:** wipe the cloud test dataset (and any pushed registry artefacts) after the run — cloud data
is disposable by decision.

## 5. Phase B — Production per-store cutover

The canonical live cutover. Steps **0–2 and 8** are the production-only hardening from review.

```
0. PREFLIGHT   Verify the real PC (see checklist §5.1): Windows ver, admin, disk, no 5432/5000
               conflict, firewall, terminal↔StoreHub LAN, DHCP reservation, AV compatibility,
               installer + MigrationTool copied local, store ID/type recorded, v3 restart verified.

1. FREEZE      Close v3 on EVERY terminal. Confirm no cashier is entering sales. Confirm all
               terminals stopped/disconnected. Record last v3 invoice number + timestamp.

2. BACKUP      With v3 fully closed, make a transactionally-safe copy of the final Store.db
               (close every writer first; if v3 cannot be stopped, use a SQLite-safe backup, not a
               bare file copy). Name it Store.db.cutover-YYYYMMDD. Compute + record SHA-256.
               *** This immutable backup is the single source for steps 4–6. ***

3. INSTALL     IndyPOS-Setup.exe --silent --store-id <ID> --store-type <T>

4. DRY-RUN     IndyPOS.MigrationTool -s <backup> -p <pg-conn> -i <ID> --dry-run

5. MIGRATE     IndyPOS.MigrationTool -s <backup> -p <pg-conn> -i <ID>                  --cloud-api <url> --client-id <id> --client-secret <secret>   # once (Rule 1)
               The cloud flags are REQUIRED in production: they push the store's history to the
               cloud in the same run (decided 2026-09-26). Rule 1 means there is no second chance —
               a store migrated without them never gets its history into the cloud.

6. VERIFY      IndyPOS.MigrationTool verify  -s <backup> -p <pg-conn> --store-id <ID>   # Rule 2

7. SMOKE       docs/operations/smoke-test.ps1 -StoreHubUrl <url> -Username <u> -Password <p>
               READ-ONLY: health + auth + GETs. It writes nothing. NEVER run scripts/smoke-test.ps1
               on a real store — it creates products, stock adjustments and sales. v4 has no
               invoice-void route, so a test sale cannot be reversed (see step 9).

8. BASELINE    Take a post-migration PostgreSQL dump (known-good v4 snapshot) before real trading.

9. TERMINALS   Point each POS terminal at StoreHub (appsettings BaseUrl → host:5000, firewall);
               ring a real test sale FROM EACH terminal, then VOID it with the invoice-void
               feature and record both invoice ids in cutover-record.md.

10. SIGN-OFF   Owner/operator confirms readiness.

11. GO-LIVE    Record the FIRST real v4 transaction. *** Point of no return (see §6). ***

12. MONITOR    health-check.ps1 + post-deployment-monitoring.md through the soak.
```

Use the **same immutable backup file** for dry-run, migrate, and verify — never the live `Store.db`,
so a late write cannot slip between capture and migration.

**Soak between stores** (default cadence, adjust freely): MimyShop live on v4 for **~2–3 business
days** (watched via `health-check.ps1` + `post-deployment-monitoring.md`) before starting MimyMart;
then **~1–2 business days** before GeneralHardware. After a clean soak, treat v3 as a **disaster
fallback, not a hot standby** (see §6).

### 5.1 Go/No-Go gates

```
Before migration:                          Before real customer traffic:
[ ] previous store soak complete           [ ] migrate exit 0
[ ] store owner/operator available         [ ] verify clean (GH: only the exact ฿1,000 ✘)
[ ] all v3 terminals stopped (FREEZE)      [ ] StoreHub healthy + auth works
[ ] final backup captured + SHA-256        [ ] every terminal connects + rings a test sale
[ ] store ID/type double-checked           [ ] smoke test IDs recorded/voided
[ ] installer + tools copied local         [ ] owner/operator sign-off
[ ] Postgres/ports (5432/5000) verified    [ ] go-live timestamp + first real txn recorded
[ ] v3 restart/rollback verified
```

## 6. Rollback model (three stages)

The plan's earlier "instant fallback" is only true **before v4 takes real transactions**. Once a real
sale exists in v4, reopening v3 returns to a DB missing those sales.

**Stage 1 — before the first real v4 transaction (fast fallback):**
```
Stop v4 → point terminals back to v3 → open untouched v3 → resume selling.
```

**Stage 2 — after v4 has taken real transactions (controlled rollback):**
```
Stop new sales → capture the v4 transaction range since cutover → export/report them →
decide reconciliation → point terminals back to v3 → reconcile → resume.
```
> **Point of no return:** once the first non-test business transaction is accepted in v4, rollback is a
> **data-reconciliation** operation, not a simple app switch.

**Stage 3 — retrying the migration itself:** never re-run the migrator against a partial target.
`STOP → inspect logs → cleanup-v4.ps1 (reset to Fresh) → confirm Fresh → dry-run → migrate once.`

(See `rollback-plan.md`, RTO ~30 min, for the Stage-1/2 mechanics.)

## 7. Evidence bundle (per store, both phases)

Keep one folder per store per run:

```
rollout/<Store>/<YYYY-MM-DD>/
  preflight.md          Store.db.backup       Store.db.sha256
  migrate-dry-run.log   migrate.log           verify.log
  smoke-test.log        health-check.log      cutover-record.md
```
`cutover-record.md` records: store ID · store type · v3 version · v4 build · operator · backup path +
hash · last v3 invoice · migrate start/end · verify result · accepted exceptions · smoke txn IDs ·
first real v4 txn · owner sign-off.

## 8. Reference index

| Need | Doc |
|------|-----|
| Fresh-install per-store checklist | `docs/operations/pilot-checklist.md` |
| Installer behaviour, silent flags, exit codes | `docs/operations/upgrade-procedure.md` |
| Terminal / multi-terminal setup | `docs/operations/store-installation-guide.md` |
| Rollback to legacy SQLite | `docs/operations/rollback-plan.md` |
| Post-cutover monitoring | `docs/operations/post-deployment-monitoring.md` |
| Migration troubleshooting (23505, re-run refusal) | `docs/operations/troubleshooting-guide.md` |
| Verification scripts | `docs/operations/smoke-test.ps1`, `health-check.ps1` |
| Cleanup / reset to Fresh | `scripts/cleanup-v4.ps1` |
| v3 footprint capture / replay | `scripts/vm-testing/Get-V3Footprint.ps1`, `New-V3Footprint.ps1` |
