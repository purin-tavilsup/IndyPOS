using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class Payment
{
    public int PaymentTypeId { get; init; }

    public decimal Amount { get; init; }

    public int Priority { get; init; }

    public string Note { get; init; } = string.Empty;
}