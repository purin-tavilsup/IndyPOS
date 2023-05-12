using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
	private readonly IUserRepository _userRepository;

	public GetUserByIdQueryHandler(IUserRepository userRepository)
	{
		_userRepository = userRepository;
	}

	public Task<UserDto> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
	{
		var result = _userRepository.GetById(query.Id);

		return Task.FromResult(result.ToDto());
	}
}