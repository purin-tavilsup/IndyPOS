# Migration Test Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the SQLite → PostgreSQL migration real test coverage against a legacy schema extracted from a real `Store.db`, and fix the three defects that currently stop the tool completing against any real store.

**Architecture:** Extend `tests/IndyPOS.MigrationTool.Tests` (it already references the shipped `IndyPOS.MigrationTool` and has a Testcontainers `PostgresFixture`). Replace the hand-written `SqliteTestDataSeeder` schema with two committed `.sql` artefacts dumped from real store databases, drive them with a deterministic row builder, and express the defects we are not fixing yet as **pinning tests** that assert today's wrong behaviour and name their defect.

**Tech Stack:** .NET 10, xUnit 2.9.3, FluentAssertions 8.3.0, Testcontainers.PostgreSql 4.4.0, System.Data.SQLite.Core 1.0.119, Dapper 2.1.72, EF Core (Npgsql).

**Spec:** `docs/superpowers/specs/2026-08-03-migration-test-coverage-design.md`

## Global Constraints

- **Target framework** is `net10.0-windows`; do not change it.
- **`tests/IndyPOS.MigrationTool.Tests` is the only test project touched.** Do not create a new test project.
- **Never hand-write legacy DDL.** All legacy schema comes from `LegacySchema/*.sql`, which is generated. Hand-writing the schema is the defect this whole plan exists to remove.
- **Never insert `InvoiceProduct.IsTrackable`.** SQLite's `DEFAULT 1` must supply it, reproducing the real dead-data condition (602,114 real lines, every one `1`).
- **Legacy payment type ids are: 1 Cash, 2 PayLater, 3 WelfareCard, 4 M33WeLove, 5 MoneyTransfer, 6 instalments (unmapped), 7 FiftyFifty, 8 WeWin.** Use `LegacyPaymentTypeMap`; never invent ids. The old seeder's `1,2,3,4 = Cash,Card,Transfer,PayLater` comment is defect 1's scrambled map and must not be copied.
- **Pinning-test naming:** `<Method>_<Condition>_Currently<WrongBehaviour>_Defect<N>`, with a comment giving the correct answer.
- **IndyPOS is a PUBLIC repo.** No real store data in any committed file. Schema only.
- **`Customers` and `Installments` are obsolete and never used, in every store** (Pond, 2026-08-03;
  0 rows measured in GeneralHardware, the only store that has them). Nothing migrates them, ever.
  They stay in the `GeneralHardware.sql` artefact only because that file is a **faithful dump** — do
  not hand-remove them, or the artefact stops matching what the extractor produces. Legacy payment
  type 6 (`ผ่อนชำระ`, instalments) is dead with them and must stay unmapped.
- **Money in assertions** uses `decimal` literals (`19.99m`), never `double`.
- **Every date test pins `CultureInfo`** explicitly. Without it, defect 11's test only passes on a Thai-locale machine and defect 4's only on a non-Thai one.
- Run tests with `dotnet test tests/IndyPOS.MigrationTool.Tests`. **Docker must be running.**

## Deviation from the spec, recorded deliberately

The spec proposed `scripts/extract-legacy-schema.ps1`. **This plan uses a skipped xUnit test instead**
(`Tools/LegacySchemaExtractor.cs`), because PowerShell has no dependable SQLite provider — `System.Data.SQLite`
ships native `SQLite.Interop.dll` whose load path is fragile outside a .NET project — while the test project
already references it. It also matches the idiom already in the repo (`CreateTestDatabaseRunner`, a skipped
`[Fact]` used as a manual tool). Amend the spec's §4.1 when this lands.

## File Structure

| File | Responsibility |
|---|---|
| `tests/.../Tools/LegacySchemaExtractor.cs` | **New.** Skipped `[Fact]`; dumps `sqlite_master` DDL from a real `Store.db` to a `.sql` artefact. Manual dev tool. |
| `tests/.../LegacySchema/GeneralHardware.sql` | **New, generated.** 13 tables, has `PayLater`. |
| `tests/.../LegacySchema/MimyShop.sql` | **New, generated.** 10 tables, no `PayLater`. |
| `tests/.../Fixtures/LegacyStoreDatabase.cs` | **New.** Temp SQLite file + applies one artefact. |
| `tests/.../Fixtures/LegacyStoreDataBuilder.cs` | **New.** Deterministic row inserts using real column names. |
| `tests/.../LegacySchemaArtefactTests.cs` | **New.** Guards the artefacts' table and column sets. |
| `tests/.../PayLaterMigrationTests.cs` | **New.** Defects 2, 3, 10 + PayLater value coverage. |
| `tests/.../MigrationDateTests.cs` | **New.** Pinning: defects 4, 11. |
| `tests/.../ProductMigrationTests.cs` | **New.** Pinning: defects 5, 7, 8 + `NUMERIC` round-trip. |
| `tests/.../InvoiceLineMigrationTests.cs` | **New.** Pinning: defect 6. |
| `tests/.../PaymentMigrationTests.cs` | **New.** Per-method green coverage (defect 1 guard). |
| `tests/.../SqliteMigrationServiceTests.cs` | **Rewrite.** Whole-run + the 3 surviving behavioural tests. |
| `tests/.../MigrationVerifierTests.cs` | **Modify.** Swap fixture only; assertions unchanged. |
| `tests/.../RealStoreSchemaTests.cs` | **Replaces** `RealDatabaseMigrationTests.cs`. Loud skips, repo-root path, exact column sets. |
| `tests/.../TestData/SqliteTestDataSeeder.cs` | **Delete** (Task 5). Hand-written schema + scrambled payment ids. |
| `tests/.../TestData/CreateTestDatabase.cs` | **Delete** (Task 1). Copies real rows; output read by no test. |
| `tests/.../CreateTestDatabaseRunner.cs` | **Delete** (Task 1). |
| `tests/.../create_test_db.ps1` | **Delete** (Task 1). |
| `src/IndyPOS.MigrationTool/MigrationResult.cs` | **Modify.** Add `PaymentIdMap`. |
| `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs` | **Modify.** Defects 2, 3, 10. |

---

### Task 1: DDL extraction tool and the two committed schema artefacts

Replaces the dead `CreateTestDatabase` tool with one that emits **schema only**, and lands the artefacts every later task depends on.

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/Tools/LegacySchemaExtractor.cs`
- Create: `tests/IndyPOS.MigrationTool.Tests/LegacySchema/GeneralHardware.sql`
- Create: `tests/IndyPOS.MigrationTool.Tests/LegacySchema/MimyShop.sql`
- Create: `tests/IndyPOS.MigrationTool.Tests/LegacySchemaArtefactTests.cs`
- Modify: `tests/IndyPOS.MigrationTool.Tests/IndyPOS.MigrationTool.Tests.csproj`
- Delete: `tests/IndyPOS.MigrationTool.Tests/TestData/CreateTestDatabase.cs`
- Delete: `tests/IndyPOS.MigrationTool.Tests/CreateTestDatabaseRunner.cs`
- Delete: `tests/IndyPOS.MigrationTool.Tests/create_test_db.ps1`

**Interfaces:**
- Consumes: nothing.
- Produces: `LegacySchema/GeneralHardware.sql` and `LegacySchema/MimyShop.sql`, copied to the test output directory. `LegacyStoreShape` enum with members `GeneralHardware` and `MimyShop`.

- [ ] **Step 1: Add the shape enum and the extractor tool**

Create `tests/IndyPOS.MigrationTool.Tests/Tools/LegacySchemaExtractor.cs`:

```csharp
using System.Data.SQLite;
using System.Text;
using Dapper;

namespace IndyPOS.MigrationTool.Tests.Tools;

/// <summary>
/// Which real store a legacy schema artefact was dumped from. The two shapes differ only by the
/// PayLater feature: GeneralHardware adds PayLater, Customers and Installments.
/// </summary>
public enum LegacyStoreShape
{
    /// <summary>13 tables. Has PayLater.</summary>
    GeneralHardware,

    /// <summary>10 tables. No PayLater. MimyMart shares this shape.</summary>
    MimyShop
}

/// <summary>
/// Manual developer tool. Dumps the legacy SQLite DDL from a real Store.db into a committed
/// .sql artefact, so tests never hand-write the legacy schema.
///
/// Hand-writing it is exactly what produced a fixture schema no store has -- see
/// docs/superpowers/specs/2026-08-03-migration-test-coverage-design.md.
///
/// SCHEMA ONLY. This must never copy rows: IndyPOS is a public repository.
///
/// Run with:
///   dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"
/// </summary>
public class LegacySchemaExtractor
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string SourceDb(LegacyStoreShape shape) => Path.Combine(
        RepoRoot, ".planning", "indypos-overhaul", "sqlite_database", shape.ToString(), "Store.db");

    private static string ArtefactPath(LegacyStoreShape shape) => Path.Combine(
        RepoRoot, "tests", "IndyPOS.MigrationTool.Tests", "LegacySchema", $"{shape}.sql");

    [Fact(Skip = "Manual tool. Run explicitly to regenerate LegacySchema/*.sql from a real Store.db.")]
    public async Task ExtractLegacySchema()
    {
        foreach (var shape in Enum.GetValues<LegacyStoreShape>())
        {
            var source = SourceDb(shape);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(
                    $"Real store database not found for {shape}: {source}. " +
                    "These files are gitignored; obtain them before regenerating the artefacts.",
                    source);
            }

            await File.WriteAllTextAsync(ArtefactPath(shape), await BuildArtefactAsync(shape, source));
        }
    }

    private static async Task<string> BuildArtefactAsync(LegacyStoreShape shape, string sourceDbPath)
    {
        await using var source = new SQLiteConnection($"Data Source={sourceDbPath};Version=3;");
        await source.OpenAsync();

        var tables = (await source.QueryAsync<(string Name, string? Sql)>("""
            SELECT name AS Name, sql AS Sql
            FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name
            """)).ToList();

        var artefact = new StringBuilder();
        artefact.AppendLine($"-- GENERATED FILE -- DO NOT HAND-EDIT.");
        artefact.AppendLine($"-- Legacy SQLite schema dumped from a real {shape} Store.db.");
        artefact.AppendLine($"-- Regenerate with:");
        artefact.AppendLine($"--   dotnet test tests/IndyPOS.MigrationTool.Tests --filter \"ExtractLegacySchema\"");
        artefact.AppendLine($"-- Tables: {tables.Count}");
        artefact.AppendLine($"-- Schema only. Never add rows: this repository is public.");
        artefact.AppendLine();

        foreach (var (name, sql) in tables)
        {
            if (string.IsNullOrWhiteSpace(sql)) continue;
            artefact.AppendLine($"-- {name}");
            artefact.AppendLine($"{sql.TrimEnd()};");
            artefact.AppendLine();
        }

        return artefact.ToString();
    }
}
```

- [ ] **Step 2: Create `LegacySchema/GeneralHardware.sql`**

This is the verified dump of the real GeneralHardware store (13 tables). Create it with exactly this content — Step 5's test verifies it, and Step 1's tool regenerates it when a real `.db` is available.

```sql
-- GENERATED FILE -- DO NOT HAND-EDIT.
-- Legacy SQLite schema dumped from a real GeneralHardware Store.db.
-- Regenerate with:
--   dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"
-- Tables: 13
-- Schema only. Never add rows: this repository is public.

-- Customers
CREATE TABLE "Customers" (
	"CustomerId"	INTEGER NOT NULL UNIQUE,
	"FirstName"	TEXT,
	"LastName"	TEXT,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT,
	PRIMARY KEY("CustomerId" AUTOINCREMENT)
);

-- Installments
CREATE TABLE "Installments" (
	"CustomerId"	INTEGER NOT NULL,
	"Installment"	TEXT NOT NULL,
	"NumberOfInstallments"	INTEGER NOT NULL,
	"Total"	TEXT,
	"DateCreated"	TEXT,
	"DueDate"	TEXT
);

