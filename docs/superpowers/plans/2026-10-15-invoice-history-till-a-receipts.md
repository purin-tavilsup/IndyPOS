# Invoice History, Till Plan A — Receipts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every v4 receipt prints its real bill number, sale time and cashier, and can no longer crash in a RELEASE build. A copy receipt can be built from the reprint response, ready for plan B.

**Architecture:** The spec's `ReceiptDocument` becomes the printer's only input. A pure `ReceiptLayout` turns header + document into rows of text, so every printed word is unit-tested without a printer. `ReceiptRenderer` only draws those rows. `ReceiptDocumentFactory` builds the document after a sale (from the till's sale and the `CompleteSaleResponse`) and on reprint (from `InvoiceReprintResultDto`). Payment display names come from one shared `IPaymentMethodNames` provider, which `SalePanel` also moves onto.

**Tech Stack:** .NET 10, xUnit 2.9, Moq 4.20, FluentAssertions 8.8, System.Drawing printing (net10.0-windows).

**Spec:** `docs/superpowers/specs/2026-09-27-invoice-history-v4-design.md` §8 (receipt printing), with §7's reprint inputs. Plan 3 of that spec is split into three PRs: **A — receipts (this plan)**, B — `IndyPOS.Presentation` + client methods + ViewModels, C — screens + stub removal.

## Why this PR first

Two live defects sit on the receipt path. Neither has been noticed because no till runs a v4 RELEASE build yet.

1. **Every v4 receipt prints `Invoice No.: 0000000000`.** `StoreHubSaleService.CreateInvoiceInfo` (`src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubSaleService.cs:340`) sets the legacy `Id = 0` and ignores `response.InvoiceNumber`, which the server has returned since plan 1.
2. **Every v4 receipt would crash in RELEASE.** `ReceiptPrinterService.GetPaymentDescription` (`src/IndyPOS.Infrastructure/Services/ReceiptPrinterService.cs:272`) reads `_paymentTypeDictionary[payment.PaymentTypeId]`. v4 payments carry a catalog `Method` code and leave `PaymentTypeId` at 0 (`StoreHubSaleService.AddPayment`, line 214). The dictionary is keyed 1–7, so the print throws `KeyNotFoundException`. `SalePanel.ResolvePaymentTypeDisplay` already learned this lesson; the printer never did.

The receipt also prints `DateTime.Now` instead of the sale's time, and the logged-in user instead of the bill's cashier.

## Global Constraints

- Bill number prints as `Invoice No.: {InvoiceNumber:0000000000}` (spec §8).
- The cashier printed is the document's cashier, **not** the logged-in user (spec §8).
- A copy prints a large **สำเนา / COPY** banner at the top and the line **`พิมพ์ซ้ำ dd/MM/yyyy HH:mm โดย <name>`** (spec §8).
- Times are shown in the store's time zone (`IStoreIdentityService.TimeZone`, default `SE Asia Standard Time`); the server sends UTC (spec §7, §8).
- Refund labels work as today: `(คืนสินค้า)` on lines, `คืนเงิน` on payments (spec §8).
- The `[Conditional("RELEASE")]` print gate is unchanged (spec §8).
- The reprinter's name is the logged-in user's (plan 1, D9).
- Dates and amounts are formatted in the till's current culture, as v3 receipts were. On a th-TH till this means Thai month names and the Buddhist year.
- Tests: `Subject_WhenScenario_DirectVerbOutcome`, negative cases first, one behaviour per test, Arrange-Act-Assert separated by blank lines, FluentAssertions with vertically aligned chains.
- No new NuGet packages.

## Review Focus

1. **A payment code the name catalog does not know** (a campaign method added after the till loaded the catalog, or the catalog failed to load). The receipt prints the raw code and never throws. Pinned in Task 3 and Task 4.
2. **A reprinted bill whose cashier name is null** (the server's `CashierName` is nullable: a user deleted since). The receipt prints `-`. Pinned in Task 2.
3. **A sale made between 00:00 and 07:00 Bangkok time**, when the UTC date is the day before. The receipt shows the Bangkok date. Pinned in Task 2.
4. **A bill number wider than 10 digits.** The format pads but must never truncate. Pinned in Task 1.
5. **A th-TH till.** The date line shows the Buddhist year, as v3 did, and the bill-number line is unaffected by culture. Pinned in Task 1.

---

## File Structure

| File | Responsibility |
|---|---|
| Create `src/IndyPOS.Application/Common/Receipts/ReceiptDocument.cs` | The records: `ReceiptDocument`, `ReceiptItem`, `ReceiptPayment`, `ReceiptReprint`, `ReceiptHeader` |
| Create `src/IndyPOS.Application/Common/Receipts/ReceiptLayout.cs` | Pure: header + document + culture → `IReadOnlyList<ReceiptRow>`; also `ReceiptRow`, `ReceiptRowStyle` |
| Create `src/IndyPOS.Application/Common/Receipts/ReceiptDocumentFactory.cs` | Builds a document after a sale (`ForSale`) and on reprint (`ForReprint`); also `CompletedSale` |
| Create `src/IndyPOS.Application/Abstractions/StoreHub/IPaymentMethodNames.cs` | Code → display name lookup the till shares |
| Create `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubPaymentMethodNames.cs` | Loads the offerable catalog once; falls back to an empty map without caching the failure |
| Create `src/IndyPOS.Infrastructure/Services/ReceiptRenderer.cs` | Draws rows on a `Graphics`; nothing else |
| Modify `src/IndyPOS.Infrastructure/Services/ReceiptPrinterService.cs` | Prints a `ReceiptDocument` through layout + renderer |
| Modify `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubSaleService.cs` | `CompleteSaleAsync` returns a `ReceiptDocument` |
| Modify `src/IndyPOS.Application/Common/Interfaces/ISaleService.cs`, `IReceiptPrinterService.cs` | Signatures take/return `ReceiptDocument` |
| Modify `src/IndyPOS.Infrastructure/ConfigureServices.cs` | Register `IPaymentMethodNames`, `ReceiptDocumentFactory` |
| Modify `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs`, `PrintReceiptForm.cs` | Use the provider; pass the document through |
| Modify `src/IndyPOS.Windows.Forms/UI/Report/SalesHistoryReportPanel.cs` (+ `.Designer.cs`) | Hide its print button until plan C rebuilds the panel |
| Delete `IInvoiceInfo`, `InvoiceInfo`, `IReportService.GetInvoiceInfoAsync`, `StubInvoiceInfo` | Dead once the printer reads `ReceiptDocument` |

Tests:
- `tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptLayoutTests.cs`
- `tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptDocumentFactoryTests.cs`
- `tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubPaymentMethodNamesTests.cs`
- `tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubSaleServiceTests.cs` (new — the service had no tests)
- `tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptRendererTests.cs`

**Branch:** `feat/receipt-document` from `development`.

---

### Task 1: `ReceiptDocument` and `ReceiptLayout`

**Files:**
- Create: `src/IndyPOS.Application/Common/Receipts/ReceiptDocument.cs`
- Create: `src/IndyPOS.Application/Common/Receipts/ReceiptLayout.cs`
- Test: `tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptLayoutTests.cs`

**Interfaces:**
- Produces:
  - `record ReceiptDocument(long InvoiceNumber, DateTime IssuedAt, string CashierName, IReadOnlyList<ReceiptItem> Items, IReadOnlyList<ReceiptPayment> Payments, decimal Total, decimal AmountReceived, decimal Change, bool IsRefund, ReceiptReprint? Reprint = null)` with `bool IsCopy => Reprint is not null`
  - `record ReceiptItem(string Description, string? Note, int Quantity, decimal UnitPrice, decimal LineTotal)`
  - `record ReceiptPayment(string DisplayName, decimal Amount, string? Note)`
  - `record ReceiptReprint(DateTime ReprintedAt, string ReprintedBy)`
  - `record ReceiptHeader(string StoreName, string AddressLine1, string AddressLine2, string PhoneNumber)`
  - `enum ReceiptRowStyle { Banner, StoreName, Text, Indented, Rule }`
  - `record ReceiptRow(ReceiptRowStyle Style, string Text = "", string? Amount = null)`
  - `static class ReceiptLayout { const string CopyBanner; static IReadOnlyList<ReceiptRow> Rows(ReceiptHeader, ReceiptDocument, CultureInfo) }`
  - All in namespace `IndyPOS.Application.Common.Receipts`. `IssuedAt` and `ReprintedAt` are already store-local times; the layout never converts.

- [ ] **Step 1: Create the records**

`src/IndyPOS.Application/Common/Receipts/ReceiptDocument.cs`:

```csharp
namespace IndyPOS.Application.Common.Receipts;

/// <summary>
/// Everything a receipt prints, and the printer's only input. Times are already in the store's time
/// zone. Built by <see cref="ReceiptDocumentFactory"/>, after a sale or for a reprint.
/// </summary>
public sealed record ReceiptDocument(
    long InvoiceNumber,
    DateTime IssuedAt,
    string CashierName,
    IReadOnlyList<ReceiptItem> Items,
    IReadOnlyList<ReceiptPayment> Payments,
    decimal Total,
    decimal AmountReceived,
    decimal Change,
    bool IsRefund,
    ReceiptReprint? Reprint = null)
{
    public bool IsCopy => Reprint is not null;
}

public sealed record ReceiptItem(string Description, string? Note, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record ReceiptPayment(string DisplayName, decimal Amount, string? Note);

/// <summary>Who asked for the copy, and when (store-local).</summary>
public sealed record ReceiptReprint(DateTime ReprintedAt, string ReprintedBy);

/// <summary>The store lines at the top of every receipt, from the till's store configuration.</summary>
public sealed record ReceiptHeader(string StoreName, string AddressLine1, string AddressLine2, string PhoneNumber);
```

- [ ] **Step 2: Write the failing layout tests**

`tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptLayoutTests.cs`:

```csharp
using System.Globalization;
using FluentAssertions;
using IndyPOS.Application.Common.Receipts;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Receipts;

public class ReceiptLayoutTests
{
    private const long ElevenDigitNumber = 12_345_678_901;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly CultureInfo Thai = new("th-TH");
    private static readonly ReceiptHeader Header = new("Test Store", "1 Main Road", "Town 10000", "000-000-0000");

    [Fact]
    public void Rows_WithABillNumberWiderThanTenDigits_PrintsEveryDigit()
    {
        var document = Sale() with { InvoiceNumber = ElevenDigitNumber };

        var rows = ReceiptLayout.Rows(Header, document, Invariant);

        Texts(rows).Should()
                   .Contain("Invoice No.: 12345678901");
    }

    [Fact]
    public void Rows_WithAPaymentNotInTheCatalog_PrintsItsName()
    {
        var document = Sale() with { Payments = [new ReceiptPayment("NewCampaign2027", 100m, null)] };

        var rows = ReceiptLayout.Rows(Header, document, Invariant);

        Texts(rows).Should()
                   .Contain("NewCampaign2027");
    }

    [Fact]
    public void Rows_WithARefund_LabelsEachItemAsReturned()
    {
        var document = Sale() with { IsRefund = true, Items = [new ReceiptItem("Hammer", null, -1, 250m, -250m)] };

        var rows = ReceiptLayout.Rows(Header, document, Invariant);

        Texts(rows).Should()
                   .Contain("Hammer : (คืนสินค้า)");
    }

    [Fact]
    public void Rows_WithARefundedItemWithANote_KeepsTheNote()
    {
        var document = Sale() with { IsRefund = true, Items = [new ReceiptItem("Hammer", "broken", -1, 250m, -250m)] };

        var rows = ReceiptLayout.Rows(Header, document, Invariant);

        Texts(rows).Should()
                   .Contain("Hammer : broken (คืนสินค้า)");
    }

    [Fact]
    public void Rows_WithARefund_LabelsEachPaymentAsReturned()
    {
        var document = Sale() with { IsRefund = true, Payments = [new ReceiptPayment("เงินสด", -250m, null)] };

        var rows = ReceiptLayout.Rows(Header, document, Invariant);

        Texts(rows).Should()
                   .Contain("เงินสด : คืนเงิน");
    }

    [Fact]
    public void Rows_WithAnOriginal_PrintsNoCopyBanner()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Invariant);

        rows.Should()
            .NotContain(r => r.Style == ReceiptRowStyle.Banner);
    }

    [Fact]
    public void Rows_WithACopy_PrintsTheBannerFirst()
    {
        var rows = ReceiptLayout.Rows(Header, Copy(), Invariant);

        rows[0].Should()
               .Be(new ReceiptRow(ReceiptRowStyle.Banner, ReceiptLayout.CopyBanner));
    }

    [Fact]
    public void Rows_WithACopy_PrintsWhoReprintedItAndWhen()
    {
        var rows = ReceiptLayout.Rows(Header, Copy(), Invariant);

        Texts(rows).Should()
                   .Contain("พิมพ์ซ้ำ 16/10/2026 09:05 โดย Somchai Manager");
    }

    [Fact]
    public void Rows_OnAThaiTill_PrintsTheBuddhistYear()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Thai);

        Texts(rows).Should()
                   .Contain(t => t.StartsWith("Date: ") && t.EndsWith("2569"));
    }

    [Fact]
    public void Rows_OnAThaiTill_PrintsTheBillNumberUnchanged()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Thai);

        Texts(rows).Should()
                   .Contain("Invoice No.: 0000000042");
    }

    [Fact]
    public void Rows_WithASale_PrintsTheBillNumberPaddedToTenDigits()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Invariant);

        Texts(rows).Should()
                   .Contain("Invoice No.: 0000000042");
    }

    [Fact]
    public void Rows_WithASale_PrintsTheDocumentsCashier()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Invariant);

        Texts(rows).Should()
                   .Contain("Cashier: Malee Cashier");
    }

    [Fact]
    public void Rows_WithASale_PrintsTheSaleTime()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Invariant);

        Texts(rows).Should()
                   .Contain(["Date: 15 October 2026", "Time: 02:30 PM"]);
    }

    [Fact]
    public void Rows_WithAnItem_PrintsQuantityTimesPriceAndTheLineTotal()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Invariant);

        rows.Should()
            .Contain(new ReceiptRow(ReceiptRowStyle.Indented, "2 x 50.00", "100.00"));
    }

    [Fact]
    public void Rows_WithAPaymentNote_PrintsTheNoteAfterTheName()
    {
        var document = Sale() with { Payments = [new ReceiptPayment("โอนเงิน", 100m, "KBank")] };

        var rows = ReceiptLayout.Rows(Header, document, Invariant);

        Texts(rows).Should()
                   .Contain("โอนเงิน : KBank");
    }

    [Fact]
    public void Rows_WithASale_PrintsTotalReceivedAndChange()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Invariant);

        rows.Should()
            .Contain([
                new ReceiptRow(ReceiptRowStyle.Text, "ราคารวม", "100.00"),
                new ReceiptRow(ReceiptRowStyle.Text, "เงินรวม", "500.00"),
                new ReceiptRow(ReceiptRowStyle.Text, "เงินทอน", "400.00")
            ]);
    }

    [Fact]
    public void Rows_WithASale_StartsWithTheStoreName()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Invariant);

        rows[0].Should()
               .Be(new ReceiptRow(ReceiptRowStyle.StoreName, "Test Store"));
    }

    private static ReceiptDocument Sale() => new(
        InvoiceNumber: 42,
        IssuedAt: new DateTime(2026, 10, 15, 14, 30, 0),
        CashierName: "Malee Cashier",
        Items: [new ReceiptItem("Nails 1kg", null, 2, 50m, 100m)],
        Payments: [new ReceiptPayment("เงินสด", 500m, null)],
        Total: 100m,
        AmountReceived: 500m,
        Change: 400m,
        IsRefund: false);

    private static ReceiptDocument Copy() =>
        Sale() with { Reprint = new ReceiptReprint(new DateTime(2026, 10, 16, 9, 5, 0), "Somchai Manager") };

    private static IEnumerable<string> Texts(IEnumerable<ReceiptRow> rows) => rows.Select(r => r.Text);
}
```

- [ ] **Step 3: Run the tests to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ReceiptLayoutTests"`
Expected: build FAILS with `CS0103: The name 'ReceiptLayout' does not exist` (and `ReceiptRow`, `ReceiptRowStyle`). That is the missing feature, not a typo.

- [ ] **Step 4: Write the layout**

`src/IndyPOS.Application/Common/Receipts/ReceiptLayout.cs`:

```csharp
using System.Globalization;

namespace IndyPOS.Application.Common.Receipts;

public enum ReceiptRowStyle
{
    Banner,
    StoreName,
    Text,
    Indented,
    Rule
}

/// <summary>One printed row: text on the left, and an amount in the price column when there is one.</summary>
public sealed record ReceiptRow(ReceiptRowStyle Style, string Text = "", string? Amount = null);

/// <summary>
/// Every word a receipt prints, in order. Pure, so the content is tested without a printer; the
/// renderer only draws these rows. Dates and amounts follow the given culture, as v3 receipts did.
/// </summary>
public static class ReceiptLayout
{
    public const string CopyBanner = "สำเนา / COPY";

    private static readonly ReceiptRow RuleRow = new(ReceiptRowStyle.Rule);

    public static IReadOnlyList<ReceiptRow> Rows(ReceiptHeader header, ReceiptDocument document, CultureInfo culture)
    {
        var rows = new List<ReceiptRow>();

        AddCopyBanner(rows, document.Reprint, culture);
        AddHeader(rows, header);
        AddBillInfo(rows, document, culture);
        AddItems(rows, document, culture);
        AddPayments(rows, document, culture);

        return rows;
    }

    private static void AddCopyBanner(List<ReceiptRow> rows, ReceiptReprint? reprint, CultureInfo culture)
    {
        if (reprint is null)
            return;

        var reprintedAt = reprint.ReprintedAt.ToString("dd/MM/yyyy HH:mm", culture);
        rows.Add(new ReceiptRow(ReceiptRowStyle.Banner, CopyBanner));
        rows.Add(new ReceiptRow(ReceiptRowStyle.Text, $"พิมพ์ซ้ำ {reprintedAt} โดย {reprint.ReprintedBy}"));
    }

    private static void AddHeader(List<ReceiptRow> rows, ReceiptHeader header)
    {
        rows.Add(new ReceiptRow(ReceiptRowStyle.StoreName, header.StoreName));
        rows.Add(new ReceiptRow(ReceiptRowStyle.Text, header.AddressLine1));
        rows.Add(new ReceiptRow(ReceiptRowStyle.Text, header.AddressLine2));
        rows.Add(new ReceiptRow(ReceiptRowStyle.Text, $"Tel.: {header.PhoneNumber}"));
        rows.Add(RuleRow);
    }

    private static void AddBillInfo(List<ReceiptRow> rows, ReceiptDocument document, CultureInfo culture)
    {
        // The bill number is digits only, so it must not take the culture's digit grouping.
        rows.Add(Text($"Invoice No.: {document.InvoiceNumber.ToString("0000000000", CultureInfo.InvariantCulture)}"));
        rows.Add(Text($"Date: {document.IssuedAt.ToString("dd MMMM yyyy", culture)}"));
        rows.Add(Text($"Time: {document.IssuedAt.ToString("hh:mm tt", culture)}"));
        rows.Add(Text($"Cashier: {document.CashierName}"));
        rows.Add(RuleRow);
    }

    private static void AddItems(List<ReceiptRow> rows, ReceiptDocument document, CultureInfo culture)
    {
        rows.Add(Text("รายการสินค้า"));

        foreach (var item in document.Items)
        {
            rows.Add(Text(ItemDescription(item, document.IsRefund)));
            rows.Add(new ReceiptRow(ReceiptRowStyle.Indented,
                                    $"{item.Quantity} x {item.UnitPrice.ToString("N", culture)}",
                                    item.LineTotal.ToString("N", culture)));
        }

        rows.Add(RuleRow);
        rows.Add(Text("ราคารวม", document.Total.ToString("N", culture)));
        rows.Add(RuleRow);
    }

    private static void AddPayments(List<ReceiptRow> rows, ReceiptDocument document, CultureInfo culture)
    {
        rows.Add(Text("รายการเงิน"));

        foreach (var payment in document.Payments)
            rows.Add(Text(PaymentDescription(payment, document.IsRefund), payment.Amount.ToString("N", culture)));

        rows.Add(RuleRow);
        rows.Add(Text("เงินรวม", document.AmountReceived.ToString("N", culture)));
        rows.Add(Text("เงินทอน", document.Change.ToString("N", culture)));
        rows.Add(RuleRow);
    }

    private static string ItemDescription(ReceiptItem item, bool isRefund) => (isRefund, string.IsNullOrEmpty(item.Note)) switch
    {
        (true, false) => $"{item.Description} : {item.Note} (คืนสินค้า)",
        (true, true) => $"{item.Description} : (คืนสินค้า)",
        (false, false) => $"{item.Description} : {item.Note}",
        _ => item.Description
    };

    private static string PaymentDescription(ReceiptPayment payment, bool isRefund)
    {
        if (isRefund)
            return $"{payment.DisplayName} : คืนเงิน";

        return string.IsNullOrEmpty(payment.Note) ? payment.DisplayName : $"{payment.DisplayName} : {payment.Note}";
    }

    private static ReceiptRow Text(string text, string? amount = null) => new(ReceiptRowStyle.Text, text, amount);
}
```

Note on the bill number: the plan deliberately formats it with the invariant culture, because a culture with native digits would otherwise change it. Every other value uses `culture`.

- [ ] **Step 5: Run the tests to see them pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ReceiptLayoutTests"`
Expected: PASS, 17 tests. If `Rows_OnAThaiTill_PrintsTheBuddhistYear` fails, print the actual date line and check that the machine's th-TH culture uses `ThaiBuddhistCalendar` (it does on Windows 11); do not weaken the assertion.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/Common/Receipts tests/IndyPOS.Application.Tests/Common/Receipts
git commit -m "feat(till): ReceiptDocument and a pure ReceiptLayout of every printed row"
```

---

### Task 2: `ReceiptDocumentFactory`

**Files:**
- Create: `src/IndyPOS.Application/Common/Receipts/ReceiptDocumentFactory.cs`
- Test: `tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptDocumentFactoryTests.cs`

**Interfaces:**
- Consumes: Task 1's records; `CompleteSaleResponse(Guid InvoiceId, decimal TotalAmount, DateTime CreatedUtc, long InvoiceNumber)` (`IndyPOS.Application.UseCases.StoreHub.Sales`); `InvoiceReprintResultDto(InvoiceDetailDto Sale, InvoiceReprintDto Reprint)` (`...Sales.Reprints`); `InvoiceDetailDto`, `InvoiceLineDto`, `PaymentDto` (`...Sales.History`).
- Produces:
  - `record CompletedSale(string CashierName, IReadOnlyList<ReceiptItem> Items, IReadOnlyList<ReceiptPayment> Payments, decimal Change)`
  - `sealed class ReceiptDocumentFactory(TimeZoneInfo storeTimeZone)` with `ReceiptDocument ForSale(CompleteSaleResponse response, CompletedSale sale)` and `ReceiptDocument ForReprint(InvoiceReprintResultDto result, string reprintedBy)`
  - `const string UnknownCashier = "-"`

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptDocumentFactoryTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Receipts;

public class ReceiptDocumentFactoryTests
{
    // 23:30 UTC on the 14th is 06:30 on the 15th in Bangkok (UTC+7, no daylight saving).
    private static readonly DateTime LateEveningUtc = new(2026, 10, 14, 23, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime NextMorningInBangkok = new(2026, 10, 15, 6, 30, 0);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private const string NoCashierName = null!;

    private readonly ReceiptDocumentFactory _factory = new(Bangkok);

    [Fact]
    public void ForReprint_WithNoCashierName_PrintsADash()
    {
        var result = Reprint() with { Sale = Detail() with { CashierName = NoCashierName } };

        var document = _factory.ForReprint(result, "Somchai Manager");

        document.CashierName.Should()
                            .Be(ReceiptDocumentFactory.UnknownCashier);
    }

    [Fact]
    public void ForSale_WithASaleBefore7amBangkok_UsesTheBangkokDate()
    {
        var document = _factory.ForSale(Response() with { CreatedUtc = LateEveningUtc }, Sale());

        document.IssuedAt.Should()
                         .Be(NextMorningInBangkok);
    }

    [Fact]
    public void ForSale_WithANegativeTotal_MarksTheReceiptARefund()
    {
        var document = _factory.ForSale(Response() with { TotalAmount = -250m }, Sale());

        document.IsRefund.Should()
                         .BeTrue();
    }

    [Fact]
    public void ForSale_WithAResponse_UsesItsBillNumber()
    {
        var document = _factory.ForSale(Response(), Sale());

        document.InvoiceNumber.Should()
                              .Be(1042);
    }

    [Fact]
    public void ForSale_WithASale_IsNotACopy()
    {
        var document = _factory.ForSale(Response(), Sale());

        document.IsCopy.Should()
                       .BeFalse();
    }

    [Fact]
    public void ForSale_WithTwoPayments_ReceivesTheirSum()
    {
        var sale = Sale() with { Payments = [new ReceiptPayment("เงินสด", 300m, null), new ReceiptPayment("โอนเงิน", 200m, null)] };

        var document = _factory.ForSale(Response(), sale);

        document.AmountReceived.Should()
                               .Be(500m);
    }

    [Fact]
    public void ForSale_WithASale_KeepsTheSalesCashierItemsAndChange()
    {
        var sale = Sale();

        var document = _factory.ForSale(Response(), sale);

        document.Should()
                .BeEquivalentTo(new { sale.CashierName, sale.Items, sale.Payments, sale.Change, Total = 100m });
    }

    [Fact]
    public void ForReprint_WithAResult_IsACopyByTheReprinter()
    {
        var document = _factory.ForReprint(Reprint(), "Somchai Manager");

        document.Reprint.Should()
                        .Be(new ReceiptReprint(NextMorningInBangkok, "Somchai Manager"));
    }

    [Fact]
    public void ForReprint_WithAResult_KeepsTheOriginalCashier()
    {
        var document = _factory.ForReprint(Reprint(), "Somchai Manager");

        document.CashierName.Should()
                            .Be("Malee Cashier");
    }

    [Fact]
    public void ForReprint_WithAResult_UsesTheBillsNumberTimeAndAmounts()
    {
        var document = _factory.ForReprint(Reprint(), "Somchai Manager");

        document.Should()
                .BeEquivalentTo(new
                {
                    InvoiceNumber = 1042L,
                    IssuedAt = NextMorningInBangkok,
                    Total = 100m,
                    AmountReceived = 500m,
                    Change = 400m,
                    IsRefund = false
                });
    }

    [Fact]
    public void ForReprint_WithALine_PrintsItsNameNoteAndTotal()
    {
        var document = _factory.ForReprint(Reprint(), "Somchai Manager");

        document.Items.Should()
                      .Equal(new ReceiptItem("Nails 1kg", "loose", 2, 50m, 100m));
    }

    [Fact]
    public void ForReprint_WithAPayment_PrintsItsDisplayName()
    {
        var document = _factory.ForReprint(Reprint(), "Somchai Manager");

        document.Payments.Should()
                         .Equal(new ReceiptPayment("เงินสด", 500m, null));
    }

    private static CompleteSaleResponse Response() => new(Guid.NewGuid(), 100m, LateEveningUtc, 1042);

    private static CompletedSale Sale() => new(
        CashierName: "Malee Cashier",
        Items: [new ReceiptItem("Nails 1kg", null, 2, 50m, 100m)],
        Payments: [new ReceiptPayment("เงินสด", 500m, null)],
        Change: 400m);

    private static InvoiceDetailDto Detail() => new(
        Id: Guid.NewGuid(),
        InvoiceNumber: 1042,
        StoreId: "store-a",
        UserId: Guid.NewGuid(),
        CashierName: "Malee Cashier",
        TotalAmount: 100m,
        AmountReceived: 500m,
        ChangeGiven: 400m,
        IsRefund: false,
        HasPayLater: false,
        PayLaterAmount: 0m,
        CreatedUtc: LateEveningUtc,
        Lines: [new InvoiceLineDto(Guid.NewGuid(), Guid.NewGuid(), "Nails 1kg", "8850000000001", "loose", null, 2, 50m, 100m)],
        Payments: [new PaymentDto(Guid.NewGuid(), "Cash", "เงินสด", 500m, null)]);

    private static InvoiceReprintResultDto Reprint()
    {
        var detail = Detail();

        return new InvoiceReprintResultDto(detail, new InvoiceReprintDto(Guid.NewGuid(), detail.Id, LateEveningUtc, Guid.NewGuid()));
    }
}
```

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ReceiptDocumentFactoryTests"`
Expected: build FAILS with `CS0246: The type or namespace name 'ReceiptDocumentFactory' could not be found` (and `CompletedSale`).

- [ ] **Step 3: Write the factory**

`src/IndyPOS.Application/Common/Receipts/ReceiptDocumentFactory.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

namespace IndyPOS.Application.Common.Receipts;

/// <summary>What the till itself knows about a sale it has just completed.</summary>
public sealed record CompletedSale(
    string CashierName,
    IReadOnlyList<ReceiptItem> Items,
    IReadOnlyList<ReceiptPayment> Payments,
    decimal Change);

/// <summary>
/// Builds the printer's input in the two places a receipt is printed: right after a sale, and on a
/// reprint. Server times are UTC; the document holds them in the store's time zone.
/// </summary>
public sealed class ReceiptDocumentFactory(TimeZoneInfo storeTimeZone)
{
    /// <summary>Printed when the server no longer knows the bill's cashier.</summary>
    public const string UnknownCashier = "-";

    public ReceiptDocument ForSale(CompleteSaleResponse response, CompletedSale sale) => new(
        InvoiceNumber: response.InvoiceNumber,
        IssuedAt: ToStoreTime(response.CreatedUtc),
        CashierName: sale.CashierName,
        Items: sale.Items,
        Payments: sale.Payments,
        Total: response.TotalAmount,
        AmountReceived: sale.Payments.Sum(p => p.Amount),
        Change: sale.Change,
        IsRefund: response.TotalAmount < 0);

    public ReceiptDocument ForReprint(InvoiceReprintResultDto result, string reprintedBy)
    {
        var bill = result.Sale;

        return new ReceiptDocument(
            InvoiceNumber: bill.InvoiceNumber,
            IssuedAt: ToStoreTime(bill.CreatedUtc),
            CashierName: bill.CashierName ?? UnknownCashier,
            Items: bill.Lines.Select(ToItem).ToList(),
            Payments: bill.Payments.Select(ToPayment).ToList(),
            Total: bill.TotalAmount,
            AmountReceived: bill.AmountReceived,
            Change: bill.ChangeGiven,
            IsRefund: bill.IsRefund,
            Reprint: new ReceiptReprint(ToStoreTime(result.Reprint.CreatedUtc), reprintedBy));
    }

    private static ReceiptItem ToItem(InvoiceLineDto line) =>
        new(line.ProductName, line.Note, line.Quantity, line.UnitPrice, line.LineTotal);

    private static ReceiptPayment ToPayment(PaymentDto payment) =>
        new(payment.MethodDisplayName, payment.Amount, payment.Note);

    // JSON leaves the Kind Unspecified; the server always sends UTC.
    private DateTime ToStoreTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), storeTimeZone);
}
```

- [ ] **Step 4: Run the tests to see them pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ReceiptDocumentFactoryTests"`
Expected: PASS, 12 tests.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/Common/Receipts/ReceiptDocumentFactory.cs tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptDocumentFactoryTests.cs
git commit -m "feat(till): ReceiptDocumentFactory builds a receipt after a sale and for a reprint"
```

---

### Task 3: One shared payment-name lookup

`SalePanel.EnsurePaymentMethodNamesLoadedAsync` (`src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs:244`) loads code → display name from `GetOfferablePaymentMethodsAsync`. The sale service now needs the same map for the receipt, which is the second copy, so it is extracted. Unlike today, a failed load is **not** cached: today one StoreHub blip at the first payment leaves raw codes on screen until the till restarts.

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/IPaymentMethodNames.cs`
- Create: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubPaymentMethodNames.cs`
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (inside `AddStoreHubClientServices`, after the `ISaleService` line, about line 210)
- Modify: `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs:27,64-65,81,244-263`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubPaymentMethodNamesTests.cs`

**Interfaces:**
- Consumes: `IStoreHubClient.GetOfferablePaymentMethodsAsync(CancellationToken)` → `IReadOnlyList<PaymentMethodDto>`; `PaymentMethodDto(string Code, string DisplayName, PaymentMethodKind Kind, bool IsEnabled, int DisplayOrder)`.
- Produces: `interface IPaymentMethodNames { Task<IReadOnlyDictionary<string, string>> GetAsync(CancellationToken cancellationToken = default); }` — keys are case-insensitive; an unknown code is simply absent, so callers use `GetValueOrDefault(code, code)`.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubPaymentMethodNamesTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class StoreHubPaymentMethodNamesTests
{
    private static readonly PaymentMethodDto Cash = new("Cash", "เงินสด", PaymentMethodKind.Standard, true, 1);

    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public async Task GetAsync_WhenTheCatalogFailsToLoad_ReturnsAnEmptyMap()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubClientException("Cannot connect to StoreHub"));

        var names = await Names().GetAsync();

        names.Should()
             .BeEmpty();
    }

    [Fact]
    public async Task GetAsync_AfterAFailedLoad_TriesAgain()
    {
        _client.SetupSequence(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubClientException("Cannot connect to StoreHub"))
               .ReturnsAsync([Cash]);
        var sut = Names();
        await sut.GetAsync();

        var names = await sut.GetAsync();

        names.Should()
             .ContainKey("Cash");
    }

    [Fact]
    public async Task GetAsync_WhenCancelled_Throws()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new OperationCanceledException());

        var act = () => Names().GetAsync();

        await act.Should()
                 .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAsync_WithADifferentlyCasedCode_FindsTheName()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([Cash]);

        var names = await Names().GetAsync();

        names["CASH"].Should()
                     .Be("เงินสด");
    }

    [Fact]
    public async Task GetAsync_AfterASuccessfulLoad_DoesNotCallStoreHubAgain()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([Cash]);
        var sut = Names();
        await sut.GetAsync();

        await sut.GetAsync();

        _client.Verify(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private StoreHubPaymentMethodNames Names() =>
        new(_client.Object, NullLogger<StoreHubPaymentMethodNames>.Instance);
}
```

`StoreHubClientException` lives in `IndyPOS.Infrastructure.Services.StoreHub`, already imported.

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubPaymentMethodNamesTests"`
Expected: build FAILS with `CS0246: ... 'StoreHubPaymentMethodNames' could not be found`.

