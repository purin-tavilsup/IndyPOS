using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IUserRepository
	{
		int CreateUser(User user);

		void UpdateUser(User user);

		User GetUserById(int id);

		IEnumerable<User> GetUsers();

		void RemoveUserById(int id);

		void CreateUserCredential(int userId, string username, string password);

		UserCredential GetUserCredentialById(int id);

		UserCredential GetUserCredentialByUsername(string username);

		void UpdateUserCredentialById(int userId, string password);

		void RemoveUserCredentialById(int userId);
	}
}
