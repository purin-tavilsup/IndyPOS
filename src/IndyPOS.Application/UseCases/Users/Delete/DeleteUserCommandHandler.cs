using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;

namespace IndyPOS.Application.UseCases.Users.Delete;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
	private readonly IUserRepository _userRepository;
	private readonly IEventAggregator _eventAggregator;

    public DeleteUserCommandHandler(IUserRepository userRepository, IEventAggregator eventAggregator)
    {
        _userRepository = userRepository;
        _eventAggregator = eventAggregator;
    }

    public Task Handle(DeleteUserCommand command, CancellationToken cancellationToken)
	{
		_userRepository.RemoveById(command.Id);

        _eventAggregator.GetEvent<UserRemovedEvent>().Publish();

		return Task.CompletedTask;
    }
}