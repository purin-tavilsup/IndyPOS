# Data-driven product categories + store types — design

**Date:** 2026-07-29
**Status:** Approved (Pond, 2026-07-29)
**Epic:** 1 of the corrected roadmap in `.planning/indypos-overhaul/PLAN.md`
**Blocks:** Epic 2 (SQLite → PostgreSQL migration hardening) — the migration needs a category model
to map into.

---

## 1. Problem

`Product.Category` is a free-text string that today holds the *name* of a two-value enum:

```csharp
public enum ProductCategory { GeneralGoods = 10, Hardware = 50 }
```

Classification is done by string comparison against that name, in three places —
`CreateProductCommandHandler`, `UpdateProductCommandHandler`, `GetLegacySalesSummaryQueryHandler`:

```csharp
var isHardware = string.Equals(command.Category, nameof(ProductCategory.Hardware),
                               StringComparison.OrdinalIgnoreCase);
```

The real stores do not work that way. Each has its own fine-grained catalogue, and **the ids collide
across store types with different meanings** (from the real databases at
`.planning/indypos-overhaul/sqlite_database/*/Store.db`):

| legacy id | GeneralHardware | MimyMart | MimyShop |
|-----------|-----------------|----------|----------|
| 10 | เบ็ดเตล็ด (misc) | เบ็ดเตล็ด | **ของขวัญ (gifts)** |
| 11 | เครื่องดื่ม (drinks) | เครื่องดื่ม | **ของเล่น (toys)** |
| 12 | ขนม (snacks) | ขนม | **เครื่องเขียน (stationery)** |
| 18 | ของเล่น (toys) | ของเล่น | **ของใช้ในบ้าน (household)** |
| 50–54 | วัสดุ* (hardware) | — | — |
| categories in the legacy table | 16 | 11 (one is a leftover — see §4) | 17 |

Three consequences:

1. **Two values cannot express three catalogues.** A minimart's เครื่องดื่ม and a gift shop's
   ของขวัญ both collapse to `GeneralGoods`, losing every distinction the stores actually use.
2. **Any global id→category mapping is wrong for at least one store.** Mapping `10 → GeneralGoods`
   is defensible for GeneralHardware and MimyMart, and meaningless for MimyShop where 10 is gifts.
   This is the same defect class as the payment-method mapping that mis-attributed ~15% of ฿21.2M
   (see `af4ea14`): an id treated as globally meaningful when it is only meaningful in context.
3. **A new category requires a redeploy**, exactly the problem Epic M solved for payment methods.

There is also a third store type, **MimyShop**, which does not exist in `StoreType` yet, and
`CoffeeShop`, which does exist but should not.

## 2. Goals

- Each store carries its own product-category catalogue as data.
- Product-type classification (the Hardware gate) stops being a string comparison.
- Adding a category is a row, not a release.
- Cross-store reporting works without a translation layer.
- `MimyShop` becomes a real store type; `CoffeeShop` is removed.

### Non-goals

- **No admin category screen.** Payment methods needed one because government campaigns churn
  roughly yearly. Categories are stable — a shop does not invent a category set often. Build it when
  there is a reason to edit categories in the field, not before.
- **No service/non-stock behaviour.** `Kind = Service` is recorded as data so MimyShop's บริการ has
  a home, but nothing changes about inventory deduction. That feature needs `Product.IsTrackable`,
  which v4 does not have (see §8).
- **No cloud work.** Store-scoped rows sync correctly under any cloud shape (see
  `.planning/indypos-overhaul/PLAN.md`, Epic I).

## 3. The data model

A store-scoped `product_category` table, mirroring `payment_method`:

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | Per the repo's entity convention |
| `StoreId` | `string` | **Mandatory scoping** — the whole point. `Code` is unique per store, not globally |
| `Code` | `string` | Stable readable identifier, e.g. `Gifts`. What `Product.Category` stores |
| `DisplayName` | `string` | Thai label, e.g. `ของขวัญ`. Reworded freely without touching products |
| `Kind` | `ProductCategoryKind` | `GeneralGoods` \| `Hardware` \| `Service` |
| `DisplayOrder` | `int` | Picker ordering |
| `IsEnabled` | `bool` | Hide from new products; history keeps rendering |
| `CreatedUtc`, `LastModifiedUtc` | `DateTime` | Repo entity convention |

