# IndyPOS Flow Diagrams

This document contains detailed ASCII diagrams for all major flows in the IndyPOS system.

---

## Table of Contents

1. [Application Startup Flow](#1-application-startup-flow)
2. [Login Flow](#2-login-flow)
3. [Sale Flow](#3-sale-flow)
4. [Sync Flow](#4-sync-flow)
5. [Migration Flow](#5-migration-flow-sqlite--postgresql)
6. [Product Management Flow](#6-product-management-flow)

---

## 1. Application Startup Flow

### Development Mode (Aspire)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          DEVELOPER MACHINE                                   │
│                                                                              │
│   $ dotnet run --project src/IndyPOS.AppHost                                │
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                        IndyPOS.AppHost                               │   │
│   │                      (Aspire Orchestrator)                           │   │
│   └───────────────────────────────┬─────────────────────────────────────┘   │
│                                   │                                          │
│         ┌─────────────────────────┼─────────────────────────┐               │
│         │                         │                         │               │
│         ▼                         ▼                         ▼               │
│   ┌───────────┐           ┌───────────────┐         ┌───────────────┐       │
│   │ PostgreSQL│           │   StoreHub    │         │   CloudApi    │       │
│   │ Container │◄──────────│     API       │         │               │       │
│   │  :5432    │  EF Core  │    :5000      │         │    :5002      │       │
│   └───────────┘           └───────────────┘         └───────────────┘       │
│         │                         │                         │               │
│         │                         │                         │               │
│         ▼                         ▼                         ▼               │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                        Aspire Dashboard                              │   │
│   │                     https://localhost:17222                          │   │
│   │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐   │   │
│   │  │Resources│  │ Console │  │  Traces │  │ Metrics │  │Structured│   │   │
│   │  │         │  │  Logs   │  │         │  │         │  │  Logs   │   │   │
│   │  └─────────┘  └─────────┘  └─────────┘  └─────────┘  └─────────┘   │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  Run WinForms separately:                                            │   │
│   │  $ dotnet run --project src/IndyPOS.Windows.Forms                   │   │
│   │                                                                      │   │
│   │  Configure in appsettings.json:                                      │   │
│   │  {                                                                   │   │
│   │    "StoreHub": {                                                     │   │
│   │      "BaseUrl": "http://localhost:5000"                              │   │
│   │    }                                                                 │   │
│   │  }                                                                   │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Production Mode (Windows Service)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            STORE HUB PC                                      │
│                                                                              │
│   Windows Service Start                                                      │
│   ┌──────────────────────────────────────────────────────────────────────┐  │
│   │                                                                       │  │
│   │   ┌────────────────────────────────────────────────────────────────┐ │  │
│   │   │  1. Windows starts IndyPOS.StoreHub.exe as service             │ │  │
│   │   └────────────────────────────────────────────────────────────────┘ │  │
│   │                                   │                                   │  │
│   │                                   ▼                                   │  │
│   │   ┌────────────────────────────────────────────────────────────────┐ │  │
│   │   │  2. Load appsettings.json                                      │ │  │
│   │   │     - ConnectionStrings:StoreHub (PostgreSQL)                  │ │  │
│   │   │     - CloudApi:BaseUrl                                         │ │  │
│   │   │     - StoreIdentity:StoreId                                    │ │  │
│   │   └────────────────────────────────────────────────────────────────┘ │  │
│   │                                   │                                   │  │
│   │                                   ▼                                   │  │
│   │   ┌────────────────────────────────────────────────────────────────┐ │  │
│   │   │  3. Initialize Services                                        │ │  │
│   │   │     - StoreHubDbContext (EF Core)                              │ │  │
│   │   │     - Apply pending migrations                                 │ │  │
│   │   │     - Register repositories                                    │ │  │
│   │   │     - Register CQRS handlers                                   │ │  │
│   │   └────────────────────────────────────────────────────────────────┘ │  │
│   │                                   │                                   │  │
│   │                                   ▼                                   │  │
│   │   ┌────────────────────────────────────────────────────────────────┐ │  │
│   │   │  4. Start Background Services                                  │ │  │
│   │   │     - SyncWorker (polls outbox every 30s)                      │ │  │
│   │   │     - Health check endpoints                                   │ │  │
│   │   └────────────────────────────────────────────────────────────────┘ │  │
│   │                                   │                                   │  │
│   │                                   ▼                                   │  │
│   │   ┌────────────────────────────────────────────────────────────────┐ │  │
│   │   │  5. Start Kestrel Web Server                                   │ │  │
│   │   │     - Listen on http://0.0.0.0:5000                            │ │  │
│   │   │     - Ready to accept requests                                 │ │  │
│   │   └────────────────────────────────────────────────────────────────┘ │  │
│   │                                                                       │  │
│   └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│   POS Terminals can now connect                                              │
│   ┌────────────────┐     HTTP      ┌────────────────┐                       │
│   │ POS Terminal 1 │──────────────►│    StoreHub    │                       │
│   └────────────────┘               │    :5000       │                       │
│   ┌────────────────┐     HTTP      │                │                       │
│   │ POS Terminal 2 │──────────────►│                │                       │
│   └────────────────┘               └────────────────┘                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### WinForms Startup Sequence

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     WinForms Application Startup                             │
└─────────────────────────────────────────────────────────────────────────────┘

     ┌─────────────────┐
     │   Program.cs    │
     │   Main()        │
     └────────┬────────┘
              │
              ▼
     ┌─────────────────┐
     │ Load Settings   │
     │ appsettings.json│
     └────────┬────────┘
              │
              ▼
     ┌─────────────────────────────────────┐
     │    Check StoreHub.Enabled           │
     └────────────────┬────────────────────┘
                      │
          ┌───────────┴───────────┐
          │                       │
          ▼                       ▼
   ┌─────────────┐         ┌─────────────┐
   │ Enabled=true│         │Enabled=false│
   │  (StoreHub) │         │  (Legacy)   │
   └──────┬──────┘         └──────┬──────┘
          │                       │
          ▼                       ▼
   ┌─────────────────┐     ┌─────────────────┐
   │ Register:       │     │ Register:       │
   │ - IStoreHubClient     │ - SQLite repos  │
   │ - StoreHubSaleService │ - Legacy services
   │ - StoreHubAuthService │                 │
   │ - ProductCacheService │                 │
   └──────┬──────────┘     └──────┬──────────┘
          │                       │
          └───────────┬───────────┘
                      │
                      ▼
            ┌─────────────────┐
            │  Show Login     │
            │    Form         │
            └────────┬────────┘
                     │
                     ▼
            ┌─────────────────┐
            │ User enters     │
            │ credentials     │
            └────────┬────────┘
                     │
                     ▼
            ┌─────────────────────────────┐
            │ Call IUserLogInService      │
            │ .LogInAsync(user, pass)     │
            └─────────────┬───────────────┘
                          │
               ┌──────────┴──────────┐
               │                     │
               ▼                     ▼
        ┌─────────────┐       ┌─────────────┐
        │  StoreHub   │       │   Legacy    │
        │ POST /login │       │ SQLite auth │
        └──────┬──────┘       └──────┬──────┘
               │                     │
               └──────────┬──────────┘
                          │
                          ▼
               ┌─────────────────────┐
               │   Login Success?    │
               └──────────┬──────────┘
                          │
               ┌──────────┴──────────┐
               │                     │
               ▼                     ▼
        ┌─────────────┐       ┌─────────────┐
        │    YES      │       │     NO      │
        │ Cache token │       │ Show error  │
        │ Sync products       │ Retry       │
        │ Show MainForm       └─────────────┘
        └─────────────┘
```

---

## 2. Login Flow

### Detailed Authentication Sequence

```
 WinForms          StoreHubClient       StoreHub API        PostgreSQL
    │                    │                    │                  │
    │ Login(user, pass)  │                    │                  │
    ├───────────────────►│                    │                  │
    │                    │                    │                  │
    │                    │ POST /auth/login   │                  │
    │                    │ {username, pass}   │                  │
    │                    ├───────────────────►│                  │
    │                    │                    │                  │
    │                    │                    │ SELECT user      │
    │                    │                    │ WHERE username=? │
    │                    │                    ├─────────────────►│
    │                    │                    │                  │
    │                    │                    │◄─────────────────┤
    │                    │                    │ StoreUser record │
    │                    │                    │                  │
    │                    │           ┌────────┴────────┐         │
    │                    │           │ BCrypt.Verify   │         │
    │                    │           │ (pass, hash)    │         │
    │                    │           └────────┬────────┘         │
    │                    │                    │                  │
    │                    │         ┌──────────┴──────────┐       │
    │                    │         │                     │       │
    │                    │         ▼                     ▼       │
    │                    │   ┌──────────┐         ┌──────────┐   │
    │                    │   │ VALID    │         │ INVALID  │   │
    │                    │   └────┬─────┘         └────┬─────┘   │
    │                    │        │                    │         │
    │                    │        ▼                    │         │
    │                    │  ┌───────────────┐          │         │
    │                    │  │ Generate JWT  │          │         │
    │                    │  │ (15min exp)   │          │         │
    │                    │  │               │          │         │
    │                    │  │ Claims:       │          │         │
    │                    │  │ - sub: userId │          │         │
    │                    │  │ - name        │          │         │
    │                    │  │ - role        │          │         │
    │                    │  │ - capabilities│          │         │
    │                    │  └───────┬───────┘          │         │
    │                    │          │                  │         │
    │                    │◄─────────┴──────────────────┤         │
    │                    │  200 OK / 401 Unauthorized  │         │
    │                    │                             │         │
    │◄───────────────────┤                             │         │
    │ LoginResult        │                             │         │
    │ - Token            │                             │         │
    │ - User             │                             │         │
    │ - Capabilities     │                             │         │
    │                    │                             │         │
    │  ┌─────────────────┴───────┐                     │         │
    │  │ Cache token in memory   │                     │         │
    │  │ for subsequent requests │                     │         │
    │  └─────────────────────────┘                     │         │
    │                                                  │         │
```

### JWT Token Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           JWT TOKEN                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  HEADER (Algorithm + Type)                                                   │
│  {                                                                           │
│    "alg": "RS256",                                                          │
│    "typ": "JWT"                                                             │
│  }                                                                           │
│                                                                              │
│  PAYLOAD (Claims)                                                            │
│  {                                                                           │
│    "sub": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",  // User ID (Guid)        │
│    "name": "cashier",                               // Username              │
│    "role": "Cashier",                               // Role name             │
│    "roleId": "1",                                   // Role ID               │
│    "cap": ["products.read", "sales.complete"],      // Capabilities          │
│    "iat": 1709251200,                               // Issued at             │
│    "exp": 1709252100,                               // Expires (15 min)      │
│    "iss": "IndyPOS.StoreHub"                        // Issuer                │
│  }                                                                           │
│                                                                              │
│  SIGNATURE                                                                   │
│  RSASHA256(base64(header) + "." + base64(payload), privateKey)              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Sale Flow

### Complete Sale Transaction

```
 POS Terminal       StoreHub API        PostgreSQL           SyncWorker        Cloud API
      │                  │                  │                    │                 │
      │ POST /sales/complete               │                    │                 │
      │ {userId, lines, payments}           │                    │                 │
      ├─────────────────►│                  │                    │                 │
      │                  │                  │                    │                 │
      │                  │ BEGIN TRANSACTION│                    │                 │
      │                  ├─────────────────►│                    │                 │
      │                  │                  │                    │                 │
      │                  │                  │                    │                 │
      │                  │  ┌───────────────┴───────────────┐    │                 │
      │                  │  │ 1. Generate Invoice Number     │    │                 │
      │                  │  │    (sequential, store-specific) │    │                 │
      │                  │  └───────────────┬───────────────┘    │                 │
      │                  │                  │                    │                 │
      │                  │  ┌───────────────┴───────────────┐    │                 │
      │                  │  │ 2. INSERT Invoice              │    │                 │
      │                  │  │    - Id (Guid)                 │    │                 │
      │                  │  │    - InvoiceNumber             │    │                 │
      │                  │  │    - TotalAmount               │    │                 │
      │                  │  │    - UserId                    │    │                 │
      │                  │  │    - CreatedUtc                │    │                 │
      │                  │  └───────────────┬───────────────┘    │                 │
      │                  │                  │                    │                 │
      │                  │  ┌───────────────┴───────────────┐    │                 │
      │                  │  │ 3. INSERT InvoiceLines         │    │                 │
      │                  │  │    (for each product in cart)  │    │                 │
      │                  │  │    - ProductId                 │    │                 │
      │                  │  │    - Quantity                  │    │                 │
      │                  │  │    - UnitPrice                 │    │                 │
      │                  │  │    - ProductName (snapshot)    │    │                 │
      │                  │  └───────────────┬───────────────┘    │                 │
      │                  │                  │                    │                 │
      │                  │  ┌───────────────┴───────────────┐    │                 │
      │                  │  │ 4. INSERT Payments             │    │                 │
      │                  │  │    - Method (Cash/PayLater)    │    │                 │
      │                  │  │    - Amount                    │    │                 │
      │                  │  └───────────────┬───────────────┘    │                 │
      │                  │                  │                    │                 │
      │                  │  ┌───────────────┴───────────────┐    │                 │
      │                  │  │ 5. INSERT InventoryMovements   │    │                 │
      │                  │  │    (for each line)             │    │                 │
      │                  │  │    - ProductId                 │    │                 │
      │                  │  │    - Quantity (negative)       │    │                 │
      │                  │  │    - MovementType: Sale        │    │                 │
      │                  │  └───────────────┬───────────────┘    │                 │
      │                  │                  │                    │                 │
      │                  │  ┌───────────────┴───────────────┐    │                 │
      │                  │  │ 6. INSERT OutboxEvent          │    │                 │
      │                  │  │    - EventType: InvoiceCompleted   │                 │
      │                  │  │    - Payload: Full invoice JSON    │                 │
      │                  │  │    - SentAt: null              │    │                 │
      │                  │  └───────────────┬───────────────┘    │                 │
      │                  │                  │                    │                 │
      │                  │ COMMIT           │                    │                 │
      │                  ├─────────────────►│                    │                 │
      │                  │                  │                    │                 │
      │ 200 OK           │                  │                    │                 │
      │ {invoiceId, num} │                  │                    │                 │
      │◄─────────────────┤                  │                    │                 │
      │                  │                  │                    │                 │
      │                  │                  │    (Background)    │                 │
      │                  │                  │                    │                 │
      │                  │                  │ SELECT unsent      │                 │
      │                  │                  │ FROM OutboxEvent   │                 │
      │                  │                  │◄───────────────────┤                 │
      │                  │                  │                    │                 │
      │                  │                  │                    │ POST /sync/events
      │                  │                  │                    ├────────────────►│
      │                  │                  │                    │                 │
      │                  │                  │                    │     200 OK      │
      │                  │                  │                    │◄────────────────┤
      │                  │                  │                    │                 │
      │                  │                  │ UPDATE SentAt=now  │                 │
      │                  │                  │◄───────────────────┤                 │
      │                  │                  │                    │                 │
```

### Inventory Movement Strategy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    INVENTORY MOVEMENT TRACKING                               │
│                                                                              │
│   Why movements instead of "SET quantity = X"?                              │
│   - Audit trail: Every change is recorded                                   │
│   - Concurrent safety: No race conditions                                   │
│   - History: Can reconstruct stock at any point in time                     │
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   Product A: Initial Stock = 100                                            │
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ InventoryMovements Table                                             │   │
│   ├──────────────┬──────────┬────────────┬─────────────┬────────────────┤   │
│   │ Timestamp    │ ProductId│ Quantity   │ MovementType│ Reference      │   │
│   ├──────────────┼──────────┼────────────┼─────────────┼────────────────┤   │
│   │ 2024-01-01   │ A        │ +100       │ Initial     │ Setup          │   │
│   │ 2024-01-02   │ A        │  -5        │ Sale        │ INV-001        │   │
│   │ 2024-01-02   │ A        │  -3        │ Sale        │ INV-002        │   │
│   │ 2024-01-03   │ A        │ +20        │ Restock     │ PO-001         │   │
│   │ 2024-01-03   │ A        │  -2        │ Adjustment  │ Damaged        │   │
│   └──────────────┴──────────┴────────────┴─────────────┴────────────────┘   │
│                                                                              │
│   Current Stock = SUM(Quantity) = 100 - 5 - 3 + 20 - 2 = 110               │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Sync Flow

### Outbox Pattern with Retry

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SYNC WORKER FLOW                                     │
└─────────────────────────────────────────────────────────────────────────────┘

     ┌─────────────────────────────────────────────────────────────────────┐
     │                       SyncWorker (BackgroundService)                 │
     │                                                                      │
     │   while (true)                                                       │
     │   {                                                                  │
     │       await Task.Delay(30_000); // Poll every 30 seconds            │
     │       var events = await GetPendingEvents();                        │
     │       foreach (var evt in events)                                   │
     │       {                                                             │
     │           await SendToCloud(evt);                                   │
     │       }                                                             │
     │   }                                                                  │
     └─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
     ┌─────────────────────────────────────────────────────────────────────┐
     │   GetPendingEvents()                                                 │
     │                                                                      │
     │   SELECT * FROM OutboxEvents                                        │
     │   WHERE SentAt IS NULL                                              │
     │   ORDER BY CreatedUtc                                               │
     │   LIMIT 100                                                         │
     └────────────────────────────────┬────────────────────────────────────┘
                                      │
                                      ▼
                         ┌────────────────────────┐
                         │   For each event:      │
                         └────────────┬───────────┘
                                      │
                                      ▼
     ┌─────────────────────────────────────────────────────────────────────┐
     │   POST https://cloud-api/sync/events                                │
     │                                                                      │
     │   Headers:                                                          │
     │     Authorization: Bearer {jwt_token}                               │
     │     Content-Type: application/json                                  │
     │                                                                      │
     │   Body:                                                             │
     │   {                                                                 │
     │     "eventId": "guid",                                              │
     │     "eventType": "InvoiceCompleted",                                │
     │     "storeId": "STORE-001",                                         │
     │     "payload": { ... full invoice data ... },                       │
     │     "occurredUtc": "2024-01-15T10:30:00Z"                          │
     │   }                                                                 │
     └────────────────────────────────┬────────────────────────────────────┘
                                      │
                           ┌──────────┴──────────┐
                           │                     │
                           ▼                     ▼
                    ┌─────────────┐       ┌─────────────┐
                    │  SUCCESS    │       │  FAILURE    │
                    │  (200 OK)   │       │ (5xx/timeout)│
                    └──────┬──────┘       └──────┬──────┘
                           │                     │
                           ▼                     ▼
            ┌─────────────────────┐  ┌─────────────────────────────┐
            │ UPDATE OutboxEvents │  │ Exponential Backoff Retry   │
            │ SET SentAt = NOW()  │  │                             │
            │ WHERE Id = {id}     │  │ Retry 1: wait 30s           │
            │                     │  │ Retry 2: wait 60s           │
            └─────────────────────┘  │ Retry 3: wait 120s          │
                                     │ Retry 4: wait 240s          │
                                     │ Retry 5: give up (log error)│
                                     └─────────────────────────────┘
```

### Cloud API Event Processing

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CLOUD API - EVENT INGESTION                               │
└─────────────────────────────────────────────────────────────────────────────┘

     ┌───────────────────────────────────────────────────────────────────┐
     │   POST /sync/events                                                │
     │                                                                    │
     │   1. Validate JWT token (signature, expiry, issuer)               │
     │   2. Extract storeId from claims                                  │
     │   3. Check eventId not already processed (idempotency)            │
     └────────────────────────────────┬──────────────────────────────────┘
                                      │
                                      ▼
                         ┌────────────────────────┐
                         │ Already processed?     │
                         └────────────┬───────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │                                   │
                    ▼                                   ▼
             ┌─────────────┐                     ┌─────────────┐
             │    YES      │                     │     NO      │
             │ Return 200  │                     │ Process it  │
             │ (idempotent)│                     │             │
             └─────────────┘                     └──────┬──────┘
                                                        │
                                                        ▼
     ┌───────────────────────────────────────────────────────────────────┐
     │   INSERT INTO SyncedEvents                                        │
     │   {eventId, eventType, storeId, payload, receivedUtc}            │
     └────────────────────────────────┬──────────────────────────────────┘
                                      │
                                      ▼
     ┌───────────────────────────────────────────────────────────────────┐
     │   EventProcessor (BackgroundService)                              │
     │                                                                    │
     │   Polls SyncedEvents WHERE processed = false                      │
     │                                                                    │
     │   For InvoiceCompleted:                                           │
     │   - INSERT CloudInvoice                                           │
     │   - INSERT CloudInvoiceLines                                      │
     │   - INSERT CloudPayments                                          │
     │   - INSERT CloudInventoryMovements                                │
     │   - INSERT ProcessedEvents (idempotency record)                   │
     └───────────────────────────────────────────────────────────────────┘
```

---

## 5. Migration Flow (SQLite → PostgreSQL)

### Data Migration Process

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SQLite → PostgreSQL MIGRATION                             │
│                                                                              │
│   Pre-Migration State:                                                       │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ SQLite (Store.db)                                                    │   │
│   │ - InventoryProducts (int Id, barcode, price, qty)                   │   │
│   │ - Invoices (int Id, invoice_number, total)                          │   │
│   │ - InvoiceProducts (int Id, product_id, qty, price)                  │   │
│   │ - Users (int Id, username, password_hash)                           │   │
│   │                                                                      │   │
│   │ Note: Uses int IDs, TripleDES passwords, direct qty tracking        │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                                    │
                                    ▼

┌─────────────────────────────────────────────────────────────────────────────┐
│                         MIGRATION STEPS                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   STEP 1: Pre-Migration Backup                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ Copy-Item Store.db Store.db.pre_migration                           │   │
│   │ Document: product count, invoice count, user count                  │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   STEP 2: Read SQLite Data                                                  │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ MigrationService reads all:                                          │   │
│   │ - Products (with current quantities)                                │   │
│   │ - Invoices + InvoiceProducts + Payments                             │   │
│   │ - Users                                                             │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   STEP 3: Transform & Map IDs                                               │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ For each product:                                                    │   │
│   │   - Generate new Guid ID                                            │   │
│   │   - Store mapping: legacyIntId → newGuidId                          │   │
│   │   - Create initial InventoryMovement for current qty                │   │
│   │                                                                      │   │
│   │ For each invoice:                                                    │   │
│   │   - Generate new Guid ID                                            │   │
│   │   - Map product IDs in lines using mapping table                    │   │
│   │   - Create InventoryMovements (negative) for each line              │   │
│   │                                                                      │   │
│   │ For each user:                                                       │   │
│   │   - Generate new Guid ID                                            │   │
│   │   - Rehash password with BCrypt (if TripleDES detected)             │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   STEP 4: Write to PostgreSQL                                               │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ BEGIN TRANSACTION                                                    │   │
│   │                                                                      │   │
│   │ INSERT Products (with Guid IDs)                                     │   │
│   │ INSERT InventoryMovements (initial stock)                           │   │
│   │ INSERT Invoices                                                     │   │
│   │ INSERT InvoiceLines (with mapped ProductIds)                        │   │
│   │ INSERT Payments                                                     │   │
│   │ INSERT InventoryMovements (sales deductions)                        │   │
│   │ INSERT StoreUsers (with BCrypt hashes)                              │   │
│   │                                                                      │   │
│   │ COMMIT                                                               │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   STEP 5: Verification                                                      │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ Assert:                                                              │   │
│   │   - Product count matches                                           │   │
│   │   - Invoice count matches                                           │   │
│   │   - User count matches                                              │   │
│   │   - Total stock quantities match                                    │   │
│   │   - Invoice totals match                                            │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                                    │
                                    ▼

┌─────────────────────────────────────────────────────────────────────────────┐
│   Post-Migration State:                                                      │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ PostgreSQL (indypos_storehub)                                        │   │
│   │ - Products (Guid Id, barcode, price, IsActive)                      │   │
│   │ - Invoices (Guid Id, invoice_number, total)                         │   │
│   │ - InvoiceLines (Guid Id, product_id (FK), qty, price)               │   │
│   │ - Payments (Guid Id, method, amount)                                │   │
│   │ - InventoryMovements (Guid Id, product_id, qty, type)               │   │
│   │ - StoreUsers (Guid Id, username, BCrypt hash, role)                 │   │
│   │                                                                      │   │
│   │ Note: Uses Guid IDs, BCrypt passwords, movement-based tracking      │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### ID Mapping Strategy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ID MAPPING TABLE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   During migration, we maintain a mapping:                                  │
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ Dictionary<int, Guid> productIdMap                                   │   │
│   │                                                                      │   │
│   │ SQLite ID (int) │ PostgreSQL ID (Guid)                              │   │
│   │ ────────────────┼───────────────────────────────────────────────    │   │
│   │       1         │ a1b2c3d4-e5f6-7890-abcd-ef1234567890              │   │
│   │       2         │ b2c3d4e5-f6a7-8901-bcde-f12345678901              │   │
│   │       3         │ c3d4e5f6-a7b8-9012-cdef-123456789012              │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   This mapping is used when:                                                │
│   - Creating InvoiceLines (ProductId FK)                                    │
│   - Creating InventoryMovements (ProductId FK)                              │
│                                                                              │
│   WinForms Compatibility:                                                   │
│   - LegacyIdHelper extracts first 4 bytes of Guid for display             │
│   - Example: a1b2c3d4-... → displays as "2718547924" in legacy UI          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Migration Tool CLI

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      MIGRATION TOOL COMMANDS                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   1. DRY RUN (Validate Data)                                                │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  IndyPOS.MigrationTool.exe                                          │   │
│   │    --sqlite "C:\ProgramData\IndyPOS\db\Store.db"                   │   │
│   │    --postgres "Host=127.0.0.1;Database=indypos;..."                │   │
│   │    --store-id "STORE-001"                                          │   │
│   │    --dry-run                                                        │   │
│   │                                                                      │   │
│   │  Output: Shows what WOULD be migrated without making changes        │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   2. EXECUTE MIGRATION                                                       │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  IndyPOS.MigrationTool.exe                                          │   │
│   │    --sqlite "C:\ProgramData\IndyPOS\db\Store.db"                   │   │
│   │    --postgres "Host=127.0.0.1;Database=indypos;..."                │   │
│   │    --store-id "STORE-001"                                          │   │
│   │                                                                      │   │
│   │  Output:                                                            │   │
│   │  ┌───────────┬──────────┬─────────┬────────┐                        │   │
│   │  │ Entity    │ Migrated │ Skipped │ Failed │                        │   │
│   │  ├───────────┼──────────┼─────────┼────────┤                        │   │
│   │  │ Users     │       5  │       0 │      0 │                        │   │
│   │  │ Products  │     150  │       0 │      0 │                        │   │
│   │  │ Invoices  │   1,234  │       0 │      0 │                        │   │
│   │  │ Payments  │   1,500  │       0 │      0 │                        │   │
│   │  │ PayLater  │      12  │       0 │      0 │                        │   │
│   │  └───────────┴──────────┴─────────┴────────┘                        │   │
│   │                                                                      │   │
│   │  ✓ Migration completed successfully!                                │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   3. VERIFY MIGRATION                                                        │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  IndyPOS.MigrationTool.exe verify                                   │   │
│   │    --sqlite "C:\ProgramData\IndyPOS\db\Store.db"                   │   │
│   │    --postgres "Host=127.0.0.1;Database=indypos;..."                │   │
│   │    --store-id "STORE-001"                                          │   │
│   │                                                                      │   │
│   │  Output:                                                            │   │
│   │  ┌───────────────┬─────────┬────────────┬────────┐                  │   │
│   │  │ Entity        │ SQLite  │ PostgreSQL │ Status │                  │   │
│   │  ├───────────────┼─────────┼────────────┼────────┤                  │   │
│   │  │ Users         │       5 │          5 │   ✓    │                  │   │
│   │  │ Products      │     150 │        150 │   ✓    │                  │   │
│   │  │ Invoices      │   1,234 │      1,234 │   ✓    │                  │   │
│   │  │ Invoice Lines │   3,500 │      3,500 │   ✓    │                  │   │
│   │  │ Payments      │   1,500 │      1,500 │   ✓    │                  │   │
│   │  │ PayLater      │      12 │         12 │   ✓    │                  │   │
│   │  │ Total Revenue │$125,000 │   $125,000 │   ✓    │                  │   │
│   │  └───────────────┴─────────┴────────────┴────────┘                  │   │
│   │                                                                      │   │
│   │  ✓ Migration verification passed!                                   │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   4. MIGRATE WITH CLOUD SYNC                                                 │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  IndyPOS.MigrationTool.exe                                          │   │
│   │    --sqlite "..."                                                   │   │
│   │    --postgres "..."                                                 │   │
│   │    --store-id "STORE-001"                                          │   │
│   │    --cloud-api "https://cloud.indypos.app"                         │   │
│   │    --client-id "<OAUTH_CLIENT_ID>"                                 │   │
│   │    --client-secret "<OAUTH_CLIENT_SECRET>"                         │   │
│   │                                                                      │   │
│   │  Flow:                                                              │   │
│   │  1. Migrate SQLite → PostgreSQL (local)                            │   │
│   │  2. Get OAuth2 token from Cloud API                                │   │
│   │  3. POST /sync/bulk-migration with all migrated data               │   │
│   │  4. Cloud imports: Users, Products, Invoices                       │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Product Management Flow

### Create Product Flow

```
 WinForms           IInventoryProductService    StoreHubClient      StoreHub API
    │                         │                       │                   │
    │ CreateProduct           │                       │                   │
    │ (name, barcode, price)  │                       │                   │
    ├────────────────────────►│                       │                   │
    │                         │                       │                   │
    │                         │ POST /products        │                   │
    │                         │ {name, barcode, ...}  │                   │
    │                         ├──────────────────────►│                   │
    │                         │                       │                   │
    │                         │                       │ POST /products    │
    │                         │                       ├──────────────────►│
    │                         │                       │                   │
    │                         │                       │    ┌──────────────┴──────┐
    │                         │                       │    │ Validate:           │
    │                         │                       │    │ - Barcode unique?   │
    │                         │                       │    │ - Name not empty?   │
    │                         │                       │    │ - Price >= 0?       │
    │                         │                       │    └──────────────┬──────┘
    │                         │                       │                   │
    │                         │                       │    ┌──────────────┴──────┐
    │                         │                       │    │ INSERT Product      │
    │                         │                       │    │ INSERT Movement     │
    │                         │                       │    │ (initial qty)       │
    │                         │                       │    └──────────────┬──────┘
    │                         │                       │                   │
    │                         │                       │◄──────────────────┤
    │                         │                       │ 201 Created       │
    │                         │                       │ {id, barcode}     │
    │                         │◄──────────────────────┤                   │
    │                         │                       │                   │
    │                         │  ┌────────────────────┴───────┐           │
    │                         │  │ Invalidate product cache   │           │
    │                         │  │ (force re-sync on next use)│           │
    │                         │  └────────────────────────────┘           │
    │                         │                                           │
    │◄────────────────────────┤                                           │
    │ ProductDto              │                                           │
    │                         │                                           │
```

### Adjust Quantity Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ADJUST PRODUCT QUANTITY                                   │
│                                                                              │
│   Use cases:                                                                │
│   - Restock from supplier                                                   │
│   - Damaged goods write-off                                                 │
│   - Inventory count adjustment                                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

 WinForms           StoreHubClient           StoreHub API          PostgreSQL
    │                      │                       │                    │
    │ AdjustQuantity       │                       │                    │
    │ (productId, +10,     │                       │                    │
    │  reason: "Restock")  │                       │                    │
    ├─────────────────────►│                       │                    │
    │                      │                       │                    │
    │                      │ POST /products/{id}/adjust-quantity        │
    │                      │ {quantity: 10, reason: "Restock"}          │
    │                      ├──────────────────────►│                    │
    │                      │                       │                    │
    │                      │                       │ INSERT INTO        │
    │                      │                       │ InventoryMovements │
    │                      │                       │ (productId, +10,   │
    │                      │                       │  type: Adjustment) │
    │                      │                       ├───────────────────►│
    │                      │                       │                    │
    │                      │                       │◄───────────────────┤
    │                      │                       │                    │
    │                      │◄──────────────────────┤                    │
    │                      │ 200 OK                │                    │
    │                      │ {newQuantity: 45}     │                    │
    │                      │                       │                    │
    │◄─────────────────────┤                       │                    │
    │ Success              │                       │                    │
    │                      │                       │                    │

    Note: Quantity is calculated as SUM(movements), not stored directly
    Current qty = 35 + 10 (new adjustment) = 45
```

---

## Related Diagrams

- [Architecture Overview](architecture-overview.md) - System components and dependencies
- [Data Flow](data-flow.md) - High-level data flow diagrams

## See Also

- [Developer Guide](../development/getting-started.md) - How to run and test
- [Operations Runbook](../operations/RUNBOOK.md) - Production operations
- [Troubleshooting Guide](../operations/troubleshooting-guide.md) - Common issues
