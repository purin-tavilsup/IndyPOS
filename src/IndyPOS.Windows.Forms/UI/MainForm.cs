#nullable enable
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Windows.Forms.Enums;
using IndyPOS.Windows.Forms.Events;
using IndyPOS.Windows.Forms.UI.Inventory;
using IndyPOS.Windows.Forms.UI.Login;
using IndyPOS.Windows.Forms.UI.PayLater;
using IndyPOS.Windows.Forms.UI.Report;
using IndyPOS.Windows.Forms.UI.Sale;
using IndyPOS.Windows.Forms.UI.Setting;
using IndyPOS.Windows.Forms.UI.User;
using Microsoft.Extensions.Configuration;
using Prism.Events;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Timer = System.Windows.Forms.Timer;

namespace IndyPOS.Windows.Forms.UI
{
	[ExcludeFromCodeCoverage]
    public partial class MainForm : Form
    {
        private readonly SalePanel _salesPanel;
        private readonly InventoryPanel _inventoryPanel;
        private readonly UsersPanel _usersPanel;
        private readonly ReportsPanel _reportsPanel;
        private readonly PayLaterPaymentPanel _accountsReceivablePanel;
        private readonly SettingsPanel _settingsPanel;
		private readonly UserLogInPanel _userLogInPanel;
        private readonly IEventAggregator _eventAggregator;
		private readonly IDbConnectionProvider _dbConnectionProvider;
		private readonly bool _isDatabaseBackupEnabled;
		private readonly string _backupDatabaseDirectory;

        private UserControl _activePanel;
		private bool _isUserLoggedIn;
		private ILoggedInUser? _loggedInUser;

		public MainForm(SalePanel salesPanel, 
						InventoryPanel inventoryPanel, 
						UsersPanel usersPanel, 
						ReportsPanel reportsPanel, 
						PayLaterPaymentPanel accountsReceivablePanel, 
						SettingsPanel settingsPanel,
						UserLogInPanel userLogInPanel,
						IEventAggregator eventAggregator,
						IDbConnectionProvider dbConnectionProvider,
						IConfiguration configuration)
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
			_accountsReceivablePanel = accountsReceivablePanel;
			_accountsReceivablePanel.Visible = false;
			_settingsPanel = settingsPanel;
			_settingsPanel.Visible = false;
			_userLogInPanel = userLogInPanel;
			_userLogInPanel.Visible = false;
			_eventAggregator = eventAggregator;
			_isUserLoggedIn = false;
			_dbConnectionProvider = dbConnectionProvider;
			_activePanel = new UserControl();

			_isDatabaseBackupEnabled = configuration.GetValue<bool>("Database:BackupEnabled");
			_backupDatabaseDirectory = GetBackupDatabaseDirectory(configuration);

			SubscribeEvents();
			
			var dateTimeUpdateTimer = new Timer();
			dateTimeUpdateTimer.Tick += DateTimeUpdateTimer_Tick;
			dateTimeUpdateTimer.Interval = 500;
			dateTimeUpdateTimer.Enabled = true;

            LogInButton.Select();
		}

		public void SetStoreName(string storeName)
		{
			StoreNameLabel.Text = storeName;
		}

        public void SetVersion(string version)
		{
			VersionLabel.Text = $"Version: {version}";
		}

		private static string GetBackupDatabaseDirectory(IConfiguration configuration)
		{
			var path = configuration.GetValue<string>("Database:BackupDirectory");

			return path ?? "C:\\ProgramData\\IndyPOS\\Reports";
		}

        private void DateTimeUpdateTimer_Tick(object? sender, EventArgs e)
		{
			var dateTime = DateTime.Now.ToString("dddd, dd MMMM yyyy hh:mm tt");

			DateTimeLabel.Text = dateTime;
		}

        private void SubscribeEvents()
		{
			_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
			_eventAggregator.GetEvent<UserLoggedOutEvent>().Subscribe(OnUserLoggedOut);
			_eventAggregator.GetEvent<SalesReportPushedEvent>().Subscribe(OnDataFeedReportPushed);
		}

