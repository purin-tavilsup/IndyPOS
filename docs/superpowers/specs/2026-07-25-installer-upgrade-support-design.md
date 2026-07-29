# In-Place Upgrade Support for the IndyPOS Installer — Design

**Status:** 🟢 Ready for planning — no open questions
**Created:** 2026-07-25
**Revised:** 2026-07-25 after two independent spec reviews (correctness; scope/buildability)
**Revised:** 2026-07-26 — §12 Velopack spike run on the VM; step 8 confirmed, no design change
**Trigger:** VM upgrade smoke test on `IndyPOS-Test` (two failed runs, both diagnosed)

---

## 1. Problem

`IndyPOS-Setup.exe` is a **fresh-install-only** tool. Running it over an existing install fails, and fails destructively. Both failure modes were reproduced on a real store image (VM `IndyPOS-Test`, store `Rungrat-001`, StoreHub 4.0.0 running):

**Failure 1 — locked binaries.** `StoreHubInstaller.InstallAsync` extracted the payload *before* stopping the service, so the running service held its own DLLs open:

```
ERROR: StoreHub installation failed: The process cannot access the file
'...\v4\StoreHub\Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.dll'
because it is being used by another process.
```

**Failure 2 — no upgrade concept.** With the order corrected, the run reached `DatabaseSetup` and stopped:

```
ERROR: Database setup failed: PostgreSQL 18 is already installed, but its
superuser password is unknown (the installer doesn't persist it across runs).
```

That guard is correct in itself — it exists to stop `psql` hanging on an interactive password prompt (`DatabaseSetup.cs:38-50`, `PostgresInstaller.cs:53-59`). The defect is that **the installer asks the database-provisioning question at all on an upgrade**, where the database, the `indypos_app` role, its password and a working DPAPI-protected connection string already exist.

**Collateral damage.** Both failures left the store worse off. Extraction overwrites `appsettings.json` with the package template and `DatabaseSetup` only writes the real values afterwards, so a failure in between strands the store on a config it cannot start from — observed as a template config with an empty `Store:Id`, and after the stop/extract fix, a stopped service. The database was never at risk in either run.

**Why it was never caught.** Every validation to date is a clean install from a wiped snapshot (the 18-check verifier). No release has ever been upgrade-tested.

**Current deployment reality (confirmed 2026-07-25):** the three live stores are still on **v3.7.0**; v4 has never been distributed. So the fresh-install path ships first and is the higher-stakes path, and this upgrade support is for release two onward. Nothing in production is currently exposed to the bugs above.

### Goals

1. Running the installer on a machine that already has IndyPOS upgrades it rather than failing.
2. A failed upgrade leaves the store running the version it started on, and verifiably serving.
3. The store's identity, type, database and secrets survive an upgrade untouched.
4. The fresh-install path keeps its current behaviour exactly.

### Non-goals

- **PostgreSQL major-version upgrades.**
- **Multi-terminal orchestration.** The installer upgrades the machine it runs on.
- **Automatic database rollback.** A dump is taken and left as a recovery artifact; restoring it is a human decision.
- **Downgrade.** Refused.
- **Upgrading through the interactive wizard.** The wizard detects and refuses (§9).
- **Persisting the PostgreSQL superuser password.** Decided 2026-07-25: not persisted. Machine-scoped DPAPI with entropy from a public constant is not a defence against a local account, so storing a superuser credential on a store PC buys little. The consequences are handled by making the failure safe instead (§3, §8).

---

## 2. Architecture

A dedicated upgrade sequence beside the existing install sequence, selected by a detector, both composing shared step units.

```
                    InstallModeDetector
                            |
            Fresh  --------- + --------- Upgrade
              |                             |
  FreshInstallOrchestrator          UpgradeOrchestrator
              |                             |
              +----------- shared ----------+
                  ServiceControl
                  StoreHubPayload
                  MigrationRunner
                  HealthProbe
                  ConfigSnapshot
                  UpgradeBackup (upgrade only)
```

