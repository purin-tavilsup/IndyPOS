using FluentAssertions;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Development once built the cloud database with EnsureCreated, which leaves tables but no migration
/// history; migrating it replays the first migration and fails. Development refuses it with a message.
/// </summary>
public class EnsureCreatedDatabaseTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    [Fact]
    public async Task RejectEnsureCreatedDatabase_WithAnEnsureCreatedDatabase_ThrowsNamingTheCause()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateEmptyDatabaseAsync());
        await db.Database.EnsureCreatedAsync();

        var act = () => CloudDbContextExtensions.RejectEnsureCreatedDatabaseAsync(db);

        await act.Should()
                 .ThrowAsync<InvalidOperationException>()
                 .WithMessage("*EnsureCreated*");
    }

    [Fact]
    public async Task RejectEnsureCreatedDatabase_WithAMigratedDatabase_DoesNotThrow()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());

        var act = () => CloudDbContextExtensions.RejectEnsureCreatedDatabaseAsync(db);

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task RejectEnsureCreatedDatabase_WithAnEmptyDatabase_DoesNotThrow()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateEmptyDatabaseAsync());

        var act = () => CloudDbContextExtensions.RejectEnsureCreatedDatabaseAsync(db);

        await act.Should()
                 .NotThrowAsync();
    }
}
