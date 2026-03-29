using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// Test collection that disables parallelism for integration tests.
/// All tests in this collection share the same database and run sequentially.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<StoreHubWebApplicationFactory>
{
}
