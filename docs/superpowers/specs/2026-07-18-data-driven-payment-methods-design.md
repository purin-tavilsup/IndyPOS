# Data-Driven Payment Methods + Real Store-Type Gating — Design

**Status:** 🟢 Ready for planning
**Created:** 2026-07-18
**Epic:** M (Multi-Store Type Support) — local slice
**Supersedes:** the payment-method portions of `.planning/indypos-overhaul/drafts/epic-m-multi-store-type.md` (April 2026 draft; its "DB-per-store-type" and `StoreTypeFeatures.AllowedPaymentMethods` designs are replaced by this spec).

---

## 1. Problem & Goals

Payment methods are currently a **hardcoded C# enum** (`PaymentType` in `src/IndyPOS.Application/Common/Enums/PaymentTypes.cs`: `Cash=1, PayLater=2, WelfareCard=3, M33WeLove=4, MoneyTransfer=5, FiftyFifty=7, WeWin=8`). The Thai government periodically launches **temporary payment campaigns** (each roughly a year, e.g. M33WeLove, FiftyFifty, WeWin) and keeps minting new ones. Today, supporting a new campaign means **editing the enum and redeploying** to every store — the core pain this design removes.

The store also runs multiple **store types** (`GeneralHardware`, `Minimart`, `CoffeeShop`). **PayLater** (buy-on-credit/trust — culturally common in rural areas but the most troublesome method to administer) must be available **only in GeneralHardware** stores. Today that rule is only half-enforced server-side and — critically — **every installed store silently defaults to `GeneralHardware`** because the installer never writes `Store:Type`, so the restriction never actually restricts anyone.

### Goals
1. Adding/retiring a government payment campaign requires **no code change and no redeploy**.
2. **Track all** payment methods ever used (including dead campaigns) so historical invoices and reports resolve.
3. Payment methods offered in the POS are gated by **store type** (PayLater → GeneralHardware only) and a **manual on/off switch**.
4. **StoreType is actually persisted per store** and read at runtime, so store-type gating is real.

### Non-goals (explicitly deferred)
- **Product-type restriction** (`MultipleProductTypesEnabled`) — its own small follow-up spec.
- **Central/cloud distribution** of the catalog across stores (needs Epic I) — each store manages its own catalog locally for now.
- **Date-based auto-expiry** — exact campaign validity dates are not known in practice, so dates are informational only (see §4).
- Any change to how **PayLater debt/receivables** are tracked — the existing `PayLater` entity + `PayLaterRepository` behavior is unchanged; this spec only governs whether PayLater is *offerable*.

---

## 2. Business Rules

> **Superseded 2026-07-25 (Kind column only).** The `Kind` domain is now
> `{ Standard = 1, GovernmentCampaign = 2, Special = 3 }` — `Standard` is a rename of
> `Permanent` (same backing value). Reclassified: **PayLater → Special** (a store credit
> arrangement, not tender) and **WelfareCard → GovernmentCampaign** (it is a government
> scheme; still seeded enabled). Cash/MoneyTransfer are `Standard`. `Kind` remains display
> metadata only and drives no behaviour. Everything else in this table still holds.

| Method | Kind | Stores | Notes |
|---|---|---|---|
| Cash | Standard | All | |
| MoneyTransfer | Standard | All | |
| WelfareCard | GovernmentCampaign | All | Government welfare card (ongoing program) — enabled |
| PayLater | Special | **GeneralHardware only** | Hard invariant (see §5); most troublesome method |
| M33WeLove, FiftyFifty, WeWin | GovernmentCampaign | (were: all) | Dead — seeded **disabled** so history resolves |
| *future campaigns* | GovernmentCampaign | configurable | Added as data rows at runtime, no redeploy |

A method is **offerable right now** ⇔ `IsEnabled` **AND** the method is allowed for this store's type. There is no date gate.

---

## 3. Architecture Overview

Clean Architecture, following existing IndyPOS layering. New/changed units:

