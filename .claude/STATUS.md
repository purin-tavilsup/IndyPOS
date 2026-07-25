# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `cleanup/cosmetic-minors` @ `df1e06e` (6 commits, NOT pushed). Off `development` @ `d7b2998` (pushed). |
| **Sprint** | Sprint 7 |
| **Phase** | ✅ **Epic M (data-driven payment methods) SHIPPED + VM-VALIDATED**, and ✅ **Product-type restriction by store type SHIPPED (2026-07-19)** — both merged to `development`. |
| **Blocked?** | Not blocked. Merged + pushed to `origin/development`. Only cosmetic Minors + a live Minimart VM smoke remain. |

## ⏯️ RESUME HERE (2026-07-25) — Cosmetic-minors cleanup batch: CODE COMPLETE, one gate left

**State:** Branch `cleanup/cosmetic-minors` @ `df1e06e`, 6 commits, not pushed. Closes every
deferred cosmetic Minor from Epic M + the product-type restriction. Ledger detail:
`.superpowers/sdd/progress.md` (last section).

**What shipped:** payment-method **Kind taxonomy re-specced** by Pond mid-batch — `Standard`(1,
was `Permanent`) / `GovernmentCampaign`(2) / `Special`(3, new); Cash+MoneyTransfer=Standard,
PayLater=Special, WelfareCard=GovernmentCampaign (stays enabled), with a reversible WHERE-guarded
data migration for the 3 live stores. Plus: Thai Kind labels (มาตรฐาน/พิเศษ/โครงการรัฐ), grid
refresh on successful toggle, payment-button icons restored (unknown campaign codes → text-only),
`PUT /products/{id}` → **404** for a missing id via the existing `ProductNotFoundException` (409
now means only a real conflict), and the SalePanel features-error dialog warns once per session.

**Test state:** Release build of the solution 0 err. Domain 8/8, Application **274/274** (+9 new),
Bootstrapper 96 pass/8 skip.

**⏳ THE ONE GATE — StoreHub integration suite (64) not run: Docker daemon is down on this box.**
It covers the `UpdateProduct_NonExistent_ReturnsNotFound` flip. Start Docker Desktop, then:
`dotnet test tests\IndyPOS.StoreHub.IntegrationTests`. After that → whole-branch review →
`finishing-a-development-branch` (merge to `development`).

**Skipped deliberately:** T7-m1 (stale Hardware category text on a legacy Hardware product) — not
reachable today, server guard blocks the persist anyway.

---

## ⏯️ Earlier checkpoint (2026-07-19) — Product-type restriction by store type: SHIPPED ✅

**State:** Merged to `origin/development` @ `202f48d`. Enforces `StoreTypeFeatures.MultipleProductTypesEnabled` → **Minimart = General Goods only**; GeneralHardware unchanged. 7 tasks via subagent-driven-development (per-task TDD + review) → final whole-branch review (1 Important closed: the update path was initially unguarded). Spec: `docs/superpowers/specs/2026-07-19-product-type-restriction-design.md`; plan: `docs/superpowers/plans/2026-07-19-product-type-restriction.md`. Ledger: `.superpowers/sdd/progress.md`.

**What shipped:** new `GET /store/features` endpoint; `CreateProduct` + `UpdateProduct` handlers reject Hardware on general-only stores (server = real boundary; independent of the UI); `/products` POST+PUT map `InvalidOperationException` → 409 Conflict; WinForms hides the Add Hardware button (SalePanel) and drops Hardware from the product create/update category pickers; WinForms fails OPEN on a features-fetch error (server still guards). **CoffeeShop = future separate app** (Pond's call — made-to-order + add-ons don't fit barcode retail); supported store types = **GeneralHardware + Minimart**. No Domain changes (the flag pre-existed).

**Test state:** Domain 8/8, Application 265/265, StoreHub integration 64/64 (real Postgres via Docker), Release build 0 err.

**Remaining (none blocking):**
- **Live Minimart VM smoke** (owed — no WinForms UI harness): `IndyPOS-Setup.exe --silent --store-id <ID> --store-type Minimart`, then confirm the sale screen shows no Add Hardware button and product create/update category pickers offer only General Goods; a GeneralHardware install still shows both. Do this alongside the Epic M smoke.
- **Minor follow-ups (ledger):** stale Hardware category text if editing a legacy Hardware product on a general-only store (cosmetic; server still blocks persist); not-found on `/products` PUT returns 409 not 404 (coarse, TODO-noted); `SalePanel` features-fetch error dialog re-fires per Sales visit on sustained outage.

---

## ⏯️ Earlier checkpoint (2026-07-19) — Epic M "data-driven payment methods": SHIPPED + VM-VALIDATED ✅

**State:** Fully done and on `origin/development` @ `405e007`. 12 tasks via subagent-driven-development (per-task TDD + review) → final whole-branch review (2 Important fixed) → merged. Then landed on `development`: cleanup batch (6 Minors), `MustChangeGateTests` isolation fix (integration suite now **62/62**), and PayLater seeder term → `ลงบัญชี`. Installer rebuilt (`publish/IndyPOS-Setup.exe`, embeds Epic M).

