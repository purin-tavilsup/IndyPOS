namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Generates the short reference an operator reads back over the phone. The same
/// value is written to the log as an <c>ErrorReference</c> property, so a support
/// call narrows to a single entry.
/// </summary>
/// <remarks>
/// Four hex characters is 65,536 values. Collisions are harmless: entries also
/// carry a timestamp, and the code only has to narrow a search, not be unique
/// forever. Hex also sidesteps the spoken 0/O ambiguity, since O is not a hex digit.
/// </remarks>
public static class UiErrorReference
{
    private const int SignificantCharacters = 4;

    public static string New() =>
        $"ERR-{Guid.NewGuid().ToString("N")[..SignificantCharacters].ToUpperInvariant()}";
}
