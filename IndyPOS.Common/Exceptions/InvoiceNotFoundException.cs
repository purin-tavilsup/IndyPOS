namespace IndyPOS.Common.Exceptions;

public class InvoiceNotFoundException: Exception
{
	public InvoiceNotFoundException(string message) : base(message)
	{
	}
}