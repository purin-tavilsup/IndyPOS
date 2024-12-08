using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.UserCredentials.Delete;

public record DeleteUserCredentialCommand(int Id) : ICommand;