- [ ] **Step 3: Write the interface and the implementation**

`src/IndyPOS.Application/Abstractions/StoreHub/IPaymentMethodNames.cs`:

```csharp
namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>
/// The catalog's display name for each payment-method code, e.g. "Cash" → "เงินสด". Keys ignore
/// case. A code the catalog does not know is absent, so callers fall back to the code itself
/// (<c>names.GetValueOrDefault(code, code)</c>) and never index a payment by it.
/// </summary>
public interface IPaymentMethodNames
{
    Task<IReadOnlyDictionary<string, string>> GetAsync(CancellationToken cancellationToken = default);
}
```

`src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubPaymentMethodNames.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Loads the offerable catalog (GET /payment-methods) once. Cashiers cannot read the full catalog,
/// and every payment the till takes comes from an offerable button, so this covers every code a
/// sale holds. A failed load is not cached: the next call tries again.
/// </summary>
public sealed class StoreHubPaymentMethodNames(IStoreHubClient storeHubClient, ILogger<StoreHubPaymentMethodNames> logger)
    : IPaymentMethodNames
{
    private static readonly IReadOnlyDictionary<string, string> NoNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyDictionary<string, string>? _names;

    public async Task<IReadOnlyDictionary<string, string>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_names is not null)
            return _names;

        try
        {
            var methods = await storeHubClient.GetOfferablePaymentMethodsAsync(cancellationToken);

            return _names = methods.ToDictionary(m => m.Code, m => m.DisplayName, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Payment-method names could not be loaded; showing raw codes until the next try");

            return NoNames;
        }
    }
}
```

