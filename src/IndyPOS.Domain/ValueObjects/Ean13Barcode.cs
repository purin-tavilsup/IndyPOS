namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// Builds EAN-13 barcodes for products the store labels itself, and computes the
/// EAN-13 check digit.
/// <para>
/// Layout: <c>200</c> + 2-digit store code + 7-digit sequence + check digit = 13 digits.
/// GS1 reserves the <c>200-299</c> prefix range for restricted circulation - barcodes a
/// shop assigns to its own goods - and the stores' existing stock already uses
/// <c>200</c>, so generated labels stay in the same range as printed ones.
/// </para>
/// <para>
/// The 13-digit length is not cosmetic: the EAN-13 writer accepts only a 12-digit body
/// (it appends the check digit itself) or a complete 13-digit code. A barcode of any
/// other length throws deep inside the writer, far from whatever produced it.
/// </para>
/// <para>
/// Legacy barcodes put the numeric <em>category</em> id in the two digits after the
/// prefix, and those ids run 10-54. Store codes for the live stores are 1-3, which
/// zero-pad to 01-03 and cannot collide. A store code in 10-54 would; the unique index
/// on <c>(store_id, barcode)</c> is the backstop.
/// </para>
/// </summary>
public static class Ean13Barcode
{
    /// <summary>GS1 restricted-circulation prefix used for in-store labelling.</summary>
    public const string InStorePrefix = "200";

    private const int CheckedDigitCount = 12;
    private const int MinStoreCode = 1;
    private const int MaxStoreCode = 99;
    private const int MinSequence = 1;
    private const int MaxSequence = 9_999_999;

    /// <summary>
    /// Builds the complete 13-digit barcode for a store's next own-label product.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The store code or sequence would not fit its field, which would change the
    /// barcode's length and make it unencodable.
    /// </exception>
    public static string ForStoreProduct(int storeCode, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(storeCode, MinStoreCode);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(storeCode, MaxStoreCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, MinSequence);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sequence, MaxSequence);

        var body = $"{InStorePrefix}{storeCode:D2}{sequence:D7}";

        return $"{body}{CalculateCheckDigit(body)}";
    }

    /// <summary>
    /// Computes the EAN-13 check digit for a 12-digit body (the code without its final
    /// digit). Digits are weighted 1, 3, 1, 3 ... from the left.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The input is not exactly 12 characters, or contains a non-digit.
    /// </exception>
    public static int CalculateCheckDigit(string twelveDigitCode)
    {
        ArgumentNullException.ThrowIfNull(twelveDigitCode);

        if (twelveDigitCode.Length != CheckedDigitCount)
        {
            throw new ArgumentException(
                $"A {CheckedDigitCount}-digit code is required to calculate an EAN-13 check digit, " +
                $"but got {twelveDigitCode.Length}.",
                nameof(twelveDigitCode));
        }

        var weightedSum = 0;

        for (var index = 0; index < CheckedDigitCount; index++)
        {
            var character = twelveDigitCode[index];

            if (!char.IsAsciiDigit(character))
            {
                throw new ArgumentException(
                    $"'{twelveDigitCode}' contains the non-digit character '{character}'.",
                    nameof(twelveDigitCode));
            }

            // Positions alternate weight 1 and 3, starting at 1 for the leftmost digit.
            var weight = index % 2 == 0 ? 1 : 3;

            weightedSum += (character - '0') * weight;
        }

        // The check digit completes the weighted sum to the next multiple of ten. The
        // outer modulo keeps a remainder of 0 mapping to 0 rather than an invalid 10.
        return (10 - weightedSum % 10) % 10;
    }
}
