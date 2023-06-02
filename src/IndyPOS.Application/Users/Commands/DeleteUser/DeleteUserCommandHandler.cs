using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Events;
using MediatR;
using Prism.Events;

namespace IndyPOS.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
	private readonly IUserRepository _userRepository;
	private readonly IEventAggregator _eventAggregator;

    public DeleteUserCommandHandler(IUserRepository userRepository, IEventAggregator eventAggregator)
    {
        _userRepository = userRepository;
        _eventAggregator = eventAggregator;
    }

    public Task<Unit> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
	{
		_userRepository.RemoveById(command.Id);

        _eventAggregator.GetEvent<UserRemovedEvent>().Publish();

		return Task.FromResult(Unit.Value);
    }
}