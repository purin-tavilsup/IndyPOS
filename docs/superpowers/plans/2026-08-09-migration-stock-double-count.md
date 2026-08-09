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
  migration pin is retired *before* the replay is deleted, because deleting it first would break it.
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
| `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs` | Modify | The defect-14 pin, the clamp test, the movement-date test; delete the defect-7 migration pin |
| `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs` | Modify (comment only) | Document defect 7b's trip-wire where the live harm is |
| `.planning/indypos-overhaul/PLAN.md` | Modify | Mark defect 14 fixed; split defect 7 into 7a/7b |

**Test-harness facts you will need** (all already exist — do not rebuild them):

- `MigrationScenario.RunAsync(store, postgres, dryRun = false)` returns `MigrationResult`. Defined at
  the top of `tests/IndyPOS.MigrationTool.Tests/PayLaterMigrationTests.cs:17` (the `MigrationScenario`
  class declaration is at `:13`).
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

### Task 1: Delete defect 7's migration pin and document 7b where the harm lives

`MigrateProducts_CurrentlyDropsIsTrackable_Defect7` asserts a service line still produces exactly one
`Migration:Sale` movement. Task 3 deletes all sale movements, so the pin's subject ceases to exist.
Doing this first keeps every commit green.

Defect 7 splits (see the spec): **7a** — migration replays a service sale — is closed by Task 3.
**7b** — live v4 sales deduct stock for services — is still broken and gets **no pin**, by Pond's
ruling on 2026-08-09. It cannot have an honest one: "non-trackable" cannot be expressed in v4 until
`Product` carries the flag, so any new test would be indistinguishable from the correct-behaviour
test that already exists. A reflection pin on `Product`'s property names was rejected — it goes red
when someone adds the property without using it, and would then invite a "deliberate inversion" that
records defect 7 as fixed while every service sale still deducts stock.

**Files:**
- Modify: `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs:52-84` (delete)
- Modify: `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs:139-144` (comment only)

**Interfaces:**
- Consumes: nothing.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Delete the migration pin**

Delete `MigrateProducts_CurrentlyDropsIsTrackable_Defect7` from `ProductMigrationTests.cs` in full —
lines 52-84, i.e. from the `[Fact]` attribute through the method's closing brace, plus the blank line
that separated it from the next test. Do not leave a stub or a `[Fact(Skip=...)]`.

- [ ] **Step 2: Comment the 7b trip-wire**

In `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs`,
inside `HandleAsync_ShouldCreateInventoryMovementsForEachLine`, replace this (currently `:139-141`):

```csharp
        // Assert
        capturedMovements.Should().NotBeNull();
        capturedMovements.Should().HaveCount(2);
```

with:

```csharp
        // Assert
        capturedMovements.Should().NotBeNull();

        // DEFECT 7b -- this is NOT a pinning test. Two ordinary products SHOULD produce two
        // movements, so the assertion below is correct as it stands.
        //
        // It is a trip-wire. v4's Core.Product has no IsTrackable, so CompleteSaleCommandHandler
        // (:89-100) builds a movement for EVERY line -- including services, which have no stock.
        // 21/7/1 products across the three real stores are non-trackable, and the migration gives
        // all 29 of them stock, so the harm starts at their first v4 sale.
        //
        // The day Product gains IsTrackable, CreateTestProduct below will not set it and this count
        // will break. DO NOT repair the number. Add a non-trackable line to this test and assert it
        // produces NO movement -- that is the assertion defect 7b has been waiting for.
        //
        // Defect 7b is deliberately unpinned: "non-trackable" cannot be expressed until the flag
        // exists, so any pin today would just duplicate this test. See PLAN.md's defect table.
        capturedMovements.Should().HaveCount(2);
```

- [ ] **Step 3: Run both affected suites**

```bash
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "Defect7"
dotnet test tests/IndyPOS.Application.Tests --filter "CompleteSaleCommandHandler"
```

Expected: the first now matches only `LegacyStoreDataBuilderTests…_Defect7Addendum` (a
fixture-fidelity guard that never runs the migrator) and passes. The second passes unchanged — Step 2
adds only comments. If the Application suite goes red, you edited an assertion; revert and redo.

- [ ] **Step 4: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs
git commit -m "test(migration): retire defect 7's migration pin, document 7b at the handler"
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

