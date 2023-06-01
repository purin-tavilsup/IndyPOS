namespace IndyPOS.Application.Common.Interfaces;

public interface IUserLogInService
{
	Task<bool> LogInAsync(string username, string password);

	void LogOut();
}