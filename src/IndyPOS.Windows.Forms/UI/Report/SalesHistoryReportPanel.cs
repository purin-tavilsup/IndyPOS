using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.InvoicePayments;
using IndyPOS.Application.InvoiceProducts;
using IndyPOS.Application.Invoices;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Report;

[ExcludeFromCodeCoverage]
public partial class SalesHistoryReportPanel : UserControl
{
	private readonly IReportService _reportService;
	private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;

	private enum SaleInvoiceColumn
	{
		InvoiceId,
		InvoiceTotal
	}

	private enum ProductColumn
	{
		ProductCode,
		Description,
		Quantity,
		UnitPrice,
		Total,
		Note
	}

	private enum PaymentColumn
	{
		PaymentType,
		PaymentAmount,
		Note
	}

	public SalesHistoryReportPanel(IReportService reportService, 
								   IStoreConstants storeConstants)
	{
		_reportService = reportService;
		_paymentTypeDictionary = storeConstants.PaymentTypes;

		InitializeComponent();
		InitializeSaleInvoiceDataView();
		InitializeInvoiceProductsDataView();
		InitializePaymentDataView();
	}

	private void InitializeSaleInvoiceDataView()
	{
		#region Initialize all columns

		SaleInvoiceDataView.Columns.Clear();
		SaleInvoiceDataView.ColumnCount = 2;

		SaleInvoiceDataView.Columns[(int)SaleInvoiceColumn.InvoiceId].Name = "Invoice ID";
		SaleInvoiceDataView.Columns[(int)SaleInvoiceColumn.InvoiceId].Width = 200;
		SaleInvoiceDataView.Columns[(int)SaleInvoiceColumn.InvoiceId].ReadOnly = true;

		SaleInvoiceDataView.Columns[(int)SaleInvoiceColumn.InvoiceTotal].Name = "ยอดขาย";
		SaleInvoiceDataView.Columns[(int)SaleInvoiceColumn.InvoiceTotal].Width = 150;
		SaleInvoiceDataView.Columns[(int)SaleInvoiceColumn.InvoiceTotal].ReadOnly = true;
		SaleInvoiceDataView.Columns[(int)SaleInvoiceColumn.InvoiceTotal].DefaultCellStyle.Format = "N2";

		#endregion
	}

	private void InitializeInvoiceProductsDataView()
	{
		#region Initialize all columns

		InvoiceProductsDataView.Columns.Clear();
		InvoiceProductsDataView.ColumnCount = 6;

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
		InvoiceProductsDataView.Columns[(int)ProductColumn.UnitPrice].DefaultCellStyle.Format = "N2";

		InvoiceProductsDataView.Columns[(int)ProductColumn.Total].Name = "ราคารวม";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Total].Width = 150;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Total].ReadOnly = true;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Total].DefaultCellStyle.Format = "N2";

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
		PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].DefaultCellStyle.Format = "N2";

		PaymentDataView.Columns[(int)PaymentColumn.Note].Name = "Note";
		PaymentDataView.Columns[(int)PaymentColumn.Note].Width = 200;
		PaymentDataView.Columns[(int)PaymentColumn.Note].ReadOnly = true;

		#endregion
	}

	private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowReportByTodayButton.Text;

		var invoices = await _reportService.GetInvoicesByPeriodAsync(TimePeriod.Today);

		ShowInvoices(invoices);
	}

	private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowReportByThisMonthButton.Text;

		var invoices = await _reportService.GetInvoicesByPeriodAsync(TimePeriod.ThisMonth);

		ShowInvoices(invoices);
	}

	private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowReportByThisYearButton.Text;

		var invoices = await _reportService.GetInvoicesByPeriodAsync(TimePeriod.ThisYear);

		ShowInvoices(invoices);
	}

	private async void ShowReportByDateRangeButton_Click(object sender, EventArgs e)
	{
		var startDate = StartDatePicker.Value.ToDateOnly();
		var endDate = EndDatePicker.Value.ToDateOnly();

		PeriodLabel.Text = $"{startDate:yyyy MMMM dd} - {endDate:yyyy MMMM dd}";

		var invoices = await _reportService.GetInvoicesByDateRangeAsync(startDate, endDate);

		ShowInvoices(invoices);
	}

	private void ShowInvoices(IEnumerable<InvoiceDto> invoices)
	{
		SaleInvoiceDataView.Rows.Clear();
		InvoiceProductsDataView.Rows.Clear();
		PaymentDataView.Rows.Clear();

		foreach (var invoice in invoices)
		{
			AddInvoiceToInvoiceDataView(invoice);
		}
	}

	private int GetInvoiceIdFromSelectedInvoice()
	{
		var selectedCell = SaleInvoiceDataView.SelectedCells[0];
		var rowIndex = selectedCell.RowIndex;
		var selectedRow = SaleInvoiceDataView.Rows[rowIndex];
		var invoiceId = (int) selectedRow.Cells[(int) SaleInvoiceColumn.InvoiceId].Value;

		return invoiceId;
	}

	private async Task ShowInvoiceProductsByInvoiceIdAsync(int invoiceId)
	{
		var products = await _reportService.GetInvoiceProductsByInvoiceIdAsync(invoiceId);

		InvoiceProductsDataView.Rows.Clear();

		foreach (var product in products)
		{
			AddProductToInvoiceDataView(product);
		}
	}

	private async Task ShowInvoicePaymentsByInvoiceIdAsync(int invoiceId)
	{
		var payments = await _reportService.GetPaymentsByInvoiceIdAsync(invoiceId);

		PaymentDataView.Rows.Clear();

		foreach (var payment in payments)
		{
			AddPaymentToPaymentDataView(payment);
		}
	}

	private void AddInvoiceToInvoiceDataView(InvoiceDto invoice)
	{
		var columnCount = SaleInvoiceDataView.ColumnCount;
		var row = new object[columnCount];

		row[(int)SaleInvoiceColumn.InvoiceId] = invoice.InvoiceId;
		row[(int)SaleInvoiceColumn.InvoiceTotal] = invoice.Total;

		var rowIndex = SaleInvoiceDataView.Rows.Add(row);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		SaleInvoiceDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private void AddProductToInvoiceDataView(InvoiceProductDto product)
	{
		var columnCount = InvoiceProductsDataView.ColumnCount;
		var row = new object[columnCount];
		var total = product.UnitPrice * product.Quantity;

		row[(int) ProductColumn.ProductCode] = product.Barcode;
		row[(int) ProductColumn.Description] = product.Description;
		row[(int) ProductColumn.Quantity] = product.Quantity;
		row[(int) ProductColumn.UnitPrice] = product.UnitPrice;
		row[(int) ProductColumn.Total] = total;
		row[(int) ProductColumn.Note] = product.Note;

		var rowIndex = InvoiceProductsDataView.Rows.Add(row);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		InvoiceProductsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private void AddPaymentToPaymentDataView(InvoicePaymentDto payment)
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

	private async void SaleInvoiceDataView_CellClick(object sender, DataGridViewCellEventArgs e)
	{
		if (SaleInvoiceDataView.SelectedCells.Count == 0)
			return;

		var invoiceId = GetInvoiceIdFromSelectedInvoice();

		await ShowInvoiceProductsByInvoiceIdAsync(invoiceId);
		await ShowInvoicePaymentsByInvoiceIdAsync(invoiceId);
	}
}