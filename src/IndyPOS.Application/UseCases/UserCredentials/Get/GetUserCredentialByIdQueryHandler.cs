using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.UserCredentials.Get;

public class GetUserCredentialByIdQueryHandler : IQueryHandler<GetUserCredentialByIdQuery, UserCredentialDto>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public GetUserCredentialByIdQueryHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task<UserCredentialDto> HandleAsync(GetUserCredentialByIdQuery query, CancellationToken cancellationToken = default)
	{
		var result = _userCredentialRepository.GetById(query.Id);

		return Task.FromResult(result.ToDto());
	}
}