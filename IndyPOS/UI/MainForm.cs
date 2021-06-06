using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Prism.Events;
using IndyPOS.DataServices;

namespace IndyPOS
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

        private readonly IEventAggregator _eventAggregator;
        private readonly IInvoicesDataService _invoicesDataService;
        private readonly IInventoryProductsDataService _inventoryProductsDataService;

        private UserControl _salesPanel;
        private UserControl _inventoryPanel;
        private UserControl _usersPanel;
        private UserControl _reportsPanel;
        private UserControl _customerAccountsPanel;
        private UserControl _settingsPanel;
        private UserControl _activePanel;


        public MainForm(IEventAggregator eventAggregator, IInvoicesDataService invoicesDataService, IInventoryProductsDataService inventoryProductsDataService)
        {
            _eventAggregator = eventAggregator;
            _invoicesDataService = invoicesDataService;
            _inventoryProductsDataService = inventoryProductsDataService;

            InitializeComponent();

            CreateUI();
        }

        private void CreateUI()
        {
            // TODO: this should be handled by Dependency Injection
            _salesPanel = new SalePanel(_eventAggregator, _invoicesDataService, _inventoryProductsDataService);
            _inventoryPanel = new InventoryPanel(_eventAggregator, _inventoryProductsDataService);
            _usersPanel = new UsersPanel();
            _reportsPanel = new ReportsPanel();
            _customerAccountsPanel = new CustomerAccountsPanel();
            _settingsPanel = new SettingsPanel();
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

            // Set origin location for the Panel to be shown
            //panelToBeShown.Location = _panelLocation;

            // Set Dock Style for the Panel
            panelToBeShown.Dock = DockStyle.Fill;

            // Add the Panel to Controls of MainForm
            ActivePanel.Controls.Add(panelToBeShown);

            // Bring the Panel to front
            panelToBeShown.BringToFront();

            // Make the Panel visible
            panelToBeShown.Visible = true;

            // The Panel becomes Active Panel
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
