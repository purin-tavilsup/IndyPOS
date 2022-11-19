﻿using IndyPOS.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.UI;

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
		try
		{
			_saleInvoiceController.PrintReceipt();
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.Show($"Error: {ex.Message}", "Unable To Print Receipt!");
		}
	}

	private void CloseFormButton_Click(object sender, EventArgs e)
	{
		Close();
	}
}