using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Windows.Forms.Enums;
using IndyPOS.Windows.Forms.Events;
using IndyPOS.Windows.Forms.Extensions;
using IndyPOS.Windows.Forms.Interfaces;
using IndyPOS.Windows.Forms.UI.Payment;
using Prism.Events;

namespace IndyPOS.Windows.Forms.UI.Sale;

[ExcludeFromCodeCoverage]
public partial class SalePanel : UserControl
{
	private readonly IEventAggregator _eventAggregator;
	private readonly AcceptPaymentForm _acceptPaymentForm;
	private readonly ISaleInvoiceController _saleInvoiceController;
	private readonly AddInvoiceProductForm _addInvoiceProductForm;
	private readonly UpdateInvoiceProductForm _updateProductForm;
	private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
	private SubPanel _activeSubPanel;
	private readonly MessageForm _messageForm;
	private readonly PrintReceiptForm _printReceiptForm;

	private const string GeneralGoodsBarcode = "2001000000012";
	private const string HardwareBarcode = "2005000000027";

	private enum SaleInvoiceColumn
	{
		Priority,
		ProductCode,
		Description,
		Quantity,
		UnitPrice,
		Total,
		Note
	}

	private enum PaymentColumn
	{
		PaymentPriority,
		PaymentType,
		PaymentAmount,
		Note
	}

	public SalePanel(IEventAggregator eventAggregator, 
					 ISaleInvoiceController saleInvoiceController, 
					 IStoreConstants storeConstants,
					 AcceptPaymentForm acceptPaymentForm, 
					 AddInvoiceProductForm addInvoiceProductForm,
					 UpdateInvoiceProductForm updateProductForm,
					 MessageForm messageForm,
					 PrintReceiptForm printReceiptForm)
	{
		InitializeComponent();
		InitializeInvoiceDataView();
		InitializePaymentDataView();

		_eventAggregator = eventAggregator;
		_saleInvoiceController = saleInvoiceController;
		_paymentTypeDictionary = storeConstants.PaymentTypes;
		_acceptPaymentForm = acceptPaymentForm;
		_addInvoiceProductForm = addInvoiceProductForm;
		_updateProductForm = updateProductForm;
		_messageForm = messageForm;
		_printReceiptForm = printReceiptForm;

		SubscribeEvents();
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
		InvoiceDataView.ColumnCount = 7;

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

		InvoiceDataView.Columns[(int)SaleInvoiceColumn.Note].Name = "Note";
		InvoiceDataView.Columns[(int)SaleInvoiceColumn.Note].Width = 200;
		InvoiceDataView.Columns[(int)SaleInvoiceColumn.Note].ReadOnly = true;

		#endregion
	}

