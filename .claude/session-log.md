# Session Log

> Recent session history for context handoff

---

## 2026-07-26: Velopack spike answered, upgrade plan written, Tasks 1–4 of 14 landed

**Focus:** close the last open question in the installer-upgrade spec, turn it into a plan, then start executing.

**The spike (§12).** Nothing in the codebase established what the embedded Velopack `Setup.exe --silent`
does on a machine where the POS app is already installed, and step 8 plus the failure table depended on it.
Ran both cases against checkpoint `Pre-Upgrade-2026-07-25` (a real store image, 4.0.0 installed, service
Running), restoring the snapshot between them. Built a 4.0.1 package with `vpk pack` from the existing
`publish\WinForms` output rather than touching `publish/` (note: `scripts/publish.ps1` deletes that whole
directory on start).

Result: **4.0.1 over 4.0.0** → exit 0, 7.5 s, `sq.version` 4.0.0→4.0.1, old `.nupkg` pruned, service
untouched, `ProgramData\IndyPOS\v4` untouched, shortcuts preserved, nothing launched. **4.0.0 over itself**
→ exit 0, 2.5 s, a safe *repair*. So step 8 needed no design change, but one real consequence fell out:
**`POS_UPDATED` cannot be inferred from the exit code**, which is 0 either way. It is derived by comparing
`current\sq.version` before and after — the binaries keep their own build stamp regardless of package
version (proved by packing 4.0.1 from 4.0.0-stamped binaries; the exe still reported 4.0.0). Also learned
Setup sweeps foreign files from the install root, not just `current\`; IndyPOS keeps no state there, but
that is now a written rule. Untested and still a documented limitation: running setup as a *different*
admin than the installing one. Also closed §11.7 — no version bump is a safe repair, not a failure.

**The plan.** `docs/superpowers/plans/2026-07-26-installer-upgrade-support.md` — 14 tasks, 87 steps, real
code in every step. Two spec items deliberately deferred with reasons recorded in a self-review section:
raising `migrate`'s 30 s Npgsql timeout (spec says "consider"; touches StoreHub's runtime, belongs with the
first long DDL migration) and the `--store-type`-writes-one-key branch (took the other option the spec
permits — refuse as `Unusable` — so the upgrade path never writes config).

**Execution.** Branch `feat/installer-upgrade-support` off `spec/installer-upgrade-support`, driven by
`superpowers:subagent-driven-development`: fresh implementer per task, independent task review, scoped
re-review of every fix. Tasks 1–4 complete. Release build 0 err / 0 warn, bootstrapper suite 133 pass /
8 skip. Ledger: `.superpowers/sdd/2026-07-26-installer-upgrade-support/progress.md`.

**The useful part: all four tasks needed a fix round, and every finding was a defect in the plan, not
implementer error.** The review loop earned its keep, and each fix was folded back into the plan text so
the remaining ten tasks can't inherit the same shapes.

- **T1** — the reference `Value()` helper called `GetValue<string>()` outside the try/catch, so a
  hand-edited `"id": 12345` threw out of a reader whose own doc comment promises graceful degradation.
- **T2** — the worst one. The superuser guard branched on `ConnectionStringUsable`, which is `false` for a
  store whose DPAPI connection string was sealed on *another machine* — i.e. a replaced POS terminal. That
  routed a live store with intact sales history to `cleanup-v4.ps1 -Force -RemovePostgres`, which drops
  `indypos_storehub` unconditionally. Exactly the data-loss path the task exists to close, in the one
  scenario where the store is already having a bad day. Now branches on `.Exists`, which fails safe.
- **T3** — `DetectStoreDatabase` inspected our own major's config path, so a same-major install that
  crashed between `DatabaseSetup` and the manifest tripped detection rule 1 and told the operator to run a
  different installer. Rule 4 is the correct diagnosis. Also: `TryGetProperty` throws on a non-object
  manifest root, the `v*` enumeration was unguarded, and `ImagePathResolvesUnder` matched sibling
  directories like `StoreHubOLD`.
- **T4** — implementer self-caught: `ServiceControl.StopAsync` swallows the `TimeoutException` the original
  propagated, and the call site discarded the bool, so a service that failed to stop proceeded into
  extraction and hit the locked DLLs the stop-before-extract order exists to prevent. Review then caught
  that the delegating `StartServiceAsync` dropped `ex.Message` — the only lead in a store's install log
  after a failed start — so `ServiceControl` now returns `ServiceControlResult(Success, ErrorMessage)`.

**Test-suite trap found and fixed (worth remembering).** `installer/build-installer.ps1` stages
`Resources/StoreHub.zip` into the source tree and never cleans it up, and the csproj embeds
`Resources\**\*` conditionally — so on any machine that has ever built the installer, the payload is
embedded and the new "no payload available" tests failed for purely environmental reasons on the next
unrelated `dotnet test`. The first verdict was "unfixable without a design change"; the reviewer overruled
it with a better middle option, and `StoreHubPayload.ExtractAsync` now takes defaulted `resourceName` /
`probeDirectory` seams. Verified green both with the 70 MB zip staged and with it absent.

**State at break (Pond's call, ~02:50):** HEAD `c4525cb`, nothing pushed, no PR, installer not rebuilt.
VM parked **Off** at `Pre-Upgrade-2026-07-25`, both snapshots intact, spike mutations discarded.
Resume at Task 5 (extract `MigrationRunner` + `HealthProbe`); Task 6 renames the orchestrator and carries
the refactor diff gate, so keep 5 and 6 in order.

---

## 2026-07-14: Admin-provisioning feature built + v4 overhaul MERGED to `development` (PR #50) 🎉

**Focus:** The last open branch item — the admin-provisioning model — taken from decision all the way to merged.

**Design (brainstorming → spec → review):** Chose **random single-use bootstrap password + server-enforced forced rotation on first login**. (Pond first proposed a `greatSales@ddmmyy` template; walked through why a derived password is only safe as a *rotated single-use bootstrap*, then landed on random-shown-once + a `reset-admin` recovery path, and **server-side** enforcement.) Spec: `docs/superpowers/specs/2026-07-14-admin-provisioning-bootstrap-design.md`. A 3-perspective design review (Architect/Engineer/QA subagents) caught, pre-code: the cosmetic-only client gate, a latent `/auth/me` `sub`-claim bug, the re-install dead-credential case, and the EF-migration-on-populated-DB risk.

**Execution (subagent-driven, 18 tasks):** Plan `docs/superpowers/plans/2026-07-14-admin-provisioning-bootstrap.md`, executed via `superpowers:subagent-driven-development` — fresh implementer + task reviewer per task, ledger at `.superpowers/sdd/progress.md`. StoreHub: `MustChangePassword` flag (+migration `defaultValue:false`) → `must_change` JWT claim → middleware 403s all routes except `/auth/change-password` → `POST /auth/change-password` (token identity, verify-then-atomic-set, fresh token) → `reset-admin` CLI. WinForms: deferred session + `FirstLoginCoordinator` + themed `ChangePasswordForm`. Installer: Store-ID-only wizard, random bootstrap, surgical `initialAdmin` removal (DPAPI secrets preserved), ACL-locked `admin-credentials.txt`. Whole-branch review (opus): "Ready with must-fixes" → 2 applied (ACL-lock summary file; change-password generic-catch crash-hardening).

**VM validation (clean install):** verifier **18/18**; DB `admin/Rungrat-001/must_change`; **single-use proven** (old bootstrap→401 after rotation); appsettings `initialAdmin` gone + DPAPI secrets intact.

**Three field-driven follow-ups this session:**
1. **Themed modal** — the stock light-gray dialog clashed with the dark POS theme; restyled to match `MessageForm` (ModernTextBox/ModernButton, `#262626`/`#1E1E1E`, Gainsboro, teal/red accents), then refined (right-aligned button pair, inset fields + teal focus underline, spacing fix). Validated live via **WinForms DLL hot-swap** into the VM; Pond approved.
2. **`reset-admin` CWD bug** — found during hot-swap: the exe loaded `appsettings.json` from the CWD, so the documented recovery command crashed ("ConnectionString is missing") from any dir but the install folder. Fix: `WebApplicationOptions.ContentRootPath = AppContext.BaseDirectory`. Proven in VM (`migrate` from `C:\` → exit 0).
3. **FC Subject fonts** — the app references family `FC Subject [Non-commercial] Reg` by name in 304 places; fresh machines lacked it (fallback font). Added `FontInstaller` (Step 0, non-fatal): bundles Reg+Bold `.ttf` as embedded resources, installs to `Windows\Fonts` + HKLM + `WM_FONTCHANGE`. Confirmed installed + GDI-visible in VM. (Gotcha: font filenames have `[brackets]` → `-LiteralPath` needed in the build script.)

**Merge:** `IndyPOS-Setup.exe` rebuilt 3× (theme+CWD, then fonts). PR #50 "IndyPOS v4 Overhaul" → out of draft → **merged to `development`** (merge commit, full history). `indypos-overhaul` branch kept.

**Reusable techniques:** WinForms DLL hot-swap into Velopack `current\` over PSDirect for fast UI iteration (no reinstall); in-guest DPAPI conn-string decrypt + psql for DB checks; non-destructive `migrate`-from-arbitrary-CWD probe to validate content-root fixes without mutating admin state.

**NEXT (all optional):** rebuild distributable installer to embed the refined modal (cosmetic gap vs validated build); Tier-2 code-cleanup minors (DRY password generators, orphaned-`.tmp` cleanup, a few test-rigor items — see `.superpowers/sdd/progress.md`); Tier-3 epics: **Epic I (Cloud Infra)**, **Epic M (multi-store-type M7–M13)**, silent installer mode. `gh` active account is now `purin-tavilsup` — `gh auth switch --user purin-mimica` for Mimica work.

---

## 2026-06-24: Stage 7 closed — clean full install validated; Bug F + service-start timeout fixed ✅

**Focus:** Validate the installer on a genuinely fresh VM install (not hot-swap), then fix what it surfaced.

**VM tidy:** deleted stale `Clean-Windows-Ready-OLD` checkpoint (online-merged, +5 GB). End of session: reverted VM to `Clean-Windows-Ready` + powered Off → C: ~81 GB free, harness intact.

**Clean full install validated:** restored `Clean-Windows-Ready`, ran wizard via vmconnect → **18/18 verifier PASS**. Pond confirmed wizard footer layout, app auto-launch on Finish, and `admin`/`myAdmin@101` login end-to-end. **Stage 7 done.**

**Bug F — FIXED & DB-validated (`115777d`, systematic-debugging + TDD):** root cause was NOT "service ignores storeId" (handoff guess). Real cause: installer wrote `storeIdentity:storeId` but `StoreIdentityOptions` binds `Store:Id` → mismatch → `StoreIdentityService` fell back to `STORE-{MachineName}`. Fix: `DatabaseSetup.BuildStoreHubConfigJson` writes `Store:Id`; test now guards the binding. Proven by decrypting the DPAPI conn string in-guest + querying Postgres: seeded admin `store_id = Rungrat-001` (SystemAdmin, active).

**Service-start timeout — FIXED & re-validated (`4a1996a`):** the fresh-install run surfaced StoreHub left **Stopped** (Event 7009 / error 1053). Root cause: EF migrations ran before `app.Run()` → host couldn't signal "Running" within the 30s SCM start timeout on a fresh DB. Fix: added `migrate` CLI mode to StoreHub (`Program.cs` — migrate+seed then return before `app.Run()`); bootstrapper runs `IndyPOS.StoreHub.exe migrate` as a console step (`StoreHubInstaller.ProvisionDatabaseAsync`) after DB setup, before starting the service (`InstallationOrchestrator` step 4b, fatal on failure). Re-validated: **18/18, service Running, health 200, store_id still Rungrat-001**. +1 guard test (43/43 bootstrapper).

**Reusable techniques:** in-guest DB verification by decrypting DPAPI conn string (machine scope, entropy `SHA256("IndyPOS:"+key)`, marker `DPAPI:`; guest is **PS 5.1** → use `[SHA256]::Create().ComputeHash`, not static `HashData`). Snapshot restore skips autologon → **reboot guest** to trigger it. vmconnect blank = remembered multi-monitor layout (plug 2nd monitor / relaunch).

**State:** both fixes committed on `indypos-overhaul`; installer rebuilt **189.9 MB (16:09)**. **NEXT:** admin-provisioning A–D decision (changes installer → needs another clean-install validation; VM ready) → whole-branch review → finishing-a-development-branch.

---

## 2026-06-23: Stage 7 — app logs in end-to-end; wizard UI polish + 3 more prod bugs fixed ✅

**Focus:** Polish the installer wizard, run the Stage 7 VM smoke test, and drive the app to a working login.

**Wizard UI polish (committed):** moved Install to footer bottom-left, Cancel bottom-right (single primary action; Finish reuses Cancel's slot on success), removed the password hint, fixed lopsided Cancel margin via `ClientSize` (not `Size`). `428e480`, `6298be8`. Verified visually in the VM.

**VM smoke test:** 18/18 verifier PASS, filesystem all under `v4` (no `v1`), and the app **reached the login screen** → **Bugs A (version skew) & B (cash-drawer serial crash) proven fixed**.

**Then debugged login to a working end-to-end POS sign-in (systematic-debugging, evidence from VM logs/event log/API + StoreHub config):**
- **Bug D** — failed login crashed the app via re-entrant `ShowDialog` on the singleton `MessageForm`. Fix: disable login button during async login + `MessageForm.ShowDialog` early-returns when already `Visible`. `f363e71`. ✅ Graceful error now.
- **Bug E** — `BuildAppConfiguration` used `Directory.GetCurrentDirectory()`; crashed when launched with a non-install CWD (wizard Finish inherits `C:\Test`). Fix: `AppContext.BaseDirectory`. `0267354`. ✅ Proven launching from `C:\Windows`.
- **StoreHub port mismatch** — WinForms `appsettings.json` → `:5012` (Aspire dev) but installed StoreHub listens on `:5000`; every prod login failed as "wrong username/password". Fix: `appsettings.json` → `:5000`; AppHost injects `StoreHub__BaseUrl=:5012` for dev; env vars added last so dev override wins. `7f1ff02`. ✅ Login works (`admin`/`myAdmin@101`).
- **Bug C was not a bug** — `/auth/login` returns 200 for the seeded admin; mismatch was the port + a mistyped password.

**Efficiency note:** validated the app-layer fixes by **hot-swapping** the freshly-published WinForms binaries into the VM's Velopack `current\` folder (over PSDirect) instead of a 24-min reinstall — the VM already had a healthy StoreHub + Postgres + seeded admin. Fast loop for app-only changes.

**State:** all committed on `indypos-overhaul`; installer rebuilt **189.9 MB (00:50)**. **NEXT:** clean full VM install to validate wizard UI + Finish-button launch on a fresh install; then Bug F (seeded `store_id` = machine name, not wizard Store ID); then admin-provisioning decision.

---

## 2026-06-21 (evening): DPAPI Vault — brainstorm → spec → plan → executed Tasks 1-3 ✅

**Focus:** Realize the "at-rest secrets" decision as a real feature. Full superpowers flow: brainstorming → writing-plans → subagent-driven-development.

**Design decisions locked (with Pond):**
- New **`IndyPOS.Vault`** zero-dependency leaf project (not duplicated, not in Infrastructure) — Pond named it Vault. Stateless seal/unseal, NOT a secret store.
- DPAPI **machine scope** (installer encrypts as admin, StoreHub decrypts as LocalSystem), `DPAPI:` marker prefix, **per-key SHA256 entropy** (binds each blob to its config key — prevents field-swap), UTF-8, fail-fast on decrypt failure.
- **Decision 2a:** drop the redundant plaintext `storehub.key`; JWT key lives only in the DPAPI-protected `appsettings.json` (which now also gets the Administrators+LocalSystem ACL for defense-in-depth parity).
- Spec pressure-tested by 3 review subagents (architect/engineer/QA) — caught: repo is xUnit (not MSTest), extension must live in StoreHub not ServiceDefaults (TFM clash), config-precedence + UTF-8 pinning, existing `DpapiSecretStorage` prior art (CurrentUser/swallow — deliberately NOT reused).

**Executed (subagent-driven, fresh implementer+reviewer per task, all reviews clean):**
- Task 1 `1fc928a` — Vault + SecretProtector, 17/17 tests.
- Task 2 `23a6f4f` — StoreHub `UnprotectSecrets`, 7/7 tests.
- Task 3 `a7e85c9` — installer encrypt + drop `storehub.key` + ACL appsettings, 3 new + 42/8 suite.

**Stopped for the day after Task 3.** Tasks 4 (scripts/docs cleanup) + 5 (build + VM validation) remain. Ledger at `.superpowers/sdd/progress.md` is the durable resume map. Minor findings logged there for final-review triage. Prior-session installer work still uncommitted in the tree.

---

## 2026-06-21: Stage 7 — final VM run validated winget + Serilog fixes, surfaced 2 NEW prod bugs 🟡

**Focus:** Run the final VM smoke test to validate the two pending fixes (#3 winget auto-install, #6 version-aware paths). Reached **18/18 verifier PASS** again — but the human-eyeball checks the verifier can't see surfaced **2 more prod bugs**. Root-caused both via in-guest log/event-log forensics (systematic-debugging). **No fixes written yet — Pond chose to fix tomorrow.**

### Validated this run ✅
- **Fix #3 (winget silent auto-install)** — Pond let the wizard run it (did NOT hand-install). Confirmed `.NET 10` (`Microsoft.NETCore.App 10.0.9`) installed via winget. **Now VM-proven.**
- **Fix #5 (Serilog.AspNetCore removal)** — republished `runtimeconfig.json` shows only `Microsoft.NETCore.App` + `Microsoft.WindowsDesktop.App` (no AspNetCore). **No pop-up at Finish.** **Now VM-proven.**
- Verifier 18/18 (filesystem under `v4`, `/health/ready`=200, manifest `v4.0.0`, Postgres 18 running, `IndyPOS.StoreHub.v4` service Running, Velopack `current` + shortcut present).

### NEW bugs found — app crashes on launch (shortcut → no window)
**Evidence:** `v4\logs` EMPTY + zero IndyPOS WER events, but app's REAL log landed in `C:\ProgramData\IndyPOS\v1\logs\log20260621.json` (33 KB, 6 crash loops). Fatal in every one:
`System.IO.FileNotFoundException: Could not find file 'COM1'` → `SerialPort.Open()` → `CashDrawerService.InitializeSerialPort()` → `CashDrawerService..ctor` (thrown during DI resolution).

**Bug A — version skew defeats `InstallPaths` (regression hiding inside fix #6).**
- DLL versions in `current\`: `IndyPOS.Application.dll`=**1.0.0.0**, `IndyPOS.Infrastructure.dll`=**1.0.0.0**; `Windows.Forms`+`Domain`=4.0.0.
- `InstallPaths` lives in **Application.dll**, so `ResolveMajorVersion()` reads `1` → app uses `...\IndyPOS\v1\...` while installer wrote `v4\`. A `v1` tree was auto-created at 3:45 (default `StoreConfiguration.json` 405 B + logs); installer's `v4\Config\StoreConfiguration.json` (283 B) ignored.
- **Cause:** Application + Infrastructure csprojs set `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` AND keep legacy `Properties/AssemblyInfo.cs` hardcoding `1.0.0.0` → `Directory.Build.props` 4.0.0 never reaches them. (Domain has no override → correctly 4.0.0.) This is exactly the modernization follow-up STATUS.md flagged.

**Bug B — `CashDrawerService` opens the serial port eagerly in its ctor → crashes app when port absent.**
- `InitializeSerialPort()` calls `_serialPort.Open()` inside the constructor. No COM1 (VM, USB drawer, unplugged cable) → `FileNotFoundException` → DI fails → app dies before any window.
- **Why dev box masked it:** `InitializeSerialPort()` is marked `[Conditional("RELEASE")]` → compiled OUT in Debug, so devs never open the port. The installer is Release → it runs.

### Proposed fix plan (for tomorrow — NOT yet implemented)
**Bug A (root fix):** delete `Properties/AssemblyInfo.cs` from Application + Infrastructure (and WinForms for uniformity) and drop `<GenerateAssemblyInfo>false>` so `Directory.Build.props` drives every assembly to 4.0.0. AssemblyInfo files only hold legacy template attrs (`AssemblyTitle`, `ComVisible(false)`, COM `Guid`) — almost certainly unused; if any matter, re-express via csproj props. (Defensive alt/also: `InstallPaths.ResolveMajorVersion()` → `Assembly.GetEntryAssembly()` version with fallback.)
**Bug B:** wrap `_serialPort.Open()` in try/catch (log warning, continue — drawer unavailable is NOT fatal); **drop `[Conditional("RELEASE")]`** so Debug==Release; guard `OpenCashDrawer()` on `IsOpen`. Re-review `ReceiptPrinterService` for the same eager-ctor pattern (lower risk — it doesn't open hardware in ctor, but `GetStoreConfiguration` rethrows).
**Then:** rebuild installer, re-run VM (clean snapshot wipes the stale `v1`+`v4.0.0` trees), expect app to open to login. Then commit the whole Stage 7 bundle.

### State
- **All changes still UNCOMMITTED.** No new code written this session — investigation only.
- VM `IndyPOS-Test` left RUNNING with the broken install (fine; next run restores `Clean-Windows-Ready`). Can stop with `Stop-VM IndyPOS-Test -Force`.
- Build/test unchanged: WinForms + bootstrapper clean; `Application.Tests` 219/219 (integration suites need Docker).

---

## 2026-05-31 (~01:00 AM): Stage 7 — VM test hit 18/18, then surfaced + fixed 3 prod bugs 🟢

**Focus:** Resume the .NET-detection blocker, run the VM smoke test end-to-end, fix everything it surfaces. Big productive session — reached 18/18 verifier PASS and uncovered 3 real bugs the dev-box install had masked.

### Fixes landed (all UNCOMMITTED)
1. **`DotNetInstaller` detection rewritten** — `FindDotNetRoot()` (no stale PATH) → folder probe `shared\Microsoft.WindowsDesktop.App\10.*` → fallback absolute `dotnet.exe`. Dropped dead registry probe. **VM-validated.**
2. **VCRedist** Step 1b — **VM-validated** (Postgres `initdb` succeeded).
3. **winget silent auto-install** added to `DotNetInstaller` (`--exact --id Microsoft.DotNet.DesktopRuntime.10 --source winget --silent ...`, 5-min timeout, manual fallback extracted to `InstallManually()`). **Built, NOT yet VM-proven** — Pond hand-installed .NET both runs, confounding it.
4. **Verifier exe-name** — looked for `IndyPOS.exe`; real `--mainExe` is `IndyPOS.Windows.Forms.exe`. Fixed → **18/18 PASS**.
5. **`Serilog.AspNetCore` removed** from WinForms csproj — it pulled the `Microsoft.AspNetCore.App` FrameworkReference into a desktop app → "must install .NET / AspNetCore.App" launch pop-up. App only uses `.UseSerilog()` (from `Serilog.Extensions.Hosting`). **Proven** via republished `runtimeconfig.json`.
6. **Version-aware app paths (major root `v4`)** — the 3rd bug: app hardcoded legacy `...\IndyPOS\Config\StoreConfiguration.json`, installer writes versioned root → `DirectoryNotFoundException` at startup. Pond chose major-root scheme: new `IndyPOS.Application.Common.InstallPaths`, `InstallationConfig.SystemRoot`→major, `StoreConfigurationService`/`Program.cs` use it, `JsonService` creates dir before write, stale `v4.0.0`→`v4` in scripts.

### State
- WinForms + bootstrapper build clean. `IndyPOS.Application.Tests` 219/219 PASS. Migration/StoreHub integration suites fail ONLY because Docker isn't running (Testcontainers — environmental).
- Installer rebuilt **5/31 12:48 AM, 189.8 MB** with all fixes — ready for the final VM run.
- VM `IndyPOS-Test` left RUNNING (old `v4.0.0` install from this session's run; next run restores fresh + writes `v4`).
- Background pollers stopped.

### Next session
1. **One final VM run**: `.\scripts\vm-testing\Reset-AndInstall.ps1 -KeepRunning`. At .NET step **don't hand-install** — let the wizard winget it. Confirm: "via winget" log, no AspNetCore pop-up, **desktop shortcut opens app to login** (config now at `v4\Config`), 18/18.
2. Then **commit the bundle** in logical groups (see STATUS.md TODO).

### New Hyper-V gotchas (memory items 11-12)
- vmconnect/Hyper-V Manager need Hyper-V Administrators group membership (`Add-LocalGroupMember ... + relog`); workaround = launch from privileged orchestrator context.
- "Use all my monitors" spans only in full-screen → `Ctrl+Alt+Break` to single window.

---

## 2026-05-30 (cont.): Stage 7 — VCRedist wired, snapshot re-baked, .NET detection bug found 🟡

**Focus:** Resume from the morning's first attempt — wire `VCRedistInstaller`, rebuild, re-bake the snapshot with the harness fixes, re-run. Got all the way to the wizard's .NET step before a new bug stopped the run.

### Done (all UNCOMMITTED — review + commit next session)
1. **Wired `VCRedistInstaller` into `InstallationOrchestrator`** — new "Step 1b" between .NET check and Postgres, progress band 5-10%, throws `InstallationException` on failure. Builds clean (0 warnings).
2. **Removed dead `DotNetDownloadUrl` constant** from `DotNetInstaller.cs` (the malformed `dotnet-runtime-10.0-win-x64.exe` one) — it was never referenced; real code uses `DotNetDownloadPageUrl`. Replaced with an honest comment.
3. **Rebuilt installer** via `installer\build-installer.ps1` — only the bootstrapper changed (payloads in `publish\Releases\` from 5/27 still current, source predates them). `publish\IndyPOS-Setup.exe` = 189.2 MB, 5/30 11:03.
4. **Re-baked the snapshot** (Pond chose "bake both fixes" over a script fix-up phase). New canonical `Clean-Windows-Ready` now contains: vmicvmsession Automatic+Running + static DNS 8.8.8.8/1.1.1.1 + **Guest Service Interface (host + guest)** + **autologon/no-lock**. Tree: `Clean-Windows` → `Clean-Windows-Ready-OLD` (partial) → `Clean-Windows-Ready` (full). `VMTestConfig.psd1` points at it.
5. **Hardened `Reset-AndInstall.ps1`** — asserts `Enable-VMIntegrationService 'Guest Service Interface'` after restore (idempotent, host-side).

### New gaps surfaced
- **Guest Service Interface disabled by default** → `Copy-VMFile` failed `0x80070015` on the first re-run. Fixed (host toggle + guest `vmicguestinterface` Automatic). See gotchas memory item 9.
- **`DotNetInstaller` detection is broken on a clean machine** (the blocker we stopped on). .NET 10 Desktop Runtime 10.0.8 was correctly installed via winget, but the wizard couldn't see it because BOTH methods fail: (1) registry key `...\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App` doesn't exist even on a legit install (wrong/stale path), (2) `dotnet --list-runtimes` relies on inherited PATH, which is stale because the wizard process started before winget updated PATH. **Fix:** detect via absolute `C:\Program Files\dotnet\dotnet.exe --list-runtimes` OR probe `shared\Microsoft.WindowsDesktop.App\10.*` folder — never inherited PATH or that registry key. Folds into the silent-.NET-install follow-up.
- **Autologon only helps on cold boot, not Standard-checkpoint resume** — vmconnect reconnect re-locks the session, so Pond still types the password on restore. Known limitation, see gotchas memory item 10.

### VM state at session close
- VM `IndyPOS-Test` left RUNNING, restored from `Clean-Windows-Ready`, wizard open mid-.NET-step. Partial state — discard on next run (Reset-AndInstall restores fresh anyway).
- Background pollers stopped cleanly.

### Next session (priority order)
1. **Unblock the in-progress wizard OR just re-run clean.** Cleanest: `.\scripts\vm-testing\Reset-AndInstall.ps1 -KeepRunning`, then in the VM relaunch the installer from a FRESH admin PowerShell (so `dotnet` is on PATH) after `winget install Microsoft.DotNet.DesktopRuntime.10`. Watch VC++ → Postgres `initdb` (should clear `-1073741515` now).
2. **Fix `DotNetInstaller` detection** (absolute dotnet path / folder probe) so the .NET step stops being a manual papercut.
3. Once green end-to-end: **commit** the bundle (VCRedist + orchestrator + DotNet cleanup + VM scaffolding + script hardening + docs).
4. Stretch: silent `--store-id`/`--app-password` bootstrapper mode (kills the manual wizard entirely) + silent .NET auto-install.

---

## 2026-05-30: Stage 7 — first VM smoke test 🟡 (bugs caught, retry pending)

**Focus:** Run `Reset-AndInstall.ps1` end-to-end against `Clean-Windows` snapshot. Surfaced 4 fresh gaps; one bootstrapper fix half-written, none committed.

### Phase progression
1. ✅ Preflight (installer + verifier + VM + snapshot all detected)
2. ✅ Snapshot restored, VM started
3. ⚠️ PSDirect probe **timed out (300s)** — root cause: `vmicvmsession` Stopped on the clean snapshot. Stage 6 only validated WinRM (`Enable-PSRemoting`), never VMBus (`Invoke-Command -VMName`). Manually started + set Automatic inside guest.
4. ✅ Installer copied to `C:\Test\IndyPOS-Setup.exe` (after Guest Service Interface enabled on VM via `Enable-VMIntegrationService`)
5. ✅ Wizard launched via vmconnect
6. ⚠️ Wizard hit two prerequisite failures on clean Win11:
   - **(a) .NET 10 download failed** — Default Switch's DNS forwarder is broken on this host; guest had connectivity (raw IP) but couldn't resolve names. Worked around in-guest by setting DNS to `8.8.8.8`,`1.1.1.1`. Also, bootstrapper's `DotNetInstaller.cs:13` URL (`dotnet-runtime-10.0-win-x64.exe`) is malformed — no patch version — would 404 even with DNS. Manually installed `Microsoft.DotNet.DesktopRuntime.10` via winget to unblock.
   - **(b) Postgres initdb failed with exit `-1073741515` (STATUS_DLL_NOT_FOUND)** — Postgres 18 binaries need VC++ Redistributable 2015-2022 x64 which isn't on clean Win11. EDB installer is invoked with `--install_runtimes 0` (`PostgresInstaller.cs:204`) — explicit "assume VC++ is present", which doesn't hold on a clean machine.

### Code written this session (uncommitted, not wired)
- `installer/IndyPOS.Bootstrapper/Installers/VCRedistInstaller.cs` — new. Detects VC++ 2015-2022 (x64) via `HKLM\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64`, downloads from `https://aka.ms/vs/17/release/vc_redist.x64.exe`, silent-installs with `/install /quiet /norestart`, tolerates exit codes 0/1638/3010. Uses same `%LOCALAPPDATA%\IndyPOS.Bootstrapper\cache\` dir as `PostgresInstaller`.

### Still TODO before next Stage 7 run
1. **Wire `VCRedistInstaller` into `InstallationOrchestrator.InstallAsync`** — call it as a new step between the .NET check (current step 1) and Postgres install (current step 2). Pattern: identical to `_postgresInstaller.EnsureInstalledAsync` (progress reporter → log + step). Use a small progress band (e.g. 8-10%) since the install is fast (~30s).
2. **Fix `DotNetInstaller.cs:13`** — malformed URL `dotnet-runtime-10.0-win-x64.exe`. Either remove the broken constant (the actual code uses `DotNetDownloadPageUrl` and prompts user) or replace with a working aka.ms permalink if MS publishes one. Worth a 30-min scan of MS docs.
3. **Rebuild installer:** `.\scripts\build-installer.ps1` (or `.\scripts\publish.ps1` — check which is current). Verify Setup.exe gets the new VC++ step in the orchestrator.
4. **Re-take `Clean-Windows` snapshot** (decision needed). Current snapshot is "vanilla Win11" — every restore loses the vmicvmsession + DNS fixes. Two options:
   - (a) Bake the fixes into the snapshot: log into VM, run `Set-Service vmicvmsession -StartupType Automatic; Start-Service vmicvmsession; Set-DnsClientServerAddress -InterfaceAlias 'Ethernet' -ServerAddresses '8.8.8.8','1.1.1.1'`, uninstall the leftover Postgres + .NET 10 manually-installed bits, then `Checkpoint-VM -SnapshotName 'Clean-Windows'` overwriting the existing one. Cleanest, but the snapshot is now "workable clean" not "vanilla clean".
   - (b) Leave snapshot vanilla, add a fix-up phase to `Reset-AndInstall.ps1` that runs after snapshot restore but before Phase 3. More honest test of bootstrapper UX (what a real customer hits) but slower per cycle.
   - **Recommendation:** (b) for honesty — but flip it once we want fast iteration on Stage 7+.
5. **Then re-run:** `.\scripts\vm-testing\Reset-AndInstall.ps1` (no flags). Expect to clear all phases this time.

### VM state at session close
- VM `IndyPOS-Test` left running (per orchestrator default), at desktop logged in as `IndyPOSAdmin`
- Partial Postgres 18 install in `C:\Program Files\PostgreSQL\18\` (binaries unpacked but no data cluster — initdb failed)
- .NET 10 Desktop Runtime 10.0.8 manually installed
- `vmicvmsession` Running + Automatic (in-guest fix, NOT in snapshot)
- DNS set to 8.8.8.8/1.1.1.1 on Ethernet interface (in-guest fix, NOT in snapshot)
- Guest Service Interface enabled on VM (host-side; this DOES persist across snapshot restore — set via Hyper-V manager, not guest)
- VM admin cred re-cached at `%LOCALAPPDATA%\IndyPOS\vm-test-cred.xml` with the *working* password (90 bytes — earlier 82-byte file was a fat-fingered/null cred from harness session, fixed by exporting from interactive pwsh)

### Memorialized
- 2 new gotchas added to `memory/reference_win11_hyperv_vm_gotchas.md` (items 7 + 8): `vmicvmsession` Stopped + Default Switch DNS flake. Both now bring the file to 8 gotchas total from the IndyPOS test VM workstream.

### Bug tally so far on the Stage 3+7 cycle
- Stage 3 (dev box): 7 prod bugs caught + fixed
- Stage 7 (clean VM, this session): **3 more** — DotNet URL malformed, Postgres `--install_runtimes 0` assumption, no VC++ prereq check. Stage 7 is doing exactly what it's supposed to.

---

## 2026-05-27 EOD → 2026-05-28: Installer Side-by-Side — Stages 5 + 6 ✅

**Focus:** Build VM smoke-test scaffolding (Stage 5) and the actual Hyper-V VM it targets (Stage 6).

### Stage 5 — VM scripts landed (uncommitted)

`scripts/vm-testing/` — 4 new files, semi-automated because the bootstrapper has no silent mode:

- `VMTestConfig.psd1` — shared config (VM name, paths, timeouts, credential cache location)
- `Test-IndyPOSInstallation.ps1` — in-VM verifier via PowerShell Direct; subset of `verify-install.ps1` (drops v3.7.0 side-by-side checks since VM is fresh); returns structured PSCustomObject
- `Reset-AndInstall.ps1` — 8-phase host orchestrator (preflight → restore snapshot → start VM → wait PSDirect → copy installer → vmconnect handoff → poll manifest → run verifier → report)
- `README.md` — one-time VM creation block + usage examples + troubleshooting

Cred storage = DPAPI-encrypted SecureString at `%LOCALAPPDATA%\IndyPOS\vm-test-cred.xml`. `-RecreateCredential` to rotate.

**Silent-mode follow-up filed:** add `--silent --store-id N --app-password X` to `IndyPOS.Bootstrapper/Program.cs` (~2-4h, `InstallationOrchestrator` already drives everything headlessly internally). Unblocks CI + customer-support unattended installs. Out of scope for v4.0.0.

### Stage 6 — Hyper-V test VM live

- `IndyPOS-Test` Gen2 VM at `C:\personal\VMs\IndyPOS-Test.vhdx` (60 GB dynamic, 4 vCPU, 4 GB startup / dynamic 2-8 GB, TPM + Secure Boot `MicrosoftWindows`)
- Win11 Pro 25H2 from `C:\personal\ISOs\Win11_25H2_English_x64_v2.iso` — detached after install, HDD boot-first
- Guest local user `IndyPOSAdmin`, network profile flipped Public→Private, PSRemoting enabled, TrustedHosts `*`, Tamper Protection off
- `Clean-Windows` Standard checkpoint captured 2026-05-28 00:31

**4 Win11/Hyper-V gotchas hit during VM build** (memorialized in `memory/reference_win11_hyperv_vm_gotchas.md`):
1. Gen2 UEFI "press any key" prompt missed because vmconnect didn't have keyboard focus
2. Win11 25H2 OOBE refused to skip the MSA flow; `Shift+F10` → `start ms-cxh:localonly` was the working bypass
3. Default Switch put the guest network in Public profile → `Enable-PSRemoting` failed on the firewall step until `Set-NetConnectionProfile -NetworkCategory Private`
4. Tamper Protection silently no-ops `Set-MpPreference -DisableRealtimeMonitoring`; only the Windows Security GUI toggle works

Path overrides used: `C:\personal\VMs\` + `C:\personal\ISOs\` instead of the README's default `C:\VMs\` + `C:\ISOs\` (Pond's preference; README updated to reflect this).

### Plan + STATUS docs updated

- `.planning/indypos-overhaul/drafts/installer-side-by-side-plan.md` — Stage 5 + 6 sections added; stage table reflects current state
- `.claude/STATUS.md` — Stage 7 boot instructions front-and-centre
- Global `~/.claude/STATUS.md` — IndyPOS row points at Stage 7

### Stage 7 — ready for next session

One command:
```powershell
cd C:\personal\IndyPOS
.\scripts\vm-testing\Reset-AndInstall.ps1
```

First run pops `Get-Credential` for `IndyPOSAdmin`. Click through wizard inside VM (Store ID + App Password ×2 + Start). Script polls for `install-manifest.json` and prints PASS/FAIL report.

**Commits:** none yet — deliberately holding the Stage 5 scaffolding + plan/STATUS edits until Stage 7 validates the orchestrator end-to-end. Plan is one bundled "Stage 5+7 — VM smoke-test scaffolding + first-run fixes" commit once green.

---

## 2026-05-02: Installer Side-by-Side — Stage 1 Refactor ✅

**Focus:** Make `InstallationConfig` the version-aware source-of-truth so v4 paths/IDs/ports come from one place. All install surfaces refactored to read from config; legacy const paths gone.

### Design calls (locked)
- **A) `InstallVersion` source:** derive from bootstrapper assembly (`<Version>4.0.0</Version>` in csproj). No drift between assembly and computed paths.
- **B) StoreHubInstaller helpers:** convert from `private static` → instance methods reading captured `_config` field, set in `InstallAsync`. Same pattern applied to DatabaseSetup + VelopackLauncher (helpers that touch paths/IDs).

### Changes
- `IndyPOS.Bootstrapper.csproj` → `<Version>4.0.0</Version>`
- `InstallationConfig.cs` → `InstallVersion` (from assembly) + 9 computed properties: `SystemRoot`, `ConfigDirectory`, `KeysDirectory`, `LogsDirectory`, `BackupsDirectory`, `StoreHubInstallPath`, `ServiceName`, `ServiceDisplayName`, `VelopackAppId`, `HealthCheckPort`
- `StoreHubInstaller.cs` → 4 consts dropped, helpers instance, captured `Config`
- `DatabaseSetup.cs` → 5 directory consts dropped, instance helpers, `appsettings.Production.json` template now pins `Urls: http://localhost:5000`
- `VelopackLauncher.cs` → takes config, finds `IndyPOS.POS.v4-Setup.exe`, install path uses `VelopackAppId`
- `InstallationOrchestrator.cs` → passes config into launcher, health check uses `config.HealthCheckPort`
- `publish.ps1` → `--packId "IndyPOS.POS.v4"`

### Result
- `dotnet build` clean: 0 warnings, 0 errors
- v4 install footprint fully isolated under `C:\ProgramData\IndyPOS\v4.0.0\` + `%LOCALAPPDATA%\IndyPOS.POS.v4\`
- Service registers as `IndyPOS.StoreHub.v4` — no collision with v3.7.0

### Next session — Stage 2: Build Pipeline
1. Install `vpk` CLI: `dotnet tool install -g vpk`
2. Run `scripts/publish.ps1` to produce `IndyPOS.POS.v4-Setup.exe` + `StoreHub-{version}.zip`
3. Copy/embed into `installer/IndyPOS.Bootstrapper/Resources/` so `GetManifestResourceStream` finds them
4. Verify resource names match what `StoreHubInstaller`/`VelopackLauncher` look up

---

## 2026-04-28: Installer Side-by-Side Planning 📋

**Focus:** Plan Velopack installer testing, with v3.7.0 ↔ v4.0.0 side-by-side coexistence requirement.

### Decisions Locked-in
- `InstallVersion = "4.0.0"` from bootstrapper assembly version (single source of truth)
- System-shared root: `C:\ProgramData\IndyPOS\v4.0.0\` (everything except Velopack)
- Velopack stays at `%LOCALAPPDATA%\IndyPOS.POS.v4\current` (per-user, supports auto-update)
- Service name: `IndyPOS.StoreHub.v4`
- Velopack app ID: `IndyPOS.POS.v4`
- Health check port: `5000` (fix orchestrator/WinForms inconsistency)
- DB name `indypos_storehub` (no version suffix — 3.7.0 uses SQLite, no collision)

### Stage 0 Discovery — Complete ✅
- All v4 paths free, no service/port conflicts
- 3.7.0 footprint: `C:\ProgramData\IndyPOS\{Config,db,Logs}` — DON'T TOUCH ZONE
- No Postgres on dev box → smoke test will trigger fresh Postgres 18 install (~300MB)
- Hyper-V enabled, vpk not yet installed

### Files Affected by Stage 1 Refactor
- `installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs`
- `installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs`
- `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs`
- `installer/IndyPOS.Bootstrapper/Installers/VelopackLauncher.cs`
- `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` (port fix)
- `scripts/publish.ps1` (`--packId IndyPOS.POS.v4`)

### Artifacts Created
- `.planning/indypos-overhaul/drafts/installer-side-by-side-plan.md` — full plan with stages 0–7
- 8 tasks tracked (Stage 0 complete, Stages 1–7 pending)

### Open Decision (next session)
Smoke test approach: **A)** let installer run real Postgres 18 install (full path, heavy cleanup) vs **B)** pre-install Postgres on dev box (faster iteration, skip Postgres-install path testing). Leaning A — VM will catch any gaps anyway.

### Next Up
Stage 1: refactor `InstallationConfig` for version-aware paths and propagate through 4 installers.

---

## 2026-04-27: Aspire Local Testing Complete ✅

**Focus:** Fix all blockers for running IndyPOS locally with Aspire

### Summary
Fixed all issues preventing team from testing IndyPOS with Aspire. Login, products, reports, and sales all working. Added comprehensive logging for debugging.

### Fixes Applied

| Area | Issue | Fix |
|------|-------|-----|
| global.json | SDK version 7.0.0 invalid | Changed to 10.0.107 |
| AppHost | PostgreSQL containers not persisting | Added WithDataVolume + WithLifetime |
| WinForms | Wrong StoreHub port (5000) | Changed to 5012 |
| CloudApi | LocalToken:SecretKey config error | Moved LocalTokenOptions to Application with defaults |
| Login | Password encrypted before sending | Removed encryption (API expects plaintext) |
| Products | Not showing after login | Fixed StoreId in seeder, singleton HttpClient |
| Reports | 403 errors not handled | Added ReportErrorHandler for graceful handling |
| Tests | Flaky SyncWorkerTests | Used TaskCompletionSource pattern |
| Tests | Handler tests missing logger | Added NullLogger |
| Tests | ReportsEndpointTests timezone | Use DateTime.Now not UtcNow |
| Security | OpenTelemetry CVE-2026-40894 | Updated to 1.15.x |
| Aspire | WinForms not in dashboard | Added with WithExplicitStart() |
| Aspire | dbgate redundant | Removed (pgAdmin sufficient) |
| Reports | Date picker not initialized | Added DateTime.Today initialization |
| Reports | Testing from Canada shows wrong dates | Added store timezone support (defaults to Thailand) |

### Timezone Support
- Added `TimeZone` property to `IStoreIdentityService`
- Added `TimeZoneId` config option to `StoreIdentityOptions` (default: "SE Asia Standard Time")
- Updated all 5 report handlers to pass store timezone to `ReportDateRange.ToUtcRange()`
- Reports now correctly use Thai local time regardless of where app is running

### Logging Added
- CompleteSaleCommandHandler
- IngestEventsCommandHandler
- All 7 report query handlers

### Test Results
- **306 tests passing** (219 + 15 + 23 + 49)
- All integration tests green ✅

### Commits (12 total)
1. `fix(aspire): correct SDK version, postgres persistence, and StoreHub URL`
2. `fix(auth): move LocalTokenOptions to Application layer and remove password encryption`
3. `feat(logging): add debug logging to command and query handlers`
4. `fix(ui): improve reports, inventory, and error handling`
5. `test: fix tests for logging changes and flaky behavior`
6. `feat(aspire): add WinForms app to AppHost with explicit start`
7. `fix(security): update OpenTelemetry packages for CVE-2026-40894`
8. `chore(aspire): remove dbgate, pgAdmin is sufficient`
9. `feat(reports): add timezone support for store-specific date calculations`

### Test Accounts
| Username | Password | Role |
|----------|----------|------|
| admin | admin123 | SystemAdmin |
| manager | manager123 | StoreManager |
| cashier | cashier123 | Cashier |

---

## 2026-04-05: Epic M Progress (M1-M6) + VM Testing Plan

**Epic:** M | **Tasks:** M1-M6 complete | **Commits:** 9

### Summary
Continued Epic M (Multi-Store Type Support). Completed M1-M6 (core domain work). Created VM testing documentation and automation plan.

### Key Decision
**StoreId Strategy:** Root entities only (Product, StoreSetting). Child entities (Payment, InvoiceLine, PayLater) inherit via JOIN to Invoice.

### Work Done

**M3: StoreId on Entities**
- Added StoreId to Product and StoreSetting entities
- Updated repositories with IStoreIdentityService injection
- Created composite unique index (StoreId, Barcode) for Product
- Created EF Core migration: `AddStoreIdToProductAndStoreSetting`

**M4: Repository Updates**
- PayLaterRepository now filters by StoreId via Invoice JOIN
- Maintains store isolation without adding StoreId to child entities

**M5-M6: Feature Validation**
- CompleteSaleCommand blocks PayLater for non-GeneralHardware stores
- GetPayLaterQuery/GetPayLaterByIdQuery validate `Features.PayLaterEnabled`
- RecordPayLaterPaymentCommand validates before processing

**Testing**
- Created `MockStoreIdentityService` in IndyPOS.Mock project
- Updated all PayLater tests to use mock
- Fixed namespace conflicts (IndyPOS.Mock vs Moq.Mock)
- All 301 tests passing ✅

**Documentation**
- Created `docs/development/vm-testing-guide.md` - Hyper-V setup guide
- Created `.planning/indypos-overhaul/drafts/vm-installer-testing-plan.md`
  - Phase 1 (Quick Win): Script-driven with pre-installed Windows
  - Phase 2 (Full Auto): Unattended Windows install + complete pipeline

### Commits
1. `feat(domain): add StoreType enum and StoreTypeFeatures value object`
2. `feat(infrastructure): add StoreId to Product and StoreSetting entities`
3. `feat(infrastructure): filter PayLater by StoreId via Invoice JOIN`
4. `feat(application): add store type feature validation for PayLater`
5. `test: add MockStoreIdentityService and update tests for StoreId`
6. `docs: add VM testing guide and installer testing plan`
7. `feat(installer): add Velopack bootstrapper and publish script`
8. `feat(winforms): add first-run setup wizard foundation`
9. `docs: update store installation guide and plan`

### Files Created
| File | Purpose |
|------|---------|
| `tests/IndyPOS.Mock/MockStoreIdentityService.cs` | Test mock for store types |
| `docs/development/vm-testing-guide.md` | Hyper-V testing guide |
| `.planning/.../vm-installer-testing-plan.md` | VM automation plan |

### Next Session
- Implement Phase 1 VM testing scripts (Quick Win)
- Continue Epic M (M7-M13): UI, installer, CloudApi updates
- Test Epic V installer in Hyper-V VM

---

## 2026-04-05: Epic M Started - Multi-Store Type Support

**Epic:** M | **Tasks:** M1-M2 complete

### Summary
Finalized Epic M decisions and began implementation. Completed M1 (Domain types) and M2 (config schemas).

### Decisions Finalized
| Question | Answer |
|----------|--------|
| StoreHub Location | Local per store (most stores = 1 POS machine) |
| Offline Support | Local PostgreSQL required (offline-first POS) |
| StoreId Generation | Manual UUID by System Admin (avoids ID mismatch) |
| Migration | 1 existing hardware store → migrate to `generalHardware` DB |

Architecture unchanged - still Local PG → SyncWorker → CloudApi → Central PG.

### Work Done

**M1: Domain types**
- Created `src/IndyPOS.Domain/Enums/StoreType.cs` (GeneralHardware, Minimart, CoffeeShop)
- Created `src/IndyPOS.Domain/ValueObjects/StoreTypeFeatures.cs` (PayLaterEnabled, MultipleProductTypesEnabled)

**M2: Config schemas**
- Updated `StoreIdentityOptions` - added `Type` property
- Updated `IStoreIdentityService` - added `StoreType` and `Features` properties
- Updated `StoreIdentityService` - implements new interface members
- Marked `Code` as obsolete (use UUID StoreId instead)

### Files Created/Modified
| File | Change |
|------|--------|
| `Domain/Enums/StoreType.cs` | Created |
| `Domain/ValueObjects/StoreTypeFeatures.cs` | Created |
| `Application/Common/Models/StoreIdentityOptions.cs` | Added Type |
| `Application/Common/Interfaces/IStoreIdentityService.cs` | Added StoreType, Features |
| `Infrastructure/Services/StoreIdentityService.cs` | Implemented new members |
| `.planning/indypos-overhaul/drafts/epic-m-multi-store-type.md` | Updated with decisions |

### Open Decision for Next Session
**M3: StoreId on entities** - Should we add StoreId to ALL entities or only root entities?
- Currently 4 have it: Invoice, StoreUser, OutboxEvent, InventoryMovement
- Missing 5: Product, Payment, PayLater, InvoiceLine, StoreSetting
- Child entities (Payment, InvoiceLine, PayLater) could inherit via JOIN to Invoice

### Build Status
✅ 0 errors, 62 warnings (including expected obsolete warnings)

---

## 2026-04-03: Epic L Complete - Local Deployment Readiness

**Epic:** L | **Commits:** `aaea941..9d810ad` (11 commits)

### Summary
Completed Epic L (Local Deployment Readiness) - system is now **ready for pilot deployment**! 🚀

### Work Done

**L1-L6 Core Tasks:**
- L1: WinForms appsettings.json + removed legacy `Enabled` flag (StoreHub is now default)
- L2: StoreHub appsettings.Production.json + README documentation
- L3: `publish.ps1` - builds self-contained releases
- L4: `install-config.ps1` - automates PostgreSQL setup
- L5: `smoke-test.ps1` - comprehensive E2E test (health, auth, products, sales, pay later)
- L6: `store-installation-guide.md` - complete deployment guide

**Velopack Prep (Bonus):**
- `Directory.Build.props` - centralized version (1.0.0)
- `AppVersion.cs` - version helper class
- `/version` endpoint in StoreHub
- `docs/versioning.md` + Bruno request

**Boy Scout Cleanup:**
- Removed outdated `Enabled` flag references from 3 docs
- Renamed `setup-local.md` → `store-installation-guide.md`
- Updated PostgreSQL 16 → 18 across all docs/scripts (10 files)

### Key Files Created
| File | Purpose |
|------|---------|
| `scripts/publish.ps1` | Build release binaries |
| `scripts/install-config.ps1` | PostgreSQL + config setup |
| `scripts/smoke-test.ps1` | E2E API tests |
| `docs/operations/store-installation-guide.md` | Deployment guide |
| `docs/versioning.md` | Version management docs |

### Next Steps
1. Run `publish.ps1` to build release binaries
2. Deploy to pilot store
3. Run `smoke-test.ps1` to verify
4. Monitor and gather feedback

---

## 2026-04-01: Epic G3 SQLite Removal Complete

**Epic:** G3 | **Commit:** `02c35fc`

### Summary
Completed full SQLite removal from main IndyPOS application. WinForms now **requires** StoreHub mode.

### Work Done
- Deleted 9 SQLite repository implementations
- Deleted 8 Pos repository interfaces
- Deleted ~70 legacy Nokpirab handlers
- Deleted legacy services (SaleService, UserLogInService, ReportService, StoreConstants)
- Updated WinForms (MainForm, UserLogInPanel, UsersPanel, AddNewUserForm)
- Removed `System.Data.SQLite.Core` and `Dapper` packages
- Created `LegacyDtos.cs` for backward compatibility

### Metrics
- 174 files changed, ~90 deleted
- 5,699 lines deleted, 236 added
- 298 tests passing

### Technical Debt
`StoreHubReportService` has stub implementations for int-based methods → address during MAUI migration.

---

## 2026-03-31: Epic G3 Phases 0a-0b

**Epic:** G3 (SQLite Removal prep)

### Summary
Created foundation for SQLite removal - HardcodedStoreConstants and StoreHubReportService.

### Work Done
- **Phase 0a:** Created `HardcodedStoreConstants.cs` using enums instead of SQLite lookups
- **Phase 0b:** Created `StoreHubReportService.cs` implementing `IReportService`
- Added legacy report endpoints to StoreHub (`/reports/legacy/sales-summary`, `/reports/legacy/payments-summary`)
- Updated `IStoreHubClient` and `StoreHubHttpClient` with report methods

---

## 2026-03-29: Epic H Complete + Solution Reorganization

**Epic:** H (Testing & Rollout)

### Summary
Completed Epic H with integration tests, migration tests, and operational docs. Reorganized solution folders.

### Work Done
- Created `IndyPOS.StoreHub.IntegrationTests` (49 tests)
- Created `IndyPOS.Migration.Tests` (15 tests)
- Created `IndyPOS.MigrationTool` console app
- Fixed NuGet vulnerabilities (Azure.Identity, KubernetesClient)
- Organized 14 projects into logical solution folders
- Created operational docs (pilot-checklist, smoke-test, rollback-plan, RUNBOOK)

---

## 2026-03-28: Epic G3 Phases 4-7

**Epic:** G3 (SQLite Removal)

### Summary
Implemented IInventoryProductService and migrated WinForms to use StoreHub.

### Work Done
- Created `IInventoryProductService` interface
- Implemented `StoreHubInventoryProductService`
- Updated 4 WinForms inventory forms
- Created initial EF Core migration for StoreHub tables
- Added 10 unit tests for StoreHubInventoryProductService

---

## 2026-03-27: Epic S5 + G1 + Report API

**Epic:** S5 (Security) + G1 (Desktop Integration)

### Summary
Implemented RSA signing, DPAPI secrets, StoreHub client integration, and Report API.

### Work Done
- **S5a:** RSA 2048+ signing for CloudApi JWT tokens
- **S5b:** DPAPI secret storage for StoreHub ClientSecret
- **G1:** StoreHub client integration (HTTP client, product cache, sale/login services)
- Simplified UserId from int to Guid
- Created Report API endpoints (5 endpoints)
- Added E2E tests with WireMock

---

*For older sessions, see `session-log-archive.md`*

---

## 2026-07-25 — Cosmetic minors shipped; installer found to be fresh-install-only

**Merged: PR #52** (cosmetic-minors cleanup batch, 11 commits). Closed every deferred cosmetic Minor
from Epic M + the product-type restriction. Pond re-specced the payment-method `Kind` taxonomy
mid-batch: `Standard = 1` (rename of `Permanent`, same backing value) / `GovernmentCampaign = 2` /
`Special = 3`, with PayLater -> Special and WelfareCard -> GovernmentCampaign, plus a WHERE-guarded
reversible data migration. Also Thai kind labels, grid refresh on toggle, payment-button icons
restored, `PUT /products/{id}` -> 404 via the existing `ProductNotFoundException`, warn-once
features dialog.

Whole-branch review (subagent) found 1 Important: the migration's row-flip branch had **zero**
coverage, because `IntegrationTestBase` provisions with `EnsureCreatedAsync` - so no migration in
this repo has ever been exercised by any test, and a fresh install runs migrations against an empty
table. Closed with `ReclassifyPaymentMethodKindsMigrationTests`, which drives `IMigrator` to the
prior revision on its own Postgres container, plants pre-change rows, migrates, and asserts the
transition (with a before-snapshot so it proves the flip rather than agreeing with the end state).
The reviewer also empirically disproved a suspected shared-`Image` disposal bug by building a probe.

**Merged: PR #53** (installer + tooling fixes). Found by running the upgrade smoke against a real
store image. Extraction ran before the service stop -> locked DLLs; and worse, it destroyed the
store's `appsettings.json` (extraction writes the package template, `DatabaseSetup` only rewrites
real values afterwards). Fixed both, with `ConfigSnapshot` covering the exception path. Plus three
latent `cleanup-v4.ps1` bugs: a safety guard still demanding `v<Major>.<Minor>.<Patch>` after the May
move to a major-only root, em-dashes making it unparseable under the guest's Windows-1252 codepage,
and the DPAPI-sealed connection string silently skipping the database drop.

**Key finding: the installer is fresh-install-only by design.** `DatabaseSetup` refuses when
PostgreSQL already exists with an unknown superuser password. So there is no automated upgrade path
to the 3 stores and never was - every validation to date is clean-install. Not urgent: all 3 stores
are still on v3.7.0.

Brainstormed -> spec'd upgrade support (separate `UpgradeOrchestrator`, 3-outcome mode detector,
backup + file rollback, superuser password deliberately NOT persisted). Two subagent reviews:
correctness returned "not safe as written" with 4 Criticals (all folded in - `Store:Type` loss
silently making a Minimart permissive, a missing `Store:Id` forking store identity, world-readable
backup artifacts, and a v5-over-v4 install routing the operator to a command that drops the sales
database); scope returned "trim and re-sequence". They disagreed on the wizard; reconciled as
detect-and-refuse. Spec on `spec/installer-upgrade-support`, awaiting Pond's review.

**VM baseline rebuilt without the deleted ISO** by running the project's own teardown in-guest, then
removing the leftover Postgres data dir and installer-added fonts (restoring coverage the old
snapshot had lost). Two snapshots now, both restorable. Fresh-install smoke 18/18 in 3m09s, which
also served as the regression proof for PR #53.

**Verification at wrap-up:** Release build 0 err; Domain 8/8, Application 274/274, Bootstrapper 101
pass/8 skip, StoreHub integration 76/76 (real Postgres) - all re-run on merged `development`.
