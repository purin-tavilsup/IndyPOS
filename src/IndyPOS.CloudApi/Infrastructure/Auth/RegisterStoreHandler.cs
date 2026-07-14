using System.Security.Cryptography;
using BCrypt.Net;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Handler for registering new stores with OAuth2 credentials.
/// Generates ClientId/ClientSecret and stores BCrypt-hashed secret.
/// </summary>
public class RegisterStoreHandler : ICommandHandler<RegisterStoreCommand, RegisterStoreResponse>
{
    private readonly CloudDbContext _dbContext;
    private readonly ILogger<RegisterStoreHandler> _logger;

    public RegisterStoreHandler(CloudDbContext dbContext, ILogger<RegisterStoreHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RegisterStoreResponse> HandleAsync(
        RegisterStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if store already exists
        var existingStore = await _dbContext.StoreConfigs
            .FirstOrDefaultAsync(s => s.StoreId == command.StoreId, cancellationToken);

        if (existingStore is not null)
        {
            throw new InvalidOperationException($"Store with ID '{command.StoreId}' already exists.");
        }

        // Generate OAuth2 credentials
        var clientId = $"store_{command.StoreId}";
        var clientSecret = GenerateClientSecret();
        var clientSecretHash = BCrypt.Net.BCrypt.HashPassword(clientSecret);

        // Create store config
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
            ClientSecretHash = clientSecretHash,
            IsActive = true,
            LastModifiedAtUtc = DateTime.UtcNow
        };

        _dbContext.StoreConfigs.Add(storeConfig);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
        // Generate 32 bytes of cryptographically secure random data
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
