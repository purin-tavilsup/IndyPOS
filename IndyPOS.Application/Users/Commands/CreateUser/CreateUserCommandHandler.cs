using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using IndyPOS.Domain.Events;
using MediatR;
using Prism.Events;

namespace IndyPOS.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;
	private readonly IEventAggregator _eventAggregator;

    public CreateUserCommandHandler(IUserRepository userRepository, IEventAggregator eventAggregator)
    {
        _userRepository = userRepository;
        _eventAggregator = eventAggregator;
    }

    public Task<Unit> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var entity = new UserAccount
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            RoleId = command.RoleId
        };

		_userRepository.Add(entity);

        _eventAggregator.GetEvent<UserAddedEvent>().Publish();

        return Task.FromResult(Unit.Value);
    }
}