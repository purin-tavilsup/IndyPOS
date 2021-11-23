using IndyPOS.Users;
using UserModel = IndyPOS.DataAccess.Models.User;

namespace IndyPOS.Adapters
{
	public class UserAdapter : IUser
	{
		private readonly UserModel _adaptee;

		public UserAdapter(UserModel adaptee)
		{
			_adaptee = adaptee;
		}

		public int UserId => _adaptee.UserId;

		public string FirstName 
		{ 
			get => _adaptee.FirstName; 
			set => _adaptee.FirstName = value;
		}

		public string LastName 
		{ 
			get => _adaptee.LastName; 
			set => _adaptee.LastName = value;
		}

		public int RoleId
        {
			get => _adaptee.RoleId;
			set => _adaptee.RoleId = value;
        }

		public string DateCreated => _adaptee.DateCreated;

		public string DateUpdated => _adaptee.DateUpdated;
	}
}
