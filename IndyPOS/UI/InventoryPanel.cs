using IndyPOS.Adapters;
using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Events;
using IndyPOS.Extensions;
using IndyPOS.Inventory;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    public partial class InventoryPanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInventoryController _inventoryController;
        private readonly IStoreConstants _storeConstants;
        private IReadOnlyDictionary<int, string> _productCategoryDictionary;
        private readonly AddNewInventoryProductForm _addNewProductForm;
        private readonly UpdateInventoryProductForm _updateProductForm;
        private int? _lastQueryCategoryId;

        private enum ProductColumn
        {
            ProductCode,
            Description,
            QuantityInStock,
            UnitPrice,
            UnitCost,
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
            UpdateInventoryProductForm updateProductForm)
        {
            _eventAggregator = eventAggregator;
            _inventoryController = inventoryController;
            _storeConstants = storeConstants;
            _productCategoryDictionary = _storeConstants.ProductCategories;
            _addNewProductForm = addNewProductForm;
            _updateProductForm = updateProductForm;

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
        }

        private void InitializeProductCategories()
        {
           ProductCategoryListBox.Items.Clear();
            
            foreach (var item in _productCategoryDictionary)
            {
                ProductCategoryListBox.Items.Add(item.Value);
            }
        }

        private void InitializeProductDataView()
        {
            #region Initialize all columns

            ProductDataView.Columns.Clear();
            ProductDataView.ColumnCount = 10;

            ProductDataView.Columns[(int)ProductColumn.ProductCode].Name = "รหัสสินค้า";
            ProductDataView.Columns[(int)ProductColumn.ProductCode].Width = 130;
            ProductDataView.Columns[(int)ProductColumn.ProductCode].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.Description].Name = "คำอธิบาย";
            ProductDataView.Columns[(int)ProductColumn.Description].Width = 250;
            ProductDataView.Columns[(int)ProductColumn.Description].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.QuantityInStock].Name = "จำนวนในคลัง";
            ProductDataView.Columns[(int)ProductColumn.QuantityInStock].Width = 100;
            ProductDataView.Columns[(int)ProductColumn.QuantityInStock].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.UnitPrice].Name = "ราคาขาย";
            ProductDataView.Columns[(int)ProductColumn.UnitPrice].Width = 100;
            ProductDataView.Columns[(int)ProductColumn.UnitPrice].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.UnitCost].Name = "ราคาซื้อ";
            ProductDataView.Columns[(int)ProductColumn.UnitCost].Width = 100;
            ProductDataView.Columns[(int)ProductColumn.UnitCost].ReadOnly = true;

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
            ProductDataView.Columns[(int)ProductColumn.DateCreated].Width = 150;
            ProductDataView.Columns[(int)ProductColumn.DateCreated].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.DateUpdated].Name = "วันที่อัพเดท";
            ProductDataView.Columns[(int)ProductColumn.DateUpdated].Width = 150;
            ProductDataView.Columns[(int)ProductColumn.DateUpdated].ReadOnly = true;

            #endregion
        }

        private void GetProductsByCategoryButton_Click(object sender, EventArgs e)
        {
            if (ProductCategoryListBox.SelectedItem == null)
                return;

            var selectedCategoryValue = ProductCategoryListBox.SelectedItem.ToString();
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == selectedCategoryValue);
            var categoryId = category.Key;

            _lastQueryCategoryId = categoryId;

            ShowProductsByCategoryId(categoryId);
        }

        private void ShowProductsByCategoryId(int id)
		{
            var products = _inventoryController.GetInventoryProductsByCategoryId(id);

            ProductDataView.Rows.Clear();

            foreach (var product in products)
            {
                AddProductToProductDataView(product);
            }
        }

        private void AddProductToProductDataView(IInventoryProduct product)
        {
            var columnCount = ProductDataView.ColumnCount;
            var productRow = new string[columnCount];

            var category = _productCategoryDictionary.ContainsKey(product.Category) ?
                _productCategoryDictionary[product.Category] :
                "Unknown";

            productRow[(int)ProductColumn.ProductCode] = product.Barcode;
            productRow[(int)ProductColumn.Description] = product.Description;
            productRow[(int)ProductColumn.QuantityInStock] = product.QuantityInStock.ToString();
            productRow[(int)ProductColumn.UnitPrice] = product.UnitPrice.ToString("0.00");
            productRow[(int)ProductColumn.UnitCost] = product.UnitCost?.ToString("0.00") ?? string.Empty;
            productRow[(int)ProductColumn.Category] = category;
            productRow[(int)ProductColumn.Manufacturer] = product.Manufacturer;
            productRow[(int)ProductColumn.Brand] = product.Brand;
            productRow[(int)ProductColumn.DateCreated] = product.DateCreated;
            productRow[(int)ProductColumn.DateUpdated] = product.DateUpdated;

            ProductDataView.Rows.Add(productRow);
        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            _addNewProductForm.ShowDialog();
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
            var barcode = selectedRow.Cells[(int)ProductColumn.ProductCode].Value as string;

            return barcode;
        }

        private void BarcodeReceived(string barcode)
        {
            if (!Visible)
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

            ProductDataView.UIThread(delegate
            {
                ProductDataView.Rows.Clear();

                AddProductToProductDataView(product);
            });
        }

        private void AddNewProduct(string barcode)
		{
            ClearLastQueryHistory();

            _addNewProductForm.UIThread(delegate
            {
                ProductDataView.Rows.Clear();

                _addNewProductForm.ShowDialog(barcode);
            });
        }

        private void NewInventoryProductAdded(int inventoryProductId)
		{
            var product = _inventoryController.GetProductByInventoryProductId(inventoryProductId);

            if (product == null) return;

            ProductDataView.UIThread(delegate
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
    }
}