-- InventoryProduct
CREATE TABLE "InventoryProduct" (
	"InventoryProductId"	INTEGER NOT NULL UNIQUE,
	"Barcode"	TEXT NOT NULL UNIQUE,
	"Description"	TEXT NOT NULL,
	"Manufacturer"	TEXT,
	"Brand"	TEXT,
	"Category"	INTEGER,
	"QuantityInStock"	INTEGER NOT NULL DEFAULT 1,
	"GroupPriceQuantity"	INTEGER,
	"IsTrackable"	INTEGER DEFAULT 1,
	"DateCreated"	TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT, UnitPrice NUMERIC NOT NULL DEFAULT 0, GroupPrice NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("InventoryProductId" AUTOINCREMENT)
);

-- Invoice
CREATE TABLE "Invoice" (
	"InvoiceId"	INTEGER NOT NULL UNIQUE,
	"UserId"	INTEGER NOT NULL,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP, Total NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("InvoiceId" AUTOINCREMENT)
);

-- InvoiceProduct
CREATE TABLE "InvoiceProduct" (
	"InvoiceProductId"	INTEGER NOT NULL UNIQUE,
	"Priority"	INTEGER,
	"InvoiceId"	INTEGER NOT NULL,
	"InventoryProductId"	INTEGER NOT NULL,
	"Barcode"	TEXT,
	"Description"	TEXT NOT NULL,
	"Manufacturer"	TEXT,
	"Brand"	TEXT,
	"Category"	INTEGER,
	"Quantity"	INTEGER NOT NULL DEFAULT 1,
	"IsTrackable"	INTEGER DEFAULT 1,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"Note"	TEXT, UnitPrice NUMERIC NOT NULL DEFAULT 0, GroupPrice NUMERIC NOT NULL DEFAULT 0, IsGroupProduct INTEGER NOT NULL DEFAULT 0, OriginalUnitPrice NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("InvoiceProductId" AUTOINCREMENT)
);

-- PayLater
CREATE TABLE "PayLater" (
	"PaymentId"	INTEGER NOT NULL UNIQUE,
	"Description"	TEXT,
	"InvoiceId"	INTEGER NOT NULL,
	"IsCompleted"	INTEGER NOT NULL DEFAULT 0,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT, PayLaterAmount NUMERIC NOT NULL DEFAULT 0, PaidAmount NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("PaymentId")
);

-- Payment
CREATE TABLE "Payment" (
	"PaymentId"	INTEGER NOT NULL UNIQUE,
	"InvoiceId"	INTEGER NOT NULL,
	"PaymentTypeId"	INTEGER NOT NULL DEFAULT 1,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"Note"	TEXT, Amount NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("PaymentId" AUTOINCREMENT)
);

-- PaymentType
CREATE TABLE "PaymentType" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Type"	TEXT,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

-- ProductBarcodeCounter
CREATE TABLE "ProductBarcodeCounter" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Counter"	INTEGER,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

-- ProductCategory
CREATE TABLE "ProductCategory" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Category"	TEXT NOT NULL,
	PRIMARY KEY("Id")
);

-- User
CREATE TABLE "User" (
	"UserId"	INTEGER NOT NULL UNIQUE,
	"FirstName"	TEXT,
	"LastName"	TEXT,
	"RoleId"	INTEGER NOT NULL,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT,
	PRIMARY KEY("UserId" AUTOINCREMENT)
);

-- UserCredential
CREATE TABLE "UserCredential" (
	"UserId"	INTEGER NOT NULL UNIQUE,
	"Username"	TEXT,
	"Password"	TEXT NOT NULL,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT,
	PRIMARY KEY("UserId")
);

-- UserRole
CREATE TABLE "UserRole" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Role"	TEXT NOT NULL,
	PRIMARY KEY("Id")
);
```

- [ ] **Step 3: Create `LegacySchema/MimyShop.sql`**

Verified: MimyShop's 10 shared tables are **byte-identical DDL** to GeneralHardware's. So this file is GeneralHardware.sql minus the `Customers`, `Installments` and `PayLater` blocks, with a header saying 10 tables.

Take the file from Step 2, change the header to:

```sql
-- GENERATED FILE -- DO NOT HAND-EDIT.
-- Legacy SQLite schema dumped from a real MimyShop Store.db.
-- Regenerate with:
--   dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"
-- Tables: 10
-- Schema only. Never add rows: this repository is public.
```

then delete the `-- Customers`, `-- Installments` and `-- PayLater` blocks entirely. Keep the other ten
blocks byte-for-byte identical.

- [ ] **Step 4: Make the artefacts available at test runtime**

In `tests/IndyPOS.MigrationTool.Tests/IndyPOS.MigrationTool.Tests.csproj`, add this `ItemGroup`:

```xml
  <ItemGroup>
    <None Include="LegacySchema\*.sql" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

- [ ] **Step 5: Write the failing artefact test**

Create `tests/IndyPOS.MigrationTool.Tests/LegacySchemaArtefactTests.cs`:

```csharp
using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Guards the generated legacy schema artefacts. If one of these fails, the artefact was
/// hand-edited or regenerated from something that is not a real store.
/// </summary>
public class LegacySchemaArtefactTests
{
    private static async Task<SQLiteConnection> ApplyAsync(LegacyStoreShape shape)
    {
        var ddl = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "LegacySchema", $"{shape}.sql"));

        var connection = new SQLiteConnection("Data Source=:memory:;Version=3;");
        await connection.OpenAsync();
        await connection.ExecuteAsync(ddl);
        return connection;
    }

    private static async Task<List<string>> TablesAsync(SQLiteConnection connection) =>
        (await connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"))
        .ToList();

    private static async Task<List<string>> ColumnsAsync(SQLiteConnection connection, string table) =>
        (await connection.QueryAsync<string>($"SELECT name FROM pragma_table_info('{table}')")).ToList();

    [Fact]
    public async Task GeneralHardwareArtefact_ShouldDeclareTheThirteenRealTables()
    {
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        (await TablesAsync(db)).Should().BeEquivalentTo([
            "Customers", "Installments", "InventoryProduct", "Invoice", "InvoiceProduct",
            "PayLater", "Payment", "PaymentType", "ProductBarcodeCounter", "ProductCategory",
            "User", "UserCredential", "UserRole"
        ]);
    }

    [Fact]
    public async Task MimyShopArtefact_ShouldDeclareTenTablesAndNoPayLater()
    {
        await using var db = await ApplyAsync(LegacyStoreShape.MimyShop);

        var tables = await TablesAsync(db);

        tables.Should().BeEquivalentTo([
            "InventoryProduct", "Invoice", "InvoiceProduct", "Payment", "PaymentType",
            "ProductBarcodeCounter", "ProductCategory", "User", "UserCredential", "UserRole"
        ]);
        tables.Should().NotContain("PayLater",
            "the MimyShop shape exists precisely to exercise a store with no PayLater feature");
    }

    [Fact]
    public async Task InvoiceProduct_ShouldHaveAllSeventeenRealColumns()
    {
        // Defect 6: the migrator SELECTs only 7 of these. The five that carry the discount and
        // group-pricing record are OriginalUnitPrice, GroupPrice, IsGroupProduct, Note, Priority.
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        (await ColumnsAsync(db, "InvoiceProduct")).Should().BeEquivalentTo([
            "InvoiceProductId", "Priority", "InvoiceId", "InventoryProductId", "Barcode",
            "Description", "Manufacturer", "Brand", "Category", "Quantity", "IsTrackable",
            "DateCreated", "Note", "UnitPrice", "GroupPrice", "IsGroupProduct", "OriginalUnitPrice"
        ]);
    }

    [Fact]
    public async Task PayLater_ShouldHaveTheRealColumns_NotTheOnesTheMigratorAsksFor()
    {
        // Defect 2. The migrator SELECTs PayLaterId, UserId, CustomerName and PaymentAmount.
        // None of them exist: PayLater is a 1:1 extension of Payment, so its PK IS the payment's id.
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        var columns = await ColumnsAsync(db, "PayLater");

        columns.Should().BeEquivalentTo([
            "PaymentId", "Description", "InvoiceId", "IsCompleted",
            "DateCreated", "DateUpdated", "PayLaterAmount", "PaidAmount"
        ]);
        columns.Should().NotContain("PayLaterId");
        columns.Should().NotContain("UserId");
        columns.Should().NotContain("CustomerName");
        columns.Should().NotContain("PaymentAmount");
    }

    [Fact]
    public async Task PayLater_PrimaryKeyShouldBePaymentId_ProvingItExtendsPayment()
    {
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        var pk = (await db.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('PayLater') WHERE pk > 0")).ToList();

        pk.Should().BeEquivalentTo(["PaymentId"],
            "PayLater is table-per-subtype on Payment: Payment generates the id, PayLater receives it");
    }
}
```

- [ ] **Step 6: Delete the dead real-data tool**

```bash
git rm tests/IndyPOS.MigrationTool.Tests/TestData/CreateTestDatabase.cs
git rm tests/IndyPOS.MigrationTool.Tests/CreateTestDatabaseRunner.cs
git rm tests/IndyPOS.MigrationTool.Tests/create_test_db.ps1
```

It copied real rows into a gitignored `Store_test.db` that **no test reads**, and its `sqlite_master`
DDL dump is now done by `LegacySchemaExtractor` without the data.

- [ ] **Step 7: Run the tests**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "LegacySchemaArtefactTests"`
Expected: **5 passed**. If `InvoiceProduct` or `PayLater` fails, the artefact was mistyped in Step 2 — fix the artefact, never the assertion.

- [ ] **Step 8: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/Tools/LegacySchemaExtractor.cs \
        tests/IndyPOS.MigrationTool.Tests/LegacySchema \
        tests/IndyPOS.MigrationTool.Tests/LegacySchemaArtefactTests.cs \
        tests/IndyPOS.MigrationTool.Tests/IndyPOS.MigrationTool.Tests.csproj
git commit -m "test: extract legacy SQLite schema from real stores into committed artefacts

Two generated .sql artefacts replace hand-written DDL: GeneralHardware (13
tables, has PayLater) and MimyShop (10, none). Guarded by tests asserting
exact table and column sets -- InvoiceProduct's real 17 columns and
PayLater's real 8, including that PayLater's PK is PaymentId, which is the
structural proof it extends Payment rather than standing alone.

Deletes CreateTestDatabase and its runner: they copied real store rows into
a gitignored Store_test.db that no test reads. The sqlite_master DDL dump
they did usefully is now schema-only, which a public repo requires."
```

---

### Task 2: `LegacyStoreDatabase`

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/Fixtures/LegacyStoreDatabase.cs`
- Test: `tests/IndyPOS.MigrationTool.Tests/LegacySchemaArtefactTests.cs` (add to it)

**Interfaces:**
- Consumes: `LegacyStoreShape` and `LegacySchema/*.sql` from Task 1.
- Produces: `LegacyStoreDatabase` with `static Task<LegacyStoreDatabase> CreateAsync(LegacyStoreShape shape)`, properties `string Path` and `SQLiteConnection Connection`, and `ValueTask DisposeAsync()`.

- [ ] **Step 1: Write the failing test**

Append to `LegacySchemaArtefactTests.cs`:

```csharp
    [Fact]
    public async Task LegacyStoreDatabase_ShouldCreateARealFileWithTheRequestedShape()
    {
        await using var store = await Fixtures.LegacyStoreDatabase.CreateAsync(LegacyStoreShape.MimyShop);

        File.Exists(store.Path).Should().BeTrue();

        var tables = (await store.Connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")).ToList();

        tables.Should().HaveCount(10);
        tables.Should().NotContain("PayLater");
    }
```

- [ ] **Step 2: Run it to confirm it fails**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "LegacyStoreDatabase_ShouldCreateARealFileWithTheRequestedShape"`
Expected: FAIL — compile error, `LegacyStoreDatabase` does not exist.

- [ ] **Step 3: Implement it**

Create `tests/IndyPOS.MigrationTool.Tests/Fixtures/LegacyStoreDatabase.cs`:

```csharp
using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests.Fixtures;

/// <summary>
/// A throwaway SQLite database carrying a real store's legacy schema.
///
/// The schema comes from a generated artefact, never from hand-written DDL: a hand-written
/// fixture schema is what let defects 2, 3 and 6 pass unnoticed.
/// </summary>
public sealed class LegacyStoreDatabase : IAsyncDisposable
{
    private LegacyStoreDatabase(string path, SQLiteConnection connection)
    {
        Path = path;
        Connection = connection;
    }

    /// <summary>Absolute path to the temp file, for <c>MigrationOptions.SqlitePath</c>.</summary>
    public string Path { get; }

    /// <summary>An open connection, for seeding and for reading legacy rows back.</summary>
    public SQLiteConnection Connection { get; }

    public static async Task<LegacyStoreDatabase> CreateAsync(LegacyStoreShape shape)
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"legacy_{shape}_{Guid.NewGuid():N}.db");

        var connection = new SQLiteConnection($"Data Source={path};Version=3;");
        await connection.OpenAsync();

        var ddlPath = System.IO.Path.Combine(AppContext.BaseDirectory, "LegacySchema", $"{shape}.sql");
        if (!File.Exists(ddlPath))
        {
            throw new FileNotFoundException(
                $"Legacy schema artefact missing: {ddlPath}. Is LegacySchema\\*.sql copied to output?",
                ddlPath);
        }

        await connection.ExecuteAsync(await File.ReadAllTextAsync(ddlPath));

        return new LegacyStoreDatabase(path, connection);
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.CloseAsync();
        Connection.Dispose();

        // SQLite holds the file until pooled handles are released.
        SQLiteConnection.ClearAllPools();

        if (File.Exists(Path))
        {
            try { File.Delete(Path); }
            catch (IOException) { /* a leaked handle must not fail an otherwise-passing test */ }
        }
    }
}
```

- [ ] **Step 4: Run it to confirm it passes**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "LegacySchemaArtefactTests"`
Expected: **6 passed**.

