using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Users.Commands.CreateUser;

public record CreateUserCommand : ICommand<int>
{
	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public int RoleId { get; set; }
}