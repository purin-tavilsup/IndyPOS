using IndyPOS.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	[ExcludeFromCodeCoverage]
	public partial class PrintReceiptForm : Form
	{
		private readonly ISaleInvoiceController _saleInvoiceController;

		public PrintReceiptForm(ISaleInvoiceController saleInvoiceController)
		{
			_saleInvoiceController = saleInvoiceController;

			InitializeComponent();
		}

		private void PrintReceiptButton_Click(object sender, EventArgs e)
		{
			_saleInvoiceController.PrintReceipt();
		}

		private void CloseFormButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