```
Domain
  PaymentMethod (entity)            NEW  — catalog row
  PaymentMethodPolicy (domain svc)  NEW  — PURE "offerable for StoreType" rule + PayLater invariant (no I/O)
  StoreType (enum)                  EXISTS
  StoreTypeFeatures (VO)            EXISTS (PayLater no longer flows through it)
Application
  IPaymentMethodRepository          NEW
  IPaymentMethodCatalogService      NEW  — loads catalog via repo, applies PaymentMethodPolicy, + admin ops
  GetOfferablePaymentMethodsQuery   NEW
  Payment-method admin commands     NEW  — TogglePaymentMethod, AddCampaignPaymentMethod
  CompleteSaleCommandHandler        CHANGED — generic offerable check (replaces PayLater-only)
Infrastructure
  PaymentMethodRepository (EF)      NEW
  StoreHub EF migration + seed      NEW
StoreHub (API)
  /payment-methods (GET offerable)  NEW
  /admin/payment-methods (CRUD/toggle) NEW  — SystemAdmin only
WinForms
  AcceptPaymentForm                 CHANGED — render buttons from catalog
  PaymentMethodsSettingsPanel       NEW  — SystemAdmin management screen
Installer
  InstallationConfig.StoreType      NEW
  InstallationWizard store-type step NEW
  DatabaseSetup writes Store:Type   CHANGED
```

**Local StoreHub PostgreSQL** owns the catalog (source of truth per store). No cloud dependency.

---

## 4. The Catalog Model

### `PaymentMethod` (Domain entity, local `payment_method` table)

| Field | Type | Notes |
|---|---|---|
| `Code` | string (PK) | Stable key stored on payments + used in reports. PascalCase, matches legacy enum names for migration continuity (`Cash`, `MoneyTransfer`, `WelfareCard`, `PayLater`, `M33WeLove`, `FiftyFifty`, `WeWin`). |
| `DisplayName` | string | Shown on the POS button (Thai). |
| `Kind` | enum `PaymentMethodKind { Standard = 1, GovernmentCampaign = 2, Special = 3 }` | Display metadata only — see the §2 note. Was `{ Permanent, GovernmentCampaign }`. |
| `IsEnabled` | bool | **Authoritative on/off.** Admin toggles when a campaign starts/ends. |
| `DisplayOrder` | int | Button ordering in the POS. |
| `ValidFrom` | DateTime? | **Optional, informational only.** Not enforced. |
| `ValidTo` | DateTime? | **Optional, informational only.** Not enforced. |
| `StoreId` | string | Multi-tenant column, consistent with other Core entities. |
| `CreatedUtc` / `LastModifiedUtc` | DateTime | Entity convention. |

**Store-type allowance** is *not* a free column for PayLater (see §5). For non-PayLater methods, all store types are allowed. If per-method store-type allow-lists are needed later, a `payment_method_store_type` join can be added; **YAGNI for now** — the only real per-type difference today is PayLater, and that is code-enforced.

### Offerable rule (single source of truth)
The pure rule is a **Domain** unit — `PaymentMethodPolicy.Offerable(methods, storeType)` — that takes a set of catalog rows + a `StoreType` and returns the offerable subset: `IsEnabled` rows, minus PayLater when `storeType != GeneralHardware` (the §5 invariant), ordered by `DisplayOrder`. No I/O, so it is fully unit-testable in the Domain layer.

`IPaymentMethodCatalogService.GetOfferable(storeType)` (Application) loads the catalog via `IPaymentMethodRepository` and applies `PaymentMethodPolicy`. Both the POS UI (§7) and server enforcement (§8) call the service — no duplicated logic.

### Seed
An EF migration seeds the seven known methods with the Kinds/enabled-states in §2. Dead campaigns are seeded `IsEnabled = false`.

---

## 5. PayLater: hard invariant

Because PayLater is a hard business rule (not a preference) and the most damaging to get wrong, its "GeneralHardware only" restriction is enforced **in code** (in `PaymentMethodPolicy`, the Domain rule), independent of catalog data:

- `PaymentMethodPolicy.Offerable` filters PayLater out unless `storeType == GeneralHardware`, regardless of the PayLater row's `IsEnabled`.
- The admin screen (§9) cannot present PayLater as offerable for a non-GeneralHardware store; toggling its `IsEnabled` only affects GeneralHardware.
- Server enforcement (§8) rejects a PayLater payment on any non-GeneralHardware store even if data were tampered with.

So even a hand-edited catalog row cannot enable PayLater for a Minimart.

---

## 6. Retiring the `PaymentType` enum + data migration

- **Decided (Q1):** go-forward storage is the stable **`Code` string** on `Payment.Method` (already a `string`) — the type best suited to a data-driven catalog keyed by `Code`. No new enum/int.
- The legacy `PaymentType` enum is removed from active use. If any code still needs a typed handle to *permanent* methods, a small internal constants class (`PaymentMethodCodes.Cash` etc.) provides stable strings without reintroducing a closed enum.
- **Existing data migration:** the migration maps each existing stored payment value to its catalog `Code`. Because the chosen Codes match the old enum names, the mapping is 1:1. Reports that grouped by the old enum group by `Code` afterward with equivalent output.

