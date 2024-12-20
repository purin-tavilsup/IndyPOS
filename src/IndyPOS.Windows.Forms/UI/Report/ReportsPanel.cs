﻿using IndyPOS.Windows.Forms.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Report;

[ExcludeFromCodeCoverage]
public partial class ReportsPanel : UserControl
{
    private readonly SalesReportPanel _salesReportPanel;
    private readonly InvoiceProductsReportPanel _invoiceProductsReportPanel;
    private readonly SalesHistoryReportPanel _salesHistoryReportPanel;
    private readonly PayLaterPaymentsReportPanel _payLaterPaymentsReportPanel;
    private readonly CashFlowCalculatorPanel _cashFlowCalculatorPanel;
    private UserControl _activePanel;

    public ReportsPanel(SalesReportPanel salesReportPanel,
                        InvoiceProductsReportPanel invoiceProductsReportPanel,
                        SalesHistoryReportPanel salesHistoryReportPanel,
                        PayLaterPaymentsReportPanel payLaterPaymentsReportPanel,
                        CashFlowCalculatorPanel cashFlowCalculatorPanel)
    {
        _salesReportPanel = salesReportPanel;
        _salesReportPanel.Visible = false;
        _invoiceProductsReportPanel = invoiceProductsReportPanel;
        _invoiceProductsReportPanel.Visible = false;
        _salesHistoryReportPanel = salesHistoryReportPanel;
        _salesHistoryReportPanel.Visible = false;
        _payLaterPaymentsReportPanel = payLaterPaymentsReportPanel;
        _cashFlowCalculatorPanel = cashFlowCalculatorPanel;
        _payLaterPaymentsReportPanel.Visible = false;

        _activePanel = new UserControl();

        InitializeComponent();
    }

    private void SwitchToPanel(ReportSubPanel subPanelToShow)
    {
        UserControl panelToShow = subPanelToShow switch
        {
            ReportSubPanel.SalesReport => _salesReportPanel,
            ReportSubPanel.InvoiceProductsReport => _invoiceProductsReportPanel,
            ReportSubPanel.SalesHistoryReport => _salesHistoryReportPanel,
            ReportSubPanel.PayLaterPaymentsReport => _payLaterPaymentsReportPanel,
            ReportSubPanel.CashFlowCalculator => _cashFlowCalculatorPanel,
            _ => _salesReportPanel
        };

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

    private void PayLaterPaymentsReportButton_Click(object sender, EventArgs e)
    {
        SwitchToPanel(ReportSubPanel.PayLaterPaymentsReport);
    }

    private void CashFlowCalculatorButton_Click(object sender, EventArgs e)
    {
		SwitchToPanel(ReportSubPanel.CashFlowCalculator);
    }
}