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
/// before Auth is asked; then Auth creates the client; then the transaction commits. Auth down: roll back, nothing
/// anywhere. Commit failed after Auth: delete the client in Auth again, so no enabled client is left for a store
/// CloudApi does not have. If that delete fails too, it is logged with the store id: the one way a stray client can
/// remain, and a later registration of the same store replaces it.
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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent registration slipped past the check above and lost the race to the unique StoreId/ClientId
            // index, before Auth was asked. Same conflict as the check (409), not a 500.
            throw AlreadyExists(command.StoreId, ex);
        }
        catch (Exception ex) when (credentials is not null && ex is not OperationCanceledException)
        {
            await DeleteClientAfterFailedCommitAsync(command.StoreId, ex);
            throw;
        }

        logger.LogInformation("Registered store {StoreId} with ClientId {ClientId}", command.StoreId, credentials!.ClientId);

        return new RegisterStoreResponse(
            StoreId: command.StoreId,
            ClientId: credentials.ClientId,
            ClientSecret: credentials.ClientSecret,
            Message: "Store registered successfully. Save the ClientSecret securely - it won't be shown again!");
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
