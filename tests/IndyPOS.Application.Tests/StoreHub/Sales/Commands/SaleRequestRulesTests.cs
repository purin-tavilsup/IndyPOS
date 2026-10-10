using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

/// <summary>
/// A body that omits a list, or writes an item as null, reaches the handler as null. Without this
/// check the handler dereferenced it and POST /sales answered 500 instead of a Thai 400.
/// </summary>
public class SaleRequestRulesTests
{
    private static readonly SaleLineRequest Line = new(Guid.NewGuid(), Quantity: 1, UnitPrice: 10m);
    private static readonly SalePaymentRequest Cash = new("Cash", Amount: 10m);

    private static Action Validate(IReadOnlyList<SaleLineRequest?>? lines, IReadOnlyList<SalePaymentRequest?>? payments) =>
        () => SaleRequestRules.EnsureWellFormed(lines, payments);

    [Fact]
    public void EnsureWellFormed_WithNullLines_Throws()
    {
        Validate(null, [Cash]).Should()
                              .Throw<SaleValidationException>()
                              .WithMessage(SaleRequestRules.MalformedLines);
    }

    [Fact]
    public void EnsureWellFormed_WithANullLine_Throws()
    {
        Validate([Line, null], [Cash]).Should()
                                      .Throw<SaleValidationException>()
                                      .WithMessage(SaleRequestRules.MalformedLines);
    }

    [Fact]
    public void EnsureWellFormed_WithNullPayments_Throws()
    {
        Validate([Line], null).Should()
                              .Throw<SaleValidationException>()
                              .WithMessage(SaleRequestRules.MalformedPayments);
    }

    [Fact]
    public void EnsureWellFormed_WithANullPayment_Throws()
    {
        Validate([Line], [Cash, null]).Should()
                                      .Throw<SaleValidationException>()
                                      .WithMessage(SaleRequestRules.MalformedPayments);
    }

    // An empty sale is a zero sale, which POST /sales accepts (SalesEndpointTests pins it).
    [Fact]
    public void EnsureWellFormed_WithEmptyLists_DoesNotThrow()
    {
        Validate([], []).Should()
                        .NotThrow();
    }

    [Fact]
    public void EnsureWellFormed_WithALineAndAPayment_DoesNotThrow()
    {
        Validate([Line], [Cash]).Should()
                                .NotThrow();
    }
}
