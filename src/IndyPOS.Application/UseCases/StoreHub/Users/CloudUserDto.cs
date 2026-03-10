namespace IndyPOS.Application.UseCases.StoreHub.Users;

/// <summary>
/// DTO representing a user from CloudApi for sync.
/// </summary>
public record CloudUserDto(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    int RoleId,
    bool IsActive,
    long Version);

/// <summary>
/// Response from CloudApi GET /master/users/{storeId} endpoint.
/// </summary>
public record CloudUserSyncResponse(
    int Count,
    List<CloudUserDto> Users,
    long MaxVersion,
    DateTime Timestamp);
