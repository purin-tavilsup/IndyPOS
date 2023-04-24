using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
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
		var entity = new UserCredential
		{
			UserId = command.UserId,
			Username = command.Username,
			Password = command.Password
		};

		_userCredentialRepository.Add(entity);

		return Task.FromResult(Unit.Value);
	}
}