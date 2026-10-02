using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Spec §5: a cashier sees today's cash figures but never another day's. Since #97 every /cash read
/// took ?businessDate= with no check, so any cashier could read any past day's sales totals, counted
/// cash, payouts, floats and debt repayments. Each read is listed so a route cannot slip the rule.
/// </summary>
[Collection("Integration")]
public class CashPastDayAccessTests : IntegrationTestBase
{
    public CashPastDayAccessTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    public static TheoryData<string> CashReads =>
    [
        "/cash/summary",
        "/cash/counts",
        "/cash/payouts",
        "/cash/floats",
        "/cash/debt-repayments",
    ];

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    private static string For(string route, DateOnly day) => $"{route}?businessDate={day:yyyy-MM-dd}";

    private sealed record ErrorBody(string Error);

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsCashierForAPastDate_ReturnsForbidden(string route)
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For(route, Today.AddDays(-1)));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsCashierForAFutureDate_ReturnsForbidden(string route)
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For(route, Today.AddDays(1)));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CashRead_AsCashierForAPastDate_ReturnsAThaiError()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For("/cash/counts", Today.AddDays(-1)));

        (await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions))!.Error.Should()
                                                                          .Contain("วันนี้");
    }

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsCashierForToday_ReturnsOk(string route)
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For(route, Today));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsManagerForAPastDate_ReturnsOk(string route)
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync(For(route, Today.AddDays(-1)));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
