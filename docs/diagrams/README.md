# IndyPOS ASCII Diagrams

Visual documentation using ASCII art diagrams that render well in any editor.

## Diagrams in This Folder

| Diagram | Description |
|---------|-------------|
| [Architecture Overview](architecture-overview.md) | System components, Clean Architecture layers, Aspire setup |
| [Data Flow](data-flow.md) | Sale flow, PayLater, cash reconciliation, cloud sync |
| [Flows](flows.md) | **Detailed flows**: Startup, Login, Sale, Sync, Migration |

## Quick Reference

### System Architecture
```
┌──────────────┐     HTTP     ┌──────────────┐    HTTPS    ┌──────────────┐
│  WinForms    │─────────────►│  StoreHub    │────────────►│  Cloud API   │
│    POS       │              │    API       │             │              │
└──────────────┘              └──────┬───────┘             └──────────────┘
                                     │
                              ┌──────▼───────┐
                              │  PostgreSQL  │
                              │  (per store) │
                              └──────────────┘
```

### Development Setup
```bash
# Start everything with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost

# Run WinForms separately (connect to Aspire-hosted StoreHub)
dotnet run --project src/IndyPOS.Windows.Forms
```

### Key Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/auth/login` | POST | Authenticate user, get JWT |
| `/products` | GET | List products |
| `/sales/complete` | POST | Complete a sale |
| `/sync/status` | GET | Check sync status |
| `/health/ready` | GET | Health check |

## Diagram Index

### Startup & Setup
- [Application Startup Flow](flows.md#1-application-startup-flow) - Aspire and production modes

### Authentication
- [Login Flow](flows.md#2-login-flow) - BCrypt + JWT authentication

### Core Business
- [Sale Flow](flows.md#3-sale-flow) - Complete transaction with inventory
- [Product Management](flows.md#6-product-management-flow) - CRUD operations

### Synchronization
- [Sync Flow](flows.md#4-sync-flow) - Outbox pattern with retry

### Migration
- [Migration Flow](flows.md#5-migration-flow-sqlite--postgresql) - SQLite → PostgreSQL

## Additional Diagrams

For more detailed architectural diagrams, see:
`.planning/indypos-overhaul/diagrams/`

| # | Diagram | Description |
|---|---------|-------------|
| 01 | Architecture Overview | High-level system architecture |
| 02 | Data Flow | Sale and sync sequences |
| 03 | Entity Relationships | Domain entities |
| 06 | Epic Roadmap | Implementation phases |
| 09 | Terminal Concurrency | Multi-terminal safety |
| 10 | Aspire Dev Environment | Development setup |
| 11 | Solution Structure | Project dependencies |

## See Also

- [Architecture Overview](../architecture/overview.md)
- [Developer Guide](../development/getting-started.md)
- [Operations Runbook](../operations/RUNBOOK.md)
