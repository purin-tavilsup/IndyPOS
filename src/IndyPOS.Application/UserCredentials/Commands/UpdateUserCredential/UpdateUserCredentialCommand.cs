using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UserCredentials.Commands.UpdateUserCredential;

public record UpdateUserCredentialCommand : ICommand 
{
	public int UserId { get; set; }

	public string Password { get; set; } = string.Empty;
}