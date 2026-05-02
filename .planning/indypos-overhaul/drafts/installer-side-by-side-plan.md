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
| 2 | Build pipeline — install vpk, embed Setup.exe + StoreHub.zip into bootstrapper resources | ⏳ |
| 3 | Smoke-test runner + uninstall/cleanup script | ⏳ |
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

## Success Criteria

- Bootstrapper writes only under `C:\ProgramData\IndyPOS\v4.0.0\` and `%LOCALAPPDATA%\IndyPOS.POS.v4\` — never touches v3.7.0 subfolders.
- Both v3.7.0 and v4.0.0 launchable on the same machine.
- Re-running the installer is idempotent (detects existing service/db, doesn't crash).
- Cleanup script reverses all v4 install actions; v3.7.0 remains intact.

## Open / Deferred

- **Migration from v3.7.0 → v4.0.0** is out of scope here. Once v4 is verified working, a separate effort handles SQLite → Postgres data migration (Epic M / MigrationTool).
- **Phase 2 (full unattended Windows install)** deferred — Phase 1 first.
