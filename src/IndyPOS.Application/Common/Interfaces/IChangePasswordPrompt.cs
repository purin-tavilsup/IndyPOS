namespace IndyPOS.Application.Common.Interfaces;

/// <summary>UI seam: prompts the user for a new password. Returns null if cancelled.</summary>
public interface IChangePasswordPrompt
{
    Task<string?> RequestNewPasswordAsync();
}
