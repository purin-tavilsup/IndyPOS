using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using MediatR;

namespace IndyPOS.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
	private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

	public Task<Unit> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
	{
		var entity = new UserAccount
		{
			UserId = command.UserId,
			FirstName = command.FirstName,
			LastName = command.LastName
		};

		_userRepository.Update(entity);

		return Task.FromResult(Unit.Value);
    }
}