Unique index on `(StoreId, Code)`.

```csharp
public enum ProductCategoryKind
{
    GeneralGoods = 1,
    Hardware = 2,
    Service = 3
}
```

`Kind` carries the gate: `MultipleProductTypesEnabled = false` means the store may only use
categories whose `Kind == GeneralGoods`. The three `nameof(ProductCategory.Hardware)` comparisons are
replaced by a lookup of the category's `Kind`.

**Removed:** `IndyPOS.Application.Common.Enums.ProductCategory` and
`HardcodedStoreConstants.ProductCategories`.

**Note on the existing `IndyPOS.Domain.Entities.ProductCategory`** (`{ int Id; string Category; }`) —
that is the *legacy-side* model used by the WinForms/legacy path, not the StoreHub Core entity. The
new entity lives in `IndyPOS.Domain.Entities.Core` alongside `PaymentMethod`. Naming them both
`ProductCategory` in different namespaces is confusing; the new one is authoritative and the legacy
one should be renamed `LegacyProductCategory` as part of this work to avoid a using-directive trap.

### Why readable codes

`Code = "Gifts"` rather than `Code = "10"`:

- Rows and reports are legible (`Category = 'PlumbingMaterials'`, not `Category = '52'`).
- A Thai label can be reworded without a data migration.
- **Cross-store reporting works by construction.** Codes are shared where meanings genuinely match,
  so `GROUP BY code` gives "Toys sales across all three stores". With legacy ids as codes that query
  would be silently wrong, since id 10 is misc in GeneralHardware and gifts in MimyShop. This
  property matters directly for Epic I reporting and the MCP surface.
- It refuses the id-as-meaning habit that produced the payment-mapping bug.

## 4. Seed data

Codes below are the contract between this epic and the migration (Epic 2), which maps
`legacy id → Code` **per store**. Translations need Pond's confirmation; they cannot be unit-tested.

### GeneralHardware (16)

| legacy id | Thai | Code | Kind |
|-----------|------|------|------|
| 10 | เบ็ดเตล็ด | `Miscellaneous` | GeneralGoods |
| 11 | เครื่องดื่ม | `Beverages` | GeneralGoods |
| 12 | ขนม | `Snacks` | GeneralGoods |
| 13 | เครื่องดื่มแอลกอฮอล์ | `AlcoholicBeverages` | GeneralGoods |
| 14 | อาหาร | `Food` | GeneralGoods |
| 15 | เครื่องเขียน | `Stationery` | GeneralGoods |
| 16 | ของใช้ในบ้าน | `Household` | GeneralGoods |
| 17 | เครื่องใช้ไฟฟ้า | `ElectricalAppliances` | GeneralGoods |
| 18 | ของเล่น | `Toys` | GeneralGoods |
| 19 | ยา | `Medicine` | GeneralGoods |
| 20 | การเกษตร | `Agriculture` | GeneralGoods |
| 50 | วัสดุและอุปกรณ์ทั่วไป | `GeneralMaterials` | **Hardware** |
| 51 | วัสดุและอุปกรณ์ | `MaterialsAndEquipment` | **Hardware** |
| 52 | วัสดุและอุปกรณ์ระบบประปา | `PlumbingMaterials` | **Hardware** |
| 53 | วัสดุและอุปกรณ์ระบบไฟฟ้า | `ElectricalMaterials` | **Hardware** |
| 54 | วัสดุก่อสร้างและอุปกรณ์การช่าง | `ConstructionMaterials` | **Hardware** |

### Minimart / MimyMart (10)

Legacy ids 10–19 with the same Thai labels and codes as GeneralHardware's 10–19, all `GeneralGoods`.

