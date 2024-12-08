using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.UserCredentials.Update;

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