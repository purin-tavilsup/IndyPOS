using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Users.Queries.GetUsers;

public record GetUsersQuery() : IQuery<IEnumerable<UserDto>>;