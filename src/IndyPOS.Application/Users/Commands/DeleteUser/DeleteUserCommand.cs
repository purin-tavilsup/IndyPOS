using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(int Id) : ICommand;