# Defect 14 — Migrated Stock Double-Count: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make migrated stock equal the number the old till displayed, by removing the historical
sales replay that subtracts every sold unit a second time.

**Architecture:** `Product` has no stock column — stock is `SUM(InventoryMovement.QuantityDelta)`.
The migrator will write exactly one `Migration:InitialStock` movement per product and no movement at
all for historical invoice lines. Negative legacy stock is clamped to zero and reported on
`MigrationResult`. The one movement is dated at migration time, not product creation.

**Tech Stack:** C# .NET 10, xunit 2.9.3, FluentAssertions, Dapper, EF Core + Npgsql, Spectre.Console
(`AnsiConsole`), Testcontainers-backed `PostgresFixture`.

**Spec:** `docs/superpowers/specs/2026-08-09-migration-stock-double-count-design.md`

## Global Constraints

- **Docker must be running.** `IndyPOS.MigrationTool.Tests` spins up a real Postgres container. With
  Docker down the suite fails fast and looks exactly like a code regression.
- **Pinning tests assert today's WRONG behaviour on purpose.** Each names its defect and records the
  correct answer in its assertion message. Never "repair" a pin because it looks backwards — read the
  comment first.
- **Every commit must leave the suite green.** Task order below exists for this reason: defect 7's
  pin is re-aimed *before* the replay is deleted, because deleting it first would break that pin.
- **Baseline:** `dotnet test IndyPOS.sln` → 546 pass / 1 skip / 0 fail with Docker up and the real
  store `.db` files present. A root `dotnet test` exits 1 via `IndyPOS.Mock` — pre-existing, ignore.
- **Never hand-edit `tests/IndyPOS.MigrationTool.Tests/LegacySchema/*.sql`.** They are generated
  dumps from real stores.
- **Branch:** `fix/migration-stock-double-count` (already created, spec already committed as
  `db7267e`).
- **Conventional commits**, one logical unit each.

---

## File Structure

| File | Change | Responsibility |
|---|---|---|
| `src/IndyPOS.MigrationTool/MigrationResult.cs` | Modify | Add the `ClampedStock` record + `ClampedStocks` list |
| `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs` | Modify | Delete the sale replay; clamp negatives; date the movement at migration time |
| `src/IndyPOS.MigrationTool/Program.cs` | Modify (`DisplayResults`) | Print the clamp list for the operator |
| `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs` | Modify | The defect-14 pin, the clamp test, the movement-date test, the re-aimed defect-7 pin |
| `.planning/indypos-overhaul/PLAN.md` | Modify | Mark defect 14 fixed; record what happened to defect 7's pin |

**Test-harness facts you will need** (all already exist — do not rebuild them):

- `MigrationScenario.RunAsync(store, postgres, dryRun = false)` returns `MigrationResult`. Defined at
  the top of `tests/IndyPOS.MigrationTool.Tests/PayLaterMigrationTests.cs:13`.
- `ProductMigrationTests` already has `SeedCashierAsync(store)` (`:19`) which adds the payment-type
  lookup and user 1.
- `LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware)` applies the generated schema.
- Builder signatures:
  - `AddProductAsync(int productId, string barcode, string description, decimal unitPrice, int quantityInStock, int? category, bool isTrackable, string dateCreated, decimal groupPrice = 0m, int? groupPriceQuantity = null)`
  - `AddInvoiceAsync(int invoiceId, int userId, decimal total, string dateCreated)`
  - `AddInvoiceLineAsync(int invoiceProductId, int invoiceId, int productId, string barcode, string description, int quantity, decimal unitPrice, decimal originalUnitPrice, ...)`
  - `AddPaymentAsync(int paymentId, int invoiceId, int paymentTypeId, decimal amount, string dateCreated)`
- `_postgres.CreateDbContext()` gives a `StoreHubDbContext` over the container.

---

### Task 1: Re-aim defect 7's pin before it is collateral damage

`MigrateProducts_CurrentlyDropsIsTrackable_Defect7` asserts a service line still produces exactly one
`Migration:Sale` movement. Task 3 deletes all sale movements, which would break it. That is **not** a
defect-7 fix: `Core.Product` still has no `IsTrackable`, so live v4 sales still deduct stock for
services. Re-aim the pin at the half that survives — doing it first keeps every commit green, because
the re-aimed assertion holds both before and after Task 3.