	private void InitializePaymentDataView()
	{
		#region Initialize all columns

		PaymentDataView.Columns.Clear();
		PaymentDataView.ColumnCount = 4;

		PaymentDataView.Columns[(int)PaymentColumn.PaymentPriority].Name = "ลำดับ";
		PaymentDataView.Columns[(int)PaymentColumn.PaymentPriority].Width = 100;
		PaymentDataView.Columns[(int)PaymentColumn.PaymentPriority].ReadOnly = true;

		PaymentDataView.Columns[(int)PaymentColumn.PaymentType].Name = "ประเภทการชำระเงิน";
		PaymentDataView.Columns[(int)PaymentColumn.PaymentType].Width = 300;
		PaymentDataView.Columns[(int)PaymentColumn.PaymentType].ReadOnly = true;

		PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].Name = "จำนวนเงิน";
		PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].Width = 250;
		PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].ReadOnly = true;

		PaymentDataView.Columns[(int)PaymentColumn.Note].Name = "Note";
		PaymentDataView.Columns[(int)PaymentColumn.Note].Width = 200;
		PaymentDataView.Columns[(int)PaymentColumn.Note].ReadOnly = true;

		#endregion
	}

	private void ActiveSubPanelChanged(SubPanel activeSubPanel)
	{
		_activeSubPanel = activeSubPanel;
	}

	private void SaleInvoiceProductChanged()
	{
		this.UiThread(delegate
		{
			InvoiceDataView.Rows.Clear();

			var invoiceInfo = _saleInvoiceController.GetInvoiceInfo();

			foreach (var product in invoiceInfo.Products)
			{
				AddProductToInvoiceDataView(product);
			}

			InvoiceTotalLabel.Text = $"{invoiceInfo.InvoiceTotal:N}";
			ChangesLabel.Text = $"{invoiceInfo.Changes:N}";
		});
	}

	private void AddProductToInvoiceDataView(ISaleInvoiceProduct product)
	{
		var columnCount = InvoiceDataView.ColumnCount;
		var productRow = new object[columnCount];
		var total = product.UnitPrice * product.Quantity;

		productRow[(int)SaleInvoiceColumn.Priority] = product.Priority;
		productRow[(int)SaleInvoiceColumn.ProductCode] = product.Barcode;
		productRow[(int)SaleInvoiceColumn.Description] = product.Description;
		productRow[(int)SaleInvoiceColumn.Quantity] = product.Quantity;
		productRow[(int)SaleInvoiceColumn.UnitPrice] = product.UnitPrice;
		productRow[(int)SaleInvoiceColumn.Total] = total;
		productRow[(int)SaleInvoiceColumn.Note] = product.Note;

		var rowIndex = InvoiceDataView.Rows.Add(productRow);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		InvoiceDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private void AddPaymentToPaymentDataView(IPayment payment)
	{
		var columnCount = PaymentDataView.ColumnCount;
		var paymentRow = new object[columnCount];

		paymentRow[(int)PaymentColumn.PaymentPriority] = payment.Priority;
		paymentRow[(int)PaymentColumn.PaymentType] = _paymentTypeDictionary[payment.PaymentTypeId];
		paymentRow[(int)PaymentColumn.PaymentAmount] = payment.Amount;
		paymentRow[(int) PaymentColumn.Note] = payment.Note;

		var rowIndex = PaymentDataView.Rows.Add(paymentRow);
		var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

		PaymentDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
	}

	private void GetPaymentButton_Click(object sender, EventArgs e)
	{
		if (!_saleInvoiceController.IsPendingPayment())
		{
			_messageForm.Show("รายการเงินที่รับมาสมบูรณ์แล้ว", "ไม่สามารถรับรายการเงินเพิ่มได้อีก");

			return;
		}

		if (_acceptPaymentForm.Visible)
			_acceptPaymentForm.Hide();

		_acceptPaymentForm.Show();
	}

	private async void SaveSaleInvoiceButton_Click(object sender, EventArgs e)
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

		try
		{
			await _saleInvoiceController.CompleteSaleAsync();
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังบันทึกข้อมูล Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกข้อมูล");

			return;
		}

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
		var barcode = (string) selectedRow.Cells[(int)SaleInvoiceColumn.ProductCode].Value;

		if (!barcode.HasValue()) 
			return;

		var priority = (int) selectedRow.Cells[(int) SaleInvoiceColumn.Priority].Value;

		_updateProductForm.ShowDialog(barcode, priority);
	}

	private void PaymentChanged()
	{
		this.UiThread(delegate
		{
			PaymentDataView.Rows.Clear();

			var invoiceInfo = _saleInvoiceController.GetInvoiceInfo();

			foreach (var payment in invoiceInfo.Payments)
			{
				AddPaymentToPaymentDataView(payment);
			}

			PaymentsTotalLabel.Text = $"{invoiceInfo.PaymentTotal:N}";
			ChangesLabel.Text = $"{invoiceInfo.Changes:N}";
		});
	}

	private void ResetSaleInvoiceScreen()
	{
		this.UiThread(delegate
		{
			InvoiceDataView.Rows.Clear();

			var invoiceInfo = _saleInvoiceController.GetInvoiceInfo();

			InvoiceTotalLabel.Text = $"{invoiceInfo.InvoiceTotal:N}";
			PaymentsTotalLabel.Text = $"{invoiceInfo.PaymentTotal:N}";
			ChangesLabel.Text = $"{invoiceInfo.Changes:N}";

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
		try
		{
			var product = _saleInvoiceController.GetInventoryProductByBarcode(barcode);

			_saleInvoiceController.AddProduct(product);
		}
		catch (Exception ex)
		{
			_messageForm.Show($"ไม่พบรหัสสินค้า {barcode} ในระบบ กรุณาเพิ่มสินค้านี้เข้าในระบบก่อนเริ่มการขาย. Error: {ex.Message}", "ไม่สามารถเพิ่มสินค้าได้");
		}
	}

	private void ClearAllPaymentsButton_Click(object sender, EventArgs e)
	{
		_saleInvoiceController.RemoveAllPayments();
	}

	private void AddGeneralGoodsProductButton_Click(object sender, EventArgs e)
	{
		_addInvoiceProductForm.ShowDialog(GeneralGoodsBarcode);
	}

	private void AddHardwareProductButton_Click(object sender, EventArgs e)
	{
		_addInvoiceProductForm.ShowDialog(HardwareBarcode);
	}

	private void LookUpProductButton_Click(object sender, EventArgs e)
	{
		var lookUpQuery = ProductLookUpTextBox.Texts.Trim();

		_eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish(lookUpQuery);

		ProductLookUpTextBox.Texts = string.Empty;
	}
}