using IndyPOS.Application.Common.Interfaces;
using MediatR;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.InventoryProducts.Create;
using IndyPOS.Application.UseCases.InventoryProducts.Get;
using IndyPOS.Application.UseCases.InventoryProducts.Update;

namespace IndyPOS.Windows.Forms.UI.Inventory
{
    [ExcludeFromCodeCoverage]
	public partial class AddNewInventoryProductWithCustomBarcodeForm : Form
    {
		private readonly IMediator _mediator;
        private readonly IBarcodeGeneratorService _barcodeService;
        private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
		private readonly MessageForm _messageForm;

        public AddNewInventoryProductWithCustomBarcodeForm(IBarcodeGeneratorService barcodeService, 
														   IStoreConstants storeConstants,
                                                           IMediator mediator,
														   MessageForm messageForm)
		{
			_barcodeService = barcodeService;
            _mediator = mediator;
            _productCategoryDictionary = storeConstants.ProductCategories;
			_messageForm = messageForm;

            InitializeComponent();
            InitializeProductCategories();
        }

        public new void ShowDialog()
        {
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

            if (!_productCategoryDictionary.Values.Contains(CategoryComboBox.Texts.Trim()))
            {
				_messageForm.ShowDialog("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
                
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

        private async void SaveProductEntryButton_Click(object sender, EventArgs e)
        {
            if (!ValidateProductEntry())
                return;

			try
			{
				await CreateNewProductAsync();
				await IncrementProductBarcodeCounter();

				Close();
			}
			catch (Exception ex)
			{
				_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า");
			}
		}

        private async Task CreateNewProductAsync()
        {
			var command = CreateCommandForCreateProduct();

			await _mediator.Send(command);
		}

        private async Task IncrementProductBarcodeCounter()
		{
			var counter = await GetProductBarcodeCounter();

			await UpdateProductBarcodeCounter(counter + 1);
		}

        private async Task UpdateProductBarcodeCounter(int newValue)
		{
			var command = new UpdateInventoryProductBarcodeCounterCommand(newValue);

            await _mediator.Send(command);
		}

		private async Task<int> GetProductBarcodeCounter()
		{
			return await _mediator.Send(new GetInventoryProductBarcodeCounterQuery());
		}

        private CreateInventoryProductCommand CreateCommandForCreateProduct()
        {
            // Required Attributes
            var quantity = int.Parse(QuantityTextBox.Texts.Trim());
            var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Texts);
            var categoryId = category.Key;

            var command = new CreateInventoryProductCommand
            {
				Barcode = BarcodeTextBox.Texts,
				Description = DescriptionTextBox.Texts.Trim(),
				QuantityInStock = quantity,
				UnitPrice = unitPrice,
				Category = categoryId,
				IsTrackable = IsTrackableCheckBox.Checked
            };

            // Optional Attributes
            if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts))
                command.Manufacturer = ManufacturerTextBox.Texts;

            if (!string.IsNullOrWhiteSpace(BrandTextBox.Texts))
                command.Brand = BrandTextBox.Texts;

            if (decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var groupPrice))
                command.GroupPrice = groupPrice;

            if (int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var groupPriceQuantity))
                command.GroupPriceQuantity = groupPriceQuantity;

            return command;
        }

        private void CancelProductEntryButton_Click(object sender, EventArgs e)
		{
			Close();
        }

        private async void CategoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
			var selectedCategoryValue = CategoryComboBox.SelectedItem.ToString(); 
			var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == selectedCategoryValue); 
			var categoryId = category.Key;

            await GenerateProductBarcodeAsync(categoryId);
		}

        private async Task GenerateProductBarcodeAsync(int categoryId)
		{
			var counter = await GetProductBarcodeCounter();
			var barcode = _barcodeService.GenerateEan13Barcode(categoryId, counter + 1);

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
