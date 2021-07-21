using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.DataAccess.Models
{
    public class User
    {
        public int UserId { get; set; }
        
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int RoleId { get; set; }

        public string DateCreated { get; set; }

        public string DateUpdated { get; set; }
    }
}
