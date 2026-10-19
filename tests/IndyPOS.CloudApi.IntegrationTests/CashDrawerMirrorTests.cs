using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.CloudApi.Domain;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The cash mirror, fed with payloads built by StoreHub's own CashDrawerOutbox, so the two sides
/// cannot drift apart. Each event is a full snapshot; the newest LastModifiedUtc wins.
/// </summary>
public class CashDrawerMirrorTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string StoreId = "1";        // the harness's store
    private const string OtherStoreId = "2";
    private const decimal PayoutAmount = 120m;
    private const decimal EditedAmount = 150m;
    private const decimal StaleAmount = 999m;

    private static readonly DateTime T0 = new(2026, 10, 18, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = T0.AddMinutes(1);
    private static readonly DateTime T2 = T0.AddMinutes(2);
    private static readonly Guid Cashier = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Manager = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAStaleEdit_KeepsTheNewerAmount()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, payout);
        await SendAsync(pipeline, Edited(payout, EditedAmount, T2));
        await SendAsync(pipeline, Edited(payout, StaleAmount, T1));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.Amount.Should()
                                                                .Be(EditedAmount, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEditAtTheSameInstant_KeepsTheStoredAmount()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, Edited(payout, EditedAmount, T1));
        await SendAsync(pipeline, Edited(payout, StaleAmount, T1));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.Amount.Should()
                                                                .Be(EditedAmount, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAStaleEditAfterADelete_KeepsTheRowDeleted()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, Deleted(payout, T2));
        await SendAsync(pipeline, Edited(payout, StaleAmount, T1));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.IsDeleted.Should()
                                                                   .BeTrue(pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEditArrivingBeforeItsAdd_KeepsTheEdit()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        var add = CashDrawerOutbox.Changed(payout, T0);
        await SendAsync(pipeline, Edited(payout, EditedAmount, T1));
        await pipeline.AddCashEventAsync(add);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.Amount.Should()
                                                                .Be(EditedAmount, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithARowIdFromAnotherStore_LeavesTheRowUnchanged()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, payout);
        await pipeline.PollAsync();
        var intruder = Edited(payout, StaleAmount, T2);
        intruder.StoreId = OtherStoreId;
        await SendAsync(pipeline, intruder);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.Amount.Should()
                                                                .Be(PayoutAmount);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithARowIdFromAnotherStore_LeavesTheEventUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, payout);
        await pipeline.PollAsync();
        var intruder = Edited(payout, StaleAmount, T2);
        intruder.StoreId = OtherStoreId;
        await SendAsync(pipeline, intruder);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAStaleEdit_MarksItProcessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, Edited(payout, EditedAmount, T2));
        await SendAsync(pipeline, Edited(payout, StaleAmount, T1));

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithTimesWithoutAZone_StoresThemAsUtc()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var cashFloat = Float();
        var payload = JsonNode.Parse(CashDrawerOutbox.Changed(cashFloat, T0).PayloadJson)!.AsObject();
        payload["CreatedUtc"] = "2026-10-18T03:00:00";
        payload["LastModifiedUtc"] = "2026-10-18T03:00:00";
        await pipeline.AddEventAsync(CashDrawerOutbox.CashFloatChanged, payload.ToJsonString());

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashFloat>(cashFloat.Id))!.CreatedAtUtc.Should()
                                                                 .Be(T0, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEmptyPayload_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddEventAsync(CashDrawerOutbox.CashPayoutChanged, "null");

        await pipeline.PollAsync();

        (await pipeline.FailedAttemptsAsync()).Should()
                                              .Be(1);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithANewHardwarePayout_StoresItsCategoryAsText()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout(PayoutCategory.Hardware);
        await SendAsync(pipeline, payout);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.Category.Should()
                                                                  .Be("Hardware", pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEdit_UpdatesWhoLastChangedIt()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, payout);
        await SendAsync(pipeline, Edited(payout, EditedAmount, T1));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.LastModifiedByUserId.Should()
                                                                              .Be(Manager, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithADelete_RecordsWhenItWasDeleted()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Payout();
        await SendAsync(pipeline, payout);
        await SendAsync(pipeline, Deleted(payout, T2));

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashPayout>(payout.Id))!.DeletedAtUtc.Should()
                                                                      .Be(T2, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithANewFloat_StoresItsAmount()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var cashFloat = Float();
        await SendAsync(pipeline, cashFloat);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudCashFloat>(cashFloat.Id))!.Amount.Should()
                                                                  .Be(cashFloat.Amount, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithANewDebtRepayment_StoresTheCustomerName()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var repayment = Stamp(new DebtRepayment { CustomerName = "ลุงสมชาย", Amount = 300m });
        await SendAsync(pipeline, repayment);

        await pipeline.PollAsync();

        (await pipeline.FindAsync<CloudDebtRepayment>(repayment.Id))!.CustomerName.Should()
                                                                          .Be("ลุงสมชาย", pipeline.LoggedErrors);
    }

    private static CashPayout Payout(PayoutCategory category = PayoutCategory.General) =>
        Stamp(new CashPayout { Category = category, Amount = PayoutAmount, Description = "ค่าน้ำแข็ง" });

    private static CashFloat Float() => Stamp(new CashFloat { Amount = 1_000m });

    // As StoreHub's add handlers stamp a new entry.
    private static T Stamp<T>(T entry) where T : CashDrawerEntry
    {
        entry.Id = Guid.NewGuid();
        entry.StoreId = StoreId;
        entry.BusinessDate = new DateOnly(2026, 10, 18);
        entry.CreatedUtc = T0;
        entry.LastModifiedUtc = T0;
        entry.CreatedByUserId = Cashier;
        return entry;
    }

    // A later snapshot of the same row; the original object stays as it was.
    private static CashPayout Edited(CashPayout payout, decimal amount, DateTime at)
    {
        var copy = Clone(payout);
        copy.Amount = amount;
        copy.Touch(Manager, at);
        return copy;
    }

    private static CashPayout Deleted(CashPayout payout, DateTime at)
    {
        var copy = Clone(payout);
        copy.MarkDeleted(Manager, at);
        return copy;
    }

    private static CashPayout Clone(CashPayout payout) =>
        JsonSerializer.Deserialize<CashPayout>(JsonSerializer.Serialize(payout))!;

    private static Task SendAsync(EventPipeline pipeline, CashDrawerEntry entry) =>
        pipeline.AddCashEventAsync(CashDrawerOutbox.Changed(entry, entry.LastModifiedUtc));
}
