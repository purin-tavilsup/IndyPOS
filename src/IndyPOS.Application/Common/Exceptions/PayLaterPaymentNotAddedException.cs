namespace IndyPOS.Application.Common.Exceptions;

public class PayLaterPaymentNotAddedException : Exception
{
    public PayLaterPaymentNotAddedException(string message) : base(message) { }
}