namespace IndyPOS.Users
{
    public class UserCredential : IUserCredential
	{
		public int UserId { get; }

		public string Username { get; }

		public string Password { get; }

		public string DateCreated { get; }

		public string DateUpdated { get; }
	}
}
