namespace IndyPOS.Application.Common.Interfaces;

public interface IReceiptPrinterHelper
{
    void PrintReceipt(IInvoiceInfo invoiceInfo, IUserAccount loggedInUser);
}