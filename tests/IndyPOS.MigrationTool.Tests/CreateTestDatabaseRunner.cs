using IndyPOS.MigrationTool.Tests.TestData;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Test that creates the lite test database when run manually.
/// Run with: dotnet test --filter "CreateTestDatabase_FromRealData"
/// </summary>
public class CreateTestDatabaseRunner
{
    [Fact(Skip = "Run manually to create test database")]
    public async Task CreateTestDatabase_FromRealData()
    {
        await CreateTestDatabase.CreateAsync();
    }
}
