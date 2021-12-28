using System.Collections.Generic;
using IndyPOS.Users;

namespace IndyPOS.Controllers
{
	public interface IUserController
	{
		IUser LoggedInUser { get; }

		bool IsLoggedIn { get; }

		void AddNewUser(IUser user, string username, string password);

		IEnumerable<IUser> GetUsers();

		IUser GetUserById(int id);

		void UpdateUser(IUser user);

		void UpdateUserCredentialById(int userId, string password);

		IUserCredential GetUserCredentialById(int id);

		bool LogIn(string username, string password);

		void LogOut();
	}
}