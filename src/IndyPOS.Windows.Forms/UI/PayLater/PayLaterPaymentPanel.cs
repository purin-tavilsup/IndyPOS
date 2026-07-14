using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Windows.Forms.UI.Report;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.PayLater;

[ExcludeFromCodeCoverage]
public partial class PayLaterPaymentPanel : UserControl
{
    private readonly IPayLaterService? _payLaterService;
    private readonly SaleHistoryByInvoiceIdForm _saleHistoryByInvoiceIdForm;
    private readonly MessageForm _messageForm;

    private enum AccountColumn
    {
        Id,
        InvoiceId,
        Description,
        Amount,
        PaidAmount,
        IsCompleted,
        DateCreated,
        DateUpdated
    }

    public PayLaterPaymentPanel(
        SaleHistoryByInvoiceIdForm saleHistoryByInvoiceIdForm,
        MessageForm messageForm,
        IPayLaterService? payLaterService = null)
    {
        _saleHistoryByInvoiceIdForm = saleHistoryByInvoiceIdForm;
        _messageForm = messageForm;
        _payLaterService = payLaterService;

        InitializeComponent();
        InitializeUserDataView();
    }

    private void InitializeUserDataView()
    {
        #region Initialize all columns

        PayLaterPaymentsDataView.Columns.Clear();
        PayLaterPaymentsDataView.ColumnCount = 8;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Id].Name = "ID";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Id].Width = 0;  // Hidden
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Id].Visible = false;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].Name = "Invoice ID";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].Width = 0;  // Hidden
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].Visible = false;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Description].Name = "คำอธิบาย";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Description].Width = 250;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Description].ReadOnly = true;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Amount].Name = "ยอดลงบัญชี";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Amount].Width = 150;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Amount].ReadOnly = true;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.Amount].DefaultCellStyle.Format = "N2";

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaidAmount].Name = "ยอดชำระ";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaidAmount].Width = 150;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaidAmount].ReadOnly = true;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaidAmount].DefaultCellStyle.Format = "N2";

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.IsCompleted].Name = "สถานะ";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.IsCompleted].Width = 150;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.IsCompleted].ReadOnly = true;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].Name = "วันที่สร้าง";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].Width = 200;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].ReadOnly = true;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].Name = "วันที่อัพเดท";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].Width = 200;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].ReadOnly = true;

        #endregion
    }

    private void AddToPayLaterPaymentsDataView(PayLaterDto payment)
    {
        var columnCount = PayLaterPaymentsDataView.ColumnCount;
        var row = new object[columnCount];

        row[(int)AccountColumn.Id] = payment.Id;
        row[(int)AccountColumn.InvoiceId] = payment.InvoiceId;
        row[(int)AccountColumn.Description] = payment.Description;
        row[(int)AccountColumn.Amount] = payment.PayLaterAmount;
        row[(int)AccountColumn.PaidAmount] = payment.PaidAmount;
        row[(int)AccountColumn.IsCompleted] = payment.IsCompleted ? "ชำระแล้ว" : "ยังไม่ชำระ";
        row[(int)AccountColumn.DateCreated] = payment.CreatedUtc.ToLocalTime();
        row[(int)AccountColumn.DateUpdated] = payment.LastModifiedUtc.ToLocalTime();

        var rowIndex = PayLaterPaymentsDataView.Rows.Add(row);
        var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38, 38, 38) : Color.FromArgb(48, 48, 48);

        PayLaterPaymentsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;

        if (payment.IsCompleted)
        {
            PayLaterPaymentsDataView.Rows[rowIndex].Cells[(int)AccountColumn.IsCompleted].Style.BackColor = Color.FromArgb(30, 65, 30);
        }
    }

    private async Task ShowPayLaterPaymentsAsync(bool showIncompleteOnly)
    {
        ResetDetails();

        if (_payLaterService is null)
        {
            _messageForm.ShowDialog("PayLater service is not available. Please ensure StoreHub mode is enabled.", "Service Not Available");
            return;
        }

        try
        {
            var response = await _payLaterService.GetAllAsync(
                includeCompleted: !showIncompleteOnly,
                searchTerm: null);

            PayLaterPaymentsDataView.Rows.Clear();

            foreach (var payment in response.Items)
            {
                AddToPayLaterPaymentsDataView(payment);
            }
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เกิดข้อผิดพลาดในการดึงข้อมูล: {ex.Message}", "Error");
        }
    }

    private async void ShowPayLaterPaymentsButton_Click(object sender, EventArgs e)
    {
        var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;
        await ShowPayLaterPaymentsAsync(showIncompleteOnly);
    }

    private async void PayLaterPaymentsDataView_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        var payLaterId = GetPayLaterIdFromSelectedRow();

        if (payLaterId.HasValue)
        {
            await ShowPayLaterPaymentDetailsAsync(payLaterId.Value);
        }
    }

    private Guid? GetPayLaterIdFromSelectedRow()
    {
        if (PayLaterPaymentsDataView.SelectedCells.Count == 0)
        {
            return null;
        }

        var selectedCell = PayLaterPaymentsDataView.SelectedCells[0];
        var rowIndex = selectedCell.RowIndex;
        var selectedRow = PayLaterPaymentsDataView.Rows[rowIndex];
        var payLaterId = selectedRow.Cells[(int)AccountColumn.Id].Value;

        return payLaterId is Guid id ? id : null;
    }

    private Guid? GetInvoiceIdFromSelectedRow()
    {
        if (PayLaterPaymentsDataView.SelectedCells.Count == 0)
        {
            return null;
        }

        var selectedCell = PayLaterPaymentsDataView.SelectedCells[0];
        var rowIndex = selectedCell.RowIndex;
        var selectedRow = PayLaterPaymentsDataView.Rows[rowIndex];
        var invoiceId = selectedRow.Cells[(int)AccountColumn.InvoiceId].Value;

        return invoiceId is Guid id ? id : null;
    }

    private void ResetDetails()
    {
        PaymentIdLabel.Text = string.Empty;
        InvoiceIdLabel.Text = string.Empty;
        DescriptionLabel.Text = string.Empty;
        AmountLabel.Text = string.Empty;
        PaidAmountTextBox.Texts = string.Empty;

        PaidAmountTextBox.ReadOnly = false;
        UpdateButton.Visible = true;
    }

    private async Task ShowPayLaterPaymentDetailsAsync(Guid payLaterId)
    {
        if (_payLaterService is null)
        {
            return;
        }

        try
        {
            var payment = await _payLaterService.GetByIdAsync(payLaterId);

            if (payment is null)
            {
                _messageForm.ShowDialog($"ไม่พบรายการลงบัญชีสำหรับ ID {payLaterId}", "ไม่พบรายการลงบัญชี");
                return;
            }

            PaymentIdLabel.Text = payment.Id.ToString();
            InvoiceIdLabel.Text = payment.InvoiceId.ToString();
            DescriptionLabel.Text = payment.Description;
            AmountLabel.Text = $"{payment.PayLaterAmount:N}";
            PaidAmountTextBox.Texts = $"{payment.PaidAmount:N}";

            PaidAmountTextBox.ReadOnly = payment.IsCompleted;
            UpdateButton.Visible = !payment.IsCompleted;
        }
        catch (PayLaterPaymentNotFoundException ex)
        {
            _messageForm.ShowDialog($"ไม่พบรายการลงบัญชี. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
        }
    }

    private bool ValidateUserInput()
    {
        if (decimal.TryParse(PaidAmountTextBox.Texts.Trim(), out var amount) && amount >= 0)
        {
            return true;
        }

        _messageForm.ShowDialog("กรุณาใส่ยอดชำระให้ถูกต้อง", "ยอดชำระไม่ถูกต้อง");
        return false;
    }

    private async void UpdateArButton_Click(object sender, EventArgs e)
    {
        if (!ValidateUserInput())
        {
            return;
        }

        if (_payLaterService is null)
        {
            _messageForm.ShowDialog("PayLater service is not available.", "Service Not Available");
            return;
        }

        if (!Guid.TryParse(PaymentIdLabel.Text, out var payLaterId))
        {
            _messageForm.ShowDialog("Invalid PayLater ID", "Error");
            return;
        }

        var newPaidAmount = decimal.Parse(PaidAmountTextBox.Texts.Trim());

        try
        {
            // Get current payment to calculate the delta
            var current = await _payLaterService.GetByIdAsync(payLaterId);
            if (current is null)
            {
                _messageForm.ShowDialog($"ไม่พบรายการลงบัญชี", "Error");
                return;
            }

            // Calculate payment amount as the difference
            var paymentAmount = newPaidAmount - current.PaidAmount;

            if (paymentAmount <= 0)
            {
                _messageForm.ShowDialog("ยอดชำระใหม่ต้องมากกว่ายอดที่ชำระแล้ว", "ยอดชำระไม่ถูกต้อง");
                return;
            }

            await _payLaterService.RecordPaymentAsync(payLaterId, paymentAmount);
        }
        catch (PayLaterPaymentNotFoundException ex)
        {
            _messageForm.ShowDialog($"ไม่พบรายการลงบัญชี. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
        }
        catch (PayLaterPaymentNotUpdatedException ex)
        {
            _messageForm.ShowDialog($"ไม่สามารถอัพเดทรายการลงบัญชี. Error: {ex.Message}", "ไม่สามารถอัพเดทรายการลงบัญชี");
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เกิดข้อผิดพลาด: {ex.Message}", "Error");
        }

        await ShowPayLaterPaymentsAsync(ShowIncompleteOnlyCheckBox.Checked);
    }

    private async void SearchByKeywordButton_Click(object sender, EventArgs e)
    {
        ResetDetails();

        var keyword = SearchByKeywordTextBox.Texts.Trim();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        if (_payLaterService is null)
        {
            _messageForm.ShowDialog("PayLater service is not available.", "Service Not Available");
            return;
        }

        try
        {
            PayLaterPaymentsDataView.Rows.Clear();

            var response = await _payLaterService.GetAllAsync(
                includeCompleted: true,
                searchTerm: keyword);

            foreach (var payment in response.Items)
            {
                AddToPayLaterPaymentsDataView(payment);
            }
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เกิดข้อผิดพลาดในการค้นหา: {ex.Message}", "Error");
        }
    }

    private async void ShowIncompleteOnlyCheckBox_Click(object sender, EventArgs e)
    {
        var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;
        await ShowPayLaterPaymentsAsync(showIncompleteOnly);
    }

    private void PayLaterPaymentsDataView_DoubleClick(object sender, EventArgs e)
    {
        // Invoice viewing is currently not supported in StoreHub mode
        // TODO: Implement when invoice detail view is available via StoreHub client
        var invoiceId = GetInvoiceIdFromSelectedRow();
        if (invoiceId.HasValue)
        {
            _messageForm.ShowDialog($"Invoice ID: {invoiceId.Value}\n\n(Invoice detail view coming soon)", "Invoice Details");
        }
    }
}
