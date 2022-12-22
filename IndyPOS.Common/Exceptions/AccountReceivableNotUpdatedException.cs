namespace IndyPOS.Common.Exceptions;

public class AccountReceivableNotUpdatedException : Exception
{
    public AccountReceivableNotUpdatedException(string message) : base(message) { }
}