In `src/IndyPOS.Infrastructure/ConfigureServices.cs`, directly after `services.AddSingleton<ISaleService, StoreHubSaleService>();`:

```csharp
		services.AddSingleton<IPaymentMethodNames, StoreHubPaymentMethodNames>();
```

- [ ] **Step 4: Run the tests to see them pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubPaymentMethodNamesTests"`
Expected: PASS, 5 tests.

- [ ] **Step 5: Move `SalePanel` onto the provider**

In `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs`:
- Replace the field `private readonly IStoreHubClient _storeHubClient;` (line 27) with `private readonly IPaymentMethodNames _paymentMethodNames;`.
- Replace the constructor parameter `IStoreHubClient storeHubClient,` with `IPaymentMethodNames paymentMethodNames,`, and `_storeHubClient = storeHubClient;` with `_paymentMethodNames = paymentMethodNames;`. (`_storeHubClient` has no other use in the file; confirm with `grep -n "_storeHubClient" src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs` returning nothing after the edit.)
- Replace the whole `EnsurePaymentMethodNamesLoadedAsync` method (lines 244–263) with this (the map field keeps its name, `_paymentMethodNamesByCode`):

```csharp
    private async Task EnsurePaymentMethodNamesLoadedAsync()
    {
        // The provider caches a good load and retries a failed one, so asking every time is cheap.
        _paymentMethodNamesByCode = await _paymentMethodNames.GetAsync();
    }
```

