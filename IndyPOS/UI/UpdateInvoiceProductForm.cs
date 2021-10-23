using IndyPOS.Controllers;
using IndyPOS.SaleInvoice;
using Prism.Events;
using System;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
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

        public void ShowDialog(string barcode)
        {
            _product = GetProductByBarcode(barcode);

            PopulateProductProperties(_product);

            CancelUpdateProductButton.Select();

            ShowDialog();
        }

        private void PopulateProductProperties(ISaleInvoiceProduct product)
        {
            ProductCodeLabel.Text = product.Barcode;
            DescriptionLabel.Text = product.Description;
            QuantityTextBox.Texts = product.Quantity.ToString();
        }

        private bool ValidateQuantityEntry()
        {
            if(int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity))
            {
                if (quantity < 0)
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

        private void UpdateProductButton_Click(object sender, EventArgs e)
        {
            if (!ValidateQuantityEntry())
                return;

            var quantity = int.Parse(QuantityTextBox.Texts.Trim());

			if (quantity == 0)
			{
				_saleInvoiceController.RemoveProduct(_product.Barcode);
			}
			else
			{
				_saleInvoiceController.UpdateProductQuantity(_product.Barcode, quantity);
			}
            
            Close();
        }

        private void CancelUpdateProductButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private ISaleInvoiceProduct GetProductByBarcode(string barcode)
		{
            return _saleInvoiceController.Products.FirstOrDefault(p => p.Barcode == barcode);
		}

        private void RemoveProductButton_Click(object sender, EventArgs e)
		{
            _saleInvoiceController.RemoveProduct(_product.Barcode);

            Close();
        }

		private void IncreaseQuantityPicBox_Click(object sender, EventArgs e)
		{
            if(int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity))
			{
                QuantityTextBox.Texts = $"{quantity + 1}";
            }
        }

		private void DecreaseQuantityPicBox_Click(object sender, EventArgs e)
		{
            if (int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity))
            {
                if (quantity == 0)
                    return;

                QuantityTextBox.Texts = $"{quantity - 1}";
            }
        }
	}
}
