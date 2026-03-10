using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.CreateUser;

/// <summary>
/// Command to create a new cloud user (master user data).
/// The user will be synced to StoreHub via the user sync endpoint.
/// </summary>
public record CreateCloudUserCommand(
    string StoreId,
    string Username,
    string FirstName,
    string LastName,
    int RoleId) : ICommand<CreateCloudUserResponse>;

/// <summary>
/// Response after creating a cloud user.
/// </summary>
public record CreateCloudUserResponse(
    Guid Id,
    string Username,
    string StoreId,
    long Version,
    string Message);

/// <summary>
/// Request DTO for creating a cloud user via the API.
/// </summary>
public record CreateCloudUserRequest(
    string StoreId,
    string Username,
    string FirstName,
    string LastName,
    int RoleId);
