namespace IndyPOS.Application.Interfaces;

public interface IReceiptPrinterHelper
{
	void PrintReceipt(IInvoiceInfo invoiceInfo, IUserAccount loggedInUser);
}