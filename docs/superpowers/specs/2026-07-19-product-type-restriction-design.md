# Product-Type Restriction by Store Type — Design Spec

**Date:** 2026-07-19
**Status:** Approved (brainstorming) → ready for implementation plan
**Related:** Epic M data-driven payment methods (`2026-07-18-data-driven-payment-methods-design.md`) — this is its sibling "make StoreType real" feature for products.

## Goal

On a **Minimart** store, restrict the catalog to **General Goods only** — hide the Hardware entry points in the UI and reject Hardware product creation server-side. **GeneralHardware** stores are unchanged (both General Goods and Hardware). This enforces the already-existing `StoreTypeFeatures.MultipleProductTypesEnabled` flag, which today is defined but **not enforced anywhere**.

## Why

`StoreTypeFeatures` already declares `MultipleProductTypesEnabled` (`true` for GeneralHardware, `false` for Minimart/CoffeeShop), but nothing consumes it: the sale screen always shows both **Add General Goods** and **Add Hardware** buttons, product creation always offers both categories, and the server never checks. A Minimart therefore behaves like a full GeneralHardware store. This feature closes that gap so a Minimart genuinely cannot deal in Hardware.

## Scope

**In scope — supported store types: GeneralHardware and Minimart.**
- GeneralHardware: General Goods + Hardware (unchanged).
- Minimart: General Goods only (Hardware hidden + server-rejected).

**Out of scope / deferred:**
- **CoffeeShop** — decision (Pond): a coffee shop's order model (made-to-order items with add-ons/modifiers like milk, cream, sugar) is fundamentally different from barcode-inventory retail and will be its **own separate application**, not an IndyPOS store type. The `StoreType.CoffeeShop` enum value is left in place (harmless; `StoreTypeFeatures.For` already maps it to general-only, so if ever installed it safely behaves like Minimart), but no CoffeeShop-specific work is done here.
- Reports / CashFlow "hardware totals" columns for general-only stores (cosmetic; deferred).
- A data-driven product-type catalog (rejected as over-engineering — only two fixed types exist and CoffeeShop is leaving).

## Background — current model (verified in code)

- `StoreTypeFeatures` (`IndyPOS.Domain.ValueObjects`): record with `PayLaterEnabled` + `MultipleProductTypesEnabled`; `StoreTypeFeatures.For(StoreType)` maps GeneralHardware→both true, Minimart→both false, CoffeeShop→both false. **No change needed.**
- `ProductCategory` enum (`IndyPOS.Application.Common.Enums`): `GeneralGoods = 10`, `Hardware = 50`. Category is stored on `Product.Category` as a **string** (the category name, e.g. `"Hardware"` / `"GeneralGoods"`; `HardcodedStoreConstants.ProductCategories` maps int→`nameof`).
- Sale screen (`SalePanel`): fixed buttons `AddGeneralGoodsProductButton` (template barcode `2001000000012`) and `AddHardwareProductButton` (template barcode `2005000000027`); no store-type gating.
- Product creation (`AddNewInventoryProductForm`, `AddNewInventoryProductWithCustomBarcodeForm`): category selector offering both categories.
- `CreateProductCommand` has `string? Category`; `CreateProductCommandHandler` **already injects `IStoreIdentityService`** and sets `Category = command.Category`.
- **WinForms has no store-type awareness today** — `StoreConfiguration.json` deliberately excludes Type ("see StoreIdentityOptions", which is server-side), and there is no store/features endpoint. `IStoreIdentityService` (with `.Features`) lives server-side only. WinForms and StoreHub are separate processes.

## Design

### 1. Server — store-features endpoint (how WinForms learns the flag)

Add `GET /store/features` in `IndyPOS.StoreHub/Program.cs`, returning the current store's feature flags computed from `IStoreIdentityService.Features`:

```
GET /store/features  (RequireAuthorization — any authenticated user)
→ 200 { "payLaterEnabled": bool, "multipleProductTypesEnabled": bool }
```

`IStoreIdentityService` is a registered **singleton** exposing `.Features` (a `StoreTypeFeatures`), so this is a **plain inline minimal-API endpoint** — no repository, no DB, no Nokpirab query handler (this is pure configuration, not a data query). Response DTO: `StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled)` in the Application layer (so both StoreHub and the WinForms client can share it). This mirrors the payment-methods principle that the **server is the source of truth for store-type gating** and WinForms learns it over HTTP.