**Files:**
- Modify: `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs:52-84`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Replace the test body**

Replace the whole of `MigrateProducts_CurrentlyDropsIsTrackable_Defect7` (lines 52-84) with:

```csharp
    [Fact]
    public async Task MigrateProducts_CurrentlyDropsIsTrackable_Defect7()
    {
        // Defect 7: v4's Core.Product has no IsTrackable, so the legacy flag is dropped.
        // CORRECT: Product carries the flag, and CompleteSaleCommandHandler skips the stock
        // deduction for a non-trackable product.
        //
        // This pin originally asserted the MIGRATION consequence -- that a service line still
        // produced a stock-deducting Migration:Sale movement. Defect 14 (2026-08-09) removed the
        // sales replay entirely, so that consequence is gone and the assertion was re-aimed, NOT
        // repaired. What remains pinned is the LIVE consequence: the flag is still absent from
        // Product, so a service sold through v4 still deducts stock.
        //
        // Structural absence cannot be checked at compile time, so it is checked by reflection --
        // the same approach defect 8's pin uses below. A fix naming the property anything other
        // than "IsTrackable" leaves this pin green, so whoever fixes defect 7 must invert it
        // deliberately rather than rely on it failing.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 4242, barcode: "2002500000014", description: "Delivery service",
            unitPrice: 50m, quantityInStock: 0, category: 25, isTrackable: false,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 50m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 4242, barcode: "2002500000014",
            description: "Delivery service", quantity: 1, unitPrice: 50m, originalUnitPrice: 50m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 50m,
            dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        // The run still has to complete for a non-trackable product -- otherwise the reflection
        // assertion below would pass on a migration that did nothing.
        result.Products.Migrated.Should().Be(1);

        var productProperties = typeof(IndyPOS.Domain.Entities.Core.Product)
            .GetProperties().Select(p => p.Name).ToList();
        productProperties.Should().NotContain("IsTrackable",
            "defect 7: the flag is dropped, so v4 stock-tracks services it should leave alone");
    }
```

- [ ] **Step 2: Run the test and confirm it passes on unchanged production code**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "Defect7"
```

Expected: PASS. It must pass *before* the replay is deleted — that is the whole point of doing this
task first.

- [ ] **Step 3: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs
git commit -m "test(migration): re-aim defect 7's pin at the flag, not the sale movement"
```

---

### Task 2: Pin defect 14

**Files:**
- Modify: `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs` (add a test before the closing brace)

**Interfaces:**
- Consumes: nothing.
- Produces: the test `MigrateProducts_CurrentlyReplaysSalesAgainstCurrentStock_Defect14`, inverted in
  Task 3.

- [ ] **Step 1: Add the pinning test**

Add at the end of the class, after `MigrateProducts_WithStock_ShouldRecordAnInitialStockMovement`:

```csharp
    [Fact]
    public async Task MigrateProducts_CurrentlyReplaysSalesAgainstCurrentStock_Defect14()
    {
        // Defect 14: QuantityInStock is TODAY's stock -- already net of every sale the store ever
        // made. The migrator writes it as a Migration:InitialStock movement and THEN replays each
        // historical invoice line as a Migration:Sale, so every sold unit is subtracted twice.
        // Product has no stock column: InventoryMovement.cs:6 defines stock as SUM(QuantityDelta).
        // CORRECT: net stock 20 -- exactly what the old till displayed.
        // Measured on real data: GeneralHardware nets -400,541 units with 7,028 of 10,590 products
        // (66%) negative; MimyMart -249,652 with 2,350 of 6,335 (37%).
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 600m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 5, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 600m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movements = await db.InventoryMovements.ToListAsync();

        // Non-vacuity: naming both movements means this cannot pass on a run that wrote nothing,
        // and it fails loudly rather than silently when the replay is removed.
        movements.Should().HaveCount(2);
        movements.Single(m => m.Reason == "Migration:InitialStock").QuantityDelta.Should().Be(20);
        movements.Single(m => m.Reason == "Migration:Sale").QuantityDelta.Should().Be(-5);

        movements.Sum(m => m.QuantityDelta).Should().Be(15,
            "defect 14: the sale is replayed against stock that already excludes it. CORRECT is 20");
    }
```

