# Route Tidy-Up — Design

**Date:** 2026-10-02 · **Status:** approved in conversation (Pond), awaiting written-spec review
**Scope:** StoreHub HTTP API (and one CloudApi route). Two PRs.

## 1. Why

The StoreHub API grew one route at a time. Most of its 745-line `Program.cs` follows no shared rule, and
some routes break the REST rules Pond set on 2026-09-27 (memory: *REST API design*):

- **Verbs in paths:** `POST /sales/complete`, `POST /pay-later/{id}/record-payment`,
  `POST /products/{id}/adjust-quantity`.
- **Two roots for one resource:** `/payment-methods` (what the till may offer) and
  `/admin/payment-methods` (the catalogue).
- **Bugs found while planning invoice history:**
  - Two writes record no user: a stock adjustment, and a deprecated `UserId` still sits in the
    sale body.
  - `/reports/*` returns a 500 on an out-of-range or swapped date.
  - The cloud's `/sync/status` is open to anyone and counts every store.

**Renames are cheap now.** No store runs v4 yet; only the test VM does. Once a store goes live, a shipped
route is never renamed (§6). So this work must land **before Epic 3 Phase A-1 installs on a real
store**.

## 2. Callers checked (2026-10-02)

The routes to be renamed are called by:

- **Production code:** only `StoreHubHttpClient` (`src/IndyPOS.Infrastructure/Services/StoreHub/`).
  It is the till's client. It ships in the same release as StoreHub, so it is updated in the same PR.
- **Ours to update:** `scripts/smoke-test.ps1`, the `.bruno/` collection,
  `docs/development/getting-started.md`, and `docs/diagrams/*` (architecture overview, flows,
  README).
- **Nothing else.** No other app, installer step or script calls these paths.

**So every rename is a hard rename.** No deprecated alias is kept. The version-skew gap cannot happen
here: the installer upgrades StoreHub first and the till last (Velopack, step 8, never rolled back), so
for a while a till could be older than its server. But that only matters once a store is live.

## 3. PR A — restructure and fixes (no path or shape change)

### 3.1 Move routes out of `Program.cs`

- **One file per resource:** `Endpoints/<Area>/<Area>Endpoints.cs`, each with a
  `Map<Area>Endpoints(this IEndpointRouteBuilder)` extension. This follows the pattern `/cash`
  and `/sales` already use.
- **Areas:** `Auth`, `Products`, `PaymentMethods`, `Catalogue` (product categories, store features),
  `PayLater`, `Reports`, `Sync`, and `System` (`/`, `/version`).
- **`Program.cs` keeps** service registration, auth policies, middleware and the `Map…Endpoints()`
  calls only.
- **Shared helpers move:** `RequireUserIdFilter` and `ClaimsPrincipalExtensions` (`GetRequiredUserId`,
  `HasCapability`) go from `Endpoints/Cash/` to `Endpoints/Common/`. Plan 1 deviation D10 deferred
  this move to here.
- **Paths, verbs, policies, status codes and response bodies stay byte-for-byte the same.** The
  existing endpoint tests are the proof: they pass with no edits to URLs or asserted bodies.

### 3.2 Fixes (each ships with a test that fails before the fix)

1. **Stock adjustment records its user.**
   - Add a **nullable** column `inventory_movement.created_by_user_id` (migration
     `AddInventoryMovementUser`).
   - `adjust-quantity` takes the user from the token (`GetRequiredUserId`) and joins the
     `RequireUserIdFilter`. A token without a usable user id gets **401**.
   - Sale movements also store the selling user, which the sale handler already has.
   - Older rows stay `NULL`.
2. **Remove `CompleteSaleRequest.UserId`.**
   - #108 deprecated it and ignores it. Remove the property, its doc comment and the client's
     assignment.
   - A body that still sends `userId` is accepted. System.Text.Json ignores unknown members by
     default, and a test pins this.
3. **`/reports/*` dates give a Thai 400, never a 500.**
   - Today a swapped range (`ArgumentException` in `ReportDateRange.ToUtcRange`) or `9999-12-31`
     (`DateOnly.AddDays` overflow) escapes as a 500.
   - Extract the date bounds and range check from `SalesQueryRules` (2000-01-01..2099-12-31, to ≥
     from) into one shared rule. Both `/sales` and `/reports` use it, so the two cannot drift.
   - Every report route that takes dates answers with a Thai `{ error }` and **400**.
4. **The cloud's `/sync/status` needs a store token.**
   - `src/IndyPOS.CloudApi/Program.cs` maps it with no authorization. Its counts (events, unprocessed,
     processed, invoices) cover **all** stores.
   - It will require the same store token as `/sync/events` and count only that token's store.
     No token gives **401**. A token without a `store_id` gives **403**.
   - **How the inbox is scoped (Pond, 2026-10-02):** a new nullable column
     `SyncedEvents.SourceStoreId` (CloudApi migration `AddSyncedEventSourceStore`, with a
     `HasComment`). Ingest sets it from the token's `store_id`, after the existing check that every
     payload names that store. `/sync/status` counts inbox rows `WHERE SourceStoreId = <token store>`.
     Events ingested before the release keep `NULL` and are counted for no store. `ProcessedEvents`
     and `Invoices` already carry a string `StoreId` and are filtered by it.
   - The StoreHub `/sync/status` already requires `CanViewSyncStatus` and is unchanged.

### 3.3 Migration gate

PR A has two additive migrations, each one nullable column:
- StoreHub `AddInventoryMovementUser`. Previous-release binaries can still write a sale and an
  adjustment.
- CloudApi `AddSyncedEventSourceStore`. A previous-release CloudApi can still ingest an event into
  `SyncedEvents` and mark it processed.

