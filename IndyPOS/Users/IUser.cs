namespace IndyPOS.Users
{
	public interface IUser
	{
		int UserId { get; }
		string FirstName { get; set; }
		string LastName { get; set; }
		int RoleId { get; set; }
		string DateCreated { get; }
		string DateUpdated { get; }
	}
}