- [ ] **Step 2: Run it and confirm it passes**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "Defect14"
```

Expected: PASS — a pin records today's behaviour, so green here is correct. If it FAILS, stop: the
production code does not do what the spec measured, and the plan needs revisiting before any fix.

- [ ] **Step 3: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs
git commit -m "test(migration): pin defect 14, stock double-counts the sales history"
```

---

### Task 3: Delete the sales replay and invert the pin

**Files:**
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:333-343`
- Modify: `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs` (invert the Task 2 pin)

**Interfaces:**
- Consumes: the pin from Task 2.
- Produces: no `Migration:Sale` movements anywhere. Task 6's date test relies on
  `Migration:InitialStock` being the only movement reason the migrator writes.

- [ ] **Step 1: Delete the movement block in `MigrateInvoicesAsync`**

Remove these lines (currently `SqliteMigrationService.cs:333-343`), leaving the
`context.InvoiceLines.Add(...)` above them untouched:

```csharp
                        // Create inventory movement for sale
                        context.InventoryMovements.Add(new InventoryMovement
                        {
                            Id = Guid.NewGuid(),
                            StoreId = _options.StoreId,
                            ProductId = productId,
                            QuantityDelta = -(int)line.Quantity,
                            Reason = "Migration:Sale",
                            ReferenceId = newInvoice.Id,
                            CreatedUtc = createdUtc
                        });
```

In its place put a comment recording why there is no movement here:

```csharp
                        // Defect 14: NO inventory movement for a historical sale. The product's
                        // QuantityInStock is today's stock, already net of every sale, so replaying
                        // lines here subtracts each sold unit a second time. The sale itself is not
                        // lost -- it is the InvoiceLine written above.
```

- [ ] **Step 2: Run the pin and watch it fail**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "Defect14"
```

Expected: FAIL on `movements.Should().HaveCount(2)` — actual 1. **Do not skip this step.** Watching
the pin go red is the proof the fix reaches the behaviour the pin describes.

- [ ] **Step 3: Invert the pin**

In `MigrateProducts_CurrentlyReplaysSalesAgainstCurrentStock_Defect14`, rename the test and replace
the comment header and the assertion block:

```csharp
    [Fact]
    public async Task MigrateProducts_DoesNotReplaySalesAgainstCurrentStock_Defect14()
    {
        // Defect 14 (fixed): QuantityInStock is TODAY's stock -- already net of every sale the store
        // ever made -- so replaying historical invoice lines as stock movements subtracted every
        // sold unit twice. GeneralHardware netted -400,541 units, 66% of products negative.
        // The migration now writes ONE movement per product and none for historical lines.
        // If this test fails with a Migration:Sale movement present, the replay has come back.
```

and, keeping the seeding identical, replace the three assertion lines with:

```csharp
        movements.Should().ContainSingle().Which.Reason.Should().Be("Migration:InitialStock");
        movements.Sum(m => m.QuantityDelta).Should().Be(20,
            "migrated stock must equal what the old till displayed");
```

- [ ] **Step 4: Run the whole migration suite**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests
```

Expected: all pass. `MigrateProducts_WithStock_ShouldRecordAnInitialStockMovement` (`:150`) and the
re-aimed defect-7 pin from Task 1 must both still be green.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs
git commit -m "fix(migration): stop replaying sales against already-net stock (defect 14)"
```

---

### Task 4: Clamp negative legacy stock and report it

1,535 products across the two large stores have negative `QuantityInStock`, reaching −3,882. The
current `> 0` guard drops them to zero silently. Keep the clamp, lose the silence.

**Files:**
- Modify: `src/IndyPOS.MigrationTool/MigrationResult.cs`
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:240-251`
- Modify: `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces:
  - `public sealed record ClampedStock(string Barcode, string ProductName, int LegacyQuantity)` in
    namespace `IndyPOS.MigrationTool`.
  - `MigrationResult.ClampedStocks` → `IReadOnlyList<ClampedStock>`.
  - `MigrationResult.AddClampedStock(string barcode, string productName, int legacyQuantity)`.
  - Task 5 consumes `ClampedStocks` for console output.

- [ ] **Step 1: Write the failing test**

Add to `ProductMigrationTests`:

