using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class DebtRepaymentEndpointsTests : IntegrationTestBase
{
    public DebtRepaymentEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private async Task<DebtRepaymentDto> AddDebtRepaymentAsync(decimal amount = 300m)
    {
        var response = await Client.PostAsJsonAsync("/cash/debt-repayments", new { customerName = "ลุงสมชาย", amount });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DebtRepaymentDto>(JsonOptions))!;
    }

    [Fact]
    public async Task AddDebtRepayment_WithBlankCustomerName_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/debt-repayments", new { customerName = "   ", amount = 300m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditDebtRepayment_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PutAsJsonAsync($"/cash/debt-repayments/{Guid.NewGuid()}", new { customerName = "ลุงสมชาย", amount = 500m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditDebtRepayment_AfterDelete_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();
        var repayment = await AddDebtRepaymentAsync();
        await Client.DeleteAsync($"/cash/debt-repayments/{repayment.Id}");

        var response = await Client.PutAsJsonAsync($"/cash/debt-repayments/{repayment.Id}", new { customerName = "ลุงสมชาย", amount = 500m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDebtRepayment_WhenDeletedTwice_ReturnsNoContentTheSecondTime()
    {
        await AuthenticateAsCashierAsync();
        var repayment = await AddDebtRepaymentAsync();
        await Client.DeleteAsync($"/cash/debt-repayments/{repayment.Id}");

        var second = await Client.DeleteAsync($"/cash/debt-repayments/{repayment.Id}");

        second.StatusCode.Should()
                         .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteDebtRepayment_ThenList_ExcludesIt()
    {
        await AuthenticateAsCashierAsync();
        var repayment = await AddDebtRepaymentAsync();

        await Client.DeleteAsync($"/cash/debt-repayments/{repayment.Id}");
        var list = await Client.GetFromJsonAsync<List<DebtRepaymentDto>>("/cash/debt-repayments", JsonOptions);

        list.Should()
            .NotContain(r => r.Id == repayment.Id);
    }
}
