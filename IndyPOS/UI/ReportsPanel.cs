using IndyPOS.UI.Reports;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using IndyPOS.Common.Enums;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
    public partial class ReportsPanel : UserControl
	{
		private readonly SalesReportPanel _salesReportPanel;
		private readonly InvoiceProductsReportPanel _invoiceProductsReportPanel;
		private readonly SalesHistoryReportPanel _salesHistoryReportPanel;
		private UserControl _activePanel;

        public ReportsPanel(SalesReportPanel salesReportPanel,
							InvoiceProductsReportPanel invoiceProductsReportPanel,
							SalesHistoryReportPanel salesHistoryReportPanel)
		{
			_salesReportPanel = salesReportPanel;
			_salesReportPanel.Visible = false;
			_invoiceProductsReportPanel = invoiceProductsReportPanel;
			_invoiceProductsReportPanel.Visible = false;
			_salesHistoryReportPanel = salesHistoryReportPanel;
			_salesHistoryReportPanel.Visible = false;

            InitializeComponent();
		}

		private void SwitchToPanel(ReportSubPanel subPanelToShow)
		{
			UserControl panelToShow = _salesReportPanel;

			switch (subPanelToShow)
			{
				case ReportSubPanel.SalesReport:

					panelToShow = _salesReportPanel;

					break;

				case ReportSubPanel.InvoiceProductsReport:

					panelToShow = _invoiceProductsReportPanel;

					break;

				case ReportSubPanel.SalesHistoryReport:

					panelToShow = _salesHistoryReportPanel;

					break;
			}

			if (_activePanel != null)
			{
				if (_activePanel.Name == panelToShow.Name)
				{
					return;
				}

				_activePanel.Visible = false;

				ActivePanel.Controls.Clear();
			}

			panelToShow.Dock = DockStyle.Fill;

			ActivePanel.Controls.Add(panelToShow);
            
			panelToShow.BringToFront();
			panelToShow.Visible = true;
            
			_activePanel = panelToShow;
		}

        private void ActivePanel_VisibleChanged(object sender, EventArgs e)
		{
			if (!Visible)
				return;

			SwitchToPanel(ReportSubPanel.SalesReport);
		}

        private void ShowSalesOverviewReportButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(ReportSubPanel.SalesReport);
        }

        private void ShowInvoiceProductsButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(ReportSubPanel.InvoiceProductsReport);
        }

        private void ShowSalesHistoryButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(ReportSubPanel.SalesHistoryReport);
        }
    }
}
