using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UserCredentials.Queries.GetUserCredentialByUsername;

public record GetUserCredentialByUsernameQuery(string Username) : IQuery<UserCredentialDto>;