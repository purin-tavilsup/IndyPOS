using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Events;
using Prism.Events;

namespace IndyPOS.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, int>
{
    private readonly IUserRepository _userRepository;
	private readonly IEventAggregator _eventAggregator;

    public CreateUserCommandHandler(IUserRepository userRepository, IEventAggregator eventAggregator)
    {
        _userRepository = userRepository;
        _eventAggregator = eventAggregator;
    }

    public Task<int> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
		var userId = _userRepository.Add(command.ToEntity());

        _eventAggregator.GetEvent<UserAddedEvent>().Publish();

        return Task.FromResult(userId);
    }
}