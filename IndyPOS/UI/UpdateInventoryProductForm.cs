using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Inventory;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	public partial class UpdateInventoryProductForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IStoreConstants _storeConstants;
        private readonly IInventoryController _inventoryController;
        private IReadOnlyDictionary<int, string> _productCategoryDictionary;
        private IInventoryProduct _product;

        public UpdateInventoryProductForm(IEventAggregator eventAggregator, 
            IStoreConstants storeConstants, 
            IInventoryController inventoryController)
        {
            _eventAggregator = eventAggregator;
            _storeConstants = storeConstants;
            _inventoryController = inventoryController;
            _productCategoryDictionary = _storeConstants.ProductCategories;

            InitializeComponent();
            InitializeProductCategories();
        }

        public void ShowDialog(IInventoryProduct product)
        {
            _product = product;
            ProductCodeTextBox.Text = _product.Barcode;
            ProductCodeTextBox.ReadOnly = true;

            PopulateProductProperties();

            CancelUpdateProductButton.Select();

            ShowDialog();
        }

        private void PopulateProductProperties()
        {
            DescriptionTextBox.Text = _product.Description;
            QuantityTextBox.Text = _product.QuantityInStock.ToString();
            UnitPriceTextBox.Text = _product.UnitPrice.ToString("0.00");
            UnitCostTextBox.Text = _product.UnitCost.HasValue ? _product.UnitCost.Value.ToString("0.00") : string.Empty;
            CategoryComboBox.Text = _productCategoryDictionary[_product.Category];
            ManufacturerTextBox.Text = _product.Manufacturer;
            BrandTextBox.Text = _product.Brand;
        }

        private bool ValidateProductEntry()
        {
            if (string.IsNullOrWhiteSpace(ProductCodeTextBox.Text))
            {
                MessageBox.Show("กรุณาใส่รหัสสินค้าหรือบาร์โค้ดให้ถูกต้อง", "รหัสสินค้าไม่ถูกต้อง");
                return false;
            }
                
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
                return false;
            }

            if(int.TryParse(QuantityTextBox.Text.Trim(), out var quantity))
            {
                if (quantity < 1)
                {
                    MessageBox.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
                    return false;
                }
            }
            else
            {
                MessageBox.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
                return false;
            }

            if (decimal.TryParse(UnitPriceTextBox.Text.Trim(), out var unitPrice))
            {
                if (unitPrice < 0m)
                {
                    MessageBox.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                    return false;
                }
            }
            else
            {
                MessageBox.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                return false;
            }

            if (!_productCategoryDictionary.Values.Contains(CategoryComboBox.Text.Trim()))
            {
                MessageBox.Show("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
                return false;
            }

            return true;
        }

        private void InitializeProductCategories()
        {
            CategoryComboBox.Items.Clear();

            foreach (var item in _productCategoryDictionary)
            {
                CategoryComboBox.Items.Add(item.Value);
            }
        }

        private void UpdateProductButton_Click(object sender, EventArgs e)
        {
            if (!ValidateProductEntry())
                return;

            var updatedProduct = UpdateProduct(_product);

            _inventoryController.UpdateProduct(updatedProduct);

            Close();
        }

        private IInventoryProduct UpdateProduct(IInventoryProduct product)
        {
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Text);
            var categoryId = category.Key;

            // Required Attributes
            product.Description = DescriptionTextBox.Text.Trim();
            product.QuantityInStock = int.Parse(QuantityTextBox.Text.Trim());
            product.UnitPrice = decimal.Parse(UnitPriceTextBox.Text.Trim());
            product.Category = categoryId;

            // Optional Attributes
            if (decimal.TryParse(UnitCostTextBox.Text.Trim(), out var unitCost))
                product.UnitCost = unitCost;

            if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Text))
                product.Manufacturer = ManufacturerTextBox.Text;

            if (!string.IsNullOrWhiteSpace(BrandTextBox.Text))
                product.Brand = BrandTextBox.Text;

            return product;
        }

        private void CancelUpdateProductButton_Click(object sender, EventArgs e)
        {
            Close();
        }

		private void RemoveProductButton_Click(object sender, EventArgs e)
		{
            _inventoryController.RemoveProductByInventoryProductId(_product.InventoryProductId);

            Close();
        }
	}
}
