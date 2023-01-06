using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;
using IndyPOS.Windows.Forms.UI.Report;

namespace IndyPOS.Windows.Forms.UI.PayLater
{
    [ExcludeFromCodeCoverage]
    public partial class PayLaterPaymentPanel : UserControl
    {
		private readonly IPayLaterPaymentController _payLaterPaymentController;
		private readonly SaleHistoryByInvoiceIdForm _saleHistoryByInvoiceIdForm;
		private readonly MessageForm _messageForm;
		private IList<IPayLaterPayment> _payLaterPayments;

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

        public PayLaterPaymentPanel(IPayLaterPaymentController arController,
									SaleHistoryByInvoiceIdForm saleHistoryByInvoiceIdForm,
									MessageForm messageForm)
		{
			_payLaterPaymentController = arController;
			_saleHistoryByInvoiceIdForm = saleHistoryByInvoiceIdForm;
			_messageForm = messageForm;

			_payLaterPayments = new List<IPayLaterPayment>();

            InitializeComponent();
			InitializeUserDataView();
		}

		private void InitializeUserDataView()
		{
			#region Initialize all columns

			PayLaterPaymentsDataView.Columns.Clear();
			PayLaterPaymentsDataView.ColumnCount = 8;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].Name = "Invoice ID";
			PayLaterPaymentsDataView.Columns[(int) AccountColumn.InvoiceId].Width = 150;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.InvoiceId].ReadOnly = true;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.Description].Name = "คำอธิบาย";
			PayLaterPaymentsDataView.Columns[(int) AccountColumn.Description].Width = 250;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.Description].ReadOnly = true;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.Amount].Name = "ยอดลงบัญชี";
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.Amount].Width = 150;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.Amount].ReadOnly = true;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaidAmount].Name = "ยอดชำระ";
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaidAmount].Width = 150;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaidAmount].ReadOnly = true;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.IsCompleted].Name = "สถานะ";
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.IsCompleted].Width = 150;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.IsCompleted].ReadOnly = true;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaymentId].Name = "Payment ID";
			PayLaterPaymentsDataView.Columns[(int) AccountColumn.PaymentId].Width = 150;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.PaymentId].ReadOnly = true;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].Name = "วันที่สร้าง";
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].Width = 200;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateCreated].ReadOnly = true;

			PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].Name = "วันที่อัพเดท";
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].Width = 200;
			PayLaterPaymentsDataView.Columns[(int)AccountColumn.DateUpdated].ReadOnly = true;

			#endregion
		}

		private void AddToPayLaterPaymentsDataView(IPayLaterPayment payLaterPayment)
		{
			var columnCount = PayLaterPaymentsDataView.ColumnCount;
			var row = new object[columnCount];
			
			row[(int)AccountColumn.InvoiceId] = payLaterPayment.InvoiceId;
			row[(int)AccountColumn.Description] = payLaterPayment.Description;
			row[(int)AccountColumn.Amount] = payLaterPayment.Amount;
			row[(int)AccountColumn.PaidAmount] = payLaterPayment.PaidAmount;
			row[(int)AccountColumn.IsCompleted] = payLaterPayment.IsCompleted ? "ชำระแล้ว" : "ยังไม่ชำระ";
			row[(int)AccountColumn.PaymentId] = payLaterPayment.PaymentId;
			row[(int)AccountColumn.DateCreated] = payLaterPayment.DateCreated;
			row[(int)AccountColumn.DateUpdated] = payLaterPayment.DateUpdated;

			var rowIndex = PayLaterPaymentsDataView.Rows.Add(row);
			var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

			PayLaterPaymentsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
			
			if (payLaterPayment.IsCompleted)
				PayLaterPaymentsDataView.Rows[rowIndex].Cells[(int)AccountColumn.IsCompleted].Style.BackColor = Color.FromArgb(30, 65, 30);
		}

		private void ShowPayLaterPayments(bool showIncompleteOnly)
        {
			ResetDetails();

			_payLaterPayments = _payLaterPaymentController.GetPayLaterPayments();

			PayLaterPaymentsDataView.Rows.Clear();

			foreach (var payment in _payLaterPayments)
			{
				if (showIncompleteOnly && payment.IsCompleted)
					continue;

				AddToPayLaterPaymentsDataView(payment);
			}
        }

        private void ShowPayLaterPaymentsButton_Click(object sender, EventArgs e)
		{
			var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;
			
			ShowPayLaterPayments(showIncompleteOnly);
		}

        private void PayLaterPaymentsDataView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			var paymentId = GetPaymentIdFromSelectedPayLaterPayment();

			ShowPayLaterPaymentDetailsByPaymentId(paymentId);
		}

		private int GetPaymentIdFromSelectedPayLaterPayment()
		{
			if (PayLaterPaymentsDataView.SelectedCells.Count == 0)
				return -1;

			var selectedCell = PayLaterPaymentsDataView.SelectedCells[0];
			var rowIndex  = selectedCell.RowIndex;
			var selectedRow = PayLaterPaymentsDataView.Rows[rowIndex];
			var paymentId = (int) selectedRow.Cells[(int)AccountColumn.PaymentId].Value;

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

		private void ShowPayLaterPaymentDetailsByPaymentId(int paymentId)
		{
			try
			{
				var payment = _payLaterPaymentController.GetPayLaterPaymentByPaymentId(paymentId);

				PaymentIdLabel.Text = payment.PaymentId.ToString();
				InvoiceIdLabel.Text = payment.InvoiceId.ToString();
				DescriptionLabel.Text = payment.Description;
				AmountLabel.Text = $"{payment.Amount:N}";
				PaidAmountTextBox.Texts = $"{payment.PaidAmount:N}";

				PaidAmountTextBox.ReadOnly = payment.IsCompleted;
				UpdateButton.Visible = !payment.IsCompleted;
			}
			catch (PayLaterPaymentNotFoundException ex)
			{
				_messageForm.Show($"ไม่พบรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
			}
		}

		private bool ValidateUserInput()
		{
			if (decimal.TryParse(PaidAmountTextBox.Texts.Trim(), out _)) 
				return true;

			_messageForm.Show("กรุณาใส่ยอดชำระให้ถูกต้อง", "ยอดชำระไม่ถูกต้อง");

			return false;
		}

        private void UpdateArButton_Click(object sender, EventArgs e)
		{
			if (!ValidateUserInput())
				return;

			var paymentId = int.Parse(PaymentIdLabel.Text);
			var paidAmount = decimal.Parse(PaidAmountTextBox.Texts.Trim());

			try
			{
				var payment = _payLaterPaymentController.GetPayLaterPaymentByPaymentId(paymentId);

				payment.PaidAmount = paidAmount;

				_payLaterPaymentController.UpdatePayLaterPayment(payment);
			}
			catch (PayLaterPaymentNotFoundException ex)
			{
				_messageForm.Show($"ไม่พบรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
			}
			catch (PayLaterPaymentNotUpdatedException ex)
			{
				_messageForm.Show($"ไม่สามารถอัพเดทรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่สามารถอัพเดทรายการลงบัญชี");
			}
			catch (Exception ex)
			{
				_messageForm.Show($"เกิดข้อผิดพลาดระหว่างที่กำลังอัพเดทรายการลงบัญชีสำหรับ Payment ID {paymentId}. Error: {ex.Message}", "ไม่สามารถอัพเดทรายการลงบัญชี");
			}

			ShowPayLaterPayments(ShowIncompleteOnlyCheckBox.Checked);
        }

        private void LookUpByInvoiceIdButton_Click(object sender, EventArgs e)
        {
			if (!int.TryParse(LookUpByInvoiceIdTextBox.Texts.Trim(), out var invoiceId))
			{
				_messageForm.Show("กรุณาใส่ Invoice ID ให้ถูกต้อง", "Invoice ID ไม่ถูกต้อง");
				return;
			}
			
			PayLaterPaymentsDataView.Rows.Clear();

			try
			{
				var payment = _payLaterPaymentController.GetPayLaterPaymentByInvoiceId(invoiceId);

				AddToPayLaterPaymentsDataView(payment);
			}
			catch (PayLaterPaymentNotFoundException ex)
			{
				_messageForm.Show($"ไม่พบรายการลงบัญชีสำหรับ Invoice ID {invoiceId}. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
			}
			catch (Exception ex)
			{
				_messageForm.Show($"เกิดข้อผิดพลาดระหว่างที่กำลังค้นหารายการลงบัญชีสำหรับ Invoice ID {invoiceId}. Error: {ex.Message}", "ไม่พบรายการลงบัญชี");
			}
		}

        private void ShowIncompleteOnlyCheckBox_Click(object sender, EventArgs e)
        {
			var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;
			
			ShowPayLaterPayments(showIncompleteOnly);
        }

        private void PayLaterPaymentsDataView_DoubleClick(object sender, EventArgs e)
        {
			if (PayLaterPaymentsDataView.SelectedCells.Count == 0)
				return;

			var selectedCell = PayLaterPaymentsDataView.SelectedCells[0];
			var rowIndex = selectedCell.RowIndex;
			var selectedRow = PayLaterPaymentsDataView.Rows[rowIndex];
			var invoiceId = (int) selectedRow.Cells[(int) AccountColumn.InvoiceId].Value;
			
			_saleHistoryByInvoiceIdForm.ShowDialog(invoiceId);
        }
    }
}
