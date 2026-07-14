namespace IndyPOS.Application.Common.Interfaces;

/// <summary>
/// Orchestrates login + forced first-login password rotation, keeping this
/// security-critical branching out of the coverage-excluded UI panel.
/// </summary>
public interface IFirstLoginCoordinator
{
    /// <summary>Returns true only if the user ends up fully logged in.</summary>
    Task<bool> LogInAsync(string username, string password);

    void LogOut();
}
