using System.Security.Cryptography;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Registers a new store. Writes the CloudStoreConfig row and creates the OpenIddict client the
/// store authenticates as, in one transaction — a store with no credentials, or credentials with
/// no store, are both broken states a partial failure would leave behind.
/// </summary>
public class RegisterStoreHandler : ICommandHandler<RegisterStoreCommand, RegisterStoreResponse>
{
    private readonly CloudDbContext _dbContext;
    private readonly IStoreClientCredentialStore _credentialStore;
    private readonly ILogger<RegisterStoreHandler> _logger;

    public RegisterStoreHandler(
        CloudDbContext dbContext,
        IStoreClientCredentialStore credentialStore,
        ILogger<RegisterStoreHandler> logger)
    {
        _dbContext = dbContext;
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public async Task<RegisterStoreResponse> HandleAsync(
        RegisterStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingStore = await _dbContext.StoreConfigs
            .FirstOrDefaultAsync(s => s.StoreId == command.StoreId, cancellationToken);

        if (existingStore is not null)
        {
            throw new InvalidOperationException($"Store with ID '{command.StoreId}' already exists.");
        }

        var clientId = $"store_{command.StoreId}";
        var clientSecret = GenerateClientSecret();

        var storeConfig = new CloudStoreConfig
        {
            StoreId = command.StoreId,
            StoreName = command.StoreName,
            StoreFullName = command.StoreFullName,
            AddressLine1 = command.AddressLine1,
            AddressLine2 = command.AddressLine2,
            PhoneNumber = command.PhoneNumber,
            PrinterName = command.PrinterName,
            ClientId = clientId,
            IsActive = true,
            LastModifiedAtUtc = DateTime.UtcNow
        };

        // All-or-nothing. The Npgsql retrying execution strategy forbids a user-initiated
        // BeginTransactionAsync unless the whole unit runs inside strategy.ExecuteAsync, so the retry
        // can replay it atomically. EF InMemory ignores the transaction, so this guarantee is proved
        // against real PostgreSQL (the plan's end-to-end task), not by a unit test.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // The strategy replays this whole lambda on a transient fault without resetting the change
            // tracker. Entities Added by a failed attempt (the StoreConfig row, and the OpenIddict
            // application the credential store adds) would otherwise linger and be re-added on retry,
            // colliding on the unique ClientId. Start each attempt from a clean tracker so a retry can
            // actually recover instead of failing with a duplicate.
            _dbContext.ChangeTracker.Clear();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            _dbContext.StoreConfigs.Add(storeConfig);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _credentialStore.CreateAsync(clientId, clientSecret, command.StoreName, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        _logger.LogInformation(
            "Registered store {StoreId} with ClientId {ClientId}",
            command.StoreId,
            clientId);

        return new RegisterStoreResponse(
            StoreId: command.StoreId,
            ClientId: clientId,
            ClientSecret: clientSecret,
            Message: "Store registered successfully. Save the ClientSecret securely - it won't be shown again!");
    }

    private static string GenerateClientSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
