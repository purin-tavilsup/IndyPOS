namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A store sent a sync batch whose events list, or an event in it, is missing. Rejected whole, before
/// anything is stored, as a malformed request rather than a server fault.
/// </summary>
public class MalformedSyncBatchException(string message) : Exception(message);
