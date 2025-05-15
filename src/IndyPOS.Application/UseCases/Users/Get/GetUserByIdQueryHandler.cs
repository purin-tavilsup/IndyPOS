using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Users.Get;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
	private readonly IUserRepository _userRepository;

	public GetUserByIdQueryHandler(IUserRepository userRepository)
	{
		_userRepository = userRepository;
	}

	public Task<UserDto> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
	{
		var result = _userRepository.GetById(query.Id);

		return Task.FromResult(result.ToDto());
	}
}