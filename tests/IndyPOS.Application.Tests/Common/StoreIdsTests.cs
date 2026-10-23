using FluentAssertions;
using IndyPOS.Application.Common.Validation;
using Xunit;

namespace IndyPOS.Application.Tests.Common;

/// <summary>
/// A store id becomes a URL segment in Cloud.Auth's store-client API and a client id. "/" and "%" are decoded
/// differently along the way, so "a/b" and "a%2Fb" could land on one Auth client and one register would rotate the
/// other store's secret. Only plain ids that fit both databases' 50-character keys are allowed.
/// </summary>
public class StoreIdsTests
{
    [Theory]
    [InlineData("STORE-001")]
    [InlineData("DEV-GENERALHARDWARE")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("store_1")]
    public void IsValid_WithAPlainId_ReturnsTrue(string storeId) =>
        StoreIds.IsValid(storeId).Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a/b")]
    [InlineData("a%2Fb")]
    [InlineData("..")]
    [InlineData("a b")]
    [InlineData("ร้าน")]
    public void IsValid_WithAnIdThatIsNotPlain_ReturnsFalse(string storeId) =>
        StoreIds.IsValid(storeId).Should().BeFalse();

    [Fact]
    public void IsValid_WithAnIdLongerThan50Characters_ReturnsFalse() =>
        StoreIds.IsValid(new string('a', 51)).Should().BeFalse();

    [Fact]
    public void IsValid_WithNull_ReturnsFalse() =>
        StoreIds.IsValid(null).Should().BeFalse();
}
