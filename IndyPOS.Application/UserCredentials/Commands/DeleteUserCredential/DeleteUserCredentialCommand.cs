using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UserCredentials.Commands.DeleteUserCredential;

public record DeleteUserCredentialCommand(int Id) : ICommand;