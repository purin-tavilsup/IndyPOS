using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndyPOS.DataServices;
using IndyPOS.EventAggregator;
using Prism.Events;
using IndyPOS.Controllers;
using IndyPOS.Adapters;

namespace IndyPOS
{
    public partial class InventoryPanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInventoryProductsDataService _inventoryProductsDataService;
        private InventoryController _inventoryController;
        private IReadOnlyDictionary<int, string> _productCategoryDictionary;

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

        public InventoryPanel(IEventAggregator eventAggregator, IInventoryProductsDataService inventoryProductsDataService)
        {
            _eventAggregator = eventAggregator;
            _inventoryProductsDataService = inventoryProductsDataService;

            // TODO: this should be handled by Dependency Injection
            _inventoryController = new InventoryController(_eventAggregator, _inventoryProductsDataService);

            _productCategoryDictionary = Machine.StoreConstants.ProductCategories;


            InitializeComponent();
            InitializeProductCategories();
            InitializeProductDataView();
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
            ProductDataView.Columns[(int)ProductColumn.ProductCode].Width = 200;
            ProductDataView.Columns[(int)ProductColumn.ProductCode].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.Description].Name = "คำอธิบาย";
            ProductDataView.Columns[(int)ProductColumn.Description].Width = 500;
            ProductDataView.Columns[(int)ProductColumn.Description].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.QuantityInStock].Name = "จำนวนในคลัง";
            ProductDataView.Columns[(int)ProductColumn.QuantityInStock].Width = 150;
            ProductDataView.Columns[(int)ProductColumn.QuantityInStock].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.UnitPrice].Name = "ราคาขาย";
            ProductDataView.Columns[(int)ProductColumn.UnitPrice].Width = 150;
            ProductDataView.Columns[(int)ProductColumn.UnitPrice].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.UnitCost].Name = "ราคาซื้อ";
            ProductDataView.Columns[(int)ProductColumn.UnitCost].Width = 150;
            ProductDataView.Columns[(int)ProductColumn.UnitCost].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.Category].Name = "ประเภทสินค้า";
            ProductDataView.Columns[(int)ProductColumn.Category].Width = 250;
            ProductDataView.Columns[(int)ProductColumn.Category].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.Manufacturer].Name = "ผู้ผลิต";
            ProductDataView.Columns[(int)ProductColumn.Manufacturer].Width = 250;
            ProductDataView.Columns[(int)ProductColumn.Manufacturer].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.Brand].Name = "ยี่ห้อ";
            ProductDataView.Columns[(int)ProductColumn.Brand].Width = 250;
            ProductDataView.Columns[(int)ProductColumn.Brand].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.DateCreated].Name = "วันที่นำเข้า";
            ProductDataView.Columns[(int)ProductColumn.DateCreated].Width = 250;
            ProductDataView.Columns[(int)ProductColumn.DateCreated].ReadOnly = true;

            ProductDataView.Columns[(int)ProductColumn.DateUpdated].Name = "วันที่อัพเดท";
            ProductDataView.Columns[(int)ProductColumn.DateUpdated].Width = 250;
            ProductDataView.Columns[(int)ProductColumn.DateUpdated].ReadOnly = true;

            #endregion
        }

        private void GetProductsByCategoryButton_Click(object sender, EventArgs e)
        { 
            var selectedCategoryValue = ProductCategoryListBox.SelectedItem.ToString();
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == selectedCategoryValue);
            var categoryId = category.Key;

            var products = _inventoryController.GetInventoryProductsByCategoryId(categoryId);

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

        private void SearchProduct_Click(object sender, EventArgs e)
        {
            var input = SearchProductTextBox.Text.Trim();

            if (SearchByBarcodeRadioButton.Checked)
            {
                var product = _inventoryController.GetInventoryProductByBarcode(input);

                if (product == null)
                    return;

                ProductDataView.Rows.Clear();
                AddProductToProductDataView(product);
            }
        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            //
        }

        private void ProductDataView_DoubleClick(object sender, EventArgs e)
        {
            //TODO: Display a dialog for editing the selected product
            var barcode = GetProductBarcodeFromSelectedProduct();
            // Test
            MessageBox.Show("Barcode : " + barcode);
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
    }
}
