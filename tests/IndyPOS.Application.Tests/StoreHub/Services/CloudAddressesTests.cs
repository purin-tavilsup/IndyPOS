using FluentAssertions;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>Tokens come from Cloud.Auth; events, users and heartbeats go to CloudApi. Two addresses, not one.</summary>
public class CloudAddressesTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value)).Build();

    [Fact]
    public void Auth_WithBothServicesConfigured_IsTheAuthService()
    {
        var configuration = Config(("services:cloud-auth:https:0", "https://auth.example/"), ("services:cloud-api:https:0", "https://api.example/"));

        CloudAddresses.Auth(configuration).Should().Be(new Uri("https://auth.example/"));
    }

    [Fact]
    public void CloudApi_WithBothServicesConfigured_IsTheApi()
    {
        var configuration = Config(("services:cloud-auth:https:0", "https://auth.example/"), ("services:cloud-api:https:0", "https://api.example/"));

        CloudAddresses.CloudApi(configuration).Should().Be(new Uri("https://api.example/"));
    }

    [Fact]
    public void Auth_WithOnlyHttp_FallsBackToIt() =>
        CloudAddresses.Auth(Config(("services:cloud-auth:http:0", "http://auth.example/"))).Should().Be(new Uri("http://auth.example/"));

    [Fact]
    public void Auth_WithNothingConfigured_UsesServiceDiscovery() =>
        CloudAddresses.Auth(Config()).Should().Be(new Uri("https+http://cloud-auth"));
}
