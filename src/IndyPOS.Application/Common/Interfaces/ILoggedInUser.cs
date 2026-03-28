namespace IndyPOS.Application.Common.Interfaces;

public interface ILoggedInUser
{
	/// <summary>
	/// Legacy SQLite user ID.
	/// </summary>
	int UserId { get; }

	/// <summary>
	/// StoreHub user ID (UUID). Null for legacy SQLite users.
	/// </summary>
	Guid? StoreHubUserId { get; }

	string FirstName { get; }

	string LastName { get; }

	int RoleId { get; }
}