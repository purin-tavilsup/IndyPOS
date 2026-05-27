# Installer Side-by-Side Plan (v3.7.0 ↔ v4.0.0)

> Allow IndyPOS v4.0.0 to install on a machine that already runs v3.7.0, with zero risk to the existing install.

## Context

- **v3.7.0:** offline SQLite-based POS, monolithic. No StoreHub, no Postgres, no Velopack, no Windows service.
- **v4.0.0:** Clean Architecture, StoreHub Windows service, PostgreSQL 18, Velopack-managed WinForms.

The architectures barely overlap, so the only true conflict zone is the shared filesystem path `C:\ProgramData\IndyPOS\`.

## Locked-in Decisions

| Topic | Decision |
|---|---|
| Version source-of-truth | `InstallVersion = "4.0.0"` read from bootstrapper assembly version |
| System-shared root | `C:\ProgramData\IndyPOS\v4.0.0\` |
| Service name | `IndyPOS.StoreHub.v4` |
| Velopack app ID | `IndyPOS.POS.v4` |
| Velopack install path | Default `%LOCALAPPDATA%\IndyPOS.POS.v4\current` (carve-out — per-user, supports auto-update) |
| Health check port | `5000` (fix 5000 vs 5012 inconsistency in orchestrator) |
| Database name | `indypos_storehub` (no collision — 3.7.0 doesn't use Postgres) |

## Target Path Layout (v4.0.0)

```
C:\ProgramData\IndyPOS\
├── Config\              # ← v3.7.0 (UNTOUCHED)
├── ...                  # ← v3.7.0 (UNTOUCHED)
└── v4.0.0\              # ← v4 isolated here
    ├── Config\
    │   └── StoreConfiguration.json
    ├── StoreHub\        # Service binaries
    │   ├── IndyPOS.StoreHub.exe
    │   └── appsettings.Production.json
    ├── keys\
    │   └── storehub.key
    ├── logs\
    └── backups\

%LOCALAPPDATA%\IndyPOS.POS.v4\current\   # ← Velopack WinForms (per-user)
```

## Stages

| # | Stage | Status |
|---|---|---|
| 0 | Discovery — verify dev box is clean for v4 paths/ports/services | ✅ |
| 1 | Refactor `InstallationConfig` to carry version + computed paths | ✅ |
| 2 | Build pipeline — install vpk, embed Setup.exe + StoreHub.zip into bootstrapper resources | ✅ |
| 3 | Smoke-test runner + uninstall/cleanup script | ✅ |
| 4 | Smoke-test on dev box, fix bugs as found | ⏳ (option A — real Postgres 18 install) |
| 5 | Implement Phase 1 VM scripts (per `vm-installer-testing-plan.md`) | ⏳ |
| 6 | Manual: Win11 ISO, Hyper-V VM, clean snapshot (Pond) | ⏳ |
| 7 | VM run, fix bugs, repeat until green | ⏳ |

## Stage 1 Files Touched ✅ (2026-05-02)

- `installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj` — added `<Version>4.0.0</Version>` so `InstallVersion` derives from assembly
- `installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs` — `InstallVersion` (from assembly) + 9 computed properties: `SystemRoot`, `ConfigDirectory`, `KeysDirectory`, `LogsDirectory`, `BackupsDirectory`, `StoreHubInstallPath`, `ServiceName`, `ServiceDisplayName`, `VelopackAppId`, `HealthCheckPort`
- `installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs` — dropped 4 consts, helpers now instance methods reading captured `Config`
- `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs` — dropped 5 directory consts, path-using helpers now instance, `appsettings.Production.json` template now pins `Urls: http://localhost:5000`
- `installer/IndyPOS.Bootstrapper/Installers/VelopackLauncher.cs` — takes `config`, looks for `IndyPOS.POS.v4-Setup.exe`, install path uses `VelopackAppId`
- `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` — passes `config` into launcher, health check URL uses `config.HealthCheckPort`
- `scripts/publish.ps1` — `--packId "IndyPOS.POS.v4"`

**Build:** `dotnet build` clean, 0 warnings, 0 errors.

## Stage 2 Files Touched ✅ (2026-05-03)

