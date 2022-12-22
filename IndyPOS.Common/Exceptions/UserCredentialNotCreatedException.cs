namespace IndyPOS.Common.Exceptions;

public class UserCredentialNotCreatedException : Exception
{
    public UserCredentialNotCreatedException(string message) : base(message) { }
}