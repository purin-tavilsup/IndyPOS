using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Infrastructure.Services.StoreHub;

public class FirstLoginCoordinator : IFirstLoginCoordinator
{
    private readonly IUserLogInService _login;
    private readonly IChangePasswordPrompt _prompt;

    public FirstLoginCoordinator(IUserLogInService login, IChangePasswordPrompt prompt)
    {
        _login = login;
        _prompt = prompt;
    }

    public async Task<bool> LogInAsync(string username, string password)
    {
        var result = await _login.LogInAsync(username, password);
        if (!result.Success)
        {
            return false;
        }

        if (!result.MustChangePassword)
        {
            return true;
        }

        var newPassword = await _prompt.RequestNewPasswordAsync();
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            _login.LogOut();
            return false;
        }

        var change = await _login.ChangePasswordAsync(password, newPassword);
        if (!change.Success)
        {
            _login.LogOut();
            return false;
        }

        // Re-login with the rotated password establishes the real session
        // (product sync + UserLoggedInEvent) via the normal path.
        var relogin = await _login.LogInAsync(username, newPassword);
        return relogin.Success;
    }

    public void LogOut() => _login.LogOut();
}