- [ ] **Step 5: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/Fixtures/LegacyStoreDatabase.cs \
        tests/IndyPOS.MigrationTool.Tests/LegacySchemaArtefactTests.cs
git commit -m "test: add LegacyStoreDatabase applying a generated schema artefact"
```

---

### Task 3: `LegacyStoreDataBuilder` and the IsTrackable dead-data guard

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/Fixtures/LegacyStoreDataBuilder.cs`
- Create: `tests/IndyPOS.MigrationTool.Tests/LegacyStoreDataBuilderTests.cs`

**Interfaces:**
- Consumes: `LegacyStoreDatabase` from Task 2.
- Produces: `LegacyStoreDataBuilder`, constructed as `new LegacyStoreDataBuilder(store)`. Methods, all returning `Task` and all taking explicit values:
  - `AddUserAsync(int userId, string username, string firstName, string lastName, int roleId, string dateCreated)`
  - `AddProductAsync(int productId, string barcode, string description, decimal unitPrice, int quantityInStock, int? category, bool isTrackable, string dateCreated, decimal groupPrice = 0m, int? groupPriceQuantity = null)`
  - `AddInvoiceAsync(int invoiceId, int userId, decimal total, string dateCreated)`
  - `AddInvoiceLineAsync(int invoiceProductId, int invoiceId, int productId, string barcode, string description, int quantity, decimal unitPrice, decimal originalUnitPrice, decimal groupPrice = 0m, bool isGroupProduct = false, string? note = null, int? priority = null, int? category = null)`
  - `AddPaymentAsync(int paymentId, int invoiceId, int paymentTypeId, decimal amount, string dateCreated, string? note = null)`
  - `AddPayLaterAsync(int paymentId, int invoiceId, string description, decimal payLaterAmount, decimal paidAmount, bool isCompleted, string dateCreated, string? dateUpdated = null)`
  - `AddPaymentTypeLookupAsync()` — seeds the 8 real `PaymentType` rows.

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.MigrationTool.Tests/LegacyStoreDataBuilderTests.cs`:

```csharp
using Dapper;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Guards the builder itself. No PostgreSQL and no migrator involved -- these assert that the
/// fixture reproduces the legacy database's real quirks.
/// </summary>
public class LegacyStoreDataBuilderTests
{
    [Fact]
    public async Task AddInvoiceLine_ShouldLeaveIsTrackableAtItsSqliteDefault_Defect7Addendum()
    {
        // Defect 7 addendum. InvoiceProduct.IsTrackable is DEAD DATA in every real store:
        // 602,114 lines, every one 1, not a single 0, because the legacy INSERT omits the column
        // and SQLite applies DEFAULT 1. A migration that restores a per-line trackable flag by
        // reading this column marks every service line stock-tracked -- the exact bug defect 7
        // exists to prevent.
        //
        // This test fails if the builder ever starts setting the column, which would silently
        // restore the blindness the whole harness exists to remove.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);

        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 4242, barcode: "2002500000014", description: "Delivery service",
            unitPrice: 50m, quantityInStock: 0, category: 25, isTrackable: false,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 50m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 4242, barcode: "2002500000014",
            description: "Delivery service", quantity: 1, unitPrice: 50m, originalUnitPrice: 50m);

        var lineFlag = await store.Connection.ExecuteScalarAsync<long>(
            "SELECT IsTrackable FROM InvoiceProduct WHERE InvoiceProductId = 1");
        var productFlag = await store.Connection.ExecuteScalarAsync<long>(
            "SELECT IsTrackable FROM InventoryProduct WHERE InventoryProductId = 4242");

        lineFlag.Should().Be(1, "the legacy INSERT omits the column, so SQLite applies DEFAULT 1");
        productFlag.Should().Be(0, "the product is genuinely non-trackable, and that column IS maintained");
    }

    [Fact]
    public async Task AddPaymentTypeLookup_ShouldSeedTheEightRealRows()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);

        await builder.AddPaymentTypeLookupAsync();

        var labels = (await store.Connection.QueryAsync<(long Id, string Type)>(
            "SELECT Id, Type FROM PaymentType ORDER BY Id")).ToDictionary(r => (int)r.Id, r => r.Type);

        labels.Should().HaveCount(8);
        labels[1].Should().Be("เงินสด");
        labels[2].Should().Be("ลงบัญชี");
        labels[3].Should().Be("บัตรสวัสดิการแห่งรัฐ");
        labels[5].Should().Be("โอนเข้าบัญชี");
        labels[7].Should().Be("คนละครึ่ง");
        labels[8].Should().Be("เราชนะ");
    }
}
```

- [ ] **Step 2: Run it to confirm it fails**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "LegacyStoreDataBuilderTests"`
Expected: FAIL — compile error, `LegacyStoreDataBuilder` does not exist.

- [ ] **Step 3: Implement the builder**

Create `tests/IndyPOS.MigrationTool.Tests/Fixtures/LegacyStoreDataBuilder.cs`:

```csharp
using Dapper;

namespace IndyPOS.MigrationTool.Tests.Fixtures;

/// <summary>
/// Inserts deterministic legacy rows with explicit ids and values, so assertions can state an
/// expected value rather than a count.
///
/// The old Bogus-based seeder could only be asserted with counts and BeGreaterThan, which is why
/// defects 4, 5, 7 and 8 went unnoticed for so long.
/// </summary>
public sealed class LegacyStoreDataBuilder
{
    private readonly LegacyStoreDatabase _store;

    public LegacyStoreDataBuilder(LegacyStoreDatabase store) => _store = store;

    /// <summary>The 8 rows every real store's PaymentType table holds, verbatim.</summary>
    public async Task AddPaymentTypeLookupAsync()
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO PaymentType (Id, Type) VALUES
                (1, 'เงินสด'),
                (2, 'ลงบัญชี'),
                (3, 'บัตรสวัสดิการแห่งรัฐ'),
                (4, 'ม.33'),
                (5, 'โอนเข้าบัญชี'),
                (6, 'ผ่อนชำระ'),
                (7, 'คนละครึ่ง'),
                (8, 'เราชนะ');
            """);
    }

    public async Task AddUserAsync(
        int userId, string username, string firstName, string lastName, int roleId, string dateCreated)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO User (UserId, FirstName, LastName, RoleId, DateCreated)
            VALUES (@userId, @firstName, @lastName, @roleId, @dateCreated);
            """, new { userId, firstName, lastName, roleId, dateCreated });

        await _store.Connection.ExecuteAsync("""
            INSERT INTO UserCredential (UserId, Username, Password, DateCreated)
            VALUES (@userId, @username, @password, @dateCreated);
            """, new { userId, username, password = $"legacy-3des-hash-{username}", dateCreated });
    }

    public async Task AddProductAsync(
        int productId, string barcode, string description, decimal unitPrice, int quantityInStock,
        int? category, bool isTrackable, string dateCreated,
        decimal groupPrice = 0m, int? groupPriceQuantity = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO InventoryProduct
                (InventoryProductId, Barcode, Description, Category, QuantityInStock,
                 GroupPriceQuantity, IsTrackable, DateCreated, UnitPrice, GroupPrice)
            VALUES
                (@productId, @barcode, @description, @category, @quantityInStock,
                 @groupPriceQuantity, @isTrackableFlag, @dateCreated, @unitPrice, @groupPrice);
            """, new
        {
            productId, barcode, description, category, quantityInStock, groupPriceQuantity,
            isTrackableFlag = isTrackable ? 1 : 0, dateCreated, unitPrice, groupPrice
        });
    }

    public async Task AddInvoiceAsync(int invoiceId, int userId, decimal total, string dateCreated)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO Invoice (InvoiceId, UserId, Total, DateCreated)
            VALUES (@invoiceId, @userId, @total, @dateCreated);
            """, new { invoiceId, userId, total, dateCreated });
    }

    /// <remarks>
    /// Deliberately does NOT insert <c>IsTrackable</c>. The legacy write path omits it, so SQLite
    /// applies DEFAULT 1 and the column is dead data in every real store. Reproducing that is the
    /// point -- see LegacyStoreDataBuilderTests.AddInvoiceLine_ShouldLeaveIsTrackableAtItsSqliteDefault_Defect7Addendum.
    /// </remarks>
    public async Task AddInvoiceLineAsync(
        int invoiceProductId, int invoiceId, int productId, string barcode, string description,
        int quantity, decimal unitPrice, decimal originalUnitPrice,
        decimal groupPrice = 0m, bool isGroupProduct = false, string? note = null,
        int? priority = null, int? category = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO InvoiceProduct
                (InvoiceProductId, Priority, InvoiceId, InventoryProductId, Barcode, Description,
                 Category, Quantity, Note, UnitPrice, GroupPrice, IsGroupProduct, OriginalUnitPrice)
            VALUES
                (@invoiceProductId, @priority, @invoiceId, @productId, @barcode, @description,
                 @category, @quantity, @note, @unitPrice, @groupPrice, @isGroupProductFlag,
                 @originalUnitPrice);
            """, new
        {
            invoiceProductId, priority, invoiceId, productId, barcode, description, category,
            quantity, note, unitPrice, groupPrice,
            isGroupProductFlag = isGroupProduct ? 1 : 0, originalUnitPrice
        });
    }

    public async Task AddPaymentAsync(
        int paymentId, int invoiceId, int paymentTypeId, decimal amount, string dateCreated,
        string? note = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO Payment (PaymentId, InvoiceId, PaymentTypeId, DateCreated, Note, Amount)
            VALUES (@paymentId, @invoiceId, @paymentTypeId, @dateCreated, @note, @amount);
            """, new { paymentId, invoiceId, paymentTypeId, dateCreated, note, amount });
    }

    /// <param name="paymentId">
    /// The id of an existing <c>Payment</c> row. PayLater is a 1:1 extension of Payment, so this
    /// is both its primary key and its foreign key -- it must reference a payment that exists.
    /// </param>
    public async Task AddPayLaterAsync(
        int paymentId, int invoiceId, string description, decimal payLaterAmount, decimal paidAmount,
        bool isCompleted, string dateCreated, string? dateUpdated = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO PayLater
                (PaymentId, Description, InvoiceId, IsCompleted, DateCreated, DateUpdated,
                 PayLaterAmount, PaidAmount)
            VALUES
                (@paymentId, @description, @invoiceId, @isCompletedFlag, @dateCreated, @dateUpdated,
                 @payLaterAmount, @paidAmount);
            """, new
        {
            paymentId, description, invoiceId, isCompletedFlag = isCompleted ? 1 : 0,
            dateCreated, dateUpdated, payLaterAmount, paidAmount
        });
    }
}
```

