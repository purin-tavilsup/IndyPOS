using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.UserCredentials.Delete;

public class DeleteUserCredentialCommandHandler : ICommandHandler<DeleteUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public DeleteUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task HandleAsync(DeleteUserCredentialCommand command, CancellationToken cancellationToken = default)
	{
		_userCredentialRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}