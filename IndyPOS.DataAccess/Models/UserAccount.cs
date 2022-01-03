using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models
{
    [ExcludeFromCodeCoverage]
    public class UserAccount
    {
        public int UserId { get; set; }
        
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int RoleId { get; set; }

        public string DateCreated { get; set; }

        public string DateUpdated { get; set; }
    }
}
