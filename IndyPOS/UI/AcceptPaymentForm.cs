using IndyPOS.Adapters;
using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Inventory;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using IndyPOS.Enums;

namespace IndyPOS.UI
{
	public partial class AcceptPaymentForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IStoreConstants _storeConstants;
        private readonly IInventoryController _inventoryController;
        private IReadOnlyDictionary<int, string> _paymentTypeDictionary;
        private PaymentType _selectedPaymentType;
        private bool _isPaymentTypeSelected;

        public AcceptPaymentForm(IEventAggregator eventAggregator, 
            IStoreConstants storeConstants, 
            IInventoryController inventoryController)
        {
            _eventAggregator = eventAggregator;
            _storeConstants = storeConstants;
            _inventoryController = inventoryController;
            _paymentTypeDictionary = _storeConstants.PaymentTypes;

            InitializeComponent();
        }

        private void ResetPaymentTypeSelection()
		{
            PaymentTypeLabel.Text = string.Empty;
            _isPaymentTypeSelected = false;

            // Default to cash
            _selectedPaymentType = PaymentType.Cash;

            PayByCashButton.Image = Properties.Resources.Money_80;
            PayByMoneyTransferButton.Image = Properties.Resources.Payment_MoneyTransfer_100;
            PayBy5050Button.Image = Properties.Resources.Payment_KLK_100;
            PayByWeWinButton.Image = Properties.Resources.Payment_WeWin_100;
            PayByWelfareCardButton.Image = Properties.Resources.Payment_PracharatCard_100;
            PayByWeLoveButton.Image = Properties.Resources.Payment_WeLove_100;
        }

        public new void ShowDialog()
        {
            ResetPaymentTypeSelection();
            CancelAcceptPaymentButton.Select();

            base.ShowDialog();
        }

        private void SaveProductEntryButton_Click(object sender, EventArgs e)
        {
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
	}
}
