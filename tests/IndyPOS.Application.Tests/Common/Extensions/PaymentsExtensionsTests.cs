using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Extensions;

public class PaymentsExtensionsTests
{
	[Fact]
	public void HasPayLaterPayment_WithCodeBasedPayLaterPayment_ReturnsTrue()
	{
		// Arrange
		var payments = new List<Payment>
		{
			new() { Method = "PayLater", Amount = 100m }
		};

		// Act
		var result = payments.HasPayLaterPayment();

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void HasPayLaterPayment_WithLegacyEnumPayLaterPayment_ReturnsTrue()
	{
		// Arrange
		var payments = new List<Payment>
		{
			new() { PaymentTypeId = (int)PaymentType.PayLater, Amount = 100m }
		};

		// Act
		var result = payments.HasPayLaterPayment();

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void HasPayLaterPayment_WithNonPayLaterCode_ReturnsFalse()
	{
		// Arrange
		var payments = new List<Payment>
		{
			new() { Method = "Cash", Amount = 100m }
		};

		// Act
		var result = payments.HasPayLaterPayment();

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void HasPayLaterPayment_WithNonPayLaterEnum_ReturnsFalse()
	{
		// Arrange
		var payments = new List<Payment>
		{
			new() { PaymentTypeId = (int)PaymentType.Cash, Amount = 100m }
		};

		// Act
		var result = payments.HasPayLaterPayment();

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void HasPayLaterPayment_WithLowercaseCode_ReturnsTrueCaseInsensitive()
	{
		// Arrange
		var payments = new List<Payment>
		{
			new() { Method = "paylater", Amount = 100m }
		};

		// Act
		var result = payments.HasPayLaterPayment();

		// Assert
		result.Should().BeTrue();
	}
}