**VM validation (Docker + Hyper-V, 2026-07-19) — all green:**
- Silent clean-install **18/18** verifier (7m48s); service Running, health 200; `Store:Type=GeneralHardware` written; `payment_method` table seeded 7 rows (4 enabled permanent / 3 disabled campaigns, correct kind+order).
- **Live UI (Pond, via vmconnect):** admin "จัดการวิธีการชำระเงิน" screen renders all 7; **edit persisted** (Pond renamed PayLater → ลงบัญชี, DB `last_modified_utc` bumped); dynamic payment buttons render from offerable catalog (campaigns excluded); **refund gate = Cash + MoneyTransfer only** (FR-2 confirmed). VM now **Off**.
- NOT separately live-run (server side covered by integration 7/7): a normal non-refund Cash sale completion; the Minimart-excludes-PayLater store-type gate (would need a `--store-type Minimart` install).

**Test state:** Domain 5/5, Application 259/259, Bootstrapper 96/8-skip, StoreHub integration **62/62** (real Postgres). Release build 0 err. Ledger: `.superpowers/sdd/progress.md`.

**Remaining (none blocking):**
- **Cosmetic Minors (follow-ups):** dynamic payment buttons lost per-method icons (T9b-m2); Kind column shows raw enum name; grid no-refresh-on-toggle-success. See ledger "Minor findings".
- **Deferred follow-up specs (new work when wanted):** product-type restriction (`MultipleProductTypesEnabled`, the Epic M sibling — gate product types by StoreType); cloud/central catalog distribution (needs Epic I).
- If distributing to the 3 real stores: copy `publish/IndyPOS-Setup.exe`; per-store run wizard (or `--silent --store-id <ID> [--store-type Minimart|CoffeeShop]`).

---

## ⏯️ Earlier checkpoint — Epic M SPEC + PLAN (superseded by merge above)

**Branch:** `payment-methods-catalog` (off freshly-synced `development`). Nothing implemented yet — spec + plan only.

**What & why:** Payment methods are a hardcoded `PaymentType` enum, so each new Thai government campaign (~1yr, e.g. M33WeLove/FiftyFifty/WeWin) needs a **redeploy**. Redesign → **data-driven `payment_method` catalog** (new campaign = a DB row, no redeploy; dead campaigns kept as disabled rows for history). Also gates methods by **StoreType** (PayLater = GeneralHardware-only, a **code invariant** — rural credit culture makes PayLater the most troublesome method) and **fixes the silent-GeneralHardware bug** (installer never writes `Store:Type` today, so every store defaults GeneralHardware and gating never bites). See memory `project_indypos_payment_methods_domain`.

- **Spec:** `docs/superpowers/specs/2026-07-18-data-driven-payment-methods-design.md` (`059afad` + `a6c45fc`).
- **Plan:** `docs/superpowers/plans/2026-07-18-data-driven-payment-methods.md` (`184c9da`) — 12 tasks, 3 phases (A backend foundation / B API+client / C UI+installer). Placeholder-free, grounded in real code shapes.
- **Key decisions (Pond-approved):** manual `IsEnabled` on/off is authoritative (dates optional/informational — exact campaign dates unknown); go-forward storage = stable `Code` **string** on `Payment.Method`; new `IndyPOS.Domain.Tests` project for the pure `PaymentMethodPolicy`; admin screen edits DisplayName+DisplayOrder too. Toggle mechanism = **in-app SystemAdmin "Payment Methods" screen** (config-file/DB-edit rejected).
- **Code-shape gotchas baked into the plan:** mediator is **Nokpirab** (register handlers individually via `AddTransient`); auth is **capability-based** (`CapabilityRequirement` + named policy, NOT role names) — add `Capability.ManagePaymentMethods` + `RoleCapabilities` grant; WinForms & StoreHub are **separate processes** (POS gates buttons by rendering `GET /payment-methods`).
- **Deferred (own follow-up specs):** product-type restriction (`MultipleProductTypesEnabled`); cloud/central catalog distribution (needs Epic I).

**NEXT:** execute the plan via `superpowers:subagent-driven-development` (fresh implementer + task reviewer per task; new ledger section in `.superpowers/sdd/progress.md`). Task 12 needs a read-only `SELECT DISTINCT method FROM payment;` to decide if a data-migration mapping is required.

**Epic I (Cloud Infra) deliberately deferred** — CloudApi already exists as single-DB-with-StoreId; the April "DB-per-store-type" premise is superseded. Epic I only unblocks central reporting, not the local store-type UX.

---

## ⏯️ Earlier checkpoint (2026-07-18 AM) — Silent Installer Mode: MERGED (PR #51)

`IndyPOS-Setup.exe --silent --store-id <ID>` — headless install, no wizard. Reuses `InstallationOrchestrator` unchanged; ACL-locked log + `INDYPOS_MARKER` lines; exit codes 0/1/2/3/4. VM harness drives it (no vmconnect). **Merged to `development` via PR #51** (2026-07-18 14:54). Unattended VM clean-install 18/18 PASS (9m27s); force-change modal confirmed live. Final whole-branch review caught a Critical hang (headless `.NET`-absent → blocking `MessageBox` in `DotNetInstaller`) — fixed via `InstallationConfig.Interactive`. Plan: `docs/superpowers/plans/2026-07-18-silent-installer-mode.md`. Ledger detail: `.superpowers/sdd/progress.md`.

---

## ⏯️ Earlier checkpoint (2026-07-14) — Admin-provisioning feature CODE COMPLETE + whole-branch reviewed; VM validation pending

**What shipped this session (branch `indypos-overhaul`, commits `bc0b908`..`0db8ddd`, 24 commits incl. spec+plan+fixes):**
Replaced the wizard-typed admin password with a **random single-use bootstrap** credential that is **server-side force-rotated on first login**. Executed the 18-task plan (`docs/superpowers/plans/2026-07-14-admin-provisioning-bootstrap.md`) via subagent-driven-development (fresh implementer + task reviewer per task; ledger at `.superpowers/sdd/progress.md`). Spec: `docs/superpowers/specs/2026-07-14-admin-provisioning-bootstrap-design.md`.