Replace the whole `MigrateProducts_CurrentlyReplaysSalesAgainstCurrentStock_Defect14` method — from
its `[Fact]` attribute through its closing brace — with the following. This is the complete final
method: name, comment, seeding and assertions. Do not merge it by hand with what is there.

```csharp
    [Fact]
    public async Task MigrateProducts_DoesNotReplaySalesAgainstCurrentStock_Defect14()
    {
        // Defect 14 (fixed 2026-08-09): QuantityInStock is TODAY's stock -- already net of every
        // sale the store ever made -- so replaying historical invoice lines as stock movements
        // subtracted every sold unit twice. Measured before the fix: GeneralHardware netted
        // -400,541 units with 7,028 of 10,590 products (66%) negative; MimyMart -249,652 with
        // 2,350 of 6,335 (37%). Product has no stock column: InventoryMovement.cs:6 defines stock
        // as SUM(QuantityDelta).
        // The migration now writes ONE movement per product and none for historical lines. If this
        // test fails with a Migration:Sale movement present, the replay has come back.
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

        movements.Should().ContainSingle().Which.Reason.Should().Be("Migration:InitialStock");
        movements.Sum(m => m.QuantityDelta).Should().Be(20,
            "migrated stock must equal what the old till displayed");

        // The sale itself is not lost -- it is the invoice line.
        (await db.InvoiceLines.CountAsync()).Should().Be(1);
    }
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
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:235-252`
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

Add to `ProductMigrationTests`, immediately after
`MigrateProducts_DoesNotReplaySalesAgainstCurrentStock_Defect14` (the test Task 3 produced):

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

Replace **lines 235-252** — the entire `if (!_options.DryRun)` block, from the `if (!_options.DryRun)`
line through its closing brace, which is exactly the snippet below. Note the "before" includes
`context.Products.Add(newProduct);` and the outer braces; replacing only the inner `if` would leave a
nested duplicate and an orphan brace that closes the `try` early.

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

Add to `ProductMigrationTests`, immediately after
`MigrateProducts_WithNegativeLegacyStock_ClampsToZeroAndReportsIt` (the test Task 4 added):

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

⚠️ **`CreatedUtc = createdUtc` appears in five initialisers in this file** (product, invoice, invoice
line, initial-stock movement, and — until Task 3 — the sale movement). Change **exactly one**: the
occurrence inside the object initialiser that also contains `Reason = "Migration:InitialStock"`. A
replace-all silently corrupts invoice dates and re-breaks defects 4 and 11.

In that one initialiser, change:

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

