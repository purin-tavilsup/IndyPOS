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

        // All-or-nothing. EF InMemory ignores this transaction; the guarantee is verified against
        // real PostgreSQL (see the plan's end-to-end task), not by a unit test.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.StoreConfigs.Add(storeConfig);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _credentialStore.CreateAsync(clientId, clientSecret, command.StoreName, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

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
