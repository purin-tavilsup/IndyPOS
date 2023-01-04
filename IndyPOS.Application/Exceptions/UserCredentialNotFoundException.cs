namespace IndyPOS.Application.Exceptions;

public class UserCredentialNotFoundException: Exception
{
	public UserCredentialNotFoundException(string message) : base(message) { }
}