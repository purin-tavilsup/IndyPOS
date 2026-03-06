using FluentAssertions;
using IndyPOS.Application.UseCases.PayLaterPayments;
using Xunit;

namespace IndyPOS.Application.Tests.PayLaterPayments;

public class PayLaterPaymentDtoTests
{
    [Fact]
    public void WouldBeCompletedWith_WhenPaidAmountEqualsReceivable_ReturnsTrue()
    {
        // Arrange
        var dto = new PayLaterPaymentDto(
            PaymentId: 1,
            Description: "Customer A",
            InvoiceId: 100,
            ReceivableAmount: 500m,
            PaidAmount: 0m,
            IsCompleted: false,
            DateCreated: "2024-01-01",
            DateUpdated: "2024-01-01"
        );

        // Act
        var result = dto.WouldBeCompletedWith(500m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void WouldBeCompletedWith_WhenPaidAmountGreaterThanReceivable_ReturnsTrue()
    {
        // Arrange
        var dto = new PayLaterPaymentDto(
            PaymentId: 1,
            Description: "Customer A",
            InvoiceId: 100,
            ReceivableAmount: 500m,
            PaidAmount: 0m,
            IsCompleted: false,
            DateCreated: "2024-01-01",
            DateUpdated: "2024-01-01"
        );

        // Act
        var result = dto.WouldBeCompletedWith(600m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void WouldBeCompletedWith_WhenPaidAmountLessThanReceivable_ReturnsFalse()
    {
        // Arrange
        var dto = new PayLaterPaymentDto(
            PaymentId: 1,
            Description: "Customer A",
            InvoiceId: 100,
            ReceivableAmount: 500m,
            PaidAmount: 0m,
            IsCompleted: false,
            DateCreated: "2024-01-01",
            DateUpdated: "2024-01-01"
        );

        // Act
        var result = dto.WouldBeCompletedWith(400m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemainingAmount_ReturnsCorrectDifference()
    {
        // Arrange
        var dto = new PayLaterPaymentDto(
            PaymentId: 1,
            Description: "Customer A",
            InvoiceId: 100,
            ReceivableAmount: 500m,
            PaidAmount: 200m,
            IsCompleted: false,
            DateCreated: "2024-01-01",
            DateUpdated: "2024-01-01"
        );

        // Act & Assert
        dto.RemainingAmount.Should().Be(300m);
    }

    [Fact]
    public void RemainingAmount_WhenFullyPaid_ReturnsZero()
    {
        // Arrange
        var dto = new PayLaterPaymentDto(
            PaymentId: 1,
            Description: "Customer A",
            InvoiceId: 100,
            ReceivableAmount: 500m,
            PaidAmount: 500m,
            IsCompleted: true,
            DateCreated: "2024-01-01",
            DateUpdated: "2024-01-01"
        );

        // Act & Assert
        dto.RemainingAmount.Should().Be(0m);
    }
}
