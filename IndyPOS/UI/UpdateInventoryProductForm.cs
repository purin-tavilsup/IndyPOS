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
		private readonly MessageForm _messageForm;
        private IReadOnlyDictionary<int, string> _productCategoryDictionary;
        private IInventoryProduct _product;

        public UpdateInventoryProductForm(IEventAggregator eventAggregator, 
										  IStoreConstants storeConstants, 
										  IInventoryController inventoryController,
										  MessageForm messageForm)
        {
            _eventAggregator = eventAggregator;
            _storeConstants = storeConstants;
            _inventoryController = inventoryController;
            _productCategoryDictionary = _storeConstants.ProductCategories;
			_messageForm = messageForm;

            InitializeComponent();
            InitializeProductCategories();
        }

        public void ShowDialog(IInventoryProduct product)
        {
            _product = product;
            ProductCodeTextBox.Texts = _product.Barcode;
            ProductCodeTextBox.ReadOnly = true;

            PopulateProductProperties();

            CancelUpdateProductButton.Select();

            ShowDialog();
        }

        private void PopulateProductProperties()
        {
            DescriptionTextBox.Texts = _product.Description;
            QuantityTextBox.Texts = _product.QuantityInStock.ToString();
            UnitPriceTextBox.Texts = _product.UnitPrice.ToString("0.00");
            UnitCostTextBox.Texts = _product.UnitCost.HasValue ? _product.UnitCost.Value.ToString("0.00") : string.Empty;
            CategoryComboBox.Texts = _productCategoryDictionary[_product.Category];
            GroupPriceTextBox.Texts = _product.GroupPrice.HasValue ? _product.GroupPrice.Value.ToString("0.00") : string.Empty;
            GroupPriceQuantityTextBox.Texts = _product.GroupPriceQuantity.HasValue ? _product.GroupPriceQuantity.Value.ToString("0.00") : string.Empty;
            ManufacturerTextBox.Texts = _product.Manufacturer;
            BrandTextBox.Texts = _product.Brand;
        }

        private bool ValidateProductEntry()
        {
            if (string.IsNullOrWhiteSpace(ProductCodeTextBox.Texts))
            {
				_messageForm.Show("กรุณาใส่รหัสสินค้าหรือบาร์โค้ดให้ถูกต้อง", "รหัสสินค้าไม่ถูกต้อง");
                return false;
            }
                
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Texts))
            {
                _messageForm.Show("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
                return false;
            }

            if(int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity))
            {
                if (quantity < 1)
                {
                    _messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
                    return false;
                }
            }
            else
            {
                _messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
                return false;
            }

            if (decimal.TryParse(UnitPriceTextBox.Texts.Trim(), out var unitPrice))
            {
                if (unitPrice < 0m)
                {
                    _messageForm.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                    return false;
                }
            }
            else
            {
                _messageForm.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                return false;
            }

            if (!_productCategoryDictionary.Values.Contains(CategoryComboBox.Texts.Trim()))
            {
                _messageForm.Show("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
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
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Texts);
            var categoryId = category.Key;

            // Required Attributes
            product.Description = DescriptionTextBox.Texts.Trim();
            product.QuantityInStock = int.Parse(QuantityTextBox.Texts.Trim());
            product.UnitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
            product.Category = categoryId;

            // Optional Attributes
            if (decimal.TryParse(UnitCostTextBox.Texts.Trim(), out var unitCost))
                product.UnitCost = unitCost;

            if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts))
                product.Manufacturer = ManufacturerTextBox.Texts;

            if (!string.IsNullOrWhiteSpace(BrandTextBox.Texts))
                product.Brand = BrandTextBox.Texts;

			if (decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var groupPrice))
			{
				product.GroupPrice = groupPrice;
			}
			else
			{
				product.GroupPrice = null;
			}

			if (int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var groupPriceQuantity))
			{
				product.GroupPriceQuantity = groupPriceQuantity;
			}
			else
			{
				product.GroupPriceQuantity = null;
			}

            return product;
        }

        private void CancelUpdateProductButton_Click(object sender, EventArgs e)
        {
            Close();
        }

		private void RemoveProductButton_Click(object sender, EventArgs e)
		{
            _inventoryController.RemoveProductById(_product.InventoryProductId);

            Close();
        }
	}
}
