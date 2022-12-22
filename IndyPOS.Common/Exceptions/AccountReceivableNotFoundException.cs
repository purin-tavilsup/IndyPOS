namespace IndyPOS.Common.Exceptions;

public class AccountReceivableNotFoundException : Exception
{
	public AccountReceivableNotFoundException(string message) : base(message) { }
}