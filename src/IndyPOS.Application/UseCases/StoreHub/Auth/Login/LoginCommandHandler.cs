using IndyPOS.Application.Abstractions.StoreHub.Services;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Auth.Login;

/// <summary>
/// Handler for LoginCommand - authenticates user via StoreAuthService.
/// </summary>
public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IStoreAuthService _authService;

    public LoginCommandHandler(IStoreAuthService authService)
    {
        _authService = authService;
    }

    public async Task<LoginResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _authService.AuthenticateAsync(
            command.Username,
            command.Password,
            cancellationToken);

        if (!result.Success)
        {
            return new LoginResponse(
                Success: false,
                Token: null,
                User: null,
                ErrorMessage: result.ErrorMessage);
        }

        var userDto = new StoreUserDto(
            Id: result.User!.Id,
            Username: result.User.Username,
            FirstName: result.User.FirstName,
            LastName: result.User.LastName,
            RoleId: result.User.RoleId,
            StoreId: result.User.StoreId);

        return new LoginResponse(
            Success: true,
            Token: result.Token,
            User: userDto,
            ErrorMessage: null,
            MustChangePassword: result.MustChangePassword);
    }
}
