# Per-Store UI — Design

**Date:** 2026-10-07 · **Status:** approved in conversation (Pond), awaiting written-spec review
**Scope:** which parts of the v4 till are on or off for each store type: sale panel, menu, reports,
cash-flow panel. Three slices, three PRs.
**Builds on:** PR #117 (dev store profiles, per-store test hosts). Lands after it.

## 1. Why

The three stores ran three different v3 tills:
- GeneralHardware ran IndyPOS v3.6.0;
- MimyMart ran `C:\personal\MimyMart`;
- MimyShop ran `C:\personal\MimyShop`, a fork of MimyMart.

v4 has one till for all of them, and today it shows **GeneralHardware's UI to every store**:
- the รายการลงบัญชี menu;
- GeneralHardware's report cards (hardware split, ลงบัญชี, and fixed campaign cards);
- GeneralHardware's cash-flow panel;
- and **no service buttons**.

A MimyShop cashier therefore **cannot sell its services** (จัดส่ง, เอกสาร) on the v4 sale screen. That makes
this a **go-live blocker for MimyShop** (Pond, 2026-10-07).

**Scope rule (Pond):** this work only switches existing pieces on or off per store type. It adds no new
features. Store workflows decide what fits, so "helpful" additions are out of scope.

## 2. The on/off table (Pond, 2026-10-07)

Built from a read-only inventory of the three v3 tills (Appendix A).

| Screen | Piece | GH | MimyMart | MimyShop |
|---|---|---|---|---|
| Menu | รายการลงบัญชี | on | off | off |
| Sale | เบ็ดเตล็ด button (`2001000000012`) | on | on | on |
| Sale | ฮาร์ดแวร์ button (`2005000000027`) | on | off | off |
| Sale | จัดส่ง (`2002500000014`) and เอกสาร (`2002500000021`) buttons | off | off | on |
| Payment | methods | the store's enabled catalogue (done in #117) | | |
| Sales history | reprint | on | on | on |
| Reports: overview | sales | on | on | on |
| Reports: overview | general / hardware split cards | on | off | off |
| Reports: overview | ลงบัญชี cards | on | off | off |
| Reports: overview | payment-method cards | one per method (§4.2) | | |
| Reports: overview | จัดส่ง / เอกสาร cards | off | off | on |
| Reports | products sold (tab) | on | on | on |
| Reports | products-sold hardware/general filter | on | off | off |
| Reports | outstanding ลงบัญชี (tab) | on | off | off |
| Cash flow | general / hardware split | on | off | off |
| Cash flow | debt-repaid and today's ลงบัญชี lists | on | off | off |
| Cash flow | non-cash lines | one per enabled non-cash method | | |
| Inventory | categories | per store type (already done) | | |

**Related rules:**
- **Expected cash counts only cash** (Pond, 2026-10-07). Transfer, บัตรสวัสดิการ, คนละครึ่ง, ม.33 and
  เราชนะ are not collected into the drawer.
- **ม.33 and เราชนะ are deprecated but kept for history.**

## 3. Store-type features

`StoreTypeFeatures` (Domain, decided by `StoreType`) gains one flag:

| Flag | GH | MimyMart | MimyShop | Turns on |
|---|---|---|---|---|
| `PayLaterEnabled` (existing) | ✓ | — | — | ลงบัญชี menu, outstanding ลงบัญชี tab, ลงบัญชี cards, cash-flow debt lists |
| `MultipleProductTypesEnabled` (existing; hardware vs general) | ✓ | — | — | ฮาร์ดแวร์ button, general/hardware splits in cards and cash flow, products-sold filter |
| **`ServiceProductsEnabled`** (new) | — | — | ✓ | จัดส่ง / เอกสาร buttons and cards |

- Everything else is either on for every store, or follows the store's **enabled payment catalogue**.
- The service barcodes become shared Domain constants. MimyShop's v3 already pins them as
  `ServiceProductBarcodes`, and the dev profiles use them.
- `GET /store/features` returns the new flag, additively (`StoreFeaturesDto`). No new route.

## 4. The slices

### 4.1 Slice 1: sale panel and menu (the MimyShop blocker)

- **ฮาร์ดแวร์ button:** shown when `MultipleProductTypesEnabled` (as today).
- **จัดส่ง / เอกสาร buttons:** shown when `ServiceProductsEnabled`. They behave as in MimyShop v3:
  - each opens the existing price-entry dialog (`AddInvoiceProductForm`) for that service product;
  - the price is typed and a note is optional;
  - the product is non-trackable, so no stock moves.
