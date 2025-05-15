using Nokpirab;

namespace IndyPOS.Application.UseCases.Users.Get;

public record GetUserByIdQuery(int Id) : IQuery<UserDto>;