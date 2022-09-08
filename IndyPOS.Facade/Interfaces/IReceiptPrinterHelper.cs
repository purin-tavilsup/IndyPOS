namespace IndyPOS.Facade.Interfaces
{
	public interface IReceiptPrinterHelper
	{
		void PrintReceipt(IInvoiceInfo invoiceInfo, IUserAccount loggedInUser);
	}
}
