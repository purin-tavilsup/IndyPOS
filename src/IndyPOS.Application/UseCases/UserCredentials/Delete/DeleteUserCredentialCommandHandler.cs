using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.UserCredentials.Delete;

public class DeleteUserCredentialCommandHandler : ICommandHandler<DeleteUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public DeleteUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task Handle(DeleteUserCredentialCommand command, CancellationToken cancellationToken)
	{
		_userCredentialRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}