- `.gitignore` — added `installer/IndyPOS.Bootstrapper/Resources/` (build-time blobs, ~80 MB)
- `installer/build-installer.ps1` — added embed step before `dotnet publish`: globs `IndyPOS.POS.v4*Setup.exe` + `IndyPOS.StoreHub-*.zip` from `publish/Releases/`, copies into `Resources/` with canonical names (`IndyPOS.POS.v4-Setup.exe`, `StoreHub.zip`) so manifest names match the bootstrapper's runtime lookup
- `Directory.Build.props` — repo-wide version bump `1.0.0` → `4.0.0` (Stage 1's bootstrapper-only `<Version>` was being clobbered by the `<AssemblyVersion>1.0.0.0</AssemblyVersion>` in D.B.props, making `InstallVersion` resolve to `1.0.0` instead of `4.0.0`)
- `installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj` — dropped redundant local `<Version>4.0.0</Version>` (now matches D.B.props)
- `src/IndyPOS.Windows.Forms/Properties/AssemblyInfo.cs` — `AssemblyVersion`/`AssemblyFileVersion` `3.7.0` → `4.0.0`. Project has `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` so D.B.props doesn't reach it; vpk auto-detects pack version from `IndyPOS.Windows.Forms.exe` ProductVersion
- `vpk` CLI installed globally (`dotnet tool install -g vpk`, v0.0.1298)

**Verified:**
- `IndyPOS.Bootstrapper.dll` reflected `AssemblyVersion = 4.0.0.0`
- Manifest contains `IndyPOS.Bootstrapper.Resources.IndyPOS.POS.v4-Setup.exe` (12.3 MB) + `IndyPOS.Bootstrapper.Resources.StoreHub.zip` (70.0 MB)
- `publish/IndyPOS-Setup.exe` final: 189 MB, FileVersion `4.0.0.0`
- vpk packed as `IndyPOS.POS.v4` v `4.0.0` (was `3.7.0` before WinForms AssemblyInfo bump)

**Follow-up (not blocking Stage 3):** `IndyPOS.Application`, `IndyPOS.Infrastructure`, `IndyPOS.Windows.Forms` all carry legacy `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` + hand-written `Properties/AssemblyInfo.cs`. Modernizing (delete those files, flip to default auto-gen) would let D.B.props drive every assembly's version. Out of scope for the side-by-side install epic.

## Stage 3 Files Touched 🟡 (2026-05-26 — in progress)

**Initial draft** — `scripts/cleanup-v4.ps1` (~250 lines). Reverses the 6 install steps in `InstallationOrchestrator.InstallAsync` in reverse order: service → Velopack → DB → `$SystemRoot` → `%LOCALAPPDATA%\IndyPOS.POS.v4\` → (opt-in) Postgres uninstall.

**P0+P1 hardening from QA review** (subagent review 2026-05-26 — surfaced 2 BLOCKERs and 3 HIGHs):
- Password parsing uses `[System.Data.Common.DbConnectionStringBuilder]` — the previous regex broke on passwords containing `;` or `"`.
- Safety guard now asserts `vM.m.p` regex pattern via `Assert-V4Path` instead of a string-equality check that could never trip (paths were hardcoded constants, so the old guard was theatre).
- `-Force` now suppresses interactive `Read-Host` prompts — previously broke unattended smoke-test loops.
- `Wait-ServiceGone` polls after `sc.exe delete` — prevents stale entry ghosting into next install.
- `Wait-VelopackProcessExit` + COM-based shortcut sweep — addresses Velopack's fire-and-forget Update.exe race + Start Menu orphans + `%TEMP%\$VelopackAppId-Setup.exe` leftover.
- `REASSIGN OWNED BY` + `DROP OWNED BY ... CASCADE` before `DROP ROLE` — prevents "role cannot be dropped" failure.

**Install manifest** (architecture review 2026-05-26 — kills the `# must match InstallationConfig.cs` lockstep coupling):
- `installer/IndyPOS.Bootstrapper/Installers/InstallManifest.cs` — record with 17 safe-to-persist fields. NO passwords/secrets. `ManifestVersion = 1`.
- `installer/IndyPOS.Bootstrapper/Installers/InstallManifestWriter.cs` — writes camelCase JSON to `$SystemRoot\install-manifest.json`. Idempotent.
- `installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs` — added `VelopackInstallPath` computed property (single source-of-truth).
- `installer/IndyPOS.Bootstrapper/Installers/VelopackLauncher.cs` — uses `Config.VelopackInstallPath`; removed duplicate `GetWinFormsInstallPath()`.
- `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` — calls `InstallManifestWriter.WriteAsync` as final post-health-check step. Non-fatal on failure.
- `scripts/cleanup-v4.ps1` — `Read-InstallManifest` glob-discovers `v*\install-manifest.json` under `$ProgramDataRoot`; falls back to baked-in defaults for partial-install resilience. Banner shows source.
- `tests/IndyPOS.Bootstrapper.Tests/Installers/InstallManifestTests.cs` — 3 tests: field-mapping, no-password-leak (asserts secret string absent from serialized JSON), filename stability constant (so a C# rename triggers test failure if PS script drifts).

**Pre-existing fix folded in:** `tests/IndyPOS.Bootstrapper.Tests/Installers/StoreHubInstallerTests.cs:131` — `VelopackLauncher.InstallAsync` signature drift from Stage 1 left a `[Skip]`'d test that wouldn't compile. One-line fix to unblock the test build.

**Verified end-to-end:**
- `dotnet build` bootstrapper: 0 warnings, 0 errors.
- `dotnet test` manifest tests: 3/3 passing.
- `cleanup-v4.ps1 -Force` no-op run on clean dev box: graceful "nothing to do" for all 4 steps, `Source: defaults` banner.
- Synthetic `v9.9.9` manifest test: glob discovered, all values overridden (ServiceName → `IndyPOS.StoreHub.v9`, DB → `indypos_storehub_v9`, etc.), correctly deleted the synthetic dir. Confirms cleanup is now version-agnostic.

**Stage 3 COMPLETED (2026-05-27)** — `verify-install.ps1` 31/31 PASS end-to-end; install ran in 9m31s; v3.7.0 untouched.

**7 production bugs caught + fixed by the smoke-test cycle:**
1. `--serviceaccount` arg unquoted → EDB exit 1.
2. 10-min Postgres timeout too tight for Defender-throttled unpack → 25 min.
3. `FindPostgresInstallation` only checked psql.exe → partial install fooled it. Now also requires `postgresql-x64-NN` service.
4. `StoreHubInstaller` zip-extract clobbered `appsettings.Production.json`. Reordered orchestrator (binaries → DB) and renamed to `appsettings.json` (single tier — no overlay needed).
5. StoreHub was a console app, not a Windows Service → SCM error 1053. Added `AddWindowsService` + `Microsoft.Extensions.Hosting.WindowsServices`.
6. Bootstrapper probed `/health/live` (didn't exist) → 404. Industry-standardised: `/health/live` + `/health/ready` (tag-filtered, in prod), `/health` dev-only.
7. `psql` hangs on missing superuser pw. Added `-w` flag + early-return with actionable error.

**Architecture wins:**
- `InstallManifest` (record + writer + tests): bootstrapper writes `$SystemRoot\install-manifest.json`. Scripts glob-discover under `v*\` subdirs → version-agnostic, no more `# must match InstallationConfig.cs` lockstep.
- Postgres installer cache at `%LOCALAPPDATA%\IndyPOS.Bootstrapper\cache\` — skips 372 MB download per cycle.

**Deferred (P2/P3, follow-ups):**
- Velopack `Update.exe` path bug in cleanup-v4 (looks in `\current\` instead of AppId root). Workaround via COM shortcut sweep + temp Setup.exe cleanup means net effect is correct.
- JWT key ACL `takeown` before `Remove-Item`.
- Velopack uninstall registry key (`HKCU\...\Uninstall\IndyPOS.POS.v4`).
- Postgres firewall rule + data directory on `--mode unattended` uninstall.
- `$env:PGPASSWORD` parent-scope leak on Ctrl-C.
- `smoke-test.ps1` reliability (hardcoded inventory `88`, stale pay-later accounts).

## Success Criteria

- Bootstrapper writes only under `C:\ProgramData\IndyPOS\v4.0.0\` and `%LOCALAPPDATA%\IndyPOS.POS.v4\` — never touches v3.7.0 subfolders.
- Both v3.7.0 and v4.0.0 launchable on the same machine.
- Re-running the installer is idempotent (detects existing service/db, doesn't crash).
- Cleanup script reverses all v4 install actions; v3.7.0 remains intact.

## Open / Deferred

- **Migration from v3.7.0 → v4.0.0** is out of scope here. Once v4 is verified working, a separate effort handles SQLite → Postgres data migration (Epic M / MigrationTool).
- **Phase 2 (full unattended Windows install)** deferred — Phase 1 first.