**Legacy id 20 (`การเกษตร`) is deliberately NOT seeded.** MimyMart's category table was created by
copying GeneralHardware's, and `การเกษตร` came along as a leftover (Pond, 2026-07-29). The data
agrees: **0 products and 0 invoice lines**. Seeding it would propagate an accident into the clean
model and put a category a minimart never sells in its picker.

The migration therefore maps MimyMart legacy ids 10–19 only. Id 20 needs no mapping because nothing
references it — verified, not assumed.

### MimyShop (17)

| legacy id | Thai | Code | Kind |
|-----------|------|------|------|
| 10 | ของขวัญ | `Gifts` | GeneralGoods |
| 11 | ของเล่น | `Toys` | GeneralGoods |
| 12 | เครื่องเขียน | `Stationery` | GeneralGoods |
| 13 | หนังสือและสมุด | `BooksAndNotebooks` | GeneralGoods |
| 14 | เครื่องสำอาง | `Cosmetics` | GeneralGoods |
| 15 | เครื่องประดับ | `Jewellery` | GeneralGoods |
| 16 | กระเป๋า | `Bags` | GeneralGoods |
| 17 | แฟชั่น | `Fashion` | GeneralGoods |
| 18 | ของใช้ในบ้าน | `Household` | GeneralGoods |
| 19 | เครื่องครัว | `Kitchenware` | GeneralGoods |
| 20 | ขนมและเครื่องดื่ม | `SnacksAndBeverages` | GeneralGoods |
| 21 | อุปกรณ์มือถือ | `MobileAccessories` | GeneralGoods |
| 22 | อุปกรณ์อิเล็กทรอนิกส์ | `Electronics` | GeneralGoods |
| 23 | อุปกรณ์งานปาร์ตี้ | `PartySupplies` | GeneralGoods |
| 24 | สินค้าตามเทศกาล | `SeasonalGoods` | GeneralGoods |
| 25 | บริการ | `Services` | **Service** |
| 26 | เบ็ดเตล็ด | `Miscellaneous` | GeneralGoods |

`Toys`, `Stationery`, `Household` and `Miscellaneous` deliberately repeat across store types — the
key is `(StoreId, Code)`, and shared codes are what make cross-store reporting meaningful.

**All 17 MimyShop categories are seeded, including the 12 with no products and no sales lines.**
MimyShop is a new store (15 invoices, 100 products), so its unused categories are the catalogue the
business intends to use, not detritus. Contrast MimyMart's `การเกษตร` above: identical evidence —
zero products, zero lines — opposite meaning. "Unused" alone cannot distinguish a leftover from a
plan, which is why this is recorded rather than derived by a rule. Do not "tidy up" MimyShop's empty
categories later.

### Category references in sales history — audited

Every category id appearing in `InvoiceProduct` exists in that store's `ProductCategory` table, in
all three databases. There are **no orphaned category references**, so the migration (Epic 2) cannot
meet a historical line whose category it is unable to map. Verified 2026-07-29 against the real
databases.

## 5. Store types

```csharp
public enum StoreType
{
    GeneralHardware = 1,
    Minimart = 2,
    // 3 was CoffeeShop, removed 2026-07-29. Do not reuse.
    MimyShop = 4
}
```

- `MimyShop` gets Minimart-equivalent features: `PayLaterEnabled = false`,
  `MultipleProductTypesEnabled = false`. Today only its category set differs; that is recorded in a
  comment so the duplication reads as deliberate.
- **`CoffeeShop` is removed** — its products and services differ enough to deserve a dedicated app
  (Pond, 2026-07-29). Before deleting: confirm no `appsettings.json` carries `Store:Type=CoffeeShop`
  and no `store_user`/config row persists the value. The numeric value `3` stays reserved.
- Installer `--store-type` accepts `MimyShop`; the wizard picker offers it. Documentation updated
  wherever the supported types are listed.

### Anticipated later divergence (not built now)

MimyShop sells services, and its reporting/receipt needs differ from a minimart's. Payment methods
will *converge* — MimyMart is expected to offer MimyShop's set. So resist adding feature flags for
payments; add them for services and presentation when those features are actually specified.

## 6. Seeding, API, data flow

