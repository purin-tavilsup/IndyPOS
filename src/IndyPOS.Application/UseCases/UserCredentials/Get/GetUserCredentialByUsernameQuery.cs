using Nokpirab;

namespace IndyPOS.Application.UseCases.UserCredentials.Get;

public record GetUserCredentialByUsernameQuery(string Username) : IQuery<UserCredentialDto>;