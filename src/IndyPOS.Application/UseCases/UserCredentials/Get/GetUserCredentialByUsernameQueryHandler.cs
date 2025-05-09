using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.UserCredentials.Get;

public class GetUserCredentialByUsernameQueryHandler : IQueryHandler<GetUserCredentialByUsernameQuery, UserCredentialDto>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public GetUserCredentialByUsernameQueryHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task<UserCredentialDto> HandleAsync(GetUserCredentialByUsernameQuery query, CancellationToken cancellationToken = default)
	{
		var result = _userCredentialRepository.GetByUsername(query.Username);

		return Task.FromResult(result.ToDto());
	}
}