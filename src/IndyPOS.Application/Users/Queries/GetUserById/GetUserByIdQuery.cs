using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Users.Queries.GetUserById;

public record GetUserByIdQuery(int Id) : IQuery<UserDto>;