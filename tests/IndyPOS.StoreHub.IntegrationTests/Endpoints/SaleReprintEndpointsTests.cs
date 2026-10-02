using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class SaleReprintEndpointsTests : IntegrationTestBase
{
    public SaleReprintEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static DateTime TwoDaysAgoUtc => DateTime.UtcNow.AddDays(-2);

    private Task<HttpResponseMessage> ReprintAsync(Guid invoiceId) =>
        Client.PostAsync($"/sales/{invoiceId}/reprints", content: null);

    private async Task<int> ReprintRowsForAsync(Guid invoiceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        return await db.InvoiceReprints.CountAsync(r => r.InvoiceId == invoiceId);
    }

    private async Task<int> ReprintEventsForAsync(Guid invoiceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        return await db.OutboxEvents.CountAsync(e =>
            e.Type == InvoiceReprintOutbox.InvoiceReprinted && e.PayloadJson.Contains(invoiceId.ToString()));
    }

    [Fact]
    public async Task Reprint_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await ReprintAsync(Guid.NewGuid());

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reprint_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutUserId(UserRole.Cashier));

        var response = await ReprintAsync(today.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reprint_WithAnUnknownInvoice_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await ReprintAsync(Guid.NewGuid());

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reprint_AsCashierForAnotherDaysBill_ReturnsNotFound()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        var response = await ReprintAsync(old.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reprint_AsCashierForAnotherDaysBill_WritesNoRow()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(old.Id);

        (await ReprintRowsForAsync(old.Id)).Should()
                                           .Be(0);
    }

    [Fact]
    public async Task Reprint_AsCashierForAnotherDaysBill_WritesNoEvent()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(old.Id);

        (await ReprintEventsForAsync(old.Id)).Should()
                                             .Be(0);
    }

    [Fact]
    public async Task Reprint_AsCashierForTodaysBill_ReturnsCreated()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();

        var response = await ReprintAsync(today.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Reprint_AsCashierForTodaysBill_WritesExactlyOneRow()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(today.Id);

        (await ReprintRowsForAsync(today.Id)).Should()
                                             .Be(1);
    }

    [Fact]
    public async Task Reprint_AsCashierForTodaysBill_WritesExactlyOneEvent()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(today.Id);

        (await ReprintEventsForAsync(today.Id)).Should()
                                               .Be(1);
    }

    [Fact]
    public async Task Reprint_AsCashier_RecordsTheTokenUserAsReprinter()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();
        var me = (await Client.GetFromJsonAsync<MeBody>("/auth/me", JsonOptions))!;

        var result = await (await ReprintAsync(today.Id)).Content.ReadFromJsonAsync<InvoiceReprintResultDto>(JsonOptions);

        result!.Reprint.CreatedByUserId.Should()
                                       .Be(Guid.Parse(me.UserId));
    }

    [Fact]
    public async Task Reprint_AsManagerForAnotherDaysBill_ReturnsCreated()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsManagerAsync();

        var response = await ReprintAsync(old.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    private sealed record MeBody(string UserId);
}
