using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;

namespace IndyPOS.Application.UseCases.Users.Create;

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