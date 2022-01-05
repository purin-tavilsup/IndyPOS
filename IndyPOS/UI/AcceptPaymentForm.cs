﻿using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Enums;
using IndyPOS.Extensions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
	public partial class AcceptPaymentForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IStoreConstants _storeConstants;
        private readonly ISaleInvoiceController _saleInvoiceController;
        private readonly IList<decimal> _values;
		private readonly MessageForm _messageForm;
        private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
        private PaymentType _selectedPaymentType;
        private bool _isPaymentTypeSelected;
        private decimal _amount;
        private string _pendingStringValue;
		
        public AcceptPaymentForm(IEventAggregator eventAggregator,
								 IStoreConstants storeConstants,
								 ISaleInvoiceController saleInvoiceController,
								 MessageForm messageForm)
        {
            _eventAggregator = eventAggregator;
            _storeConstants = storeConstants;
            _saleInvoiceController = saleInvoiceController;
            _paymentTypeDictionary = _storeConstants.PaymentTypes;
			_messageForm = messageForm;

            InitializeComponent();

            _values = new List<decimal>();
        }

        private void ResetPaymentTypeSelection()
		{
            // Default to cash
            _selectedPaymentType = PaymentType.Cash;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
        }

        public new void ShowDialog()
        {
            _amount = 0m;
            _pendingStringValue = string.Empty;
            _values.Clear();

            DisplayValue(_amount);
            ResetPaymentTypeSelection();
            CancelAcceptPaymentButton.Select();

            base.ShowDialog();
        }

        private bool ValidatePaymentType()
		{
			if (_isPaymentTypeSelected)
				return true;

			_messageForm.Show("กรุณาเลือกวิธีการชำระเงิน", "วิธีการชำระเงินยังไม่ถูกเลือก");

			return false;
		}

        private void AcceptPaymentButton_Click(object sender, EventArgs e)
        {
            if (!ValidatePaymentType())
				return;

			CalculateLatestAmount();

            _saleInvoiceController.AddPayment(_selectedPaymentType, _amount);

            Hide();
        }

		private void RefundButton_Click(object sender, EventArgs e)
		{
			if (!ValidatePaymentType())
				return;

			CalculateLatestAmount();

			_saleInvoiceController.AddPayment(_selectedPaymentType, _amount * -1);

			Hide();
		}

        private void CancelAcceptPaymentButton_Click(object sender, EventArgs e)
        {
			Hide();
        }

		private void PayByCashButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.Cash;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
        }

		private void PayByMoneyTransferButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.MoneyTransfer;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
        }

		private void PayBy5050Button_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.FiftyFifty;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
        }

		private void PayByWeWinButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.WeWin;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
        }

		private void PayByWelfareCardButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.WelfareCard;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
        }

		private void PayByWeLoveButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.M33WeLove;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
        }

		private void PayByArButton_Click(object sender, EventArgs e)
		{
			_selectedPaymentType = PaymentType.AccountReceivable;
			_isPaymentTypeSelected = true;

			PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];
		}

		private void Add20Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue = string.Empty;

            var value = 20m;

            _values.Add(value);
            _amount = _values.Sum();

            DisplayValue(_amount);
        }

		private void Add50Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue = string.Empty;

            var value = 50m;

            _values.Add(value);
            _amount = _values.Sum();

            DisplayValue(_amount);
        }

		private void Add100Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue = string.Empty;

            var value = 100m;

            _values.Add(value);
            _amount = _values.Sum();

            DisplayValue(_amount);
        }

		private void Add500Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue = string.Empty;

            var value = 500m;

            _values.Add(value);
            _amount = _values.Sum();

            DisplayValue(_amount);
        }

		private void Add1000Button_Click(object sender, EventArgs e)
		{
            _pendingStringValue = string.Empty;

            var value = 1000m;

            _values.Add(value);
            _amount = _values.Sum();

            DisplayValue(_amount);
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
            DisplayValue(value.ToString("0.00"));
        }

        private void DisplayValue(string value)
        {
            PaymentAmountLabel.Text = value;
        }
	}
}
