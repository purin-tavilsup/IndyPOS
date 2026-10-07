using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public void AddApplicationServices_WithAnOpenGenericHandler_RegistersNoOpenGenericImplementation()
    {
        var services = new ServiceCollection().AddApplicationServices();

        var openGenerics = services.Where(d => d.ImplementationType is { IsGenericTypeDefinition: true });

        openGenerics.Should()
                    .BeEmpty("a closed service type cannot be built from an open generic implementation");
    }
}
