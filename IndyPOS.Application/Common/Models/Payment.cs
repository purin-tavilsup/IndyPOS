using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class Payment : IPayment
{
    public int PaymentTypeId { get; set; }

    public decimal Amount { get; set; }

    public int Priority { get; set; }

    public string Note { get; set; }
}