namespace IndyPOS.Common.Exceptions;

public class ProductNotAddedException : Exception
{
	public ProductNotAddedException(string message) : base(message) { }
}