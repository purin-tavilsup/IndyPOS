using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;
using Xunit;
using static IndyPOS.Application.Tests.UseCases.StoreHub.Sales.SalesHistoryTestContext;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class GetSaleQueryHandlerTests
{
    private const long KnownNumber = 7001;
    private const string RetiredCampaignCode = "WeWin";

    [Fact]
    public async Task HandleById_WithAnUnknownId_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(Guid.NewGuid(), CanViewAnyDay: true));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleById_AsTodayOnlyCallerForYesterdaysBill_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleByNumber_AsTodayOnlyCallerForYesterdaysBill_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(KnownNumber, CanViewAnyDay: false));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleByNumber_WithAnotherStoresNumber_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc) { StoreId = OtherStoreId });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(KnownNumber, CanViewAnyDay: true));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleByNumber_WithZero_ThrowsValidation()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(0, CanViewAnyDay: true));

        await act.Should()
                 .ThrowAsync<SalesQueryValidationException>();
    }

    [Fact]
    public async Task HandleById_WithAnUnknownCashier_ReturnsNullCashierName()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.CashierName.Should()
                         .BeNull();
    }

    [Fact]
    public async Task HandleById_WithALineWithoutACategory_ReturnsNullKind()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc) { ProductCategory = null });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Lines.Single().CategoryKind.Should()
                                         .BeNull();
    }

    [Fact]
    public async Task HandleById_WithAMethodMissingFromTheCatalogue_UsesTheCodeAsDisplayName()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc)
        {
            Payments = [new SalePayment(RetiredCampaignCode, 350m)]
        });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Payments.Single().MethodDisplayName.Should()
                                                 .Be(RetiredCampaignCode);
    }

    [Fact]
    public async Task HandleById_AsTodayOnlyCallerForABillJustAfterBangkokMidnight_ReturnsIt()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, JustAfterMidnightTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale.Should()
            .NotBeNull();
    }

    [Fact]
    public async Task HandleById_AsAnyDayCallerForYesterdaysBill_ReturnsIt()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: true));

        sale!.InvoiceNumber.Should()
                           .Be(KnownNumber);
    }

    [Fact]
    public async Task HandleByNumber_WithAKnownNumber_ReturnsTheSameBillAsById()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var byNumber = await c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(KnownNumber, CanViewAnyDay: false));
        var byId = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        byNumber.Should()
                .BeEquivalentTo(byId);
    }

    [Fact]
    public async Task HandleById_WithAKnownCashier_ReturnsTheirFullName()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedCashierAsync("สมชาย", "ใจดี");
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.CashierName.Should()
                         .Be("สมชาย ใจดี");
    }

    [Fact]
    public async Task HandleById_WithAHardwareLine_ReturnsTheHardwareKind()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedCategoryAsync(ProductCategoryCodes.GeneralMaterials, ProductCategoryKind.Hardware);
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc) { ProductCategory = ProductCategoryCodes.GeneralMaterials });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Lines.Single().CategoryKind.Should()
                                         .Be(ProductCategoryKind.Hardware);
    }

    [Fact]
    public async Task HandleById_WithACatalogueMethod_ReturnsItsDisplayName()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedPaymentMethodAsync(PaymentMethodCodes.Cash, "เงินสด");
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Payments.Single().MethodDisplayName.Should()
                                                 .Be("เงินสด");
    }

    // CompleteSaleCommandHandler accepts a method code case-insensitively and stores the caller's
    // spelling, so "cash" is a valid stored code while the catalogue holds "Cash".
    [Fact]
    public async Task HandleById_WithALowerCaseCatalogueMethod_ReturnsItsDisplayName()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedPaymentMethodAsync(PaymentMethodCodes.Cash, "เงินสด");
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc)
        {
            Payments = [new(PaymentMethodCodes.Cash.ToLowerInvariant(), 500m)],
        });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Payments.Single().MethodDisplayName.Should()
                                                 .Be("เงินสด");
    }

    [Fact]
    public async Task HandleById_WithOverpayment_ReturnsTheChangeGiven()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.ChangeGiven.Should()
                         .Be(150m);
    }

    [Fact]
    public async Task HandleById_WithALineNote_ReturnsIt()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Lines.Single().Note.Should()
                                 .Be("ถุงใหญ่");
    }

    // The catalogue key (StoreId, Code) is case-sensitive, so a store can hold both "Cash" and "cash".
    [Fact]
    public async Task HandleById_WithCatalogueCodesDifferingOnlyByCase_ReturnsTheBill()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedPaymentMethodAsync(PaymentMethodCodes.Cash, "เงินสด");
        await c.SeedPaymentMethodAsync(PaymentMethodCodes.Cash.ToLowerInvariant(), "เงินสด (เก่า)");
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale.Should()
            .NotBeNull();
    }
}
