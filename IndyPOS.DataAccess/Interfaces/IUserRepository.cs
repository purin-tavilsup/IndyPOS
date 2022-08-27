using System.Collections.Generic;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces
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
