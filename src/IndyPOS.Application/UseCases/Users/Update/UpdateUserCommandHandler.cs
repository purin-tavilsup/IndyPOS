using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Users.Update;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
	private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

	public Task HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
	{
		_userRepository.Update(command.ToEntity());

		return Task.CompletedTask;
	}
}