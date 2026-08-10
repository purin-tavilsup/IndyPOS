# Stock visible — and correctable — at the till

**Date:** 2026-08-09
**Status:** Approved (Pond, 2026-08-09)
**Epic:** 2 (SQLite → PostgreSQL migration hardening) — rollout confidence
**Branch:** `feat/stock-visible-at-till`

---

## Why now

Defect 14 shipped on 2026-08-09: migrated stock is no longer current stock minus the entire sales
history. GeneralHardware had netted −400,541 units across 66% of its products; that is fixed and
verified by `MigrationVerifier`'s new per-product stock check.

**But nobody can see it.** The till shows `0` for every product, so the fix cannot be confirmed by
eye at a real store. `.claude/STATUS.md` records this as the reason the rollout must not yet claim
stock is fixed. This spec closes that gap.

Investigating it surfaced a second defect in the same area, described below. The two are delivered
together because the first is not useful without the second.

---

## The two defects

### Defect A — stock cannot be seen

`ProductDto` (`src/IndyPOS.Application/UseCases/StoreHub/Products/ProductDto.cs`) carries no
quantity. `StoreHubInventoryProductService.MapToInventoryProductDto` therefore falls back to a
literal zero:

```csharp
QuantityInStock = quantityOverride ?? 0, // TODO: Get from StoreHub when available
```

(`src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubInventoryProductService.cs:233`)

`quantityOverride` is supplied only on the create and update paths, from the request the operator
just typed. Every product that arrives through the cache — i.e. every row `InventoryPanel` renders —
gets `0`.

The grid column already exists and is already wired: `InventoryPanel` declares
`ProductColumn.QuantityInStock`, labels it `จำนวนในคลัง`, and binds it at
`InventoryPanel.cs:255`. Nothing on the UI side is missing except a true number.

The data is present and queryable. `InventoryMovement` defines stock as `SUM(QuantityDelta)` per
`(StoreId, ProductId)`, and `InventoryMovementRepository.GetCurrentBalanceAsync` already computes it
for a single product.

### Defect B — stock cannot be changed

`UpdateInventoryProductForm`'s increase/decrease buttons mutate a label and nothing else:

```csharp
QuantityLabel.Text = $"{quantity + direction * amount}";
```

(`src/IndyPOS.Windows.Forms/UI/Inventory/UpdateInventoryProductForm.cs:221`)

On save, `CreateRequestForUpdateProduct` parses that label into
`UpdateInventoryProductRequest.QuantityInStock` (`:162`). `StoreHubInventoryProductService.UpdateAsync`
then builds an `UpdateProductCommand` — which **deliberately has no quantity field**:

```csharp
/// Does NOT update quantity - use AdjustQuantity for that.
```

(`src/IndyPOS.Application/UseCases/StoreHub/Products/Update/UpdateProductCommand.cs:7`)

So the typed quantity is parsed, packed into a request, and dropped. **Restocking through the UI is
a silent no-op.**

`AdjustQuantityAsync` exists end to end — `IInventoryProductService` → `StoreHubHttpClient` →
`POST /products/{id}/adjust-quantity` → `AdjustProductQuantityCommandHandler` — and has **zero UI
callers**. The plumbing was built and never connected.

**Why both ship together:** defect A alone hands a shopkeeper an accurate number they have no way to
correct. Given the whole point is confirming migrated stock at a real store, read without write is
not a usable increment.

---

## Design

### Read path

**Repository.** Add one batch method beside the existing single-product one:

```csharp
Task<IReadOnlyDictionary<Guid, int>> GetBalancesAsync(
    string storeId, CancellationToken cancellationToken = default);
```

One `GROUP BY ProductId` over `inventory_movement` for the store — not N calls to
`GetCurrentBalanceAsync`. Products with no movements are simply absent from the dictionary and read
as `0`.

Volume is comfortable and stays that way *because of* defect 14's fix: the migration writes one
`Migration:InitialStock` row per product rather than replaying every historical sale line.
GeneralHardware starts at ~10,590 movement rows, not ~336,000, and grows only with new sales. A
snapshot column on `Product` is therefore unnecessary — and would reintroduce the
snapshot-vs-movement drift `InventoryMovement`'s doc comment exists to prevent.

**Query + endpoint.** `GetProductStockQuery` → `IReadOnlyList<ProductStockDto>`, exposed as
`GET /products/stock`, gated on the existing `CanReadProducts` policy. The handler takes
`IInventoryMovementRepository` and `IStoreIdentityService`; the store scope comes from the service,
never from the caller.

**UI.** `InventoryPanel` fetches stock on open and on refresh, and merges it into the rows it is
already building. Remove the `// TODO` at `StoreHubInventoryProductService.cs:233`.

**Stock stays off `ProductDto`.** This is the load-bearing choice. `ProductCacheService` holds every
product in memory from login onward; a quantity on that record would be stale the moment any sale
completed, including one rung up on the store's *other* terminal. Keeping stock out of the cached
record means no stale figure can be displayed anywhere, and `SalePanel`'s barcode-scan hot path
remains a pure in-memory lookup with no added I/O.

`SalePanel` shows no stock at all. That is deliberate for this increment: a number that is right at
login and wrong by mid-morning is worse than no number on the screen where speed matters most.

### Write path

`UpdateInventoryProductForm` calls `AdjustQuantityAsync` when the operator changed the figure, and
skips the call when they did not.

**Delta, not target.** `AdjustQuantityRequest(int TargetQuantity, string? Reason)` and
`AdjustProductQuantityCommandHandler` currently compute `delta = target - currentBalance`
server-side. That has a lost-update hole: the form loads 10, another terminal sells 3 (balance 7),
the operator restocks by 2 and saves 12 — the server writes a delta of +5 and the sale is silently
absorbed. Switching the contract to a delta closes it, and it matches what the UI already produces:
the +/- buttons are a delta by construction.

