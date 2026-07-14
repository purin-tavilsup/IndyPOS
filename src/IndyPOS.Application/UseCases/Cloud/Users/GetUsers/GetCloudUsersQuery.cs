using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.GetUsers;

/// <summary>
/// Query to get cloud users with optional filtering and pagination.
/// </summary>
public record GetCloudUsersQuery(
    string? StoreId,
    bool? ActiveOnly,
    int Page,
    int PageSize) : IQuery<GetCloudUsersResponse>;

/// <summary>
/// Response containing paginated cloud users.
/// </summary>
public record GetCloudUsersResponse(
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<CloudUserItem> Users);

/// <summary>
/// Individual user item in the response.
/// </summary>
public record CloudUserItem(
    Guid Id,
    string StoreId,
    string Username,
    string FirstName,
    string LastName,
    int RoleId,
    bool IsActive,
    long Version,
    DateTime CreatedAtUtc,
    DateTime LastModifiedAtUtc);
