# ADR-003: Recommended IndyPOS Solution Structure

Status: Accepted
Date: 2026-02-28

## Decision
Adopt a repo structure with:
- shared `Domain`, `Application`, `Infrastructure`
- `Windows.Forms` retained during migration
- new `StoreHub` and `CloudApi` service projects
- Aspire `AppHost` and `ServiceDefaults`

## Rationale
This supports:
- offline-first local operations
- clean cloud sync services
- incremental migration
- better observability and developer workflow

## Consequences
- new projects added to the solution
- docs updated to reflect project boundaries
- Windows Forms becomes thinner over time
