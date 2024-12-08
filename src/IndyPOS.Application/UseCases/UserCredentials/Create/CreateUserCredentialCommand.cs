using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.UserCredentials.Create;

public record CreateUserCredentialCommand : ICommand 
{
	public int UserId { get; set; }

	public string Username { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;
}