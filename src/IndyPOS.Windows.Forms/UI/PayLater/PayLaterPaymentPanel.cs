using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.PayLaterPayments;
using IndyPOS.Application.PayLaterPayments.Commands.UpdatePayLaterPayment;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentById;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPayments;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentsByDescriptionKeyword;
using IndyPOS.Windows.Forms.UI.Report;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.PayLater;

[ExcludeFromCodeCoverage]
public partial class PayLaterPaymentPanel : UserControl
{
    private readonly IMediator _mediator;
    private readonly SaleHistoryByInvoiceIdForm _saleHistoryByInvoiceIdForm;
    private readonly MessageForm _messageForm;

    private enum AccountColumn
    {
        InvoiceId,
        Description,
        Amount,
        PaidAmount,
        IsCompleted,
        PaymentId,
        DateCreated,
        DateUpdated
    }

    public PayLaterPaymentPanel(IMediator mediator,
                                SaleHistoryByInvoiceIdForm saleHistoryByInvoiceIdForm,
                                MessageForm messageForm)
    {
        _mediator = mediator;
        _saleHistoryByInvoiceIdForm = saleHistoryByInvoiceIdForm;
        _messageForm = messageForm;

        InitializeComponent();
        InitializeUserDataView();
    }

    private void InitializeUserDataView()
    {
        #region Initialize all columns

        PayLaterPaymentsDataView.Columns.Clear();
        PayLaterPaymentsDataView.ColumnCount = 8;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].Name = "Invoice ID";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].Width = 150;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].ReadOnly = true;

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

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaymentId].Name = "Payment ID";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaymentId].Width = 150;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaymentId].ReadOnly = true;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].Name = "วันที่สร้าง";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].Width = 200;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].ReadOnly = true;

        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].Name = "วันที่อัพเดท";
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].Width = 200;
        PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].ReadOnly = true;

        #endregion
    }

    private void AddToPayLaterPaymentsDataView(PayLaterPaymentDto payment)
    {
        var columnCount = PayLaterPaymentsDataView.ColumnCount;
        var row = new object[columnCount];

        row[(int)AccountColumn.InvoiceId] = payment.InvoiceId;
        row[(int)AccountColumn.Description] = payment.Description;
        row[(int)AccountColumn.Amount] = payment.ReceivableAmount;
        row[(int)AccountColumn.PaidAmount] = payment.PaidAmount;
        row[(int)AccountColumn.IsCompleted] = payment.IsCompleted ? "ชำระแล้ว" : "ยังไม่ชำระ";
        row[(int)AccountColumn.PaymentId] = payment.PaymentId;
        row[(int)AccountColumn.DateCreated] = payment.DateCreated;
        row[(int)AccountColumn.DateUpdated] = payment.DateUpdated;

        var rowIndex = PayLaterPaymentsDataView.Rows.Add(row);
        var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38, 38, 38) : Color.FromArgb(48, 48, 48);

        PayLaterPaymentsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;

        if (payment.IsCompleted)
        {
            PayLaterPaymentsDataView.Rows[rowIndex].Cells[(int)AccountColumn.IsCompleted].Style.BackColor = Color.FromArgb(30, 65, 30);
        }
    }

    private async Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsAsync()
    {
        return await _mediator.Send(new GetPayLaterPaymentsQuery());
    }

	private async Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsByDescriptionKeywordAsync(string keyword)
	{
		return await _mediator.Send(new GetPayLaterPaymentsByDescriptionKeywordQuery(keyword));
	}

    private async Task<PayLaterPaymentDto> GetPayLaterPaymentByPaymentIdAsync(int paymentId)
    {
        return await _mediator.Send(new GetPayLaterPaymentByIdQuery(paymentId));
    }

    private async Task UpdatePayLaterPaymentAsync(PayLaterPaymentDto payment, decimal paidAmount)
    {
        var command = CreateCommandForUpdatePayLaterPayment(payment, paidAmount);

        await _mediator.Send(command);
    }

    private static UpdatePayLaterPaymentCommand CreateCommandForUpdatePayLaterPayment(PayLaterPaymentDto payment, decimal paidAmount)
    {
        var isCompleted = paidAmount == payment.ReceivableAmount;

        return new UpdatePayLaterPaymentCommand
        {
            PaymentId = payment.PaymentId,
            PaidAmount = paidAmount,
            IsCompleted = isCompleted
        };
    }

    private async Task ShowPayLaterPaymentsAsync(bool showIncompleteOnly)
    {
        ResetDetails();

        var payments = await GetPayLaterPaymentsAsync();

        PayLaterPaymentsDataView.Rows.Clear();

        foreach (var payment in payments)
        {
            if (showIncompleteOnly && payment.IsCompleted)
                continue;

            AddToPayLaterPaymentsDataView(payment);
        }
    }

    private async void ShowPayLaterPaymentsButton_Click(object sender, EventArgs e)
    {
        var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;

        await ShowPayLaterPaymentsAsync(showIncompleteOnly);
    }

    private async void PayLaterPaymentsDataView_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        var paymentId = GetPaymentIdFromSelectedPayLaterPayment();

        await ShowPayLaterPaymentDetailsByPaymentIdAsync(paymentId);
    }

    private int GetPaymentIdFromSelectedPayLaterPayment()
    {
        if (PayLaterPaymentsDataView.SelectedCells.Count == 0)
        {
            return -1;
        }

        var selectedCell = PayLaterPaymentsDataView.SelectedCells[0];
        var rowIndex = selectedCell.RowIndex;
        var selectedRow = PayLaterPaymentsDataView.Rows[rowIndex];
        var paymentId = (int)selectedRow.Cells[(int)AccountColumn.PaymentId].Value;

        return paymentId;
    }

    private void ResetDetails()
    {
        InvoiceIdLabel.Text = string.Empty;
        DescriptionLabel.Text = string.Empty;
        AmountLabel.Text = string.Empty;
        PaidAmountTextBox.Texts = string.Empty;

        PaidAmountTextBox.ReadOnly = false;
        UpdateButton.Visible = true;
    }

    private async Task ShowPayLaterPaymentDetailsByPaymentIdAsync(int paymentId)
    {
        try
        {
            var payment = await GetPayLaterPaymentByPaymentIdAsync(paymentId);

            PaymentIdLabel.Text = payment.PaymentId.ToString();
            InvoiceIdLabel.Text = payment.InvoiceId.ToString();
            DescriptionLabel.Text = payment.Description;
            AmountLabel.Text = $"{payment.ReceivableAmount:N}";
            PaidAmountTextBox.Texts = $"{payment.PaidAmount:N}";

            PaidAmountTextBox.ReadOnly = payment.IsCompleted;
            UpdateButton.Visible = !payment.IsCompleted;
        }
        catch (PayLaterPaymentNotFoundException ex)
        {
            _messageForm.ShowDialog($"ไม่พบรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
        }
    }

    private bool ValidateUserInput()
    {
        if (decimal.TryParse(PaidAmountTextBox.Texts.Trim(), out _))
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

        var paymentId = int.Parse(PaymentIdLabel.Text);
        var paidAmount = decimal.Parse(PaidAmountTextBox.Texts.Trim());

        try
        {
            var payment = await GetPayLaterPaymentByPaymentIdAsync(paymentId);

            await UpdatePayLaterPaymentAsync(payment, paidAmount);
        }
        catch (PayLaterPaymentNotFoundException ex)
        {
            _messageForm.ShowDialog($"ไม่พบรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
        }
        catch (PayLaterPaymentNotUpdatedException ex)
        {
            _messageForm.ShowDialog($"ไม่สามารถอัพเดทรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่สามารถอัพเดทรายการลงบัญชี");
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เกิดข้อผิดพลาดระหว่างที่กำลังอัพเดทรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่สามารถอัพเดทรายการลงบัญชี");
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

        PayLaterPaymentsDataView.Rows.Clear();

		var payments = await GetPayLaterPaymentsByDescriptionKeywordAsync(keyword);

		PayLaterPaymentsDataView.Rows.Clear();

		foreach (var payment in payments)
		{
			AddToPayLaterPaymentsDataView(payment);
		}
    }

    private async void ShowIncompleteOnlyCheckBox_Click(object sender, EventArgs e)
    {
        var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;

        await ShowPayLaterPaymentsAsync(showIncompleteOnly);
    }

    private async void PayLaterPaymentsDataView_DoubleClick(object sender, EventArgs e)
    {
        if (PayLaterPaymentsDataView.SelectedCells.Count == 0)
            return;

        var selectedCell = PayLaterPaymentsDataView.SelectedCells[0];
        var rowIndex = selectedCell.RowIndex;
        var selectedRow = PayLaterPaymentsDataView.Rows[rowIndex];
        var invoiceId = (int)selectedRow.Cells[(int)AccountColumn.InvoiceId].Value;

        await _saleHistoryByInvoiceIdForm.ShowDialogAsync(invoiceId);
    }
}