namespace IndyPOS.Application.Common.Interfaces;

public interface ILoggedInUser
{
	int UserId { get; }

	string FirstName { get; }

	string LastName { get;}

	int RoleId { get; }
}