`ResolvePaymentTypeDisplay` (line 235) is unchanged: it already does `GetValueOrDefault(payment.Method, payment.Method)`.

- [ ] **Step 6: Build and run the Windows.Forms suite**

Run: `dotnet build src/IndyPOS.Windows.Forms` then `dotnet test tests/IndyPOS.Windows.Forms.Tests`
Expected: build succeeds; 88 pass. (`SalePanel` is built by DI only; no test constructs it.) Then run `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~TillHostTests"` once more on its own: it resolves the till's container, so it proves the new registration is there.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/IPaymentMethodNames.cs src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubPaymentMethodNames.cs src/IndyPOS.Infrastructure/ConfigureServices.cs src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubPaymentMethodNamesTests.cs
git commit -m "refactor(till): one shared payment-method name lookup, which retries a failed load"
```

---

### Task 4: The sale's receipt is a `ReceiptDocument`, end to end (fixes `0000000000` and the RELEASE crash)

This task changes three signatures that must move together, or the till does not build: `ISaleService.CompleteSaleAsync`, `PrintReceiptForm.ShowDialog` and `IReceiptPrinterService.PrintReceipt`.

**Files:**
- Create: `src/IndyPOS.Infrastructure/Services/ReceiptRenderer.cs`
- Modify: `src/IndyPOS.Application/Common/Interfaces/ISaleService.cs:16`
- Modify: `src/IndyPOS.Application/Common/Interfaces/IReceiptPrinterService.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubSaleService.cs` (constructor, `CompleteSaleAsync` 295–320, `CreateInvoiceInfo` 340–355)
- Modify: `src/IndyPOS.Infrastructure/Services/ReceiptPrinterService.cs` (whole file)
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (register the factory)
- Modify: `src/IndyPOS.Windows.Forms/UI/Sale/PrintReceiptForm.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/SalesHistoryReportPanel.cs` (+ `.Designer.cs:616`) and `tests/IndyPOS.Windows.Forms.Tests/UI/Report/ReportPanelsLayoutTests.cs:87`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubSaleServiceTests.cs`, `tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptRendererTests.cs`

