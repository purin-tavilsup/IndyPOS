using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Events;
using IndyPOS.Domain.Events;
using IndyPOS.Windows.Forms.Enums;
using IndyPOS.Windows.Forms.Events;
using IndyPOS.Windows.Forms.Extensions;
using IndyPOS.Windows.Forms.UI.Payment;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Sale;

[ExcludeFromCodeCoverage]
public partial class SalePanel : UserControl
{
    private readonly IEventAggregator _eventAggregator;
    private readonly AcceptPaymentForm _acceptPaymentForm;
    private readonly ISaleService _saleService;
    private readonly AddInvoiceProductForm _addInvoiceProductForm;
    private readonly UpdateInvoiceProductForm _updateProductForm;
    private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
    private SubPanel _activeSubPanel;
    private readonly MessageForm _messageForm;
    private readonly PrintReceiptForm _printReceiptForm;
	private readonly ICashDrawerService _cashDrawerService;

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
                     ISaleService saleService,
                     IStoreConstants storeConstants,
                     AcceptPaymentForm acceptPaymentForm,
                     AddInvoiceProductForm addInvoiceProductForm,
                     UpdateInvoiceProductForm updateProductForm,
                     MessageForm messageForm,
                     PrintReceiptForm printReceiptForm, 
					 ICashDrawerService cashDrawerService)
    {
        InitializeComponent();
        InitializeInvoiceDataView();
        InitializePaymentDataView();

        _eventAggregator = eventAggregator;
        _saleService = saleService;
        _paymentTypeDictionary = storeConstants.PaymentTypes;
        _acceptPaymentForm = acceptPaymentForm;
        _addInvoiceProductForm = addInvoiceProductForm;
        _updateProductForm = updateProductForm;
        _messageForm = messageForm;
        _printReceiptForm = printReceiptForm;
		_cashDrawerService = cashDrawerService;

		SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        _eventAggregator.GetEvent<InvoiceProductAddedEvent>().Subscribe(SaleInvoiceProductChanged);
        _eventAggregator.GetEvent<InvoiceProductRemovedEvent>().Subscribe(SaleInvoiceProductChanged);
        _eventAggregator.GetEvent<InvoiceProductUpdatedEvent>().Subscribe(SaleInvoiceProductChanged);
        _eventAggregator.GetEvent<InvoicePaymentAddedEvent>().Subscribe(PaymentChanged);
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
        InvoiceDataView.Columns[(int)SaleInvoiceColumn.UnitPrice].DefaultCellStyle.Format = "N2";

        InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Name = "ราคารวม";
        InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].Width = 150;
        InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].ReadOnly = true;
        InvoiceDataView.Columns[(int)SaleInvoiceColumn.Total].DefaultCellStyle.Format = "N2";

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
        PaymentDataView.Columns[(int)PaymentColumn.PaymentAmount].DefaultCellStyle.Format = "N2";

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

            var products = _saleService.Products;
            var invoiceTotal = _saleService.CalculateInvoiceTotal();
            var changes = _saleService.CalculateChanges();

            foreach (var product in products)
            {
                AddProductToInvoiceDataView(product);
            }

            InvoiceTotalLabel.Text = $"{invoiceTotal:N}";
            ChangesLabel.Text = $"{changes:N}";
        });
    }

    private void AddProductToInvoiceDataView(Product product)
    {
        var columnCount = InvoiceDataView.ColumnCount;
        var productRow = new object[columnCount];
        var total = !product.IsGroupProduct ? product.UnitPrice * product.Quantity : product.GroupPrice;

        productRow[(int)SaleInvoiceColumn.Priority] = product.Priority;
        productRow[(int)SaleInvoiceColumn.ProductCode] = product.Barcode;
        productRow[(int)SaleInvoiceColumn.Description] = product.Description;
        productRow[(int)SaleInvoiceColumn.Quantity] = product.Quantity;
        productRow[(int)SaleInvoiceColumn.UnitPrice] = product.UnitPrice;
        productRow[(int)SaleInvoiceColumn.Total] = total;
        productRow[(int)SaleInvoiceColumn.Note] = product.Note;

        var rowIndex = InvoiceDataView.Rows.Add(productRow);
        var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38, 38, 38) : Color.FromArgb(48, 48, 48);

        InvoiceDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
        InvoiceDataView.CurrentCell = InvoiceDataView.Rows[rowIndex].Cells[0];
    }

    private void AddPaymentToPaymentDataView(Application.Common.Models.Payment payment)
    {
        var columnCount = PaymentDataView.ColumnCount;
        var paymentRow = new object[columnCount];

        paymentRow[(int)PaymentColumn.PaymentPriority] = payment.Priority;
        paymentRow[(int)PaymentColumn.PaymentType] = _paymentTypeDictionary[payment.PaymentTypeId];
        paymentRow[(int)PaymentColumn.PaymentAmount] = payment.Amount;
        paymentRow[(int)PaymentColumn.Note] = payment.Note;

        var rowIndex = PaymentDataView.Rows.Add(paymentRow);
        var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38, 38, 38) : Color.FromArgb(48, 48, 48);

        PaymentDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
        PaymentDataView.CurrentCell = PaymentDataView.Rows[rowIndex].Cells[0];
    }

    private void GetPaymentButton_Click(object sender, EventArgs e)
    {
        if (!_saleService.IsPendingPayment())
        {
            _messageForm.ShowDialog("รายการเงินที่รับมาสมบูรณ์แล้ว", "ไม่สามารถรับรายการเงินเพิ่มได้อีก");

            return;
        }

        if (_acceptPaymentForm.Visible)
        {
            _acceptPaymentForm.Hide();
        }

        _acceptPaymentForm.ShowDialog();
    }

    private async void SaveSaleInvoiceButton_Click(object sender, EventArgs e)
    {
        var errorMessages = _saleService.ValidateSaleInvoice();

        if (errorMessages.Any())
        {
            var message = string.Empty;

            foreach (var item in errorMessages)
            {
                message += $"- {item}" + Environment.NewLine;
            }

            _messageForm.ShowDialog(message, "ไม่สามารถบันทึกการขายได้");

            return;
        }

        try
        {
            var invoiceInfo = await _saleService.CompleteSaleAsync();

            _printReceiptForm.ShowDialog(invoiceInfo);

            _saleService.StartNewSale();
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังบันทึกข้อมูล Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกข้อมูล");
        }
    }

    private void CancelSaleInvoiceButton_Click(object sender, EventArgs e)
    {
        _saleService.StartNewSale();
    }

    private void InvoiceDataView_DoubleClick(object sender, EventArgs e)
    {
        if (InvoiceDataView.SelectedCells.Count == 0)
            return;

        var selectedCell = InvoiceDataView.SelectedCells[0];
        var rowIndex = selectedCell.RowIndex;
        var selectedRow = InvoiceDataView.Rows[rowIndex];
        var barcode = (string)selectedRow.Cells[(int)SaleInvoiceColumn.ProductCode].Value;

        if (!barcode.HasValue())
            return;

        var priority = (int)selectedRow.Cells[(int)SaleInvoiceColumn.Priority].Value;

        _updateProductForm.ShowDialog(barcode, priority);
    }

    private void PaymentChanged()
    {
        this.UiThread(delegate
        {
            PaymentDataView.Rows.Clear();

            var payments = _saleService.Payments;
            var paymentTotal = _saleService.CalculatePaymentTotal();
            var changes = _saleService.CalculateChanges();

            foreach (var payment in payments)
            {
                AddPaymentToPaymentDataView(payment);
            }

            PaymentsTotalLabel.Text = $"{paymentTotal:N}";
            ChangesLabel.Text = $"{changes:N}";
        });
    }

    private void ResetSaleInvoiceScreen()
    {
        this.UiThread(delegate
        {
            InvoiceDataView.Rows.Clear();

            var invoiceTotal = _saleService.CalculateInvoiceTotal();
            var paymentTotal = _saleService.CalculatePaymentTotal();
            var changes = _saleService.CalculateChanges();

            InvoiceTotalLabel.Text = $"{invoiceTotal:N}";
            PaymentsTotalLabel.Text = $"{paymentTotal:N}";
            ChangesLabel.Text = $"{changes:N}";

            PaymentDataView.Rows.Clear();
        });
    }

    private async void BarcodeReceived(string barcode)
    {
        if (_activeSubPanel != SubPanel.Sales)
            return;

        await AddProductToInvoiceAsync(barcode);
    }

    private async Task AddProductToInvoiceAsync(string barcode)
    {
        try
        {
            var product = await _saleService.GetInventoryProductByBarcodeAsync(barcode);

            _saleService.AddProduct(product);
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"ไม่พบรหัสสินค้า {barcode} ในระบบ กรุณาเพิ่มสินค้านี้เข้าในระบบก่อนเริ่มการขาย. Error: {ex.Message}", "ไม่สามารถเพิ่มสินค้าได้");
        }
    }

    private void ClearAllPaymentsButton_Click(object sender, EventArgs e)
    {
        _saleService.RemoveAllPayments();
    }

    private async void AddGeneralGoodsProductButton_Click(object sender, EventArgs e)
    {
        await _addInvoiceProductForm.ShowDialog(GeneralGoodsBarcode);
    }

    private async void AddHardwareProductButton_Click(object sender, EventArgs e)
    {
        await _addInvoiceProductForm.ShowDialog(HardwareBarcode);
    }

    private void LookUpProductButton_Click(object sender, EventArgs e)
    {
        var lookUpQuery = ProductLookUpTextBox.Texts.Trim();

        _eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish(lookUpQuery);

        ProductLookUpTextBox.Texts = string.Empty;
    }

    private void CashDrawerButton_Click(object sender, EventArgs e)
    {
        _cashDrawerService.OpenCashDrawer();
    }
}