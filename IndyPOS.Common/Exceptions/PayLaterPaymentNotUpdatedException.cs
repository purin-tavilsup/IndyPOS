namespace IndyPOS.Common.Exceptions;

public class PayLaterPaymentNotUpdatedException : Exception
{
    public PayLaterPaymentNotUpdatedException(string message) : base(message) { }
}