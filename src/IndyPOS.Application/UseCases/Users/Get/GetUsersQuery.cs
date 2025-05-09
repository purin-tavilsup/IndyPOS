using Nokpirab;

namespace IndyPOS.Application.UseCases.Users.Get;

public record GetUsersQuery : IQuery<IEnumerable<UserDto>>;