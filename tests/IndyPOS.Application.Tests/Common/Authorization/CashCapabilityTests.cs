using FluentAssertions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Authorization;

public class CashCapabilityTests
{
    private const int UnknownRoleId = 99;

    [Fact]
    public void HasCapability_WithUnknownRole_ReturnsFalse()
    {
        var result = RoleCapabilities.HasCapability(UnknownRoleId, Capability.CashManage);

        result.Should()
              .BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.StoreManager)]
    [InlineData(UserRole.SystemAdmin)]
    public void HasCapability_WithStoreRole_GrantsCashManage(UserRole role)
    {
        var result = RoleCapabilities.HasCapability((int)role, Capability.CashManage);

        result.Should()
              .BeTrue();
    }
}
