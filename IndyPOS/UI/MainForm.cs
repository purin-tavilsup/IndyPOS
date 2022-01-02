using IndyPOS.Enums;
using IndyPOS.Events;
using IndyPOS.Users;
using Prism.Events;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
    public partial class MainForm : Form
    {
        private readonly SalePanel _salesPanel;
        private readonly InventoryPanel _inventoryPanel;
        private readonly UsersPanel _usersPanel;
        private readonly ReportsPanel _reportsPanel;
        private readonly CustomerAccountsPanel _customerAccountsPanel;
        private readonly SettingsPanel _settingsPanel;
		private readonly UserLogInPanel _userLogInPanel;
        private readonly IEventAggregator _eventAggregator;
        private UserControl _activePanel;
		private bool _isUserLoggedIn;
		private IUser _loggedInUser;
		private Timer _dateTimeUpdateTimer;

        public MainForm(SalePanel salesPanel, 
                        InventoryPanel inventoryPanel, 
                        UsersPanel usersPanel, 
                        ReportsPanel reportsPanel, 
                        CustomerAccountsPanel customerAccountsPanel, 
                        SettingsPanel settingsPanel,
						UserLogInPanel userLogInPanel,
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
			_userLogInPanel = userLogInPanel;
			_userLogInPanel.Visible = false;
            _eventAggregator = eventAggregator;
			_isUserLoggedIn = false;

			SubscribeEvents();
			CreateDateTimeUpdateTimer();

            LogInButton.Select();
		}

        private void CreateDateTimeUpdateTimer()
        {
			_dateTimeUpdateTimer = new Timer();
			_dateTimeUpdateTimer.Interval = 500;
			_dateTimeUpdateTimer.Tick += DateTimeUpdateTimer_Tick;
			_dateTimeUpdateTimer.Enabled = true;
        }

        private void DateTimeUpdateTimer_Tick(object sender, EventArgs e)
		{
			var dateTime = DateTime.Now.ToString("dddd, dd MMMM yyyy hh:mm tt");

			DateTimeLabel.Text = dateTime;
		}

        private void SubscribeEvents()
		{
			_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
			_eventAggregator.GetEvent<UserLoggedOutEvent>().Subscribe(OnUserLoggedOut);
		}

        private void SwitchToPanel(Subpanel subPanelToShow)
        {
            UserControl panelToShow = _userLogInPanel;

            switch (subPanelToShow)
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

                case Subpanel.UserLogIn:

                    panelToShow = _userLogInPanel;

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

            _eventAggregator.GetEvent<ActiveSubPanelChangedEvent>().Publish(subPanelToShow);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            SwitchToPanel(Subpanel.UserLogIn);
        }

        private void SaleButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return;

			SwitchToPanel(Subpanel.Sales);
		}

        private void InventoryButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return;
			
			SwitchToPanel(Subpanel.Inventory);
        }

        private void UsersButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return;

			SwitchToPanel(Subpanel.Users);
        }

        private void ReportsButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return; 
			
			SwitchToPanel(Subpanel.Reports);
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        { 
			if (!_isUserLoggedIn)
				return;
			
			SwitchToPanel(Subpanel.Settings);
        }

		private void CustomerAccountsButton_Click(object sender, EventArgs e)
		{
			if (!_isUserLoggedIn)
				return;

			SwitchToPanel(Subpanel.CustomerAccounts);
		}

		private void LogInButton_Click(object sender, EventArgs e)
		{
			SwitchToPanel(Subpanel.UserLogIn);
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
					ResizeWindowsButton.Image = Properties.Resources.maximize_window_24px;
                    break;
                case FormWindowState.Normal:
                    WindowState = FormWindowState.Maximized;
					ResizeWindowsButton.Image = Properties.Resources.restore_window_24px;
                    break;
			}
        }

		private void CloseWindows_Click(object sender, EventArgs e)
		{
            Close();
        }
        
		private void MainForm_Load(object sender, EventArgs e)
		{
            WindowState = FormWindowState.Maximized;
			ResizeWindowsButton.Image = Properties.Resources.restore_window_24px;
        }

        private void OnUserLoggedIn(IUser loggedInUser)
		{
			_loggedInUser = loggedInUser;

			LoggedInUserLabel.Text = $"User: {_loggedInUser.FirstName} {_loggedInUser.LastName}";

			_isUserLoggedIn = true;

			SwitchToPanel(Subpanel.Sales);
        }

        private void OnUserLoggedOut()
		{
			_loggedInUser = null;

			LoggedInUserLabel.Text = "User:";

			_isUserLoggedIn = false;
		}
	}
}
