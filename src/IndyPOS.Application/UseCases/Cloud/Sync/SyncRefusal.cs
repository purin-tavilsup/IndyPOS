namespace IndyPOS.Application.UseCases.Cloud.Sync;

/// <summary>
/// The marker on a /sync/events refusal that the cloud gives on purpose. StoreHub treats only a marked
/// 400 or 403 as final and parks the event as Failed; any other failure status, from a proxy, a missing
/// route or the auth layer, looks like an outage and is retried.
/// </summary>
public static class SyncRefusal
{
    /// <summary>The problem details extension that carries the refusal code.</summary>
    public const string ExtensionKey = "syncRefusal";

    public const string MalformedBatch = "malformed-batch";

    public const string StoreMismatch = "store-mismatch";
}
