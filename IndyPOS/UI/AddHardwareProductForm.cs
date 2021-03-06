using IndyPOS.Events;
using Prism.Events;
using System;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    public partial class AddHardwareProductForm : Form
    {
		private readonly IEventAggregator _eventAggregator;

        public AddHardwareProductForm(IEventAggregator eventAggregator)
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
			_eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish("NONTRACTABLE2");

            Hide();
        }
    }
}
