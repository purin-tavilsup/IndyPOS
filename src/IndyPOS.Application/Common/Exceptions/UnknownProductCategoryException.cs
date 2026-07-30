namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// The requested category code is not in this store's catalogue. A malformed request (400), not a
/// conflict: accepting it would file a product under a category nothing can resolve.
/// </summary>
public class UnknownProductCategoryException : Exception
{
    public UnknownProductCategoryException(string message) : base(message) { }
}
