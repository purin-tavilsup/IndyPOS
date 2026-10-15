using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Application.Common.Interfaces;

public interface IReceiptPrinterService
{
    void PrintReceipt(ReceiptDocument document);
}
