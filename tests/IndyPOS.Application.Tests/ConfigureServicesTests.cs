using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndyPOS.Application.Tests;

/// <summary>
/// The WinForms till registers every Application handler through this scan. Building the provider is
/// the first thing the till does, so a registration the container cannot build stops the till before
/// any window opens: an open generic handler did exactly that, with every other test still green.
/// </summary>
public class ConfigureServicesTests
{
    [Fact]
    public void AddApplicationServices_WithTheWholeAssemblyScanned_BuildsAServiceProvider()
    {
        var services = new ServiceCollection().AddApplicationServices();

        var build = () => services.BuildServiceProvider();

        build.Should()
             .NotThrow();
    }

    // The till's host registers logging (ILogger<> -> Logger<>, a valid open generic) before this runs.
    // Only the scan's own broken registrations may go.
    [Fact]
    public void AddApplicationServices_WithLoggingAlreadyRegistered_KeepsTheGenericLogger()
    {
        var services = new ServiceCollection().AddLogging()
                                              .AddApplicationServices();

        using var provider = services.BuildServiceProvider();

        provider.GetService<ILogger<ConfigureServicesTests>>()
                .Should()
                .NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_WithAnOpenGenericHandler_RegistersNoOpenGenericImplementation()
    {
        var services = new ServiceCollection().AddApplicationServices();

        var openGenerics = services.Where(d => d.ImplementationType is { IsGenericTypeDefinition: true });

        openGenerics.Should()
                    .BeEmpty("a closed service type cannot be built from an open generic implementation");
    }
}
