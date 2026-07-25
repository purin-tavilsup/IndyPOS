# In-Place Upgrade Support for the IndyPOS Installer — Design

**Status:** 🟢 Ready for planning
**Created:** 2026-07-25
**Trigger:** VM upgrade smoke test on `IndyPOS-Test`, 2026-07-25 (two failed runs, both diagnosed)

---

## 1. Problem

`IndyPOS-Setup.exe` is a **fresh-install-only** tool. Running it over an existing install fails, and fails destructively. Both failure modes were reproduced on a real store image (VM `IndyPOS-Test`, store `Rungrat-001`, StoreHub 4.0.0 running):

**Failure 1 — locked binaries.** `StoreHubInstaller.InstallAsync` extracted the StoreHub payload *before* stopping the service, so the running service held its own DLLs open:

```
ERROR: StoreHub installation failed: The process cannot access the file
'...\v4\StoreHub\Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.dll'
because it is being used by another process.
```

**Failure 2 — no upgrade concept.** With the stop/extract order corrected, the run reached `DatabaseSetup` and stopped there:

```
ERROR: Database setup failed: PostgreSQL 18 is already installed, but its
superuser password is unknown (the installer doesn't persist it across runs).
```

This is by design: `PostgresInstaller` returns `SuperuserPassword = ""` for an existing install (`PostgresInstaller.cs:58`), and `DatabaseSetup.SetupAsync` refuses to continue without it (`DatabaseSetup.cs:38-50`). The guard is correct — it exists to stop `psql` hanging on an interactive password prompt. The bug is that **the installer asks the database-provisioning question at all on an upgrade**, where the database, the `indypos_app` role, its password, and a working DPAPI-protected connection string already exist.

**Collateral damage.** Both failures left the store worse than before. Extraction overwrites `appsettings.json` with the package template and `DatabaseSetup` only writes the real values afterwards, so a failure in between strands the store on a config it cannot start from — observed as a template config with an empty `Store:Id` and, after the stop/extract fix, a stopped service. The database and its data were never at risk in either run.

**Why this was never caught.** Every validation to date has been a clean install from a wiped snapshot (the 18/18 verifier suite). No release has ever been upgrade-tested. Consequently **there is no supported upgrade path to the three live stores today.**

### Goals

1. Running the installer on a machine that already has IndyPOS upgrades it, rather than failing.
2. A failed upgrade leaves the store running the version it started on.
3. The store's identity, database, and secrets survive an upgrade untouched.
4. The fresh-install path keeps its current behaviour exactly — it is the only path validated in production.

### Non-goals

- **PostgreSQL major-version upgrades.** Out of scope; requires data migration and is its own project.
- **Config merging.** A preserved config does not gain new keys introduced by a newer template (see §7).
- **Multi-terminal orchestration.** The installer upgrades the machine it runs on. A second POS terminal is its own run.
- **Automatic database rollback.** A dump is taken and left as a recovery artifact; restoring it is a human decision (§5).
- **Downgrade.** Refused (§4, step 1).

---

## 2. Architecture

A dedicated upgrade sequence beside the existing install sequence, selected by a detector, with both composing shared step units.

```
                    InstallModeDetector
                            |
            Fresh  --------- + --------- Upgrade
              |                             |
   InstallationOrchestrator          UpgradeOrchestrator
              |                             |
              +----------- shared ----------+
                  ServiceControl
                  StoreHubPayload
                  SchemaProvisioner
                  ConfigSnapshot
                  UpgradeBackup (upgrade only)
```

An upgrade is not a variation on a fresh install — it is a different algorithm with different safety requirements (back up first, roll back on failure, never touch database provisioning). Forcing both through one flow is what produced Failure 1: a step order that is correct for a fresh install and destructive for an upgrade. Keeping the validated path structurally frozen while the new path is built and tested separately is also the lower-risk way to reach three live stores.

Rejected alternatives:

- **Branch inside `InstallationOrchestrator`.** Cheapest to write, but threads new conditionals through the only path with production validation, and makes the upgrade sequence hard to read or test in isolation.
- **Make every step idempotent and mode-agnostic.** `DatabaseSetup` would still have to distinguish "reuse existing" from "provision new" — mode detection in disguise — with maximum blast radius on the fresh path.

