using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.UserCredentials.Commands.CreateUserCredential;

public class CreateUserCredentialCommandHandler : ICommandHandler<CreateUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public CreateUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task<Unit> Handle(CreateUserCredentialCommand command, CancellationToken cancellationToken)
	{
		_userCredentialRepository.Add(command.ToEntity());

		return Task.FromResult(Unit.Value);
	}
}