An upgrade is not a variation on a fresh install — it is a different algorithm with different safety requirements. Forcing both through one flow is what produced Failure 1: a step order correct for one case and destructive for the other.

**`InstallationOrchestrator` is renamed `FreshInstallOrchestrator`** in its own mechanical commit. The unprefixed name reads as "the orchestrator" and the new one as a special case, which is precisely backwards. Its two call sites are `SilentInstaller.cs:61` and `InstallationWizard.cs:41`.

**`SchemaProvisioner` is named `MigrationRunner`** — it runs `IndyPOS.StoreHub.exe migrate` and runs on **both** paths, whereas `DatabaseSetup` is fresh-only. Those two must never be confused; shared vocabulary invites exactly that.

**`DatabaseSetup` gets a banner comment:** *"FRESH INSTALL ONLY. Never runs on an upgrade — the role, password and DPAPI connection string already exist."* Both of today's failures ultimately landed in this class.

**Each shared unit gets a one-line note** that it is used by both paths, so a future change for the upgrade case cannot quietly alter what ships to a store.

**Refactor safety gate.** The extraction of shared units must leave `FreshInstallOrchestrator` semantically unchanged. Enforced as a review gate: after the extraction tasks, `git diff` on that file shows **only the rename**, no logic changes. This matters because the fresh-only branches (`sc create`, first-time directory creation, `DatabaseSetup`, the Postgres install) **never execute on the only VM snapshot available**, so no amount of upgrade testing would catch a regression in them.

Rejected: branching inside the existing orchestrator (threads conditionals through the only production-validated path); making every step mode-agnostic (`DatabaseSetup` would still need to distinguish reuse from provision — mode detection in disguise — with maximum blast radius on the fresh path).

---

## 3. Mode detection

```csharp
enum InstallMode { Fresh, Upgrade, Unusable }

record DetectedInstall(
    InstallMode Mode,
    string? InstalledVersion,   // install-manifest.json
    string? StoreId,            // appsettings.json — the manifest does not record it
    string  Reason);            // surfaced on Unusable
```

Three outcomes, not two. A machine can be neither freshly installable nor upgradeable — the state observed after Failure 2 had a manifest and a registered service but a template config. Collapsing `Unusable` into `Fresh` sends the run *past* the mutation line: extraction succeeds, then `DatabaseSetup` fails on the superuser guard, which is exactly the destructive state this design exists to prevent.

Rules are evaluated **in order**; the first match wins.

| # | Condition | Mode |
|---|---|---|
| 1 | A PostgreSQL install with an `indypos_storehub` database exists, but no IndyPOS manifest for the target major | `Unusable` |
| 2 | Manifest present, config has a usable connection string, non-empty `Store:Id`, present-and-parseable `Store:Type`, and the service's ImagePath resolves under `StoreHubInstallPath` | `Upgrade` |
| 3 | No manifest and no prior StoreHub config | `Fresh` |
| 4 | Anything else | `Unusable` |

