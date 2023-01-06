namespace IndyPOS.Application.Common.Exceptions;

public class UserNotCreatedException : Exception
{
    public UserNotCreatedException(string message) : base(message) { }
}