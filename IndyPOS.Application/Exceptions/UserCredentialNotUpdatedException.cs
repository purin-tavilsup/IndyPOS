namespace IndyPOS.Application.Exceptions;

public class UserCredentialNotUpdatedException : Exception
{
    public UserCredentialNotUpdatedException(string message) : base(message) { }
}