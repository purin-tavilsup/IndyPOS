using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.UserCredentials.Create;

public class CreateUserCredentialCommandHandler : ICommandHandler<CreateUserCredentialCommand>
{
	private readonly IUserCredentialRepository _userCredentialRepository;

	public CreateUserCredentialCommandHandler(IUserCredentialRepository userCredentialRepository)
	{
		_userCredentialRepository = userCredentialRepository;
	}

	public Task HandleAsync(CreateUserCredentialCommand command, CancellationToken cancellationToken = default)
	{
		_userCredentialRepository.Add(command.ToEntity());

		return Task.CompletedTask;
	}
}