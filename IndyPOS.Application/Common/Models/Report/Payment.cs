using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Common.Models.Report;

[ExcludeFromCodeCoverage]
public class Payment
{
    public int Priority { get; set; }
    public string PaymentType { get; set; }
    public double Amount { get; set; }
    public string Note { get; set; }
    public DateTime DateTime { get; set; }
}