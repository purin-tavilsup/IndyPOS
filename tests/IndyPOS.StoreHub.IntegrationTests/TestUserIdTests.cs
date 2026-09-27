using FluentAssertions;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

public class TestUserIdTests
{
    // Well past the point where ~9,000 random ids would repeat (birthday bound ~ 112).
    private const int CallsInAFullRun = 1000;

    [Fact]
    public void NextLegacyUserId_WhenCalledForAFullRun_NeverRepeats()
    {
        var ids = Enumerable.Range(0, CallsInAFullRun)
                            .Select(_ => IdSource.Next())
                            .ToList();

        ids.Should()
           .OnlyHaveUniqueItems();
    }

    private sealed class IdSource(StoreHubWebApplicationFactory factory) : IntegrationTestBase(factory)
    {
        public static int Next() => NextLegacyUserId();
    }
}