⚠️ **Run every command in this task in Git Bash, not PowerShell.** `mkdir -p`, `rm -f`, `/c/...`
paths, `\` line continuations and the `python -c "..."` block are all bash syntax.

**Files:** none modified.

**Interfaces:** none.

- [ ] **Step 1: Run the full solution suite**

```bash
dotnet test IndyPOS.sln
```

Expected: 0 failures, **549 pass / 1 skip**. Baseline is 546 pass / 1 skip (547 discovered, verified
with `dotnet test IndyPOS.sln --list-tests`); this branch adds 3 `[Fact]` methods — the defect-14 pin,
the clamp, the movement date — and rewrites two existing ones without changing the count. Docker must
be running.

⚠️ `CLAUDE.md` still claims 546 total / 545 pass and "MigrationTool 86". That is **stale** since
defect 13's fix. Trust the `--list-tests` number over the doc; fixing the doc is not part of this
plan.

- [ ] **Step 2: Build a fixture from the committed artefact**

```bash
mkdir -p /c/temp/defect14 && rm -f /c/temp/defect14/Store.db
python -c "
import sqlite3
schema = open(r'C:/personal/IndyPOS/tests/IndyPOS.MigrationTool.Tests/LegacySchema/GeneralHardware.sql', encoding='utf-8').read()
c = sqlite3.connect(r'C:/temp/defect14/Store.db')
c.executescript(schema)
c.execute(\"INSERT INTO UserRole (Id, Role) VALUES (1, 'Admin')\")
c.execute(\"INSERT INTO [User] (UserId, FirstName, LastName, RoleId, DateCreated) VALUES (1,'Somchai','Jaidee',1,'2024-03-15 09:00:00')\")
c.execute(\"INSERT INTO UserCredential (UserId, Username, Password, DateCreated) VALUES (1,'cashier','legacy-3des-hash','2024-03-15 09:00:00')\")
c.execute(\"INSERT INTO InventoryProduct (InventoryProductId, Barcode, Description, Category, QuantityInStock, IsTrackable, DateCreated, UnitPrice) VALUES (10,'8850001000010','Cement 50kg',50,20,1,'2021-06-01 09:00:00',120)\")
c.execute(\"INSERT INTO InventoryProduct (InventoryProductId, Barcode, Description, Category, QuantityInStock, IsTrackable, DateCreated, UnitPrice) VALUES (11,'8850001000027','Sand 25kg',50,-7,1,'2023-02-14 09:00:00',80)\")
c.execute(\"INSERT INTO Invoice (InvoiceId, UserId, Total, DateCreated) VALUES (1,1,600,'2024-03-15 14:30:00')\")
c.execute(\"INSERT INTO InvoiceProduct (InvoiceProductId, InvoiceId, InventoryProductId, Barcode, Description, Quantity, UnitPrice, OriginalUnitPrice) VALUES (1,1,10,'8850001000010','Cement 50kg',5,120,120)\")
c.execute(\"INSERT INTO Payment (PaymentId, InvoiceId, PaymentTypeId, Amount, DateCreated) VALUES (500,1,1,600,'2024-03-15 14:30:00')\")
c.commit()
print('fixture rows:', c.execute('SELECT COUNT(*) FROM InventoryProduct').fetchone())
"
```

⚠️ **`UserCredential` is a separate table and the third INSERT is not optional.** Legacy `User` has no
`Username`/`Password` columns, and `UserRole` is `(Id, Role)` — not `(RoleId, RoleName)`. Without the
credential row, `MigrateUsersAsync` (`SqliteMigrationService.cs:135-140`) skips the user,
`UserIdMap` is empty, and the invoice gets `Guid.Empty` from the fallback at `:286` — the run still
reports success while writing a junk `user_id`, which would make Step 3's expected output a lie.

No `PaymentType` rows are needed: `LegacyPaymentTypeMap` is a static dictionary, and there is no FK
from `payment` to `payment_method`.

If any column still does not exist, read the real column list out of `GeneralHardware.sql` and correct
the INSERT — **never** edit the `.sql`.

- [ ] **Step 2b: Start Postgres**

```bash
docker run -d --name defect14-check -e POSTGRES_PASSWORD=pw -p 55432:5432 postgres:16-alpine
until docker exec defect14-check pg_isready -U postgres -q; do sleep 1; done
```

`docker run -d` returns before the server accepts connections, so without the wait Step 3 usually
hits connection-refused on the first attempt.

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

The StoreHub schema is **snake_case and singular** — `product`, `inventory_movement`, `invoice_line`,
with columns `barcode`, `quantity_delta`, `product_id`, `id` (see
`Persistence/StoreHub/Configurations/*.cs`, each of which calls `ToTable` and `HasColumnName`
explicitly). Unquoted lowercase identifiers need no escaping:

```bash
docker exec defect14-check psql -U postgres -c \
  "SELECT p.barcode, COALESCE(SUM(m.quantity_delta), 0) AS stock, COUNT(m.id) AS movements
   FROM product p LEFT JOIN inventory_movement m ON m.product_id = p.id
   GROUP BY p.barcode ORDER BY p.barcode;"
```

Expected exactly:

| Barcode | stock | movements |
|---|---|---|
| 8850001000010 | **20** | 1 |
| 8850001000027 | **0** | 0 |

20, not 15 — the sale of 5 is not replayed. Also confirm the invoice line survived:

```bash
docker exec defect14-check psql -U postgres -c "SELECT COUNT(*) FROM invoice_line;"
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

Row 14 is at `PLAN.md:157`. Change its Status cell from `❌ **new 2026-08-08** — not pinned yet` to
`✅ Fixed <Task 3 sha> (14b: <Task 4 sha>)` — two SHAs, because the double-count and the negative-stock
clamp land in different commits. Append to its Impact cell:

```
Fixed by removing the sales replay: QuantityInStock is migrated as the single source of stock. Also fixed in the same method (14b): the `> 0` guard silently dropped the 1,535 products already at negative legacy stock to zero -- they are still clamped to zero (QuantityInStock was never strictly maintained, so -3,882 is unrecorded restock, not shelf state) but now reported on `MigrationResult.ClampedStocks` and printed for recount.
```

- [ ] **Step 2: Split defect 7 into 7a and 7b**

Row 7 is at `PLAN.md:150`. ⚠️ Anchor on the **full** Status cell text
`❌ **priority raised** — **pinned** \`3ae92f1\`` — the substring `` **pinned** `3ae92f1` `` alone
also appears in rows 5 and 8, so a naive find/replace hits the wrong row.

Replace that Status cell with:

```
❌ **split 2026-08-09.** **7a** (migration replays a service sale as a stock deduction) ✅ closed by defect 14 — the replay is gone. **7b** (live v4 sales deduct stock for services) ❌ **open and deliberately UNPINNED** — see the note below
```

- [ ] **Step 3: Add a note under the table**

After the existing "Pinned means..." blockquote, add:

```markdown
> **Defect 7 has no pin, on purpose (Pond, 2026-08-09).** Its pin used to assert that migrating a
> service line produced a `Migration:Sale` stock movement. Defect 14 deleted that replay, so the pin's
> subject ceased to exist and the pin was **deleted, not re-aimed**. A replacement asserting by
> reflection that `Core.Product` has no `IsTrackable` was considered and rejected: it goes red when
> someone adds the property without using it, and would then invite an "inversion" that records
> defect 7 as fixed while `CompleteSaleCommandHandler.cs:89-100` still builds a movement for every
> line. Defect 8's pin gets away with reflection because its defect *is* a structural absence;
> 7b's harm is behavioural.
>
> 7b cannot be pinned honestly today — "non-trackable" cannot be expressed until `Product` carries the
> flag, so any test would duplicate the correct-behaviour one. It is documented instead, as a
> commented trip-wire at `CompleteSaleCommandHandlerTests.cs` (`HandleAsync_ShouldCreateInventoryMovementsForEachLine`):
> when the flag arrives, that test's `HaveCount(2)` breaks, and the comment says to add a
> non-trackable line expecting **1** movement rather than repair the number.
>
> Measured 2026-08-09: all 29 non-trackable products across the three stores have nonzero
> `QuantityInStock` (mostly a sentinel 1; MimyMart's ice holds 15, 100, 100), so the migration gives
> them stock and 7b's harm starts at their first v4 sale.
>
> A second test still carries defect 7's number and is unaffected by all of this:
> `LegacyStoreDataBuilderTests…_Defect7Addendum` is a fixture-fidelity guard on SQLite's `DEFAULT 1`
> and never runs the migrator.
```

- [ ] **Step 4: Commit**

```bash
git add .planning/indypos-overhaul/PLAN.md
git commit -m "docs(migration): record defect 14 fixed and defect 7 split into 7a/7b"
```

---

## Done When

- `dotnet test IndyPOS.sln` → 549 pass / 1 skip / 0 fail, Docker up.
- The real run in Task 7 shows stock 20 and 0, one movement and none, one invoice line, and the
  clamp list on screen.
- `PLAN.md` says defect 14 is fixed and explains why defect 7b has no pin.
- Defects still open afterwards: **5, 6, 8** (pinned) and **7b** (open, deliberately unpinned).

## Notes for the reviewer

- **Task 1 is the judgement call**, and it was revised after review. The first draft re-aimed defect
  7's pin at `Core.Product`'s property names by reflection. That pin can flip red when someone adds
  `IsTrackable` without wiring it into `CompleteSaleCommandHandler` — and its own comment would then
  invite an inversion recording defect 7 as fixed while every service sale still deducts stock. A pin
  that can report a false fix is worse than no pin, so it is deleted and 7b is documented at the
  handler test instead. Pond ruled "no pin is fine, just document it clearly" on 2026-08-09.
- **Clamping is a data decision, not a bug fix.** Pond chose it over carrying negatives forward,
  because `QuantityInStock` was never strictly maintained. If that call is revisited, only Task 4
  changes.
- **Task 7 was wrong in its first draft** in two ways that three reviewers caught: the fixture
  INSERTs used a legacy user schema that does not exist (`UserCredential` is a separate table), and
  the verification queries used PascalCase table names against a snake_case schema. Both are fixed
  and verified against the real files — but it is the part of the plan most worth re-checking if it
  misbehaves, since none of it is covered by a test.
