namespace IndyPOS.Application.Exceptions;

public class PayLaterPaymentNotFoundException : Exception
{
	public PayLaterPaymentNotFoundException(string message) : base(message) { }
}