**Interfaces:**
- Consumes: Task 1 (`ReceiptLayout.Rows`, records), Task 2 (`ReceiptDocumentFactory`, `CompletedSale`), Task 3 (`IPaymentMethodNames`).
- Produces:
  - `Task<ReceiptDocument> ISaleService.CompleteSaleAsync()`
  - `void IReceiptPrinterService.PrintReceipt(ReceiptDocument document)`
  - `static class ReceiptRenderer { static int Draw(Graphics graphics, IReadOnlyList<ReceiptRow> rows) }` — returns the y after the last row.
  - `StoreHubSaleService(IStoreHubClient, IProductCacheService, IEventAggregator, IPaymentMethodNames, ReceiptDocumentFactory, ILogger<StoreHubSaleService>)` — six parameters, all DI-injected; the ≤3 guideline is for methods, and this is the existing constructor plus two.

- [ ] **Step 1: Write the failing sale-service tests**

`tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubSaleServiceTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>
/// v4 receipts printed bill 0000000000 (the legacy Id was set to 0 and the response's number
/// ignored), and in RELEASE threw KeyNotFoundException on every payment (the printer indexed the
/// legacy 1–7 payment dictionary with PaymentTypeId, which v4 payments leave at 0).
/// </summary>
public class StoreHubSaleServiceTests
{
    private const long BillNumber = 1042;
    private const string UnknownCode = "NewCampaign2027";
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<IEventAggregator> _events = new();
    private readonly Mock<IPaymentMethodNames> _names = new();
    private readonly UserLoggedInEvent _loggedIn = new();
    private readonly StoreHubSaleService _sut;

    public StoreHubSaleServiceTests()
    {
        _events.Setup(e => e.GetEvent<UserLoggedInEvent>()).Returns(_loggedIn);
        _events.Setup(e => e.GetEvent<UserLoggedOutEvent>()).Returns(new UserLoggedOutEvent());
        _events.Setup(e => e.GetEvent<InvoiceProductAddedEvent>()).Returns(new InvoiceProductAddedEvent());
        _events.Setup(e => e.GetEvent<InvoicePaymentAddedEvent>()).Returns(new InvoicePaymentAddedEvent());
        _client.SetupGet(c => c.IsAuthenticated).Returns(true);
        _client.Setup(c => c.CompleteSaleAsync(It.IsAny<CompleteSaleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new CompleteSaleResponse(Guid.NewGuid(), 100m, DateTime.UtcNow, BillNumber));
        _names.Setup(n => n.GetAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Cash"] = "เงินสด" });

        _sut = new StoreHubSaleService(_client.Object, Mock.Of<IProductCacheService>(), _events.Object,
                                       _names.Object, new ReceiptDocumentFactory(Bangkok),
                                       NullLogger<StoreHubSaleService>.Instance);
        _loggedIn.Publish(Cashier());
    }

    [Fact]
    public async Task CompleteSaleAsync_WithAPaymentCodeNotInTheCatalog_PrintsTheCode()
    {
        RingUpOneProductPaidWith(UnknownCode);

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Payments.Single().DisplayName.Should()
                                             .Be(UnknownCode);
    }

    [Fact]
    public async Task CompleteSaleAsync_WithAResponse_PrintsItsBillNumber()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.InvoiceNumber.Should()
                             .Be(BillNumber);
    }

    [Fact]
    public async Task CompleteSaleAsync_WithACatalogPayment_PrintsItsDisplayName()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Payments.Single().DisplayName.Should()
                                             .Be("เงินสด");
    }

    [Fact]
    public async Task CompleteSaleAsync_WithALoggedInCashier_PrintsTheirName()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.CashierName.Should()
                           .Be("Malee Cashier");
    }

    [Fact]
    public async Task CompleteSaleAsync_WithOverpayment_PrintsTheChange()
    {
        RingUpOneProductPaidWith("Cash", amount: 500m);

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Change.Should()
                      .Be(400m);
    }

    [Fact]
    public async Task CompleteSaleAsync_WithAProduct_PrintsItAsAnItem()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Items.Should()
                     .Equal(new ReceiptItem("Nails 1kg", null, 1, 100m, 100m));
    }

    private void RingUpOneProductPaidWith(string methodCode, decimal amount = 100m)
    {
        _sut.AddProduct(new InventoryProductDto { Id = Guid.NewGuid(), Description = "Nails 1kg", UnitPrice = 100m });
        _sut.AddPayment(methodCode, amount, string.Empty);
    }

    private static ILoggedInUser Cashier() =>
        Mock.Of<ILoggedInUser>(u => u.UserId == Guid.NewGuid() && u.FirstName == "Malee" && u.LastName == "Cashier");
}
```

