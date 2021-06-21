using IndyPOS.Adapters;
using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Inventory;
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
        private readonly IInventoryController _inventoryController;
        private IReadOnlyDictionary<int, string> _productCategoryDictionary;

        public AcceptPaymentForm(IEventAggregator eventAggregator, 
            IStoreConstants storeConstants, 
            IInventoryController inventoryController)
        {
            _eventAggregator = eventAggregator;
            _storeConstants = storeConstants;
            _inventoryController = inventoryController;
            _productCategoryDictionary = _storeConstants.ProductCategories;

            InitializeComponent();
        }

        public new void ShowDialog()
        {
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
    }
}
