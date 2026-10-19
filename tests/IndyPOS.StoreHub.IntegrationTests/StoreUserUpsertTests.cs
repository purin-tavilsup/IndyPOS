using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// The user sync's upsert on real Postgres, where the unique (store, username) index lives. The
/// shared database is not reset between tests, so each test uses its own usernames and ids.
/// </summary>
[Collection("Integration")]
public class StoreUserUpsertTests : IntegrationTestBase
{
    private const string StoredPasswordHash = "$2a$11$storedhashstoredhashstoredhashstoredhashstoredhashst";

    public StoreUserUpsertTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithAMigratedUser_LinksItInsteadOfAddingAnother()
    {
        var (id, username) = NewUser();
        await SeedAsync(id, username, cloudUserId: null);

        await UpsertAsync(FromCloud(id, username));

        var rows = await UsersNamedAsync(username);
        rows.Should()
            .ContainSingle()
            .Which.CloudUserId.Should()
            .Be(id);
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithAMigratedUser_KeepsItsPassword()
    {
        var (id, username) = NewUser();
        await SeedAsync(id, username, cloudUserId: null);

        await UpsertAsync(FromCloud(id, username));

        (await UsersNamedAsync(username)).Single().PasswordHash.Should()
                                                               .Be(StoredPasswordHash);
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithAMigratedUserDeactivatedInTheCloud_DeactivatesIt()
    {
        var (id, username) = NewUser();
        await SeedAsync(id, username, cloudUserId: null);

        await UpsertAsync(FromCloud(id, username, isActive: false));

        (await UsersNamedAsync(username)).Single().IsActive.Should()
                                                           .BeFalse();
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithAUserSyncedBeforeTheFix_KeepsItsStoreId()
    {
        var (oldRandomId, username) = NewUser();
        var cloudId = Guid.NewGuid();
        await SeedAsync(oldRandomId, username, cloudUserId: cloudId);

        await UpsertAsync(FromCloud(cloudId, username));

        (await UsersNamedAsync(username)).Should()
                                         .ContainSingle()
                                         .Which.Id.Should()
                                         .Be(oldRandomId);
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithAnIdHeldByAUserLinkedToAnotherCloudUser_Throws()
    {
        var (id, username) = NewUser();
        await SeedAsync(id, username, cloudUserId: Guid.NewGuid());

        var act = () => UpsertAsync(FromCloud(id, username + "-other"));

        await act.Should()
                 .ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithAnIdHeldByAUserLinkedToAnotherCloudUser_LeavesThatUserAlone()
    {
        var (id, username) = NewUser();
        var otherCloudId = Guid.NewGuid();
        await SeedAsync(id, username, cloudUserId: otherCloudId);

        await TryUpsertAsync(FromCloud(id, username + "-other"));

        (await UsersNamedAsync(username)).Single().CloudUserId.Should()
                                                              .Be(otherCloudId);
    }

    // A null cloud id would match any unlinked user (CloudUserId IS NULL), e.g. the store's own admin.
    [Fact]
    public async Task UpsertByCloudIdAsync_WithoutACloudId_ThrowsArgumentException()
    {
        var (id, username) = NewUser();

        var act = () => UpsertAsync(WithoutCloudId(FromCloud(id, username)));

        await act.Should()
                 .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithoutACloudId_LeavesAnUnlinkedUserAlone()
    {
        var (localId, localName) = NewUser();
        await SeedAsync(localId, localName, cloudUserId: null);
        var (id, username) = NewUser();

        await TryUpsertAsync(WithoutCloudId(FromCloud(id, username)));

        (await UsersNamedAsync(localName)).Should()
                                          .ContainSingle();
    }

    // One StoreHub serves one store, but a row of another store must never be renamed into this one's user.
    [Fact]
    public async Task UpsertByCloudIdAsync_WithAMigratedUserOfAnotherStore_LeavesItAlone()
    {
        var (id, username) = NewUser();
        await SeedAsync(id, username, cloudUserId: null, storeId: "ANOTHER-STORE");

        await TryUpsertAsync(FromCloud(id, username + "-renamed"));

        (await UsersNamedAsync(username)).Should()
                                         .ContainSingle();
    }

    // The sync upserts a whole run through one DbContext; a refused insert must not ride along
    // into the next user's save.
    [Fact]
    public async Task UpsertByCloudIdAsync_AfterAUserIsRefused_StillStoresTheNextUser()
    {
        var (localId, takenUsername) = NewUser();
        await SeedAsync(localId, takenUsername, cloudUserId: null);
        var (nextId, nextUsername) = NewUser();
        await using var scope = Factory.Services.CreateAsyncScope();
        var repository = new StoreUserRepository(scope.ServiceProvider.GetRequiredService<StoreHubDbContext>());
        var refused = () => repository.UpsertByCloudIdAsync(FromCloud(Guid.NewGuid(), takenUsername));
        await refused.Should()
                     .ThrowAsync<DbUpdateException>();

        await repository.UpsertByCloudIdAsync(FromCloud(nextId, nextUsername));

        (await UsersNamedAsync(nextUsername)).Should()
                                             .ContainSingle();
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithANewUser_StoresItUnderTheCloudId()
    {
        var (cloudId, username) = NewUser();

        await UpsertAsync(FromCloud(cloudId, username));

        (await UsersNamedAsync(username)).Single().Id.Should()
                                                     .Be(cloudId);
    }

    [Fact]
    public async Task UpsertByCloudIdAsync_WithAUserAlreadyLinked_UpdatesItsName()
    {
        var (id, username) = NewUser();
        await SeedAsync(id, username, cloudUserId: id);

        await UpsertAsync(FromCloud(id, username, firstName: "สมชาย"));

        (await UsersNamedAsync(username)).Single().FirstName.Should()
                                                            .Be("สมชาย");
    }

    private static (Guid Id, string Username) NewUser()
    {
        var id = Guid.NewGuid();
        return (id, $"upsert-{id:N}"[..20]);
    }

    // As UserSyncService now builds it: the cloud id is both the store id and CloudUserId.
    private static StoreUser FromCloud(Guid cloudId, string username, bool isActive = true, string firstName = "Somchai") => new()
    {
        Id = cloudId, StoreId = TestStoreIdentityService.TestStoreId, CloudUserId = cloudId, Username = username,
        FirstName = firstName, LastName = "Jaidee", RoleId = 1, IsActive = isActive, CloudVersion = 1,
        LastSyncedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, LastModifiedAtUtc = DateTime.UtcNow,
        PasswordHash = string.Empty, PasswordHashVersion = 0, LegacyUserId = 0
    };

    private async Task SeedAsync(Guid id, string username, Guid? cloudUserId, string storeId = TestStoreIdentityService.TestStoreId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        db.StoreUsers.Add(new StoreUser
        {
            Id = id, StoreId = storeId, CloudUserId = cloudUserId, Username = username,
            FirstName = "Somchai", LastName = "Jaidee", RoleId = 1, IsActive = true,
            PasswordHash = StoredPasswordHash, PasswordHashVersion = 2, LegacyUserId = NextLegacyUserId(),
            CreatedAtUtc = DateTime.UtcNow, LastModifiedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static StoreUser WithoutCloudId(StoreUser user)
    {
        user.CloudUserId = null;
        return user;
    }

    // For tests that check what is left behind, whether or not the upsert was refused.
    private async Task TryUpsertAsync(StoreUser user)
    {
        try
        {
            await UpsertAsync(user);
        }
        catch (Exception ex) when (ex is DbUpdateException or ArgumentException)
        {
        }
    }

    private async Task UpsertAsync(StoreUser user)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        await new StoreUserRepository(db).UpsertByCloudIdAsync(user);
    }

    private async Task<List<StoreUser>> UsersNamedAsync(string username)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().StoreUsers
                          .AsNoTracking()
                          .Where(u => u.Username == username)
                          .ToListAsync();
    }
}
