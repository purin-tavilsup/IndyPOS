# IndyPOS Documentation

## Quick Start

**New to the project?** Start with the [Developer Guide](development/getting-started.md)

## Architecture

- [Overview](architecture/overview.md) - High-level system architecture
- [Store Identity](architecture/store-identity.md) - Multi-store identity concept

## Diagrams

- [Architecture Overview](diagrams/architecture-overview.md) - Visual system diagram with ASCII art
- [Data Flow](diagrams/data-flow.md) - How data flows through the system

## Development

- [Getting Started](development/getting-started.md) - **Comprehensive setup & testing guide**
- [Docker Setup](development/docker-setup.md) - Local Postgres with Docker

## Testing Quick Reference

```bash
# Run all tests (179 tests, no Docker needed)
dotnet test tests/IndyPOS.Application.Tests/

# Run with Aspire for manual E2E testing (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

## Detailed Planning

For detailed planning documents, ADRs, and implementation guides, see:
- `.planning/indypos-overhaul/` - Full overhaul planning documentation
- `.planning/indypos-overhaul/implementation-status.md` - Current implementation status
