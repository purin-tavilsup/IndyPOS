using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
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
    private readonly IStoreHubClient _storeHubClient;
    private readonly AddNewInventoryProductForm _addNewProductForm;
    private readonly UpdateInventoryProductForm _updateProductForm;
    private readonly AddNewInventoryProductWithCustomBarcodeForm _addNewProductWithCustomBarcodeForm;
    private readonly MessageForm _messageForm;

    /// <summary>The store's catalogue, refreshed on login. Empty until then.</summary>
    private IReadOnlyList<ProductCategoryDto> _categories = [];

    /// <summary>Catalogue code of the current filter; null means "all products".</summary>
    private string? _lastQueryCategoryCode;
    private SubPanel _activeSubPanel;
    private bool _suppressCategorySelectionChanged;

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
                          IStoreHubClient storeHubClient,
                          AddNewInventoryProductForm addNewProductForm,
                          UpdateInventoryProductForm updateProductForm,
                          AddNewInventoryProductWithCustomBarcodeForm addNewProductWithCustomBarcodeForm,
                          MessageForm messageForm,
                          IInventoryProductService inventoryProductService)
    {
        _eventAggregator = eventAggregator;
        _inventoryProductService = inventoryProductService;
        _storeHubClient = storeHubClient;
        _addNewProductForm = addNewProductForm;
        _updateProductForm = updateProductForm;
        _addNewProductWithCustomBarcodeForm = addNewProductWithCustomBarcodeForm;
        _messageForm = messageForm;

        InitializeComponent();
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

    /// <summary>
    /// Fills the filter from the store's catalogue. Called on login rather than in the
    /// constructor: the catalogue is fetched over an authenticated endpoint.
    /// </summary>
    private async Task InitializeProductCategoriesAsync()
    {
        try
        {
            _categories = await _storeHubClient.GetProductCategoriesAsync();
        }
        catch
        {
            // Leave the filter at "all products" rather than blocking the inventory view.
            return;
        }

        // Repopulating and selecting must happen in ONE marshalled block. UiThread uses
        // BeginInvoke when called off the UI thread, so splitting them lets the caller's
        // "select all products" run against a combo whose Items are still empty — leaving the
        // filter reading its placeholder after a successful login.
        CategoryComboBox.UiThread(delegate
        {
            // Clear() raises SelectedIndexChanged while an item is still selected, which would
            // otherwise re-enter the handler mid-rebuild on a second login.
            _suppressCategorySelectionChanged = true;

            try
            {
                CategoryComboBox.Items.Clear();
                CategoryComboBox.Items.Add(AllProductsCategoryText);

                foreach (var category in _categories.Where(c => c.IsEnabled))
                {
                    CategoryComboBox.Items.Add(category.DisplayName);
                }

                CategoryComboBox.SelectedItem = AllProductsCategoryText;
                _lastQueryCategoryCode = null;
            }
            finally
            {
                _suppressCategorySelectionChanged = false;
            }
        });
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
        await InitializeProductCategoriesAsync();
        await ShowAllProductsAsync();
    }

    private async Task RefreshCurrentProductViewAsync()
    {
        if (_lastQueryCategoryCode is null)
        {
            await ShowAllProductsAsync();
            return;
        }

        await ShowProductsByCategoryAsync(_lastQueryCategoryCode);
    }

    private async Task ShowAllProductsAsync()
    {
        _lastQueryCategoryCode = null;
        SelectAllProductsCategory();

        var products = await _inventoryProductService.GetAllAsync();
        ShowProducts(products);
    }

    private async Task ShowProductsByCategoryAsync(string categoryCode)
    {
        var products = await _inventoryProductService.GetByCategoryAsync(categoryCode);

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

        // A product may reference a category this store no longer offers; show the raw code
        // rather than "Unknown" so the row stays traceable.
        var category = _categories.FirstOrDefault(c => c.Code == product.Category)?.DisplayName
                       ?? product.Category;

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

        InventoryProductDto product;

        try
        {
            product = await GetInventoryProductsByByBarcodeAsync(barcode);
        }
        catch (KeyNotFoundException)
        {
            _messageForm.ShowDialog($"ไม่พบรหัสสินค้า {barcode} ในระบบ", "ไม่พบสินค้าในระบบ");
            return;
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังค้นหาสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังค้นหาสินค้า");
            return;
        }

        await _updateProductForm.ShowDialog(product);
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

        InventoryProductDto product;

        try
        {
            product = await GetInventoryProductsByByBarcodeAsync(barcode);
        }
        catch (KeyNotFoundException)
        {
            // Genuinely not in the catalogue - offer to add it as new.
            AddNewProduct(barcode);
            return;
        }
        catch (Exception ex)
        {
            // A StoreHub outage, an expired token, a timeout - none of these mean the
            // product does not exist, so falling through to "Add New Product" would offer
            // to recreate a product that is already there. Surface the failure instead.
            _messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังค้นหาสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังค้นหาสินค้า");
            return;
        }

        ShowExistingProduct(product);
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
        _lastQueryCategoryCode = null;
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

        var categoryCode = _categories.FirstOrDefault(c => c.DisplayName == selectedCategoryValue)?.Code;

        if (categoryCode is null)
        {
            await ShowAllProductsAsync();
            return;
        }

        _lastQueryCategoryCode = categoryCode;

        await ShowProductsByCategoryAsync(categoryCode);
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
