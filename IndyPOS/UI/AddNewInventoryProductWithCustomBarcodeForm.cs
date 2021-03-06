using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Inventory;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using IndyPOS.Barcode;

namespace IndyPOS.UI
{
	[ExcludeFromCodeCoverage]
	public partial class AddNewInventoryProductWithCustomBarcodeForm : Form
    {
        private readonly IBarcodeHelper _barcodeHelper;
        private readonly IEventAggregator _eventAggregator;
        private readonly IInventoryController _inventoryController;
		private readonly IConfig _config;
        private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
		private readonly MessageForm _messageForm;

        public AddNewInventoryProductWithCustomBarcodeForm(IBarcodeHelper barcodeHelper,
														   IEventAggregator eventAggregator, 
														   IStoreConstants storeConstants, 
														   IInventoryController inventoryController,
														   IConfig config,
														   MessageForm messageForm)
		{
			_barcodeHelper = barcodeHelper;
            _eventAggregator = eventAggregator;
            _inventoryController = inventoryController;
            _config = config;
            _productCategoryDictionary = storeConstants.ProductCategories;
			_messageForm = messageForm;

            InitializeComponent();
            InitializeProductCategories();
        }

        public new void ShowDialog()
        {
            ResetProductEntry();

            CancelProductEntryButton.Select();

			

            base.ShowDialog();
        }

        private void ResetProductEntry()
        {
            BarcodeLabel.Text = string.Empty;
            DescriptionTextBox.Texts = string.Empty;
            QuantityTextBox.Texts = string.Empty;
            UnitPriceTextBox.Texts = string.Empty;
            CategoryComboBox.Texts = "เลือกประเภทสินค้า";
            GroupPriceTextBox.Texts = string.Empty;
            GroupPriceQuantityTextBox.Texts = string.Empty;
            ManufacturerTextBox.Texts = string.Empty;
            BrandTextBox.Texts = string.Empty;
			IsTrackableCheckBox.Checked = true;
			BarcodePictureBox.Image = null;
		}

        private bool ValidateProductEntry()
        {
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

        private void SaveProductEntryButton_Click(object sender, EventArgs e)
        {
            if (!ValidateProductEntry())
                return;

            var product = CreateNewProduct();

            _inventoryController.AddNewProduct(product);

			SaveBarcodeImage(product);

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
                Barcode = BarcodeLabel.Text,
                Description = DescriptionTextBox.Texts.Trim(),
                QuantityInStock = quantity,
                UnitPrice = unitPrice,
                Category = categoryId,
				IsTrackable = IsTrackableCheckBox.Checked
            };

            // Optional Attributes
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

        private void CategoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
			if (CategoryComboBox.SelectedItem == null)
				return;

			var selectedCategoryValue = CategoryComboBox.SelectedItem.ToString(); 
			var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == selectedCategoryValue); 
			var categoryId = category.Key;

            GenerateProductBarcode(categoryId);
		}

        private void GenerateProductBarcode(int categoryId)
		{
			var counter = _inventoryController.GetProductBarcodeCounter();
			var barcode = _barcodeHelper.GenerateEan13Barcode(categoryId, counter + 1);

			BarcodeLabel.Text = barcode;

			var barcodeImage = _barcodeHelper.CreateEan13BarcodeImage(barcode, 200, 400, 10);

            BarcodePictureBox.Image = barcodeImage;
		}

        private void SaveBarcodeImage(IInventoryProduct product)
		{
			var barcodeImage = _barcodeHelper.CreateEan13BarcodeImage(product.Barcode, 100, 200, 10);
			var filePath = $"{_config.BarcodeDirectory}\\{product.Barcode}-{product.Description}.jpg";

            barcodeImage.Save(filePath, ImageFormat.Jpeg);

            _inventoryController.IncrementProductBarcodeCounter();
		}

        private void IsTrackableCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (!IsTrackableCheckBox.Checked)
            {
				QuantityTextBox.Texts = "1";
            }

			QuantityTextBox.Enabled = IsTrackableCheckBox.Checked;
		}
    }
}