```csharp
    [Fact]
    public async Task MigrateProducts_WithNegativeLegacyStock_ClampsToZeroAndReportsIt()
    {
        // QuantityInStock was never strictly maintained: restocks often went unrecorded, so 952
        // GeneralHardware products and 583 MimyMart products sit at negative stock, down to -3,882.
        // Those are unrecorded restocks, not shelf state, so they are clamped to zero -- but the
        // clamp is a deliberate data change and must be reported, not silent. The operator needs
        // the list to drive a recount.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: -5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 11, barcode: "8850001000027", description: "Sand 25kg",
            unitPrice: 80m, quantityInStock: 12, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movements = await db.InventoryMovements.ToListAsync();

        movements.Should().ContainSingle("the clamped product gets no movement, so its stock is 0");
        movements.Single().QuantityDelta.Should().Be(12);

        var clamped = result.ClampedStocks.Should().ContainSingle().Subject;
        clamped.Barcode.Should().Be("8850001000010");
        clamped.ProductName.Should().Be("Cement 50kg");
        clamped.LegacyQuantity.Should().Be(-5);

        result.IsSuccess.Should().BeTrue(
            "a clamp is a reported data decision, not a row failure -- it must not change the outcome");
    }
```

- [ ] **Step 2: Run it and verify it fails to compile**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ClampsToZero"
```

Expected: build error — `MigrationResult` does not contain a definition for `ClampedStocks`.

- [ ] **Step 3: Add the record and the collection to `MigrationResult`**

Add above `public class MigrationResult` in `src/IndyPOS.MigrationTool/MigrationResult.cs`:

```csharp
/// <summary>
/// A product whose legacy <c>QuantityInStock</c> was negative and was migrated as zero. Unrecorded
/// restocks, not shelf state -- 1,535 products across the two large stores, down to -3,882.
/// </summary>
public sealed record ClampedStock(string Barcode, string ProductName, int LegacyQuantity);
```

Inside `MigrationResult`, next to the `_errors` field:

```csharp
    private readonly List<ClampedStock> _clampedStocks = [];

    /// <summary>
    /// Products whose negative legacy stock was migrated as zero. Deliberately UNCAPPED, unlike
    /// <see cref="AddError"/>: error strings are capped because one phase failure can produce an
    /// error per invoice line (325,780 on real data), while clamps are bounded by the product count.
    /// This is not an error -- <see cref="Outcome"/> is unaffected and no row failed.
    /// </summary>
    public IReadOnlyList<ClampedStock> ClampedStocks => _clampedStocks;

    public void AddClampedStock(string barcode, string productName, int legacyQuantity) =>
        _clampedStocks.Add(new ClampedStock(barcode, productName, legacyQuantity));
```

- [ ] **Step 4: Change the guard in `MigrateProductsAsync`**

Replace the movement block at `SqliteMigrationService.cs:239-251`:

```csharp
                if (!_options.DryRun)
                {
                    context.Products.Add(newProduct);

                    // Create initial inventory movement for current stock
                    if (product.QuantityInStock > 0)
                    {
                        context.InventoryMovements.Add(new InventoryMovement
                        {
                            ...
                        });
                    }
                }
```

with:

```csharp
                // Recorded in BOTH modes on purpose: a dry run exists to preview what a real run
                // would do, and clamping stock is the one thing it does that the operator must
                // decide about beforehand.
                if (product.QuantityInStock < 0)
                {
                    _result.AddClampedStock(
                        newProduct.Barcode, newProduct.Name, (int)product.QuantityInStock);
                    _logger.LogWarning(
                        "Product {Barcode} has negative legacy stock {Quantity}; migrating as 0",
                        newProduct.Barcode, product.QuantityInStock);
                }

                if (!_options.DryRun)
                {
                    context.Products.Add(newProduct);

                    if (product.QuantityInStock > 0)
                    {
                        context.InventoryMovements.Add(new InventoryMovement
                        {
                            Id = Guid.NewGuid(),
                            StoreId = _options.StoreId,
                            ProductId = newProduct.Id,
                            QuantityDelta = (int)product.QuantityInStock,
                            Reason = "Migration:InitialStock",
                            CreatedUtc = createdUtc
                        });
                    }
                }
```

(`CreatedUtc = createdUtc` is corrected in Task 6 — leave it for now so each task has one concern.)

- [ ] **Step 5: Run the test and verify it passes**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ClampsToZero"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.MigrationTool/MigrationResult.cs src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs
git commit -m "fix(migration): report products whose negative legacy stock is clamped to zero"
```

---

### Task 5: Show the clamp list to the operator

