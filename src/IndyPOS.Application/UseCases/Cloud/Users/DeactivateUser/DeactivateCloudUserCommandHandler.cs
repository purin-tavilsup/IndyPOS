using IndyPOS.Application.Abstractions.Cloud.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.DeactivateUser;

/// <summary>
/// Handler for deactivating a cloud user (soft delete).
/// Sets IsActive to false and increments version for sync.
/// </summary>
public class DeactivateCloudUserCommandHandler(ICloudUserRepository userRepository)
    : ICommandHandler<DeactivateCloudUserCommand, DeactivateCloudUserResponse>
{
    public async Task<DeactivateCloudUserResponse> HandleAsync(
        DeactivateCloudUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // Find the user
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return new DeactivateCloudUserResponse(
                Found: false,
                Id: null,
                Version: null,
                Message: $"User with ID '{command.Id}' not found.");
        }

        // Check if already deactivated
        if (!user.IsActive)
        {
            return new DeactivateCloudUserResponse(
                Found: true,
                Id: user.Id,
                Version: user.Version,
                Message: $"User '{user.Username}' is already deactivated.");
        }

        // Deactivate
        user.IsActive = false;

        // Increment version for sync (critical!)
        var nextVersion = await userRepository.GetNextVersionAsync(user.StoreId, cancellationToken);
        user.Version = nextVersion;
        user.LastModifiedAtUtc = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        return new DeactivateCloudUserResponse(
            Found: true,
            Id: user.Id,
            Version: user.Version,
            Message: $"User '{user.Username}' deactivated. Version {user.Version} will sync to StoreHub.");
    }
}
