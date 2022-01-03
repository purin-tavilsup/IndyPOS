using IndyPOS.Sales;
using IndyPOS.Users;

namespace IndyPOS.Devices
{
    public interface IReceiptPrinter
	{
		void PrintReceipt(ISaleInvoice saleInvoice, IUserAccount loggedInUser);
	}
}
