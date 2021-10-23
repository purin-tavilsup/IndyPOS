using IndyPOS.Adapters;
using IndyPOS.Controllers;
using IndyPOS.SaleInvoice;
using IndyPOS.Events;
using IndyPOS.Extensions;
using IndyPOS.Enums;
using Prism.Events;
using System;
using System.Windows.Forms;
using IndyPOS.Constants;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.UI
{
    public partial class SalePanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly AcceptPaymentForm _acceptPaymentForm;
        private readonly ISaleInvoiceController _saleInvoiceController;
        private readonly UpdateInvoiceProductForm _updateProductForm;
        private readonly IStoreConstants _storeConstants;
        private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
        private Subpanel _activeSubPanel;
		private readonly MessageForm _messageForm;

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
						 MessageForm messageForm)
        {
            InitializeComponent();
            InitializeInvoiceDataView();
            InitializePaymentDataView();

            _eventAggregator = eventAggregator;
            _saleInvoiceController = saleInvoiceController;
            _storeConstants = storeConstants;
            _paymentTypeDictionary = _storeConstants.PaymentTypes;
            _acceptPaymentForm = acceptPaymentForm;
            _updateProductForm = updateProductForm;
			_messageForm = messageForm;

            SubscribeEvents();

            _saleInvoiceController.StartNewSale();
            AddProductToInvoice("8850999009674");
			AddProductToInvoice("8850999009674");
            AddProductToInvoice("8850250012238");
            AddProductToInvoice("8850250011613");
            AddProductToInvoice("8850999143002");
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
            _eventAggregator.GetEvent<ActiveSubpanelChangedEvent>().Subscribe(ActiveSubPanelChanged);
            
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

		private void ActiveSubPanelChanged(Subpanel activeSubPanel)
		{
            _activeSubPanel = activeSubPanel;
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
			_saleInvoiceController.StartNewSale();
		}

        private void CancelSaleInvoiceButton_Click(object sender, EventArgs e)
        {
            _saleInvoiceController.StartNewSale();
        }

        private void InvoiceDataView_DoubleClick(object sender, EventArgs e)
        {
            var barcode = GetProductBarcodeFromSelectedProduct();

            if (!barcode.HasValue()) return;

            _updateProductForm.ShowDialog(barcode);
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
            if (_activeSubPanel != Subpanel.Sales)
                return;

            AddProductToInvoice(barcode);
        }

        private void AddProductToInvoice(string barcode)
		{
            _saleInvoiceController.AddProduct(barcode);
        }

		private void AddProductToInvoice(string barcode, int quantity)
		{
			_saleInvoiceController.AddProduct(barcode, quantity);
		}

		private void ClearAllPaymentsButton_Click(object sender, EventArgs e)
		{
            _saleInvoiceController.RemoveAllPayments();
		}
	}
}