# Epic F: Cloud API - COMPLETE

**Completed:** 2026-03-09

## Goal
Central cloud API + PostgreSQL (Singapore)

## Tasks Completed

| Task | Description |
|------|-------------|
| F1 | Create IndyPOS.CloudApi project |
| F1a | Add CloudApi to AppHost |
| F2 | Idempotent event ingestion |
| F3 | Process event types |
| F4 | Master data endpoints |
| F5 | OAuth2 + OpenIddict auth |

## Implementation Details

**F1-F2: CloudApi Foundation**
- Created `IndyPOS.CloudApi` project (ASP.NET Core, net10.0)
- Added Scalar API documentation UI
- Implemented `POST /sync/events` with idempotent ingestion

**F3: Event Processing**
- Defined `InvoiceCompletedEvent` contract with schema versioning
- Rich transaction snapshot: invoice header, lines, payments, inventory movements
- Created Cloud domain entities (CloudInvoice, CloudInvoiceLine, CloudPayment, CloudInventoryMovement)
- Added `EventProcessor` BackgroundService

**F4: Master Data Endpoints**
- `GET /master/products` with filtering (activeOnly, modifiedSince)
- `GET /master/config/{storeId}` for store configuration

**F5: OAuth2 + OpenIddict**
- Client Credentials flow
- Token endpoint at /oauth/token
- 15-minute access tokens, 24-hour refresh tokens
- Scopes: sync.write, master.read
- Store registration with BCrypt-hashed secrets
- CloudTokenService for StoreHub token caching
- HttpCloudSyncClient with Bearer auth

## Deliverable
Cloud accepts authenticated events; stores them; supports master data pull
