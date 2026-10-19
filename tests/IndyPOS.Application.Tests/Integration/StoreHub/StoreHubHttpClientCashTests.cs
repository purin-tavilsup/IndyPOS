using System.Globalization;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace IndyPOS.Application.Tests.Integration.StoreHub;

/// <summary>The till's /cash calls against a mocked StoreHub: routes, dates on the wire, errors.</summary>
public class StoreHubHttpClientCashTests
{
    private const string ClosedDayReason = "แก้ไขได้เฉพาะรายการของวันนี้เท่านั้น";
    private static readonly DateOnly Day = new(2026, 10, 18);
    private static readonly Guid EntryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);

    private readonly Mock<HttpMessageHandler> _handler = new();
    private readonly StoreHubHttpClient _sut;

    public StoreHubHttpClientCashTests()
    {
        var http = new HttpClient(_handler.Object) { BaseAddress = new Uri("http://localhost:5000") };
        _sut = new StoreHubHttpClient(http, NullLogger<StoreHubHttpClient>.Instance);
        _sut.SetAuthToken("valid-token");
    }

    [Fact]
    public async Task AddCashPayoutAsync_WhenTheDayIsClosed_ThrowsTheServersReason()
    {
        Respond(HttpStatusCode.Conflict, JsonSerializer.Serialize(new { error = ClosedDayReason }));

        var act = () => _sut.AddCashPayoutAsync(new AddCashPayoutRequest(100m, null, null));

        await act.Should()
                 .ThrowAsync<StoreHubException>()
                 .WithMessage(ClosedDayReason);
    }

    [Fact]
    public async Task GetCashSummaryAsync_WithoutLoggingIn_ThrowsNotAuthenticated()
    {
        _sut.ClearAuthToken();

        var act = () => _sut.GetCashSummaryAsync(null);

        await act.Should()
                 .ThrowAsync<StoreHubException>()
                 .WithMessage("Not authenticated*");
    }

    [Fact]
    public async Task GetCashSummaryAsync_OnAThaiTill_SendsAGregorianDate()
    {
        var request = Respond(HttpStatusCode.OK, SummaryJson());

        await RunUnderThaiCultureAsync(() => _sut.GetCashSummaryAsync(Day));

        request().PathAndQuery.Should()
                              .Be("/cash/summary?businessDate=2026-10-18");
    }

    // Completed ones too: a debt paid off later was still taken that day.
    [Fact]
    public async Task GetPayLaterTakenOnAsync_OnAThaiTill_AsksForThatDaysDebtsOnly()
    {
        var request = Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new GetPayLaterResponse(0m, 0m, 0, 0, []), CamelCase));

        await RunUnderThaiCultureAsync(() => _sut.GetPayLaterTakenOnAsync(Day));

        request().PathAndQuery.Should()
                              .Be("/pay-later?includeCompleted=true&createdOn=2026-10-18");
    }

    [Fact]
    public async Task GetCashSummaryAsync_WithoutADate_SendsNoBusinessDate()
    {
        var request = Respond(HttpStatusCode.OK, SummaryJson());

        await _sut.GetCashSummaryAsync(null);

        request().PathAndQuery.Should()
                              .Be("/cash/summary");
    }

    [Fact]
    public async Task DeleteCashFloatAsync_WhenStoreHubAnswersNoContent_SendsADeleteToTheEntry()
    {
        var request = Respond(HttpStatusCode.NoContent, null);

        await _sut.DeleteCashFloatAsync(EntryId);

        request().Should()
                 .Be(("DELETE", $"/cash/floats/{EntryId}", null));
    }

    [Fact]
    public async Task EditDebtRepaymentAsync_SendsAPutToTheEntry()
    {
        var request = Respond(HttpStatusCode.OK, JsonSerializer.Serialize(
            new DebtRepaymentDto(EntryId, "ลุงสมชาย", 300m, Day, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid(), null), CamelCase));

        await _sut.EditDebtRepaymentAsync(EntryId, new EditDebtRepaymentRequest("ลุงสมชาย", 300m));

        (request().Method, request().PathAndQuery).Should()
                                                  .Be(("PUT", $"/cash/debt-repayments/{EntryId}"));
    }

    [Fact]
    public async Task AddCashPayoutAsync_WithAHardwarePayout_SendsTheCategoryByName()
    {
        var request = Respond(HttpStatusCode.Created, JsonSerializer.Serialize(
            new CashPayoutDto(EntryId, PayoutCategory.Hardware, 100m, null, Day, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid(), null), CamelCase));

        await _sut.AddCashPayoutAsync(new AddCashPayoutRequest(100m, PayoutCategory.Hardware, null));

        request().Body.Should()
                      .Contain("\"category\":\"Hardware\"");
    }

    [Fact]
    public async Task AddCashCountAsync_PostsToCounts()
    {
        var request = Respond(HttpStatusCode.Created, JsonSerializer.Serialize(
            new CashCountDto(EntryId, Day, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1000m, DateTime.UtcNow, Guid.NewGuid()), CamelCase));

        var count = await _sut.AddCashCountAsync(new AddCashCountRequest(1, 0, 0, 0, 0, 0, 0, 0, 0));

        (request().Method, request().PathAndQuery, count.CountedTotal).Should()
                                                                      .Be(("POST", "/cash/counts", 1000m));
    }

    private static string SummaryJson() => JsonSerializer.Serialize(new CashDrawerSummaryDto
    {
        BusinessDate = Day, IsEditable = true, GeneralProductsTotal = 0m, HardwareProductsTotal = 0m,
        CashPaymentsTotal = 0m, NonCashPayments = [], PayLaterGeneralProductsTotal = 0m,
        PayLaterHardwareProductsTotal = 0m, PayoutsTotal = 0m, GeneralPayoutsTotal = 0m,
        HardwarePayoutsTotal = 0m, CashFloatsTotal = 0m, DebtRepaymentsTotal = 0m, ExpectedCash = 0m
    }, CamelCase);

    private static async Task RunUnderThaiCultureAsync(Func<Task> action)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            await action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // Answers every request with the given status and JSON body; returns the last request seen.
    private Func<(string Method, string PathAndQuery, string? Body)> Respond(HttpStatusCode status, string? json)
    {
        (string, string, string?) captured = default;
        _handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
                {
                    var body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
                    captured = (request.Method.Method, request.RequestUri!.PathAndQuery, body);
                    return new HttpResponseMessage(status)
                    {
                        Content = json is null ? null : new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                });

        return () => captured;
    }
}