> Implementation confirms how payment values are physically stored today (enum int vs. string) purely to write the *mapping* in the data-migration step; the go-forward decision (store `Code`) is settled.

---

## 7. WinForms payment UI

`AcceptPaymentForm` currently hardcodes buttons (incl. `PayByPayLaterButton` / `AcceptPayLaterPaymentButton`) and uses the `PaymentType` enum. It becomes **catalog-driven**:

- On open, it calls the StoreHub `GET /payment-methods` endpoint (which returns the offerable set for *this* store) and renders one button per returned method, ordered by `DisplayOrder`, labeled `DisplayName`.
- No PayLater button appears for Minimart/CoffeeShop — not merely disabled, absent.
- Selecting a method records its `Code` on the payment.

This removes the hardcoded per-method UI and makes new campaigns appear automatically once enabled.

---

## 8. Server-side enforcement

`CompleteSaleCommandHandler` today rejects PayLater when `Features.PayLaterEnabled` is false (string compare on `"PayLater"`). Generalize it:

- For each payment, verify its `Code` is in `GetOfferable(store.StoreType)`. If not → reject the sale with a clear domain error (unknown/disabled/not-allowed-for-store-type method).
- This closes the gap where a tampered client could submit a disabled or store-type-forbidden method.
- The handler **no longer reads `StoreTypeFeatures.PayLaterEnabled`** — store-type gating now flows entirely through `GetOfferable` + the §5 invariant. `StoreTypeFeatures` itself stays (it still carries `MultipleProductTypesEnabled` for the deferred product-type work); whether to delete the now-unused `PayLaterEnabled` field is a small planning-time cleanup decision, but it is no longer authoritative for anything.

---

## 9. Toggle / add-campaign mechanism (recommended)

**Recommendation: an in-app, SystemAdmin-gated "Payment Methods" settings screen** (`PaymentMethodsSettingsPanel`) backed by StoreHub admin endpoints.

Capabilities:
- List catalog rows with their `IsEnabled` state.
- **Toggle** `IsEnabled` (enable a launching campaign; retire an ended one).
- **Add** a new `GovernmentCampaign` method: `Code`, `DisplayName`, optional `ValidFrom/To`, `DisplayOrder`.
- **Edit `DisplayName` and `DisplayOrder`** for any method (permanent or campaign) — decided (Q3): helpful for tuning the POS button labels/layout over time.
- Not editable: a method's `Code` (stable key, referenced by invoices/reports), its `Kind`, and PayLater's store-type restriction (§5, code invariant).

Backed by `POST /admin/payment-methods` (add) and `PATCH /admin/payment-methods/{code}` (toggle), gated to the SystemAdmin role via the existing auth middleware.

**Why this over the alternatives:**
- *Raw DB edit* — rejected: requires DB access + SQL comfort; wrong tool for a store operator; error-prone (no invariant checks).
- *JSON config file + restart* — rejected: still needs file access on each store machine + a service restart, and a file that seeds a relational table invites drift from the rows invoices already reference.
- *In-app screen* — chosen: no developer, no restart, invariants enforced server-side, catalog stays in the DB where reports/invoices live. It reuses the existing SystemAdmin role + settings-panel pattern.

**Scope note:** the screen is deliberately minimal (list + toggle + add). For 3 stores, an operator (or Pond) applies changes per store; **central distribution across stores is a future Epic-I enhancement**, called out so it isn't mistaken for done.

---

## 10. Make StoreType real

Today `StoreIdentityService.StoreType` reads `StoreIdentityOptions.Type` (config section `Store`), which the installer never writes → always defaults to `GeneralHardware`. Fix the chain:

1. `InstallationConfig` gains a `StoreType` property.
2. `InstallationWizard` gains a **store-type selection step** (the operator picks GeneralHardware / Minimart / CoffeeShop at install). StoreType is **immutable after install**.
3. `DatabaseSetup` writes `Store:Type` alongside `Store:Id` into `appsettings.json`.
4. Runtime already binds `Store` → `StoreIdentityOptions`; no change needed beyond the value now being present.

The wizard step's exact layout is a UI detail for the plan; functionally it is a required single-choice selection defaulting to GeneralHardware for continuity.

---

## 11. Data Flow

