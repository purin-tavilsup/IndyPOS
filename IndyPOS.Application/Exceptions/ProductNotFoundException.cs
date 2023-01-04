namespace IndyPOS.Application.Exceptions;

public class ProductNotFoundException : Exception
{
	public ProductNotFoundException(string message) : base(message) { }
}