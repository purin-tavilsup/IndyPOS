namespace IndyPOS.DataAccess.Models
{
	public class UserCredential
	{
		public int UserId { get; set; }

		public string Password { get; set; }

		public string DateCreated { get; set; }

		public string DateUpdated { get; set; }
	}
}
