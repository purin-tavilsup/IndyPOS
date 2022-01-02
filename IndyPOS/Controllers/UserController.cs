using System;
using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Events;
using IndyPOS.Users;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Controllers
{
    public class UserController : IUserController
	{
		private readonly IUserRepository _userRepository;
		private readonly IEventAggregator _eventAggregator;
		private IUser _loggedInUser;

		public UserController(IEventAggregator eventAggregator, IUserRepository userRepository)
		{
			_eventAggregator = eventAggregator;
			_userRepository = userRepository;
		}

		public IUser LoggedInUser => _loggedInUser;

		public bool IsLoggedIn => _loggedInUser != null;

		public bool LogIn(string username, string password)
		{
			try
			{
				var userCredential = GetUserCredentialByUserName(username);

				if (userCredential == null || userCredential.Password != password)
					return false;

				var user = GetUserById(userCredential.UserId);

				if (user == null)
					return false;

				_loggedInUser = user;

				_eventAggregator.GetEvent<UserLoggedInEvent>().Publish(_loggedInUser);

				return true;
			}
			catch
			{
				return false;
			}
		}

		public void LogOut()
		{
			_loggedInUser = null;

			_eventAggregator.GetEvent<UserLoggedOutEvent>().Publish();
		}

		public void AddNewUser(IUser user, string username, string password)
        {
			var userId = AddNewUserInternal(user);

			AddNewUserCredentialById(userId, username, password);
		}

		private int AddNewUserInternal(IUser user)
        {
			var userModel = new DataAccess.Models.User
			{
				FirstName = user.FirstName,
				LastName = user.LastName,
				RoleId = user.RoleId
			};

			return _userRepository.CreateUser(userModel);
		}

		private void AddNewUserCredentialById(int id, string username, string password)
        {
			_userRepository.CreateUserCredential(id, username, password);
		}

		public IEnumerable<IUser> GetUsers()
		{
			var results = _userRepository.GetUsers();

			return results.Select(p => new UserAdapter(p) as IUser).ToList();
		}

		public IUser GetUserById(int id)
        {
			var result = _userRepository.GetUserById(id);

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

		public void RemoveUserById(int id)
        {
			try
			{
				_userRepository.RemoveUserCredentialById(id);
				_userRepository.RemoveUserById(id);

				_eventAggregator.GetEvent<UserRemovedEvent>().Publish();
			}
			catch (Exception ex)
			{
				throw new Exception($"Error occurred while trying to delete the user. {ex.Message}", ex);
			}
        }

		public void UpdateUserCredentialById(int userId, string password)
		{
			_userRepository.UpdateUserCredentialById(userId, password);
		}

		public IUserCredential GetUserCredentialById(int id)
		{
			var result = _userRepository.GetUserCredentialById(id);

			return new UserCredentialAdapter(result);
		}

		private IUserCredential GetUserCredentialByUserName(string username)
		{
			var result = _userRepository.GetUserCredentialByUsername(username);

			return new UserCredentialAdapter(result);
		}
	}
}
