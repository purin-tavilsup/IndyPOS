using System.Collections.Generic;
using IndyPOS.Users;

namespace IndyPOS.Controllers
{
	public interface IUserController
	{
		void AddNewUser(IUser user, string encryptedSecret);

		IList<IUser> GetUsers();

		IUser GetUserByUserId(int id);

		void UpdateUser(IUser user);

		void UpdateUserSecretByUserId(int id, string encryptedSecret);
	}
}