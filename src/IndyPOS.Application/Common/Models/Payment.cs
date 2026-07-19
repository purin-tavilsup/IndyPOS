using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class Payment
{
    public int PaymentTypeId { get; init; }

    /// <summary>Catalog payment-method Code (e.g. "MoneyTransfer"). Null for legacy enum-path payments.</summary>
    public string? Method { get; init; }

    public decimal Amount { get; init; }

    public int Priority { get; init; }

    public string Note { get; init; } = string.Empty;
}