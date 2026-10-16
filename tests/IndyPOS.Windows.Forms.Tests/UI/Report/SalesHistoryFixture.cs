using System.Windows.Forms;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

/// <summary>Builders for the till's sales-history tests. Now is 10:00 on 2026-10-15 in Bangkok.</summary>
internal static class SalesHistoryFixture
{
    public static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 10, 15);
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public static SalesHistoryViewModelFactory Factory(IStoreHubClient client, ICurrentUser currentUser) => new(
        client, Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
        new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), currentUser);

    public static ICurrentUser LoggedIn(UserRole role)
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Somchai");
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

    // Every bill at 02:00 UTC = 09:00 Bangkok today.
    public static SalesPage Page(bool hasMore, params long[] numbers) =>
        new(numbers.Select(n => new InvoiceSummaryDto(Guid.NewGuid(), n, new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc),
                                                      100m, "Cash", 1)).ToList(),
            1, InvoiceHistoryViewModel.PageSize, hasMore);

    public static Control Find(Control root, string name) => root.Controls.Find(name, searchAllChildren: true).Single();
}
