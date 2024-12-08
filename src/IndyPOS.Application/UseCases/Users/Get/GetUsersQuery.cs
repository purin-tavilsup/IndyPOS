using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Users.Get;

public record GetUsersQuery() : IQuery<IEnumerable<UserDto>>;