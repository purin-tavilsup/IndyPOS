using Prism.Events;
using System;
using System.Windows.Forms;
using IndyPOS.Facade.Events;

namespace IndyPOS.UI
{
    public partial class AddGeneralGoodsProductForm : Form
    {
		private readonly IEventAggregator _eventAggregator;

        public AddGeneralGoodsProductForm(IEventAggregator eventAggregator)
		{
			_eventAggregator = eventAggregator;

            InitializeComponent();
        }

        private void CancelProductEntryButton_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void AddNonTrackableProductButton_Click(object sender, EventArgs e)
        {
            _eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish("NONTRACTABLE1");

            Hide();
        }
    }
}
