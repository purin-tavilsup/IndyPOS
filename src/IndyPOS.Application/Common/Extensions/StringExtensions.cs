namespace IndyPOS.Application.Common.Extensions;

public static class StringExtensions
{
    public static bool HasValue(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>The text cut to at most <paramref name="maxLength"/> UTF-16 units, never between a surrogate pair.</summary>
    /// <remarks>A lone high surrogate makes PostgreSQL's UTF-8 encoding throw, so a write that stores it fails.</remarks>
    public static string? CutTo(this string? text, int maxLength)
    {
        if (text is null || text.Length <= maxLength)
            return text;

        var length = char.IsHighSurrogate(text[maxLength - 1]) ? maxLength - 1 : maxLength;

        return text[..length];
    }
}