using IndyPOS.Facade.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models
{
	[ExcludeFromCodeCoverage]
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