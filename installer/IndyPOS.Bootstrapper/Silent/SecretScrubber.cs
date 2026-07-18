using System.Text.RegularExpressions;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Best-effort redaction of connection-string-shaped secrets from text before it
/// is persisted to the install log or echoed to stdout on a failure path.
/// </summary>
public static partial class SecretScrubber
{
    // Password=... or Pwd=... up to the next ';' or end of string (case-insensitive).
    [GeneratedRegex(@"(?i)\b(password|pwd)\s*=\s*[^;]*")]
    private static partial Regex PasswordAssignment();

    public static string Scrub(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return PasswordAssignment().Replace(text, "$1=***REDACTED***");
    }
}