---

## 3. Mode detection

```csharp
enum InstallMode { Fresh, Upgrade, Unusable }

record DetectedInstall(
    InstallMode Mode,
    string? InstalledVersion,   // from install-manifest.json
    string? StoreId,            // from appsettings.json — see note
    string  Reason);            // human-readable, surfaced on Unusable
```

Three outcomes, not two. A machine can be neither freshly installable nor upgradeable — the state observed after Failure 2 had a manifest and a registered service but a template config with no connection string. `Fresh` would fail on Postgres provisioning; `Upgrade` would fail on the missing connection string. Detecting that up front and refusing is strictly better than discovering it halfway through.

| Manifest at `$SystemRoot\install-manifest.json` | Config has usable connection string | Mode |
|---|---|---|
| absent | absent | `Fresh` |
| present | present | `Upgrade` |
| present | absent or unreadable | `Unusable` |
| absent | present | `Unusable` |

"Usable connection string" means `ConnectionStrings:storehub-db` exists, is non-empty, and — when `DPAPI:`-marked — round-trips through `SecretProtector.Unprotect("ConnectionStrings:storehub-db", value)`. That is the same check the upgrade needs anyway to run `pg_dump`, so detection and preflight share one code path. A value that fails to decrypt means the config came from a different machine, which is exactly an `Unusable` store.

**`StoreId` comes from `appsettings.json`, not the manifest.** `InstallManifest` records `InstallVersion`, paths, service identifiers, `DatabaseName`, `AppUser` and `PostgresBinPath`, but **not** the store id. Reading `Store:Id` from the StoreHub config works on stores already deployed today, which a manifest change could not. Adding `StoreId` to a future `ManifestVersion = 2` is optional convenience and deliberately not required here.

**Store-id conflict.** If `--store-id` is passed and differs from the detected `Store:Id`, the run fails before mutating anything. Silently rewriting a store's identity would orphan its sales history from its reports.

---

## 4. The upgrade sequence

```
0  Detect        -> Upgrade (+ installed version, store id)
1  Preflight     elevation; refuse downgrade (allow same-version repair);
                 pg_dump present at manifest PostgresBinPath;
                 read + DPAPI-unprotect the connection string
2  Backup        pg_dump           -> <BackupsDirectory>\<stamp>\storehub.dump
                 copy StoreHub dir -> <BackupsDirectory>\<stamp>\StoreHub\
                 ConfigSnapshot.Capture(appsettings.json)
--- nothing above this line mutates the install ---
3  Stop service
4  Deploy        StoreHubPayload.Extract   (overwrites config with template)
5  Restore config  ConfigSnapshot.Restore()
6  Migrate       StoreHub.exe migrate
7  Start service + health probe
--- server upgrade complete ---
8  POS app       re-run embedded Velopack setup
9  Manifest      InstallManifestWriter.WriteAsync (new version, same paths)
10 Markers       MODE, RESULT, SERVICE_STARTED, HEALTH, BACKUP_DIR, POS_UPDATED
```

**Step 5 is the heart of the design.** `DatabaseSetup` never runs on an upgrade — no superuser password is needed, no role is created, no connection string is generated. The existing config is preserved verbatim. This is what makes Failure 2 disappear rather than be worked around.

`BackupsDirectory` already exists in `InstallationConfig` and the manifest, and the directory is already present in a deployed install tree — no new path concept is introduced.

`SchemaProvisioner` (step 6) is today's `StoreHubInstaller.ProvisionDatabaseAsync`, which runs `IndyPOS.StoreHub.exe migrate` as a one-shot console process before the service starts. On an upgrade it applies pending EF migrations; the payment-method seeder it also runs is idempotent (insert-if-absent), so re-running is safe.

---

## 5. Failure handling

