using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.UserCredentials.Commands.UpdateUserCredential;

public class UpdateUserCredentialCommandHandler : ICommandHandler<UpdateUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

    public UpdateUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
    {
        _userCredentialRepository = userCredentialRepository;
    }

    public Task Handle(UpdateUserCredentialCommand command, CancellationToken cancellationToken)
	{
		_userCredentialRepository.UpdatePassword(command.ToEntity());

		return Task.CompletedTask;
    }
}