`ProductCategorySeeder` mirrors `PaymentMethodSeeder`: idempotent, inserts only a `Code` absent for
this store, scoped by `IStoreIdentityService.StoreId`, non-fatal, logged. One difference — the seed
set is selected by `IStoreIdentityService.StoreType`, because the catalogues genuinely differ. It runs
in the existing `migrate` step, so a fresh install arrives already correct.

`GET /product-categories` mirrors `GET /payment-methods`: store-scoped rows in `DisplayOrder`, used by
the POS to render category pickers. `StoreHubInventoryProductService.MapCategoryName` and the
hardcoded dictionary are removed.

**A window worth exploiting:** no store runs v4 yet, so there is no production `Product.Category`
data to convert. The EF migration creates and seeds the table; it needs no reclassification data
migration (unlike Epic M's `MapLegacyPaymentValuesToCodes`) and puts no live data at risk. Dev and
test databases are simply reseeded. **This window closes when the first store goes live**, which is
the concrete reason Epic 1 precedes Epic 3.

## 7. Error handling

| Situation | Behaviour |
|-----------|-----------|
| Unknown `Code` on product create/update | **400** — a malformed request, not a conflict |
| Category with `Kind != GeneralGoods` on a store with `MultipleProductTypesEnabled = false` | **409**, reusing the product-type-restriction path and its tests |
| Disabled category on a **new** product | Treated as unknown → 400 |
| Disabled category on an **existing** product | Still renders; history is never rewritten (same rule as dead payment campaigns) |
| Seeder cannot write | Logged, non-fatal — matches `PaymentMethodSeeder` |

## 8. Known gap: `Product.IsTrackable`

v4's `Domain.Entities.Core.Product` has no `IsTrackable`, while the legacy schema does — and the real
databases contain non-trackable products (21 GeneralHardware, 7 MimyMart, 1 MimyShop). Services are
inherently non-stock, so `Kind = Service` is only half a feature without it.

Deliberately **not** added here: it is a migration-fidelity concern (Epic 2 must preserve the flag)
and a service-behaviour feature (later). Recorded so `Kind = Service` is not mistaken for working
non-stock support.

## 9. Testing

| Layer | Coverage |
|-------|----------|
| Domain | `ProductCategoryKind` policy — which kinds each store type may use. Pure, no infrastructure |
| Application | Create/update handlers: accept a valid code; reject an unknown code (400); reject `Hardware` on a general-only store (409) |
| Integration (real PostgreSQL) | Endpoint returns store-scoped rows in `DisplayOrder`; seeder idempotent across two runs; **each of the three store types seeds its own expected set** |
| Migration | `migrate` creates and seeds the table on a fresh database |

The per-store-type seeding test is the one that catches an id or name mix-up between MimyMart and
MimyShop — the mistake this design exists to prevent.

**Explicit limitation:** no test can confirm a *translation* is right (`Gifts` for `ของขวัญ`). The
integration tests pin whatever is agreed, so an error is consistent and visible in the seed table
rather than silent. The §4 tables need Pond's review.

## 10. Decisions log

| Decision | Rationale |
|----------|-----------|
| Store-scoped catalogue, not global | Legacy ids collide with different meanings across store types |
| Readable English `Code` | Legible rows, stable under relabelling, makes cross-store `GROUP BY` correct |
| `Kind` on the category row | Replaces string comparison against an enum name; extensible to `Service` |
| No admin screen | Categories are stable, unlike churning government campaigns (YAGNI) |
| `MimyShop` as a real `StoreType` | Pond's call: services and reporting will diverge |
| `CoffeeShop` removed, value 3 reserved | Deserves a dedicated app; reserving 3 avoids a future collision |
| `Kind = Service` as data only | Non-stock behaviour needs `Product.IsTrackable`, which does not exist |
| MimyMart's `การเกษตร` dropped, MimyShop's empty categories kept | Both are unused; one is a copy-paste leftover, the other a new shop's intended catalogue. Only Pond's context distinguishes them |
| Rename legacy `ProductCategory` entity | Two entities with one name in sibling namespaces is a using-directive trap |
