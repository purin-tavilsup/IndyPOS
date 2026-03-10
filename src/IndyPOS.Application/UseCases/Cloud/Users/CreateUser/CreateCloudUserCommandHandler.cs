using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.Common.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.CreateUser;

/// <summary>
/// Handler for creating a new cloud user.
/// Validates store exists, username uniqueness, and role validity.
/// </summary>
public class CreateCloudUserCommandHandler(ICloudUserRepository userRepository)
    : ICommandHandler<CreateCloudUserCommand, CreateCloudUserResponse>
{
    public async Task<CreateCloudUserResponse> HandleAsync(
        CreateCloudUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate store exists
        var storeExists = await userRepository.StoreExistsAsync(command.StoreId, cancellationToken);
        if (!storeExists)
        {
            throw new InvalidOperationException($"Store '{command.StoreId}' does not exist. Register the store first.");
        }

        // Validate username is unique within the store
        var existingUser = await userRepository.GetByUsernameAsync(
            command.StoreId,
            command.Username,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException($"Username '{command.Username}' already exists in store '{command.StoreId}'.");
        }

        // Validate role is valid (1=Cashier, 2=StoreManager, 3=SystemAdmin)
        if (!Enum.IsDefined(typeof(UserRole), command.RoleId))
        {
            throw new InvalidOperationException($"Invalid RoleId '{command.RoleId}'. Valid values: 1 (Cashier), 2 (StoreManager), 3 (SystemAdmin).");
        }

        // Get next version for this store
        var version = await userRepository.GetNextVersionAsync(command.StoreId, cancellationToken);

        // Create the user
        var now = DateTime.UtcNow;
        var user = new CloudUserEntity
        {
            Id = Guid.NewGuid(),
            StoreId = command.StoreId,
            Username = command.Username,
            FirstName = command.FirstName,
            LastName = command.LastName,
            RoleId = command.RoleId,
            IsActive = true,
            Version = version,
            CreatedAtUtc = now,
            LastModifiedAtUtc = now
        };

        await userRepository.AddAsync(user, cancellationToken);

        return new CreateCloudUserResponse(
            user.Id,
            user.Username,
            user.StoreId,
            user.Version,
            $"User '{user.Username}' created successfully. Version {user.Version} will sync to StoreHub.");
    }
}
