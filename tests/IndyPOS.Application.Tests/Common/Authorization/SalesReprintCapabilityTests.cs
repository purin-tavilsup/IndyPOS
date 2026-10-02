using FluentAssertions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Authorization;

public class SalesReprintCapabilityTests
{
    private const int UnknownRoleId = 99;

    [Fact]
    public void HasCapability_WithUnknownRole_ReturnsFalse()
    {
        RoleCapabilities.HasCapability(UnknownRoleId, Capability.SalesReprint).Should()
                                                                              .BeFalse();
    }

    [Fact]
    public void HasCapability_AsCashier_DoesNotGrantReportsView()
    {
        // The today-only limit rests on this: sales.reprint without reports.view.
        RoleCapabilities.HasCapability((int)UserRole.Cashier, Capability.ReportsView).Should()
                                                                                     .BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.StoreManager)]
    [InlineData(UserRole.SystemAdmin)]
    public void HasCapability_WithStoreRole_GrantsSalesReprint(UserRole role)
    {
        RoleCapabilities.HasCapability((int)role, Capability.SalesReprint).Should()
                                                                          .BeTrue();
    }
}