The forward-only recipe in `docs/operations/upgrade-procedure.md` is run for both databases. It gains a
short cloud variant, since the existing recipe covers StoreHub only. Both results are recorded.

## 4. PR B — renames (hard) and conventions

| Today | After | Notes |
|---|---|---|
| `POST /sales/complete` | `POST /sales` | Returns **201** with `Location: /sales/{id}` and the same body (`InvoiceId`, `InvoiceNumber`, …) |
| `POST /pay-later/{id}/record-payment` | `POST /pay-later/{id}/payments` | Same body and responses |
| `POST /products/{id}/adjust-quantity` | `POST /products/{id}/stock-adjustments` | Same body and responses |
| `GET /admin/payment-methods` | `GET /payment-methods?include=all` | Needs `CanManagePaymentMethods`, else **403** |
| `POST /admin/payment-methods` | `POST /payment-methods` | `CanManagePaymentMethods` |
| `PATCH /admin/payment-methods/{code}` | `PATCH /payment-methods/{code}` | `CanManagePaymentMethods` |

- **`GET /payment-methods` with no `include`** stays the offerable list under `CanReadProducts`,
  exactly as today. Any `include` value other than `all` gets a Thai **400**.
- **Every old path returns 404,** and one test per route pins this.
- **The till client** (`StoreHubHttpClient`) moves to the new paths in the same PR. The E2E tests
  (`StoreHubE2ETests`) follow.
- **Updated in the same PR:** the smoke script, Bruno (requests, folder and README), getting-started
  and the diagrams.
- **New `docs/architecture/api-conventions.md`:**
  - one resource root;
  - nouns, and sub-resources for commands;
  - typed keys on the same slot;
  - query-param filters and paging;
  - `/reports` for aggregates only;
  - the user id from the token, never the body;
  - Thai `{ error }` bodies, while a policy-level 401/403 has no body;
  - **and §6.**

## 5. Left alone, on purpose

- **`POST /products/next-barcode`:** it advances a counter, so it cannot be a safe `GET`. A noun
  rewrite (`POST /barcodes`) adds no clarity.
- **`/reports/legacy/*`:** these are aggregates in the WinForms model's shape, and they already sit
  under `/reports`. They go when the Avalonia port drops those models.
- **`record-payment` recording a user, the repayment history, and sync events for stock adjustments
  and repayments.** See §7.

## 6. Rule for after go-live (goes into `api-conventions.md`)

Never rename or remove a shipped route in one step:

1. Add the new route.
2. Keep the old one as a deprecated alias for **one release.** It calls the same handler and returns a
   `Deprecation` header.
3. Move the till client to the new route.
4. Remove the alias in the next release.

The reason: the installer upgrades StoreHub before the till and never rolls the till back. A till that
cannot reach its route cannot ring up a sale, and offline-first is priority #1.

## 7. Found while designing — needs its own spec

**Stock adjustments and PayLater repayments never reach the cloud.**
- `AdjustProductQuantityCommandHandler` and the `RecordPayLaterPayment` handler write no outbox
  event.
- `record-payment` keeps no row at all. It only raises `PayLater.PaidAmount`, so there is no
  repayment history and nowhere to record who took the money.

This breaks "sync is the core". The fix needs:
- a repayment history table;
- two new events and their cloud mirrors;
- a decision on how it relates to the cash drawer's `DebtRepayment`.

That is a feature, not a tidy-up.

**`HttpCloudSyncClient.ParseStoreId` sends `0` as the envelope `StoreId` for every real store.** Real
store ids are strings, and the inbox's `StoreId` column is an `int`. This is still a bug, and it stays
separate. PR A leaves the column and the client alone. Its new `SyncedEvents.SourceStoreId` makes the bug
irrelevant to `/sync/status`, but not to anything else that reads the int column.

## 8. Testing (negative cases first; `Subject_WhenScenario_DirectVerbOutcome`)

**PR A:**
- All existing endpoint tests pass unchanged.
- `AdjustQuantity_WithATokenWithoutAUserId_ReturnsUnauthorized`
- `AdjustQuantity_WithAValidToken_RecordsTheCaller`
- `CompleteSale_WithAStaleUserIdInTheBody_AcceptsTheSale`
- For each report route that takes dates:
  - `…_WithToBeforeFrom_ReturnsBadRequest`
  - `…_WithADateBeyondTheLatest_ReturnsBadRequest`
- `SyncStatus_WithoutAToken_ReturnsUnauthorized`
- `SyncStatus_WithAStoreToken_CountsOnlyThatStore`
- Ingest stamps the token's store on `SourceStoreId`; another store's row and a legacy `NULL` row are not counted.
- The migration gate is recorded for both databases.

**PR B:**
- For each renamed route:
  - `…_OnTheNewPath_…` behaves as the old one did;
  - `…_OnTheOldPath_ReturnsNotFound`.
- `CreateSale_WithAValidSale_ReturnsCreatedWithItsLocation`
- `ListPaymentMethods_WithIncludeAllAsCashier_ReturnsForbidden`
- `ListPaymentMethods_WithIncludeAllAsManager_ReturnsTheWholeCatalogue`
- `ListPaymentMethods_WithAnUnknownInclude_ReturnsBadRequest`
- `ListPaymentMethods_WithoutInclude_ReturnsTheOfferableList`
- The client E2E tests pass on the new paths.
- The suite counts are measured, never derived, and CLAUDE.md and ONBOARDING.md are updated.

## 9. Success

- `Program.cs` holds wiring only.
- Every StoreHub route follows `api-conventions.md`.
- The till rings up a sale, adjusts stock, records a repayment and manages payment methods on the
  new paths.
- No 500 is reachable through a report date.
- No cloud route is open without a token.