- [ ] **Step 4: Run it to confirm it passes**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "LegacyStoreDataBuilderTests"`
Expected: **2 passed**.

- [ ] **Step 5: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/Fixtures/LegacyStoreDataBuilder.cs \
        tests/IndyPOS.MigrationTool.Tests/LegacyStoreDataBuilderTests.cs
git commit -m "test: add deterministic legacy row builder

Explicit ids and values so assertions can state an expected value instead
of a count -- the Bogus seeder's randomness is why every existing
assertion is a count or BeGreaterThan.

AddInvoiceLineAsync deliberately omits IsTrackable so SQLite applies
DEFAULT 1, reproducing the dead-data condition measured across all three
real stores (602,114 lines, not a single 0). A guard test pins it, so the
column cannot quietly start being set."
```

---

### Task 4: Whole-run coverage on the honest schema, and fix defects 2 and 3

The suite goes red here for the right reason, then green. **This is the task that makes every later one possible:** until the PayLater query stops throwing, no run reaches `SaveChangesAsync`, so no migrated row can be inspected.

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/PayLaterMigrationTests.cs`
- Rewrite: `tests/IndyPOS.MigrationTool.Tests/SqliteMigrationServiceTests.cs`
- Modify: `tests/IndyPOS.MigrationTool.Tests/MigrationVerifierTests.cs`
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:334-341`
- Delete: `tests/IndyPOS.MigrationTool.Tests/TestData/SqliteTestDataSeeder.cs`

**Interfaces:**
- Consumes: `LegacyStoreDatabase`, `LegacyStoreDataBuilder`, `PostgresFixture`.
- Produces: a `MigrationScenario` helper — `static Task<MigrationResult> RunAsync(LegacyStoreDatabase store, PostgresFixture postgres, bool dryRun = false)` — used by every later task.

- [ ] **Step 1: Write the failing whole-run test**

Create `tests/IndyPOS.MigrationTool.Tests/PayLaterMigrationTests.cs`:

```csharp
using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Runs the shipped migrator against a legacy schema taken from a real store.
/// </summary>
public static class MigrationScenario
{
    public const string StoreId = "TEST-STORE";

    public static async Task<MigrationResult> RunAsync(
        LegacyStoreDatabase store, PostgresFixture postgres, bool dryRun = false)
    {
        var options = new MigrationOptions
        {
            SqlitePath = store.Path,
            PostgresConnectionString = postgres.ConnectionString,
            StoreId = StoreId,
            DryRun = dryRun
        };

        return await new SqliteMigrationService(
            options, NullLogger<SqliteMigrationService>.Instance).MigrateAllAsync();
    }
}

[Collection("Postgres")]
public class PayLaterMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public PayLaterMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Seeds one credit sale: an invoice, a line, a PaymentTypeId=2 payment, and the PayLater
    /// extension row keyed by that payment's id -- the shape a real till writes.
    /// </summary>
    private static async Task SeedCreditSaleAsync(
        LegacyStoreDatabase store,
        decimal payLaterAmount = 700m,
        decimal paidAmount = 299m,
        bool isCompleted = false,
        string customer = "Somchai (shop next door)")
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 700m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: payLaterAmount, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 1, unitPrice: payLaterAmount,
            originalUnitPrice: payLaterAmount);

        // The till writes the Payment FIRST, then the PayLater that tracks the debt.
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 2, amount: payLaterAmount,
            dateCreated: "2024-03-15 14:30:00", note: customer);
        await builder.AddPayLaterAsync(
            paymentId: 500, invoiceId: 1, description: customer, payLaterAmount: payLaterAmount,
            paidAmount: paidAmount, isCompleted: isCompleted, dateCreated: "2024-03-15 14:30:00",
            dateUpdated: "2024-04-02 11:05:00");
    }

    [Fact]
    public async Task MigrateAllAsync_AgainstGeneralHardwareShape_CompletesAndPersists()
    {
        // Defect 2. Before the fix this THROWS "no such column: PayLaterId", and because that
        // query sits outside the per-row try and SaveChangesAsync runs after the PayLater phase,
        // the entire migration is discarded -- nothing is persisted, for any real store.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(1, "the run must reach SaveChangesAsync");
        (await db.Products.CountAsync()).Should().Be(1);
        (await db.StoreUsers.CountAsync()).Should().Be(1);
        (await db.PayLaters.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MigrateAllAsync_AgainstAStoreWithNoPayLaterTable_Completes()
    {
        // Defect 3. The PayLater query runs unconditionally, so on MimyShop and MimyMart -- which
        // have no such table -- it throws "no such table: PayLater". PayLater is a
        // GeneralHardware-only feature, so its absence is normal, not a failure.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.MimyShop);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Malee", "Sooksan", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850002000020", description: "Instant noodles",
            unitPrice: 6m, quantityInStock: 100, category: 20, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 6m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850002000020",
            description: "Instant noodles", quantity: 1, unitPrice: 6m, originalUnitPrice: 6m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 6m,
            dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.PayLater.Migrated.Should().Be(0);
        result.PayLater.Failed.Should().Be(0, "a store with no PayLater feature is not a failure");
        result.Errors.Should().BeEmpty();

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MigratePayLater_ReadsRealColumns_MapsDescriptionAndAmounts()
    {
        // Defect 2. PaidAmount is READ, never derived from IsCompleted: the customer returns and
        // pays in instalments, so PaidAmount is the only surviving record of that progress.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 700m, paidAmount: 299m, isCompleted: false);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.Description.Should().Be("Somchai (shop next door)");
        payLater.PayLaterAmount.Should().Be(700m);
        payLater.PaidAmount.Should().Be(299m, "read from the column, not derived from IsCompleted");
        payLater.IsCompleted.Should().BeFalse();
        payLater.RemainingAmount.Should().Be(401m);
    }
}
```

- [ ] **Step 2: Run it to confirm all three fail**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "PayLaterMigrationTests"`
Expected: **3 failed.** Two with `SQLiteException: no such column: PayLaterId`, one with `no such table: PayLater`.

- [ ] **Step 3: Fix defects 2 and 3**

In `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs`, replace the body of
`MigratePayLaterAsync` from its opening log line through the `.ToList();` (lines 336–341) with:

```csharp
        // Defect 3: PayLater is a GeneralHardware-only feature. Minimart and MimyShop have no such
        // table, so querying it unconditionally threw "no such table: PayLater" and -- because that
        // throw escaped before SaveChangesAsync -- discarded the entire migration.
        var hasPayLaterTable = await sqlite.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'PayLater'") > 0;

        if (!hasPayLaterTable)
        {
            _logger.LogInformation(
                "No PayLater table in this store; skipping. PayLater is a GeneralHardware-only feature.");
            return;
        }

        _logger.LogInformation("Migrating PayLater records...");

        // Defect 2: the previous SELECT named PayLaterId, UserId, CustomerName and PaymentAmount.
        // None exist. PayLater is a 1:1 extension of Payment (its PK IS the payment's id), so it
        // needs no id of its own, no user (the payment's invoice has one) and no amount column
        // beyond the debt it is tracking.
        var payLaters = (await sqlite.QueryAsync<LegacyPayLater>("""
            SELECT PaymentId, Description, InvoiceId, IsCompleted, DateCreated, DateUpdated,
                   PayLaterAmount, PaidAmount
            FROM PayLater
            """)).ToList();
```

Replace the `LegacyPayLater` class (lines 580–590) with:

```csharp
    private class LegacyPayLater
    {
        /// <summary>Both the primary key and the FK to <c>Payment.PaymentId</c>.</summary>
        public long PaymentId { get; set; }
        public long InvoiceId { get; set; }

        /// <summary>The customer who owes the debt. Mirrored in <c>Payment.Note</c>.</summary>
        public string? Description { get; set; }
        public double PayLaterAmount { get; set; }
        public double PaidAmount { get; set; }
        public long IsCompleted { get; set; }
        public string? DateCreated { get; set; }
        public string? DateUpdated { get; set; }
    }
```

In the `foreach` body, delete the user-id mapping (lines 356–360) — real `PayLater` has no `UserId`
column — and replace the `newPayLater` initialiser's amount lines so `PaidAmount` is read:

```csharp
                var newPayLater = new PayLater
                {
                    Id = Guid.NewGuid(),
                    PaymentId = paymentId,
                    InvoiceId = invoiceId,
                    Description = Truncate(payLater.Description ?? string.Empty, 500),
                    PayLaterAmount = (decimal)payLater.PayLaterAmount,
                    // Read, never derived. The customer repays in instalments, so this column is
                    // the only record of that progress -- Installments is empty in every store.
                    PaidAmount = (decimal)payLater.PaidAmount,
                    IsCompleted = payLater.IsCompleted == 1,
                    CreatedUtc = createdUtc,
                    LastModifiedUtc = ParseDate(payLater.DateUpdated) ?? createdUtc
                };
```

Also update the two log/error messages in the method that interpolate `payLater.PayLaterId` to use
`payLater.PaymentId`.

- [ ] **Step 4: Run the new tests to confirm they pass**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "PayLaterMigrationTests"`
Expected: **3 passed.**

- [ ] **Step 5: Rewrite `SqliteMigrationServiceTests` onto the honest fixture**

The old file's 11 tests are built on `SqliteTestDataSeeder`, whose `PayLater` matches the migrator's
old SELECT and whose payment ids use defect 1's scrambled map (`4` treated as PayLater; really ม.33).
Replace the whole file with the three tests that assert real behaviour:

```csharp
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class SqliteMigrationServiceTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public SqliteMigrationServiceTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task SeedOneSaleAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 120m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 1, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 120m,
            dateCreated: "2024-03-15 14:30:00");
    }

    [Fact]
    public async Task MigrateAllAsync_WithAnEmptyStore_ShouldSucceedAndMigrateNothing()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.TotalMigrated.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateAllAsync_CalledTwice_ShouldSkipUsersAndProductsWithoutDuplicating()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(store);

        var first = await MigrationScenario.RunAsync(store, _postgres);
        var second = await MigrationScenario.RunAsync(store, _postgres);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Users.Skipped.Should().Be(first.Users.Migrated);
        second.Products.Skipped.Should().Be(first.Products.Migrated);

        await using var db = _postgres.CreateDbContext();
        (await db.StoreUsers.CountAsync()).Should().Be(1, "users must not be duplicated");
        (await db.Products.CountAsync()).Should().Be(1, "products must not be duplicated");
    }

    [Fact]
    public async Task MigrateAllAsync_WithAUserThatHasNoCredentials_ShouldSkipThatUser()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await store.Connection.ExecuteAsync("""
            INSERT INTO User (UserId, FirstName, LastName, RoleId, DateCreated)
            VALUES (9, 'No', 'Credentials', 1, '2024-03-15 09:00:00');
            """);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Users.Skipped.Should().Be(1);
        result.Users.Migrated.Should().Be(0);
    }

    [Fact]
    public async Task MigrateAllAsync_ShouldIgnoreTheObsoleteCustomersAndInstallmentsTables()
    {
        // Customers and Installments are obsolete and never used in any store (0 rows measured;
        // confirmed by Pond 2026-08-03). Legacy payment type 6 (ผ่อนชำระ) is dead with them.
        //
        // They exist in the GeneralHardware schema, so this pins that the migration ignores them --
        // otherwise someone later "completes" the migration by adding them, importing a feature no
        // store uses and giving legacy type 6 a home it should not have.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(store);

        await store.Connection.ExecuteAsync("""
            INSERT INTO Customers (CustomerId, FirstName, LastName, DateCreated)
            VALUES (1, 'Obsolete', 'Feature', '2024-03-15 09:00:00');
            INSERT INTO Installments (CustomerId, Installment, NumberOfInstallments, Total, DateCreated, DueDate)
            VALUES (1, 'never used', 3, '900', '2024-03-15 09:00:00', '2024-06-15');
            """);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty("obsolete tables must be ignored, not reported as a problem");

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(1);
        (await db.PayLaters.CountAsync()).Should().Be(0,
            "an Installments row must never become a PayLater");
    }

    [Fact]
    public async Task MigrateAllAsync_InDryRun_ShouldWriteNothing()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres, dryRun: true);

        result.Users.Migrated.Should().Be(1);
        result.Products.Migrated.Should().Be(1);

        await using var db = _postgres.CreateDbContext();
        (await db.StoreUsers.CountAsync()).Should().Be(0);
        (await db.Products.CountAsync()).Should().Be(0);
    }
}
```

