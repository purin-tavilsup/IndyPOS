namespace IndyPOS.Common.Exceptions;

public class AccountReceivableNotAddedException : Exception
{
    public AccountReceivableNotAddedException(string message) : base(message) {}
}