namespace IndyPOS.Application.Exceptions;

public class ProductNotUpdatedException : Exception
{
	public ProductNotUpdatedException(string message) : base(message) { }
}