namespace IndyPOS.Common.Exceptions;

public class UserNotUpdatedException : Exception
{
    public UserNotUpdatedException(string message) : base(message) { }
}