| Failing step | Action |
|---|---|
| 1–2 | Nothing mutated. Report and exit. |
| 3–7 | **Roll back:** `ConfigSnapshot.Restore()`, restore the StoreHub dir from backup, start the service, report the backup path. Store resumes on its previous version. |
| 8 | **No rollback.** The server is upgraded and serving; tearing that down to fix a POS copy would be worse. Report `RESULT=failed` with `SERVICE_STARTED=true, POS_UPDATED=false`. |
| 9–10 | Non-fatal. The upgrade succeeded; a stale manifest is cosmetic and is repaired by the next run. |

`RESULT` keeps its existing two values (`success`, `failed`) so the VM harness gating rule is unchanged. The additional markers say *what* succeeded.

**Migrations are forward-only.** EF applies each migration in its own transaction on PostgreSQL, so a failed migration leaves a consistent but possibly partially-advanced schema. Rollback restores binaries and config, not schema. Old binaries against an additively-advanced schema is tolerable in practice — and the `pg_dump` from step 2 is the escape hatch when it is not. Automatic database restore is deliberately excluded: it is riskier than the failure it would fix.

---

## 6. Testing

**Unit-testable — real seams, so these get real tests:**

| Unit | Cases |
|---|---|
| `InstallModeDetector` | all four table rows of §3; store-id conflict; a `DPAPI:` value that fails to decrypt → `Unusable` |
| `ConfigSnapshot` | ✅ built and tested 2026-07-25 (5 tests): byte-exact restore, absent-at-capture, template cleanup, idempotency |
| `UpgradeBackup` | file copy + restore round-trip over temp dirs; restore when backup is absent |
| Marker mapping | pure, mirroring `SilentOutcomeMapper`: `MODE`, `BACKUP_DIR`, `POS_UPDATED` permutations |

**No seam — the VM's job:** `ServiceControl` (`ServiceController` is not injectable), `pg_dump`, Velopack.

**VM validation** on `IndyPOS-Test` from checkpoint `Pre-Upgrade-2026-07-25` (a genuine pre-reclassification store: `PayLater kind=1`, `WelfareCard kind=1`, `Store:Id=Rungrat-001`, service running):

1. **Happy upgrade** — `MODE=upgrade`; `PayLater kind=3`; `WelfareCard kind=2` **and still enabled**; `Cash`/`MoneyTransfer` `kind=1`; config still `DPAPI:`-protected with `Store:Id=Rungrat-001`; service Running; health 200; `storehub.dump` present in the backup dir.
2. **Forced mid-upgrade failure** — induce a failure between steps 4 and 7 and assert the store came back on its old version with its real config and a running service. This is the case both of today's failures would have flunked, so it is not optional.
3. **Fresh-install regression** — one clean install still reaching 18/18, proving the refactor into shared units changed nothing on the validated path.

Case 3 needs a clean Windows snapshot. `Clean-Windows` and `Clean-Windows-Ready` were deleted in the 2026-07-25 disk cleanup, along with the Win11 ISO, so **rebuilding the clean snapshot is a prerequisite for case 3** and should be planned as its own task.

---

## 7. Known limitations

1. **No config merge.** Step 5 restores the old `appsettings.json` wholesale, so keys introduced by a newer package template are absent after an upgrade. StoreHub must rely on code-side defaults (`IOptions<T>` defaults) for anything new. A merge is YAGNI until a release actually needs a new key.
2. **Velopack is per-user.** Re-running setup under `--silent` updates the profile the installer runs as. A cashier's copy on another profile can be left on the old version until Velopack's own update path reaches it.
3. **Forward-only migrations** (§5).
4. **No PostgreSQL version management.** An upgrade assumes the installed major version is the one the release targets.
5. **Single machine per run** (§1 non-goals).

---

## 8. Out-of-scope defect found alongside

`StoreHubInstaller` stopped the service after extracting rather than before. The reorder plus a `ConfigSnapshot` guard was implemented on branch `fix/installer-upgrade-path` on 2026-07-25 (`ConfigSnapshot` + 5 tests; bootstrapper suite 101 pass / 8 skip). That fix is a strict improvement to the fresh path too — any extraction failure now leaves config intact — and this design assumes it as its starting point. Note its guard covers extraction failure only; the broader rollback in §5 supersedes it for the upgrade path.
