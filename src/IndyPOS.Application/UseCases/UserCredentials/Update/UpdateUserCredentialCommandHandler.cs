using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.UserCredentials.Update;

public class UpdateUserCredentialCommandHandler : ICommandHandler<UpdateUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

    public UpdateUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
    {
        _userCredentialRepository = userCredentialRepository;
    }

	public Task HandleAsync(UpdateUserCredentialCommand command, CancellationToken cancellationToken = default)
	{
		_userCredentialRepository.UpdatePassword(command.ToEntity());

		return Task.CompletedTask;
	}
}