        private void SwitchToPanel(SubPanel subPanelToShow)
        {
            UserControl panelToShow = _userLogInPanel;

            switch (subPanelToShow)
            {
                case SubPanel.Sales:

                    panelToShow = _salesPanel;

                    break;

                case SubPanel.Inventory:

                    panelToShow = _inventoryPanel;

                    break;

                case SubPanel.Users:

                    panelToShow = _usersPanel;

                    break;

                case SubPanel.Reports:

                    panelToShow = _reportsPanel;

                    break;

                case SubPanel.AccountsReceivable:

                    panelToShow = _accountsReceivablePanel;

                    break;

                case SubPanel.Settings:

                    panelToShow = _settingsPanel;

                    break;

                case SubPanel.UserLogIn:

                    panelToShow = _userLogInPanel;

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

            _eventAggregator.GetEvent<ActiveSubPanelChangedEvent>().Publish(subPanelToShow);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            SwitchToPanel(SubPanel.UserLogIn);
        }

        private void SaleButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return;

			SwitchToPanel(SubPanel.Sales);
		}

        private void InventoryButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return;
			
			SwitchToPanel(SubPanel.Inventory);
        }

        private void UsersButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return;

			SwitchToPanel(SubPanel.Users);
        }

        private void ReportsButton_Click(object sender, EventArgs e)
        {
			if (!_isUserLoggedIn)
				return; 
			
			SwitchToPanel(SubPanel.Reports);
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        { 
			if (!_isUserLoggedIn || _loggedInUser?.RoleId == (int) UserRole.Cashier)
				return;
			
			SwitchToPanel(SubPanel.Settings);
        }

		private void AccountsReceivableButton_Click(object sender, EventArgs e)
		{
			if (!_isUserLoggedIn)
				return;

			SwitchToPanel(SubPanel.AccountsReceivable);
		}

		private void LogInButton_Click(object sender, EventArgs e)
		{
			SwitchToPanel(SubPanel.UserLogIn);
		}

		private void CloseApplicationButton_Click(object sender, EventArgs e)
		{
			CloseApplication();
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
			CloseApplication();
		}

		private void CloseApplication()
        {
			BackupDatabase();

			Close();
        }

		[Conditional("RELEASE")]
		private void BackupDatabase()
		{
			if (_isDatabaseBackupEnabled.IsFalse())
				return;

			var today = DateTime.Today;
			var byDateBackupDirectory = $"{_backupDatabaseDirectory}\\{today.Year}\\{today.Month:00}\\{today.Day:00}";
			
			if (!Directory.Exists(byDateBackupDirectory)) 
				Directory.CreateDirectory(byDateBackupDirectory);

			_dbConnectionProvider.BackupDatabase(byDateBackupDirectory);
			_dbConnectionProvider.BackupDatabase(_backupDatabaseDirectory);
        }
        
		private void MainForm_Load(object sender, EventArgs e)
		{
            WindowState = FormWindowState.Maximized;
			ResizeWindowsButton.Image = Properties.Resources.restore_window_24px;
        }

        private void OnUserLoggedIn(ILoggedInUser loggedInUser)
		{
			_loggedInUser = loggedInUser;

			LoggedInUserLabel.Text = $"User: {_loggedInUser.FirstName} {_loggedInUser.LastName}";

			_isUserLoggedIn = true;

			LogInButton.Text = "Log Out";

			SwitchToPanel(SubPanel.Sales);
        }

        private void OnUserLoggedOut()
		{
			_loggedInUser = null;

			LoggedInUserLabel.Text = "User:";

			_isUserLoggedIn = false;

			LogInButton.Text = "Log In";
		}

		private void OnDataFeedReportPushed(string status)
		{
			DataFeedStatusLabel.Text = status;
		}
	}
}
