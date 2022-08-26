namespace IndyPOS.Interfaces
{
	public interface IUserAccount
	{
		int UserId { get; }
		string FirstName { get; }
		string LastName { get;}
		int RoleId { get; }
		string DateCreated { get; }
		string DateUpdated { get; }
	}
}