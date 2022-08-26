namespace IndyPOS.Interfaces
{
	public interface IReceiptPrinter
	{
		void PrintReceipt(ISaleInvoice saleInvoice, IUserAccount loggedInUser);
	}
}
