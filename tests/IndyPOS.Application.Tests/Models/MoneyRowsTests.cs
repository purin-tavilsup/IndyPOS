using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class MoneyRowsTests
{
    private static SalesSummaryDto Summary(IReadOnlyList<PaymentMethodTotalDto> methods, IReadOnlyList<ServiceSaleDto> services) =>
        new(new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 7), 0, 0m, new PaymentBreakdownDto(0, 0, 0, 0, 0, 0), [])
        {
            PaymentsByMethod = methods,
            ServiceSales = services
        };

    [Fact]
    public void From_WithNoMethodsOrServices_ReturnsNothing()
    {
        MoneyRows.From(Summary([], [])).Should()
                                       .BeEmpty();
    }

    [Fact]
    public void From_WithMethodsAndServices_ListsMethodsThenServicesByTheirNames()
    {
        var rows = MoneyRows.From(Summary(
            [new PaymentMethodTotalDto("Cash", "เงินสด", 100m), new PaymentMethodTotalDto("MoneyTransfer", "เงินโอน", 50m)],
            [new ServiceSaleDto("2002500000014", "จัดส่ง", 30m)]));

        rows.Should()
            .Equal(new MoneyRow("เงินสด", 100m), new MoneyRow("เงินโอน", 50m), new MoneyRow("จัดส่ง", 30m));
    }
}
