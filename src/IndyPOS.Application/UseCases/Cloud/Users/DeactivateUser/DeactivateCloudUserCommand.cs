using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.DeactivateUser;

/// <summary>
/// Command to deactivate a cloud user (soft delete).
/// Sets IsActive to false and increments version for sync.
/// </summary>
public record DeactivateCloudUserCommand(Guid Id) : ICommand<DeactivateCloudUserResponse>;

/// <summary>
/// Response after deactivating a cloud user.
/// </summary>
public record DeactivateCloudUserResponse(
    bool Found,
    Guid? Id,
    long? Version,
    string Message);
