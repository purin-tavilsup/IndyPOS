namespace IndyPOS.Application.Exceptions;

public class ProductNotAddedException : Exception
{
	public ProductNotAddedException(string message) : base(message) { }
}