`MigrationResult` now carries the list; nothing prints it. There is no automated test for console
output in this repo — this task is verified by eye in Task 7's real run.

**Files:**
- Modify: `src/IndyPOS.MigrationTool/Program.cs` (`DisplayResults`, after `AnsiConsole.Write(resultsTable);` at `:310`)

**Interfaces:**
- Consumes: `MigrationResult.ClampedStocks` and `ClampedStock` from Task 4.
- Produces: nothing.

- [ ] **Step 1: Add the output block**

Immediately after `AnsiConsole.Write(resultsTable);` (`Program.cs:310`) and before
`switch (result.Outcome)`:

```csharp
    // Placed above the outcome banner deliberately: on an aborted run the "discarded" warning
    // printed before the table covers this list too.
    if (result.ClampedStocks.Count > 0)
    {
        AnsiConsole.MarkupLine(
            $"\n[yellow]{result.ClampedStocks.Count} product(s) had negative stock in the legacy " +
            "database and were migrated as 0. These need a physical recount:[/]");

        foreach (var clamped in result.ClampedStocks.Take(10))
        {
            AnsiConsole.MarkupLine(
                $"  [yellow]•[/] {clamped.Barcode} {clamped.ProductName.EscapeMarkup()} " +
                $"([red]{clamped.LegacyQuantity}[/])");
        }

        if (result.ClampedStocks.Count > 10)
        {
            AnsiConsole.MarkupLine(
                $"  [grey]... and {result.ClampedStocks.Count - 10} more (see the log for all)[/]");
        }
    }
```

`EscapeMarkup()` matters: product names are free text from the shopkeeper and a stray `[` would
otherwise throw inside Spectre's markup parser.

- [ ] **Step 2: Build**

```bash
dotnet build src/IndyPOS.MigrationTool
```

Expected: 0 errors. (`EscapeMarkup` lives in `Spectre.Console`, already imported by this file.)

- [ ] **Step 3: Commit**

```bash
git add src/IndyPOS.MigrationTool/Program.cs
git commit -m "feat(migration): print the clamped-stock recount list in the run summary"
```

---

### Task 6: Date the initial-stock movement at migration time

The movement is stamped with the **product's** `DateCreated`, sometimes years back. Its quantity means
"observed at cutover", so any stock-over-time report would claim the store held today's inventory
four years ago.

