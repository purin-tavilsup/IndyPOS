using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Interfaces;
using IndyPOS.Common.Enums;
using IndyPOS.Common.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;

namespace IndyPOS.Windows.Forms.UI.Report;

[ExcludeFromCodeCoverage]
public partial class SaleHistoryByInvoiceIdForm : Form
{
	private readonly IReportController _reportController;
	private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;

	private enum ProductColumn
	{
		ProductCode,
		Description,
		Quantity,
		UnitPrice,
		Total,
		Category,
		Note
	}

	private enum PaymentColumn
	{
		PaymentType,
		PaymentAmount,
		Note
	}

	public SaleHistoryByInvoiceIdForm(IReportController reportController,
									  IStoreConstants storeConstants)
	{
		_reportController = reportController;
		_paymentTypeDictionary = storeConstants.PaymentTypes;

		InitializeComponent();
		InitializeInvoiceProductsDataView();
		InitializePaymentDataView();
	}

	private void InitializeInvoiceProductsDataView()
	{
		#region Initialize all columns

		InvoiceProductsDataView.Columns.Clear();
		InvoiceProductsDataView.ColumnCount = 7;

		InvoiceProductsDataView.Columns[(int)ProductColumn.ProductCode].Name = "รหัสสินค้า";
		InvoiceProductsDataView.Columns[(int)ProductColumn.ProductCode].Width = 200;
		InvoiceProductsDataView.Columns[(int)ProductColumn.ProductCode].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.Description].Name = "คำอธิบาย";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Description].Width = 350;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Description].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.Quantity].Name = "จำนวน";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Quantity].Width = 100;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Quantity].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.UnitPrice].Name = "ราคาต่อหน่วย";
		InvoiceProductsDataView.Columns[(int)ProductColumn.UnitPrice].Width = 150;
		InvoiceProductsDataView.Columns[(int)ProductColumn.UnitPrice].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.Total].Name = "ราคารวม";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Total].Width = 150;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Total].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].Name = "ประเภทสินค้า";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].Width = 200;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.Note].Name = "Note";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Note].Width = 200;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Note].ReadOnly = true;

		#endregion
	}

	private void InitializePaymentDataView()
	{
		#region Initialize all columns

		PaymentDataView.Columns.Clear();
		PaymentDataView.ColumnCount = 3;

		PaymentDataView.Columns[(int)PaymentColumn.PaymentType].Name = "ประเภทการชำระเงิน";
		PaymentDataView.Columns[(int)PaymentColumn.PaymentType].Width = 300;
		PaymentDataView.Columns[(int)PaymentColumn.PaymentType].ReadOnly = true;

		PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].Name = "จำนวนเงิน";
		PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].Width = 250;
		PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].ReadOnly = true;

		PaymentDataView.Columns[(int)PaymentColumn.Note].Name = "Note";
		PaymentDataView.Columns[(int)PaymentColumn.Note].Width = 200;
		PaymentDataView.Columns[(int)PaymentColumn.Note].ReadOnly = true;

		#endregion
	}

	public void ShowDialog(int invoiceId)
	{
		InvoiceIdLabel.Text = $"Invoice ID: {invoiceId}";

		ShowInvoiceProductsByInvoiceId(invoiceId);
		ShowInvoicePaymentsByInvoiceId(invoiceId);

		ShowDialog();
	}

	private void ShowInvoiceProductsByInvoiceId(int invoiceId)
	{
		var hardwareProductsTotal = 0m;
		var generalProductsTotal = 0m;
		var products = _reportController.GetInvoiceProductsByInvoiceId(invoiceId);

		InvoiceProductsDataView.Rows.Clear();

		foreach (var product in products)
		{
			var total = product.UnitPrice * product.Quantity;

			if (IsHardwareProduct(product))
			{
				hardwareProductsTotal += total;
			}
			else
			{
				generalProductsTotal += total;
			}

			AddProductToInvoiceDataView(product);
		}

		GeneralProductsTotalLabel.Text = $"{generalProductsTotal:N}";
		HardwareProductsTotalLabel.Text = $"{hardwareProductsTotal:N}";
	}

	private void ShowInvoicePaymentsByInvoiceId(int invoiceId)
	{
		var payments = _reportController.GetPaymentsByInvoiceId(invoiceId);

		PaymentDataView.Rows.Clear();

		foreach (var payment in payments)
		{
			AddPaymentToPaymentDataView(payment);
		}
	}

	private void AddProductToInvoiceDataView(IFinalInvoiceProduct product)
	{
		var columnCount = InvoiceProductsDataView.ColumnCount;
		var row = new object[columnCount];
		var total = product.UnitPrice * product.Quantity;

		row[(int) ProductColumn.ProductCode] = product.Barcode;
		row[(int) ProductColumn.Description] = product.Description;
		row[(int) ProductColumn.Quantity] = product.Quantity;
		row[(int) ProductColumn.UnitPrice] = product.UnitPrice;
		row[(int) ProductColumn.Total] = total;
		row[(int) ProductColumn.Category] = IsHardwareProduct(product) ? "สินค้าฮาร์ดแวร์" : "สินค้าเบ็ดเตล็ด";
		row[(int) ProductColumn.Note] = product.Note;

		var rowIndex = InvoiceProductsDataView.Rows.Add(row);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		InvoiceProductsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private void AddPaymentToPaymentDataView(IFinalInvoicePayment payment)
	{
		var columnCount = PaymentDataView.ColumnCount;
		var row = new object[columnCount];
			
		row[(int)PaymentColumn.PaymentType] = _paymentTypeDictionary[payment.PaymentTypeId];
		row[(int)PaymentColumn.PaymentAmount] = payment.Amount;
		row[(int) PaymentColumn.Note] = payment.Note;

		var rowIndex = PaymentDataView.Rows.Add(row);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		PaymentDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private static bool IsHardwareProduct(IFinalInvoiceProduct product)
	{
		return product.Category >= (int) ProductCategory.Hardware;
	}

	private void CloseButton_Click(object sender, EventArgs e)
	{
		Hide();
	}
}