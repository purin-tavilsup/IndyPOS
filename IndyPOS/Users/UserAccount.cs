using IndyPOS.Interfaces;

namespace IndyPOS.Users
{
	public class UserAccount : IUserAccount
	{
		public int UserId { get; }
        
		public string FirstName { get; set; }

		public string LastName { get; set; }

		public int RoleId { get; set; }

		public string DateCreated { get; }

		public string DateUpdated { get; }
	}
}