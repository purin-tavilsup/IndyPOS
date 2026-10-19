using System.Windows.Forms;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace IndyPOS.Windows.Forms.Tests.UI.CashDrawer;

/// <summary>Builders for the till's cash-drawer tests. Now is 10:00 on 2026-10-19 in Bangkok.</summary>
internal static class CashDrawerFixture
{
    public static readonly DateTimeOffset NowUtc = new(2026, 10, 19, 3, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 10, 19);
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    public static readonly StoreFeaturesDto GeneralHardware = new(PayLaterEnabled: true, MultipleProductTypesEnabled: true);
    public static readonly StoreFeaturesDto Minimart = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false);

    public static StoreClock Clock() => new(new FakeTimeProvider(NowUtc), Bangkok);

    public static CashDrawerViewModelFactory Factory(IStoreHubClient client, ICurrentUser currentUser) =>
        new(client, Clock(), currentUser);

    public static ICurrentUser LoggedIn(UserRole role)
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Malee");
        user.SetupGet(u => u.LastName).Returns(role.ToString());
        user.SetupGet(u => u.RoleId).Returns((int)role);

        var current = new Mock<ICurrentUser>();
        current.SetupGet(c => c.User).Returns(user.Object);

        return current.Object;
    }

    public static ICurrentUser NobodyLoggedIn() => new Mock<ICurrentUser>().Object;

    public static IStoreFeaturesProvider Features(StoreFeaturesDto features)
    {
        var provider = new Mock<IStoreFeaturesProvider>();
        provider.Setup(p => p.GetAsync()).ReturnsAsync(features);

        return provider.Object;
    }

    /// <summary>A StoreHub that answers every cash call for <paramref name="day"/> with an empty day.</summary>
    public static Mock<IStoreHubClient> EmptyDay(DateOnly day, bool isEditable = true, decimal expectedCash = 0m)
    {
        var client = new Mock<IStoreHubClient>();
        client.Setup(c => c.GetCashSummaryAsync(day, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Summary(day, isEditable, expectedCash));
        client.Setup(c => c.GetCashPayoutsAsync(day, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        client.Setup(c => c.GetCashFloatsAsync(day, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        client.Setup(c => c.GetDebtRepaymentsAsync(day, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        client.Setup(c => c.GetCashCountsAsync(day, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        client.Setup(c => c.GetPayLaterTakenOnAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new GetPayLaterResponse(0m, 0m, 0, 0, []));

        return client;
    }

    public static CashDrawerSummaryDto Summary(DateOnly day, bool isEditable, decimal expectedCash,
                                               IReadOnlyList<PaymentMethodTotalDto>? nonCash = null) => new()
    {
        BusinessDate = day, IsEditable = isEditable, GeneralProductsTotal = 0m, HardwareProductsTotal = 0m,
        CashPaymentsTotal = 0m, NonCashPayments = nonCash ?? [], PayLaterGeneralProductsTotal = 0m,
        PayLaterHardwareProductsTotal = 0m, PayoutsTotal = 0m, GeneralPayoutsTotal = 0m,
        HardwarePayoutsTotal = 0m, CashFloatsTotal = 0m, DebtRepaymentsTotal = 0m, ExpectedCash = expectedCash
    };

    public static Control Find(Control root, string name) => root.Controls.Find(name, searchAllChildren: true).Single();
}
