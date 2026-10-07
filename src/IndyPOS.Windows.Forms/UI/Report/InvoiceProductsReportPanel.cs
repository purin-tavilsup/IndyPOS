using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;
using IndyPOS.Windows.Forms.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Windows.Forms.UI;

namespace IndyPOS.Windows.Forms.UI.Report;

[ExcludeFromCodeCoverage]
public partial class InvoiceProductsReportPanel : UserControl
{
	private const int LinesPerPage = 200;

	private readonly IStoreHubClient _storeHubClient;
	private readonly MessageForm _messageForm;
	private readonly IStoreFeaturesProvider _storeFeatures;
	private IReadOnlyList<SaleLineRowDto> _products;

	/// <summary>
	/// Catalogue codes classified as Hardware, refreshed alongside each report fetch. This panel
	/// reports on HISTORICAL rows, so it classifies by looking the stored code up rather than by
	/// an id range — the legacy ranges collide across store types.
	/// </summary>
	private HashSet<string> _hardwareCodes = new(StringComparer.Ordinal);

	/// <summary>
	/// False until the catalogue has been fetched at least once. Without it an empty
	/// <see cref="_hardwareCodes"/> is indistinguishable from "this store sells no hardware",
	/// and the panel would report a plausible but wrong split instead of an error.
	/// </summary>
	private bool _catalogueLoaded;

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

	public InvoiceProductsReportPanel(IStoreHubClient storeHubClient,
									  IStoreFeaturesProvider storeFeatures,
									  MessageForm messageForm)
	{
		_storeHubClient = storeHubClient;
		_storeFeatures = storeFeatures;
		_messageForm = messageForm;
		_products = [];

		InitializeComponent();
		InitializeInvoiceProductsDataView();

		// Follow the store as soon as the panel shows, not only after the first fetch.
		VisibleChanged += async (_, _) =>
		{
			if (Visible)
				await ApplyStoreLayoutAsync();
		};

		StartDatePicker.Value = DateTime.Today;
		EndDatePicker.Value = DateTime.Today;
	}

	private void InitializeInvoiceProductsDataView()
	{
		#region Initialize all columns

		InvoiceProductsDataView.Columns.Clear();
		InvoiceProductsDataView.ColumnCount = 9;

		InvoiceProductsDataView.Columns[(int)ProductColumn.InvoiceId].Name = "เลขที่บิล";
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

	private void AddProductToInvoiceDataView(SaleLineRowDto line)
	{
		var columnCount = InvoiceProductsDataView.ColumnCount;
		var productRow = new object[columnCount];

		productRow[(int) ProductColumn.InvoiceId] = line.InvoiceNumber;
		productRow[(int) ProductColumn.ProductCode] = line.Barcode;
		productRow[(int) ProductColumn.Description] = line.ProductName;
		productRow[(int) ProductColumn.Quantity] = line.Quantity;
		productRow[(int) ProductColumn.UnitPrice] = line.UnitPrice;
		productRow[(int) ProductColumn.Total] = line.LineTotal;
		productRow[(int) ProductColumn.Category] = IsHardwareProductGroup(line) ? "Hardware" : "General";
		productRow[(int) ProductColumn.DateCreated] = line.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
		productRow[(int) ProductColumn.Note] = line.Note;

		var rowIndex = InvoiceProductsDataView.Rows.Add(productRow);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		InvoiceProductsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private bool IsHardwareProductGroup(SaleLineRowDto line)
	{
		return !string.IsNullOrEmpty(line.CategoryCode) && _hardwareCodes.Contains(line.CategoryCode);
	}

	private bool IsGeneralProductGroup(SaleLineRowDto line)
	{
		return !IsHardwareProductGroup(line);
	}

	private void ShowInvoiceProducts(IEnumerable<SaleLineRowDto> products)
	{
		InvoiceProductsDataView.Rows.Clear();

		foreach (var product in products)
		{
			AddProductToInvoiceDataView(product);
		}
	}

	private async Task<IReadOnlyList<SaleLineRowDto>> GetSoldLinesAsync()
	{
		var startDate = StartDatePicker.Value.ToDateOnly();
		var endDate = EndDatePicker.Value.ToDateOnly();

		// The layout first: a store without the hardware split needs no category catalogue, and a failed
		// catalogue fetch must not leave GeneralHardware's filter showing.
		var layout = await ApplyStoreLayoutAsync();
		if (layout.ShowProductTypeSplit)
			await RefreshHardwareCodesAsync();

		return await PageReader.ReadAllAsync(async page =>
		{
			var result = await _storeHubClient.ListSaleLinesAsync(startDate, endDate, page, LinesPerPage);
			return (result.Items, result.HasMore);
		});
	}

	private async Task<TillLayout> ApplyStoreLayoutAsync()
	{
		TillLayout layout;
		try
		{
			layout = TillLayout.For(await _storeFeatures.GetAsync());
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Could not load store features for products sold");
			layout = TillLayout.WhenFeaturesUnavailable;
		}

		groupBox1.Visible = layout.ShowProductTypeSplit;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].Visible = layout.ShowProductTypeSplit;
		if (!layout.ShowProductTypeSplit)
			AllProductGroupsButton.Checked = true;

		return layout;
	}

	private async Task RefreshHardwareCodesAsync()
	{
		try
		{
			var categories = await _storeHubClient.GetProductCategoriesAsync();

			_hardwareCodes = categories
				.Where(c => c.Kind == ProductCategoryKind.Hardware)
				.Select(c => c.Code)
				.ToHashSet(StringComparer.Ordinal);
			_catalogueLoaded = true;
		}
		catch when (_catalogueLoaded)
		{
			// Keep the last known set rather than reclassifying every line as general.
			// If it has NEVER loaded there is no safe set to fall back on, so the exception
			// propagates to ReportErrorHandler rather than showing a wrong hardware/general split.
		}
	}

	private async Task ShowCachedProductsAsync(Func<IEnumerable<SaleLineRowDto>, IEnumerable<SaleLineRowDto>> filter)
	{
		try
		{
			if (_products.Count == 0)
			{
				_products = await GetSoldLinesAsync();
			}

			ShowInvoiceProducts(filter(_products));
		}
		catch (Exception ex)
		{
			ReportErrorHandler.Show(_messageForm, ex);
		}
	}

	private async void GeneralProductsOnlyButton_Click(object sender, EventArgs e)
	{
		await ShowCachedProductsAsync(products => products.Where(IsGeneralProductGroup));
	}

	private async void HardwareProductsOnlyButton_Click(object sender, EventArgs e)
	{
		await ShowCachedProductsAsync(products => products.Where(IsHardwareProductGroup));
	}

	private async void AllProductGroupsButton_Click(object sender, EventArgs e)
	{
		await ShowCachedProductsAsync(products => products);
	}

	private async void ShowProductsByDateRangeButton_Click(object sender, EventArgs e)
	{
		try
		{
			_products = await GetSoldLinesAsync();

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
		catch (Exception ex)
		{
			ReportErrorHandler.Show(_messageForm, ex);
		}
	}
}