- **StoreHub:** `StoreUser.MustChangePassword` (+EF migration, defaultValue false) → `AuthResult`/`LoginResponse` → `must_change` JWT claim → middleware 403s all routes except `/auth/change-password` until rotated → `POST /auth/change-password` (identity from token, verify-then-atomic-set, fresh token w/o claim) → `reset-admin` CLI recovery.
- **WinForms:** must-change login DEFERS session (no event/sync); `FirstLoginCoordinator` prompts (`ChangePasswordForm`) → change → re-login establishes real session.
- **Installer:** Store-ID-only wizard; random 14-char `AdminPassword`; seed via `migrate` (prints `ADMIN_SEEDED=`); surgical removal of plaintext `initialAdmin` from appsettings (DPAPI secrets preserved byte-identical); finish screen shows one-time cred (or "retained") + ACL-locked `admin-credentials.txt`.

**Verification:** `dotnet build` 0 err; `IndyPOS.Application.Tests` 241/241; `IndyPOS.Bootstrapper.Tests` 49 pass/8 skip. StoreHub integration tests (change-password, must_change gate) **authored but Docker-gated — NOT run here**, deferred to VM/CI. Whole-branch review (opus): "Ready with must-fixes"; both must-fixes (ACL-lock summary file `4efd14f`; crash-hardening ChangePasswordAsync generic catch `4efd14f`) + 2 doc/cosmetic (`0db8ddd`) APPLIED. HEAD `0db8ddd`.

**DONE this session:** installer rebuilt (190 MB) · **VM clean-install validation PASSED** — verifier 18/18; admin-credentials.txt ACL-locked (SYSTEM+Administrators, no-inherit); `initialAdmin` removed from appsettings while `storehub-db`+`localToken:secretKey` stay DPAPI-protected; DB `store_user` = admin/Rungrat-001/must_change=f; **WinForms force-change modal worked** (Pond: "it works", DB confirms rotation + login); old bootstrap → **401** (single-use proven). PR #50 (`indypos-overhaul`→`development`) pushed + described + **draft**.