**Files:**
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs` (new field, set in `MigrateAllAsync`, used in `MigrateProductsAsync`)
- Modify: `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs`

**Interfaces:**
- Consumes: `Migration:InitialStock` being the migrator's only movement reason (Task 3).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Write the failing test**

```csharp
    [Fact]
    public async Task MigrateProducts_InitialStockMovement_IsDatedAtMigrationTime()
    {
        // The quantity describes stock OBSERVED AT CUTOVER, not stock held when the product was
        // first created -- dating it 2021 would have any stock-over-time report claim the store
        // held today's inventory four years ago. One timestamp is captured per run, so every
        // product's movement shares it.
        var startedUtc = DateTime.UtcNow;

        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2021-06-01 09:00:00");
        await builder.AddProductAsync(
            productId: 11, barcode: "8850001000027", description: "Sand 25kg",
            unitPrice: 80m, quantityInStock: 12, category: 50, isTrackable: true,
            dateCreated: "2023-02-14 09:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movements = await db.InventoryMovements.ToListAsync();
        var products = await db.Products.ToListAsync();

        movements.Should().HaveCount(2);

        // The products keep their own legacy dates -- only the movement moves.
        products.Select(p => p.CreatedUtc.Year).Should().BeEquivalentTo([2021, 2023]);

        foreach (var movement in movements)
        {
            movement.CreatedUtc.Should().BeOnOrAfter(startedUtc).And.BeOnOrBefore(DateTime.UtcNow);
        }

        movements.Select(m => m.CreatedUtc).Distinct().Should().ContainSingle(
            "one timestamp is captured per run, not one per product");
    }
```

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "IsDatedAtMigrationTime"
```

Expected: FAIL — the movement dates are 2021 and 2023, so `BeOnOrAfter(startedUtc)` fails and the
`Distinct()` count is 2, not 1.

- [ ] **Step 3: Add the field**

In `SqliteMigrationService`, below `private MigrationResult _result = new();` (`:19`):

```csharp
    /// <summary>
    /// When this run started. Migration:InitialStock records stock as observed AT CUTOVER, so every
    /// such movement in a run carries this one timestamp rather than the product's creation date.
    /// Initialised here as well as in MigrateAllAsync so it is never default(DateTime).
    /// </summary>
    private DateTime _migrationStartedUtc = DateTime.UtcNow;
```

In `MigrateAllAsync`, alongside the existing reset (`:36-37`):

```csharp
        _result = new MigrationResult();
        _productIdByBarcode.Clear();
        _migrationStartedUtc = DateTime.UtcNow;
```

- [ ] **Step 4: Use it for the movement**

In `MigrateProductsAsync`, in the `Migration:InitialStock` initialiser, change:

```csharp
                            CreatedUtc = createdUtc
```

to:

```csharp
                            CreatedUtc = _migrationStartedUtc
```

Leave `newProduct.CreatedUtc = createdUtc` alone — the product keeps its legacy date.

- [ ] **Step 5: Run the test and verify it passes**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "IsDatedAtMigrationTime"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs
git commit -m "fix(migration): date the initial-stock movement at cutover, not product creation"
```

---

### Task 7: Verify against a real store shape

The console strings have no automated cover, and a previous session found two places reporting
success on a run that wrote nothing. Tests are not enough — run the tool.

**Files:** none modified.

**Interfaces:** none.

- [ ] **Step 1: Run the full solution suite**

```bash
dotnet test IndyPOS.sln
```

Expected: 0 failures. Baseline was 546 pass / 1 skip; this branch adds 3 tests (defect-14 pin, clamp,
movement date) and modifies 1, so expect **549 pass / 1 skip**. Docker must be running.

- [ ] **Step 2: Build a fixture from the committed artefact**

```bash
mkdir -p /c/temp/defect14 && cd /c/temp/defect14 && rm -f Store.db
python -c "
import sqlite3
schema = open(r'C:/personal/IndyPOS/tests/IndyPOS.MigrationTool.Tests/LegacySchema/GeneralHardware.sql', encoding='utf-8').read()
c = sqlite3.connect('Store.db')
c.executescript(schema)
c.execute(\"INSERT INTO UserRole (RoleId, RoleName) VALUES (1, 'Admin')\")
c.execute(\"INSERT INTO [User] (UserId, Username, Password, FirstName, LastName, RoleId, DateCreated) VALUES (1,'cashier','x','Somchai','Jaidee',1,'2024-03-15 09:00:00')\")
c.execute(\"INSERT INTO InventoryProduct (InventoryProductId, Barcode, Description, Category, QuantityInStock, IsTrackable, DateCreated, UnitPrice) VALUES (10,'8850001000010','Cement 50kg',50,20,1,'2021-06-01 09:00:00',120)\")
c.execute(\"INSERT INTO InventoryProduct (InventoryProductId, Barcode, Description, Category, QuantityInStock, IsTrackable, DateCreated, UnitPrice) VALUES (11,'8850001000027','Sand 25kg',50,-7,1,'2023-02-14 09:00:00',80)\")
c.execute(\"INSERT INTO Invoice (InvoiceId, UserId, Total, DateCreated) VALUES (1,1,600,'2024-03-15 14:30:00')\")
c.execute(\"INSERT INTO InvoiceProduct (InvoiceProductId, InvoiceId, InventoryProductId, Barcode, Description, Quantity, UnitPrice, OriginalUnitPrice) VALUES (1,1,10,'8850001000010','Cement 50kg',5,120,120)\")
c.execute(\"INSERT INTO Payment (PaymentId, InvoiceId, PaymentTypeId, Amount, DateCreated) VALUES (500,1,1,600,'2024-03-15 14:30:00')\")
c.commit()
print('fixture written to', c.execute('PRAGMA database_list').fetchall())
"
```

If a column in one of those INSERTs does not exist, read the real column list out of
`GeneralHardware.sql` and correct the INSERT — **never** edit the `.sql`.

- [ ] **Step 2b: Start Postgres**

```bash
docker run -d --name defect14-check -e POSTGRES_PASSWORD=pw -p 55432:5432 postgres:16-alpine
```

- [ ] **Step 3: Run the migrator**

```bash
cd /c/personal/IndyPOS
dotnet run --project src/IndyPOS.MigrationTool -- \
  --sqlite "C:/temp/defect14/Store.db" \
  --postgres "Host=localhost;Port=55432;Database=postgres;Username=postgres;Password=pw" \
  --store-id TEST-STORE
```

Expected on screen:
- the results table, then
- `1 product(s) had negative stock in the legacy database and were migrated as 0...` listing
  `8850001000027 Sand 25kg (-7)`, then
- `✓ Migration completed successfully!`

- [ ] **Step 4: Check the stock in Postgres**

```bash
docker exec defect14-check psql -U postgres -c \
  "SELECT p.\"Barcode\", COALESCE(SUM(m.\"QuantityDelta\"), 0) AS stock, COUNT(m.*) AS movements
   FROM \"Products\" p LEFT JOIN \"InventoryMovements\" m ON m.\"ProductId\" = p.\"Id\"
   GROUP BY p.\"Barcode\" ORDER BY p.\"Barcode\";"
```

Expected exactly:

| Barcode | stock | movements |
|---|---|---|
| 8850001000010 | **20** | 1 |
| 8850001000027 | **0** | 0 |

20, not 15 — the sale of 5 is not replayed. Also confirm the invoice line survived:

```bash
docker exec defect14-check psql -U postgres -c "SELECT COUNT(*) FROM \"InvoiceLines\";"
```

Expected: 1. Deleting the movement must not have deleted the line.

- [ ] **Step 5: Tear down**

```bash
docker rm -f defect14-check
```

- [ ] **Step 6: No commit** — this task changes no files. Record the observed numbers in the PR
  description in Task 8.

---

### Task 8: Update the defect ledger

**Files:**
- Modify: `.planning/indypos-overhaul/PLAN.md:157` (the defect 14 row) and `:150` (defect 7's row)

**Interfaces:** none.

- [ ] **Step 1: Mark defect 14 fixed**

In the defects table, change row 14's Status cell from
`❌ **new 2026-08-08** — not pinned yet` to `✅ Fixed <commit sha from Task 3>`, and append to its
Impact cell:

```
Fixed by removing the sales replay: QuantityInStock is migrated as the single source of stock. Also fixed in the same method (14b): the `> 0` guard silently dropped the 1,535 products already at negative legacy stock to zero -- they are still clamped to zero (QuantityInStock was never strictly maintained, so -3,882 is unrecorded restock, not shelf state) but now reported on `MigrationResult.ClampedStocks` and printed for recount.
```

- [ ] **Step 2: Record what happened to defect 7's pin**

Append to row 7's Status cell, after `**pinned** \`3ae92f1\``:

```
 — pin re-aimed 2026-08-09: it asserted the migration consequence (a service line producing a `Migration:Sale` movement), which defect 14 removed. It now asserts the live consequence, that `Core.Product` still has no `IsTrackable`. Defect 7 itself is NOT fixed
```

- [ ] **Step 3: Add a note under the table**

After the existing "Pinned means..." blockquote, add:

```markdown
> **Defect 14 changed what defect 7's pin can observe.** Removing the sales replay deleted the
> movement defect 7's pin was asserting on. The pin was re-aimed at the surviving half — the flag's
> absence from `Core.Product` — rather than repaired or deleted. If you fix defect 7, invert that
> reflection assertion deliberately; it will not fail on its own for a property named anything else.
```

- [ ] **Step 4: Commit**

```bash
git add .planning/indypos-overhaul/PLAN.md
git commit -m "docs(migration): record defect 14 fixed and defect 7's pin re-aimed"
```

---

## Done When

- `dotnet test IndyPOS.sln` → 549 pass / 1 skip / 0 fail, Docker up.
- The real run in Task 7 shows stock 20 and 0, one movement and none, one invoice line, and the
  clamp list on screen.
- `PLAN.md` says defect 14 is fixed and explains defect 7's re-aimed pin.
- Defects still open afterwards: **5, 6, 7, 8** (all pinned).

## Notes for the reviewer

- The only judgement call is **Task 1**. Deleting the sales replay makes defect 7's pin unsatisfiable
  as written; re-aiming it at the flag's structural absence keeps defect 7 pinned without pretending
  it was fixed. The alternative — deleting the pin — would lose the record that v4 still stock-tracks
  services.
- **Clamping is a data decision, not a bug fix.** Pond chose it over carrying negatives forward,
  because `QuantityInStock` was never strictly maintained. If that call is revisited, only Task 4
  changes.
