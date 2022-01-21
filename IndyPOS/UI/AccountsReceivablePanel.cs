using IndyPOS.Controllers;
using IndyPOS.Enums;
using IndyPOS.Sales;
using IndyPOS.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	[ExcludeFromCodeCoverage]
    public partial class AccountsReceivablePanel : UserControl
    {
		private readonly IAccountsReceivableController _arController;
		private readonly IUserAccountHelper _accountHelper;
		private readonly MessageForm _messageForm;
		private IList<IAccountsReceivable> _accountsReceivables;
		private IAccountsReceivable _selectedAccountsReceivable;

		private enum AccountColumn
		{
			InvoiceId,
			Description,
			ReceivableAmount,
			PaidAmount,
			IsCompleted,
			PaymentId,
			DateCreated,
			DateUpdated
		}

        public AccountsReceivablePanel(IAccountsReceivableController arController,
									   IUserAccountHelper accountHelper, 
									   MessageForm messageForm)
		{
			_arController = arController;
			_accountHelper = accountHelper;
			_messageForm = messageForm;

            InitializeComponent();
			InitializeUserDataView();
		}

		private void InitializeUserDataView()
		{
			#region Initialize all columns

			ArDataView.Columns.Clear();
			ArDataView.ColumnCount = 8;

			ArDataView.Columns[(int)AccountColumn.InvoiceId].Name = "Invoice ID";
			ArDataView.Columns[(int) AccountColumn.InvoiceId].Width = 150;
			ArDataView.Columns[(int)AccountColumn.InvoiceId].ReadOnly = true;

			ArDataView.Columns[(int)AccountColumn.Description].Name = "คำอธิบาย";
			ArDataView.Columns[(int) AccountColumn.Description].Width = 250;
			ArDataView.Columns[(int)AccountColumn.Description].ReadOnly = true;

			ArDataView.Columns[(int)AccountColumn.ReceivableAmount].Name = "ยอดลงบัญชี";
			ArDataView.Columns[(int)AccountColumn.ReceivableAmount].Width = 150;
			ArDataView.Columns[(int)AccountColumn.ReceivableAmount].ReadOnly = true;

			ArDataView.Columns[(int)AccountColumn.PaidAmount].Name = "ยอดชำระ";
			ArDataView.Columns[(int)AccountColumn.PaidAmount].Width = 150;
			ArDataView.Columns[(int)AccountColumn.PaidAmount].ReadOnly = true;

			ArDataView.Columns[(int)AccountColumn.IsCompleted].Name = "สถานะ";
			ArDataView.Columns[(int)AccountColumn.IsCompleted].Width = 150;
			ArDataView.Columns[(int)AccountColumn.IsCompleted].ReadOnly = true;

			ArDataView.Columns[(int)AccountColumn.PaymentId].Name = "Payment ID";
			ArDataView.Columns[(int) AccountColumn.PaymentId].Width = 150;
			ArDataView.Columns[(int)AccountColumn.PaymentId].ReadOnly = true;

			ArDataView.Columns[(int)AccountColumn.DateCreated].Name = "วันที่สร้าง";
			ArDataView.Columns[(int)AccountColumn.DateCreated].Width = 200;
			ArDataView.Columns[(int)AccountColumn.DateCreated].ReadOnly = true;

			ArDataView.Columns[(int)AccountColumn.DateUpdated].Name = "วันที่อัพเดท";
			ArDataView.Columns[(int)AccountColumn.DateUpdated].Width = 200;
			ArDataView.Columns[(int)AccountColumn.DateUpdated].ReadOnly = true;

			#endregion
		}

		private void AddArToArDataView(IAccountsReceivable accountsReceivable)
		{
			var columnCount = ArDataView.ColumnCount;
			var row = new object[columnCount];
			
			row[(int)AccountColumn.InvoiceId] = accountsReceivable.InvoiceId;
			row[(int)AccountColumn.Description] = accountsReceivable.Description;
			row[(int)AccountColumn.ReceivableAmount] = accountsReceivable.ReceivableAmount;
			row[(int)AccountColumn.PaidAmount] = accountsReceivable.PaidAmount;
			row[(int)AccountColumn.IsCompleted] = accountsReceivable.IsCompleted ? "ชำระแล้ว" : "ยังไม่ชำระ";
			row[(int)AccountColumn.PaymentId] = accountsReceivable.PaymentId;
			row[(int)AccountColumn.DateCreated] = accountsReceivable.DateCreated;
			row[(int)AccountColumn.DateUpdated] = accountsReceivable.DateUpdated;

			ArDataView.Rows.Add(row);
		}

		private void ShowAccountReceivables(bool showIncompleteOnly)
        {
			ResetDetails();

			_accountsReceivables = _arController.GetAccountsReceivables();

			ArDataView.Rows.Clear();

			foreach (var accountsReceivable in _accountsReceivables)
			{
				if (showIncompleteOnly && accountsReceivable.IsCompleted)
					continue;

				AddArToArDataView(accountsReceivable);
			}
        }

        private void ShowArsButton_Click(object sender, EventArgs e)
		{
			var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;
			
			ShowAccountReceivables(showIncompleteOnly);
		}

        private void ArDataView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			var paymentId = GetPaymentIdFromSelectedAr();

			ShowArDetailsByPaymentId(paymentId);
		}

		private int GetPaymentIdFromSelectedAr()
		{
			if (ArDataView.SelectedCells.Count == 0)
				return -1;

			var selectedCell = ArDataView.SelectedCells[0];
			var rowIndex  = selectedCell.RowIndex;
			var selectedRow = ArDataView.Rows[rowIndex];
			var paymentId = (int) selectedRow.Cells[(int)AccountColumn.PaymentId].Value;

			return paymentId;
		}

		private void ResetDetails()
        {
			InvoiceIdLabel.Text = string.Empty;
			DescriptionLabel.Text = string.Empty;
			ReceivableAmountLabel.Text = string.Empty;
			PaidAmountTextBox.Texts = string.Empty;

			PaidAmountTextBox.ReadOnly = false;
			UpdateArButton.Visible = true;
        }

		private void ShowArDetailsByPaymentId(int paymentId)
		{
			_selectedAccountsReceivable = _accountsReceivables.FirstOrDefault(x => x.PaymentId == paymentId);
			
			if (_selectedAccountsReceivable == null)
				return;

			InvoiceIdLabel.Text = _selectedAccountsReceivable.InvoiceId.ToString();
			DescriptionLabel.Text = _selectedAccountsReceivable.Description;
			ReceivableAmountLabel.Text = $"{_selectedAccountsReceivable.ReceivableAmount:N}";
			PaidAmountTextBox.Texts = $"{_selectedAccountsReceivable.PaidAmount:N}";

			PaidAmountTextBox.ReadOnly = _selectedAccountsReceivable.IsCompleted;
			UpdateArButton.Visible = !_selectedAccountsReceivable.IsCompleted;
		}

		private bool ValidateUserInput()
		{
			if (!decimal.TryParse(PaidAmountTextBox.Texts.Trim(), out _))
			{
				_messageForm.Show("กรุณาใส่ยอดชำระให้ถูกต้อง", "ยอดชำระไม่ถูกต้อง");
				return false;
			}

			return true;
		}

        private void UpdateArButton_Click(object sender, EventArgs e)
		{
			if (!ValidateUserInput())
				return;

			var paidAmount = decimal.Parse(PaidAmountTextBox.Texts.Trim());

			_selectedAccountsReceivable.PaidAmount = paidAmount;

			_arController.UpdateAccountsReceivable(_selectedAccountsReceivable);

			ShowAccountReceivables(false);
        }

        private void LookUpArByInvoiceIdButton_Click(object sender, EventArgs e)
        {
			if (!int.TryParse(ArLookUpKeywordTextBox.Texts.Trim(), out var invoiceId))
			{
				_messageForm.Show("กรุณาใส่ Invoice ID ให้ถูกต้อง", "Invoice ID ไม่ถูกต้อง");
				return;
			}
			
			ArDataView.Rows.Clear();

			var accountsReceivable = _arController.GetAccountsReceivableByInvoiceId(invoiceId);

			if (accountsReceivable == null)
			{
				_messageForm.Show($"ไม่พบรายการลงบัญชีสำหรับ Invoice ID {invoiceId}", "ไม่พบรายการลงบัญชี");
				return;
			}

			AddArToArDataView(accountsReceivable);
		}

        private void ShowIncompleteOnlyCheckBox_Click(object sender, EventArgs e)
        {
			var showIncompleteOnly = ShowIncompleteOnlyCheckBox.Checked;
			
			ShowAccountReceivables(showIncompleteOnly);
        }

        private void CreateARForUnlinkedPaymentsButton_Click(object sender, EventArgs e)
        {
			_arController.ConvertPaymentsToAccountsReceivables();
        }

        private void AccountsReceivablePanel_VisibleChanged(object sender, EventArgs e)
		{
			if (_accountHelper.IsLoggedIn)
            {
				CreateARForUnlinkedPaymentsButton.Visible = _accountHelper.LoggedInUser.RoleId == (int) UserRole.SystemAdmin;
            }
		}
    }
}
