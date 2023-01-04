namespace IndyPOS.Application.Exceptions;

public class ProductNotDeletedException : Exception
{
	public ProductNotDeletedException(string message) : base(message) { }
}