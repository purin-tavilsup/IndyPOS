using IndyPOS.Application.Abstractions.StoreHub.Services;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// BCrypt password hasher implementation.
/// Work factor 12 provides ~250ms hash time on modern hardware.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Invalid hash format (e.g., legacy TripleDES hash passed by mistake)
            return false;
        }
    }
}
