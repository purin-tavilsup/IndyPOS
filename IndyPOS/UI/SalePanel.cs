using IndyPOS.Adapters;
using IndyPOS.Controllers;
using IndyPOS.Events;
using IndyPOS.Extensions;
using Prism.Events;
using System;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    public partial class SalePanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private ISaleInvoiceController _saleInvoiceController;

        private enum SaleInvoiceColumn
        {
            ProductCode,
            Description,
            Quantity,
            UnitPrice,
            Total
        }

        public SalePanel(IEventAggregator eventAggregator, ISaleInvoiceController saleInvoiceController)
        {
            InitializeComponent();
            InitializeInvoiceDataView();

            _eventAggregator = eventAggregator;
            _saleInvoiceController = saleInvoiceController;
            
            eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Subscribe(SaleInvoiceProductChanged);
            eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Subscribe(SaleInvoiceProductChanged);
        }

        private void InitializeInvoiceDataView()
        {
            #region Initialize all columns

            InvoiceDataView.Columns.Clear();
            InvoiceDataView.ColumnCount = 5;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].Name = "รหัสสินค้า";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].Width = 200;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].Name = "คำอธิบาย";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].Width = 500;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].Name = "จำนวน";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].Width = 100;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].Name = "ราคาต่อหน่วย";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].Width = 170;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Name = "ราคารวม";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Width = 150;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].ReadOnly = true;

            #endregion
        }

        private void TestGetAllProductsButton_Click(object sender, EventArgs e)
        {
            // Test
        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            // Test
            _saleInvoiceController.AddProductToSaleInvoice("8850999009674");
            _saleInvoiceController.AddProductToSaleInvoice("8850999143002");
        }

        private void RemoveProductButton_Click(object sender, EventArgs e)
        {
            var barcode = GetProductBarcodeFromSelectedProduct();

            _saleInvoiceController.RemoveProductFromSaleInvoice(barcode);
        }

        private string GetProductBarcodeFromSelectedProduct()
        {
            if (InvoiceDataView.SelectedCells.Count == 0)
                return string.Empty;

            var selectedCell = InvoiceDataView.SelectedCells[0];
            var rowIndex = selectedCell.RowIndex;
            var selectedRow = InvoiceDataView.Rows[rowIndex];
            var barcode = selectedRow.Cells[(int)SaleInvoiceColumn.ProductCode].Value as string;

            return barcode;
        }

        private void SaleInvoiceProductChanged(string barcode)
        {
            InvoiceDataView.UIThread(delegate
            {
                InvoiceDataView.Rows.Clear();

                var products = _saleInvoiceController.Products;

                foreach (var product in products)
                {
                    AddProductToInvoiceDataView(product);
                }
            });

            TotalLabel.UIThread(delegate
            {
                TotalLabel.Text = _saleInvoiceController.InvoiceTotal.ToString("0.00");
            });
        }

        private void AddProductToInvoiceDataView(ISaleInvoiceProduct product)
        {
            var columnCount = InvoiceDataView.ColumnCount;
            var productRow = new string[columnCount];
            var total = product.UnitPrice * product.Quantity;

            productRow[(int)SaleInvoiceColumn.ProductCode] = product.Barcode;
            productRow[(int)SaleInvoiceColumn.Description] = product.Description;
            productRow[(int)SaleInvoiceColumn.Quantity] = product.Quantity.ToString();
            productRow[(int)SaleInvoiceColumn.UnitPrice] = product.UnitPrice.ToString("0.00");
            productRow[(int)SaleInvoiceColumn.Total] = total.ToString("0.00");

            InvoiceDataView.Rows.Add(productRow);
        }
        
        private void GetPaymentButton_Click(object sender, EventArgs e)
        {
            // TODO: Display a dialog for getting a payment
        }

        private void SaveSaleInvoiceButton_Click(object sender, EventArgs e)
        {
            // TODO: Insert invoice, products, and payments to database  
        }

        private void CancelSaleInvoiceButton_Click(object sender, EventArgs e)
        {
            // TODO: Call to _saleInvoiceController to clear products on invoice and reset invoice
        }

        private void InvoiceDataView_DoubleClick(object sender, EventArgs e)
        {
            //TODO: Display a dialog for editing the selected product
            var barcode = GetProductBarcodeFromSelectedProduct();
            // Test
            MessageBox.Show("Barcode : " + barcode);
        }
    }
}
