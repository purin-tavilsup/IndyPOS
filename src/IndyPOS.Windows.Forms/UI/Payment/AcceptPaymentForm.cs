using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Payment
{
    [ExcludeFromCodeCoverage]
	public partial class AcceptPaymentForm : Form
    {
        private readonly ISaleService _saleService;
        private readonly IStoreHubClient _storeHubClient;
        private readonly IList<decimal> _values;
		private readonly MessageForm _messageForm;
        private readonly List<Button> _paymentMethodButtons;
        private IReadOnlyList<PaymentMethodDto> _offerableMethods;
        private string _selectedMethodCode;
        private bool _isPaymentTypeSelected;
        private decimal _amount;
        private string _pendingStringValue;

        public AcceptPaymentForm(ISaleService saleService,
								 IStoreHubClient storeHubClient,
								 MessageForm messageForm)
        {
			_saleService = saleService;
            _storeHubClient = storeHubClient;
			_messageForm = messageForm;

            InitializeComponent();

			_pendingStringValue = string.Empty;
            _values = new List<decimal>();
            _paymentMethodButtons = new List<Button>();
            _offerableMethods = Array.Empty<PaymentMethodDto>();
            _selectedMethodCode = string.Empty;
        }

        private void ResetPaymentTypeSelection()
		{
            // Default to Cash if offered, otherwise the first method by DisplayOrder.
            var defaultMethod = _offerableMethods
                                .FirstOrDefault(m => string.Equals(m.Code, PaymentMethodCodes.Cash, StringComparison.OrdinalIgnoreCase))
                                ?? _offerableMethods.OrderBy(m => m.DisplayOrder).FirstOrDefault();

            if (defaultMethod is null)
                return;

            ChangePaymentType(defaultMethod.Code);
        }

        public new async Task ShowDialog()
        {
            try
            {
                _offerableMethods = await _storeHubClient.GetOfferablePaymentMethodsAsync();
            }
            catch (Exception ex)
            {
                _messageForm.BringToFront();
                _messageForm.ShowDialog($"ไม่สามารถโหลดวิธีการชำระเงินได้ Error: {ex.Message}", "เกิดความผิดพลาด");

                return;
            }

            BuildPaymentMethodButtons();

            _pendingStringValue = string.Empty;
            _values.Clear();
			NoteTextBox.Texts = string.Empty;

			ResetPaymentTypeSelection();

			var balanceRemaining = _saleService.CalculateBalanceRemaining();

			BalanceRemainingLabel.Text = $"{balanceRemaining:N}";

			var isRefundInvoice = _saleService.IsRefundInvoice();

			if (!isRefundInvoice)
			{
				ConfigureFormForRegularPayment();
			}
			else
			{
				ConfigureFormForRefund(balanceRemaining);
			}

			base.ShowDialog();
        }

        private void BuildPaymentMethodButtons()
        {
            foreach (var button in _paymentMethodButtons)
            {
                button.Click -= PaymentMethodButton_Click;
                PaymentTypePanel.Controls.Remove(button);
                button.Dispose();
            }

            _paymentMethodButtons.Clear();

            var orderedMethods = _offerableMethods.OrderBy(m => m.DisplayOrder).ToList();

            for (var index = 0; index < orderedMethods.Count; index++)
            {
                var button = CreatePaymentMethodButton(orderedMethods[index], index);

                button.Click += PaymentMethodButton_Click;

                _paymentMethodButtons.Add(button);
                PaymentTypePanel.Controls.Add(button);
            }
        }

        private static Button CreatePaymentMethodButton(PaymentMethodDto method, int index)
        {
            const int columnCount = 2;
            var column = index % columnCount;
            var row = index / columnCount;

            var button = new Button
            {
                Tag = method.Code,
                Text = method.DisplayName,
                Size = new Size(195, 129),
                Location = new Point(10 + column * 201, 16 + row * 135),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Leelawadee UI", 12F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };

            var icon = PaymentMethodIcons.For(method.Code);

            if (icon is not null)
            {
                button.Image = icon;
                button.TextImageRelation = TextImageRelation.ImageAboveText;
            }

            return button;
        }

        private void PaymentMethodButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button { Tag: string code })
                ChangePaymentType(code);
        }

		private void ConfigureFormForRegularPayment()
		{
			_amount = 0m;
			
			DisplayValue(_amount);

			AcceptPaymentButton.Visible = true;
			RefundButton.Visible = false;
			AcceptPayLaterPaymentButton.Visible = false;
			KeypadPanel.Enabled = true;

			EnableAcceptablePaymentTypesForRegularPayment();
		}

		private void ConfigureFormForRefund(decimal refundAmount)
		{
			_amount = refundAmount;
			
			DisplayValue(_amount);

			NoteTextBox.Texts = "Refund";
			RefundButton.Visible = true;
			AcceptPaymentButton.Visible = false;
			AcceptPayLaterPaymentButton.Visible = false;
			KeypadPanel.Enabled = false;

			DisableNonAcceptablePaymentTypeSForRefund();
		}

		private void DisableNonAcceptablePaymentTypeSForRefund()
		{
			// Refund allows only Cash + MoneyTransfer.
			foreach (var button in _paymentMethodButtons)
			{
				var code = (string)button.Tag!;

				button.Enabled = string.Equals(code, PaymentMethodCodes.Cash, StringComparison.OrdinalIgnoreCase)
							  || string.Equals(code, PaymentMethodCodes.MoneyTransfer, StringComparison.OrdinalIgnoreCase);
			}
		}

		private void EnableAcceptablePaymentTypesForRegularPayment()
		{
			foreach (var button in _paymentMethodButtons)
				button.Enabled = true;
		}

        private bool ValidatePaymentType()
		{
			if (!_isPaymentTypeSelected)
            {
				_messageForm.BringToFront();
				_messageForm.ShowDialog("กรุณาเลือกวิธีการชำระเงิน", "วิธีการชำระเงินยังไม่ถูกเลือก");

				return false;
			}

			if (string.Equals(_selectedMethodCode, PaymentMethodCodes.PayLater, StringComparison.OrdinalIgnoreCase) && !NoteTextBox.Texts.HasValue())
			{
				_messageForm.BringToFront();
				_messageForm.ShowDialog("กรุณาใส่ Note สำหรับการลงบัญชี", "Note ไม่ถูกต้อง");

				return false;
			}

			if (_saleService.IsRefundInvoice()
				&& !string.Equals(_selectedMethodCode, PaymentMethodCodes.Cash, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(_selectedMethodCode, PaymentMethodCodes.MoneyTransfer, StringComparison.OrdinalIgnoreCase))
			{
				_messageForm.BringToFront();
				_messageForm.ShowDialog("การคืนเงินรองรับเฉพาะเงินสดหรือเงินโอนเท่านั้น กรุณาเลือกวิธีการชำระเงินใหม่", "วิธีการชำระเงินไม่ถูกต้อง");

				return false;
			}

			return true;
		}

        private void AcceptPaymentButton_Click(object sender, EventArgs e)
        {
            if (!ValidatePaymentType())
				return;

			CalculateLatestAmount();

			var note = NoteTextBox.Texts.Trim();

            _saleService.AddPayment(_selectedMethodCode, _amount, note);

            Hide();
        }

		private void RefundButton_Click(object sender, EventArgs e)
		{
			if (!ValidatePaymentType())
				return;

			var note = NoteTextBox.Texts.Trim();

			_saleService.AddPayment(_selectedMethodCode, _amount, note);

			Hide();
		}

        private void CancelAcceptPaymentButton_Click(object sender, EventArgs e)
        {
			Hide();
        }

		private void ChangePaymentType(string methodCode)
		{
			_selectedMethodCode = methodCode;
			_isPaymentTypeSelected = true;

			var selectedMethod = _offerableMethods
								 .FirstOrDefault(m => string.Equals(m.Code, methodCode, StringComparison.OrdinalIgnoreCase));

			PaymentTypeLabel.Text = selectedMethod?.DisplayName ?? methodCode;

			var isPayLater = string.Equals(methodCode, PaymentMethodCodes.PayLater, StringComparison.OrdinalIgnoreCase);
			var isRefundInvoice = _saleService.IsRefundInvoice();

			AcceptPaymentButton.Visible = !isPayLater && !isRefundInvoice;
			AcceptPayLaterPaymentButton.Visible = isPayLater && !isRefundInvoice;

			if (isPayLater)
				DisplayValue(_saleService.CalculateBalanceRemaining());
		}

        private void AddByBankNoteValue(decimal value)
        {
			_pendingStringValue = string.Empty;

			_values.Add(value);
			_amount = _values.Sum();

			DisplayValue(_amount);
        }

		private void Add20Button_Click(object sender, EventArgs e)
		{
			AddByBankNoteValue(20m);
		}

		private void Add50Button_Click(object sender, EventArgs e)
		{
			AddByBankNoteValue(50m);
        }

		private void Add100Button_Click(object sender, EventArgs e)
		{
			AddByBankNoteValue(100m);
        }

		private void Add500Button_Click(object sender, EventArgs e)
		{
			AddByBankNoteValue(500m);
        }

		private void Add1000Button_Click(object sender, EventArgs e)
		{
			AddByBankNoteValue(1000m);
        }

		private void Digit1Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "1";

            DisplayValue(_pendingStringValue);
        }

		private void Digit2Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "2";

            DisplayValue(_pendingStringValue);
        }

		private void Digit3Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "3";

            DisplayValue(_pendingStringValue);
        }

		private void Digit4Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "4";

            DisplayValue(_pendingStringValue);
        }

		private void Digit5Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "5";

            DisplayValue(_pendingStringValue);
        }

		private void Digit6Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "6";

            DisplayValue(_pendingStringValue);
        }

		private void Digit7Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "7";

            DisplayValue(_pendingStringValue);
        }

		private void Digit8Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "8";

            DisplayValue(_pendingStringValue);
        }

		private void Digit9Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "9";

            DisplayValue(_pendingStringValue);
        }

		private void DecimalPointButton_Click(object sender, EventArgs e)
		{
            if (_pendingStringValue.Contains("."))
                return;

			var decimalPoint = _pendingStringValue.HasValue() ? "." : "0.";

            _pendingStringValue += decimalPoint;

            DisplayValue(_pendingStringValue);
        }

		private void PlusButton_Click(object sender, EventArgs e)
		{
            CalculateLatestAmount();
            DisplayValue(_amount);
        }

        private void CalculateLatestAmount()
		{
            if (!_pendingStringValue.HasValue())
                return;

            var value = decimal.Parse(_pendingStringValue);

            _values.Add(value);
            _amount = _values.Sum();

            _pendingStringValue = string.Empty;
        }

		private void Digit0Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue += "0";

            DisplayValue(_pendingStringValue);
        }

		private void ClearButton_Click(object sender, EventArgs e)
		{
            _amount = 0m;
            _values.Clear();
            _pendingStringValue = string.Empty;

            DisplayValue(_amount);
        }

		private void EqualButton_Click(object sender, EventArgs e)
		{
            CalculateLatestAmount();
            DisplayValue(_amount);
        }

        private void DisplayValue(decimal value)
		{
            DisplayValue($"{value:N}");
        }

        private void DisplayValue(string value)
        {
            PaymentAmountLabel.Text = value;
        }

        private void AcceptPayLaterPaymentButton_Click(object sender, EventArgs e)
        {
			if (!ValidatePaymentType())
				return;

			var note = NoteTextBox.Texts.Trim();

			_amount = _saleService.CalculateBalanceRemaining();

			_saleService.AddPayment(_selectedMethodCode, _amount, note);

			Hide();
        }
    }
}
