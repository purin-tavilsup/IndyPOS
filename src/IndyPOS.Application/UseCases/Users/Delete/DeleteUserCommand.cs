using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Users.Delete;

public record DeleteUserCommand(int Id) : ICommand;