using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;

/// <summary>
/// Command to register a new store with OAuth2 credentials.
/// </summary>
public record RegisterStoreCommand(
    string StoreId,
    string StoreName,
    string StoreFullName,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? PhoneNumber = null,
    string? PrinterName = null) : ICommand<RegisterStoreResponse>;

/// <summary>
/// Response containing the generated OAuth2 credentials.
/// The ClientSecret is only shown once during registration!
/// </summary>
public record RegisterStoreResponse(
    string StoreId,
    string ClientId,
    string ClientSecret,
    string Message);

/// <summary>
/// Request DTO for the registration endpoint.
/// </summary>
public record RegisterStoreRequest(
    string StoreId,
    string StoreName,
    string StoreFullName,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? PhoneNumber = null,
    string? PrinterName = null);
