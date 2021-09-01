using IndyPOS.Adapters;
using IndyPOS.Constants;
using IndyPOS.SaleInvoice;
using IndyPOS.Controllers;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	public partial class UpdateInvoiceProductForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISaleInvoiceController _saleInvoiceController;
        private ISaleInvoiceProduct _product;

        public UpdateInvoiceProductForm(IEventAggregator eventAggregator, ISaleInvoiceController saleInvoiceController)
        {
            _eventAggregator = eventAggregator;
            _saleInvoiceController = saleInvoiceController;

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
            QuantityTextBox.Text = product.Quantity.ToString();
        }

        private bool ValidateProductEntry()
        {
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

            return true;
        }

        private void UpdateProductButton_Click(object sender, EventArgs e)
        {
            if (!ValidateProductEntry())
                return;

            var quantity = int.Parse(QuantityTextBox.Text.Trim());

            _saleInvoiceController.UpdateProductQuantity(_product.Barcode, quantity);

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
	}
}
