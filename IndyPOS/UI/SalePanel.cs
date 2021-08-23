﻿using IndyPOS.Adapters;
using IndyPOS.Controllers;
using IndyPOS.SaleInvoice;
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
        private readonly AcceptPaymentForm _acceptPaymentForm;
        private readonly ISaleInvoiceController _saleInvoiceController;
        private readonly UpdateInvoiceProductForm _updateProductForm;

        private enum SaleInvoiceColumn
        {
            ProductCode,
            Description,
            Quantity,
            UnitPrice,
            Total
        }

        public SalePanel(IEventAggregator eventAggregator, 
            ISaleInvoiceController saleInvoiceController, 
            AcceptPaymentForm acceptPaymentForm,
            UpdateInvoiceProductForm updateProductForm)
        {
            InitializeComponent();
            InitializeInvoiceDataView();

            _eventAggregator = eventAggregator;
            _saleInvoiceController = saleInvoiceController;
            _acceptPaymentForm = acceptPaymentForm;
            _updateProductForm = updateProductForm;

            SubscribeEvents();
        }

        private void SubscribeEvents()
		{
            _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Subscribe(SaleInvoiceProductChanged);
            _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Subscribe(SaleInvoiceProductChanged);
            _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Subscribe(SaleInvoiceProductChanged);
            _eventAggregator.GetEvent<PaymentAddedEvent>().Subscribe(PaymentChanged);
            _eventAggregator.GetEvent<NewSaleStartedEvent>().Subscribe(ResetSaleInvoiceScreen);
            _eventAggregator.GetEvent<BarcodeReceivedEvent>().Subscribe(BarcodeReceived);
        }

        private void InitializeInvoiceDataView()
        {
            #region Initialize all columns

            InvoiceDataView.Columns.Clear();
            InvoiceDataView.ColumnCount = 5;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].Name = "รหัสสินค้า";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].Width = 130;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].Name = "คำอธิบาย";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].Width = 250;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].Name = "จำนวน";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].Width = 100;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].Name = "ราคาต่อหน่วย";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].Width = 100;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Name = "ราคารวม";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Width = 100;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].ReadOnly = true;

            #endregion
        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            //TODO: Add dialog for adding product manually
        }

        private void RemoveProductButton_Click(object sender, EventArgs e)
        {
            var barcode = GetProductBarcodeFromSelectedProduct();

            _saleInvoiceController.RemoveProduct(barcode);
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
            _acceptPaymentForm.ShowDialog();
        }

        private void SaveSaleInvoiceButton_Click(object sender, EventArgs e)
        {
            if (_saleInvoiceController.Products.Count == 0)
			{
                MessageBox.Show("ไม่สามารถบันทึกการขายได้ เนื่องจากไม่มีสินค้าในรายการ", "การบันทึกการขาย");
                return;
			}
                
            if (_saleInvoiceController.InvoiceTotal > _saleInvoiceController.PaymentTotal)
			{
                MessageBox.Show("ไม่สามารถบันทึกการขายได้ เนื่องจากยังค้างค่าชำระสินค้า", "การบันทึกการขาย");
                return;
            }
                
            // TODO: Validate Payments, Insert invoice, products, and payments to database  

            _saleInvoiceController.StartNewSale();
        }

        private void CancelSaleInvoiceButton_Click(object sender, EventArgs e)
        {
            _saleInvoiceController.StartNewSale();
        }

        private void InvoiceDataView_DoubleClick(object sender, EventArgs e)
        {
            var barcode = GetProductBarcodeFromSelectedProduct();

            _updateProductForm.ShowDialog(barcode);
        }

        private void PaymentChanged()
		{
            TotalLabel.UIThread(delegate
            {
                TotalPaymentsLabel.Text = _saleInvoiceController.PaymentTotal.ToString("0.00");
                ChangesLabel.Text = _saleInvoiceController.Changes.ToString("0.00");
            });
        }

        private void ResetSaleInvoiceScreen()
		{
            InvoiceDataView.UIThread(delegate
            {
                InvoiceDataView.Rows.Clear();

                TotalLabel.Text = _saleInvoiceController.InvoiceTotal.ToString("0.00");
                TotalPaymentsLabel.Text = _saleInvoiceController.PaymentTotal.ToString("0.00");
                ChangesLabel.Text = _saleInvoiceController.Changes.ToString("0.00");
            });
        }

        private void BarcodeReceived(string barcode)
		{
            if (!Visible)
                return;

            AddProductToInvoice(barcode);
        }

        private void AddProductToInvoice(string barcode)
		{
            _saleInvoiceController.AddProduct(barcode);
        }
    }
}
