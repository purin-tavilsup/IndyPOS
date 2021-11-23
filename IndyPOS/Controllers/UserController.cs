using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Inventory;
using IndyPOS.Users;

namespace IndyPOS.Controllers
{
	public class UserController : IUserController
	{
		private readonly IUserRepository _userRepository;

		public UserController(IUserRepository userRepository)
		{
			_userRepository = userRepository;
		}

		public void AddNewUser(IUser user, string encryptedSecret)
        {
			var userId = AddNewUser(user);

			AddNewUserSecretByUserId(userId, encryptedSecret);
		}

		private int AddNewUser(IUser user)
        {
			var userModel = new DataAccess.Models.User
			{
				FirstName = user.FirstName,
				LastName = user.LastName,
				RoleId = user.RoleId
			};

			return _userRepository.AddUser(userModel);
		}

		private void AddNewUserSecretByUserId(int id, string encryptedSecret)
        {
			_userRepository.AddUserCredential(id, encryptedSecret);
		}

		public IList<IUser> GetUsers()
		{
			var results = _userRepository.GetUsers();

			return results.Select(p => new UserAdapter(p) as IUser).ToList();
		}

		public IUser GetUserByUserId(int id)
        {
			var result = _userRepository.GetUserByUserId(id);

			return new UserAdapter(result);
        }

		public void UpdateUser(IUser user)
        {
			var userModel = new DataAccess.Models.User
            {
				UserId = user.UserId,
				FirstName = user.FirstName,
				LastName = user.LastName
            };

			_userRepository.UpdateUser(userModel);
		}

		public void UpdateUserSecretByUserId(int id, string encryptedSecret)
		{
			_userRepository.UpdateUserCredential(id, encryptedSecret);
		}

		private void GetUserCredentialByUserId(int id)
		{
			var result = _userRepository.GetUserCredentialByUserId(id);
		}
	}
}
