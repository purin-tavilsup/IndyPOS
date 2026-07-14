using Xunit;

namespace IndyPOS.Migration.Tests;

/// <summary>
/// Test collection that disables parallelism for migration tests.
/// </summary>
[CollectionDefinition("Migration")]
public class MigrationTestCollection : ICollectionFixture<MigrationTestFixture>
{
}
