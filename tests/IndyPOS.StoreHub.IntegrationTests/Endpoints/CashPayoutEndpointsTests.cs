using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashPayoutEndpointsTests : IntegrationTestBase
{
    public CashPayoutEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private async Task<CashPayoutDto> AddPayoutAsync(decimal amount = 150m)
    {
        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount, category = "Hardware", description = "ตะปู" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CashPayoutDto>(JsonOptions))!;
    }

    private async Task<Guid> SeedYesterdaysPayoutAsync()
    {
        await using var db = GetDbContext();
        var payout = new CashPayout
        {
            Id = Guid.NewGuid(), StoreId = "test-store", Amount = 80m,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
            CreatedUtc = DateTime.UtcNow.AddDays(-2), LastModifiedUtc = DateTime.UtcNow.AddDays(-2),
            CreatedByUserId = Guid.NewGuid()
        };
        db.CashPayouts.Add(payout);
        await db.SaveChangesAsync();
        return payout.Id;
    }

    [Fact]
    public async Task AddPayout_WithZeroAmount_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 0m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPayout_WithZeroAmount_ReturnsAThaiError()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 0m });

        (await response.Content.ReadAsStringAsync()).Should()
                                                    .Contain("จำนวนเงิน");
    }

    [Fact]
    public async Task AddPayout_WithUndefinedNumericCategory_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m, category = 7 });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPayout_WithUnknownCategoryName_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m, category = "Food" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditPayout_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PutAsJsonAsync($"/cash/payouts/{Guid.NewGuid()}", new { amount = 10m, category = "General" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditPayout_AfterDelete_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();
        var payout = await AddPayoutAsync();
        await Client.DeleteAsync($"/cash/payouts/{payout.Id}");

        var response = await Client.PutAsJsonAsync($"/cash/payouts/{payout.Id}", new { amount = 10m, category = "General" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditPayout_FromAPastDay_ReturnsConflict()
    {
        await AuthenticateAsCashierAsync();
        var id = await SeedYesterdaysPayoutAsync();

        var response = await Client.PutAsJsonAsync($"/cash/payouts/{id}", new { amount = 10m, category = "General" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletePayout_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.DeleteAsync($"/cash/payouts/{Guid.NewGuid()}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePayout_Twice_ReturnsNoContentBothTimes()
    {
        await AuthenticateAsCashierAsync();
        var payout = await AddPayoutAsync();

        var first = await Client.DeleteAsync($"/cash/payouts/{payout.Id}");
        var second = await Client.DeleteAsync($"/cash/payouts/{payout.Id}");

        first.StatusCode.Should()
                        .Be(HttpStatusCode.NoContent);
        second.StatusCode.Should()
                         .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ListPayouts_WithMalformedDate_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/cash/payouts?businessDate=2026-13-01");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPayout_AsCashier_RecordsTheTokenUserNotABodyField()
    {
        await AuthenticateAsCashierAsync();
        var me = await Client.GetFromJsonAsync<Dictionary<string, string>>("/auth/me", JsonOptions);
        var forged = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m, createdByUserId = forged, userId = forged });
        var payout = await response.Content.ReadFromJsonAsync<CashPayoutDto>(JsonOptions);

        payout!.CreatedByUserId.Should()
                               .Be(Guid.Parse(me!["userId"]));
    }

    [Fact]
    public async Task AddPayout_AsCashier_ReturnsCreated()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 150m, category = "Hardware" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddPayout_AsCashier_ReturnsCategoryByName()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 150m, category = "Hardware" });

        (await response.Content.ReadAsStringAsync()).Should()
                                                    .Contain("\"Hardware\"");
    }

    [Fact]
    public async Task AddPayout_AsCashier_WritesTheOutboxEventWithTheRow()
    {
        await AuthenticateAsCashierAsync();

        var payout = await AddPayoutAsync();

        await using var db = GetDbContext();
        (await db.OutboxEvents.AnyAsync(e => e.Type == "CashPayoutChanged" && e.PayloadJson.Contains(payout.Id.ToString())))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task DeletePayout_ThenList_ExcludesIt()
    {
        await AuthenticateAsCashierAsync();
        var payout = await AddPayoutAsync();

        await Client.DeleteAsync($"/cash/payouts/{payout.Id}");
        var list = await Client.GetFromJsonAsync<List<CashPayoutDto>>("/cash/payouts", JsonOptions);

        list.Should()
            .NotContain(p => p.Id == payout.Id);
    }
}
