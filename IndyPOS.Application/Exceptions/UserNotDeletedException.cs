namespace IndyPOS.Application.Exceptions;

public class UserNotDeletedException : Exception
{
    public UserNotDeletedException(string message) : base(message) { }
}