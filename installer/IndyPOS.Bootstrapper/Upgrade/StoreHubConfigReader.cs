using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using IndyPOS.Domain.Enums;
using IndyPOS.Vault;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// What an existing StoreHub <c>appsettings.json</c> says about the store on this
/// machine. Detection (spec section 3) and upgrade preflight (section 4 step 1) both read
/// through this one type, so the two can never disagree about whether a store is
/// upgradeable.
/// </summary>
public sealed record StoreHubConfigFacts(
    bool Exists,
    bool ConnectionStringUsable,
    string? StoreId,
    StoreType? StoreType);

public static class StoreHubConfigReader
{
    /// <summary>Config key the connection string is DPAPI-bound to. Entropy depends on it.</summary>
    public const string ConnectionStringKey = "ConnectionStrings:storehub-db";

    private static readonly StoreHubConfigFacts Absent = new(false, false, null, null);

    /// <param name="unprotect">
    /// Seam for the DPAPI round-trip, so tests need not author a machine-scoped blob.
    /// Defaults to <see cref="SecretProtector.Unprotect"/>.
    /// </param>
    public static StoreHubConfigFacts Read(
        string appSettingsPath,
        Func<string, string, string>? unprotect = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSettingsPath);

        if (!File.Exists(appSettingsPath))
        {
            return Absent;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(appSettingsPath))?.AsObject();
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            // The file is there but unreadable — that is an Unusable store, not a fresh one.
            return new StoreHubConfigFacts(Exists: true, false, null, null);
        }

        if (root is null)
        {
            return new StoreHubConfigFacts(Exists: true, false, null, null);
        }

        var store = Section(root, "store");

        return new StoreHubConfigFacts(
            Exists: true,
            ConnectionStringUsable: IsConnectionStringUsable(root, unprotect ?? SecretProtector.Unprotect),
            StoreId: NonBlank(Value(store, "id")),
            StoreType: ParseStoreType(Value(store, "type")));
    }

    /// <summary>
    /// The plaintext connection string, or null when it is absent, unreadable, or sealed to
    /// another machine. Shares <see cref="Read"/>'s case-insensitive lookup so the upgrade's
    /// preflight and detection can never disagree about the same file.
    /// </summary>
    public static string? ReadConnectionString(
        string appSettingsPath,
        Func<string, string, string>? unprotect = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSettingsPath);

        if (!File.Exists(appSettingsPath))
        {
            return null;
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(appSettingsPath))?.AsObject();
            var value = root is null ? null : Value(Section(root, "connectionStrings"), "storehub-db");

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return SecretProtector.IsProtected(value)
                ? (unprotect ?? SecretProtector.Unprotect)(ConnectionStringKey, value)
                : value;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException
                                      or UnauthorizedAccessException or CryptographicException
                                      or FormatException)
        {
            return null;
        }
    }

    private static bool IsConnectionStringUsable(JsonObject root, Func<string, string, string> unprotect)
    {
        var value = Value(Section(root, "connectionStrings"), "storehub-db");

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!SecretProtector.IsProtected(value))
        {
            return true;
        }

        try
        {
            return !string.IsNullOrWhiteSpace(unprotect(ConnectionStringKey, value));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Sealed on another machine. Nothing here can recover it.
            return false;
        }
    }

    // Config binding is case-insensitive, so a hand-edited PascalCase file must still bind.
    private static JsonObject? Section(JsonObject root, string name) =>
        root.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase))
            .Value as JsonObject;

    private static string? Value(JsonObject? section, string name)
    {
        var node = section?.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase))
                          .Value;

        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<string>();
        }
        catch (InvalidOperationException)
        {
            // JSON value is not a string (e.g., numeric or object). Return null gracefully.
            return null;
        }
    }

    private static string? NonBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    // Only a defined enum NAME counts; Enum.TryParse would also accept "2".
    private static StoreType? ParseStoreType(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var matched = Enum.GetNames<StoreType>()
            .FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));

        return matched is null ? null : Enum.Parse<StoreType>(matched);
    }
}
