using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Infrastructure.Services;
using IndyPOS.StoreProfiles;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.Application.Tests.StoreProfiles;

// The till reads this file with its own JsonService; a file it cannot read gives an empty receipt
// header without any error.
public class StoreProfileFilesTests
{
    [Fact]
    public async Task StoreConfigurationJson_ReadByTheTill_GivesTheStoresReceiptHeader()
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory("indypos-storeconfig-").FullName, "StoreConfiguration.json");
        await File.WriteAllTextAsync(path, StoreProfileFiles.StoreConfigurationJson(Profiles.MimyShop));

        var configuration = await new JsonService().ReadFromFileAsync<StoreConfiguration>(path);

        configuration.Should()
                     .BeEquivalentTo(new
                     {
                         StoreName = Profiles.MimyShop.Name,
                         StoreFullName = Profiles.MimyShop.FullName,
                         StorePhoneNumber = Profiles.MimyShop.Phone,
                         Code = Profiles.MimyShop.Code
                     });
    }
}