Add `using Dapper;` at the top for the raw `ExecuteAsync`.

- [ ] **Step 6: Point `MigrationVerifierTests` at the honest fixture**

That file's 5 tests call `seeder.SeedCompleteDataSetAsync(...)`. Its assertions are about the
verifier, not the schema, so change only the arrangement. In each of the 5 tests, replace the
seeder lines with a `LegacyStoreDatabase` + `LegacyStoreDataBuilder` arrangement, and replace the
class's `_sqliteConnection`/`_sqliteDbPath` fields with a `LegacyStoreDatabase` created in
`InitializeAsync` and disposed in `DisposeAsync`:

```csharp
    private LegacyStoreDatabase _store = null!;

    public async Task InitializeAsync()
    {
        _store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await _postgres.ResetDatabaseAsync();
    }

    public async Task DisposeAsync() => await _store.DisposeAsync();
```

Then in each test, seed with the builder rather than the deleted seeder. Where a test needed volume,
loop the builder — for example:

```csharp
        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");

        for (var i = 1; i <= 15; i++)
        {
            await builder.AddProductAsync(
                productId: i, barcode: $"885000100{i:D4}", description: $"Product {i}",
                unitPrice: 10m * i, quantityInStock: 50, category: 50, isTrackable: true,
                dateCreated: "2024-03-15 09:00:00");
        }

        for (var i = 1; i <= 30; i++)
        {
            await builder.AddInvoiceAsync(i, userId: 1, total: 100m, dateCreated: "2024-03-15 14:30:00");
            await builder.AddInvoiceLineAsync(
                invoiceProductId: i, invoiceId: i, productId: ((i - 1) % 15) + 1,
                barcode: $"885000100{((i - 1) % 15) + 1:D4}", description: "Product",
                quantity: 1, unitPrice: 100m, originalUnitPrice: 100m);
            await builder.AddPaymentAsync(
                paymentId: i, invoiceId: i, paymentTypeId: 1, amount: 100m,
                dateCreated: "2024-03-15 14:30:00");
        }
```

Adjust each test's existing expected counts to the numbers seeded above. Do **not** weaken any
verifier assertion to make it pass — if one fails, that is a finding to report, not to paper over.

- [ ] **Step 7: Delete the fabricated seeder**

```bash
git rm tests/IndyPOS.MigrationTool.Tests/TestData/SqliteTestDataSeeder.cs
```

- [ ] **Step 8: Run the whole suite**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests`
Expected: all green. Record the count.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "fix(migration): read PayLater's real columns and skip stores without it

Defects 2 and 3. The PayLater SELECT named PayLaterId, UserId,
CustomerName and PaymentAmount -- none of which exist. Measured against
the real stores: GeneralHardware gives 'no such column: PayLaterId',
MimyMart and MimyShop 'no such table: PayLater'.

Because that query sat outside the per-row try and SaveChangesAsync runs
after the PayLater phase, the throw escaped before any save: the tool did
the entire migration in memory and discarded it. It had never persisted
anything from a real store.

PayLater is a 1:1 extension of Payment -- its PK IS the payment's id --
so it needs no id of its own and no user. PaidAmount is now READ rather
than derived from IsCompleted, because the customer repays in instalments
and that column is the only surviving record of progress.

Also replaces SqliteTestDataSeeder, whose schema matched the migrator's
old SELECT and whose payment ids used defect 1's scrambled map (4 treated
as PayLater; really the M33 campaign)."
```

---

### Task 5: Fix defect 10 — the double-counted PayLater payment

**Files:**
- Modify: `src/IndyPOS.MigrationTool/MigrationResult.cs:26`
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:292-318, 362-378`
- Modify: `tests/IndyPOS.MigrationTool.Tests/PayLaterMigrationTests.cs`

**Interfaces:**
- Consumes: `MigrationScenario.RunAsync` from Task 4.
- Produces: `MigrationResult.PaymentIdMap` (`Dictionary<int, Guid>`, legacy `Payment.PaymentId` → new `Guid`).

- [ ] **Step 1: Write the failing tests**

Append to `PayLaterMigrationTests.cs`:

```csharp
    [Fact]
    public async Task MigratePayLater_LinksTheExistingPayment_WithoutCreatingASecond()
    {
        // Defect 10. At the till a credit sale writes the Payment first, then the PayLater that
        // tracks the debt. MigratePayLaterAsync re-enacted that sequence -- correct for a NEW sale,
        // wrong for a migration, because both rows already exist in SQLite. On real GeneralHardware
        // data that double-counts THB 836,013 across 5,181 rows.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 700m, paidAmount: 299m);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLaterPayments = await db.Payments
            .Where(p => p.Method == PaymentMethodCodes.PayLater)
            .ToListAsync();

        payLaterPayments.Should().HaveCount(1, "the legacy Payment row is the money; PayLater only annotates it");
        payLaterPayments.Single().Amount.Should().Be(700m);
        (await db.Payments.SumAsync(p => p.Amount)).Should().Be(700m, "no money may be invented");

        var payLater = await db.PayLaters.SingleAsync();
        payLater.PaymentId.Should().Be(payLaterPayments.Single().Id,
            "the extension row must point at the migrated payment");
    }

    [Fact]
    public async Task MigratePayLater_WhenThePaymentIsMissing_RecordsAnErrorAndSkips()
    {
        // The real ฿70 orphan is the reverse case: PaymentId 90 is a type-2 Payment with an empty
        // Note and no PayLater row. Here we test the other direction -- a PayLater whose payment
        // was never migrated must be refused, never furnished with an invented payment.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 700m, dateCreated: "2024-03-15 14:30:00");
        // No Payment row at all, so PaymentId 999 cannot resolve.
        await builder.AddPayLaterAsync(
            paymentId: 999, invoiceId: 1, description: "Somchai", payLaterAmount: 700m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PayLater.Failed.Should().Be(1);
        result.Errors.Should().ContainSingle().Which.Should().Contain("999");

        await using var db = _postgres.CreateDbContext();
        (await db.PayLaters.CountAsync()).Should().Be(0);
        (await db.Payments.CountAsync()).Should().Be(0, "a missing payment must never be invented");
    }
```

Add `using IndyPOS.Application.Common.Constants;` if not already present.

- [ ] **Step 2: Run to confirm they fail**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "MigratePayLater_LinksTheExistingPayment_WithoutCreatingASecond|MigratePayLater_WhenThePaymentIsMissing_RecordsAnErrorAndSkips"`
Expected: **2 failed.** The first finds 2 payments totalling ฿1,400; the second finds 1 invented payment and 1 migrated PayLater.

- [ ] **Step 3: Add the payment id map**

In `src/IndyPOS.MigrationTool/MigrationResult.cs`, after `InvoiceIdMap` (line 26):

```csharp
    /// <summary>
    /// Legacy <c>Payment.PaymentId</c> to the migrated payment's Guid. PayLater is a 1:1 extension
    /// of Payment, so its rows attach to an already-migrated payment through this map rather than
    /// creating one -- see defect 10.
    /// </summary>
    public Dictionary<int, Guid> PaymentIdMap { get; } = [];
```

- [ ] **Step 4: Populate the map, outside the DryRun guard**

In `MigrateInvoicesAsync`, the payments loop currently sits inside `if (!_options.DryRun)`. Move the
loop out so the map is always built, guarding only the `context.Payments.Add`. Replace the
`foreach (var payment in payments)` block (lines 292–318) with:

```csharp
                foreach (var payment in payments)
                {
                    var method = LegacyPaymentTypeMap.ToCode((int)payment.PaymentTypeId);
                    if (method is null)
                    {
                        // Refused, never guessed. The previous fallback wrote "Other", which
                        // is not a catalogue code, so the amount became unresolvable while
                        // the row counts still reconciled.
                        _result.Errors.Add(
                            $"Invoice {invoice.InvoiceId} payment {payment.PaymentId}: legacy " +
                            $"PaymentTypeId {payment.PaymentTypeId} has no payment-method code. " +
                            $"Migrating it would misattribute {payment.Amount:N2}.");
                        _result.Payments.Failed++;
                        continue;
                    }

                    var newPayment = new Payment
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = newInvoice.Id,
                        Method = method,
                        Amount = (decimal)payment.Amount,
                        Note = payment.Note,
                        CreatedUtc = createdUtc
                    };

                    if (!_options.DryRun)
                    {
                        context.Payments.Add(newPayment);
                    }

                    // Built in dry-run too: MigratePayLaterAsync resolves against this map, and an
                    // empty map would make every PayLater row fail its lookup.
                    _result.PaymentIdMap[(int)payment.PaymentId] = newPayment.Id;
                    _result.Payments.Migrated++;
                }
```

Note the enclosing `if (!_options.DryRun)` block (line 256) must now close **before** this loop, so
that the lines and inventory-movement loops stay inside it and the payments loop sits outside.

- [ ] **Step 5: Link instead of creating**

In `MigratePayLaterAsync`, replace the payment-creation block (lines 362–378) with:

```csharp
                var createdUtc = ParseDate(payLater.DateCreated) ?? DateTime.UtcNow;

                // Defect 10: the legacy Payment row IS this money. PayLater is its 1:1 extension,
                // keyed by the same id. Creating a payment here double-counted every credit sale
                // -- THB 836,013 across the 5,181 real rows.
                if (!_result.PaymentIdMap.TryGetValue((int)payLater.PaymentId, out var paymentId))
                {
                    _result.Errors.Add(
                        $"PayLater {payLater.PaymentId}: no migrated payment for legacy PaymentId " +
                        $"{payLater.PaymentId}, so the debt of {payLater.PayLaterAmount:N2} cannot be " +
                        $"attached. Refusing rather than inventing a payment.");
                    _result.PayLater.Failed++;
                    continue;
                }
```

- [ ] **Step 6: Run to confirm they pass**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests`
Expected: all green.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.MigrationTool tests/IndyPOS.MigrationTool.Tests
git commit -m "fix(migration): attach PayLater to its existing payment, not a new one

Defect 10. PayLater.PaymentId is an FK to Payment.PaymentId -- 5,181 of
5,181 real rows match, zero orphans, every linked payment PaymentTypeId=2,
with InvoiceId and Amount agreeing 100%. MigrateInvoicesAsync already
migrates that money as Method=PayLater, then MigratePayLaterAsync created a
SECOND payment for it: THB 836,013 counted twice.

Root cause was a write model where a copy model was needed. At the till a
credit sale writes the Payment then the PayLater that tracks the debt, and
the method faithfully re-enacted that -- correct for a new sale, wrong for
a migration, because both rows already exist in SQLite.

Adds MigrationResult.PaymentIdMap and resolves against it. A miss is an
error and a skip, never an invented payment: real data has one such case
(PaymentId 90, THB 70, empty Note, no PayLater row).

The map is built outside the DryRun guard, or dry-run would leave it empty
and fail every PayLater lookup."
```

---

### Task 6: PayLater value coverage

**Files:**
- Modify: `tests/IndyPOS.MigrationTool.Tests/PayLaterMigrationTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 4 and 5. Produces nothing new.

- [ ] **Step 1: Add the tests**

Append to `PayLaterMigrationTests.cs`:

```csharp
    [Fact]
    public async Task MigratePayLater_WithPaidAmountExceedingTheDebt_CarriesItUnchanged()
    {
        // Real data holds PaymentId 139978: THB 82 owed against THB 8,200 recorded paid -- exactly
        // 100x, a dropped decimal at the till, with IsCompleted still 0 because the app's
        // completion check compares for equality. The migration must NOT normalise or clamp this.
        // Inventing a correction is worse than carrying a visible error: a store can only fix what
        // it can see.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 82m, paidAmount: 8200m, isCompleted: false);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.PayLaterAmount.Should().Be(82m);
        payLater.PaidAmount.Should().Be(8200m, "carried verbatim; the migration does not invent corrections");
        payLater.IsCompleted.Should().BeFalse();
        payLater.RemainingAmount.Should().Be(-8118m, "a negative remainder is the visible symptom");
    }

    [Fact]
    public async Task MigratePayLater_WithNeverRepaidRow_PreservesZeroPaidAmount()
    {
        // The 142-row case: nothing repaid yet, DateUpdated null.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 500m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 2, amount: 500m,
            dateCreated: "2024-03-15 14:30:00", note: "Malee");
        await builder.AddPayLaterAsync(
            paymentId: 500, invoiceId: 1, description: "Malee", payLaterAmount: 500m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00",
            dateUpdated: null);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.PaidAmount.Should().Be(0m);
        payLater.IsCompleted.Should().BeFalse();
        payLater.RemainingAmount.Should().Be(500m);
        payLater.LastModifiedUtc.Should().Be(payLater.CreatedUtc,
            "a null DateUpdated falls back to the created timestamp");
    }

    [Fact]
    public async Task MigratePayLater_CustomerName_SurvivesInBothDescriptionAndNote()
    {
        // Measured: PayLater.Description and the linked Payment.Note hold the same customer name on
        // all 5,181 real rows -- zero disagreements, zero rows where only one is set. Description is
        // the intended home; Note mirrors it. Both migrate through their own columns.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, customer: "คุณสมชาย ใจดี");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.PayLaters.SingleAsync()).Description.Should().Be("คุณสมชาย ใจดี");
        (await db.Payments.SingleAsync()).Note.Should().Be("คุณสมชาย ใจดี");
    }

    [Fact]
    public async Task MigratePayLater_WhenCompleted_PreservesFullPayment()
    {
        // The 5,037-row case: settled in full, IsCompleted 1.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 195m, paidAmount: 195m, isCompleted: true);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.IsCompleted.Should().BeTrue();
        payLater.PaidAmount.Should().Be(195m);
        payLater.RemainingAmount.Should().Be(0m);
    }
