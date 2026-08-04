using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class PaymentMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public PaymentMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigratePayments_EachLegacyType_MapsToItsCatalogueCodeAndAmount()
    {
        // Defect 1 regression guard. The original mapping was SHIFTED, not misnamed: PayLater to
        // "Card", WelfareCard to "Transfer", MoneyTransfer to "WelfareCard". ~15% of THB 21.2M
        // landed on the wrong method. It survived because the verifier compared only row counts and
        // SUM(Invoice.Total), both of which reconcile perfectly under a scramble.
        //
        // A DISTINCT AMOUNT PER METHOD is what makes this test able to catch a shift: with equal
        // amounts, any permutation would still sum correctly.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 1143m, dateCreated: "2024-03-15 14:30:00");

        // Every amount MUST be distinct. The plan's original table gave PayLater and WeWin both 10,
        // which left the guard blind to a 2<->8 swap -- a shift between the store's highest-value
        // method (฿836k of credit) and a dead campaign. Verified: with both at 10 the swap passed.
        var expected = new (int LegacyId, string Code, decimal Amount)[]
        {
            (1, PaymentMethodCodes.Cash,          1m),
            (2, PaymentMethodCodes.PayLater,     10m),
            (3, PaymentMethodCodes.WelfareCard, 100m),
            (4, PaymentMethodCodes.M33WeLove,     5m),
            (5, PaymentMethodCodes.MoneyTransfer, 1000m),
            (7, PaymentMethodCodes.FiftyFifty,   20m),
            (8, PaymentMethodCodes.WeWin,         7m)
        };

        expected.Select(e => e.Amount).Should().OnlyHaveUniqueItems(
            "a repeated amount makes this guard blind to a shift between those two methods");

        var paymentId = 500;
        foreach (var (legacyId, _, amount) in expected)
        {
            await builder.AddPaymentAsync(
                paymentId: paymentId++, invoiceId: 1, paymentTypeId: legacyId, amount: amount,
                dateCreated: "2024-03-15 14:30:00");
        }

        // PaymentTypeId 2 needs its PayLater extension, or defect 10's lookup has nothing to attach.
        await builder.AddPayLaterAsync(
            paymentId: 501, invoiceId: 1, description: "Somchai", payLaterAmount: 10m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        await using var db = _postgres.CreateDbContext();
        var byMethod = await db.Payments
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount), Count = g.Count() })
            .ToListAsync();

        foreach (var (_, code, amount) in expected)
        {
            var row = byMethod.SingleOrDefault(m => m.Method == code);
            row.Should().NotBeNull($"legacy type mapping to {code} must produce exactly one payment");
            row!.Total.Should().Be(amount, $"{code} must carry its own amount, not another method's");
            row.Count.Should().Be(1);
        }

        byMethod.Should().HaveCount(expected.Length);
    }

    [Fact]
    public async Task MigratePayments_WithAnUnmappableType_RefusesRatherThanGuessing()
    {
        // Legacy id 6 (ผ่อนชำระ, instalments) has no catalogue equivalent. It must be refused, not
        // written as "Other": that is not a catalogue code, so the amount becomes unresolvable
        // while the row counts still reconcile.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 300m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 6, amount: 300m,
            dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.Payments.Failed.Should().Be(1);
        result.Errors.Should().ContainSingle().Which.Should().Contain("PaymentTypeId 6");

        await using var db = _postgres.CreateDbContext();
        (await db.Payments.CountAsync()).Should().Be(0);
    }
}