Before running, check the `GetEvent<...>` list against what `AddProduct(InventoryProductDto)` and `AddPayment` publish (`grep -n "GetEvent<" src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubSaleService.cs`). A missing setup makes Moq return null and the test throws `NullReferenceException` in arrange, which is the wrong failure. Add any missing event, and fix the `using` for `IEventAggregator` and the event classes (they live where `UserLoggedInEvent` does: `src/IndyPOS.Application/Events`).

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubSaleServiceTests"`
Expected: build FAILS: the constructor takes no `IPaymentMethodNames`/`ReceiptDocumentFactory`, and `CompleteSaleAsync` returns `IInvoiceInfo`, which has no `InvoiceNumber`, `CashierName` or `Items`. That is the defect itself: there is nowhere for the bill number to go.

- [ ] **Step 3: Change the two interfaces**

`src/IndyPOS.Application/Common/Interfaces/ISaleService.cs`, line 16 becomes:

```csharp
	Task<ReceiptDocument> CompleteSaleAsync();
```

and add `using IndyPOS.Application.Common.Receipts;` at the top.

`src/IndyPOS.Application/Common/Interfaces/IReceiptPrinterService.cs` becomes:

```csharp
using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Application.Common.Interfaces;

public interface IReceiptPrinterService
{
    void PrintReceipt(ReceiptDocument document);
}
```

- [ ] **Step 4: Make the sale service return the document**

In `StoreHubSaleService.cs`:
- Add `using IndyPOS.Application.Common.Receipts;`.
- Add fields `private readonly IPaymentMethodNames _paymentMethodNames;` and `private readonly ReceiptDocumentFactory _receiptFactory;`, and the two constructor parameters between `IEventAggregator eventAggregator` and `ILogger<StoreHubSaleService> logger`, assigned in the constructor.
- `CompleteSaleAsync` returns `Task<ReceiptDocument>`; replace its last three statements (`// Create invoice info...`, `var invoiceInfo = CreateInvoiceInfo(response);`, `return invoiceInfo;`) with:

```csharp
        return _receiptFactory.ForSale(response, await CreateCompletedSaleAsync());
```

- Replace `CreateInvoiceInfo` (lines 340–355) with:

```csharp
    private async Task<CompletedSale> CreateCompletedSaleAsync()
    {
        var names = await _paymentMethodNames.GetAsync();

        var items = Products.Select(p => new ReceiptItem(
            p.Description, p.Note.HasValue() ? p.Note : null, p.Quantity, p.UnitPrice, p.GetTotal())).ToList();

        // Every v4 payment carries a catalog code; an unknown one prints as the code, never throws.
        var payments = Payments.Select(p => new ReceiptPayment(
            names.GetValueOrDefault(p.Method ?? string.Empty, p.Method ?? string.Empty),
            p.Amount,
            p.Note.HasValue() ? p.Note : null)).ToList();

        return new CompletedSale(CashierName(), items, payments, CalculateChanges());
    }

    // CompleteSaleAsync has already refused a sale with no logged-in user.
    private string CashierName() => $"{_loggedInUser!.FirstName} {_loggedInUser.LastName}";
```

