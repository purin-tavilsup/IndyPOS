using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.UserCredentials.Commands.DeleteUserCredential;

public class DeleteUserCredentialCommandHandler : ICommandHandler<DeleteUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public DeleteUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task<Unit> Handle(DeleteUserCredentialCommand command, CancellationToken cancellationToken)
	{
		_userCredentialRepository.RemoveById(command.Id);

		return Task.FromResult(Unit.Value);
	}
}