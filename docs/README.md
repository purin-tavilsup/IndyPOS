# IndyPOS Documentation

## Quick Start

**New to the project?** Start with the [Developer Guide](development/getting-started.md)

## Documentation Map

```
docs/
├── architecture/           # System design
│   ├── overview.md        # Architecture overview with diagrams
│   └── store-identity.md  # Multi-store concepts
│
├── diagrams/               # ASCII art diagrams
│   ├── README.md          # Diagram index
│   ├── architecture-overview.md  # System components
│   ├── data-flow.md       # Business flows
│   └── flows.md           # Detailed technical flows ⭐
│
├── development/            # Developer resources
│   ├── getting-started.md # Setup and testing guide
│   └── docker-setup.md    # Docker/PostgreSQL setup
│
└── operations/             # Production operations
    ├── RUNBOOK.md         # Operations reference
    ├── pilot-checklist.md # Deployment checklist
    ├── smoke-test.ps1     # Health verification
    ├── health-check.ps1   # Scheduled monitoring
    ├── rollback-plan.md   # Emergency recovery
    ├── troubleshooting-guide.md  # Issue resolution
    ├── update-procedure.md      # Update process
    └── post-deployment-monitoring.md  # Metrics & alerts
```

## Key Documentation

### For Developers

| Document | Description |
|----------|-------------|
| [Developer Guide](development/getting-started.md) | Setup, testing, project structure |
| [Architecture Overview](architecture/overview.md) | System design and layers |
| [Flow Diagrams](diagrams/flows.md) | Startup, login, sale, sync flows |

### For Operations

| Document | Description |
|----------|-------------|
| [Operations Runbook](operations/RUNBOOK.md) | Daily operations reference |
| [Troubleshooting Guide](operations/troubleshooting-guide.md) | Common issues and fixes |
| [Pilot Checklist](operations/pilot-checklist.md) | Deployment steps |
| [Rollback Plan](operations/rollback-plan.md) | Emergency recovery |
| [Migration Tool Guide](../src/IndyPOS.MigrationTool/README.md) | SQLite → PostgreSQL migration |

## Quick Commands

```bash
# Run all tests (535 with Docker running; two suites REQUIRE it -- see ONBOARDING.md Trap 1)
dotnet test

# Run with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https

# Run WinForms connecting to Aspire
# 1. Start AppHost first
# 2. Then run WinForms
dotnet run --project src/IndyPOS.Windows.Forms
```

## Test Summary

See [`ONBOARDING.md`](../ONBOARDING.md) for the per-suite counts, which suites need Docker, and the
installer suite that sits outside `IndyPOS.sln`. Kept in one place so the numbers cannot drift apart.

## Planning Documents

For detailed planning, ADRs, and implementation tracking:

| Document | Location |
|----------|----------|
| Implementation Status | `.planning/indypos-overhaul/implementation-status.md` |
| Epic Plans | `.planning/indypos-overhaul/` |
| Security Spec | `.planning/indypos-overhaul/security/` |
| Detailed Diagrams | `.planning/indypos-overhaul/diagrams/` |
