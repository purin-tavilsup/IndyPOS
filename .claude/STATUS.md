# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `indypos-overhaul` |
| **Sprint** | Sprint 7 |
| **Phase** | Local Deployment Readiness |
| **Blocked?** | No |

## Recent Completion (2026-04-03)

### Epic L: Local Deployment Readiness ✅ COMPLETE

| Task | Description | Status |
|------|-------------|--------|
| L1 | WinForms appsettings.json + remove `Enabled` flag | ✅ |
| L2 | StoreHub appsettings.Production.json + README | ✅ |
| L3 | publish.ps1 script | ✅ |
| L4 | install-config.ps1 script | ✅ |
| L5 | smoke-test.ps1 (comprehensive E2E) | ✅ |
| L6 | setup-local.md guide | ✅ |

### Bonus: Velopack Prep

| Task | Description | Status |
|------|-------------|--------|
| Version system | `Directory.Build.props`, `AppVersion.cs` | ✅ |
| Version endpoint | `GET /version` in StoreHub | ✅ |
| Bruno request | `get-version.bru` | ✅ |
| Versioning docs | `docs/versioning.md` | ✅ |

## Deployment Scenarios

| Scenario | Config | Guide |
|----------|--------|-------|
| **Development** | Aspire + Docker | `dotnet run --project src/IndyPOS.AppHost` |
| **Local Production** | PostgreSQL on Windows | `docs/operations/setup-local.md` |
| **Cloud** | DigitalOcean | Epic I (not started) |

## Next Actions (Priority Order)

### Ready for Pilot! 🚀

1. [ ] Run `publish.ps1` to build release binaries
2. [ ] Deploy to pilot store using `setup-local.md`
3. [ ] Run `smoke-test.ps1` to verify
4. [ ] Monitor and gather feedback

### Future (Epic I: Cloud Infrastructure)

- [ ] I1-I6: CloudApi deployment
- [ ] I7: Cloud setup guide

## Key Files

| Purpose | Path |
|---------|------|
| Full plan | `.planning/indypos-overhaul/PLAN.md` |
| Local setup guide | `docs/operations/setup-local.md` |
| Pilot checklist | `docs/operations/pilot-checklist.md` |
| Publish script | `scripts/publish.ps1` |
| Install script | `scripts/install-config.ps1` |
| Smoke test | `scripts/smoke-test.ps1` |
| Versioning | `docs/versioning.md` |

## Quick Context

IndyPOS StoreHub migration is **ready for pilot deployment**. Epic L (Local Deployment Readiness) is complete with:
- Production config files
- Automated scripts (publish, install, smoke test)
- Comprehensive setup guide
- Version system ready for future auto-update (Velopack)

## Stats

- **Tests:** 298 passing (211 unit)
- **Build:** 0 errors, 55 warnings
- **Bruno:** 26 requests (100% coverage)
- **Scripts:** 3 (publish, install-config, smoke-test)

---
*Last updated: 2026-04-03*
