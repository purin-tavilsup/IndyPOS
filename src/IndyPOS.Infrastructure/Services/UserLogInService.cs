using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.UserCredentials;
using IndyPOS.Application.UseCases.UserCredentials.Get;
using IndyPOS.Application.UseCases.Users;
using IndyPOS.Application.UseCases.Users.Get;
using Nokpirab;

namespace IndyPOS.Infrastructure.Services;

public class UserLogInService : IUserLogInService
{
	private readonly INokpirab _nokpirab;
	private readonly IEventAggregator _eventAggregator;

	public UserLogInService(IEventAggregator eventAggregator, INokpirab nokpirab)
	{
		_eventAggregator = eventAggregator;
		_nokpirab = nokpirab;
	}

	public async Task<bool> LogInAsync(string username, string password)
	{
		try
		{
			var userCredential = await GetUserCredentialByUserNameAsync(username);

			if (userCredential.Password != password)
				return false;

			var user = await GetUserByIdAsync(userCredential.UserId);

			_eventAggregator.GetEvent<UserLoggedInEvent>().Publish(new LoggedInUser(user));

			return true;
		}
		catch
		{
			return false;
        }
    }

	public void LogOut()
	{
		_eventAggregator.GetEvent<UserLoggedOutEvent>().Publish();
	}

	private async Task<UserCredentialDto> GetUserCredentialByUserNameAsync(string username)
	{
		return await _nokpirab.SendAsync(new GetUserCredentialByUsernameQuery(username));
	}

	private async Task<UserDto> GetUserByIdAsync(int id)
	{
		return await _nokpirab.SendAsync(new GetUserByIdQuery(id));
	}

	private class LoggedInUser : ILoggedInUser
	{
		private readonly UserDto _user;

		public LoggedInUser(UserDto user)
		{
			_user = user;
		}

		// Legacy users get a deterministic GUID from their int ID
		public Guid UserId => new Guid(0, 0, 0, BitConverter.GetBytes(_user.UserId));

		public string FirstName => _user.FirstName;

		public string LastName => _user.LastName;

		public int RoleId => _user.RoleId;
	}
}