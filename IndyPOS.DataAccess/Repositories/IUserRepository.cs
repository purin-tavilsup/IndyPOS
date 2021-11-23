using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IUserRepository
	{
		IList<User> GetUsers();

		int AddUser(User user);

		void UpdateUser(User user);

		User GetUserByUserId(int id);

		void AddUserCredential(int userId, string encryptedSecret);

		UserCredential GetUserCredentialByUserId(int id);

		void UpdateUserCredential(int userId, string encryptedSecret);
	}
}