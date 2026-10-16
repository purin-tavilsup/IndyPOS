using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Sale;

[ExcludeFromCodeCoverage]
public partial class PrintReceiptForm : Form
{
	private readonly IReceiptPrinterService _receiptPrinterService;
	private ReceiptDocument? _document;

	public PrintReceiptForm(IReceiptPrinterService receiptPrinterService)
	{
		_receiptPrinterService = receiptPrinterService;

		InitializeComponent();
	}

	public void ShowDialog(ReceiptDocument document)
	{
		_document = document;

		ShowDialog();
	}

	private void PrintReceiptButton_Click(object sender, EventArgs e)
	{
		try
		{
			if (_document is null) { return; }

			PrintReceipt(_document);
			
			Hide();
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.ShowDialog($"Error: {ex.Message}", "Unable To Print Receipt!");
		}
	}

	private void PrintReceipt(ReceiptDocument document)
	{
		_receiptPrinterService.PrintReceipt(document);
	}

	private void CloseFormButton_Click(object sender, EventArgs e)
	{
		Hide();
	}
}