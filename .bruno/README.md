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
│   │   ├── delete-product.bru
│   │   ├── create-stock-adjustment.bru
│   │   └── generate-barcode.bru
│   ├── payment-methods/
│   │   ├── list-offerable.bru
│   │   ├── list-catalogue.bru
│   │   ├── add-campaign.bru
│   │   └── update-method.bru
│   ├── sales/
│   │   ├── create-sale.bru
│   │   ├── create-sale-example.bru
│   │   ├── list-sales.bru
│   │   └── get-sale-detail.bru
│   ├── pay-later/
│   │   ├── list-pay-later.bru
│   │   ├── get-pay-later-detail.bru
│   │   └── create-payment.bru
│   ├── sync/
│   │   └── get-sync-status.bru
│   ├── reports/
│   │   ├── get-sales-summary.bru
│   │   ├── get-pay-later.bru
│   │   └── get-product-sales.bru
│   └── health/
│       ├── get-version.bru
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
   - **Create Sale** → Ring up a sale

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
| `paymentMethodCode` | Campaign code for add/update | Manual |

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
| POST | /products/{id}/stock-adjustments | Record a stock adjustment | Manager+ |
| POST | /products/next-barcode | Generate barcode | Manager+ |

### Payment Methods
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /payment-methods | What the till may offer | Cashier+ |
| GET | /payment-methods?include=all | The whole catalogue | Admin |
| POST | /payment-methods | Add a campaign method | Admin |
| PATCH | /payment-methods/{code} | Enable, disable or rename | Admin |

### Sales
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| POST | /sales | Ring up a sale (201 + Location) | Cashier+ |
| GET | /sales | List bills (defaults to today) | Cashier+ |
| GET | /sales/{id} | Bill detail by id | Cashier+ |
| GET | /sales/{number} | Bill detail by bill number | Cashier+ |
| POST | /sales/{id}/reprints | Record a reprint | Cashier+ |

### Pay Later (Credit)
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /pay-later | List credit records | Manager+ |
| GET | /pay-later/{id} | Get credit detail | Manager+ |
| POST | /pay-later/{id}/payments | Record a repayment | Cashier+ |

### Reports
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /reports/sales-summary | Sales summary | Manager+ |
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
