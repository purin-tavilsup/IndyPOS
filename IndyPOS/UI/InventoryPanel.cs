using IndyPOS.Common.Interfaces;
using IndyPOS.Extensions;
using IndyPOS.Interfaces;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IndyPOS.Common.Enums;
using IndyPOS.Facade.Events;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
	public partial class InventoryPanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInventoryController _inventoryController;
        private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
        private readonly AddNewInventoryProductForm _addNewProductForm;
        private readonly UpdateInventoryProductForm _updateProductForm;
		private readonly AddNewInventoryProductWithCustomBarcodeForm _addNewProductWithCustomBarcodeForm;
        private int? _lastQueryCategoryId;
        private SubPanel _activeSubPanel;

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
							  IInventoryController inventoryController,
							  IStoreConstants storeConstants,
							  AddNewInventoryProductForm addNewProductForm,
							  UpdateInventoryProductForm updateProductForm,
							  AddNewInventoryProductWithCustomBarcodeForm addNewProductWithCustomBarcodeForm)
        {
            _eventAggregator = eventAggregator;
            _inventoryController = inventoryController;
            _productCategoryDictionary = storeConstants.ProductCategories;
            _addNewProductForm = addNewProductForm;
            _updateProductForm = updateProductForm;
			_addNewProductWithCustomBarcodeForm = addNewProductWithCustomBarcodeForm;

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
        }

        private void InitializeProductCategories()
        {
            CategoryComboBox.Items.Clear();

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

            ProductDataView.Columns[(int)ProductColumn.GroupPrice].Name = "ราคาขายต่อกลุ่ม";
            ProductDataView.Columns[(int)ProductColumn.GroupPrice].Width = 170;
            ProductDataView.Columns[(int)ProductColumn.GroupPrice].ReadOnly = true;

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

        private void ActiveSubPanelChanged(SubPanel activeSubPanel)
        {
            _activeSubPanel = activeSubPanel;
        }

		private void ShowProductsByCategoryId(int id)
		{
			var products = _inventoryController.GetInventoryProductsByCategoryId(id);

            ProductDataView.Rows.Clear();

            if (products.Count == 0)
                return;

            foreach (var product in products)
            {
                AddProductToProductDataView(product);
            }
        }

        private void AddProductToProductDataView(IInventoryProduct product)
        {
            var columnCount = ProductDataView.ColumnCount;
            var productRow = new object[columnCount];

            var category = _productCategoryDictionary.ContainsKey(product.Category) ?
                _productCategoryDictionary[product.Category] :
                "Unknown";

            productRow[(int)ProductColumn.ProductCode] = product.Barcode;
            productRow[(int)ProductColumn.Description] = product.Description;
            productRow[(int)ProductColumn.QuantityInStock] = product.QuantityInStock;
			productRow[(int) ProductColumn.UnitPrice] = product.UnitPrice;
			productRow[(int)ProductColumn.Category] = category;
            productRow[(int)ProductColumn.Manufacturer] = product.Manufacturer;
            productRow[(int)ProductColumn.Brand] = product.Brand;
            productRow[(int)ProductColumn.DateCreated] = product.DateCreated;
            productRow[(int)ProductColumn.DateUpdated] = product.DateUpdated;

            if (product.GroupPrice.HasValue) 
				productRow[(int)ProductColumn.GroupPrice] = product.GroupPrice.Value;
			
			if (product.GroupPriceQuantity.HasValue) 
				productRow[(int)ProductColumn.GroupPriceQuantity] = product.GroupPriceQuantity.Value;

			var rowIndex = ProductDataView.Rows.Add(productRow);
			var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

			ProductDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
        }

        private void AddProductButton_Click(object sender, EventArgs e)
		{
			_addNewProductWithCustomBarcodeForm.ShowDialog();
		}

        private void ProductDataView_DoubleClick(object sender, EventArgs e)
        {
            var barcode = GetProductBarcodeFromSelectedProduct();
            var product = _inventoryController.GetInventoryProductByBarcode(barcode);

            _updateProductForm.ShowDialog(product);
        }

        private string GetProductBarcodeFromSelectedProduct()
        {
            if (ProductDataView.SelectedCells.Count == 0)
                return string.Empty;

            var selectedCell = ProductDataView.SelectedCells[0];
            var rowIndex = selectedCell.RowIndex;
            var selectedRow = ProductDataView.Rows[rowIndex];
            var barcode = (string) selectedRow.Cells[(int)ProductColumn.ProductCode].Value;

            return barcode;
        }

        private void BarcodeReceived(string barcode)
        {
            if (_activeSubPanel != SubPanel.Inventory)
                return;

            var product = SearchExistingProductByBarcode(barcode);

            if (product != null)
			{
                ShowExistingProduct(product);
                return;
            }

            AddNewProduct(barcode);
        }

        private IInventoryProduct SearchExistingProductByBarcode(string barcode)
		{
            return _inventoryController.GetInventoryProductByBarcode(barcode);
        }

        private void ShowExistingProduct(IInventoryProduct product)
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

            ProductDataView.UiThread(delegate
            {
                ProductDataView.Rows.Clear();

                _addNewProductForm.ShowDialog(barcode);
            });
        }

        private void NewInventoryProductAdded(int inventoryProductId)
		{
            var product = _inventoryController.GetProductById(inventoryProductId);

            if (product == null) return;

            ProductDataView.UiThread(delegate
            {
                ProductDataView.Rows.Clear();

                AddProductToProductDataView(product);
            });
        }

        private void InventoryProductUpdated(int inventoryProductId)
		{
            if (!_lastQueryCategoryId.HasValue)
                return;

            ShowProductsByCategoryId(_lastQueryCategoryId.GetValueOrDefault());
        }

        private void InventoryProductDeleted()
		{
            if (!_lastQueryCategoryId.HasValue)
                return;
			
			ShowProductsByCategoryId(_lastQueryCategoryId.GetValueOrDefault());
        }

        private void ClearLastQueryHistory()
		{
            _lastQueryCategoryId = null;
        }

		private void CategoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (CategoryComboBox.SelectedItem == null)
                return;
			var selectedCategoryValue = CategoryComboBox.SelectedItem.ToString(); 
			var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == selectedCategoryValue); 
			var categoryId = category.Key;
			
			_lastQueryCategoryId = categoryId;
			
			ShowProductsByCategoryId(categoryId);
        }

        private void AddProductWithBarcodeButton_Click(object sender, EventArgs e)
        {
			_addNewProductForm.ShowDialog();
        }
    }
}
