using IndyPOS.Interfaces;
using Prism.Events;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using IndyPOS.Facade.Interfaces;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
	public partial class UpdateInvoiceProductForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISaleInvoiceController _saleInvoiceController;
		private readonly MessageForm _messageForm;
        private ISaleInvoiceProduct _product;

        public UpdateInvoiceProductForm(IEventAggregator eventAggregator, 
										ISaleInvoiceController saleInvoiceController,
										MessageForm messageForm)
        {
            _eventAggregator = eventAggregator;
            _saleInvoiceController = saleInvoiceController;
			_messageForm = messageForm;

            InitializeComponent();
        }

        public void ShowDialog(string barcode, int priority)
        {
            _product = _saleInvoiceController.Products.FirstOrDefault(p => p.Barcode == barcode && 
																		   p.Priority == priority);

            PopulateProductProperties(_product);

            CancelUpdateProductButton.Select();

            ShowDialog();
        }

        private void PopulateProductProperties(ISaleInvoiceProduct product)
        {
            ProductCodeLabel.Text = product.Barcode;
            DescriptionLabel.Text = product.Description;
            QuantityTextBox.Texts = $"{product.Quantity}";

			UnitPriceTextBox.Texts = $"{product.UnitPrice:N}";
			UnitPriceLabel.Visible = !product.IsTrackable;
            UnitPriceTextBox.Visible = !product.IsTrackable;
			NoteTextBox.Texts = string.Empty;
            NoteLabel.Visible = !product.IsTrackable;
			NoteTextBox.Visible = !product.IsTrackable;
		}

        private bool ValidateUserInputs()
        {
            if (!int.TryParse(QuantityTextBox.Texts.Trim(), out _))
            {
                _messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
                return false;
            }

            if (UnitPriceTextBox.Visible && !decimal.TryParse(UnitPriceTextBox.Texts.Trim(), out _))
            {
				_messageForm.Show("กรุณาใส่ราคาสินค้าให้ถูกต้อง", "ราคาสินค้าไม่ถูกต้อง");
				return false;
            }

			//if (NoteTextBox.Visible && !NoteTextBox.Texts.HasValue())
			//{
			//	_messageForm.Show("กรุณาใส่ Note สำหรับสินค้าให้ถูกต้อง", "Note สำหรับสินค้าไม่ถูกต้อง");
			//	return false;
			//}

            return true;
        }

        private void UpdateProductButton_Click(object sender, EventArgs e)
        {
            if (!ValidateUserInputs())
                return;

            var quantity = int.Parse(QuantityTextBox.Texts.Trim());

			if (quantity == 0)
			{
				_saleInvoiceController.RemoveProduct(_product);

				Close();
			}
			
			_saleInvoiceController.UpdateProductQuantity(_product.InventoryProductId, _product.Priority, quantity);

			if (!_product.IsTrackable)
			{
				var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
				var note = NoteTextBox.Texts.Trim();

                _saleInvoiceController.UpdateProductUnitPrice(_product.InventoryProductId, _product.Priority, unitPrice, note);
			}
            
            Close();
        }

        private void CancelUpdateProductButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RemoveProductButton_Click(object sender, EventArgs e)
		{
            _saleInvoiceController.RemoveProduct(_product);

            Close();
        }

        private void IncreaseQuantityButton_Click(object sender, EventArgs e)
        {
			if (!int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity)) 
				return;

			QuantityTextBox.Texts = $"{quantity + 1}";
        }

        private void DecreaseQuantityButton_Click(object sender, EventArgs e)
        {
			if (!int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity)) 
				return;

			QuantityTextBox.Texts = $"{quantity - 1}";
        }
    }
}
