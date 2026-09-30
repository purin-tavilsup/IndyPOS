using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

/// <summary>
/// Spec 2026-09-30 §4.2: what a sale's payments must satisfy before anything is saved. A PayLater
/// sale is paid wholly on credit, by a named customer, and never on a refund.
/// </summary>
public class SalePaymentRulesTests
{
    private const decimal Total = 350m;
    private const string CustomerName = "ลุงสมชาย";
    private const string DisabledCampaign = PaymentMethodCodes.M33WeLove;

    private static readonly IReadOnlySet<string> Offerable =
        new HashSet<string>([PaymentMethodCodes.Cash, PaymentMethodCodes.PayLater], StringComparer.OrdinalIgnoreCase);

    private static SalePaymentRequest Credit(decimal amount = Total, string? note = CustomerName) =>
        new(PaymentMethodCodes.PayLater, amount, note);

    private static Action Validate(decimal invoiceTotal, params SalePaymentRequest[] payments) =>
        () => SalePaymentRules.EnsureValid(payments, invoiceTotal, Offerable);

    [Fact]
    public void EnsureValid_WithAMethodNotOfferable_Throws()
    {
        Validate(Total, new SalePaymentRequest(DisabledCampaign, Total)).Should()
                                                                        .Throw<SaleValidationException>()
                                                                        .WithMessage($"*'{DisabledCampaign}'*");
    }

    [Fact]
    public void EnsureValid_WithPayLaterAndNoNote_Throws()
    {
        Validate(Total, Credit(note: null)).Should()
                                           .Throw<SaleValidationException>()
                                           .WithMessage("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");
    }

    [Fact]
    public void EnsureValid_WithPayLaterAndAWhitespaceNote_Throws()
    {
        Validate(Total, Credit(note: "   ")).Should()
                                            .Throw<SaleValidationException>()
                                            .WithMessage("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");
    }

    [Fact]
    public void EnsureValid_WithACustomerNameOver500Characters_Throws()
    {
        var name = new string('ก', SalePaymentRules.MaxCustomerNameLength + 1);

        Validate(Total, Credit(note: name)).Should()
                                           .Throw<SaleValidationException>()
                                           .WithMessage("ชื่อลูกค้ายาวเกิน 500 ตัวอักษร");
    }

    [Fact]
    public void EnsureValid_WithPayLaterMixedWithCash_Throws()
    {
        Validate(Total, Credit(amount: 200m), new SalePaymentRequest(PaymentMethodCodes.Cash, 150m)).Should()
                                                                                                    .Throw<SaleValidationException>()
                                                                                                    .WithMessage("การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น");
    }

    [Fact]
    public void EnsureValid_WithTwoPayLaterPayments_Throws()
    {
        Validate(Total, Credit(amount: 200m), Credit(amount: 150m)).Should()
                                                                   .Throw<SaleValidationException>()
                                                                   .WithMessage("การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น");
    }

    [Fact]
    public void EnsureValid_WithPayLaterOnARefund_Throws()
    {
        Validate(-Total, Credit(amount: Total)).Should()
                                               .Throw<SaleValidationException>()
                                               .WithMessage("ไม่สามารถลงบัญชีบิลคืนสินค้าได้");
    }

    [Fact]
    public void EnsureValid_WithPayLaterOnAnEmptySale_Throws()
    {
        Validate(0m, Credit(amount: 0m)).Should()
                                        .Throw<SaleValidationException>()
                                        .WithMessage("ไม่สามารถลงบัญชีบิลคืนสินค้าได้");
    }

    [Fact]
    public void EnsureValid_WithANegativePayLaterAmount_Throws()
    {
        Validate(Total, Credit(amount: -1m)).Should()
                                            .Throw<SaleValidationException>()
                                            .WithMessage("ยอดลงบัญชีต้องมากกว่า 0");
    }

    [Fact]
    public void EnsureValid_WithAZeroPayLaterAmount_Throws()
    {
        Validate(Total, Credit(amount: 0m)).Should()
                                           .Throw<SaleValidationException>()
                                           .WithMessage("ยอดลงบัญชีต้องมากกว่า 0");
    }

    [Fact]
    public void EnsureValid_WithAPayLaterAmountBelowTheTotal_Throws()
    {
        Validate(Total, Credit(amount: Total - 1m)).Should()
                                                   .Throw<SaleValidationException>()
                                                   .WithMessage("ยอดลงบัญชีต้องเท่ากับยอดบิล");
    }

    [Fact]
    public void EnsureValid_WithAPayLaterCoveringTheWholeBill_DoesNotThrow()
    {
        Validate(Total, Credit()).Should()
                                 .NotThrow();
    }

    [Fact]
    public void EnsureValid_WithALowerCasePayLaterCode_AppliesThePayLaterRules()
    {
        Validate(Total, new SalePaymentRequest("paylater", Total, Note: null)).Should()
                                                                              .Throw<SaleValidationException>()
                                                                              .WithMessage("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");
    }

    [Fact]
    public void EnsureValid_WithExactly500Characters_DoesNotThrow()
    {
        var name = new string('ก', SalePaymentRules.MaxCustomerNameLength);

        Validate(Total, Credit(note: name)).Should()
                                           .NotThrow();
    }

    // The stored name is the trimmed one, so padding does not count towards the limit.
    [Fact]
    public void EnsureValid_WithA500CharacterNameAndPadding_DoesNotThrow()
    {
        var name = $"  {new string('ก', SalePaymentRules.MaxCustomerNameLength)}  ";

        Validate(Total, Credit(note: name)).Should()
                                           .NotThrow();
    }

    // Codex P2 (PR #106): each emoji is two UTF-16 code units but one varchar character.
    [Fact]
    public void EnsureValid_WithA500CharacterNameOfEmoji_DoesNotThrow()
    {
        var name = string.Concat(Enumerable.Repeat("🙂", SalePaymentRules.MaxCustomerNameLength));

        Validate(Total, Credit(note: name)).Should()
                                           .NotThrow();
    }

    [Fact]
    public void EnsureValid_WithCashAndMoneyTransfer_DoesNotThrow()
    {
        var offerable = new HashSet<string>([PaymentMethodCodes.Cash, "MoneyTransfer"], StringComparer.OrdinalIgnoreCase);
        SalePaymentRequest[] payments = [new(PaymentMethodCodes.Cash, 200m), new("MoneyTransfer", 150m)];

        var act = () => SalePaymentRules.EnsureValid(payments, Total, offerable);

        act.Should()
           .NotThrow();
    }
}
