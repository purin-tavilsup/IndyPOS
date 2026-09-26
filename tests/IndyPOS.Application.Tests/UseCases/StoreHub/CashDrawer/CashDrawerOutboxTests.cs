using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashDrawerOutboxTests
{
    private static readonly DateTime NowUtc = new(2026, 9, 26, 3, 0, 0, DateTimeKind.Utc);

    private static CashPayout NewPayout() => new()
    {
        Id = Guid.NewGuid(),
        StoreId = "test-store",
        Category = PayoutCategory.Hardware,
        Amount = 250m,
        BusinessDate = new DateOnly(2026, 9, 26),
        CreatedUtc = NowUtc,
        LastModifiedUtc = NowUtc,
        CreatedByUserId = Guid.NewGuid()
    };

    [Fact]
    public void Changed_WithPayout_SerializesCategoryByName()
    {
        var outboxEvent = CashDrawerOutbox.Changed(NewPayout(), NowUtc);

        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);
        payload.RootElement.GetProperty("Category").GetString().Should()
                                                               .Be("Hardware");
    }

    [Fact]
    public void Changed_WithDeletedPayout_CarriesTheDeletedFlag()
    {
        var payout = NewPayout();
        payout.MarkDeleted(Guid.NewGuid(), NowUtc);

        var outboxEvent = CashDrawerOutbox.Changed(payout, NowUtc);

        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);
        payload.RootElement.GetProperty("IsDeleted").GetBoolean().Should()
                                                                 .BeTrue();
    }

    [Theory]
    [InlineData(typeof(CashPayout), CashDrawerOutbox.CashPayoutChanged)]
    [InlineData(typeof(CashFloat), CashDrawerOutbox.CashFloatChanged)]
    [InlineData(typeof(DebtRepayment), CashDrawerOutbox.DebtRepaymentChanged)]
    public void Changed_WithEntryType_ReturnsItsEventType(Type entryType, string expectedType)
    {
        var entry = (CashDrawerEntry)Activator.CreateInstance(entryType)!;
        entry.StoreId = "test-store";

        var outboxEvent = CashDrawerOutbox.Changed(entry, NowUtc);

        outboxEvent.Type.Should()
                        .Be(expectedType);
    }

    [Fact]
    public void CountAdded_WithCount_ReturnsPendingCashCountChangedEvent()
    {
        var count = new CashCount { Id = Guid.NewGuid(), StoreId = "test-store" };

        var outboxEvent = CashDrawerOutbox.CountAdded(count, NowUtc);

        outboxEvent.Type.Should()
                        .Be(CashDrawerOutbox.CashCountChanged);
        outboxEvent.Status.Should()
                          .Be("Pending");
    }
}
