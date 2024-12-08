using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Users.Get;

public record GetUserByIdQuery(int Id) : IQuery<UserDto>;