namespace IndyPOS.Application.Common.Exceptions;

/// <summary>Another repayment on the same debt was saved first; nothing was saved, so the till can reload and retry.</summary>
public class PayLaterPaymentConflictException : Exception
{
    public PayLaterPaymentConflictException(string message) : base(message) { }
}
