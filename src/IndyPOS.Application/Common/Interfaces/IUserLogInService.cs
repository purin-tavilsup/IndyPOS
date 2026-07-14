namespace IndyPOS.Application.Common.Interfaces;

public interface IUserLogInService
{
    Task<LogInResult> LogInAsync(string username, string password);

    Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword);

    void LogOut();
}
