# IndyPOS Bruno Collection

API collection for testing IndyPOS StoreHub API using [Bruno](https://www.usebruno.com/).

## Setup

1. Install Bruno: https://www.usebruno.com/downloads
2. Open Bruno
3. Click "Open Collection"
4. Select this `.bruno` folder

## Structure

```
.bruno/
├── bruno.json              # Collection config
├── StoreHub/
│   ├── environments/
│   │   └── local.bru       # Local dev environment (localhost:5012)
│   ├── auth/
│   │   ├── login-cashier.bru
│   │   ├── login-manager.bru
│   │   └── login-admin.bru
│   ├── products/
│   │   ├── get-all-products.bru
│   │   └── get-product-by-barcode.bru
│   ├── sales/
│   │   ├── complete-sale.bru
│   │   └── complete-sale-example.bru
│   ├── sync/
│   │   └── get-sync-status.bru
│   └── health/
│       ├── health-ready.bru
│       └── health-live.bru
└── README.md
```

## Quick Start

1. Start the API:
   ```bash
   dotnet run --project src/IndyPOS.AppHost --launch-profile https
   ```

2. In Bruno, select "local" environment

3. Run requests in order:
   - **Login (Cashier)** → Sets `token` variable automatically
   - **Get All Products** → View available products
   - **Complete Sale** → Create a sale

## Test Users

| Username | Password    | Role     | Capabilities |
|----------|-------------|----------|--------------|
| cashier  | cashier123  | Cashier  | products, sales |
| manager  | manager123  | Manager  | + sync status |
| admin    | admin123    | Admin    | + all admin ops |

## Environment Variables

| Variable | Description | Set By |
|----------|-------------|--------|
| `baseUrl` | API base URL | Environment file |
| `token` | JWT auth token | Login requests (auto) |

## RBAC Testing

Test authorization by logging in as different users:

1. Login as **cashier**
2. Try **Get Sync Status** → Should get `403 Forbidden`
3. Login as **manager**
4. Try **Get Sync Status** → Should get `200 OK`

## Tips

- Token is auto-saved after login via post-response script
- Each request has documentation (click "Docs" tab)
- Requests can be run in sequence using Bruno Runner
