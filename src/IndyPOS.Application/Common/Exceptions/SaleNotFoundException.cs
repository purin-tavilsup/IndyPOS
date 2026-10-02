namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// An unknown bill, or another day's bill asked for by a today-only caller. Mapped to 404 either way,
/// so an old bill number's existence is not revealed.
/// </summary>
public class SaleNotFoundException : Exception
{
    public SaleNotFoundException() : base("ไม่พบบิล") { }

    public SaleNotFoundException(long invoiceNumber) : base($"ไม่พบบิลเลขที่ {invoiceNumber}") { }
}
