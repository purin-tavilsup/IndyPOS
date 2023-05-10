using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
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
		_userRepository.Update(command.ToEntity());

		return Task.FromResult(Unit.Value);
    }
}