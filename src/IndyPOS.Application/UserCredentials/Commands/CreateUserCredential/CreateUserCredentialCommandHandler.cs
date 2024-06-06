using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UserCredentials.Commands.CreateUserCredential;

public class CreateUserCredentialCommandHandler : ICommandHandler<CreateUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public CreateUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task Handle(CreateUserCredentialCommand command, CancellationToken cancellationToken)
	{
		_userCredentialRepository.Add(command.ToEntity());

		return Task.CompletedTask;
	}
}