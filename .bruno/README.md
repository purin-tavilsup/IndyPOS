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
│   │   ├── login-admin.bru
│   │   └── get-current-user.bru
│   ├── products/
│   │   ├── get-all-products.bru
│   │   ├── get-product-by-barcode.bru
│   │   ├── create-product.bru
│   │   ├── update-product.bru
│   │   └── delete-product.bru
│   ├── inventory/
│   │   ├── adjust-quantity.bru
│   │   └── generate-barcode.bru
│   ├── sales/
│   │   ├── complete-sale.bru
│   │   └── complete-sale-example.bru
│   ├── pay-later/
│   │   ├── list-pay-later.bru
│   │   ├── get-pay-later-detail.bru
│   │   └── record-payment.bru
│   ├── sync/
│   │   └── get-sync-status.bru
│   ├── reports/
│   │   ├── get-sales-summary.bru
│   │   ├── get-invoices.bru
│   │   ├── get-invoice-detail.bru
│   │   ├── get-pay-later.bru
│   │   └── get-product-sales.bru
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
   - **Login (Manager)** → Sets `token` variable automatically
   - **Get All Products** → View available products
   - **Complete Sale** → Create a sale

## Test Users

| Username | Password    | Role     | Capabilities |
|----------|-------------|----------|--------------|
| cashier  | cashier123  | Cashier  | products (read), sales |
| manager  | manager123  | Manager  | + products (write), inventory, sync, reports |
| admin    | admin123    | Admin    | + all admin operations |

## Environment Variables

| Variable | Description | Set By |
|----------|-------------|--------|
| `baseUrl` | API base URL | Environment file |
| `token` | JWT auth token | Login requests (auto) |
| `productId` | Product GUID for update/delete | Manual or script |
| `payLaterId` | Pay-later record GUID | Manual or script |

## API Endpoints

### Auth
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| POST | /auth/login | Login and get JWT token | Any |
| GET | /auth/me | Get current user info | Authenticated |

### Products
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /products | List all products | Cashier+ |
| POST | /products | Create product | Manager+ |
| PUT | /products/{id} | Update product | Manager+ |
| DELETE | /products/{id} | Soft delete product | Manager+ |

### Inventory
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| POST | /products/{id}/adjust-quantity | Adjust stock | Manager+ |
| POST | /products/next-barcode | Generate barcode | Manager+ |

### Sales
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| POST | /sales/complete | Complete a sale | Cashier+ |

### Pay Later (Credit)
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /pay-later | List credit records | Manager+ |
| GET | /pay-later/{id} | Get credit detail | Manager+ |
| POST | /pay-later/{id}/record-payment | Record payment | Cashier+ |

### Reports
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /reports/sales-summary | Sales summary | Manager+ |
| GET | /reports/invoices | List invoices | Manager+ |
| GET | /reports/invoices/{id} | Invoice detail | Manager+ |
| GET | /reports/pay-later | Pay-later report | Manager+ |
| GET | /reports/product-sales | Product sales | Manager+ |

### Sync & Health
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /sync/status | Sync status | Manager+ |
| GET | /health/ready | Readiness check | Any |
| GET | /health/live | Liveness check | Any |

## RBAC Testing

Test authorization by logging in as different users:

1. Login as **cashier**
2. Try **Create Product** → Should get `403 Forbidden`
3. Login as **manager**
4. Try **Create Product** → Should get `200 OK`

## Tips

- Token is auto-saved after login via post-response script
- Each request has documentation (click "Docs" tab)
- Requests can be run in sequence using Bruno Runner
- Set `productId` and `payLaterId` variables manually for update/delete operations
