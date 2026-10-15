namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A StoreHub call failed. The message is safe to show the user: StoreHub's own Thai reason when it
/// sent one. Declared here so a ViewModel can catch it without referencing Infrastructure.
/// </summary>
public class StoreHubException : Exception
{
    public StoreHubException(string message) : base(message) { }

    public StoreHubException(string message, Exception innerException) : base(message, innerException) { }
}
