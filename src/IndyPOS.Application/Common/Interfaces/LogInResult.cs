namespace IndyPOS.Application.Common.Interfaces;

/// <summary>Outcome of a login attempt. MustChangePassword true means the session
/// is NOT yet established — the caller must rotate the password first.</summary>
public record LogInResult(bool Success, bool MustChangePassword);

/// <summary>Outcome of a password change.</summary>
public record ChangePasswordResult(bool Success, string? ErrorMessage);
