using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Domain.Events;
using IndyPOS.Windows.Forms.Enums;
using IndyPOS.Windows.Forms.Events;
using IndyPOS.Windows.Forms.Extensions;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.InventoryProducts;

namespace IndyPOS.Windows.Forms.UI.Inventory;

[ExcludeFromCodeCoverage]
public partial class InventoryPanel : UserControl
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IInventoryProductService _inventoryProductService;
    private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
    private readonly AddNewInventoryProductForm _addNewProductForm;
    private readonly UpdateInventoryProductForm _updateProductForm;
    private readonly AddNewInventoryProductWithCustomBarcodeForm _addNewProductWithCustomBarcodeForm;
    private readonly MessageForm _messageForm;
    private int? _lastQueryCategoryId;
    private SubPanel _activeSubPanel;
    private bool _suppressCategorySelectionChanged;

    private const int AllProductsCategoryId = 0;
    private const string AllProductsCategoryText = "ทั้งหมด";

    private enum ProductColumn
    {
        ProductCode,
        Description,
        QuantityInStock,
        UnitPrice,
        GroupPrice,
        GroupPriceQuantity,
        Category,
        Manufacturer,
        Brand,
        DateCreated,
        DateUpdated
    }

    public InventoryPanel(IEventAggregator eventAggregator,
                          IStoreConstants storeConstants,
                          AddNewInventoryProductForm addNewProductForm,
                          UpdateInventoryProductForm updateProductForm,
                          AddNewInventoryProductWithCustomBarcodeForm addNewProductWithCustomBarcodeForm,
                          MessageForm messageForm,
                          IInventoryProductService inventoryProductService)
    {
        _eventAggregator = eventAggregator;
        _inventoryProductService = inventoryProductService;
        _productCategoryDictionary = storeConstants.ProductCategories;
        _addNewProductForm = addNewProductForm;
        _updateProductForm = updateProductForm;
        _addNewProductWithCustomBarcodeForm = addNewProductWithCustomBarcodeForm;
        _messageForm = messageForm;

        InitializeComponent();
        InitializeProductCategories();
        InitializeProductDataView();

        SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        _eventAggregator.GetEvent<BarcodeReceivedEvent>().Subscribe(BarcodeReceived);
        _eventAggregator.GetEvent<InventoryProductAddedEvent>().Subscribe(NewInventoryProductAdded);
        _eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Subscribe(InventoryProductUpdated);
        _eventAggregator.GetEvent<InventoryProductDeletedEvent>().Subscribe(InventoryProductDeleted);
        _eventAggregator.GetEvent<ActiveSubPanelChangedEvent>().Subscribe(ActiveSubPanelChanged);
        _eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(UserLoggedIn);
    }

    private void InitializeProductCategories()
    {
        CategoryComboBox.Items.Clear();
        CategoryComboBox.Items.Add(AllProductsCategoryText);

        foreach (var item in _productCategoryDictionary)
        {
            CategoryComboBox.Items.Add(item.Value);
        }
    }

    private void InitializeProductDataView()
    {
        #region Initialize all columns

        ProductDataView.Columns.Clear();
        ProductDataView.ColumnCount = 11;

        ProductDataView.Columns[(int)ProductColumn.ProductCode].Name = "รหัสสินค้า";
        ProductDataView.Columns[(int)ProductColumn.ProductCode].Width = 150;
        ProductDataView.Columns[(int)ProductColumn.ProductCode].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.Description].Name = "คำอธิบาย";
        ProductDataView.Columns[(int)ProductColumn.Description].Width = 300;
        ProductDataView.Columns[(int)ProductColumn.Description].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.QuantityInStock].Name = "จำนวนในคลัง";
        ProductDataView.Columns[(int)ProductColumn.QuantityInStock].Width = 150;
        ProductDataView.Columns[(int)ProductColumn.QuantityInStock].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.UnitPrice].Name = "ราคาขาย";
        ProductDataView.Columns[(int)ProductColumn.UnitPrice].Width = 150;
        ProductDataView.Columns[(int)ProductColumn.UnitPrice].ReadOnly = true;
        ProductDataView.Columns[(int)ProductColumn.UnitPrice].DefaultCellStyle.Format = "N2";

        ProductDataView.Columns[(int)ProductColumn.GroupPrice].Name = "ราคาขายต่อกลุ่ม";
        ProductDataView.Columns[(int)ProductColumn.GroupPrice].Width = 170;
        ProductDataView.Columns[(int)ProductColumn.GroupPrice].ReadOnly = true;
        ProductDataView.Columns[(int)ProductColumn.GroupPrice].DefaultCellStyle.Format = "N2";

        ProductDataView.Columns[(int)ProductColumn.GroupPriceQuantity].Name = "จำนวนต่อกลุ่ม";
        ProductDataView.Columns[(int)ProductColumn.GroupPriceQuantity].Width = 150;
        ProductDataView.Columns[(int)ProductColumn.GroupPriceQuantity].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.Category].Name = "ประเภทสินค้า";
        ProductDataView.Columns[(int)ProductColumn.Category].Width = 200;
        ProductDataView.Columns[(int)ProductColumn.Category].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.Manufacturer].Name = "ผู้ผลิต";
        ProductDataView.Columns[(int)ProductColumn.Manufacturer].Width = 200;
        ProductDataView.Columns[(int)ProductColumn.Manufacturer].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.Brand].Name = "ยี่ห้อ";
        ProductDataView.Columns[(int)ProductColumn.Brand].Width = 200;
        ProductDataView.Columns[(int)ProductColumn.Brand].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.DateCreated].Name = "วันที่นำเข้า";
        ProductDataView.Columns[(int)ProductColumn.DateCreated].Width = 200;
        ProductDataView.Columns[(int)ProductColumn.DateCreated].ReadOnly = true;

        ProductDataView.Columns[(int)ProductColumn.DateUpdated].Name = "วันที่อัพเดท";
        ProductDataView.Columns[(int)ProductColumn.DateUpdated].Width = 200;
        ProductDataView.Columns[(int)ProductColumn.DateUpdated].ReadOnly = true;

        #endregion
    }

    private async void ActiveSubPanelChanged(SubPanel activeSubPanel)
    {
        _activeSubPanel = activeSubPanel;

        if (_activeSubPanel == SubPanel.Inventory)
        {
            await RefreshCurrentProductViewAsync();
        }
    }

    private async void UserLoggedIn(ILoggedInUser loggedInUser)
    {
        await ShowAllProductsAsync();
    }

    private async Task RefreshCurrentProductViewAsync()
    {
        if (_lastQueryCategoryId is null or AllProductsCategoryId)
        {
            await ShowAllProductsAsync();
            return;
        }

        await ShowProductsByCategoryId(_lastQueryCategoryId.Value);
    }

    private async Task ShowAllProductsAsync()
    {
        _lastQueryCategoryId = AllProductsCategoryId;
        SelectAllProductsCategory();

        var products = await _inventoryProductService.GetAllAsync();
        ShowProducts(products);
    }

    private async Task ShowProductsByCategoryId(int id)
    {
        var products = await GetInventoryProductsByCategoryIdAsync(id);

        ShowProducts(products);
    }

    private void ShowProducts(IReadOnlyList<InventoryProductDto> products)
    {
        ProductDataView.UiThread(delegate
        {
            ProductDataView.Rows.Clear();

            if (products.Count == 0)
                return;

            foreach (var product in products)
            {
                AddProductToProductDataView(product);
            }
        });
    }

    private void AddProductToProductDataView(InventoryProductDto product)
    {
        var columnCount = ProductDataView.ColumnCount;
        var productRow = new object[columnCount];

        var category = _productCategoryDictionary.ContainsKey(product.Category) ?
                           _productCategoryDictionary[product.Category] :
                           "Unknown";

        productRow[(int)ProductColumn.ProductCode] = product.Barcode;
        productRow[(int)ProductColumn.Description] = product.Description;
        productRow[(int)ProductColumn.QuantityInStock] = product.QuantityInStock;
        productRow[(int)ProductColumn.UnitPrice] = product.UnitPrice;
        productRow[(int)ProductColumn.Category] = category;
        productRow[(int)ProductColumn.Manufacturer] = product.Manufacturer;
        productRow[(int)ProductColumn.Brand] = product.Brand;
        productRow[(int)ProductColumn.DateCreated] = product.DateCreated;
        productRow[(int)ProductColumn.DateUpdated] = product.DateUpdated;
        productRow[(int)ProductColumn.GroupPrice] = product.GroupPrice;
        
        if (product.GroupPriceQuantity.HasValue)
            productRow[(int)ProductColumn.GroupPriceQuantity] = product.GroupPriceQuantity.Value;

        var rowIndex = ProductDataView.Rows.Add(productRow);
        var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38, 38, 38) : Color.FromArgb(48, 48, 48);

        ProductDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
    }

    private void SelectAllProductsCategory()
    {
        if (CategoryComboBox.SelectedItem?.ToString() == AllProductsCategoryText)
            return;

        _suppressCategorySelectionChanged = true;
        try
        {
            CategoryComboBox.SelectedItem = AllProductsCategoryText;
        }
        finally
        {
            _suppressCategorySelectionChanged = false;
        }
    }

    private async void AddProductButton_Click(object sender, EventArgs e)
    {
        await _addNewProductWithCustomBarcodeForm.ShowDialog();
    }

    private async void ProductDataView_DoubleClick(object sender, EventArgs e)
    {
        var barcode = GetProductBarcodeFromSelectedProduct();

        try
        {
            var product = await GetInventoryProductsByByBarcodeAsync(barcode);

            _updateProductForm.ShowDialog(product);
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"ไม่พบรหัสสินค้า {barcode} ในระบบ Error: {ex.Message}", "ไม่พบสินค้าในระบบ");
        }
    }

    private string GetProductBarcodeFromSelectedProduct()
    {
        if (ProductDataView.SelectedCells.Count == 0)
            return string.Empty;

        var selectedCell = ProductDataView.SelectedCells[0];
        var rowIndex = selectedCell.RowIndex;
        var selectedRow = ProductDataView.Rows[rowIndex];
        var barcode = (string)selectedRow.Cells[(int)ProductColumn.ProductCode].Value;

        return barcode;
    }

    private async void BarcodeReceived(string barcode)
    {
        if (_activeSubPanel != SubPanel.Inventory)
            return;

        try
        {
            var product = await GetInventoryProductsByByBarcodeAsync(barcode);

            ShowExistingProduct(product);
            return;
        }
        catch
        {
            // ignored
        }

        AddNewProduct(barcode);
    }

    private async Task<IReadOnlyList<InventoryProductDto>> GetInventoryProductsByCategoryIdAsync(int id)
    {
        return await _inventoryProductService.GetByCategoryIdAsync(id);
    }

    private async Task<InventoryProductDto> GetInventoryProductsByByBarcodeAsync(string barcode)
    {
        return await _inventoryProductService.GetByBarcodeAsync(barcode);
    }

    private async Task<IReadOnlyList<InventoryProductDto>> GetProductsByDescriptionKeywordAsync(string keyword)
	{
		return await _inventoryProductService.SearchByDescriptionAsync(keyword);
	}

	private async Task<IReadOnlyList<InventoryProductDto>> GetProductsByBrandKeywordAsync(string keyword)
	{
		return await _inventoryProductService.SearchByBrandAsync(keyword);
	}

    private void ShowExistingProduct(InventoryProductDto product)
    {
        ClearLastQueryHistory();

        ProductDataView.UiThread(delegate
        {
            ProductDataView.Rows.Clear();

            AddProductToProductDataView(product);
        });
    }

    private void AddNewProduct(string barcode)
    {
        ClearLastQueryHistory();

        ProductDataView.UiThread(async delegate
        {
            ProductDataView.Rows.Clear();

            await _addNewProductForm.ShowDialog(barcode);
        });
    }

    private void NewInventoryProductAdded(Guid id)
    {
        // When a new product is added, the cache is already updated by the service.
        // We trigger a refresh of the current category view if applicable.
        ProductDataView.UiThread(async delegate
        {
            await RefreshCurrentProductViewAsync();
        });
    }

    private async void InventoryProductUpdated(Guid productId)
    {
        await RefreshCurrentProductViewAsync();
    }

    private async void InventoryProductDeleted()
    {
        await RefreshCurrentProductViewAsync();
    }

    private void ClearLastQueryHistory()
    {
        _lastQueryCategoryId = null;
    }

    private async void CategoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_suppressCategorySelectionChanged)
            return;

        var selectedCategoryValue = CategoryComboBox.SelectedItem?.ToString();

        if (selectedCategoryValue == AllProductsCategoryText)
        {
            await ShowAllProductsAsync();
            return;
        }

        var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == selectedCategoryValue);
        var categoryId = category.Key;

        _lastQueryCategoryId = categoryId;

        await ShowProductsByCategoryId(categoryId);
    }

    private async void AddProductWithBarcodeButton_Click(object sender, EventArgs e)
    {
        await _addNewProductForm.ShowDialog();
    }

    private async void SearchByKeywordButton_Click(object sender, EventArgs e)
	{
		var keyword = SearchByKeywordTextBox.Texts.Trim();

		if (string.IsNullOrWhiteSpace(keyword))
		{
			return;

		}

		var products = await SearchProductsByKeyword(keyword);

        ShowProducts(products);
	}

	private async Task<IReadOnlyList<InventoryProductDto>> SearchProductsByKeyword(string keyword)
	{
		if (SearchByDescriptionKeywordRadioButton.Checked)
		{
			return await GetProductsByDescriptionKeywordAsync(keyword);
		}
		
		if (SearchByBrandKeywordRadioButton.Checked)
		{
			return await GetProductsByBrandKeywordAsync(keyword);
		}

		return new List<InventoryProductDto>();
	}
}
