using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UserCredentials.Queries.GetUserCredentialById;

public record GetUserCredentialByIdQuery(int Id) : IQuery<UserCredentialDto>;