`CompleteSaleAsync` reads `CreateCompletedSaleAsync()` before returning, while `Products` and `Payments` still hold this sale: `SalePanel` calls `StartNewSale` only after the receipt dialog closes, and `StartNewSale` replaces the lists rather than clearing them.

In `ConfigureServices.AddStoreHubClientServices`, before the `ISaleService` line:

```csharp
		services.AddSingleton(sp => new ReceiptDocumentFactory(sp.GetRequiredService<IStoreIdentityService>().TimeZone));
```

(`IStoreIdentityService` is registered by `AddInfrastructureServices`, which `TillHost` calls first. Its `TimeZone` is lazy and defaults to `SE Asia Standard Time`.)

- [ ] **Step 5: Run the sale-service tests to see them pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubSaleServiceTests"`
Expected: the test project may not build yet because `ReceiptPrinterService` still prints `IInvoiceInfo`. Do Steps 6–8 first if so, then run: PASS, 6 tests.

- [ ] **Step 6: Write the renderer and its failing test**

`tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptRendererTests.cs`:

```csharp
using System.Drawing;
using System.Globalization;
using FluentAssertions;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Infrastructure.Services;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Receipts;

public class ReceiptRendererTests
{
    private const int TopOfPage = 5;

    [Fact]
    public void Draw_WithNoRows_LeavesThePositionAtTheTop()
    {
        using var bitmap = new Bitmap(200, 100);
        using var graphics = Graphics.FromImage(bitmap);

        var bottom = ReceiptRenderer.Draw(graphics, []);

        bottom.Should()
              .Be(TopOfPage);
    }

    [Fact]
    public void Draw_WithACopyReceipt_MovesDownForEveryRow()
    {
        var rows = ReceiptLayout.Rows(new ReceiptHeader("Store", "a", "b", "c"), CopyReceipt(), CultureInfo.InvariantCulture);
        using var bitmap = new Bitmap(200, 1000);
        using var graphics = Graphics.FromImage(bitmap);

        var bottom = ReceiptRenderer.Draw(graphics, rows);

        bottom.Should()
              .BeGreaterThan(TopOfPage + rows.Count * 5);
    }

    private static ReceiptDocument CopyReceipt() => new(
        42, new DateTime(2026, 10, 15, 14, 30, 0), "Malee Cashier",
        [new ReceiptItem("Nails 1kg", null, 2, 50m, 100m)],
        [new ReceiptPayment("เงินสด", 500m, null)],
        100m, 500m, 400m, false,
        new ReceiptReprint(new DateTime(2026, 10, 16, 9, 5, 0), "Somchai Manager"));
}
```

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ReceiptRendererTests"`
Expected: build FAILS with `'ReceiptRenderer' does not exist`.

`src/IndyPOS.Infrastructure/Services/ReceiptRenderer.cs`:

```csharp
using System.Runtime.Versioning;
using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// Draws receipt rows on a 58 mm roll. Only positions and fonts live here; every word comes from
/// <see cref="ReceiptLayout"/>. Spacing matches the v3 printer's.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ReceiptRenderer
{
    private const int TopOfPage = 5;
    private const int RowHeight = 11;
    private const int StoreNameHeight = 30;
    private const int BannerHeight = 24;
    private const int StoreNameX = 60;
    private const int IndentX = 10;
    private const int PriceColumn = 135;

    private const string FontFamilyName = "FC Subject [Non-commercial] Reg";
    private const string RuleText = "-------------------------------------------------------";

    private static readonly Font BannerFont = new(FontFamilyName, 13, FontStyle.Bold);
    private static readonly Font StoreNameFont = new(FontFamilyName, 15);
    private static readonly Font TextFont = new(FontFamilyName, 7);
    private static readonly Font RuleFont = new(FontFamilyName, 5);

    /// <returns>The y position below the last row.</returns>
    public static int Draw(Graphics graphics, IReadOnlyList<ReceiptRow> rows)
    {
        var y = TopOfPage;

        foreach (var row in rows)
            y = DrawRow(graphics, row, y);

        return y;
    }

    private static int DrawRow(Graphics graphics, ReceiptRow row, int y)
    {
        switch (row.Style)
        {
            case ReceiptRowStyle.Banner:
                graphics.DrawString(row.Text, BannerFont, Brushes.Black, 0, y);
                return y + BannerHeight;
            case ReceiptRowStyle.StoreName:
                graphics.DrawString(row.Text, StoreNameFont, Brushes.Black, StoreNameX, y);
                return y + StoreNameHeight;
            case ReceiptRowStyle.Rule:
                graphics.DrawString(RuleText, RuleFont, Brushes.Black, 0, y);
                return y + RowHeight;
            default:
                DrawText(graphics, row, y);
                return y + RowHeight;
        }
    }

    private static void DrawText(Graphics graphics, ReceiptRow row, int y)
    {
        var x = row.Style == ReceiptRowStyle.Indented ? IndentX : 0;
        graphics.DrawString(row.Text, TextFont, Brushes.Black, x, y);

        if (row.Amount is not null)
            graphics.DrawString(row.Amount, TextFont, Brushes.Black, PriceColumn, y);
    }
}
```

- [ ] **Step 7: Make the printer print the document**

Replace `src/IndyPOS.Infrastructure/Services/ReceiptPrinterService.cs` with:

```csharp
using System.Drawing.Printing;
using System.Globalization;
using System.Runtime.Versioning;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// Prints a <see cref="ReceiptDocument"/>. It no longer reads the logged-in user or the legacy
/// payment dictionary: the document carries the bill's own cashier and each payment's display name.
/// </summary>
[type:SupportedOSPlatform("windows")]
public class ReceiptPrinterService : IReceiptPrinterService
{
	private readonly ILogger<ReceiptPrinterService> _logger;
	private readonly PrintDocument _printDocument;
	private ReceiptHeader _header = new(string.Empty, string.Empty, string.Empty, string.Empty);
	private string _printerName = "XP-58";
	private ReceiptDocument? _document;

	public ReceiptPrinterService(IStoreConfigurationService storeConfigurationService,
								 ILogger<ReceiptPrinterService> logger)
	{
		_logger = logger;

		GetStoreConfiguration(storeConfigurationService);

		_printDocument = new PrintDocument();
		_printDocument.PrinterSettings.PrinterName = _printerName;
		_printDocument.PrintPage += PrintPageHandler;
	}

	private void GetStoreConfiguration(IStoreConfigurationService storeConfigurationService)
	{
		try
		{
			var config = storeConfigurationService.Get();

			_printerName = config.PrinterName ?? "XP-58";
			_header = new ReceiptHeader(config.StoreName ?? string.Empty,
										config.StoreAddressLine1 ?? string.Empty,
										config.StoreAddressLine2 ?? string.Empty,
										config.StorePhoneNumber ?? string.Empty);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to get store configuration for a receipt printer!");
			throw;
		}
	}

	public void PrintReceipt(ReceiptDocument document)
	{
		_document = document;

		_printDocument.Print();
	}

	private void PrintPageHandler(object sender, PrintPageEventArgs e)
	{
		if (_document is null || e.Graphics is null)
			throw new InvalidOperationException("There is no receipt to print.");

		ReceiptRenderer.Draw(e.Graphics, ReceiptLayout.Rows(_header, _document, CultureInfo.CurrentCulture));
	}
}
```

The constructor loses `IStoreConstants` and `IEventAggregator`. It is resolved by DI only (`ConfigureServices.cs:41`), so no call site changes.

- [ ] **Step 8: Pass the document through the till**

