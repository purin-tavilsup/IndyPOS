using FluentAssertions;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class NameBasedGuidTests
{
    // RFC 4122's DNS namespace and the widely published v5 vector for "www.example.com".
    private static readonly Guid DnsNamespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
    private static readonly Guid ExampleComV5 = Guid.Parse("2ed6657d-e927-568b-95e1-2665a8aea6a2");

    [Fact]
    public void Create_WithDifferentNames_ReturnsDifferentGuids()
    {
        var first = NameBasedGuid.Create(DnsNamespace, "GENERAL-HARDWARE:1");
        var second = NameBasedGuid.Create(DnsNamespace, "GENERAL-HARDWARE:2");

        first.Should()
             .NotBe(second);
    }

    [Fact]
    public void Create_WithTheSameName_ReturnsTheSameGuid()
    {
        var first = NameBasedGuid.Create(DnsNamespace, "GENERAL-HARDWARE:1");
        var second = NameBasedGuid.Create(DnsNamespace, "GENERAL-HARDWARE:1");

        first.Should()
             .Be(second);
    }

    [Fact]
    public void Create_WithTheRfcExample_ReturnsItsUuid()
    {
        var guid = NameBasedGuid.Create(DnsNamespace, "www.example.com");

        guid.Should()
            .Be(ExampleComV5);
    }
}
