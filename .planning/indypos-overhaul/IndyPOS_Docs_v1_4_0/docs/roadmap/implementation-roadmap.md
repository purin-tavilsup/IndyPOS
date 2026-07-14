# IndyPOS Implementation Roadmap
Version: 1.5.1

## Phase 1 - Foundation
Create new projects:

src/
  IndyPOS.StoreHub
  IndyPOS.CloudApi
  IndyPOS.AppHost
  IndyPOS.ServiceDefaults

Commands:

dotnet new webapi -n IndyPOS.StoreHub
dotnet new webapi -n IndyPOS.CloudApi
dotnet new aspire-apphost -n IndyPOS.AppHost
dotnet new aspire-servicedefaults -n IndyPOS.ServiceDefaults

Add EF Core PostgreSQL:

dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design

Add PublicId GUID to entities.

## Phase 2 - StoreHub API
Endpoints:
POST /sales/complete
GET /products
GET /health

## Phase 3 - Outbox Sync
Create table outbox_events and background worker to push events to cloud.

## Phase 4 - Cloud API
POST /sync/events
Idempotent ingestion using PublicId.

## Phase 5 - POS -> API
POS UI -> StoreHub API -> PostgreSQL

## Phase 6 - Reporting + MCP
Future services:
- MCP server
- reporting API
- analytics