`src/IndyPOS.Windows.Forms/UI/Sale/PrintReceiptForm.cs`: change `using IndyPOS.Application.Common.Interfaces;` to also import `IndyPOS.Application.Common.Receipts`, and replace every `IInvoiceInfo` with `ReceiptDocument` and `_invoiceInfo`/`invoiceInfo` with `_document`/`document` (the field, `ShowDialog(ReceiptDocument document)`, and `PrintReceipt(ReceiptDocument document)`). `SalePanel.cs:337-339` needs no change: `var invoiceInfo = await _saleService.CompleteSaleAsync();` now holds a `ReceiptDocument`; rename the local to `receipt` for honesty.

`SalesHistoryReportPanel` printed through `GetInvoiceInfoAsync(int)`, a stub that always returned an empty bill 0. Plan C rebuilds this panel around `InvoiceDetailView`. Until then its print button is hidden rather than left printing a blank receipt:
- In `SalesHistoryReportPanel.cs`, delete `PrintReceiptButton_Click`, `PrintReceipt(IInvoiceInfo)`, `GetInvoiceInfoAsync(int)`, the `_receiptPrinterService` field and the `IReceiptPrinterService receiptPrinterService` constructor parameter and its assignment. Add as the constructor's last line:

```csharp
        // Reprint returns with plan C's InvoiceDetailView; the old path printed an empty stub bill.
        PrintReceiptButton.Visible = false;
```

- In `SalesHistoryReportPanel.Designer.cs`, delete line 616: `PrintReceiptButton.Click += PrintReceiptButton_Click;`.
- In `tests/IndyPOS.Windows.Forms.Tests/UI/Report/ReportPanelsLayoutTests.cs:87`, drop the `Mock.Of<IReceiptPrinterService>(), ` argument.
- If `System.Diagnostics` is now unused in the panel, remove that `using`.

- [ ] **Step 9: Build everything and run the affected suites**

Run:
```bash
dotnet build
dotnet test tests/IndyPOS.Application.Tests
dotnet test tests/IndyPOS.Windows.Forms.Tests
```
Expected: build succeeds with no new warnings; Application 644 + 17 + 12 + 5 + 6 + 2 = **686** pass; Windows.Forms **88** pass. If the Application count differs, recount from `--list-tests` rather than adjusting the expectation.

- [ ] **Step 10: See the receipt**

The layout tests prove the words; this proves the drawing. Add a throwaway test (do **not** commit it) to `ReceiptRendererTests` that draws `ReceiptLayout.Rows(...)` of a copy receipt with a th-TH culture to a 220×700 white `Bitmap` and saves it to the session scratchpad as `receipt-copy.png`. Run it, then look at the PNG with the glimpse skill. Check that the banner is at the top, the store name sits where v3 put it, the amounts line up in the price column, and nothing overlaps. Fix spacing constants in `ReceiptRenderer` if needed, then delete the throwaway test.

- [ ] **Step 11: Commit**

```bash
git add -A
git commit -m "fix(till): a v4 receipt prints its bill number, sale time and cashier, and cannot crash on a payment

The receipt printed Invoice No. 0000000000: the legacy Id was set to 0 and
the response's InvoiceNumber ignored. In RELEASE it would also have thrown
KeyNotFoundException on every payment, because the printer indexed the
legacy 1-7 payment dictionary with PaymentTypeId, which v4 payments leave
at 0. The sale now returns a ReceiptDocument built by ReceiptDocumentFactory,
with the payment's catalog display name, and the printer draws
ReceiptLayout's rows. The sales-history print button is hidden until plan C,
because it printed an empty stub bill."
```

---

### Task 5: Delete what the printer no longer reads

**Files:**
- Delete: `src/IndyPOS.Application/Common/Interfaces/IInvoiceInfo.cs`
- Modify: `src/IndyPOS.Application/Common/Models/LegacyDtos.cs:82-97` (delete `InvoiceInfo`)
- Modify: `src/IndyPOS.Application/Common/Interfaces/IReportService.cs:26` (delete `GetInvoiceInfoAsync`)
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubReportService.cs:80-105` (delete `GetInvoiceInfoAsync` and `StubInvoiceInfo`)

Spec §7 lists `GetInvoiceInfoAsync` and `StubInvoiceInfo` for removal in plan 3, and §9 lists `IInvoiceInfo`/`InvoiceInfo` among the legacy DTOs to delete. After Task 4 nothing reads them.

- [ ] **Step 1: Prove they are dead**

Run: `grep -rn "IInvoiceInfo\|InvoiceInfo\b\|GetInvoiceInfoAsync\|StubInvoiceInfo" src tests --include=*.cs`
Expected: hits only in the four definitions above. Any other hit is a caller Task 4 missed: fix that first.

- [ ] **Step 2: Delete them**

Delete the file and the three members listed above, including `StubInvoiceInfo`'s whole nested class.

- [ ] **Step 3: Build and test**

Run: `dotnet build` then `dotnet test tests/IndyPOS.Application.Tests` and `dotnet test tests/IndyPOS.Windows.Forms.Tests`
Expected: build succeeds; 686 and 88 pass, unchanged from Task 4.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(till): delete IInvoiceInfo and the GetInvoiceInfoAsync stub the printer no longer reads"
```

---

### Task 6: Full run, docs, PR

- [ ] **Step 1: Run the whole solution with Docker up**

Run: `dotnet test --logger trx --results-directory <scratchpad>/trx-plan-a`
Expected: every suite green. Record the per-suite counts; the TRX catches any flake's message.

- [ ] **Step 2: Update the measured counts**

In `CLAUDE.md` (Quick Commands paragraph after the code block) and `ONBOARDING.md`'s per-suite breakdown: Application 641 → its measured value, Windows.Forms 79 → 88 (PR #125, not yet recorded), and the solution total to the measured value, appending one sentence in the existing style naming PR #125's +9 Windows.Forms, PR #126's +3 Application and +3 CloudApi.IntegrationTests, and this PR's Application additions. Docker-down counts: the CloudApi.IntegrationTests line goes 58 → 61 (#126), derived.

- [ ] **Step 3: Commit, push, open the PR**

```bash
git add CLAUDE.md ONBOARDING.md
git commit -m "docs: measured counts after the receipt document"
git push -u origin feat/receipt-document
gh pr create --base development --title "fix(till): v4 receipts print their bill number, and cannot crash on a payment"
```

PR body: the two defects (with file:line), the design (document → layout → renderer), the hidden sales-history print button and why, the Review Focus list, and the counts. Wait for Codex before `gh pr merge --auto --merge`.

---

## Self-Review

**Spec coverage (§8):** `ReceiptDocument` as the only printer input — Task 1/4. Bill details incl. original cashier, Bangkok time, refund — Tasks 1, 2. `IsCopy`, reprint time and name — Tasks 1, 2. Factory after a sale (fixes 0000000000) — Tasks 2, 4. Factory on reprint — Task 2 (called by plan B's `InvoiceDetailViewModel`). Printer prints `Invoice No.: {InvoiceNumber:0000000000}`, the document's cashier, the COPY banner and reprint line — Tasks 1, 4. RELEASE gate unchanged — Task 4 keeps `[Conditional("RELEASE")]` on `PrintReceiptForm.PrintReceipt`. `IsVoided` is left to the void feature, as the spec says.
**Spec §7 items deferred on purpose:** client methods and ViewModels (plan B); screens, the บิลวันนี้ button and the remaining stub removals (plan C).
**Placeholders:** none.
**Type consistency:** `ReceiptDocument`, `ReceiptItem`, `ReceiptPayment`, `ReceiptReprint`, `ReceiptHeader`, `ReceiptRow(Style, Text, Amount)`, `ReceiptLayout.Rows(header, document, culture)`, `ReceiptDocumentFactory.ForSale(response, sale)` / `ForReprint(result, reprintedBy)`, `CompletedSale(CashierName, Items, Payments, Change)`, `IPaymentMethodNames.GetAsync()`, `ReceiptRenderer.Draw(graphics, rows)` — same names in every task.
**Review Focus:** all five have a test in the owning task (Task 1: wide number, th-TH date and number; Task 2: null cashier, pre-7am sale; Tasks 3 and 4: unknown code).
