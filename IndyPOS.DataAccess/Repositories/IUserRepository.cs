using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IUserRepository
	{
		int CreateUser(UserAccount user);

		void UpdateUser(UserAccount user);

		UserAccount GetUserById(int id);

		IEnumerable<UserAccount> GetUsers();

		void RemoveUserById(int id);

		void CreateUserCredential(int userId, string username, string password);

		UserCredential GetUserCredentialById(int id);

		UserCredential GetUserCredentialByUsername(string username);

		void UpdateUserCredentialById(int userId, string password);

		void RemoveUserCredentialById(int userId);
	}
}
