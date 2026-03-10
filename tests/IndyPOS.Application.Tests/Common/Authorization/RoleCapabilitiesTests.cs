namespace IndyPOS.Application.Tests.Common.Authorization;

using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Enums;
using Xunit;

public class RoleCapabilitiesTests
{
    // ==========================================
    // Cashier Role Tests
    // ==========================================

    [Fact]
    public void Cashier_HasCapability_ProductsRead_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.Cashier, Capability.ProductsRead);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Cashier_HasCapability_SalesComplete_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.Cashier, Capability.SalesComplete);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Cashier_HasCapability_SyncViewStatus_ReturnsFalse()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.Cashier, Capability.SyncViewStatus);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Cashier_HasCapability_AdminStoresRegister_ReturnsFalse()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.Cashier, Capability.AdminStoresRegister);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(Capability.UsersRead)]
    [InlineData(Capability.UsersCreate)]
    [InlineData(Capability.UsersUpdate)]
    [InlineData(Capability.UsersDeactivate)]
    public void Cashier_HasCapability_UserManagement_ReturnsFalse(string capability)
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.Cashier, capability);

        // Assert
        Assert.False(result);
    }

    // ==========================================
    // StoreManager Role Tests
    // ==========================================

    [Fact]
    public void StoreManager_HasCapability_ProductsRead_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.StoreManager, Capability.ProductsRead);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StoreManager_HasCapability_SalesComplete_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.StoreManager, Capability.SalesComplete);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StoreManager_HasCapability_SyncViewStatus_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.StoreManager, Capability.SyncViewStatus);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StoreManager_HasCapability_AdminStoresRegister_ReturnsFalse()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.StoreManager, Capability.AdminStoresRegister);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(Capability.UsersRead)]
    [InlineData(Capability.UsersCreate)]
    [InlineData(Capability.UsersUpdate)]
    [InlineData(Capability.UsersDeactivate)]
    public void StoreManager_HasCapability_UserManagement_ReturnsFalse(string capability)
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.StoreManager, capability);

        // Assert
        Assert.False(result);
    }

    // ==========================================
    // SystemAdmin Role Tests
    // ==========================================

    [Fact]
    public void SystemAdmin_HasCapability_ProductsRead_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.SystemAdmin, Capability.ProductsRead);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SystemAdmin_HasCapability_SalesComplete_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.SystemAdmin, Capability.SalesComplete);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SystemAdmin_HasCapability_SyncViewStatus_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.SystemAdmin, Capability.SyncViewStatus);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SystemAdmin_HasCapability_AdminStoresRegister_ReturnsTrue()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.SystemAdmin, Capability.AdminStoresRegister);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(Capability.UsersRead)]
    [InlineData(Capability.UsersCreate)]
    [InlineData(Capability.UsersUpdate)]
    [InlineData(Capability.UsersDeactivate)]
    public void SystemAdmin_HasCapability_UserManagement_ReturnsTrue(string capability)
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.SystemAdmin, capability);

        // Assert
        Assert.True(result);
    }

    // ==========================================
    // Invalid Role Tests
    // ==========================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(int.MaxValue)]
    public void InvalidRoleId_HasCapability_ReturnsFalse(int invalidRoleId)
    {
        // Act
        var result = RoleCapabilities.HasCapability(invalidRoleId, Capability.ProductsRead);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidRole_HasCapability_UnknownCapability_ReturnsFalse()
    {
        // Act
        var result = RoleCapabilities.HasCapability((int)UserRole.SystemAdmin, "unknown.capability");

        // Assert
        Assert.False(result);
    }

    // ==========================================
    // GetCapabilities Tests
    // ==========================================

    [Fact]
    public void GetCapabilities_Cashier_ReturnsExpectedCapabilities()
    {
        // Act
        var capabilities = RoleCapabilities.GetCapabilities(UserRole.Cashier);

        // Assert
        Assert.Contains(Capability.ProductsRead, capabilities);
        Assert.Contains(Capability.SalesComplete, capabilities);
        Assert.DoesNotContain(Capability.SyncViewStatus, capabilities);
        Assert.DoesNotContain(Capability.AdminStoresRegister, capabilities);
        Assert.Equal(2, capabilities.Count);
    }

    [Fact]
    public void GetCapabilities_StoreManager_ReturnsExpectedCapabilities()
    {
        // Act
        var capabilities = RoleCapabilities.GetCapabilities(UserRole.StoreManager);

        // Assert
        Assert.Contains(Capability.ProductsRead, capabilities);
        Assert.Contains(Capability.SalesComplete, capabilities);
        Assert.Contains(Capability.SyncViewStatus, capabilities);
        Assert.DoesNotContain(Capability.AdminStoresRegister, capabilities);
        Assert.Equal(3, capabilities.Count);
    }

    [Fact]
    public void GetCapabilities_SystemAdmin_ReturnsAllCapabilities()
    {
        // Act
        var capabilities = RoleCapabilities.GetCapabilities(UserRole.SystemAdmin);

        // Assert
        Assert.Contains(Capability.ProductsRead, capabilities);
        Assert.Contains(Capability.SalesComplete, capabilities);
        Assert.Contains(Capability.SyncViewStatus, capabilities);
        Assert.Contains(Capability.AdminStoresRegister, capabilities);
        Assert.Contains(Capability.UsersRead, capabilities);
        Assert.Contains(Capability.UsersCreate, capabilities);
        Assert.Contains(Capability.UsersUpdate, capabilities);
        Assert.Contains(Capability.UsersDeactivate, capabilities);
        Assert.Equal(8, capabilities.Count);
    }

    [Fact]
    public void GetCapabilities_InvalidRole_ReturnsEmptySet()
    {
        // Act
        var capabilities = RoleCapabilities.GetCapabilities((UserRole)99);

        // Assert
        Assert.Empty(capabilities);
    }
}
