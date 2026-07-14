using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.Common.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.UpdateUser;

/// <summary>
/// Handler for updating an existing cloud user.
/// Validates role if provided and always increments version for sync.
/// </summary>
public class UpdateCloudUserCommandHandler(ICloudUserRepository userRepository)
    : ICommandHandler<UpdateCloudUserCommand, UpdateCloudUserResponse>
{
    public async Task<UpdateCloudUserResponse> HandleAsync(
        UpdateCloudUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // Find the user
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return new UpdateCloudUserResponse(
                Found: false,
                Id: null,
                Version: null,
                Message: $"User with ID '{command.Id}' not found.");
        }

        // Validate role if provided
        if (command.RoleId.HasValue && !Enum.IsDefined(typeof(UserRole), command.RoleId.Value))
        {
            throw new InvalidOperationException(
                $"Invalid RoleId '{command.RoleId.Value}'. Valid values: 1 (Cashier), 2 (StoreManager), 3 (SystemAdmin).");
        }

        // Apply changes (only non-null fields)
        if (command.FirstName is not null)
        {
            user.FirstName = command.FirstName;
        }

        if (command.LastName is not null)
        {
            user.LastName = command.LastName;
        }

        if (command.RoleId.HasValue)
        {
            user.RoleId = command.RoleId.Value;
        }

        if (command.IsActive.HasValue)
        {
            user.IsActive = command.IsActive.Value;
        }

        // Always increment version for sync
        var nextVersion = await userRepository.GetNextVersionAsync(user.StoreId, cancellationToken);
        user.Version = nextVersion;
        user.LastModifiedAtUtc = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        return new UpdateCloudUserResponse(
            Found: true,
            Id: user.Id,
            Version: user.Version,
            Message: $"User '{user.Username}' updated successfully. Version {user.Version} will sync to StoreHub.");
    }
}
