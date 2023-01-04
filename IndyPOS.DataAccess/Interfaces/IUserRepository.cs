using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IUserRepository
{
	int CreateUser(UserAccount user);

	bool UpdateUser(UserAccount user);

	UserAccount? GetUserById(int id);

	IEnumerable<UserAccount> GetUsers();

	bool RemoveUserById(int id);

	bool CreateUserCredential(int userId, string username, string password);

	UserCredential? GetUserCredentialById(int id);

	UserCredential? GetUserCredentialByUsername(string username);

	bool UpdateUserCredentialById(int userId, string password);

	bool RemoveUserCredentialById(int userId);
}