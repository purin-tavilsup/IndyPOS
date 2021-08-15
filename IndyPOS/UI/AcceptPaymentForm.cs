using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Enums;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	public partial class AcceptPaymentForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IStoreConstants _storeConstants;
        private readonly ISaleInvoiceController _saleInvoiceController;
        private readonly IList<decimal> _values;
        private IReadOnlyDictionary<int, string> _paymentTypeDictionary;
        private PaymentType _selectedPaymentType;
        private bool _isPaymentTypeSelected;
        private decimal _amount;
        private string _pendingStringValue;
        

        public AcceptPaymentForm(IEventAggregator eventAggregator, 
            IStoreConstants storeConstants,
            ISaleInvoiceController saleInvoiceController)
        {
            _eventAggregator = eventAggregator;
            _storeConstants = storeConstants;
            _saleInvoiceController = saleInvoiceController;
            _paymentTypeDictionary = _storeConstants.PaymentTypes;

            InitializeComponent();

            _values = new List<decimal>();
        }

        private void ResetPaymentTypeSelection()
		{
            // Default to cash
            _selectedPaymentType = PaymentType.Cash;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];

            PayByCashButton.Image = Properties.Resources.Money_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_Gray_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_Gray_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_Gray_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_Gray_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_Gray_100;
        }

        public new void ShowDialog()
        {
            _amount = 0m;
            _pendingStringValue = string.Empty;
            _values.Clear();

            ResetPaymentTypeSelection();
            CancelAcceptPaymentButton.Select();

            base.ShowDialog();
        }

        private void SaveProductEntryButton_Click(object sender, EventArgs e)
        {
            if (!_isPaymentTypeSelected)
			{
                MessageBox.Show("กรุณาเลือกวิธีการชำระเงิน", "วิธีการชำระเงิน");
                return;
			}
                
            _saleInvoiceController.AddPayment(_selectedPaymentType, _amount);

            Close();
        }

        private void CancelAcceptPaymentButton_Click(object sender, EventArgs e)
        {
            Close();
        }

		private void PayByCashButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.Cash;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];

            PayByCashButton.Image = Properties.Resources.Money_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_Gray_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_Gray_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_Gray_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_Gray_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_Gray_100;
        }

		private void PayByMoneyTransferButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.MoneyTransfer;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];

            PayByCashButton.Image = Properties.Resources.Money_Gray_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_Gray_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_Gray_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_Gray_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_Gray_100;
        }

		private void PayBy5050Button_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.FiftyFifty;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];

            PayByCashButton.Image = Properties.Resources.Money_Gray_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_Gray_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_Gray_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_Gray_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_Gray_100;
        }

		private void PayByWeWinButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.WeWin;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];

            PayByCashButton.Image = Properties.Resources.Money_Gray_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_Gray_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_Gray_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_Gray_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_Gray_100;
        }

		private void PayByWelfareCardButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.WelfareCard;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];

            PayByCashButton.Image = Properties.Resources.Money_Gray_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_Gray_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_Gray_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_Gray_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_Gray_100;
        }

		private void PayByWeLoveButton_Click(object sender, EventArgs e)
		{
            _selectedPaymentType = PaymentType.M33WeLove;
            _isPaymentTypeSelected = true;

            PaymentTypeLabel.Text = _paymentTypeDictionary[(int)_selectedPaymentType];

            PayByCashButton.Image = Properties.Resources.Money_Gray_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_Gray_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_Gray_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_Gray_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_Gray_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_100;
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

            var decimalPoint = string.IsNullOrEmpty(_pendingStringValue) ? "0." : ".";

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
            if (string.IsNullOrEmpty(_pendingStringValue))
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
