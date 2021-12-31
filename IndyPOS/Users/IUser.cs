namespace IndyPOS.Users
{
	public interface IUser
	{
		int UserId { get; }
		string FirstName { get; }
		string LastName { get;}
		int RoleId { get; }
		string DateCreated { get; }
		string DateUpdated { get; }
	}
}