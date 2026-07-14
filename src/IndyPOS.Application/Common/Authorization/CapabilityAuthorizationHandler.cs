namespace IndyPOS.Application.Common.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization requirement for capability-based access.
/// </summary>
public class CapabilityRequirement(string capability) : IAuthorizationRequirement
{
    public string Capability { get; } = capability;
}

/// <summary>
/// Handler that checks if the user's role has the required capability.
/// </summary>
public class CapabilityAuthorizationHandler : AuthorizationHandler<CapabilityRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CapabilityRequirement requirement)
    {
        var roleIdClaim = context.User.FindFirst("role_id")?.Value;

        if (!string.IsNullOrEmpty(roleIdClaim)
            && int.TryParse(roleIdClaim, out var roleId)
            && RoleCapabilities.HasCapability(roleId, requirement.Capability))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
