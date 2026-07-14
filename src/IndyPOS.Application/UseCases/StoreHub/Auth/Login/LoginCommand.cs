using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Auth.Login;

/// <summary>
/// Command to authenticate a user.
/// </summary>
public record LoginCommand(string Username, string Password) : ICommand<LoginResponse>;
