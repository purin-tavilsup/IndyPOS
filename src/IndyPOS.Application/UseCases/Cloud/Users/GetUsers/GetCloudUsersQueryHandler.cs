using IndyPOS.Application.Abstractions.Cloud.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Users.GetUsers;

/// <summary>
/// Handler for getting cloud users with filtering and pagination.
/// </summary>
public class GetCloudUsersQueryHandler(ICloudUserRepository userRepository)
    : IQueryHandler<GetCloudUsersQuery, GetCloudUsersResponse>
{
    public async Task<GetCloudUsersResponse> HandleAsync(
        GetCloudUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        // Normalize pagination
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        // Get total count for pagination
        var totalCount = await userRepository.CountAsync(
            query.StoreId,
            query.ActiveOnly,
            cancellationToken);

        // Get users for current page
        var users = await userRepository.GetAllAsync(
            query.StoreId,
            query.ActiveOnly,
            skip,
            pageSize,
            cancellationToken);

        // Map to response items
        var items = users.Select(u => new CloudUserItem(
            u.Id,
            u.StoreId,
            u.Username,
            u.FirstName,
            u.LastName,
            u.RoleId,
            u.IsActive,
            u.Version,
            u.CreatedAtUtc,
            u.LastModifiedAtUtc)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new GetCloudUsersResponse(
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages,
            Users: items);
    }
}
