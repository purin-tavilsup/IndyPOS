using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Inventory;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using IndyPOS.Barcode;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
	public partial class UpdateInventoryProductForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInventoryController _inventoryController;
		private readonly IBarcodeHelper _barcodeHelper;
        private readonly IConfig _config;
		private readonly MessageForm _messageForm;
        private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
        private IInventoryProduct _product;

        public UpdateInventoryProductForm(IEventAggregator eventAggregator, 
										  IStoreConstants storeConstants, 
										  IInventoryController inventoryController,
										  IBarcodeHelper barcodeHelper,
                                          IConfig config,
										  MessageForm messageForm)
        {
            _eventAggregator = eventAggregator;
            _inventoryController = inventoryController;
            _productCategoryDictionary = storeConstants.ProductCategories;
			_barcodeHelper = barcodeHelper;
            _config = config;
			_messageForm = messageForm;

            InitializeComponent();
            InitializeProductCategories();
        }

        public void ShowDialog(IInventoryProduct product)
        {
            _product = product;

            BarcodeLabel.Text = _product.Barcode;

            PopulateProductProperties();

            CancelUpdateProductButton.Select();

			RemoveProductButton.Enabled = product.IsTrackable;

            ShowDialog();
        }

        private void PopulateProductProperties()
        {
            DescriptionTextBox.Texts = _product.Description;
            QuantityLabel.Text = $"{_product.QuantityInStock}";
            UnitPriceTextBox.Texts = $"{_product.UnitPrice:N}";
            CategoryComboBox.Texts = _productCategoryDictionary[_product.Category];
            GroupPriceTextBox.Texts = _product.GroupPrice.HasValue ? $"{_product.GroupPrice.Value:N}" : string.Empty;
            GroupPriceQuantityTextBox.Texts = _product.GroupPriceQuantity.HasValue ? $"{_product.GroupPriceQuantity.Value}" : string.Empty;
            ManufacturerTextBox.Texts = _product.Manufacturer;
            BrandTextBox.Texts = _product.Brand;

			ShowBarcode();
		}

		private bool ValidateProductEntry()
        {
			if (string.IsNullOrWhiteSpace(DescriptionTextBox.Texts))
            {
                _messageForm.Show("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
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

			BarcodePictureBox.Image = null;

            Close();
        }

        private IInventoryProduct UpdateProduct(IInventoryProduct product)
        {
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Texts);
            var categoryId = category.Key;

            // Required Attributes
            product.Description = DescriptionTextBox.Texts.Trim();
            product.QuantityInStock = int.Parse(QuantityLabel.Text.Trim());
            product.UnitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
            product.Category = categoryId;

            // Optional Attributes
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
			BarcodePictureBox.Image = null;

            Close();
        }

		private void RemoveProductButton_Click(object sender, EventArgs e)
		{
            _inventoryController.RemoveProductById(_product.InventoryProductId);

			BarcodePictureBox.Image = null;

            Close();
        }

        private void IncreaseQuantityButton_Click(object sender, EventArgs e)
        {
			if (!ValidateQuantity())
                return;

			var amount = int.Parse(QuantityTextBox.Texts.Trim());
            var quantity = int.Parse(QuantityLabel.Text.Trim());

			QuantityLabel.Text = $"{quantity + amount}";

			QuantityTextBox.Texts = string.Empty;
		}

        private void DecreaseQuantityButton_Click(object sender, EventArgs e)
        {
			if (!ValidateQuantity())
				return;

			var amount = int.Parse(QuantityTextBox.Texts.Trim());
			var quantity = int.Parse(QuantityLabel.Text.Trim());

			QuantityLabel.Text = $"{quantity - amount}";

			QuantityTextBox.Texts = string.Empty;
        }

        private bool ValidateQuantity()
        {
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

			return true;
		}

        private void ShowBarcode()
		{
			if (_product.Barcode.Length != 13)
				return;

			var barcodeImage = _barcodeHelper.CreateEan13BarcodeImage(_product.Barcode, 200, 400, 10);

			BarcodePictureBox.Image = barcodeImage;
        }

		private void SaveBarcodeImage()
		{
			if (_product.Barcode.Length != 13)
				return;

			var barcodeImage = _barcodeHelper.CreateEan13BarcodeImage(_product.Barcode, 400, 800, 10);
			var filePath = $"{_config.BarcodeDirectory}\\{_product.Barcode}-{_product.Description}.jpg";

			barcodeImage.Save(filePath, ImageFormat.Jpeg);
		}

        private void SaveBarcodeToFileButton_Click(object sender, EventArgs e)
		{
			SaveBarcodeImage();
		}
    }
}
