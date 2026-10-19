using FluentAssertions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Infrastructure.Services;
using IndyPOS.Presentation.CashDrawer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests;

/// <summary>
/// The Aspire AppHost starts the till as Development, where the default host validates every
/// registration on build. A service the till registers but cannot construct then stops it before any
/// window opens, while Production, which never resolves that service, starts fine.
/// </summary>
public class TillHostTests
{
    [Fact]
    public void CreateBuilder_WhenBuiltInDevelopment_BuildsTheHost()
    {
        var builder = TillHost.CreateBuilder()
                              .UseEnvironment(Environments.Development);

        var build = () => builder.Build()
                                 .Dispose();

        build.Should()
             .NotThrow();
    }

    // No screen opens the cash drawer until D4, so only the registration can show it is wired.
    [Fact]
    public void CreateBuilder_WhenBuilt_RegistersTheCashDrawerFactory()
    {
        IServiceCollection? services = null;
        var builder = TillHost.CreateBuilder()
                              .UseEnvironment(Environments.Development)
                              .ConfigureServices((_, s) => services = s);

        builder.Build()
               .Dispose();

        services!.Should()
                 .Contain(d => d.ServiceType == typeof(CashDrawerViewModelFactory));
    }

#if DEBUG
    // The till's whole print gate: a DEBUG build must never reach the thermal printer. Checks which
    // registration wins rather than resolving it, which would read the machine's store configuration.
    [Fact]
    public void CreateBuilder_InADebugBuild_SavesReceiptsAsImages()
    {
        IServiceCollection? services = null;
        var builder = TillHost.CreateBuilder()
                              .UseEnvironment(Environments.Development)
                              .ConfigureServices((_, s) => services = s);

        builder.Build()
               .Dispose();

        services!.Last(d => d.ServiceType == typeof(IReceiptPrinterService)).ImplementationType.Should()
                                                                                              .Be<ReceiptImagePrinter>();
    }
#endif
}
