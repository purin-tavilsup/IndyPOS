namespace IndyPOS.Application.Common.Exceptions;

public class ProductNotDeletedException : Exception
{
    public ProductNotDeletedException(string message) : base(message) { }
}