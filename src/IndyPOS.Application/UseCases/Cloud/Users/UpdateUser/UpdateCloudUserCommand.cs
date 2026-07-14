using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.UpdateUser;

/// <summary>
/// Command to update an existing cloud user.
/// Only non-null fields will be updated.
/// Version is always incremented for sync.
/// </summary>
public record UpdateCloudUserCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    int? RoleId,
    bool? IsActive) : ICommand<UpdateCloudUserResponse>;

/// <summary>
/// Response after updating a cloud user.
/// </summary>
public record UpdateCloudUserResponse(
    bool Found,
    Guid? Id,
    long? Version,
    string Message);

/// <summary>
/// Request DTO for updating a cloud user via the API.
/// </summary>
public record UpdateCloudUserRequest(
    string? FirstName,
    string? LastName,
    int? RoleId,
    bool? IsActive);
