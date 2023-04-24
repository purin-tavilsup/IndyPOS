using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using MediatR;

namespace IndyPOS.Application.UserCredentials.Commands.UpdateUserCredential;

public class UpdateUserCredentialCommandHandler : ICommandHandler<UpdateUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

    public UpdateUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
    {
        _userCredentialRepository = userCredentialRepository;
    }

    public Task<Unit> Handle(UpdateUserCredentialCommand command, CancellationToken cancellationToken)
	{
		var entity = new UserCredential
		{
			UserId = command.UserId,
			Password = command.Password
		};

		_userCredentialRepository.UpdatePassword(entity);

		return Task.FromResult(Unit.Value);
    }
}