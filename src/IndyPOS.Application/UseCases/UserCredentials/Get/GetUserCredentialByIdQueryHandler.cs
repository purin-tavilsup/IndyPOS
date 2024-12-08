using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.UserCredentials.Get;

public class GetUserCredentialByIdQueryHandler : IQueryHandler<GetUserCredentialByIdQuery, UserCredentialDto>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public GetUserCredentialByIdQueryHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task<UserCredentialDto> Handle(GetUserCredentialByIdQuery query, CancellationToken cancellationToken)
	{
		var result = _userCredentialRepository.GetById(query.Id);

		return Task.FromResult(result.ToDto());
	}
}