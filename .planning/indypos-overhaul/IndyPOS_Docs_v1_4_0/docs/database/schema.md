# PostgreSQL Schema (StoreHub)

Version: 1.3.0
Updated: 2026-02-28

## Identifier strategy
- Keep integer PKs if needed for legacy imports.
- Add `PublicId` UUID UNIQUE NOT NULL.
- New APIs and sync use `PublicId`.
