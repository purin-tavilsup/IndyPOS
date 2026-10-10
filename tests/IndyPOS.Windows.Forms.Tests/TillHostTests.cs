using FluentAssertions;
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
}