```
Install:  wizard store-type pick -> InstallationConfig.StoreType
          -> DatabaseSetup writes Store:Type + Store:Id -> appsettings.json
          -> EF migration creates+seeds payment_method table

Runtime (sale):  AcceptPaymentForm -> GET /payment-methods
                 -> IPaymentMethodCatalogService.GetOfferable(store.StoreType)
                 -> buttons rendered (Code/DisplayName/order)
                 -> payment recorded with Code
                 -> CompleteSale -> re-validates Code is offerable -> persists

Admin (campaign):  PaymentMethodsSettingsPanel -> POST/PATCH /admin/payment-methods
                   -> catalog row added/toggled -> next AcceptPaymentForm reflects it
```

---

## 12. Error Handling

- **Missing `Store:Type` config** (upgraded store installed before this change): bind default `GeneralHardware` with a startup **warning log** (existing store is GeneralHardware, so this is safe) — do not throw.
- **CompleteSale with a non-offerable method:** reject with a specific domain exception message naming the method + reason (disabled / not-allowed-for-store-type / unknown). Never silently drop.
- **Admin add with a duplicate `Code`:** reject (409-style) — Codes are unique.
- **Admin add/toggle without SystemAdmin:** 403 via existing auth middleware.
- **Catalog empty / unreachable at POS open:** fail visibly (cashier can't complete a sale with no methods) rather than defaulting to a hardcoded set — surfaces a real misconfiguration.

---

## 13. Testing Strategy

- **Domain tests — new `IndyPOS.Domain.Tests` project (decided, Q2):** the pure `PaymentMethodPolicy` rule and `StoreTypeFeatures` live in Domain, so they are tested there.
  - `PaymentMethodPolicy.Offerable`: PayLater present for GeneralHardware, absent for Minimart/CoffeeShop; disabled methods excluded; ordering by `DisplayOrder`.
  - PayLater invariant: PayLater not offerable for non-GeneralHardware **even when its row is enabled**.
- **Application tests (`IndyPOS.Application.Tests`)** (reuse `MockStoreIdentityService.GeneralHardware()/Minimart()/CoffeeShop()`):
  - `IPaymentMethodCatalogService.GetOfferable`: composes repo + policy correctly.
  - `CompleteSale`: accepts an offerable method; rejects a disabled method; rejects PayLater on Minimart (the currently-untested path).
  - Add-campaign / toggle / edit-display command behavior, including duplicate-Code rejection.
- **Migration test:** existing payment values map to the correct catalog `Code`s; dead campaigns seed disabled; seven rows seeded.
- **StoreType round-trip:** installer writes `Store:Type` → options bind → `StoreIdentityService.StoreType` returns it. Missing-Type → GeneralHardware + warning.
- **StoreHub integration** (Docker-gated, deferred to CI/VM as usual): `/payment-methods` returns the store-type-correct set; `/admin/payment-methods` gated to SystemAdmin.

`IndyPOS.Domain.Tests` does not exist yet — this slice **adds it** (Q2 decision) as the home for `PaymentMethodPolicy` + `StoreTypeFeatures` tests. Its first task is scaffolding the project (xUnit + FluentAssertions, matching the other test projects' conventions).

---

## 14. Migration Plan (existing single store)

The one existing store is GeneralHardware:
1. Deploy adds the `payment_method` table + seed (enabled permanent methods, disabled dead campaigns, PayLater enabled — store is GeneralHardware).
2. Data migration maps existing payment values → Codes (§6).
3. `Store:Type` is absent on the already-installed store → binds `GeneralHardware` (correct) + warning; a later reinstall/upgrade sets it explicitly.
4. Verify: reports group by Code with equivalent output; PayLater still offered (GeneralHardware); dead campaigns appear in history but not in the POS.

---

## 15. Out of Scope (restated)

Product-type restriction; cloud/central catalog distribution; PayLater receivables changes; date-based auto-expiry; per-method store-type allow-lists beyond the PayLater invariant.

---

## 16. Resolved Decisions

1. **Payment value storage (Q1):** go-forward = stable `Code` **string** on `Payment.Method`. The data migration still inspects current physical storage only to write the value→`Code` mapping (§6).
2. **Test location (Q2):** add a new `IndyPOS.Domain.Tests` project for the domain-level `PaymentMethodPolicy` + `StoreTypeFeatures`; the catalog service, handlers, and migration are tested in `IndyPOS.Application.Tests` (§13).
3. **Admin screen scope (Q3):** allows editing `DisplayName` + `DisplayOrder` for any method (not campaigns-only), plus toggle + add-campaign. `Code`/`Kind`/PayLater-restriction are not editable (§9).
