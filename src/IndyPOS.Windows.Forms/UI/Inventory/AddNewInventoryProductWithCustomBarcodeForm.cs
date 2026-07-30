using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Inventory
{
    [ExcludeFromCodeCoverage]
	public partial class AddNewInventoryProductWithCustomBarcodeForm : Form
    {
		private readonly IInventoryProductService _inventoryProductService;
        private readonly IBarcodeGeneratorService _barcodeService;
        private readonly IStoreHubClient _storeHubClient;
		private readonly MessageForm _messageForm;

        /// <summary>The store's catalogue, fetched when the dialog opens. The combo shows
        /// DisplayName; the server expects Code, so every save looks the code back up from here.</summary>
        private IReadOnlyList<ProductCategoryDto> _categories = [];

        public AddNewInventoryProductWithCustomBarcodeForm(IBarcodeGeneratorService barcodeService,
														   MessageForm messageForm,
														   IInventoryProductService inventoryProductService,
														   IStoreHubClient storeHubClient)
		{
			_barcodeService = barcodeService;
			_messageForm = messageForm;
			_inventoryProductService = inventoryProductService;
			_storeHubClient = storeHubClient;

			InitializeComponent();
        }

        public new async Task ShowDialog()
        {
            await PopulateProductCategoryComboBoxAsync();
            ResetProductEntry();

            base.ShowDialog();
        }

        private void ResetProductEntry()
        {
			BarcodeTextBox.Texts = string.Empty;
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
				_messageForm.ShowDialog("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
                
                return false;
            }

            if(int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity))
            {
                if (quantity < 1)
                {
					_messageForm.ShowDialog("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");

                    return false;
                }
            }
            else
            {
				_messageForm.ShowDialog("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");

                return false;
            }

            if (decimal.TryParse(UnitPriceTextBox.Texts.Trim(), out var unitPrice))
            {
                if (unitPrice < 0m)
                {
					_messageForm.ShowDialog("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");

                    return false;
                }
            }
            else
            {
				_messageForm.ShowDialog("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                
                return false;
            }

            if (SelectedCategoryCode() is null)
            {
				_messageForm.ShowDialog("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
                
                return false;
            }

            return true;
        }

        private async Task PopulateProductCategoryComboBoxAsync()
        {
            CategoryComboBox.Items.Clear();
            _categories = [];

            IReadOnlyList<ProductCategoryDto> categories;
            bool multipleTypes;
            try
            {
                categories = await _storeHubClient.GetProductCategoriesAsync();
                multipleTypes = (await _storeHubClient.GetStoreFeaturesAsync()).MultipleProductTypesEnabled;
            }
            catch
            {
                // The server still guards creation, so a fetch failure must not offer a wrong set.
                // Leave the picker empty and let the operator retry.
                return;
            }

            _categories = categories;

            foreach (var category in categories.Where(c => c.IsEnabled))
            {
                // Hide kinds this store may not use; the server rejects them anyway.
                if (!multipleTypes && category.Kind != ProductCategoryKind.GeneralGoods)
                    continue;

                CategoryComboBox.Items.Add(category.DisplayName);
            }
        }

        /// <summary>The catalogue Code behind the selected DisplayName, or null if nothing matches.</summary>
        private string? SelectedCategoryCode() =>
            _categories.FirstOrDefault(c => c.DisplayName == CategoryComboBox.Texts.Trim())?.Code;

        private async void SaveProductEntryButton_Click(object sender, EventArgs e)
        {
            if (!ValidateProductEntry())
                return;

			try
			{
				var request = CreateRequestForCreateProduct();

				await _inventoryProductService.CreateAsync(request);

				Close();
			}
			catch (Exception ex)
			{
				_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า");
			}
		}

        private CreateInventoryProductRequest CreateRequestForCreateProduct()
        {
            // Required Attributes
            var quantity = int.Parse(QuantityTextBox.Texts.Trim());
            var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
            // Validated before we get here, so a null code is unreachable.
            var categoryCode = SelectedCategoryCode()!;

            // Optional Attributes
            decimal? groupPrice = decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var gp) ? gp : null;
            int? groupPriceQuantity = int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var gpq) ? gpq : null;

            return new CreateInventoryProductRequest
            {
				Barcode = BarcodeTextBox.Texts.Trim(),
				Description = DescriptionTextBox.Texts.Trim(),
				QuantityInStock = quantity,
				UnitPrice = unitPrice,
				Category = categoryCode,
				IsTrackable = IsTrackableCheckBox.Checked,
				Manufacturer = string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts) ? null : ManufacturerTextBox.Texts.Trim(),
				Brand = string.IsNullOrWhiteSpace(BrandTextBox.Texts) ? null : BrandTextBox.Texts.Trim(),
				GroupPrice = groupPrice,
				GroupPriceQuantity = groupPriceQuantity
            };
        }

        private void CancelProductEntryButton_Click(object sender, EventArgs e)
		{
			Close();
        }

        private async void CategoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            await GenerateProductBarcodeAsync();
		}

        private async Task GenerateProductBarcodeAsync()
		{
			var barcode = await _inventoryProductService.GenerateBarcodeAsync();

			BarcodeTextBox.Texts = barcode;

			var barcodeImage = _barcodeService.CreateEan13BarcodeImage(barcode, 200, 400, 10);

            BarcodePictureBox.Image = barcodeImage;
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
