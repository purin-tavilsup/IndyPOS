using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Enums;
using IndyPOS.Events;
using IndyPOS.Extensions;
using IndyPOS.Sales;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
    public partial class SalePanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly AcceptPaymentForm _acceptPaymentForm;
        private readonly ISaleInvoiceController _saleInvoiceController;
        private readonly UpdateInvoiceProductForm _updateProductForm;
        private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
        private SubPanel _activeSubPanel;
		private readonly MessageForm _messageForm;
		private readonly PrintReceiptForm _printReceiptForm;
		private readonly AddGeneralGoodsProductForm _addGeneralGoodsProductForm;
		private readonly AddHardwareProductForm _addHardwareProductForm;

        private enum SaleInvoiceColumn
        {
            Priority,
            ProductCode,
            Description,
            Quantity,
            UnitPrice,
            Total
        }

        private enum PaymentColumn
        {
            PaymentPriority,
            PaymentType,
            PaymentAmount
        }

        public SalePanel(IEventAggregator eventAggregator, 
						 ISaleInvoiceController saleInvoiceController, 
						 IStoreConstants storeConstants,
						 AcceptPaymentForm acceptPaymentForm, 
						 UpdateInvoiceProductForm updateProductForm,
						 MessageForm messageForm,
						 PrintReceiptForm printReceiptForm,
						 AddGeneralGoodsProductForm addGeneralGoodsProductForm,
						 AddHardwareProductForm addHardwareProductForm)
        {
            InitializeComponent();
            InitializeInvoiceDataView();
            InitializePaymentDataView();

            _eventAggregator = eventAggregator;
            _saleInvoiceController = saleInvoiceController;
            _paymentTypeDictionary = storeConstants.PaymentTypes;
            _acceptPaymentForm = acceptPaymentForm;
            _updateProductForm = updateProductForm;
			_messageForm = messageForm;
			_printReceiptForm = printReceiptForm;
			_addGeneralGoodsProductForm = addGeneralGoodsProductForm;
			_addHardwareProductForm = addHardwareProductForm;

            SubscribeEvents();

            _saleInvoiceController.StartNewSale();
            AddProductToInvoice("8850999009674");
            AddProductToInvoice("8850250012238");
            AddProductToInvoice("8850250011613");
        }

        private void SubscribeEvents()
		{
            _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Subscribe(SaleInvoiceProductChanged);
            _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Subscribe(SaleInvoiceProductChanged);
            _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Subscribe(SaleInvoiceProductChanged);
            _eventAggregator.GetEvent<PaymentAddedEvent>().Subscribe(PaymentChanged);
            _eventAggregator.GetEvent<AllPaymentsRemovedEvent>().Subscribe(PaymentChanged);
            _eventAggregator.GetEvent<NewSaleStartedEvent>().Subscribe(ResetSaleInvoiceScreen);
            _eventAggregator.GetEvent<BarcodeReceivedEvent>().Subscribe(BarcodeReceived);
            _eventAggregator.GetEvent<ActiveSubPanelChangedEvent>().Subscribe(ActiveSubPanelChanged);
		}

        private void InitializeInvoiceDataView()
        {
            #region Initialize all columns

            InvoiceDataView.Columns.Clear();
            InvoiceDataView.ColumnCount = 6;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Priority].Name = "ลำดับ";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Priority].Width = 100;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Priority].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].Name = "รหัสสินค้า";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].Width = 200;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.ProductCode].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].Name = "คำอธิบาย";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].Width = 350;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Description].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].Name = "จำนวน";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].Width = 100;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Quantity].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].Name = "ราคาต่อหน่วย";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].Width = 150;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].ReadOnly = true;

            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Name = "ราคารวม";
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Width = 150;
            InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].ReadOnly = true;

            #endregion
        }

        private void InitializePaymentDataView()
		{
			#region Initialize all columns

            PaymentDataView.Columns.Clear();
            PaymentDataView.ColumnCount = 3;

            PaymentDataView.Columns[(int)PaymentColumn.PaymentPriority].Name = "ลำดับ";
            PaymentDataView.Columns[(int)PaymentColumn.PaymentPriority].Width = 100;
            PaymentDataView.Columns[(int)PaymentColumn.PaymentPriority].ReadOnly = true;

            PaymentDataView.Columns[(int)PaymentColumn.PaymentType].Name = "ประเภทการชำระเงิน";
            PaymentDataView.Columns[(int)PaymentColumn.PaymentType].Width = 300;
            PaymentDataView.Columns[(int)PaymentColumn.PaymentType].ReadOnly = true;

            PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].Name = "จำนวนเงิน";
            PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].Width = 250;
            PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].ReadOnly = true;

            #endregion
        }

		private void ActiveSubPanelChanged(SubPanel activeSubPanel)
		{
            _activeSubPanel = activeSubPanel;
		}

		private void SaleInvoiceProductChanged()
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

            productRow[(int)SaleInvoiceColumn.Priority] = product.Priority.ToString();
            productRow[(int)SaleInvoiceColumn.ProductCode] = product.Barcode;
            productRow[(int)SaleInvoiceColumn.Description] = product.Description;
            productRow[(int)SaleInvoiceColumn.Quantity] = product.Quantity.ToString();
            productRow[(int)SaleInvoiceColumn.UnitPrice] = product.UnitPrice.ToString("0.00");
            productRow[(int)SaleInvoiceColumn.Total] = total.ToString("0.00");

            InvoiceDataView.Rows.Add(productRow);
        }

        private void AddPaymentToPaymentDataView(IPayment payment)
        {
            var columnCount = PaymentDataView.ColumnCount;
            var paymentRow = new string[columnCount];

            paymentRow[(int)PaymentColumn.PaymentPriority] = payment.Priority.ToString();
            paymentRow[(int)PaymentColumn.PaymentType] = _paymentTypeDictionary[payment.PaymentTypeId];
            paymentRow[(int)PaymentColumn.PaymentAmount] = payment.Amount.ToString("0.00");

            PaymentDataView.Rows.Add(paymentRow);
        }

        private void GetPaymentButton_Click(object sender, EventArgs e)
        {
            _acceptPaymentForm.ShowDialog();
        }

        private void SaveSaleInvoiceButton_Click(object sender, EventArgs e)
		{
			var errorMessages = _saleInvoiceController.ValidateSaleInvoice();

			if (errorMessages.Any())
			{
				var message = string.Empty;

				foreach (var item in errorMessages)
				{
					message += $"- {item}" + Environment.NewLine;
				}

				_messageForm.Show(message, "ไม่สามารถบันทึกการขายได้");

                return;
			}

			_saleInvoiceController.CompleteSale();

            _printReceiptForm.ShowDialog();
			
			_saleInvoiceController.StartNewSale();
		}

        private void CancelSaleInvoiceButton_Click(object sender, EventArgs e)
        {
            _saleInvoiceController.StartNewSale();
        }

        private void InvoiceDataView_DoubleClick(object sender, EventArgs e)
        {
            if (InvoiceDataView.SelectedCells.Count == 0)
				return;

			var selectedCell = InvoiceDataView.SelectedCells[0];
			var rowIndex = selectedCell.RowIndex;
			var selectedRow = InvoiceDataView.Rows[rowIndex];
			var barcode = selectedRow.Cells[(int)SaleInvoiceColumn.ProductCode].Value as string;

			if (!barcode.HasValue()) 
				return;

			if (!int.TryParse(selectedRow.Cells[(int) SaleInvoiceColumn.Priority].Value as string, out var priority))
				return;

            _updateProductForm.ShowDialog(barcode, priority);
        }

        private void PaymentChanged()
		{
            PaymentDataView.UIThread(delegate
            {
                PaymentDataView.Rows.Clear();

                var payments = _saleInvoiceController.Payments;

                foreach (var payment in payments)
                {
                    AddPaymentToPaymentDataView(payment);
                }
            });

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

            PaymentDataView.UIThread(delegate
            {
                PaymentDataView.Rows.Clear();
            });
        }

        private void BarcodeReceived(string barcode)
		{
            if (_activeSubPanel != SubPanel.Sales)
                return;

            AddProductToInvoice(barcode);
        }

        private void AddProductToInvoice(string barcode)
		{
            _saleInvoiceController.AddProduct(barcode);
        }

		private void ClearAllPaymentsButton_Click(object sender, EventArgs e)
		{
            _saleInvoiceController.RemoveAllPayments();
		}

        private void AddGeneralGoodsProductButton_Click(object sender, EventArgs e)
        {
			_addGeneralGoodsProductForm.Show();
        }

        private void AddHardwareProductButton_Click(object sender, EventArgs e)
        {
            _addHardwareProductForm.Show();
        }
    }
}