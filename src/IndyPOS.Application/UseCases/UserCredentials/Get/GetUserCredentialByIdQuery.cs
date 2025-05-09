using Nokpirab;

namespace IndyPOS.Application.UseCases.UserCredentials.Get;

public record GetUserCredentialByIdQuery(int Id) : IQuery<UserCredentialDto>;