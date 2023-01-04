using IndyPOS.Application.Adapters;
using IndyPOS.Application.Events;
using IndyPOS.Application.Interfaces;
using IndyPOS.DataAccess.Interfaces;
using Prism.Events;

namespace IndyPOS.Application.Helpers;

public class UserHelper : IUserHelper
{
	private readonly IUserRepository _userRepository;
	private readonly IEventAggregator _eventAggregator;

	public UserHelper(IEventAggregator eventAggregator, IUserRepository userRepository)
	{
		_eventAggregator = eventAggregator;
		_userRepository = userRepository;
	}

	public IUserAccount? LoggedInUser { get; private set; }

	public bool IsLoggedIn => LoggedInUser != null;

	public bool LogIn(string username, string password)
	{
		try
		{
			var userCredential = GetUserCredentialByUserName(username);

			if (userCredential.Password != password)
				return false;

			var user = GetUserById(userCredential.UserId);

			LoggedInUser = user;

			_eventAggregator.GetEvent<UserLoggedInEvent>().Publish(LoggedInUser);

			return true;
		}
		catch
		{
			return false;
		}
	}

	public void LogOut()
	{
		LoggedInUser = null;

		_eventAggregator.GetEvent<UserLoggedOutEvent>().Publish();
	}

	public void AddNewUser(IUserAccount user, string username, string password)
	{
		var userId = AddNewUserInternal(user);

		AddNewUserCredentialById(userId, username, password);

		_eventAggregator.GetEvent<UserAddedEvent>().Publish();
	}

	private int AddNewUserInternal(IUserAccount user)
	{
		var userModel = new DataAccess.Models.UserAccount
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

	public IEnumerable<IUserAccount> GetUsers()
	{
		var results = _userRepository.GetUsers();

		return results.Select(p => new UserAccountAdapter(p) as IUserAccount).ToList();
	}

	public IUserAccount GetUserById(int id)
	{
		var result = _userRepository.GetUserById(id);

		return new UserAccountAdapter(result);
	}

	public void UpdateUser(IUserAccount user)
	{
		var userModel = new DataAccess.Models.UserAccount
		{
			UserId = user.UserId,
			FirstName = user.FirstName,
			LastName = user.LastName
		};

		_userRepository.UpdateUser(userModel);
	}

	public void RemoveUserById(int id)
	{
		_userRepository.RemoveUserCredentialById(id);
		_userRepository.RemoveUserById(id);

		_eventAggregator.GetEvent<UserRemovedEvent>().Publish();
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