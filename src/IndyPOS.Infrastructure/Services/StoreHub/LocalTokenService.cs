using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Models;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// JWT token service for local POS authentication.
/// Generates 12-hour tokens for shift-length sessions.
/// </summary>
public class LocalTokenService : ILocalTokenService
{
    private readonly LocalTokenOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public LocalTokenService(IOptions<LocalTokenOptions> options)
    {
        _options = options.Value;
        _tokenHandler = new JwtSecurityTokenHandler();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    public string GenerateToken(StoreUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("role_id", user.RoleId.ToString()),
            new Claim("store_id", user.StoreId),
            new Claim("first_name", user.FirstName),
            new Claim("last_name", user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_options.ExpiryHours),
            signingCredentials: _signingCredentials);

        return _tokenHandler.WriteToken(token);
    }

    public TokenClaims? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            // Claims may be mapped to different types depending on token handler settings
            var userId = Guid.Parse(
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? string.Empty);
            var username =
                principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                ?? string.Empty;
            var roleId = int.Parse(principal.FindFirst("role_id")?.Value ?? "0");
            var storeId = principal.FindFirst("store_id")?.Value ?? string.Empty;

            return new TokenClaims(
                UserId: userId,
                Username: username,
                RoleId: roleId,
                StoreId: storeId,
                ExpiresAt: jwtToken.ValidTo);
        }
        catch
        {
            return null;
        }
    }
}
