using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.Common.Validation;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Nokpirab;
using Npgsql;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Registers a new store: CloudApi's record of it, and its client in Cloud.Auth, which alone holds the secret.
/// </summary>
/// <remarks>
/// Two services, so no shared transaction. The order makes every failure leave nothing behind: CloudApi's row is
/// inserted first inside a transaction, so a concurrent registration of the same store fails on the unique index
/// before Auth is asked; then Auth creates the client; then the transaction commits. Auth down: roll back, nothing in
/// CloudApi. After Auth created the client, any failure (a cancellation included) checks whether CloudApi's row landed
/// anyway: if it did, the store is registered and the secret is returned; if not, the client is deleted in Auth
/// again. A stray client can remain only if that delete fails too (logged with the store id) or Auth created the
/// client but its answer was lost; its secret was never shown, and registering the store again replaces it.
/// </remarks>
public class RegisterStoreHandler(CloudDbContext dbContext, IAuthStoreClients authStoreClients, ILogger<RegisterStoreHandler> logger)
    : ICommandHandler<RegisterStoreCommand, RegisterStoreResponse>
{
    public async Task<RegisterStoreResponse> HandleAsync(RegisterStoreCommand command, CancellationToken cancellationToken = default)
    {
        if (!StoreIds.IsValid(command.StoreId))
            throw new ArgumentException($"A store id must be {StoreIds.Rule}.", nameof(command));

        if (await dbContext.StoreConfigs.AnyAsync(s => s.StoreId == command.StoreId, cancellationToken))
            throw AlreadyExists(command.StoreId);

        StoreClientCredentials? credentials = null;
        try
        {
            // The Npgsql retrying strategy allows a user transaction only inside ExecuteAsync, which may replay the
            // lambda; each attempt starts from a clean change tracker so a retry does not re-add a failed row.
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                dbContext.ChangeTracker.Clear();
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                dbContext.StoreConfigs.Add(NewStoreConfig(command));
                await dbContext.SaveChangesAsync(cancellationToken);

                credentials ??= await authStoreClients.CreateAsync(command.StoreId, command.StoreName, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
        }
        catch (DbUpdateException ex) when (credentials is null && ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent registration slipped past the check above and lost the race to the unique StoreId/ClientId
            // index, before Auth was asked. Same conflict as the check (409), not a 500.
            throw AlreadyExists(command.StoreId, ex);
        }
        catch (Exception ex) when (credentials is not null)
        {
            // The commit's outcome is unknown: it may have landed and then reported a failure (a dropped connection,
            // or a retry that hit the unique index on our own row). If the row is there, the store is registered.
            if (await RegisteredAsync(command.StoreId))
            {
                logger.LogWarning(ex, "Registering store {StoreId} reported a failure after its commit landed; it is registered", command.StoreId);
                return Registered(command.StoreId, credentials);
            }

            await DeleteClientAfterFailedCommitAsync(command.StoreId, ex);
            throw;
        }

        logger.LogInformation("Registered store {StoreId} with ClientId {ClientId}", command.StoreId, credentials!.ClientId);
        return Registered(command.StoreId, credentials);
    }

    private static RegisterStoreResponse Registered(string storeId, StoreClientCredentials credentials) => new(
        StoreId: storeId,
        ClientId: credentials.ClientId,
        ClientSecret: credentials.ClientSecret,
        Message: "Store registered successfully. Save the ClientSecret securely - it won't be shown again!");

    /// <summary>Read on a fresh query, never cancelled: it decides whether Auth's client stays.</summary>
    private async Task<bool> RegisteredAsync(string storeId)
    {
        dbContext.ChangeTracker.Clear();
        return await dbContext.StoreConfigs.AsNoTracking().AnyAsync(s => s.StoreId == storeId, CancellationToken.None);
    }

    private async Task DeleteClientAfterFailedCommitAsync(string storeId, Exception commitFailure)
    {
        logger.LogWarning(commitFailure, "Registering store {StoreId} failed after Auth created its client; deleting the client", storeId);
        try
        {
            await authStoreClients.DeleteAsync(storeId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Store {StoreId} has a client in Auth but no record in CloudApi; registering it again replaces the client", storeId);
        }
    }

    private static CloudStoreConfig NewStoreConfig(RegisterStoreCommand command) => new()
    {
        StoreId = command.StoreId,
        StoreName = command.StoreName,
        StoreFullName = command.StoreFullName,
        AddressLine1 = command.AddressLine1,
        AddressLine2 = command.AddressLine2,
        PhoneNumber = command.PhoneNumber,
        PrinterName = command.PrinterName,
        ClientId = $"store_{command.StoreId}",
        IsActive = true,
        LastModifiedAtUtc = DateTime.UtcNow
    };

    private static InvalidOperationException AlreadyExists(string storeId, Exception? inner = null) =>
        new($"Store with ID '{storeId}' already exists.", inner);
}
