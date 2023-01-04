namespace IndyPOS.Application.Exceptions;

public class PayLaterPaymentNotUpdatedException : Exception
{
    public PayLaterPaymentNotUpdatedException(string message) : base(message) { }
}