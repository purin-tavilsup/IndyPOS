using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Silent;

/// <param name="StoreId">
/// Null is legal: an upgrade adopts the detected store id. The router rejects a supplied
/// value that conflicts with the detected one — silently rewriting store identity would
/// orphan the store's sales history.
/// </param>
public sealed record SilentInstallOptions(
    string? StoreId,
    int TimeoutMinutes,
    StoreType StoreType = StoreType.GeneralHardware,
    UpgradeStage? SimulateFailure = null);

public enum ParseStatus { Silent, NotSilent, UsageError }

public sealed record ParseResult(ParseStatus Status, SilentInstallOptions? Options, string? ErrorMessage)
{
    public static ParseResult Silent(SilentInstallOptions options) => new(ParseStatus.Silent, options, null);
    public static ParseResult NotSilent() => new(ParseStatus.NotSilent, null, null);
    public static ParseResult Usage(string message) => new(ParseStatus.UsageError, null, message);
}

/// <summary>
/// Pure parser for the silent-install command line. No I/O — fully unit-testable.
/// Flag NAMES are case-insensitive; the --store-id VALUE is preserved verbatim
/// (only trimmed).
/// </summary>
public static class SilentArgs
{
    private const int DefaultTimeoutMinutes = 45;

    public static ParseResult Parse(string[] args)
    {
        var isSilent = args.Any(a => NameOf(a).Equals("--silent", StringComparison.OrdinalIgnoreCase));
        if (!isSilent) return ParseResult.NotSilent();

        string? storeId = null;
        var timeoutMinutes = DefaultTimeoutMinutes;
        var storeType = StoreType.GeneralHardware;
        UpgradeStage? simulateFailure = null;

        for (var i = 0; i < args.Length; i++)
        {
            var name = NameOf(args[i]);
            var inlineValue = InlineValueOf(args[i]);

            switch (name.ToLowerInvariant())
            {
                case "--silent":
                    break;

                case "--store-id":
                    if (!TryReadValue(args, ref i, inlineValue, out storeId))
                        return ParseResult.Usage("--store-id requires a value.");
                    break;

                case "--timeout-minutes":
                    if (!TryReadValue(args, ref i, inlineValue, out var raw)
                        || !int.TryParse(raw, out timeoutMinutes)
                        || timeoutMinutes <= 0)
                        return ParseResult.Usage("--timeout-minutes requires a positive integer.");
                    break;

                case "--store-type":
                    if (!TryReadValue(args, ref i, inlineValue, out var storeTypeRaw)
                        || !TryParseStoreTypeName(storeTypeRaw, out storeType))
                        return ParseResult.Usage(
                            "--store-type must be one of: GeneralHardware, Minimart, CoffeeShop.");
                    break;

                case SimulatedFailure.ArgumentName:
                    if (!TryReadValue(args, ref i, inlineValue, out var stageRaw)
                        || (simulateFailure = SimulatedFailure.Parse(stageRaw)) is null)
                        return ParseResult.Usage(
                            $"{SimulatedFailure.ArgumentName} must name a stage (test hook).");
                    break;

                default:
                    return ParseResult.Usage($"Unknown argument: {args[i]}");
            }
        }

        // A supplied-but-blank value is still a usage error; an absent flag is not — the
        // router decides, because an upgrade adopts the store id it detected.
        if (storeId is not null && string.IsNullOrWhiteSpace(storeId))
            return ParseResult.Usage("--store-id requires a non-empty value.");

        return ParseResult.Silent(
            new SilentInstallOptions(storeId?.Trim(), timeoutMinutes, storeType, simulateFailure));
    }

    // Enum.TryParse<StoreType> also accepts defined underlying numeric values (e.g.
    // "2" -> Minimart), which is not a valid --store-type token. Only accept a
    // case-insensitive match against a defined enum NAME.
    private static bool TryParseStoreTypeName(string? value, out StoreType storeType)
    {
        storeType = StoreType.GeneralHardware;

        if (value is null)
            return false;

        var matchedName = Enum.GetNames<StoreType>()
            .FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));

        if (matchedName is null)
            return false;

        storeType = Enum.Parse<StoreType>(matchedName);
        return true;
    }

    private static string NameOf(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq >= 0 ? arg[..eq] : arg;
    }

    private static string? InlineValueOf(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq >= 0 ? arg[(eq + 1)..] : null;
    }

    // Reads the value for a flag: the inline "=value" if present, else the next
    // token (which must exist and not itself be a flag). Advances i past a
    // consumed next token so it is not re-parsed as unknown.
    private static bool TryReadValue(string[] args, ref int i, string? inlineValue, out string? value)
    {
        if (inlineValue is not null)
        {
            value = inlineValue;
            return true;
        }

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        {
            value = args[++i];
            return true;
        }

        value = null;
        return false;
    }
}