**Rule 1 exists to close a data-loss path (§8).** `SystemRoot` is `C:\ProgramData\IndyPOS\v{Major}` (`InstallationConfig.cs:80`), so a **v5 installer on a live v4 store** finds no manifest under `v5\` and would otherwise be classified `Fresh`. Because `DatabaseName` is **not** version-scoped (`indypos_storehub` for every major), that "side-by-side" install collides with the existing database. Note this differs from v3.7.0 ↔ v4 coexistence, which is genuinely safe: v3.7.0 lives outside every `v{N}` root and does not use PostgreSQL at all.

**"Usable connection string"** means `ConnectionStrings:storehub-db` exists, is non-empty, and — when `DPAPI:`-marked — round-trips through `SecretProtector.Unprotect`. Catch broadly: `Unprotect` throws `CryptographicException` **or** `FormatException`. A value that fails to decrypt came from another machine, which is an `Unusable` store. Detection and the §7 preflight share this one code path.

**`Store:Id` must be non-empty** (rule 2). `StoreIdentityService` falls back to `STORE-{MachineName}` when it is absent, and `payment_method` is keyed on the composite `(StoreId, Code)` (`PaymentMethodConfiguration.cs:12`), so the seeder in step 6 would insert a **second full set of seven payment methods** under the fallback id — and every subsequent sale would be written against it (`Program.cs:495`), orphaning new sales from the store's history.

**`Store:Type` must be present** (rule 2). It was only added on 2026-07-18 (`DatabaseSetup.cs:388-391`); a store installed before that build has no such key, and `StoreIdentityOptions.Type` defaults to `GeneralHardware` — the **most permissive** value, which re-enables PayLater and Hardware product types. Preserving config verbatim would therefore silently undo the product-type restriction for a Minimart. Options when absent: require `--store-type` and write that single key into the restored config, or refuse as `Unusable`. Accepting the default silently is not permitted.

**`--store-id` becomes optional in upgrade mode**, adopted from the detected config. It is currently mandatory for `--silent` (`SilentArgs.cs:70-71`), which would force a comparison on every upgrade with no correct behaviour when the detected value is null. If it *is* supplied and differs from the detected value, the run fails before mutating anything — silently rewriting store identity would orphan sales history.

**`Unusable` recovery** must be documented per rule, in `docs/operations/`. Since the superuser password is deliberately not persisted, recovery is manual: for rule 1, point the operator at the older major's install root; for rule 4, name the specific missing element. **The message must never recommend `cleanup-v4.ps1 -Force -RemovePostgres`** (§8).

---

## 4. The upgrade sequence

```
0  Detect        -> Upgrade (+ installed version, store id, store type)
1  Preflight     elevation; refuse downgrade (allow same-version repair);
                 pg_dump located; connection string decrypts; free space;
                 no POS app running; ensure fonts / .NET 10 / VC++ redist
2  Stop service
3  Backup        pg_dump           -> <BackupsDirectory>\<stamp>\storehub.dump
                 copy StoreHub dir -> <BackupsDirectory>\<stamp>\StoreHub\
                 ConfigSnapshot.Capture(appsettings.json)
                 ACL-lock the stamp dir and both artifacts; verify dump
                 integrity; prune to last N stamps
--- nothing above this line has mutated the install ---
4  Deploy        StoreHubPayload.Extract   (overwrites config with template)
5  Restore config  ConfigSnapshot.Restore()
6  Migrate       MigrationRunner: StoreHub.exe migrate
7  Start service + HealthProbe
--- server upgrade complete ---
8  POS app       re-run embedded Velopack setup; read current\sq.version
                 before and after to derive POS_UPDATED (verified, §12)
9  Manifest      InstallManifestWriter.WriteAsync (new version, same paths)
10 Markers       MODE, RESULT, SERVICE_STARTED, HEALTH, BACKUP_DIR,
                 BACKUP_LOCKED, POS_UPDATED, ROLLED_BACK
