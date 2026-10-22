using System.ComponentModel;
using ModelContextProtocol.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace McpSignIn;

[McpServerToolType]
public sealed class WhoAmITool(IHttpContextAccessor httpContextAccessor)
{
    [McpServerTool(Name = "whoami"), Description("Says which IndyPOS person this connection is signed in as.")]
    public string WhoAmI()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return $"Signed in as {user?.FindFirst(Claims.Email)?.Value} "
               + $"(Google subject {user?.FindFirst(Claims.Subject)?.Value}).";
    }
}
