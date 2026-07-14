using System.Security.Cryptography;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Generates a human-typable random admin password. Excludes ambiguous glyphs
/// (0/O/1/l/I). ~14 chars from a 56-char alphabet ≈ 81 bits — plenty for a
/// single-use bootstrap credential that is force-rotated on first login.
/// </summary>
public static class AdminPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const int Length = 14;

    public static string Generate()
    {
        var chars = new char[Length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(chars);
    }
}