### 2. Server — guard in `CreateProductCommandHandler`

Before persisting, reject a Hardware product on a general-only store:

- If `command.Category` equals the Hardware category name (`nameof(ProductCategory.Hardware)` — implementation confirms the exact stored string) **and** `!_storeIdentityService.Features.MultipleProductTypesEnabled` → throw `InvalidOperationException` with a clear message.
- The `/products` POST endpoint surfaces it via the codebase's existing per-endpoint exception→result convention (e.g. `Results.Conflict`/`BadRequest`, matching how other product/domain exceptions are surfaced).

This is the real enforcement boundary: even a direct API call cannot create a Hardware product on a Minimart. `IStoreIdentityService` is already injected into the handler, so no new dependency.

### 3. WinForms — consume the flag

- **Client:** add `Task<StoreFeaturesDto> GetStoreFeaturesAsync(CancellationToken)` to `IStoreHubClient` + implement in `StoreHubHttpClient` (authenticated GET `/store/features`, reusing the existing `SendAuthenticatedAsync<T>` helper).
- **Fetch + cache:** load the features once after login (before the main panels render). The exact fetch/caching seam (e.g. a small session-scoped holder, or fetch in the panel that needs it) is an implementation decision for the plan; it must be available to both `SalePanel` and the product-creation form, and must refresh on login (multi-user terminal).
- **SalePanel:** when `!MultipleProductTypesEnabled`, **hide** `AddHardwareProductButton` (set `Visible = false` — a Minimart never deals in hardware, so hide rather than merely disable); GeneralHardware keeps both buttons unchanged.
- **Product-creation form(s)** (`AddNewInventoryProductForm` / `AddNewInventoryProductWithCustomBarcodeForm`): when `!MultipleProductTypesEnabled`, remove/omit the **Hardware** option from the category selector so only General Goods can be created. GeneralHardware unchanged.

### Behavior by store type

| Surface | GeneralHardware | Minimart |
|---|---|---|
| Sale: Add General Goods button | shown | shown |
| Sale: Add Hardware button | shown | **hidden** |
| Product create: category options | GeneralGoods + Hardware | **GeneralGoods only** |
| Server: create Hardware product | allowed | **rejected (4xx)** |
| Server: create General Goods product | allowed | allowed |

## Interfaces

- `StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled)` — new, Application layer, shared by StoreHub endpoint + WinForms client.
- `IStoreHubClient.GetStoreFeaturesAsync(CancellationToken = default) : Task<StoreFeaturesDto>` — new.
- `GET /store/features` — new StoreHub endpoint.
- `CreateProductCommandHandler` — modified (add the Hardware/general-only guard).
- No Domain changes (`StoreTypeFeatures` + `ProductCategory` already exist).

## Testing

- **Application (`IndyPOS.Application.Tests`):** `CreateProductCommandHandler` — (a) Hardware category + general-only store → throws; (b) Hardware category + GeneralHardware store → succeeds; (c) GeneralGoods category + general-only store → succeeds. Use the existing `MockStoreIdentityService` (settable `StoreType`, `.Features` derived).
- **Integration (`IndyPOS.StoreHub.IntegrationTests`, Docker-gated):** `GET /store/features` returns `{true,true}` for a GeneralHardware test host and `{false,false}` for a Minimart host (the `TestStoreIdentityService`/factory store type governs this — the plan determines whether a second factory/config is needed or the store type is overridable per test).
- **Domain (`IndyPOS.Domain.Tests`):** add a focused `StoreTypeFeatures.For` mapping test if not already covered (GeneralHardware→both true; Minimart→both false).
- **WinForms:** `[ExcludeFromCodeCoverage]` forms have no automated harness; validate the button/category gating live on the VM (a Minimart install — `--store-type Minimart`) alongside the existing payment-methods smoke.

## Open decisions — resolved

- **CoffeeShop:** its own future app; not an IndyPOS store type going forward. Enum value retained but unsupported/untargeted.
- **Supported store types:** GeneralHardware + Minimart.
- **Enforcement surface:** sale screen + product management + server guard (all three).
- **Features delivery to WinForms:** dedicated `GET /store/features` endpoint (plain inline, not a Nokpirab query).
- **Product-type model:** keep the fixed `ProductCategory` enum (GeneralGoods/Hardware); no data-driven catalog.
