namespace IndyPOS.Application.Common.Interfaces;

public interface IReceiptPrinterService
{
    void PrintReceipt(IInvoiceInfo invoiceInfo, IUserAccount loggedInUser);
}