`AdjustQuantityRequest` has no production callers outside this work, so changing the contract costs
nothing in compatibility.

**Third defect, found while planning: the adjust response is deserialized as the wrong type.** The
endpoint returns an anonymous `{ productId, quantity }` (`Program.cs:496`) while
`IStoreHubClient.AdjustProductQuantityAsync` declares `Task<ProductDto>`. Neither property binds, so
the client has always returned a `ProductDto` with a default `Id` and empty strings — and
`StoreHubInventoryProductService` then fed that empty record straight into
`_productCacheService.UpsertProduct`, which would have corrupted the cache entry the moment anything
called it. Nothing did, because no UI path calls adjust. Fixed here with a named
`AdjustQuantityResponse(Guid ProductId, int Quantity)`; the `UpsertProduct` call is dropped, since
stock is deliberately not part of `ProductDto` and nothing else about the product changed.

**The existing endpoint test hides this.** `AdjustQuantity_AsManager_ReturnsSuccess` posts
`quantityDelta`, which binds to nothing, so `TargetQuantity` defaults to `0` and the call *zeroes*
the product's stock — and the test asserts only `200 OK`, with a comment conceding it verifies
nothing further. It is replaced, not repaired.

- `AdjustQuantityRequest(int Delta, string? Reason = null)`
- `AdjustProductQuantityCommand` carries `Delta`
- The handler writes the movement directly from `Delta`, then reads the balance back to return it.
  The balance read no longer *determines what is written*, which is the whole point; it only reports
  the result. The `delta == 0` early return goes away with it (the form no longer calls on no change)
- A zero delta arriving anyway is rejected at the boundary rather than silently ignored

**Request cleanup.** Drop `QuantityInStock` from `UpdateInventoryProductRequest`, so a field that
does nothing stops looking like it does something. `AddNewInventoryProductRequest` keeps its
quantity — that one is real, flowing to `CreateProductCommand.InitialQuantity`.

### Out of scope

- **Defect 7b / `IsTrackable`.** Non-trackable products (29 across all three stores) will display a
  stock figure until `Core.Product` carries the flag. Unchanged by this work, and the trip-wire
  comment on `CompleteSaleCommandHandlerTests` still stands.
- **Stock on `SalePanel`.** See above.
- **Low-stock warnings, reorder points, stock reports.** YAGNI.

---

## Testing

| Test | Suite | Asserts |
|---|---|---|
| `GetBalancesAsync` returns a summed balance per product | StoreHub.IntegrationTests | Real Postgres; two movements on one product sum; a second product is independent |
| `GetBalancesAsync` omits products with no movements | StoreHub.IntegrationTests | Absent key, so the caller reads 0 |
| `GetBalancesAsync` is scoped to the store | StoreHub.IntegrationTests | Another store's movements do not leak in |
| `GET /products/stock` returns balances | StoreHub.IntegrationTests | Endpoint + auth policy wired |
| `GET /products/stock` requires `CanReadProducts` | StoreHub.IntegrationTests | 401/403 unauthenticated |
| Adjust applies the delta to the existing balance | StoreHub.IntegrationTests | Balance 100, delta +50 → 150 |
| Adjust does not swallow a concurrent sale | StoreHub.IntegrationTests | Balance 100, a −30 sale lands, then delta +50 → **120**, not 150. This is the reason for delta semantics; it must fail if anyone reverts to target. Written against real Postgres rather than mocks, so it exercises the actual movement sum |
| Adjust rejects a zero delta | StoreHub.IntegrationTests | 400 at the boundary, not a silent return |
| The service sends the delta and returns the new balance | Application.Tests | Via a mocked `IStoreHubClient` |
| Pending adjustment accumulates +/- presses and reports the delta | Windows.Forms.Tests | Including "back to where it started ⇒ no change", so no zero-delta call is made |

The last row is the edit form's logic, extracted into a `PendingStockAdjustment` value object. The
repo has no harness for instantiating WinForms forms — `IndyPOS.Windows.Forms.Tests` covers only the
error-reporting helpers — so the decision logic lives somewhere testable and the form stays a thin
caller. The rest of the form change is covered by the manual run below, plus a compile-time
guarantee: `UpdateInventoryProductRequest.QuantityInStock` is deleted, so the silently-dropped path
cannot return.

**Verify by running it, not only by tests.** Consistent with how defect 14 was closed: migrate a
fixture store, open `InventoryPanel`, confirm the figures match `SUM(QuantityDelta)` in Postgres,
restock one product through the form, and confirm both the new movement row and the refreshed grid.
The console/UI path has no automated coverage and has hidden two "reported success, wrote nothing"
bugs before.

---

## Risks

- **`InventoryPanel` refresh cost.** One extra request per panel open, returning ~10.6k small
  records. On a LAN, fine. If it ever is not, the fix is a filter parameter, not a cached column.
- **Changing `AdjustQuantityRequest`'s meaning.** `TargetQuantity` → `Delta` is a same-typed rename:
  a stale caller would compile and be wrong. Mitigated by there being no production caller, and by
  the concurrent-sale test. Existing tests in `ProductsEndpointTests` and
  `StoreHubInventoryProductServiceTests` must be updated deliberately, not mechanically.
- **Negative balances are permitted.** Nothing here clamps. A delta that drives stock below zero is
  allowed and visible, which is the honest behaviour — see defect 14b, where 1,535 products carried
  negative legacy stock and hiding it was the bug.
