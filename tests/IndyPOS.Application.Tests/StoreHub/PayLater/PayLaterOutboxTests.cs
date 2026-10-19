using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Domain.Entities.Core;
using Xunit;

using PayLaterDebt = IndyPOS.Domain.Entities.Core.PayLater;

namespace IndyPOS.Application.Tests.StoreHub.PayLater;

public class PayLaterOutboxTests
{
    private const string StoreId = "GENERAL-HARDWARE";
    private const int V3PaymentId = 4_242;
    private static readonly DateTime Now = new(2026, 10, 19, 5, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Cashier = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // The cloud's ingest check reads the payload's top-level StoreId; without it the whole batch gets a 403.
    [Fact]
    public void Changed_ForADebt_PutsTheStoreIdAtTheTop()
    {
        var outboxEvent = PayLaterOutbox.Changed(Debt(), StoreId, Now);

        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);

        payload.RootElement.GetProperty("StoreId").GetString().Should()
                                                            .Be(StoreId);
    }

    [Fact]
    public void Changed_ForADebt_CarriesTheFullState()
    {
        var debt = Debt();

        var payload = JsonSerializer.Deserialize<PayLaterChangedPayload>(PayLaterOutbox.Changed(debt, StoreId, Now).PayloadJson);

        payload.Should()
               .BeEquivalentTo(new { debt.Id, PaidAmount = 350m, IsCompleted = true, LegacyPaymentId = (int?)V3PaymentId });
    }

    [Fact]
    public void Changed_ForADebt_IsAPendingPayLaterChangedEvent()
    {
        var outboxEvent = PayLaterOutbox.Changed(Debt(), StoreId, Now);

        outboxEvent.Should()
                   .BeEquivalentTo(new { StoreId, Type = PayLaterOutbox.PayLaterChanged, Status = "Pending", CreatedUtc = Now });
    }

    [Fact]
    public void Repaid_ForARepayment_CarriesTheCashier()
    {
        var repayment = new PayLaterRepayment
        {
            Id = Guid.NewGuid(), StoreId = StoreId, PayLaterId = Guid.NewGuid(), Amount = 100m,
            BusinessDate = new DateOnly(2026, 10, 19), CreatedUtc = Now, CreatedByUserId = Cashier
        };

        var outboxEvent = PayLaterOutbox.Repaid(repayment, Now);
        var payload = JsonSerializer.Deserialize<PayLaterRepaidPayload>(outboxEvent.PayloadJson);

        payload.Should()
               .BeEquivalentTo(new { repayment.Id, StoreId, repayment.PayLaterId, CreatedByUserId = Cashier });
    }

    private static PayLaterDebt Debt() => new()
    {
        Id = Guid.NewGuid(), PaymentId = Guid.NewGuid(), InvoiceId = Guid.NewGuid(), Description = "ลุงสมชาย",
        PayLaterAmount = 350m, PaidAmount = 350m, IsCompleted = true, LegacyPaymentId = V3PaymentId,
        CreatedUtc = Now, LastModifiedUtc = Now
    };
}
