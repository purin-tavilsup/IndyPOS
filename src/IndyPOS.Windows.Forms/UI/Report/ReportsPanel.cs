using System.Diagnostics.CodeAnalysis;
using IndyPOS.Windows.Forms.Enums;

namespace IndyPOS.Windows.Forms.UI.Report;

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

		_activePanel = new UserControl(); 

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

		if (_activePanel.Name == panelToShow.Name)
		{
			return;
		}

		_activePanel.Visible = false;

		ActivePanel.Controls.Clear();

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