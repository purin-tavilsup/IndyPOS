using IndyPOS.Application.Adapters;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Domain.Events;
using Prism.Events;

namespace IndyPOS.Application.Helpers;

public class UserHelper : IUserHelper
{
	private readonly IUserRepository _userRepository;
	private readonly IUserCredentialRepository _userCredentialRepository;
	private readonly IEventAggregator _eventAggregator;

	public UserHelper(IEventAggregator eventAggregator,
					  IUserRepository userRepository,
					  IUserCredentialRepository userCredentialRepository)
	{
		_eventAggregator = eventAggregator;
		_userRepository = userRepository;
		_userCredentialRepository = userCredentialRepository;
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
		var userModel = new Domain.Entities.UserAccount
		{
			FirstName = user.FirstName,
			LastName = user.LastName,
			RoleId = user.RoleId
		};

		return _userRepository.Add(userModel);
	}

	private void AddNewUserCredentialById(int id, string username, string password)
	{
		_userCredentialRepository.Add(id, username, password);
	}

	public IEnumerable<IUserAccount> GetUsers()
	{
		var results = _userRepository.GetAll();

		return results.Select(p => new UserAccountAdapter(p) as IUserAccount).ToList();
	}

	public IUserAccount GetUserById(int id)
	{
		var result = _userRepository.GetById(id);

		return new UserAccountAdapter(result);
	}

	public void UpdateUser(IUserAccount user)
	{
		var userModel = new Domain.Entities.UserAccount
		{
			UserId = user.UserId,
			FirstName = user.FirstName,
			LastName = user.LastName
		};

		_userRepository.Update(userModel);
	}

	public void RemoveUserById(int id)
	{
		_userCredentialRepository.RemoveById(id);
		_userRepository.RemoveById(id);

		_eventAggregator.GetEvent<UserRemovedEvent>().Publish();
	}

	public void UpdateUserCredentialById(int userId, string password)
	{
		_userCredentialRepository.UpdatePasswordById(userId, password);
	}

	public IUserCredential GetUserCredentialById(int id)
	{
		var result = _userCredentialRepository.GetById(id);

		return new UserCredentialAdapter(result);
	}

	private IUserCredential GetUserCredentialByUserName(string username)
	{
		var result = _userCredentialRepository.GetByUsername(username);

		return new UserCredentialAdapter(result);
	}
}