- **If a service product is missing** from the store's catalogue, the button tells the cashier so, in
  Thai, rather than failing silently. Dev stores always have them (#117 profiles). A real MimyShop store
  gets them from its migrated data; verify this before go-live.
- **รายการลงบัญชี menu:** shown only when `PayLaterEnabled`.
- **The decision lives in a small ViewModel** (which buttons and menu items this store has), not in the
  form.

### 4.2 Slice 2: reports

**StoreHub's sales summary (`/reports/sales-summary`) gains two lists.** The existing fixed
`PaymentBreakdown` fields stay, for compatibility.
- **`paymentsByMethod`:** `[{ code, displayName, total }]`, one row per catalogue method that is
  **enabled or has sales in the range**, ordered by display order.
  - A deprecated method (ม.33, เราชนะ) appears only in a range where it was used.
  - คนละครึ่ง no longer hides in `Other`.
- **`serviceSales`:** `[{ barcode, name, total }]` for the service products. It is empty for stores
  without `ServiceProductsEnabled`.

**The overview cards** come from a ViewModel that builds one list of `(title, amount)`:
1. sales (always);
2. the general / hardware split cards, if `MultipleProductTypesEnabled`;
3. the ลงบัญชี cards, if `PayLaterEnabled`;
4. one card per `paymentsByMethod` row;
5. the จัดส่ง / เอกสาร cards, if `ServiceProductsEnabled`.

The panel draws the list (a flowing layout of cards), instead of the hard-coded labels it has today.

**Other report tabs:**
- outstanding ลงบัญชี: shown only with `PayLaterEnabled`;
- products sold: shown for every store; its hardware/general filter only with
  `MultipleProductTypesEnabled`.

### 4.3 Slice 3: cash-flow panel (together with cash plans 2–3)

Cash plans 2–3 already move this panel into its own ลิ้นชักเก็บเงิน section, backed by StoreHub. This slice
adds:
- the general/hardware split and the debt lists, gated by the same two flags;
- one non-cash line per enabled non-cash method;
- **expected cash = Cash-method payments + debt repaid + floats − payouts.**
  - Today's `CashDrawerCalculator` copies GeneralHardware v3's bug: it subtracts only transfer and
    welfare, so คนละครึ่ง inflates expected cash.
  - The fix counts the Cash method directly, so no future campaign can break it.
  - It may ship earlier as its own PR if Pond wants it sooner.

## 5. Testing (negative cases first; expected values written in the tests)

- **Domain:** `StoreTypeFeatures` per store type, all three flags. Only MimyShop has
  `ServiceProductsEnabled`.
- **StoreHub, once per store** (the #117 `StoreProfileHosts`):
  - `/store/features` reports each store's flags;
  - `paymentsByMethod` lists exactly the store's enabled methods, plus a disabled method only when it
    had sales in the range;
  - `serviceSales` sums MimyShop's service sales and is empty elsewhere.
- **ViewModels (no UI):**
  - sale buttons and menu items per feature set;
  - report cards per store, for example:
    - MimyShop: no ลงบัญชี or hardware card, and both service cards;
    - MimyMart: a deprecated method's card only when it had sales.
- **Expected cash:** a คนละครึ่ง sale does not change expected cash (slice 3, or its own PR).
- **By hand (Pond):** `--store MimyShop`, `MimyMart` and `GeneralHardware`, checking the sale screen,
  menu and reports.
- **Counts:** measured, never derived.

## 6. Rollout and compatibility

- **Every server change is additive:** the new flag and the two report lists sit beside the existing
  fields. An older till ignores them.
- **A rolled-back StoreHub with a newer till:** the flag is missing, so the till hides the service
  buttons and does not crash.
- **No database migration.**
- **Slices ship as separate PRs**, in order 1 → 2 → 3.
  - Slice 1 is required before MimyShop goes live.
  - Slice 3 ships with cash plans 2–3.

## 7. Left out, on purpose

- New features of any kind (Pond's scope rule).
- A store-type picker in the till: the store type is a property of the StoreHub it talks to.
- Per-store theming or layout changes beyond showing and hiding.

## Appendix A — v3 inventory (2026-10-07, read-only)

| Area | GeneralHardware v3.6.0 | MimyMart v3 | MimyShop v3 |
|---|---|---|---|
| Menu | + รายการลงบัญชี | standard | standard |
| Sale quick buttons | เบ็ดเตล็ด, ฮาร์ดแวร์ | เบ็ดเตล็ด | เบ็ดเตล็ด, จัดส่ง, เอกสาร (icon buttons, price typed) |
| Payment methods | cash, transfer, คนละครึ่ง, เราชนะ, บัตรสวัสดิการ, ม.33, ลงบัญชี | cash, transfer | cash, transfer, บัตรสวัสดิการ, คนละครึ่ง |
| Reprint | yes | no | no |
| Overview cards | 8 sales cards (general/hardware/ลงบัญชี) plus transfer, คนละครึ่ง, ม.33, เราชนะ, บัตรสวัสดิการ, ลงบัญชี | sales, cash, transfer | sales, cash, transfer, บัตรสวัสดิการ, คนละครึ่ง |
| Other reports | products sold (hardware/general filter), sales history, outstanding ลงบัญชี | products sold, sales history | products sold, sales history |
| Cash flow | general/hardware split; float, payouts, debt repaid, today's ลงบัญชี; subtracts transfer + welfare (bug) | sales, transfer; float, payouts | sales, transfer, welfare, คนละครึ่ง; float, payouts |

Real enabled methods today (Pond, 2026-10-07):
- GeneralHardware: cash, transfer, บัตรสวัสดิการ, ลงบัญชี, คนละครึ่ง;
- MimyMart: cash, transfer;
- MimyShop: cash, transfer, บัตรสวัสดิการ, คนละครึ่ง.
