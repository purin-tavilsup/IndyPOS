namespace IndyPOS.Application.Exceptions;

public class UserNotLoggedInException : Exception
{
	public UserNotLoggedInException(string message) : base(message) {}
}