namespace IndyPOS.Common.Exceptions;

public class UserNotDeletedException : Exception
{
    public UserNotDeletedException(string message) : base(message) { }
}