using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UserCredentials.Commands.CreateUserCredential;

public record CreateUserCredentialCommand : ICommand 
{
	public int UserId { get; set; }

	public string Username { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;
}