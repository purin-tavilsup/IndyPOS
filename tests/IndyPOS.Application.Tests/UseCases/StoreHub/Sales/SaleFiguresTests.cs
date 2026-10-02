using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class SaleFiguresTests
{
    private const decimal RefundTotal = -120m;

    private static SalePayment Cash(decimal amount) => new(PaymentMethodCodes.Cash, amount);

    [Fact]
    public void From_WithARefundTotal_ReturnsNoChange()
    {
        SaleFigures.From(RefundTotal, [Cash(RefundTotal)]).ChangeGiven.Should()
                                                                      .Be(0m);
    }

    [Fact]
    public void From_WithARefundTotal_FlagsARefund()
    {
        SaleFigures.From(RefundTotal, [Cash(RefundTotal)]).IsRefund.Should()
                                                                   .BeTrue();
    }

    [Fact]
    public void From_WithUnderpayment_ReturnsNoChange()
    {
        SaleFigures.From(350m, [Cash(300m)]).ChangeGiven.Should()
                                                        .Be(0m);
    }

    [Fact]
    public void From_WithALowerCasePayLaterCode_FlagsPayLater()
    {
        // The sale handler accepts method codes case-insensitively, so a till may store "paylater".
        SaleFigures.From(350m, [new SalePayment("paylater", 350m)]).HasPayLater.Should()
                                                                              .BeTrue();
    }

    [Fact]
    public void From_WithoutPayLater_ReturnsZeroPayLaterAmount()
    {
        SaleFigures.From(350m, [Cash(500m)]).PayLaterAmount.Should()
                                                           .Be(0m);
    }

    [Fact]
    public void From_WithOverpayment_ReturnsTheChange()
    {
        SaleFigures.From(350m, [Cash(500m)]).ChangeGiven.Should()
                                                        .Be(150m);
    }

    [Fact]
    public void From_WithTwoPayments_SumsTheAmountReceived()
    {
        SaleFigures.From(350m, [Cash(200m), new SalePayment(PaymentMethodCodes.MoneyTransfer, 150m)]).AmountReceived.Should()
                                                                                                                  .Be(350m);
    }

    [Fact]
    public void From_WithAPayLaterPayment_ReturnsItsAmount()
    {
        SaleFigures.From(350m, [Cash(100m), new SalePayment(PaymentMethodCodes.PayLater, 250m)]).PayLaterAmount.Should()
                                                                                                           .Be(250m);
    }
}
