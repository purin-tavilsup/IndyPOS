#nullable enable
using IndyPOS.Facade.Interfaces;
using IndyPOS.Interfaces;

namespace IndyPOS.Controllers
{
	public class UserController : IUserController
	{
		private readonly IUserHelper _userHelper;

		public UserController(IUserHelper userHelper)
		{
			_userHelper = userHelper;
		}

		public IUserAccount? LoggedInUser => _userHelper.LoggedInUser;

		public bool IsLoggedIn => _userHelper.LoggedInUser is not null;

		public bool LogIn(string username, string password)
		{
			return _userHelper.LogIn(username, password);
		}

		public void LogOut()
		{
			_userHelper.LogOut();
		}

		public void AddNewUser(IUserAccount user, string username, string password)
        {
			_userHelper.AddNewUser(user, username, password);
		}
		
		public IEnumerable<IUserAccount> GetUsers()
		{
			return _userHelper.GetUsers();
		}

		public IUserAccount GetUserById(int id)
		{
			return _userHelper.GetUserById(id);
		}

		public void UpdateUser(IUserAccount user)
        {
			_userHelper.UpdateUser(user);
		}

		public void RemoveUserById(int id)
        {
			_userHelper.RemoveUserById(id);
        }

		public void UpdateUserCredentialById(int userId, string password)
		{
			_userHelper.UpdateUserCredentialById(userId, password);
		}

		public IUserCredential GetUserCredentialById(int id)
		{
			return _userHelper.GetUserCredentialById(id);
		}
	}
}
