using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.InvoiceProducts;

namespace IndyPOS.Windows.Forms.UI.Report;

[ExcludeFromCodeCoverage]
public partial class InvoiceProductsReportPanel : UserControl
{
	private readonly IReportService _reportService;
	private IEnumerable<InvoiceProductDto> _products;

	private enum ProductColumn
	{
		InvoiceId,
		ProductCode,
		Description,
		Quantity,
		UnitPrice,
		Total,
		Category,
		DateCreated,
		Note
	}

	public InvoiceProductsReportPanel(IReportService reportService)
	{
		_reportService = reportService;
		_products = Enumerable.Empty<InvoiceProductDto>();

		InitializeComponent();
		InitializeInvoiceProductsDataView();

		StartDatePicker.Value = DateTime.Today;
		EndDatePicker.Value = DateTime.Today;
	}

	private void InitializeInvoiceProductsDataView()
	{
		#region Initialize all columns

		InvoiceProductsDataView.Columns.Clear();
		InvoiceProductsDataView.ColumnCount = 9;

		InvoiceProductsDataView.Columns[(int)ProductColumn.InvoiceId].Name = "Invoice ID";
		InvoiceProductsDataView.Columns[(int)ProductColumn.InvoiceId].Width = 200;
		InvoiceProductsDataView.Columns[(int)ProductColumn.InvoiceId].ReadOnly = true;

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

		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].Name = "กลุ่มสินค้า";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].Width = 200;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.DateCreated].Name = "วันและเวลาที่บันทึก";
		InvoiceProductsDataView.Columns[(int)ProductColumn.DateCreated].Width = 200;
		InvoiceProductsDataView.Columns[(int)ProductColumn.DateCreated].ReadOnly = true;

		InvoiceProductsDataView.Columns[(int)ProductColumn.Note].Name = "Note";
		InvoiceProductsDataView.Columns[(int)ProductColumn.Note].Width = 200;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Note].ReadOnly = true;

		#endregion
	}

	private void AddProductToInvoiceDataView(InvoiceProductDto product)
	{
		var columnCount = InvoiceProductsDataView.ColumnCount;
		var productRow = new object[columnCount];
		var total = !product.IsGroupProduct ? product.UnitPrice * product.Quantity : product.GroupPrice;

		productRow[(int) ProductColumn.InvoiceId] = product.InvoiceId;
		productRow[(int) ProductColumn.ProductCode] = product.Barcode;
		productRow[(int) ProductColumn.Description] = product.Description;
		productRow[(int) ProductColumn.Quantity] = product.Quantity;
		productRow[(int) ProductColumn.UnitPrice] = product.UnitPrice;
		productRow[(int) ProductColumn.Total] = total;
		productRow[(int) ProductColumn.Category] = IsHardwareProductGroup(product) ? "Hardware" : "General";
		productRow[(int) ProductColumn.DateCreated] = product.DateCreated;
		productRow[(int) ProductColumn.Note] = product.Note;

		var rowIndex = InvoiceProductsDataView.Rows.Add(productRow);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		InvoiceProductsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private static bool IsHardwareProductGroup(InvoiceProductDto product)
	{
		return product.Category >= (int) ProductCategory.Hardware;
	}

	private static bool IsGeneralProductGroup(InvoiceProductDto product)
	{
		return product.Category < (int) ProductCategory.Hardware;
	}

	private void ShowInvoiceProducts(IEnumerable<InvoiceProductDto> products)
	{
		InvoiceProductsDataView.Rows.Clear();

		foreach (var product in products)
		{
			AddProductToInvoiceDataView(product);
		}
	}

	private async Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsAsync()
	{
		var startDate = StartDatePicker.Value.ToDateOnly();
		var endDate = EndDatePicker.Value.ToDateOnly();

		return await _reportService.GetInvoiceProductsByDateRangeAsync(startDate, endDate);
	}

	private async void GeneralProductsOnlyButton_Click(object sender, EventArgs e)
	{
		if (!_products.Any())
		{
			_products = await GetInvoiceProductsAsync();
		}

		ShowInvoiceProducts(_products.Where(IsGeneralProductGroup));
	}

	private async void HardwareProductsOnlyButton_Click(object sender, EventArgs e)
	{
		if (!_products.Any())
		{
			_products = await GetInvoiceProductsAsync();
		}

		ShowInvoiceProducts(_products.Where(IsHardwareProductGroup));
	}

	private async void AllProductGroupsButton_Click(object sender, EventArgs e)
	{
		if (!_products.Any())
		{
			_products = await GetInvoiceProductsAsync();
		}

		ShowInvoiceProducts(_products);
	}

	private async void ShowProductsByDateRangeButton_Click(object sender, EventArgs e)
	{
		_products = await GetInvoiceProductsAsync();

		if (AllProductGroupsButton.Checked)
		{
			ShowInvoiceProducts(_products);
		}
		else if (HardwareProductsOnlyButton.Checked)
		{
			ShowInvoiceProducts(_products.Where(IsHardwareProductGroup));
		}
		else
		{
			ShowInvoiceProducts(_products.Where(IsGeneralProductGroup));
		}
	}
}