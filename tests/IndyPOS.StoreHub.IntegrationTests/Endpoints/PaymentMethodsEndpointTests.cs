using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// One root for payment methods. Each test makes its own campaign method with a unique code: the test
/// database is shared and never reset, and other tests sell with the seeded methods, so a seeded one
/// is never disabled here.
/// </summary>
[Collection("Integration")]
public class PaymentMethodsEndpointTests : IntegrationTestBase
{
    private const string Route = "/payment-methods";
    private const string CatalogueRoute = "/payment-methods?include=all";
    private const string SeededMethod = "Cash";
    private const string UnknownCode = "NoSuchMethod";

    public PaymentMethodsEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllWithoutAToken_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync(CatalogueRoute);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPaymentMethods_WithAnUnknownInclude_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.GetAsync($"{Route}?include=disabled");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListPaymentMethods_WithAnUnknownInclude_ExplainsInThai()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.GetAsync($"{Route}?include=disabled");

        (await ErrorOfAsync(response)).Should()
                                      .Be("ค่า include ไม่ถูกต้อง: disabled (ใช้ได้เฉพาะ all)");
    }

    // Query values are case-sensitive by convention, and the only caller is our own client.
    [Fact]
    public async Task ListPaymentMethods_WithAnUpperCaseIncludeAll_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.GetAsync($"{Route}?include=ALL");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllAsCashier_ReturnsForbidden()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(CatalogueRoute);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    // payment_methods.manage belongs to SystemAdmin only. A manager can do almost everything else,
    // so this is the likeliest surprise.
    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllAsStoreManager_ReturnsForbidden()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync(CatalogueRoute);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddPaymentMethod_AsCashier_ReturnsForbidden()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync(Route, NewCampaign(UniqueCode()));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePaymentMethod_WithAnUnknownCode_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.PatchAsJsonAsync($"{Route}/{UnknownCode}", Disable());

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllAsAdmin_ReturnsTheWholeCatalogue()
    {
        await AuthenticateAsAdminAsync();
        var disabled = await AddDisabledCampaignAsync();

        var methods = await ListAsync(CatalogueRoute);

        methods.Should()
               .Contain(m => m.Code == disabled && !m.IsEnabled);
    }

    [Fact]
    public async Task ListPaymentMethods_WithoutInclude_ReturnsTheOfferableList()
    {
        await AuthenticateAsAdminAsync();
        var disabled = await AddDisabledCampaignAsync();

        var codes = (await ListAsync(Route)).Select(m => m.Code);

        codes.Should()
             .Contain(SeededMethod)
             .And
             .NotContain(disabled);
    }

    [Fact]
    public async Task ListPaymentMethods_WithAnEmptyInclude_ReturnsTheOfferableList()
    {
        await AuthenticateAsAdminAsync();
        var disabled = await AddDisabledCampaignAsync();

        var codes = (await ListAsync($"{Route}?include=")).Select(m => m.Code);

        codes.Should()
             .Contain(SeededMethod)
             .And
             .NotContain(disabled);
    }

    [Fact]
    public async Task AddPaymentMethod_AsAdmin_AddsTheCampaignToTheCatalogue()
    {
        await AuthenticateAsAdminAsync();
        var code = UniqueCode();

        var response = await Client.PostAsJsonAsync(Route, NewCampaign(code));

        response.EnsureSuccessStatusCode();
        (await ListAsync(CatalogueRoute)).Should()
                                         .Contain(m => m.Code == code);
    }

    [Fact]
    public async Task UpdatePaymentMethod_AsAdmin_DisablesTheMethod()
    {
        await AuthenticateAsAdminAsync();
        var code = UniqueCode();
        (await Client.PostAsJsonAsync(Route, NewCampaign(code))).EnsureSuccessStatusCode();

        var response = await Client.PatchAsJsonAsync($"{Route}/{code}", Disable());

        response.EnsureSuccessStatusCode();
        (await ListAsync(CatalogueRoute)).Should()
                                         .Contain(m => m.Code == code && !m.IsEnabled);
    }

    private static string UniqueCode() => $"Test{Guid.NewGuid():N}";

    private static AddCampaignPaymentMethodRequest NewCampaign(string code) =>
        new(code, DisplayName: "โครงการทดสอบ", DisplayOrder: 99);

    private static UpdatePaymentMethodRequest Disable() =>
        new(IsEnabled: false, DisplayName: null, DisplayOrder: null);

    private async Task<string> AddDisabledCampaignAsync()
    {
        var code = UniqueCode();
        (await Client.PostAsJsonAsync(Route, NewCampaign(code))).EnsureSuccessStatusCode();
        (await Client.PatchAsJsonAsync($"{Route}/{code}", Disable())).EnsureSuccessStatusCode();
        return code;
    }

    private async Task<IReadOnlyList<PaymentMethodDto>> ListAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<PaymentMethodDto>>(JsonOptions))!;
    }

    private static async Task<string?> ErrorOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();
}
