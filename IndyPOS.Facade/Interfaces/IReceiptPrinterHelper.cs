namespace IndyPOS.Facade.Interfaces
{
	public interface IReceiptPrinterHelper
	{
		void PrintReceipt(ISaleInvoice saleInvoice, IUserAccount loggedInUser);
	}
}
