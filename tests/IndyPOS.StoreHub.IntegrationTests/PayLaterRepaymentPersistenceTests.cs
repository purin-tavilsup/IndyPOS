using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class PayLaterRepaymentPersistenceTests : IntegrationTestBase
{
    private const int V3PaymentId = 4_242;
    private static readonly Guid Cashier = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public PayLaterRepaymentPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Insert_ForAnUnknownDebt_ThrowsForeignKeyViolation()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        db.PayLaterRepayments.Add(Repayment(payLaterId: Guid.NewGuid()));

        var act = () => db.SaveChangesAsync();

        await act.Should()
                 .ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Insert_ForADebt_RoundTripsTheCashier()
    {
        var debt = await SeedDebtAsync(legacyPaymentId: null);
        var repayment = Repayment(debt.Id);
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
            db.PayLaterRepayments.Add(repayment);
            await db.SaveChangesAsync();
        }

        var stored = await FindAsync<PayLaterRepayment>(repayment.Id);

        stored!.CreatedByUserId.Should()
                               .Be(Cashier);
    }

    [Fact]
    public async Task LegacyPaymentId_WhenSet_RoundTrips()
    {
        var debt = await SeedDebtAsync(legacyPaymentId: V3PaymentId);

        var stored = await FindAsync<PayLater>(debt.Id);

        stored!.LegacyPaymentId.Should()
                               .Be(V3PaymentId);
    }

    private static PayLaterRepayment Repayment(Guid payLaterId) => new()
    {
        Id = Guid.NewGuid(), StoreId = TestStoreIdentityService.TestStoreId, PayLaterId = payLaterId, Amount = 100m,
        BusinessDate = new DateOnly(2026, 10, 19), CreatedUtc = DateTime.UtcNow, CreatedByUserId = Cashier
    };

    // A debt needs its invoice and its payment (1:1).
    private async Task<PayLater> SeedDebtAsync(int? legacyPaymentId)
    {
        var invoice = await SeedInvoiceAsync(DateTime.UtcNow);
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var payment = new Payment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, Method = "PayLater", Amount = 350m, Note = "ลุงสมชาย", CreatedUtc = DateTime.UtcNow };
        var debt = new PayLater
        {
            Id = Guid.NewGuid(), PaymentId = payment.Id, InvoiceId = invoice.Id, Description = "ลุงสมชาย",
            PayLaterAmount = 350m, CreatedUtc = DateTime.UtcNow, LastModifiedUtc = DateTime.UtcNow, LegacyPaymentId = legacyPaymentId
        };
        db.Payments.Add(payment);
        db.PayLaters.Add(debt);
        await db.SaveChangesAsync();
        return debt;
    }

    private async Task<T?> FindAsync<T>(Guid id) where T : class
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Set<T>().FindAsync(id);
    }
}