```

- [ ] **Step 2: Run them**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "PayLaterMigrationTests"`
Expected: **9 passed.**

- [ ] **Step 3: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/PayLaterMigrationTests.cs
git commit -m "test: cover every real PayLater repayment state

Four states measured in the real store: never repaid (142 rows), settled
(5,037), mid-repayment, and PaidAmount above the debt.

The last is a live data-entry error -- PaymentId 139978, THB 82 owed
against THB 8,200 paid, exactly 100x. The test asserts it is carried
VERBATIM, so nobody later 'fixes' it by clamping: inventing a correction
is worse than carrying a visible error.

Also pins that the customer name survives in both PayLater.Description and
Payment.Note, which agree on all 5,181 real rows."
```

---

### Task 7: Pinning tests for the date defects (4 and 11)

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/MigrationDateTests.cs`

**Interfaces:**
- Consumes: `MigrationScenario`, `LegacyStoreDatabase`, `LegacyStoreDataBuilder`. Produces nothing new.

- [ ] **Step 1: Write the pinning tests**

Create `tests/IndyPOS.MigrationTool.Tests/MigrationDateTests.cs`:

```csharp
using System.Globalization;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// PINNING TESTS. These assert what the migrator does TODAY, including its defects, so the suite
/// stays green and meaningful while the fixes land in their own specs. Each names its defect and
/// records the correct answer. When a defect is fixed, invert exactly one test here.
/// </summary>
[Collection("Postgres")]
public class MigrationDateTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public MigrationDateTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Thai local time, the only kind a legacy store records.</summary>
    private const string ThaiLocalTimestamp = "2024-03-15 14:30:00";

    private static async Task SeedOneInvoiceAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, ThaiLocalTimestamp);
        await builder.AddInvoiceAsync(1, userId: 1, total: 120m, dateCreated: ThaiLocalTimestamp);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 120m,
            dateCreated: ThaiLocalTimestamp);
    }

    [Fact]
    public async Task MigrateInvoices_DateCreated_CurrentlyRelabelsThaiLocalAsUtc_Defect4()
    {
        // Defect 4: ParseDate does SpecifyKind(..., Utc) on a value written by
        // datetime('now','localtime'), so a 14:30 Bangkok sale becomes 14:30 UTC.
        // CORRECT: 2024-03-15T07:30:00Z (Thailand is UTC+7).
        // Across 139,680 real invoices every timestamp is 7 hours out, which silently corrupts
        // every daily and monthly total while reconciling perfectly under any count-based check.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
            await SeedOneInvoiceAsync(store);

            await MigrationScenario.RunAsync(store, _postgres);

            await using var db = _postgres.CreateDbContext();
            var invoice = await db.Invoices.SingleAsync();

            invoice.CreatedUtc.Should().Be(new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc));
            invoice.CreatedUtc.Should().NotBe(new DateTime(2024, 3, 15, 7, 30, 0, DateTimeKind.Utc),
                "this is the correct answer and defect 4 does not yet produce it");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task MigrateInvoices_UnderThaiCulture_CurrentlyLandsIn1481_Defect11()
    {
        // Defect 11: ParseDate calls bare DateTime.TryParse with NO CultureInfo, and the migration
        // tool runs on the store's own Thai-locale till. Under th-TH the Buddhist calendar reads
        // 2024 as a Buddhist-era year, giving 1481 AD -- 543 years off.
        // CORRECT: year 2024, by parsing with CultureInfo.InvariantCulture.
        // Every invoice then falls outside every date-range report, so a migrated store shows ZERO
        // sales history. Same six-line method as defect 4.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
        try
        {
            await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
            await SeedOneInvoiceAsync(store);

            await MigrationScenario.RunAsync(store, _postgres);

            await using var db = _postgres.CreateDbContext();
            var invoice = await db.Invoices.SingleAsync();

            invoice.CreatedUtc.Year.Should().Be(1481,
                "the Thai Buddhist calendar reads 2024 as a BE year; 2024 - 543 = 1481");
            invoice.CreatedUtc.Year.Should().NotBe(2024,
                "this is the correct answer and defect 11 does not yet produce it");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
```

- [ ] **Step 2: Run them**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "MigrationDateTests"`
Expected: **2 passed** — they pin current behaviour.

- [ ] **Step 3: Prove they are not vacuous**

Temporarily change `ParseDate` in `SqliteMigrationService.cs` to
`DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)`.
Run the two tests again: **both must fail.** Then revert the change. A pinning test that cannot fail
documents nothing.

- [ ] **Step 4: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/MigrationDateTests.cs
git commit -m "test: pin the two date defects (4 and 11)

Defect 4 -- SpecifyKind(Utc) on a localtime value, so a 14:30 Bangkok sale
becomes 14:30 UTC. 7 hours out across 139,680 invoices, and it reconciles
perfectly under any count-based check.

Defect 11 -- bare DateTime.TryParse with no CultureInfo, and the tool runs
on the store's Thai till. Under th-TH the Buddhist calendar reads 2024 as a
BE year, giving 1481 AD: 543 years off, so a migrated store shows zero
sales in any date-range report.

Both pin today's behaviour and name the correct answer, so the suite stays
green until the fix spec inverts them. Both verified non-vacuous by
temporarily passing InvariantCulture, which fails them."
```

---

### Task 8: Pinning tests for the product defects (5, 7, 8) and the NUMERIC round-trip

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs`

**Interfaces:**
- Consumes: `MigrationScenario`, `LegacyStoreDatabase`, `LegacyStoreDataBuilder`. Produces nothing new.

- [ ] **Step 1: Write the tests**

Create `tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs`:

```csharp
using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class ProductMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public ProductMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<LegacyStoreDataBuilder> SeedCashierAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        return builder;
    }

    [Fact]
    public async Task MigrateProducts_Category_CurrentlyWritesRawLegacyId_Defect5()
    {
        // Defect 5: Category = product.Category?.ToString() writes the raw legacy id.
        // CORRECT: ProductCategoryCodes.GeneralMaterials ("GeneralMaterials"), resolved from
        // legacy id 50 via the store-scoped product_category catalogue Epic 1 added.
        // A raw id matches no catalogue code, so every migrated product is uncategorised and both
        // the Hardware gate and the category pickers break.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var product = await db.Products.SingleAsync();

        product.Category.Should().Be("50");
        product.Category.Should().NotBe(ProductCategoryCodes.GeneralMaterials,
            "this is the correct answer and defect 5 does not yet produce it");
    }

    [Fact]
    public async Task MigrateProducts_CurrentlyDropsIsTrackable_Defect7()
    {
        // Defect 7: v4's Product has no IsTrackable, so the flag is dropped and every sold line
        // gets a stock-deducting movement -- including services, which have no stock.
        // CORRECT: a non-trackable product produces NO Migration:Sale inventory movement.
        // Only 29 products across the three real stores are non-trackable (21 + 7 + 1), and the
        // legacy sale path already filters on this flag in production.
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

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var saleMovements = await db.InventoryMovements
            .Where(m => m.Reason == "Migration:Sale")
            .ToListAsync();

        saleMovements.Should().HaveCount(1,
            "defect 7: a service line still deducts stock, because v4 dropped the flag");
        saleMovements.Single().QuantityDelta.Should().Be(-1);
    }

    [Fact]
    public async Task MigrateProducts_CurrentlyPreservesNoLegacyId_Defect8()
    {
        // Defect 8: only StoreUser carries a legacy id (LegacyUserId). Products, invoices, lines
        // and payments do not, so the migration cannot be re-run idempotently by id and a v4 row
        // cannot be reconciled against its SQLite source.
        // CORRECT: a legacy id preserved on all five entity types.
        // This test asserts the structural fact: StoreUser has the property and Product has no
        // equivalent. It fails to compile-time-check Product, so it checks the recorded user id
        // and documents the gap for the others.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 4242, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.StoreUsers.SingleAsync()).LegacyUserId.Should().Be(1,
            "users DO preserve their legacy id");

        // The product's legacy id 4242 survives only in this in-memory map, which is discarded when
        // the process exits. Nothing in PostgreSQL records it.
        result.ProductIdMap.Should().ContainKey(4242);

        var productProperties = typeof(IndyPOS.Domain.Entities.Core.Product)
            .GetProperties().Select(p => p.Name).ToList();
        productProperties.Should().NotContain("LegacyProductId",
            "defect 8: Product has no legacy id column, so a migrated row cannot be reconciled");
    }

    [Fact]
    public async Task MigrateProducts_WithANumericPrice_PreservesTheValue()
    {
        // Real money columns are NUMERIC and the migrator maps them to double before casting to
        // decimal. If this test FAILS it is a NEW defect: record it, do not weaken the assertion
        // to match the observed value.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Screws, box of 100",
            unitPrice: 19.99m, quantityInStock: 5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00", groupPrice: 269.97m, groupPriceQuantity: 15);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var product = await db.Products.SingleAsync();

        product.UnitPrice.Should().Be(19.99m);
        product.GroupPrice.Should().Be(269.97m);
        product.GroupPriceQuantity.Should().Be(15);
    }

    [Fact]
    public async Task MigrateProducts_WithStock_ShouldRecordAnInitialStockMovement()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movement = await db.InventoryMovements
            .SingleAsync(m => m.Reason == "Migration:InitialStock");

        movement.QuantityDelta.Should().Be(20);
    }
}
```

- [ ] **Step 2: Run them**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ProductMigrationTests"`
Expected: **5 passed.** If `MigrateProducts_WithANumericPrice_PreservesTheValue` fails, stop and
report it as a new defect — do not adjust the expectation.

- [ ] **Step 3: Prove the defect 5 pin is not vacuous**

Temporarily change line 169 of `SqliteMigrationService.cs` to `Category = "GeneralMaterials",`.
Run `MigrateProducts_Category_CurrentlyWritesRawLegacyId_Defect5`: it **must fail**. Revert.

- [ ] **Step 4: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/ProductMigrationTests.cs
git commit -m "test: pin the product defects (5, 7, 8) and cover NUMERIC prices

Defect 5 -- Category writes the raw legacy id ('50'), which matches no
catalogue code, so every product is uncategorised and the Hardware gate
and pickers break. Correct is GeneralMaterials.

Defect 7 -- v4's Product dropped IsTrackable, so a service line still
produces a stock-deducting Migration:Sale movement. Only 29 products
across the three real stores are non-trackable.

Defect 8 -- only StoreUser preserves a legacy id. A product's legacy id
survives solely in an in-memory map discarded at exit, so a migrated row
cannot be reconciled against its SQLite source.