```

**Step 5 is the heart of the design.** `DatabaseSetup` never runs on an upgrade — no superuser password needed, no role created, no connection string generated. This is what makes Failure 2 disappear rather than be worked around.

**The service stops *before* the dump** (step 2 before step 3). Dumping a live database yields an internally consistent snapshot, but any sale completed between the dump and step 7 would exist only in the live database — and "restore the dump" is the documented recovery, so that window would be silently discarded. Stopping the service is not an irreversible mutation (rollback restarts it), so it belongs above the mutation line.

**Prerequisite ensures are part of preflight** (step 1). The fresh path runs fonts, .NET 10 and VC++ redist first (`FreshInstallOrchestrator` steps 0–1b), all idempotent check-then-install. Omitting them on upgrade has a concrete consequence: the bundled **FC Subject fonts** were only added on 2026-07-14, so a store installed before that build would receive this UI-polish release and render it in a fallback face — a visible regression delivered *by* the upgrade. A future .NET major bump would otherwise deploy binaries the machine cannot run, surfacing as a dead service *after* the mutation line.

`BackupsDirectory` already exists in `InstallationConfig` and the manifest, though only `DatabaseSetup.CreateDirectories` guarantees it — `UpgradeBackup` must create the stamp path itself.

Connection-string parsing uses `NpgsqlConnectionStringBuilder`, not string splitting. The dump uses custom format (`-Fc`); the matching `pg_restore` command is documented in `docs/operations/`.

---

## 5. Failure handling

| Failing step | Action |
|---|---|
| 0–3 | Nothing mutated. Report and exit. |
| 4–7 | **Roll back:** delete `StoreHubInstallPath`, copy the backup tree back, `ConfigSnapshot.Restore()`, start the service, **run `HealthProbe`**, emit `ROLLED_BACK=true` with post-rollback `SERVICE_STARTED` / `HEALTH`, report the backup path. |
| 8 | **No rollback.** The server is upgraded and serving. `RESULT=failed` with `SERVICE_STARTED=true, POS_UPDATED=false`. The spike confirms step 8 cannot disturb the service (§12), so the two are genuinely independent. |
| 9–10 | Non-fatal. A stale manifest is cosmetic and the next run repairs it. |

**Rollback is delete-then-copy, not copy-over.** Extraction is per-entry `ExtractToFile(overwrite: true)` with no clean step (`StoreHubInstaller.cs:227-235`), so after step 4 the tree is `old ∪ new`. Copying the backup over that would leave new-version files behind, violating goal 2.

**Rollback must verify it worked.** Starting the service is not evidence it came back; without a probe, a rollback can report "store resumes on its previous version" while the store is dead. When the post-rollback probe fails, the outcome escalates to advising restoration of the dump.

**Migrations are forward-only, and that is now a release gate rather than an assumption.** Rollback restores binaries and config, not schema, so every migration in a release must be old-binary-compatible: additive only, new columns nullable or defaulted, no renames or drops. Without that rule the rollback claim is false — a `NOT NULL`-without-default column makes the restored binaries' INSERT fail (the till cannot complete a sale), and a rename makes every SELECT throw `42703`. The migration set already contains column-adding shapes (`20260714085744_AddMustChangePassword`).

For the record, `ReclassifyPaymentMethodKinds` is safe under old binaries, verified rather than assumed: the column is a plain `int` with no CHECK constraint, EF's int→enum conversion is an unchecked cast, `PaymentMethodPolicy` ignores `Kind` entirely, and the pre-reclassification grid rendered `Kind.ToString()`, so the cell simply reads `"3"`.

`RESULT` keeps its two values (`success`, `failed`) so the harness gating rule is unchanged; the other markers say *what* succeeded.

---

## 6. Backup artifacts

**They must be ACL-locked.** `appsettings.json` is deliberately restricted to Administrators + LocalSystem with inheritance disabled (`DatabaseSetup.TryRestrictFilePermissions`, applied at `:316`), but `BackupsDirectory` is created with a plain `Directory.CreateDirectory` and inherits ProgramData's default DACL, which grants `BUILTIN\Users` read. Copying the config into a backup would silently undo its protection, and the dump is worse: the entire sales history plus BCrypt admin hashes. `UpgradeBackup` applies `TryRestrictFilePermissions` to the stamp directory and both artifacts, and reports `BACKUP_LOCKED` the way `CRED_LOCKED` already works.

**Dump integrity is verified, not assumed.** A disk-full `pg_dump` leaves a truncated file that still satisfies "present", so `BACKUP_DIR` would advertise an unrestorable artifact. Require a zero exit code **and** a non-trivial file size. A free-space preflight precedes the dump.

**Retention: keep the last N stamps** (N = 2). Each stamp costs roughly 130 MB of self-contained binaries plus the dump, forever, on a small retail PC with nobody watching it.

**`pg_dump` location falls back.** The manifest's `PostgresBinPath` can be stale; fall back to `PostgresInstaller.FindPostgresInstallation()`. That probes 18 → 17 → 16, so assert a major matching the server.

---

## 7. Markers and exit codes

`SilentOutcomeMapper.Map` ends in `_ => throw new ArgumentOutOfRangeException`, so every new outcome needs an explicit arm and exit code. The harness gates on exit code **and** `RESULT` **and** `SERVICE_STARTED`.

- Add sibling records — `UpgradeSucceeded`, `UpgradeFailed`, `Unusable`, `DowngradeRefused` — rather than widening `InstallSucceeded`, which is already a five-field positional record and belongs to the frozen fresh path.
- **Suppress the fresh-path admin markers on upgrade.** `SuccessMarkers` always emits `ADMIN_SEEDED`, which on an upgrade is necessarily `false` (a fresh install stripped the `InitialAdmin` block, so `InitialAdminSeeder` skips), and the `else` branch then emits `RESET_HINT=run "IndyPOS.StoreHub.exe reset-admin"…` — advising a bootstrap-password reissue on a store whose admin is fine. The upgrade path ignores `AdminSeeded`, emits no `CRED_FILE`, and never writes `admin-credentials.txt`.
- `MODE=upgrade|fresh` lands in `install-latest.log`, so months later a store's own log answers "which path ran" without reading any C#.

---

## 8. Closing the data-loss path

Independent of everything above, one existing message must change. The superuser guard currently instructs the operator to run `scripts\cleanup-v4.ps1 -Force -RemovePostgres` (`DatabaseSetup.cs:46`). That script calls `DROP DATABASE IF EXISTS indypos_storehub` **unconditionally unless `-SkipDatabase`**, and *before* it touches PostgreSQL — while the flag name and its own help ("the Postgres data directory is left behind") both read as scoped to the PostgreSQL install. An operator following the installer's own guidance destroys every sale the store has recorded.

**Requirement:** the guard must detect whether a populated `indypos_storehub` exists and, when it does, never recommend the cleanup script. Combined with detection rule 1, the v5-over-v4 case never reaches this guard at all.

---

## 9. Entry points

Both entry points consume **one** detection result from a single router — if `Program.cs` and `SilentInstaller` each called the detector, the two forks would diverge.

- **`--silent`** is the only path that performs an upgrade.
- **The interactive wizard detects and refuses.** It must not be left undetected: a double-clicked `Setup.exe` on a live store otherwise reproduces both failures, and that is the likely path on a hands-on store visit. But it does not gain an upgrade UI — it collects StoreId and StoreType up front (both already fixed on an upgrade) and its finish screen is built around bootstrap credentials that do not exist on an upgrade. It shows the detected version and directs the operator to the `--silent` command.

---

## 10. Testing

**Unit-testable — real seams:**

| Unit | Cases |
|---|---|
| `InstallModeDetector` | all four rules in order; rule 1 ahead of the manifest lookup; empty `Store:Id`; absent `Store:Type`; unresolvable service ImagePath; undecryptable `DPAPI:` value; store-id conflict incl. the null-detected case |
| `ConfigSnapshot` | ✅ built and tested (5 tests) — byte-exact restore, absent-at-capture, template cleanup, idempotency |
| `UpgradeBackup` | copy/restore round-trip; delete-then-copy semantics; retention prune; missing-backup restore; integrity rejection of a truncated dump |
| Marker mapping | pure, mirroring `SilentOutcomeMapperTests`: every new outcome, suppression of `ADMIN_SEEDED`/`RESET_HINT`, `ROLLED_BACK` |

**No seam — the VM's job:** `ServiceControl`, `pg_dump`, Velopack.

**VM validation** from checkpoint `Pre-Upgrade-2026-07-25` (a genuine pre-reclassification store: `PayLater kind=1`, `WelfareCard kind=1`, `Store:Id=Rungrat-001`, service running):

1. **Happy upgrade** — `MODE=upgrade`; `PayLater kind=3`; `WelfareCard kind=2` **and still enabled**; `Cash`/`MoneyTransfer` `kind=1`; config still `DPAPI:`-protected; `Store:Id` **and `Store:Type`** preserved; service Running; health 200; dump present, non-trivial and ACL-locked.
2. **Forced mid-upgrade failure** — old version restored, real config, service Running **and health 200**, `ROLLED_BACK=true`. Both of today's failures would have flunked this, so it is not optional.
3. **Fresh-install regression** — one clean install reaching 18/18, proving the refactor changed nothing on the path that ships first.

**The fault hook must be runtime-selected** (an env var read once, or a hidden `--simulate-failure=deploy`), not compile-time, so one 190 MB installer build serves cases 1 and 2 in a single session. At ~10 minutes per cycle plus build, that is the difference between one evening and two.

**The harness needs changes to run case 2 at all:** it hardcodes the snapshot name and *throws* whenever `RESULT ≠ success` (`Reset-AndInstall.ps1:153`, `:269-272`), so a deliberate-rollback run fails by construction. It needs a `-SnapshotName` parameter and an expect-rollback mode. Case 2 must assert against the fresh timestamped log, not `install-latest.log`, which can hold a previous run's `RESULT=success`.

**Database assertions** are already proven feasible: decrypt the connection string in-guest using `SHA256("IndyPOS:ConnectionStrings:storehub-db")` as DPAPI entropy at LocalMachine scope, then drive `psql`. Done interactively on 2026-07-25 to capture the pre-upgrade state; it needs scripting into the verifier. Note the guest is **PowerShell 5.1 with a Windows-1252 codepage**, so any script sent to it must be ASCII or BOM-encoded.

**Case 3 gates distribution, not merge.** The clean-Windows snapshot and the Win11 ISO were deleted on 2026-07-25; the baseline is being rebuilt by tearing down the existing VM. Because the fresh path is protected structurally (§2 refactor gate) and by the existing bootstrapper suite, the branch can land before case 3 — but no installer goes to a store until 18/18 passes on a clean baseline.

---

## 11. Known limitations

1. **Velopack is per-user.** Re-running setup under `--silent` updates the profile the installer runs as, so a cashier's copy on another profile can stay on the old version until Velopack's own updater reaches it. The rewritten manifest's `VelopackInstallPath` also records the *installing* user's LocalAppData, so upgrading as a different admin repoints what cleanup and verify consume.
2. **The POS app self-updates independently** (`UpdateService` against GitHub Releases), so it can already be *ahead* of StoreHub without the installer running. The upgrade path cannot fix that skew, only avoid adding to it — hence the running-app check in step 1, since two Velopack processes on one install root can leave the POS unlaunchable.
3. **Forward-only migrations** (§5).
4. **No PostgreSQL version management.**
5. **Single machine per run.**
6. **`LocalToken:SecretKey` is preserved**, so existing POS sessions survive an upgrade — a real benefit, but it also means the key is never rotated over the product's lifetime.
7. **A release version bump is a precondition.** `Directory.Build.props` pins the version and requires a manual edit; without a bump, downgrade protection always takes the same-version repair branch and `InstalledVersion` is decorative. Step 8 itself is safe without a bump — the spike measured a 2.5 s repair exiting 0 (§12) — and reports `POS_UPDATED=false` because it compares `sq.version` rather than trusting the exit code.
8. **`StoreConfiguration.json` is create-if-absent on the fresh path**, so skipping `DatabaseSetup` loses nothing — but an upgrade of a store missing that file will not create it either.
9. **`migrate` inherits Npgsql's 30s command timeout.** A future long DDL migration over a multi-year invoice table would time out and trigger a rollback for no real reason. Consider raising it on the migrate path.

---

## 12. Step 8 — resolved by spike (2026-07-26)

**Question:** what does the embedded Velopack `Setup.exe --silent` do on a machine where the POS app is already installed? Step 8 and §5 row 8 both depended on the answer.

**Method.** Both cases were run against checkpoint `Pre-Upgrade-2026-07-25` (a real store image with `IndyPOS.POS.v4` 4.0.0 installed and the StoreHub service Running), restoring the snapshot between them. A 4.0.1 package was produced with `vpk pack` from the existing `publish\WinForms` output; the shipping 4.0.0 `Setup.exe` served the same-version case. Canary files were planted at the install root and inside `current\` to detect sweeping. This invoked the Velopack `Setup.exe` directly rather than through `VelopackLauncher`, which is the same executable with the same `--silent` argument.

| | 4.0.1 over 4.0.0 | 4.0.0 over 4.0.0 |
|---|---|---|
| Exit code | 0 | 0 |
| Elapsed | 7.5 s | 2.5 s |
| `current\sq.version` | 4.0.0 → **4.0.1** | 4.0.0 → 4.0.0 |
| `packages\` | old `.nupkg` pruned, 4.0.1 written | unchanged |
| StoreHub service | untouched | **Running → Running** |
| `C:\ProgramData\IndyPOS\v4` | untouched | untouched |
| Shortcuts | preserved, same two paths | preserved, same two paths |
| POS process after | none launched | none launched |

**Conclusions:**

1. **Step 8 stands as designed.** Velopack upgrades in place, prunes the superseded package, and reports 0. No refusal, no destructive reinstall — so §5 row 8 ("no rollback; the server is already serving") remains correct.
2. **Nothing newer is a safe repair, not a failure.** The same-version run rebuilt `current\` from the existing package and exited 0 in 2.5 s. This closes the §11.7 open item: step 8 needs no special case, but `POS_UPDATED` must not be inferred from the exit code.
3. **`POS_UPDATED` is derived by reading `current\sq.version` before and after** and comparing. The exit code cannot distinguish an upgrade from a repair, and the binaries keep their own build stamp — Velopack's `sq.version` (a nuspec document carrying `<version>`) is the only truthful record of what is installed.
4. **Setup sweeps foreign files from the install root.** Both canaries were deleted, at the root as well as inside `current\`. IndyPOS keeps no state there — its data lives under `C:\ProgramData\IndyPOS\v4` — so nothing is at risk today, but the install root must never be used for store state.
5. **The POS-app step cannot disturb the server.** The service stayed Running across a full package swap, confirming steps 7 and 8 are independent and that step 8's failure genuinely does not warrant rollback.

Untested and still a limitation, not a blocker: running setup as a *different* admin than the one that installed (§11.1) — the spike ran as the installing user throughout.

---

## 13. Prerequisite already landed

The stop-before-extract reorder plus the `ConfigSnapshot` guard ship in **PR #53** (`fix/installer-upgrade-robustness`), with 5 tests and the bootstrapper suite at 101 pass / 8 skip, plus a fresh-install VM regression at 18/18. It is a strict improvement to the fresh path too — any extraction failure now leaves config intact — and this design builds on it. Its guard covers extraction failure only; §5's rollback supersedes it for the upgrade path.

**This spec assumes PR #53 has merged.** If it has not, the upgrade path's first failure is still the locked-DLL error from §1.

That PR also carries three `cleanup-v4.ps1` bugs found while rebuilding the VM baseline — two from the May move to a major-only install root being only half-applied (the safety guard still demanded `v<Major>.<Minor>.<Patch>`, and three em-dashes made the script unparseable under the guest's Windows-1252 codepage), plus one where the DPAPI-sealed connection string was fed to a connection-string builder, so the teardown silently skipped the database drop.
