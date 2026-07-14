using IndyPOS.Infrastructure.Services.StoreHub;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class AdminPasswordGeneratorTests
{
    [Fact]
    public void Generate_ProducesUnambiguous14CharPassword()
    {
        var pw = AdminPasswordGenerator.Generate();

        Assert.Equal(14, pw.Length);
        Assert.DoesNotContain(pw, c => "0O1lI".Contains(c));
    }

    [Fact]
    public void Generate_ProducesDifferentValuesEachCall()
    {
        Assert.NotEqual(AdminPasswordGenerator.Generate(), AdminPasswordGenerator.Generate());
    }
}