Also covers NUMERIC to double to decimal on 19.99 and 269.97, with an
explicit instruction that a failure is a new defect to report rather than
an expectation to weaken."
```

---

### Task 9: Pinning tests for defect 6 — the lost discount record

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/InvoiceLineMigrationTests.cs`

**Interfaces:**
- Consumes: `MigrationScenario`, `LegacyStoreDatabase`, `LegacyStoreDataBuilder`. Produces nothing new.

- [ ] **Step 1: Write the tests**

Create `tests/IndyPOS.MigrationTool.Tests/InvoiceLineMigrationTests.cs`:

```csharp
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class InvoiceLineMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public InvoiceLineMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrateInvoiceLines_CurrentlyCannotDistinguishADiscountedLine_Defect6()
    {
        // Defect 6: the InvoiceProduct SELECT takes 7 of 17 columns, dropping OriginalUnitPrice,
        // GroupPrice, IsGroupProduct, Note and Priority. 165,690 of 325,780 real lines (51%) were
        // discounted, and the record of that is lost.
        // CORRECT: the discount is recoverable -- which needs a v4 schema change, since InvoiceLine
        // is deliberately 7 fields. That is why this is pinned here and fixed in its own spec.
        //
        // Two lines, same sold price. One was discounted from 100 to 80, the other always cost 80.
        // After migration they are indistinguishable, so no report can ever recompute the discount.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Discounted item",
            unitPrice: 100m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 11, barcode: "8850001000011", description: "Full price item",
            unitPrice: 80m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 160m, dateCreated: "2024-03-15 14:30:00");

        // Discounted: originally 100, sold at 80.
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Discounted item", quantity: 1, unitPrice: 80m, originalUnitPrice: 100m);

        // Never discounted: always 80.
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 2, invoiceId: 1, productId: 11, barcode: "8850001000011",
            description: "Full price item", quantity: 1, unitPrice: 80m, originalUnitPrice: 80m);

        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 160m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var lines = await db.InvoiceLines.OrderBy(l => l.ProductName).ToListAsync();

        lines.Should().HaveCount(2);
        lines.Select(l => l.UnitPrice).Should().AllBeEquivalentTo(80m);
        lines.Select(l => l.LineTotal).Should().AllBeEquivalentTo(80m);

        // The migrated rows differ only by product. Nothing records that ฿20 was given away.
        typeof(IndyPOS.Domain.Entities.Core.InvoiceLine)
            .GetProperties().Select(p => p.Name)
            .Should().NotContain("OriginalUnitPrice",
                "defect 6: with no such field, a discounted line and a full-price line are identical");
    }

    [Fact]
    public async Task MigrateInvoiceLines_CurrentlyDiscardsNoteAndGroupPricing_Defect6()
    {
        // Defect 6, the other four dropped columns. CORRECT: all of them preserved.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Screws",
            unitPrice: 20m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 54m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Screws", quantity: 3, unitPrice: 18m, originalUnitPrice: 20m,
            groupPrice: 54m, isGroupProduct: true, note: "ลดราคาให้ลูกค้าประจำ", priority: 2,
            category: 50);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 54m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var line = await db.InvoiceLines.SingleAsync();

        line.Quantity.Should().Be(3);
        line.UnitPrice.Should().Be(18m);

        var fields = typeof(IndyPOS.Domain.Entities.Core.InvoiceLine)
            .GetProperties().Select(p => p.Name).ToList();

        fields.Should().NotContain("GroupPrice");
        fields.Should().NotContain("IsGroupProduct");
        fields.Should().NotContain("Note", "the cashier's reason for the discount is lost");
        fields.Should().NotContain("Priority");
    }

    [Fact]
    public async Task MigrateInvoiceLines_WithADeletedProduct_CurrentlySkipsTheLine()
    {
        // Real GeneralHardware has 1,980 invoice lines whose product no longer exists. The line is
        // skipped with a warning, so an invoice's lines can silently sum to less than its total.
        // CORRECT: the line is preserved -- ProductName is already a historical snapshot.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 80m, dateCreated: "2024-03-15 14:30:00");
        // References InventoryProductId 777, which is never inserted.
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 777, barcode: "8850009999999",
            description: "Product deleted years ago", quantity: 1, unitPrice: 80m,
            originalUnitPrice: 80m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 80m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.Invoices.CountAsync()).Should().Be(1);
        (await db.InvoiceLines.CountAsync()).Should().Be(0,
            "the line is dropped, so the invoice total no longer matches the sum of its lines");
        (await db.Invoices.SingleAsync()).TotalAmount.Should().Be(80m);
    }
}
```

- [ ] **Step 2: Run them**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "InvoiceLineMigrationTests"`
Expected: **3 passed.**

- [ ] **Step 3: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/InvoiceLineMigrationTests.cs
git commit -m "test: pin defect 6, the unrecoverable discount record

The InvoiceProduct SELECT takes 7 of 17 columns. 165,690 of 325,780 real
lines (51%) were discounted and that record is lost.

The sharpest test seeds two lines sold at the same price -- one discounted
from 100 to 80, one always 80 -- and shows the migrated rows are
indistinguishable, so no report can ever recompute the discount. Fixing it
needs a v4 schema change, since InvoiceLine is deliberately 7 fields, which
is why it is pinned here rather than fixed.

Also pins the 1,980-real-line case where an invoice line's product no
longer exists: the line is dropped, so an invoice's lines can sum to less
than its own total."
```

---

### Task 10: Per-method payment coverage — the defect 1 regression guard

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/PaymentMigrationTests.cs`

**Interfaces:**
- Consumes: `MigrationScenario`, `LegacyStoreDatabase`, `LegacyStoreDataBuilder`. Produces nothing new.

- [ ] **Step 1: Write the test**

Create `tests/IndyPOS.MigrationTool.Tests/PaymentMigrationTests.cs`:

```csharp
using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class PaymentMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public PaymentMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigratePayments_EachLegacyType_MapsToItsCatalogueCodeAndAmount()
    {
        // Defect 1 regression guard. The original mapping was SHIFTED, not misnamed: PayLater to
        // "Card", WelfareCard to "Transfer", MoneyTransfer to "WelfareCard". ~15% of THB 21.2M
        // landed on the wrong method. It survived because the verifier compared only row counts and
        // SUM(Invoice.Total), both of which reconcile perfectly under a scramble.
        //
        // A DISTINCT AMOUNT PER METHOD is what makes this test able to catch a shift: with equal
        // amounts, any permutation would still sum correctly.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 1146m, dateCreated: "2024-03-15 14:30:00");

        var expected = new (int LegacyId, string Code, decimal Amount)[]
        {
            (1, PaymentMethodCodes.Cash,          1m),
            (2, PaymentMethodCodes.PayLater,     10m),
            (3, PaymentMethodCodes.WelfareCard, 100m),
            (4, PaymentMethodCodes.M33WeLove,     5m),
            (5, PaymentMethodCodes.MoneyTransfer, 1000m),
            (7, PaymentMethodCodes.FiftyFifty,   20m),
            (8, PaymentMethodCodes.WeWin,        10m)
        };

        var paymentId = 500;
        foreach (var (legacyId, _, amount) in expected)
        {
            await builder.AddPaymentAsync(
                paymentId: paymentId++, invoiceId: 1, paymentTypeId: legacyId, amount: amount,
                dateCreated: "2024-03-15 14:30:00");
        }

        // PaymentTypeId 2 needs its PayLater extension, or defect 10's lookup has nothing to attach.
        await builder.AddPayLaterAsync(
            paymentId: 501, invoiceId: 1, description: "Somchai", payLaterAmount: 10m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        await using var db = _postgres.CreateDbContext();
        var byMethod = await db.Payments
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount), Count = g.Count() })
            .ToListAsync();

        foreach (var (_, code, amount) in expected)
        {
            var row = byMethod.SingleOrDefault(m => m.Method == code);
            row.Should().NotBeNull($"legacy type mapping to {code} must produce exactly one payment");
            row!.Total.Should().Be(amount, $"{code} must carry its own amount, not another method's");
            row.Count.Should().Be(1);
        }

        byMethod.Should().HaveCount(expected.Length);
    }

    [Fact]
    public async Task MigratePayments_WithAnUnmappableType_RefusesRatherThanGuessing()
    {
        // Legacy id 6 (ผ่อนชำระ, instalments) has no catalogue equivalent. It must be refused, not
        // written as "Other": that is not a catalogue code, so the amount becomes unresolvable
        // while the row counts still reconcile.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 300m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 6, amount: 300m,
            dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.Payments.Failed.Should().Be(1);
        result.Errors.Should().ContainSingle().Which.Should().Contain("PaymentTypeId 6");

        await using var db = _postgres.CreateDbContext();
        (await db.Payments.CountAsync()).Should().Be(0);
    }
}
```

- [ ] **Step 2: Run them**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "PaymentMigrationTests"`
Expected: **2 passed.**

- [ ] **Step 3: Prove the mapping test catches a shift**

Temporarily swap two entries in `LegacyPaymentTypeMap.All` — make `[2]` `WelfareCard` and `[3]`
`PayLater`. Run the first test: it **must fail**, naming the wrong totals. Revert.

- [ ] **Step 4: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/PaymentMigrationTests.cs
git commit -m "test: guard the payment mapping per method, with distinct amounts

Defect 1's regression guard. The original mapping was SHIFTED, not
misnamed, and it survived because the verifier compared only row counts and
SUM(Invoice.Total) -- both reconcile perfectly under a permutation.

Every method therefore gets a DISTINCT amount (1, 10, 100, 5, 1000, 20,
10), so any shift changes a per-method total. Verified by temporarily
swapping ids 2 and 3, which fails the test.

