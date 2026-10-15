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
