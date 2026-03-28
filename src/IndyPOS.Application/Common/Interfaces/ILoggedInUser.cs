namespace IndyPOS.Application.Common.Interfaces;

public interface ILoggedInUser
{
	/// <summary>
	/// User ID (UUID from StoreHub).
	/// </summary>
	Guid UserId { get; }

	string FirstName { get; }

	string LastName { get; }

	int RoleId { get; }
}