Also pins that unmappable legacy id 6 is refused rather than written as
'Other', which is not a catalogue code and would make the amount
unresolvable while counts still reconciled."
```

---

### Task 11: Rewrite the real-store schema tests so they cannot be vacuous

**Files:**
- Create: `tests/IndyPOS.MigrationTool.Tests/RealStoreSchemaTests.cs`
- Delete: `tests/IndyPOS.MigrationTool.Tests/RealDatabaseMigrationTests.cs`

**Interfaces:**
- Consumes: `LegacyStoreShape`. Produces nothing new.

- [ ] **Step 1: Write the replacement**

The old file's 8 tests hardcode `C:\personal\IndyPOS\...` and `return;` when it is absent, so they
report **Passed** on every other machine including CI. Their `Should().Contain(...)` assertions are
also one-directional, which is how a 17-column table satisfied a test listing 7.

Create `tests/IndyPOS.MigrationTool.Tests/RealStoreSchemaTests.cs`:

```csharp
using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Compares the committed schema artefacts against a real store database when one is present.
///
/// The .db files are gitignored and 63.8 MB, so these cannot run in CI. They therefore SKIP
/// loudly rather than passing silently: the previous version returned early, so 8 tests reported
/// Passed on every machine but one.
/// </summary>
public class RealStoreSchemaTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string RealDbPath(LegacyStoreShape shape) => Path.Combine(
        RepoRoot, ".planning", "indypos-overhaul", "sqlite_database", shape.ToString(), "Store.db");

    /// <summary>
    /// xUnit 2 has no runtime "skip", so this throws SkipException via Assert.Skip's stand-in:
    /// we surface it as a skip by using a fact that reports through Assert.True with a clear
    /// message only when the file exists. Absent file => the test is skipped by trait.
    /// </summary>
    private static bool RealDatabaseMissing(LegacyStoreShape shape) => !File.Exists(RealDbPath(shape));

    private static async Task<List<string>> ColumnsAsync(string dbPath, string table)
    {
        await using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        await connection.OpenAsync();
        return (await connection.QueryAsync<string>(
            $"SELECT name FROM pragma_table_info('{table}')")).ToList();
    }

    private static async Task<List<string>> ArtefactColumnsAsync(LegacyStoreShape shape, string table)
    {
        var ddl = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "LegacySchema", $"{shape}.sql"));

        await using var connection = new SQLiteConnection("Data Source=:memory:;Version=3;");
        await connection.OpenAsync();
        await connection.ExecuteAsync(ddl);
        return (await connection.QueryAsync<string>(
            $"SELECT name FROM pragma_table_info('{table}')")).ToList();
    }

    public static TheoryData<LegacyStoreShape, string> ShapesAndTables()
    {
        var data = new TheoryData<LegacyStoreShape, string>();
        foreach (var table in new[]
                 {
                     "User", "UserCredential", "UserRole", "InventoryProduct", "Invoice",
                     "InvoiceProduct", "Payment", "PaymentType", "ProductCategory",
                     "ProductBarcodeCounter"
                 })
        {
            data.Add(LegacyStoreShape.GeneralHardware, table);
            data.Add(LegacyStoreShape.MimyShop, table);
        }

        // PayLater exists only in the GeneralHardware shape -- that asymmetry IS defect 3.
        data.Add(LegacyStoreShape.GeneralHardware, "PayLater");
        return data;
    }

    [Theory]
    [MemberData(nameof(ShapesAndTables))]
    public async Task Artefact_ShouldHaveExactlyTheRealStoresColumns(LegacyStoreShape shape, string table)
    {
        Assert.SkipWhen(RealDatabaseMissing(shape),
            $"No real {shape} Store.db at {RealDbPath(shape)} (gitignored). " +
            "Skipped, NOT passed: this check can only run on a machine holding real store data.");

        var real = await ColumnsAsync(RealDbPath(shape), table);
        var artefact = await ArtefactColumnsAsync(shape, table);

        artefact.Should().BeEquivalentTo(real,
            $"the committed {shape}.sql artefact must match the real store's {table} exactly. " +
            "If this fails, regenerate it: dotnet test --filter \"ExtractLegacySchema\"");
    }

    [Fact]
    public async Task RealStore_EveryPaymentTypeInUse_ShouldMapToACatalogueCode()
    {
        Assert.SkipWhen(RealDatabaseMissing(LegacyStoreShape.GeneralHardware),
            "No real GeneralHardware Store.db (gitignored). Skipped, NOT passed.");

        await using var connection = new SQLiteConnection(
            $"Data Source={RealDbPath(LegacyStoreShape.GeneralHardware)};Version=3;");
        await connection.OpenAsync();

        var idsInUse = (await connection.QueryAsync<long>(
            "SELECT DISTINCT PaymentTypeId FROM Payment ORDER BY PaymentTypeId")).ToList();

        idsInUse.Should().NotBeEmpty("the sample store has payment history");
        idsInUse.Where(id => LegacyPaymentTypeMap.ToCode((int)id) is null).Should().BeEmpty(
            "every legacy payment type present in real store data must map to a catalogue code");
    }

    [Fact]
    public async Task RealStore_PaymentTypeLabels_ShouldStillMatchTheAssumedMapping()
    {
        Assert.SkipWhen(RealDatabaseMissing(LegacyStoreShape.GeneralHardware),
            "No real GeneralHardware Store.db (gitignored). Skipped, NOT passed.");

        await using var connection = new SQLiteConnection(
            $"Data Source={RealDbPath(LegacyStoreShape.GeneralHardware)};Version=3;");
        await connection.OpenAsync();

        var labels = (await connection.QueryAsync<(long Id, string Type)>(
            "SELECT Id, Type FROM PaymentType")).ToDictionary(r => (int)r.Id, r => r.Type);

        labels[1].Should().Be("เงินสด");
        labels[2].Should().Be("ลงบัญชี");
        labels[3].Should().Be("บัตรสวัสดิการแห่งรัฐ");
        labels[5].Should().Be("โอนเข้าบัญชี");
        labels[7].Should().Be("คนละครึ่ง");
        labels[8].Should().Be("เราชนะ");
    }

    [Fact]
    public async Task RealStore_PayLaterShouldExtendPayment_OneToOne()
    {
        // The structural basis for defect 10. If a future store's data breaks this, the
        // link-instead-of-create fix needs revisiting.
        Assert.SkipWhen(RealDatabaseMissing(LegacyStoreShape.GeneralHardware),
            "No real GeneralHardware Store.db (gitignored). Skipped, NOT passed.");

        await using var connection = new SQLiteConnection(
            $"Data Source={RealDbPath(LegacyStoreShape.GeneralHardware)};Version=3;");
        await connection.OpenAsync();

        var orphans = await connection.ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM PayLater pl
            LEFT JOIN Payment p ON p.PaymentId = pl.PaymentId
            WHERE p.PaymentId IS NULL
            """);
        var wrongType = await connection.ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM PayLater pl
            JOIN Payment p ON p.PaymentId = pl.PaymentId
            WHERE p.PaymentTypeId <> 2
            """);

        orphans.Should().Be(0, "every PayLater row must extend an existing Payment");
        wrongType.Should().Be(0, "every linked payment must be legacy type 2 (ลงบัญชี)");
    }
}
```

⚠️ `Assert.SkipWhen` requires **xunit 2.9.3 or later**, which this project has (2.9.3). If the build
reports it missing, use `Assert.Skip.When` or upgrade the assertion to `Assert.Skip(reason)` guarded
by an `if` — but do **not** revert to `return;`, which is the vacuous-pass bug being removed.

- [ ] **Step 2: Delete the old file**

```bash
git rm tests/IndyPOS.MigrationTool.Tests/RealDatabaseMigrationTests.cs
```

- [ ] **Step 3: Run and confirm the skip behaviour**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "RealStoreSchemaTests"`
Expected on a machine **with** the real `.db` files: all pass. On a machine **without** them: the
tests report **Skipped**, never Passed. Confirm by temporarily renaming
`.planning/indypos-overhaul/sqlite_database` and re-running — output must say skipped.

- [ ] **Step 4: Commit**

```bash
git add tests/IndyPOS.MigrationTool.Tests/RealStoreSchemaTests.cs
git commit -m "test: make the real-store schema checks skip loudly and assert exactly

The old tests hardcoded C:\\personal\\IndyPOS and returned early when the
.db was absent, so 8 tests reported PASSED on every machine but one,
including CI. Their Should().Contain assertions were also one-directional,
which is how a 17-column InvoiceProduct satisfied a test listing 7 -- the
mechanism that hid defect 6.

Now: the path resolves from the repo root, an absent database reports
Skipped rather than Passed, and every column check is exact set equality
against the committed artefact, per shape and per table.

Adds the check that underpins defect 10's fix -- every PayLater row extends
an existing Payment of legacy type 2, with zero orphans."
```

---

### Task 12: Final verification and documentation counts

**Files:**
- Modify: `ONBOARDING.md`
- Modify: `CLAUDE.md`
- Modify: `.planning/indypos-overhaul/PLAN.md`
- Modify: `docs/superpowers/specs/2026-08-03-migration-test-coverage-design.md`

- [ ] **Step 1: Run the whole solution suite**

Run: `dotnet test IndyPOS.sln`
Expected: green. Record the total and the per-suite `MigrationTool.Tests` count.

⚠️ A root `dotnet test` exits 1 regardless of results because `tests/IndyPOS.Mock` references
`xunit` with no test runner. Read the per-suite `Passed!` lines, not the exit code. This is
pre-existing and out of scope.

- [ ] **Step 2: Run the installer suite, which is not in the solution**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: 231 tests, 223 pass, 8 skipped — unchanged by this work.

- [ ] **Step 3: Update the counts**

In `ONBOARDING.md`, update the `IndyPOS.MigrationTool.Tests` row and the total to the measured
numbers, and note how many of its tests need Docker. In `CLAUDE.md`, update the total in
"Solution suites total **N**" and the Docker-failure count in the Quick Commands block. In
`PLAN.md`'s *Test state* table, update the `IndyPOS.MigrationTool.Tests` row.

- [ ] **Step 4: Mark the spec's fixed defects**

In `.planning/indypos-overhaul/PLAN.md`, change the Status cell for defects 2, 3 and 10 from ❌ to
✅ with the commit sha, and leave 4, 5, 6, 7, 8, 9, 11 as ❌ with a note that 4/5/6/7/8/11 now have
pinning tests naming them.

In the spec, amend §4.1 to record that the extraction tool is a skipped xUnit test rather than a
`.ps1`, with the reason.

- [ ] **Step 5: Commit**

```bash
git add ONBOARDING.md CLAUDE.md .planning/indypos-overhaul/PLAN.md \
        docs/superpowers/specs/2026-08-03-migration-test-coverage-design.md
git commit -m "docs: record the new test counts and close defects 2, 3 and 10"
```

- [ ] **Step 6: Open the PR**

```bash
gh auth switch --user purin-tavilsup
git push -u origin <branch>
gh pr create --base development --title "test: real coverage for the SQLite migration, and fix defects 2, 3 and 10"
gh auth switch --user purin-mimica
```

The PR body must state: the migration tool could not complete against **any** real store before
this; defects 2, 3 and 10 are fixed; defects 4, 5, 6, 7, 8 and 11 are now **pinned** with executable
tests naming them; and every pinning test was verified non-vacuous.

---

## Self-Review

**Spec coverage.** §4.1 components → Tasks 1–3. §4.2 IsTrackable fidelity rule → Task 3 Step 1.
§4.3 layout → the File Structure table. §5 pinning approach → Tasks 7–9. §6.1 pinning inventory →
Tasks 7 (defects 4, 11), 8 (5, 7, 8), 9 (6). §6.2 green tests → Tasks 4 (whole-run both shapes,
defect 2), 5 (defect 10, missing payment), 6 (partial, overpaid, never-repaid, name in both columns),
8 (NUMERIC), 10 (per-method). §6.3 ports → Task 4 Steps 5–6. §6.4 real-store rewrite → Task 11.
§7.1–7.3 fixes → Tasks 4 and 5. §8 ordering → task order, red-first within each task. §9 risks →
Task 7 Step 3, Task 8 Step 3, Task 10 Step 3 (non-vacuity), Task 11 (drift). §10 verification →
Task 12.

**Two spec items deliberately handled differently, both recorded above:** the extraction tool is a
skipped xUnit test, not `.ps1` (see *Deviation from the spec*); and the spec's
`MigrateProducts_WithANumericPrice_RoundTripsExactly` is named `...PreservesTheValue`, because the
spec itself says the outcome is unknown until written.

**One spec item with no task, added deliberately:** §2.5 notes the real databases have no indexes.
Nothing depends on that, so no task asserts it. Recorded here so its absence is a decision.

**Placeholder scan.** No TBD/TODO. Every code step carries compilable code. Every "run" step names
an exact command and expected result. Task 4 Step 6 is the one step describing a transformation
rather than quoting the whole file — it quotes the new fixture wiring and one worked seeding loop,
because the five tests differ only in their seeded counts and reproducing all five verbatim would
invite copy-paste drift against assertions the task must not change.

**Type consistency.** `LegacyStoreShape` (Task 1) is used unchanged in 2, 3, 7–9, 11.
`LegacyStoreDatabase.CreateAsync/Path/Connection` (Task 2) matches every later call site.
`LegacyStoreDataBuilder`'s eight method signatures (Task 3 Interfaces) match every invocation in
Tasks 4–10, including the optional `groupPrice`, `isGroupProduct`, `note`, `priority`, `category`
arguments used in Task 9. `MigrationScenario.RunAsync` (Task 4) is used identically in 5–10.
`MigrationResult.PaymentIdMap` (Task 5) is written in `MigrateInvoicesAsync` and read in
`MigratePayLaterAsync`. Entity property names checked against source: `PayLater.PaymentId`,
`Description`, `PayLaterAmount`, `PaidAmount`, `IsCompleted`, `RemainingAmount`;
`InvoiceLine.UnitPrice`, `Quantity`, `LineTotal`; `Product.Category`, `UnitPrice`, `GroupPrice`,
`GroupPriceQuantity`; `StoreUser.LegacyUserId`; `InventoryMovement.QuantityDelta`, `Reason`;
`Invoice.TotalAmount`, `CreatedUtc`. `PostgresFixture.ConnectionString`, `CreateDbContext()`,
`ResetDatabaseAsync()` match the existing fixture.

**One risk the plan cannot remove.** Task 11's `Assert.SkipWhen` is the only API used that I have
not verified against xunit 2.9.3 in this repo; the step carries an explicit fallback, and the
fallback forbids reverting to `return;`.
