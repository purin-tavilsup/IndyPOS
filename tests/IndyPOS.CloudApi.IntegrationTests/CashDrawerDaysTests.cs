using FluentAssertions;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;
using Npgsql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// CashDrawerDays answers "does the count match the drawer (ลงตัวมั้ย)?" per store per day in the cloud
/// (spec 2026-10-19 §3). Its figures must equal the till's, so the parity test computes them with the
/// till's own functions and compares.
/// </summary>
public class CashDrawerDaysTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string Store = "1";        // the harness's store
    private const string OtherStore = "2";
    private const decimal FloatAmount = 1_000m;
    private const decimal PayoutAmount = 120m;
    private const decimal RepaidAmount = 300m;

    private static readonly DateOnly Day = new(2026, 10, 19);
    private static readonly DateOnly NextDay = Day.AddDays(1);
    private static readonly DateTime Noon = new(2026, 10, 19, 5, 0, 0, DateTimeKind.Utc);     // 12:00 Bangkok
    private static readonly DateTime LaterNoon = Noon.AddHours(1);
    private static readonly Guid Cashier = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task CashDrawerDays_WithNoCount_HasNoDifference()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await SellAsync(pipeline, 80m, Cash(100m));

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day)).Should()
                                               .BeEquivalentTo(new { CountedCash = (decimal?)null, CashDifference = (decimal?)null }, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithARefund_TakesItsCashOff()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await SellAsync(pipeline, 100m, Cash(100m));
        await SellAsync(pipeline, -100m, Cash(-100m));

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day))!.CashSalesTotal.Should()
                                                              .Be(0m, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithADeletedPayout_LeavesItOut()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var payout = Stamp(new CashPayout { Amount = PayoutAmount });
        payout.MarkDeleted(Cashier, LaterNoon);
        await SendAsync(pipeline, payout);

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day))!.PayoutsTotal.Should()
                                                            .Be(0m, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithAPayLaterSale_KeepsNoCash()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await SellAsync(pipeline, 350m, Paid("PayLater", 350m));

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day))!.CashSalesTotal.Should()
                                                              .Be(0m, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithAnotherStoresRows_KeepsThemApart()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await SellAsync(pipeline, 80m, Cash(80m));
        await SellAsync(pipeline, 500m, Cash(500m), storeId: OtherStore);

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day))!.CashSalesTotal.Should()
                                                              .Be(80m, pipeline.LoggedErrors);
    }

    // 00:30 in Bangkok on the 20th is 17:30 UTC on the 19th; the store's day wins (spec §3).
    [Fact]
    public async Task CashDrawerDays_WithASaleJustAfterBangkokMidnight_CountsItOnItsBusinessDate()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddInvoiceCompletedAsync(businessDate: NextDay, totalAmount: 80m, payments: Cash(80m),
                                                createdAtUtc: new DateTime(2026, 10, 19, 17, 30, 0, DateTimeKind.Utc));

        await pipeline.PollAsync();

        (await DaysAsync(pipeline, Store)).Select(d => d.BusinessDate).Should()
                                                                      .Equal([NextDay], pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithASaleWithoutABusinessDate_FallsBackToItsUtcDate()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddInvoiceCompletedAsync(businessDate: null, totalAmount: 80m, payments: Cash(80m),
                                                createdAtUtc: new DateTime(2026, 10, 19, 17, 30, 0, DateTimeKind.Utc));

        await pipeline.PollAsync();

        (await DaysAsync(pipeline, Store)).Select(d => d.BusinessDate).Should()
                                                                      .Equal([Day], pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithOnlyAFloat_ShowsTheDay()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await SendAsync(pipeline, Stamp(new CashFloat { Amount = FloatAmount }));

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day)).Should()
                                               .BeEquivalentTo(new { CashSalesTotal = 0m, CashFloatsTotal = FloatAmount }, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithTwoCounts_UsesTheLatest()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await CountAsync(pipeline, Count(bankNote100: 5, at: Noon));
        var latest = Count(bankNote100: 7, at: LaterNoon);
        await CountAsync(pipeline, latest);

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day)).Should()
                                               .BeEquivalentTo(new { CountedCash = (decimal?)latest.CountedTotal, LatestCountAtUtc = (DateTime?)LaterNoon }, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_WithACountShort_ShowsANegativeDifference()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await SellAsync(pipeline, 500m, Cash(500m));
        await CountAsync(pipeline, Count(bankNote100: 4, at: LaterNoon));

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day))!.CashDifference.Should()
                                                              .Be(-100m, pipeline.LoggedErrors);
    }

    // The parity test (spec §3): one busy day, figures computed with the till's own functions.
    [Fact]
    public async Task CashDrawerDays_ForADay_MatchesTheTillsExpectedCash()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        (decimal Total, SalePayment[] Payments)[] bills =
        [
            (80m, [new SalePayment("Cash", 100m)]),
            (100m, [new SalePayment("MoneyTransfer", 60m), new SalePayment("Cash", 50m)]),
            (-100m, [new SalePayment("Cash", -100m)]),
            (350m, [new SalePayment("PayLater", 350m)])
        ];
        foreach (var (total, payments) in bills)
            await SellAsync(pipeline, total, payments.Select(p => new PaymentSnapshot { PaymentId = Guid.NewGuid(), Method = p.Method, Amount = p.Amount }).ToArray());
        await SendAsync(pipeline, Stamp(new DebtRepayment { CustomerName = "ลุงสมชาย", Amount = RepaidAmount }));
        await SendAsync(pipeline, Stamp(new CashFloat { Amount = FloatAmount }));
        await SendAsync(pipeline, Stamp(new CashPayout { Amount = PayoutAmount }));
        var deleted = Stamp(new CashPayout { Amount = 999m });
        deleted.MarkDeleted(Cashier, LaterNoon);
        await SendAsync(pipeline, deleted);
        await CountAsync(pipeline, Count(bankNote100: 5, at: Noon));
        var latest = Count(bankNote1000: 1, bankNote100: 2, at: LaterNoon);
        await CountAsync(pipeline, latest);
        var cashKept = bills.Sum(b => CashDrawerCalculator.CashKept(b.Total, b.Payments));
        var expected = CashDrawerCalculator.ExpectedCash(new CashDrawerTotals(cashKept, RepaidAmount, FloatAmount, PayoutAmount));

        await pipeline.PollAsync();

        (await DayAsync(pipeline, Store, Day)).Should()
                                               .BeEquivalentTo(new
                                               {
                                                   ExpectedCash = expected,
                                                   CashDifference = CashDrawerCalculator.Difference(latest.CountedTotal, expected)
                                               }, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task CashDrawerDays_ForEveryColumn_HasAComment()
    {
        var comments = await CatalogAsync(
            """
            select coalesce(col_description('"CashDrawerDays"'::regclass, c.ordinal_position), '')
            from information_schema.columns c
            where c.table_name = 'CashDrawerDays'
            """);

        // NotBeEmpty first, so a missing view fails instead of passing on an empty list.
        comments.Should()
                .NotBeEmpty()
                .And.NotContain(string.Empty);
    }

    [Fact]
    public async Task CashDrawerDays_ForTheView_HasAComment()
    {
        var comment = await CatalogAsync("""select coalesce(obj_description('"CashDrawerDays"'::regclass, 'pg_class'), '')""");

        comment.Single().Should()
                        .NotBeEmpty();
    }

    private static PaymentSnapshot[] Cash(decimal amount) => Paid("Cash", amount);

    private static PaymentSnapshot[] Paid(string method, decimal amount) =>
        [new PaymentSnapshot { PaymentId = Guid.NewGuid(), Method = method, Amount = amount }];

    private static Task SellAsync(EventPipeline pipeline, decimal total, PaymentSnapshot[] payments, string storeId = Store) =>
        pipeline.AddInvoiceCompletedAsync(storeId: storeId, businessDate: Day, totalAmount: total, payments: payments, createdAtUtc: Noon);

    // As StoreHub's add handlers stamp a new entry.
    private static T Stamp<T>(T entry) where T : CashDrawerEntry
    {
        entry.Id = Guid.NewGuid();
        entry.StoreId = Store;
        entry.BusinessDate = Day;
        entry.CreatedUtc = Noon;
        entry.LastModifiedUtc = Noon;
        entry.CreatedByUserId = Cashier;
        return entry;
    }

    private static CashCount Count(int bankNote100 = 0, int bankNote1000 = 0, DateTime? at = null) => new()
    {
        Id = Guid.NewGuid(), StoreId = Store, BusinessDate = Day, BankNote1000Count = bankNote1000,
        BankNote100Count = bankNote100, CreatedUtc = at ?? Noon, LastModifiedUtc = at ?? Noon, CreatedByUserId = Cashier
    };

    private static Task SendAsync(EventPipeline pipeline, CashDrawerEntry entry) =>
        pipeline.AddOutboxEventAsync(CashDrawerOutbox.Changed(entry, entry.LastModifiedUtc));

    private static Task CountAsync(EventPipeline pipeline, CashCount count) =>
        pipeline.AddOutboxEventAsync(CashDrawerOutbox.CountAdded(count, count.CreatedUtc));

    private static async Task<CashDrawerDayRow?> DayAsync(EventPipeline pipeline, string storeId, DateOnly day) =>
        (await DaysAsync(pipeline, storeId)).SingleOrDefault(d => d.BusinessDate == day);

    private static Task<List<CashDrawerDayRow>> DaysAsync(EventPipeline pipeline, string storeId) =>
        pipeline.SqlAsync<CashDrawerDayRow>($"""select * from "CashDrawerDays" where "StoreId" = {storeId} order by "BusinessDate" """);

    private async Task<List<string>> CatalogAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(await postgres.CreateDatabaseAsync());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));
        return rows;
    }

    /// <summary>One row of the view, as a dashboard reads it.</summary>
    public sealed class CashDrawerDayRow
    {
        public string StoreId { get; init; } = string.Empty;
        public DateOnly BusinessDate { get; init; }
        public decimal CashSalesTotal { get; init; }
        public decimal DebtRepaidTotal { get; init; }
        public decimal CashFloatsTotal { get; init; }
        public decimal PayoutsTotal { get; init; }
        public decimal ExpectedCash { get; init; }
        public decimal? CountedCash { get; init; }
        public decimal? CashDifference { get; init; }
        public DateTime? LatestCountAtUtc { get; init; }
    }
}
