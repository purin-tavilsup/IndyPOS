using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Sale;

[ExcludeFromCodeCoverage]
public partial class PrintReceiptForm : Form
{
	private readonly IReceiptPrinterService _receiptPrinterService;
	private IInvoiceInfo? _invoiceInfo;

	public PrintReceiptForm(IReceiptPrinterService receiptPrinterService)
	{
		_receiptPrinterService = receiptPrinterService;

		InitializeComponent();
	}

	public void ShowDialog(IInvoiceInfo invoiceInfo)
	{
		_invoiceInfo = invoiceInfo;

		ShowDialog();
	}

	private void PrintReceiptButton_Click(object sender, EventArgs e)
	{
		try
		{
			if (_invoiceInfo is null) { return; }

			PrintReceipt(_invoiceInfo);

			Close();
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.Show($"Error: {ex.Message}", "Unable To Print Receipt!");
		}
	}

	[Conditional("RELEASE")]
	private void PrintReceipt(IInvoiceInfo invoiceInfo)
	{
		_receiptPrinterService.PrintReceipt(invoiceInfo);
	}

	private void CloseFormButton_Click(object sender, EventArgs e)
	{
		Close();
	}
}