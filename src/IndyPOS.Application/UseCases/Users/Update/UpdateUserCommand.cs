using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Users.Update;

public record UpdateUserCommand : ICommand
{
	public int UserId { get; set; }
        
	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;
}