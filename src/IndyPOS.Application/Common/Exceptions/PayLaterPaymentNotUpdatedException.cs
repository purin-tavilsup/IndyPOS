namespace IndyPOS.Application.Common.Exceptions;

public class PayLaterPaymentNotUpdatedException : Exception
{
    public PayLaterPaymentNotUpdatedException(string message) : base(message) { }
}