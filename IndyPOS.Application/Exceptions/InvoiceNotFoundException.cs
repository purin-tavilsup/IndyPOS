namespace IndyPOS.Application.Exceptions;

public class InvoiceNotFoundException: Exception
{
	public InvoiceNotFoundException(string message) : base(message) { }
}