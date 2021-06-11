using System;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    public partial class MainForm : Form
    {
        public enum Panels
        {
            Sales = 0,
            Inventory,
            Users,
            Reports,
            CustomerAccounts,
            Settings
        }

        private SalePanel _salesPanel;
        private InventoryPanel _inventoryPanel;
        private UsersPanel _usersPanel;
        private ReportsPanel _reportsPanel;
        private CustomerAccountsPanel _customerAccountsPanel;
        private SettingsPanel _settingsPanel;
        private UserControl _activePanel;

        public MainForm(SalePanel salesPanel, 
            InventoryPanel inventoryPanel, 
            UsersPanel usersPanel, 
            ReportsPanel reportsPanel, 
            CustomerAccountsPanel customerAccountsPanel, 
            SettingsPanel settingsPanel)
        {
            InitializeComponent();

            _salesPanel = salesPanel;
            _inventoryPanel = inventoryPanel;
            _usersPanel = usersPanel;
            _reportsPanel = reportsPanel;
            _customerAccountsPanel = customerAccountsPanel;
            _settingsPanel = settingsPanel;

            SaleButton.Select();
        }

        private void SwitchToPanel(Panels panelName)
        {
            UserControl panelToBeShown = null;

            switch (panelName)
            {
                case Panels.Sales:

                    panelToBeShown = _salesPanel;

                    break;

                case Panels.Inventory:

                    panelToBeShown = _inventoryPanel;

                    break;

                case Panels.Users:

                    panelToBeShown = _usersPanel;

                    break;

                case Panels.Reports:

                    panelToBeShown = _reportsPanel;

                    break;

                case Panels.CustomerAccounts:

                    panelToBeShown = _customerAccountsPanel;

                    break;

                case Panels.Settings:

                    panelToBeShown = _settingsPanel;

                    break;
            }

            if (_activePanel != null)
            {
                if (_activePanel.Name == panelToBeShown.Name)
                {
                    return;
                }

                _activePanel.Visible = false;

                ActivePanel.Controls.Remove(_activePanel);
            }

            panelToBeShown.Dock = DockStyle.Fill;

            ActivePanel.Controls.Add(panelToBeShown);
            
            panelToBeShown.BringToFront();
            panelToBeShown.Visible = true;
            
            _activePanel = panelToBeShown;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            SwitchToPanel(Panels.Sales);
        }

        private void SaleButton_Click(object sender, EventArgs e)
        {
            SwitchToPanel(Panels.Sales);
        }

        private void InventoryButton_Click(object sender, EventArgs e)
        {
            SwitchToPanel(Panels.Inventory);
        }

        private void UsersButton_Click(object sender, EventArgs e)
        {
            SwitchToPanel(Panels.Users);
        }

        private void ReportsButton_Click(object sender, EventArgs e)
        {
            SwitchToPanel(Panels.Reports);
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            SwitchToPanel(Panels.Settings);
        }
    }
}
