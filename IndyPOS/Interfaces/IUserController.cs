using IndyPOS.Facade.Interfaces;

namespace IndyPOS.Interfaces
{
	public interface IUserController
	{
		IUserAccount LoggedInUser { get; }

		bool IsLoggedIn { get; }

		void AddNewUser(IUserAccount user, string username, string password);

		IEnumerable<IUserAccount> GetUsers();

		IUserAccount GetUserById(int id);

		void UpdateUser(IUserAccount user);

		void RemoveUserById(int id);

		void UpdateUserCredentialById(int userId, string password);

		IUserCredential GetUserCredentialById(int id);

		bool LogIn(string username, string password);

		void LogOut();
	}
}