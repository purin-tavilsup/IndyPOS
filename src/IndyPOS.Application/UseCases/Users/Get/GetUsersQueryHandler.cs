using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Users.Get;

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IEnumerable<UserDto>>
{
	private readonly IUserRepository _userRepository;

	public GetUsersQueryHandler(IUserRepository userRepository)
	{
		_userRepository = userRepository;
	}

	public Task<IEnumerable<UserDto>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
	{
		var results = _userRepository.GetAll();

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}