**NEXT (Pond's call — merge):**
1. `gh pr ready 50` (mark PR ready — VM gate passed) then merge to `development`; OR merge locally.
2. Optional un-run check: `reset-admin` CLI round-trip (recovery path) — validated by inspection, not exercised in VM.
3. Post-merge follow-ups (deferred Minors in `.superpowers/sdd/progress.md` — none block merge): DRY the two password generators, orphaned-.tmp cleanup, the couple of test-rigor nice-to-haves.
- **NOTE:** `gh` active account is now `purin-tavilsup` (personal). Switch back with `gh auth switch --user purin-mimica` for Mimica work.

---

## ⏯️ Earlier checkpoint (2026-06-24) — Stage 7 DONE; Bug F + service-start timeout FIXED, committed & VM-validated

**2026-06-24 — full arc:**
1. **Clean full VM install validated 18/18** (restored `Clean-Windows-Ready`, fresh wizard install). Pond confirmed: wizard footer correct, app auto-launches on Finish, `admin`/`myAdmin@101` logs in end-to-end. **Stage 7 complete.**
2. **Bug F FIXED & validated** (commit `115777d`). Root cause was NOT "service ignores storeId" — it was a config **section/key mismatch**: installer wrote `storeIdentity:storeId` but `StoreIdentityOptions` binds `Store:Id` → fell back to `STORE-{MachineName}`. Fix: `DatabaseSetup` writes `Store:Id`. Proven in DB: seeded admin `store_id = Rungrat-001` (was `STORE-DESKTOP-C0QER3F`).
3. **Service-start timeout FIXED & validated** (commit `4a1996a`). Fresh-install run surfaced it: StoreHub ran EF migrations before `app.Run()` → overran the 30s SCM start timeout (Event 7009/1053) → service left **Stopped** after install. Fix: added `migrate` CLI mode to StoreHub (migrate+seed, then exit before `app.Run()`); bootstrapper runs `IndyPOS.StoreHub.exe migrate` as a console step after DB setup, before starting the service. Re-validated: **18/18, service Running, health 200, store_id still Rungrat-001**.
4. **VM space reclaimed, harness kept:** deleted stale `Clean-Windows-Ready-OLD` checkpoint; after validation reverted to `Clean-Windows-Ready` + powered VM **Off**. C: free now ~81 GB. `Clean-Windows-Ready` intact for the next clean-install run.

**Both fixes committed on `indypos-overhaul`** (`115777d`, `4a1996a`). Bootstrapper tests 43/43 (+1 new `ProvisionDatabaseAsync` guard test). Installer rebuilt 189.9 MB (2026-06-24 16:09) with both fixes.

**Reusable techniques (this session):** verify DB state in-guest by decrypting the DPAPI conn string — machine scope, entropy `SHA256("IndyPOS:" + key)`, marker `DPAPI:`; guest runs **PowerShell 5.1** (use `[SHA256]::Create().ComputeHash`, not static `HashData`). Snapshot restore skips autologon → **reboot the guest** to trigger it (avoids the lock-screen fight). vmconnect blank/parked = it remembered a multi-monitor layout; plug in a 2nd monitor or relaunch.

**NEXT:** (1) **admin-provisioning A–D decision** (will change installer → needs another clean-install validation; VM is ready) · (2) whole-branch review → finishing-a-development-branch. *(Older 2026-06-23 checkpoint below retained for detail.)*

---

## ⏯️ Earlier checkpoint (2026-06-23) — App logs in end-to-end; needs clean full-install validation

**Today:** Polished the installer wizard UI, ran the Stage 7 VM smoke test (18/18 verifier PASS, app reached login → **Bugs A & B proven fixed**), then debugged the login flow to a working end-to-end POS sign-in. Found + fixed **3 more prod bugs**; all committed on `indypos-overhaul`.

**Commits this session:** `428e480` (wizard footer: single primary action) · `6298be8` (wizard footer: ClientSize margins) · `0267354` (**Bug E**: config base path) · `f363e71` (**Bug D**: failed-login crash) · `7f1ff02` (**port mismatch**: StoreHub 5000/5012). **Installer rebuilt 189.9 MB (2026-06-23 00:50).**

**Bugs fixed + how validated (all in the VM):**
- **Bug D — failed login crashed the app.** `async void LogInButton_Click` had no re-entrancy guard → re-entrant `ShowDialog` on the shared singleton `MessageForm` threw "Form already visible". Fix: disable login button during async login + `MessageForm.ShowDialog` early-returns if already `Visible`. ✅ VM now shows a graceful Thai error instead of the .NET crash dialog.
- **Bug E — app crashed when launched with a different CWD.** `Program.BuildAppConfiguration` used `Directory.GetCurrentDirectory()` → `appsettings.json` not found when CWD ≠ install dir (the wizard Finish inherits `C:\Test`). Fix: `AppContext.BaseDirectory`. ✅ Proven by launching from `C:\Windows` — no config crash (also re-confirmed Bug B: missing COM1 → warning, not crash).
- **StoreHub port mismatch — every prod login failed as "wrong username/password".** WinForms `appsettings.json` pointed at `:5012` (Aspire dev value) but installed StoreHub listens on `:5000`. Fix: `appsettings.json` → `:5000` (prod canonical); AppHost injects `StoreHub__BaseUrl=:5012` for dev; env vars added last so the dev override wins. ✅ Login succeeds in VM.
- **Bug C ("username/password mismatch") was NOT a bug** — the StoreHub `/auth/login` API returns 200 for `admin`/`myAdmin@101`; the real causes were the port mismatch + a mistyped password.

**NEXT SESSION:**
1. **Clean full VM install** of the new `IndyPOS-Setup.exe` (`Reset-AndInstall.ps1 -KeepRunning`) to validate the **wizard UI** (footer layout) + **Finish-button launch** + end-to-end login on a fresh install (today's app fixes were validated by hot-swapping WinForms binaries into `current\`, not via a fresh install).
2. **Bug F (latent):** seeded admin `store_id` comes out as the machine name (`STORE-DESKTOP-C0QER3F`), not the wizard's Store ID (`Rungrat-001`). `IStoreIdentityService` ignores configured `storeIdentity.storeId`. Audit + fix.
3. Decide the **admin-provisioning model (A–D)** below; then whole-branch review → finishing-a-development-branch.

---

## ⏯️ Earlier checkpoint (2026-06-21 EVENING) — DPAPI Vault

**Today (evening):** Brainstormed → spec'd → planned → executing the **DPAPI secret-protection** work (the "at-rest secrets" decision below, now realized as a new `IndyPOS.Vault` leaf project). Spec: `docs/superpowers/specs/2026-06-21-vault-dpapi-secret-protection-design.md`. Plan: `docs/superpowers/plans/2026-06-21-vault-dpapi-secret-protection.md`. SDD ledger: `.superpowers/sdd/progress.md`.

**Done + committed this session (subagent-driven, all reviewed clean):**
- **Task 1** (`1fc928a`): `IndyPOS.Vault` + `SecretProtector` (DPAPI machine-scope, `DPAPI:` marker, per-key SHA256 entropy, fail-fast). 17/17 tests.
- **Task 2** (`23a6f4f`): StoreHub `UnprotectSecrets` ConfigurationManager extension wired into `Program.cs` (decrypts before consumers; no-op in dev). 7/7 tests. *(NB: this commit also swept in prior-session Program.cs prod-startup wiring — intended work, flag for history cleanup.)*
- **Task 3** (`a7e85c9`): installer DPAPI-protects connection string + JWT key, **drops plaintext `storehub.key`**, ACL-locks `appsettings.json`. 3 new tests + full bootstrapper suite 42 pass/8 skip.

**NEXT SESSION — resume at Task 4:**
1. **Task 4** — scripts + docs cleanup: remove `storehub.key` refs from `scripts/verify-install.ps1`, `scripts/install-config.ps1`, `docs/operations/store-installation-guide.md`. (Base `a7e85c9`.)
2. **Task 5** — full `dotnet build`/`test`, rebuild installer, Hyper-V VM validation (happy + negative-wrong-machine + idempotency, per plan checklist).
3. Final whole-branch code review → finishing-a-development-branch. Triage the Minor findings logged in the ledger (T3 M2/M3 are trivial).
4. Decide history cleanup: this branch still has **lots of uncommitted prior-session installer work** (InstallationOrchestrator, DotNetInstaller, VCRedistInstaller, InstallationConfig, InstallationWizard, InitialAdminSeeder, …) mixed with Vault commits.

**Still DEFERRED (separate decision Pond will make):** admin-login provisioning model for a **Store-ID-only wizard**.
- **A (recommended):** installer generates a strong admin password, seeds it as a BCrypt hash, reveals it once at finish + protected summary file, forces change on first login. Offline, no shared secret, shippable now.
- **B:** cloud-provisioned by Store ID via existing `UserSyncService` — best long-term, blocked on CloudApi deploy (Epic I).
- **C:** bundled encrypted credential blob (installer holds decrypt key — extractable).
- **D:** documented default + force change on first login (simplest, weakest).

**Decided:** at-rest secrets (DB connection string, JWT key) → **DPAPI machine-scope** (encrypt; StoreHub decrypts at startup). Not yet implemented.

**Impact on already-written Bug D code:** the StoreHub side is a KEEPER regardless of A–D — prod-startup EF `MigrateAsync` + `InitialAdminSeeder` (reads `InitialAdmin:{Username,Password}` from StoreHub appsettings, seeds SystemAdmin if absent, idempotent) + auto-generated DB password (`InstallationConfig.GenerateSecret`, alphanumeric) + `CREATE`-or-`ALTER` role password. **PROVISIONAL:** the wizard's admin **input fields** (`InstallationWizard` Admin Username/Password/Confirm) — options A/C/D remove user-entered admin creds and have the installer write generated/derived/default creds into the `InitialAdmin` config section instead. So expect to rework `InstallationWizard` + how `InstallationConfig.AdminUsername/AdminPassword` get populated once A–D is chosen. Also still TODO under any option: DPAPI wrap, reveal/force-change (A), clear `InitialAdmin:Password` from appsettings after seed.

**Also noted (separate follow-ups surfaced this session):**
- Installer writes a `"My Store"` `StoreConfiguration.json` template that ignores the wizard's Store ID — audit other `IsDevelopment()`-only gaps (product seeding is dev-only too).
- Settings-panel `JsonService.SaveToFile` uses `File.Create` — may hit the same admin-owned-file ACL when overwriting; verify separately.
- High-leverage idea: build silent install mode (`--silent --store-id …`) to run the whole VM cycle unattended/CI (every bug A–D was caught by the VM, ~15 min + manual clicks each).

**Immediate next once decision made:** implement chosen provisioning + DPAPI → rebuild installer → `Reset-AndInstall.ps1 -KeepRunning` → log in with admin creds → then commit the whole validated bundle.

## Recent Session (2026-04-27)

### Aspire Local Testing - COMPLETE ✅

Fixed all blockers for running IndyPOS locally with Aspire:

| Fix | Description |
|-----|-------------|
| global.json | SDK version 7.0.0 → 10.0.107 |
| AppHost postgres | Added WithDataVolume + WithLifetime for persistence |
| WinForms URL | Port 5000 → 5012 for StoreHub |
| LocalTokenOptions | Moved to Application layer with defaults |
| Login | Removed password encryption (API expects plaintext) |
| Products | Fixed StoreId in seeder, singleton HttpClient for auth |
| Reports | Added ReportErrorHandler for 403/error handling |
| Logging | Added debug logging to all command/query handlers |
| OpenTelemetry | Updated to 1.15.x for CVE-2026-40894 fix |
| WinForms in Aspire | Added with WithExplicitStart() |
| dbgate | Removed (pgAdmin sufficient) |
| **Timezone** | Added store timezone support for reports (defaults to Thailand) |

**Test Results:** All 306 tests passing (219 + 15 + 23 + 49)

### Timezone Support
Reports now use store-configured timezone (default: "SE Asia Standard Time" for Thailand).
This allows testing from any location (e.g., Canada) while reports use Thai local time for date calculations.

### Test Accounts (seeded)

| Username | Password | Role |
|----------|----------|------|
| admin | admin123 | SystemAdmin |
| manager | manager123 | StoreManager |
| cashier | cashier123 | Cashier |

## In Progress

### Epic M: Multi-Store Type Support 🟡 IN PROGRESS

**Completed:** M1-M6 (core domain)
**Pending:** M7-M13 (UI, installer, CloudApi, migrations, docs)

## Next Actions (Priority Order)

### 1. Installer Side-by-Side (v3.7.0 ↔ v4.0.0) — ACTIVE 📋
**Plan:** `.planning/indypos-overhaul/drafts/installer-side-by-side-plan.md`
**Tasks:** 8 stages tracked (Stages 0, 1, 2 ✅ · Stages 3–7 pending)

**Stage 0 ✅** — Discovery: dev box clean, 3.7.0 footprint mapped, no conflicts.

**Stage 1 ✅** (2026-05-02) — `InstallationConfig` version-aware (9 computed properties from assembly version).

**Stage 2 ✅** (2026-05-03) — Build pipeline + version alignment:
- `vpk` CLI installed globally (v0.0.1298)
- `build-installer.ps1` now stages `Resources/` from `publish/Releases/` before `dotnet publish` (globs vpk Setup.exe pattern, renames to canonical names)
- Repo-wide version bump in `Directory.Build.props`: `1.0.0` → `4.0.0` (Stage 1 leak — D.B.props was clobbering bootstrapper's local `<Version>`)
- `WinForms/Properties/AssemblyInfo.cs`: `3.7.0` → `4.0.0` (legacy `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` bypassed D.B.props; vpk auto-detects pack version here)
- `Resources/` gitignored
- **Verified:** Bootstrapper `AssemblyVersion 4.0.0.0`, both manifest resources embedded with expected names, final `IndyPOS-Setup.exe` 189 MB v4.0.0.0, vpk packs `IndyPOS.POS.v4` v `4.0.0`

**Stage 3 ✅ COMPLETE** (2026-05-27) — `verify-install.ps1`: **31/31 PASS** end-to-end. Install ran in 9m31s (Postgres extraction ~9m + everything else <30s). v3.7.0 footprint confirmed untouched.

**7 production bugs caught + fixed** by the cycle (none would've been caught by unit tests):
1. Postgres installer `--serviceaccount NT AUTHORITY\NetworkService` was unquoted → EDB exit 1 in 30s.
2. 10-min Postgres install timeout too tight when Defender real-time-scans every extracted file → bumped to 25 min.
3. `FindPostgresInstallation` only checked `psql.exe`; a half-installed Postgres (killed mid-unpack) fooled it → also requires the `postgresql-x64-NN` service.
4. `StoreHubInstaller` zip-extracted AFTER `DatabaseSetup` wrote `appsettings.Production.json` → clobbered real config with the template. Reordered orchestrator (binaries first, DB+config second).
5. `IndyPOS.StoreHub` was a console ASP.NET app → SCM start callback timed out (error 1053). Added `AddWindowsService` + `Microsoft.Extensions.Hosting.WindowsServices`.
6. Bootstrapper probed `/health/live` which didn't exist → 404. Switched to `/health/ready` and standardised endpoints (see Stage 3 refactors below).
7. `psql` defaults to interactive password prompt → hung when `pgResult.SuperuserPassword=""` (existing-install case). Added `-w` (never-prompt) flag + early-return in `DatabaseSetup.SetupAsync` with actionable error.

**Stage 3 refactors (architecture + QA reviews):**
- `scripts/cleanup-v4.ps1` (~250 lines) with P0+P1 hardening: `DbConnectionStringBuilder` parse, `Assert-V4Path` regex guard, `Wait-ServiceGone` poll, Velopack process wait + shortcut sweep, `DROP OWNED BY` before `DROP ROLE`.
- `scripts/verify-install.ps1` (~330 lines) — install-artifact audit, no admin required, 31 checks across 7 categories.
- **Install manifest** (`InstallManifest.cs` + `Writer` + 3 tests): bootstrapper writes `$SystemRoot\install-manifest.json` (camelCase JSON, ManifestVersion=1, no secrets). Cleanup + verify glob-discover under `v*\` subdirs of `$ProgramDataRoot` — version-agnostic, no more `# must match InstallationConfig.cs` lockstep.
- **Postgres installer cache** at `%LOCALAPPDATA%\IndyPOS.Bootstrapper\cache\` — skips 372 MB re-download every cycle. Stale partial downloads (<100 MB) auto-discarded.
- **`appsettings.Production.json` → `appsettings.json`** + orchestrator reorder. Single-tier deployment, no overlay machinery. Removed the runtime-generated-files skip-list workaround entirely.
- **Industry-standard health endpoints** in `ServiceDefaults`: `/health/live` (tag=live) + `/health/ready` (tag=ready). Replaced StoreHub's custom `MapGet` with framework `AddDbContextCheck`. `/health` stays dev-only (verbose body).

**Stage 5 ✅ COMPLETE** (2026-05-27) — VM smoke-test scaffolding landed. Semi-automated (bootstrapper has no silent mode yet, so wizard runs via `vmconnect.exe`):
- `scripts/vm-testing/VMTestConfig.psd1` — shared config
- `scripts/vm-testing/Test-IndyPOSInstallation.ps1` — in-VM verifier (subset of `verify-install.ps1`, no v3 side-by-side checks needed in fresh VM)
- `scripts/vm-testing/Reset-AndInstall.ps1` — host orchestrator (8-phase: preflight → snapshot restore → start → PSDirect wait → copy installer → vmconnect handoff → poll manifest → run verifier → report)
- `scripts/vm-testing/README.md` — one-time VM creation block, credential setup, example output
- Cred storage: DPAPI-encrypted XML at `%LOCALAPPDATA%\IndyPOS\vm-test-cred.xml`. `-RecreateCredential` to rotate
- Both scripts parse-checked clean

**Stage 6 ✅ COMPLETE** (2026-05-28) — Test VM live:
- `IndyPOS-Test` Gen2 VM at `C:\personal\VMs\IndyPOS-Test.vhdx` (60 GB dynamic, 4 vCPU, 4 GB startup / dynamic 2-8 GB, TPM + Secure Boot `MicrosoftWindows`)
- Win11 Pro 25H2 from `C:\personal\ISOs\Win11_25H2_English_x64_v2.iso` (now detached, HDD boot-first)
- Guest user `IndyPOSAdmin` · network profile `Private` · PSRemoting on · TrustedHosts `*` · Tamper Protection off
- `Clean-Windows` Standard checkpoint captured 2026-05-28 00:31

**Notable gotchas burned through (memorialized in memory):** vmconnect input capture, `ms-cxh:localonly` for local-account bypass, Default Switch defaulting to Public network profile (Enable-PSRemoting firewall step fails), Tamper Protection silently no-ops `Set-MpPreference`. See `memory/reference_win11_hyperv_vm_gotchas.md`.

**Stage 7 (2026-05-30→31) — VM smoke test reached 18/18 verifier PASS and then surfaced + fixed 3 real prod bugs the dev-box install never caught. All changes UNCOMMITTED.**

**Fixes landed + how validated:**
1. ✅ **`VCRedistInstaller` wired** into `InstallationOrchestrator` "Step 1b" (between .NET + Postgres). Fixes Postgres `initdb -1073741515`. **VM-validated** (Postgres 18 service Running).
2. ✅ **`DotNetInstaller` detection** rewritten — `FindDotNetRoot()` (DOTNET_ROOT → %ProgramFiles%\dotnet → %ProgramW6432%, no stale PATH) → primary folder probe `shared\Microsoft.WindowsDesktop.App\10.*` → fallback absolute `dotnet.exe --list-runtimes`. Dropped dead registry probe + `DotNetDownloadUrl`. **VM-validated** (wizard detected .NET 10, no PATH refresh).
3. ✅ **winget silent auto-install** added to `DotNetInstaller.EnsureInstalledAsync` — tries `winget install --exact --id Microsoft.DotNet.DesktopRuntime.10 --source winget --silent --accept-*-agreements --disable-interactivity` (5-min timeout, async stream drain), falls back to manual browser flow. Extracted manual path to `InstallManually()`. **Built, NOT yet VM-proven** (last run Pond installed .NET by hand, confounding the test).
4. ✅ **Verifier exe-name fix** — `Test-IndyPOSInstallation.ps1` looked for `IndyPOS.exe`; actual Velopack `--mainExe` is `IndyPOS.Windows.Forms.exe`. **VM-validated** (18/18 PASS).
5. ✅ **Serilog.AspNetCore removed** from WinForms csproj — it dragged `Microsoft.AspNetCore.App` FrameworkReference into a desktop app → "must install .NET / AspNetCore.App 10" pop-up at launch. App only uses `.UseSerilog()` on IHostBuilder (from `Serilog.Extensions.Hosting`). **Proven** via published `runtimeconfig.json` (now only NETCore.App + WindowsDesktop.App) + no pop-up at Finish.
6. ✅ **Version-aware app paths (major root `v4`)** — *the 3rd bug:* WinForms hardcoded legacy `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json` but installer writes versioned root → `DirectoryNotFoundException` crash at startup (ReceiptPrinterService ctor). Fix (Pond chose major-root scheme):
   - `InstallationConfig.SystemRoot` → `v{Major}` (was `v{InstallVersion}`); now matches `ServiceName`/`VelopackAppId`, survives Velopack patch updates.
   - New `IndyPOS.Application.Common.InstallPaths` — computes `C:\ProgramData\IndyPOS\v{Major}\...` at runtime from assembly version (mirrors installer).
   - `StoreConfigurationService` defaults to `InstallPaths.StoreConfigPath`; `Program.cs` logs to `InstallPaths.LogsDirectory`; dropped legacy keys from WinForms `appsettings.json`.
   - `JsonService.SaveToFile(Async)` now `Directory.CreateDirectory` first (hardening — kills the latent first-run crash even if dir missing).
   - Stale `v4.0.0` defaults → `v4` in `VMTestConfig.psd1`, `verify-install.ps1`, `cleanup-v4.ps1` (manifest-glob already version-agnostic).
7. ✅ **Snapshot `Clean-Windows-Ready` re-baked** (vmicvmsession + DNS + Guest Service Interface + autologon). `Reset-AndInstall.ps1` hardened (idempotent Guest Service Interface enable).

**Build/test state:** WinForms + bootstrapper build clean. `IndyPOS.Application.Tests` 219/219 PASS. Migration/StoreHub integration suites fail ONLY because Docker/Testcontainers isn't running (environmental, unrelated). **Installer rebuilt 5/31 12:48 AM, 189.8 MB** with all fixes.

**Hyper-V/vmconnect friction this session (memory-worthy):** launching vmconnect/Hyper-V Manager directly needs the user in **Hyper-V Administrators** group (`Add-LocalGroupMember -Group "Hyper-V Administrators" -Member $env:USERNAME` + relog) — otherwise "no permission"; workaround is launching vmconnect from the already-privileged orchestrator context. "Use all my monitors" spans only in full-screen — `Ctrl+Alt+Break` drops to single window (no saved registry pref found). Autologon only on cold boot, not snapshot resume.

**2026-06-21 FINAL VM RUN — 18/18 PASS, validated #3 + #5, but app crashes on launch → 2 NEW bugs found.**

Validated this run: **#3 winget auto-install** (Pond let wizard run it; `.NET 10` via winget confirmed) and **#5 Serilog.AspNetCore removal** (clean `runtimeconfig.json`, no pop-up). Verifier 18/18.

But double-clicking the shortcut → no window. Root-caused via in-guest forensics (app's real log was at `C:\ProgramData\IndyPOS\v1\logs`, NOT `v4`):

**🐛 Bug A — version skew defeats `InstallPaths` (regression hiding in fix #6).**
- `IndyPOS.Application.dll` + `IndyPOS.Infrastructure.dll` ship as **1.0.0.0**; `Windows.Forms`+`Domain`=4.0.0.
- `InstallPaths` is in Application.dll → `ResolveMajorVersion()` reads `1` → app reads/writes `...\IndyPOS\v1\...` while installer wrote `v4\`. App made its own `v1` tree w/ default config (SerialPortName=COM1); installer's `v4\Config\StoreConfiguration.json` ignored.
- **Cause:** Application + Infrastructure csprojs have `<GenerateAssemblyInfo>false>` + legacy `Properties/AssemblyInfo.cs` hardcoding `1.0.0.0` → D.B.props 4.0.0 doesn't reach them. (The exact modernization follow-up flagged below.)

**🐛 Bug B — `CashDrawerService` crashes app when serial port absent.**
- `InitializeSerialPort()` calls `_serialPort.Open()` in the **constructor** → `FileNotFoundException: Could not find file 'COM1'` when no COM port → DI resolution fails → app dies before any window. Hits any terminal w/o COM1 (VM, USB drawer, unplugged).
- **Masked on dev box** by `[Conditional("RELEASE")]` on `InitializeSerialPort()` → compiled out in Debug, runs only in Release (installer).

**FIX STATUS (2026-06-21 — both IMPLEMENTED + unit-verified, UNCOMMITTED):**
1. ✅ **Bug A (root) DONE:** deleted `Properties/AssemblyInfo.cs` from `Application` + `Infrastructure` + `WinForms`; dropped `<GenerateAssemblyInfo>false>` from all 3 csprojs → D.B.props now drives every assembly. **Verified:** Application/Infrastructure/WinForms DLLs all stamp `AssemblyVersion=4.0.0.0` → `InstallPaths` resolves `v4`. Legacy template attrs (COM Guid/AssemblyCompany "Exconeer") dropped — none functionally used; metadata now uniform from D.B.props. (Did NOT need the defensive `GetEntryAssembly` alt — root fix suffices.)
2. ✅ **Bug B DONE:** dropped `[Conditional("RELEASE")]` on `InitializeSerialPort()`; wrapped `_serialPort.Open()` in try/catch (logs warning, continues); guarded `OpenCashDrawer()` on `_serialPort.IsOpen`. `ReceiptPrinterService` reviewed — clean (never opens a port; `PrintDocument` only fails at print time). Left `GetStoreConfiguration` rethrow alone — Bug A fixes the missing-config root cause.
3. ✅ **Installer rebuilt:** `publish.ps1` → `build-installer.ps1` clean. Velopack `IndyPOS.POS.v4` packed at `4.0.0`, StoreHub zip `4.0.0`, `IndyPOS-Setup.exe` 189.9 MB v4.0.0 (2026-06-21 08:38).
4. ⏳ **NEXT — VM validation:** re-run `.\scripts\vm-testing\Reset-AndInstall.ps1 -KeepRunning` (clean snapshot wipes stale `v1`+`v4.0.0`) → confirm **app opens to login**. (Needs vmconnect/Pond.)
5. Then **commit the bundle** — logical groups (Pond style): (a) VCRedist+orchestrator, (b) DotNet detection+winget, (c) Serilog.AspNetCore removal, (d) version-aware paths (InstallPaths + InstallationConfig major-root + JsonService hardening), (e) **assembly-version unification (Bug A)**, (f) **cash-drawer resilience (Bug B)**, (g) verifier/script/VM-scaffolding + docs.
5. Stretch: silent bootstrapper mode (`--store-id`/`--app-password`).

**Follow-up filed (post-v4.0.0):** Add `--silent --store-id N --app-password X` to `IndyPOS.Bootstrapper/Program.cs` to drop the wizard step. ~2-4h work — `InstallationOrchestrator` already drives everything headlessly internally. Unblocks CI + customer-support unattended installs.

**Follow-up (modernization) — NOW LOAD-BEARING, see Bug A above:** Delete legacy `Properties/AssemblyInfo.cs` from Application/Infrastructure/WinForms; flip `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` → default. Would let D.B.props drive every assembly's version uniformly. No longer just cleanup — it's the root fix for Bug A (version skew breaking `InstallPaths`).

### 2. Epic I: Cloud Infrastructure
- [ ] I0: Create Dockerfile for CloudApi
- [ ] I1-I2: Provision DigitalOcean (Droplet + PostgreSQL)
- [ ] I3-I4: Deploy CloudApi, configure SyncWorker
- Full plan in `.planning/indypos-overhaul/PLAN.md`

### 3. Continue Epic M (M7-M13)
- M7: Update First-Run Wizard (store type selection)
- M8: Update WinForms UI to respect feature flags
- M9: Update CloudApi for store type routing

## Key Files

| Purpose | Path |
|---------|------|
| Full plan | `.planning/indypos-overhaul/PLAN.md` |
| Installer side-by-side plan | `.planning/indypos-overhaul/drafts/installer-side-by-side-plan.md` |
| VM testing plan | `.planning/indypos-overhaul/drafts/vm-installer-testing-plan.md` |
| Epic M draft | `.planning/indypos-overhaul/drafts/epic-m-multi-store-type.md` |
| Session log | `.claude/session-log.md` |

## Quick Commands

```bash
# Run Aspire (http profile)
dotnet run --project src/IndyPOS.AppHost --launch-profile http

# Run tests
dotnet test

# Build
dotnet build
```

---
*See "⏯️ RESUME HERE" near the top for the live checkpoint (2026-06-21 PM). Older note retained below.*

*Last updated: 2026-06-21 (PM) — **Stage 7: Bug A + Bug B FIXED & unit-verified.** Bug A: deleted legacy `AssemblyInfo.cs` from Application/Infrastructure/WinForms + dropped `GenerateAssemblyInfo=false` → all DLLs now stamp `4.0.0.0`, so `InstallPaths` resolves `v4` (was `v1`). Bug B: `CashDrawerService` now wraps `_serialPort.Open()` in try/catch + dropped `[Conditional("RELEASE")]` + guarded `OpenCashDrawer()` on `IsOpen` → missing COM port no longer crashes the app. Release build clean, `Application.Tests` 219/219. **Installer rebuilt 189.9 MB v4.0.0** (Velopack + StoreHub both 4.0.0) with both fixes. **NEXT: VM run (`Reset-AndInstall.ps1 -KeepRunning`) to confirm app opens to login → then commit a–g bundle. All uncommitted.***
