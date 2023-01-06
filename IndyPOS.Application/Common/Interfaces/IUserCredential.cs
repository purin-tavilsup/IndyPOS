namespace IndyPOS.Application.Common.Interfaces;

public interface IUserCredential
{
	int UserId { get; }

	string Username { get; }

	string Password { get; }

	string DateCreated { get; }

	string DateUpdated { get; }
}