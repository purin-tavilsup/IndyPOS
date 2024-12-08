using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.UserCredentials.Get;

public record GetUserCredentialByIdQuery(int Id) : IQuery<UserCredentialDto>;