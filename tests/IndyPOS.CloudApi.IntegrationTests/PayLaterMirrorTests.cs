using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.CloudApi.Domain;
using IndyPOS.Domain.Entities.Core;
using Xunit;

using PayLaterDebt = IndyPOS.Domain.Entities.Core.PayLater;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The ลงบัญชี mirror, fed with payloads built by StoreHub's own PayLaterOutbox, so the two sides cannot
/// drift apart. A debt is a snapshot where the newest or equal LastModifiedUtc wins (spec 2026-10-19 §3).
/// </summary>
public class PayLaterMirrorTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string StoreId = "1";        // the harness's store
    private const string OtherStoreId = "2";
    private const decimal DebtAmount = 700m;
    private const decimal PartPaid = 299m;
    private const decimal StalePaid = 100m;
    private const int V3PaymentId = 4242;
    private const string CustomerName = "ลุงสมชาย";
    private const decimal RepaidAmount = 100m;

    private static readonly DateTime T0 = new(2026, 10, 19, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = T0.AddMinutes(1);
    private static readonly DateTime T2 = T0.AddMinutes(2);
    private static readonly DateOnly Today = new(2026, 10, 19);
    private static readonly Guid Cashier = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAStaleDebtSnapshot_KeepsTheNewerPaidAmount()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, Repaid(debt, PartPaid, T2));
        await SendAsync(pipeline, Repaid(debt, StalePaid, T1));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id))!.PaidAmount.Should()
                                                                 .Be(PartPaid, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAStaleDebtSnapshot_MarksItProcessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, Repaid(debt, PartPaid, T2));
        await SendAsync(pipeline, Repaid(debt, StalePaid, T1));

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithADebtIdFromAnotherStore_LeavesTheDebtUnchanged()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, debt);
        await pipeline.PollAsync();
        await SendAsync(pipeline, Repaid(debt, PartPaid, T2), OtherStoreId);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id))!.PaidAmount.Should()
                                                                 .Be(0m);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithADebtIdFromAnotherStore_LeavesTheEventUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, debt);
        await pipeline.PollAsync();
        await SendAsync(pipeline, Repaid(debt, PartPaid, T2), OtherStoreId);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEmptyDebtPayload_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddEventAsync(PayLaterOutbox.PayLaterChanged, "null");

        await pipeline.PollAsync();

        (await pipeline.FailedAttemptsAsync()).Should()
                                              .Be(1);
    }

    // Re-migration keeps the debt's id but gives its sale new ids, at the same v3 timestamp (spec §3).
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEqualTimestamp_TakesTheNewInvoiceId()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, debt);
        var remigrated = Repaid(debt, 0m, debt.LastModifiedUtc);
        remigrated.InvoiceId = Guid.NewGuid();
        await SendAsync(pipeline, remigrated);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id))!.InvoiceId.Should()
                                                                .Be(remigrated.InvoiceId, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithANewDebt_StoresTheCustomerName()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, debt);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id))!.CustomerName.Should()
                                                                   .Be(CustomerName, pipeline.LoggedErrors);
    }

    // A v3 store whose bulk upload was skipped has no invoices in the cloud; its debts must still arrive (spec §5).
    [Fact]
    public async Task ProcessPendingEventsAsync_WithADebtWhoseInvoiceIsNotInTheCloud_StoresIt()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt(V3PaymentId);
        await SendAsync(pipeline, debt);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id)).Should()
                                                         .NotBeNull(pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithANewerSnapshot_CompletesTheDebt()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, Repaid(debt, DebtAmount, T2));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id)).Should()
                                                         .BeEquivalentTo(new { IsCompleted = true, OwedAmount = 0m }, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAV3Debt_MarksItFromV3()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt(V3PaymentId);
        await SendAsync(pipeline, debt);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id))!.IsFromV3.Should()
                                                               .BeTrue(pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAV3DebtRepaidOnV4_KeepsItMarkedFromV3()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt(V3PaymentId);
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, Repaid(debt, PartPaid, T1));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLater>(debt.Id)).Should()
                                                         .BeEquivalentTo(new { IsFromV3 = true, PaidAmount = PartPaid }, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithARepaymentBeforeItsDebt_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var repayment = Repayment(Debt());
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
    }

    // A repayment and its debt's snapshot are saved together at the store; either may reach the cloud first.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithARepaymentBeforeItsDebt_StoresItOnceTheDebtArrives()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        var repayment = Repayment(debt);
        await SendAsync(pipeline, repayment);
        await pipeline.PollAsync();
        await SendAsync(pipeline, debt);
        await pipeline.PollAsync();
        await pipeline.ElapseRetryDelaysAsync();

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLaterRepayment>(repayment.Id)).Should()
                                                                        .NotBeNull(pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithARepaymentForAnotherStoresDebt_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        await SendAsync(pipeline, debt);
        await pipeline.PollAsync();
        var intruder = Repayment(debt);
        intruder.StoreId = OtherStoreId;
        await SendAsync(pipeline, intruder);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
    }

    // Codex review: StoreHub refuses a repayment of 0 or less, but the cloud is its own boundary; such a row
    // would sit in the insert-only audit for good and understate repayment totals.
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task ProcessPendingEventsAsync_WithANonPositiveRepayment_StoresNoRow(int amount)
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        var repayment = Repayment(debt);
        repayment.Amount = amount;
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.CountAsync<CloudPayLaterRepayment>()).Should()
                                                             .Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task ProcessPendingEventsAsync_WithANonPositiveRepayment_LeavesItUnprocessed(int amount)
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        var repayment = Repayment(debt);
        repayment.Amount = amount;
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithTheSameRepaymentTwice_StoresOneRow()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        var repayment = Repayment(debt);
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, repayment);
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.CountAsync<CloudPayLaterRepayment>()).Should()
                                                             .Be(1, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithTheSameRepaymentTwice_MarksBothProcessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        var repayment = Repayment(debt);
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, repayment);
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithARepayment_RecordsTheCashier()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        var repayment = Repayment(debt);
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLaterRepayment>(repayment.Id))!.CreatedByUserId.Should()
                                                                                   .Be(Cashier, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithARepayment_StoresItsBusinessDate()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var debt = Debt();
        var repayment = Repayment(debt);
        await SendAsync(pipeline, debt);
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudPayLaterRepayment>(repayment.Id))!.BusinessDate.Should()
                                                                                .Be(Today, pipeline.LoggedErrors);
    }

    // As a credit sale creates it: nothing paid yet.
    private static PayLaterDebt Debt(int? legacyPaymentId = null) => new()
    {
        Id = Guid.NewGuid(), PaymentId = Guid.NewGuid(), InvoiceId = Guid.NewGuid(), Description = CustomerName,
        PayLaterAmount = DebtAmount, LegacyPaymentId = legacyPaymentId, CreatedUtc = T0, LastModifiedUtc = T0
    };

    // The debt as StoreHub holds it after a repayment, at the given instant.
    private static PayLaterDebt Repaid(PayLaterDebt debt, decimal paidAmount, DateTime at) => new()
    {
        Id = debt.Id, PaymentId = debt.PaymentId, InvoiceId = debt.InvoiceId, Description = debt.Description,
        PayLaterAmount = debt.PayLaterAmount, PaidAmount = paidAmount, IsCompleted = paidAmount >= debt.PayLaterAmount,
        LegacyPaymentId = debt.LegacyPaymentId, CreatedUtc = debt.CreatedUtc, LastModifiedUtc = at
    };

    // As the ลงบัญชี screen records one.
    private static PayLaterRepayment Repayment(PayLaterDebt debt) => new()
    {
        Id = Guid.NewGuid(), StoreId = StoreId, PayLaterId = debt.Id, Amount = RepaidAmount, BusinessDate = Today,
        CreatedUtc = T1, CreatedByUserId = Cashier
    };

    private static Task SendAsync(EventPipeline pipeline, PayLaterRepayment repayment) =>
        pipeline.AddOutboxEventAsync(PayLaterOutbox.Repaid(repayment, repayment.CreatedUtc));

    private static Task SendAsync(EventPipeline pipeline, PayLaterDebt debt, string storeId = StoreId) =>
        pipeline.AddOutboxEventAsync(PayLaterOutbox.Changed(debt, storeId, debt.LastModifiedUtc));
}
