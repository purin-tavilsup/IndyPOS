using IndyPOS.Enums;
using IndyPOS.Events;
using Prism.Events;
using System;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	public partial class MainForm : Form
    {
        private readonly SalePanel _salesPanel;
        private readonly InventoryPanel _inventoryPanel;
        private readonly UsersPanel _usersPanel;
        private readonly ReportsPanel _reportsPanel;
        private readonly CustomerAccountsPanel _customerAccountsPanel;
        private readonly SettingsPanel _settingsPanel;
        private readonly IEventAggregator _eventAggregator;
        private UserControl _activePanel;

        public MainForm(SalePanel salesPanel, 
                        InventoryPanel inventoryPanel, 
                        UsersPanel usersPanel, 
                        ReportsPanel reportsPanel, 
                        CustomerAccountsPanel customerAccountsPanel, 
                        SettingsPanel settingsPanel,
                        IEventAggregator eventAggregator)
		{
            InitializeComponent();

            _salesPanel = salesPanel;
            _salesPanel.Visible = false;
            _inventoryPanel = inventoryPanel;
            _inventoryPanel.Visible = false;
            _usersPanel = usersPanel;
            _usersPanel.Visible = false;
            _reportsPanel = reportsPanel;
            _reportsPanel.Visible = false;
            _customerAccountsPanel = customerAccountsPanel;
            _customerAccountsPanel.Visible = false;
            _settingsPanel = settingsPanel;
            _settingsPanel.Visible = false;
            _eventAggregator = eventAggregator;

            SaleButton.Select();
        }

        private void SwitchToPanel(Subpanel subpanelToShow)
        {
            UserControl panelToShow = null;

            switch (subpanelToShow)
            {
                case Subpanel.Sales:

                    panelToShow = _salesPanel;

                    break;

                case Subpanel.Inventory:

                    panelToShow = _inventoryPanel;

                    break;

                case Subpanel.Users:

                    panelToShow = _usersPanel;

                    break;

                case Subpanel.Reports:

                    panelToShow = _reportsPanel;

                    break;

                case Subpanel.CustomerAccounts:

                    panelToShow = _customerAccountsPanel;

                    break;

                case Subpanel.Settings:

                    panelToShow = _settingsPanel;

                    break;
            }

            if (_activePanel != null)
            {
                if (_activePanel.Name == panelToShow.Name)
                {
                    return;
                }

                _activePanel.Visible = false;

                ActivePanel.Controls.Remove(_activePanel);
            }

            panelToShow.Dock = DockStyle.Fill;

            ActivePanel.Controls.Add(panelToShow);
            
            panelToShow.BringToFront();
            panelToShow.Visible = true;
            
            _activePanel = panelToShow;

            _eventAggregator.GetEvent<ActiveSubpanelChangedEvent>().Publish(subpanelToShow);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
			SwitchToPanel(Subpanel.Sales);
        }

        private void SaleButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(Subpanel.Sales);
        }

        private void InventoryButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(Subpanel.Inventory);
        }

        private void UsersButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(Subpanel.Users);
        }

        private void ReportsButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(Subpanel.Reports);
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
			SwitchToPanel(Subpanel.Settings);
        }

		private void CloseButton_Click(object sender, EventArgs e)
		{
            Close();
		}

		private void MinimizeWindows_Click(object sender, EventArgs e)
		{
            WindowState = FormWindowState.Minimized;
		}

		private void ResizeWindows_Click(object sender, EventArgs e)
		{
            var currentState = WindowState;

            switch(currentState)
			{
                case FormWindowState.Maximized:
                    WindowState = FormWindowState.Normal;
                    ResizeWindows.Image = Properties.Resources.maximize_window_24px;
                    break;
                case FormWindowState.Normal:
                    WindowState = FormWindowState.Maximized;
                    ResizeWindows.Image = Properties.Resources.restore_window_24px;
                    break;
			}
        }

		private void CloseWindows_Click(object sender, EventArgs e)
		{
            Close();
        }

		private void CustomerAccountsButton_Click(object sender, EventArgs e)
		{
            SwitchToPanel(Subpanel.CustomerAccounts);
		}
	}
}
