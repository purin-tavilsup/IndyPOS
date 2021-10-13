﻿using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Inventory;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	public partial class AddNewInventoryProductForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IStoreConstants _storeConstants;
        private readonly IInventoryController _inventoryController;
        private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
		private readonly MessageForm _messageForm;

        public AddNewInventoryProductForm(IEventAggregator eventAggregator, 
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

        public void ShowDialog(string productBarcode = null)
        {
            ResetProductEntry();

            if (string.IsNullOrWhiteSpace(productBarcode))
            {
                ProductCodeTextBox.ReadOnly = false;
            }
            else
            {
                ProductCodeTextBox.Texts = productBarcode;
                ProductCodeTextBox.ReadOnly = true;
            }

            CancelProductEntryButton.Select();

            base.ShowDialog();
        }

        private void ResetProductEntry()
        {
            ProductCodeTextBox.Texts = string.Empty;
            DescriptionTextBox.Texts = string.Empty;
            QuantityTextBox.Texts = string.Empty;
            UnitPriceTextBox.Texts = string.Empty;
            UnitCostTextBox.Texts = string.Empty;
            CategoryComboBox.Texts = "เลือกประเภทสินค้า";
            GroupPriceTextBox.Texts = string.Empty;
            GroupPriceQuantityTextBox.Texts = string.Empty;
            ManufacturerTextBox.Texts = string.Empty;
            BrandTextBox.Texts = string.Empty;
        }

        private bool ValidateProductEntry()
        {
            if (string.IsNullOrWhiteSpace(ProductCodeTextBox.Text))
			{
				var message = "รหัสสินค้าไม่ถูกต้อง" + Environment.NewLine + "กรุณาใส่รหัสสินค้าหรือบาร์โค้ดให้ถูกต้อง";

				_messageForm.Show(message, false);
                
                return false;
            }
                
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
				var message = "คำอธิบายสินค้าไม่ถูกต้อง" + Environment.NewLine + "กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง";

				_messageForm.Show(message, false);
                
                return false;
            }

            if(int.TryParse(QuantityTextBox.Text.Trim(), out var quantity))
            {
                if (quantity < 1)
                {
					var message = "จำนวนสินค้าไม่ถูกต้อง" + Environment.NewLine + "กรุณาใส่จำนวนสินค้าให้ถูกต้อง";

					_messageForm.Show(message, false);

                    return false;
                }
            }
            else
            {
				var message = "จำนวนสินค้าไม่ถูกต้อง" + Environment.NewLine + "กรุณาใส่จำนวนสินค้าให้ถูกต้อง";

				_messageForm.Show(message, false);

                return false;
            }

            if (decimal.TryParse(UnitPriceTextBox.Text.Trim(), out var unitPrice))
            {
                if (unitPrice < 0m)
                {
					var message = "ราคาขายไม่ถูกต้อง" + Environment.NewLine + "กรุณาใส่ราคาขายให้ถูกต้อง";

					_messageForm.Show(message, false);

                    return false;
                }
            }
            else
            {
				var message = "ราคาขายไม่ถูกต้อง" + Environment.NewLine + "กรุณาใส่ราคาขายให้ถูกต้อง";

				_messageForm.Show(message, false);
                
                return false;
            }

            if (!_productCategoryDictionary.Values.Contains(CategoryComboBox.Texts.Trim()))
            {
				var message = "ประเภทสินค้าไม่ถูกต้อง" + Environment.NewLine + "กรุณาเลือกประเภทสินค้าให้ถูกต้อง";

				_messageForm.Show(message, false);
                
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

        private void SaveProductEntryButton_Click(object sender, EventArgs e)
        {
            if (!ValidateProductEntry())
                return;

            var product = CreateNewProduct();

            _inventoryController.AddNewProduct(product);

            Close();
        }

        private IInventoryProduct CreateNewProduct()
        {
            // Required Attributes
            var quantity = int.Parse(QuantityTextBox.Texts.Trim());
            var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Texts);
            var categoryId = category.Key;

            var product = new InventoryProduct
            {
                Barcode = ProductCodeTextBox.Texts.Trim(),
                Description = DescriptionTextBox.Texts.Trim(),
                QuantityInStock = quantity,
                UnitPrice = unitPrice,
                Category = categoryId
            };

            // Optional Attributes
            if (decimal.TryParse(UnitCostTextBox.Texts.Trim(), out var unitCost))
                product.UnitCost = unitCost;

            if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts))
                product.Manufacturer = ManufacturerTextBox.Texts;

            if (!string.IsNullOrWhiteSpace(BrandTextBox.Texts))
                product.Brand = BrandTextBox.Texts;

            if (decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var groupPrice))
                product.GroupPrice = groupPrice;

            if (int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var groupPriceQuantity))
                product.GroupPriceQuantity = groupPriceQuantity;

            return product;
        }

        private void CancelProductEntryButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
