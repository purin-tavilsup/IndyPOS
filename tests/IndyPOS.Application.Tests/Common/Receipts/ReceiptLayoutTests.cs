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
                   .ContainInConsecutiveOrder("พิมพ์ซ้ำ 16/10/2026 09:05", "โดย Somchai Manager");
    }

    [Fact]
    public void Rows_OnAThaiTill_PrintsTheBuddhistYear()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Thai);

        Texts(rows).Should()
                   .Contain(t => t.StartsWith("Date: ") && t.EndsWith("2569"));
    }

    // th-TH's "tt" printed หลังเที่ยง; the store wants the time as 02:30 PM on every till.
    [Fact]
    public void Rows_OnAThaiTill_PrintsTheTimeAsTwelveHourWithAmPm()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Thai);

        Texts(rows).Should()
                   .Contain("Time: 02:30 PM");
    }

    // th-TH's "N" has three decimals under .NET's ICU data; baht has two.
    [Fact]
    public void Rows_OnAThaiTill_PrintsMoneyWithTwoDecimals()
    {
        var rows = ReceiptLayout.Rows(Header, Sale(), Thai);

        rows.Should()
            .Contain(new ReceiptRow(ReceiptRowStyle.Indented, "2 x 50.00", "100.00"));
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
