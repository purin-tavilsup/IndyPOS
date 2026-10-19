using IndyPOS.Windows.Forms.UI.CashDrawer;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Events;
using IndyPOS.Windows.Forms.Enums;
using IndyPOS.Windows.Forms.Events;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI.Inventory;
using IndyPOS.Windows.Forms.UI.Login;
using IndyPOS.Windows.Forms.UI.PayLater;
using IndyPOS.Windows.Forms.UI.Report;
using IndyPOS.Windows.Forms.UI.Sale;
using IndyPOS.Windows.Forms.UI.Setting;
using IndyPOS.Windows.Forms.UI.User;
using Prism.Events;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using Timer = System.Windows.Forms.Timer;

namespace IndyPOS.Windows.Forms.UI;

[ExcludeFromCodeCoverage]
public partial class MainForm : Form
{
	private readonly SalePanel _salesPanel;
	private readonly InventoryPanel _inventoryPanel;
	private readonly UsersPanel _usersPanel;
	private readonly ReportsPanel _reportsPanel;
	private readonly CashDrawerPanel _cashDrawerPanel;
	private readonly PayLaterPaymentPanel _accountsReceivablePanel;
	private readonly SettingsPanel _settingsPanel;
	private readonly UserLogInPanel _userLogInPanel;
	private readonly IEventAggregator _eventAggregator;
	private readonly IStoreFeaturesProvider _storeFeatures;

	// The menu buttons' tops as laid out, after any display scaling, so stacking never uses design pixels.
	// Recorded at the first login: the form is shown and scaled by then, and no button has moved yet.

	private UserControl _activePanel;
	private bool _isUserLoggedIn;
	private ILoggedInUser? _loggedInUser;

	public MainForm(SalePanel salesPanel,
					InventoryPanel inventoryPanel,
					UsersPanel usersPanel,
					ReportsPanel reportsPanel,
					CashDrawerPanel cashDrawerPanel,
					PayLaterPaymentPanel accountsReceivablePanel,
					SettingsPanel settingsPanel,
					UserLogInPanel userLogInPanel,
					IEventAggregator eventAggregator,
					IStoreFeaturesProvider storeFeatures)
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
		_cashDrawerPanel = cashDrawerPanel;
		_cashDrawerPanel.Visible = false;
		_accountsReceivablePanel = accountsReceivablePanel;
		_accountsReceivablePanel.Visible = false;
		_settingsPanel = settingsPanel;
		_settingsPanel.Visible = false;
		_userLogInPanel = userLogInPanel;
		_userLogInPanel.Visible = false;
		_eventAggregator = eventAggregator;
		_storeFeatures = storeFeatures;
		_isUserLoggedIn = false;
		_activePanel = new UserControl();

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

			case SubPanel.CashDrawer:

				panelToShow = _cashDrawerPanel;

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
		if (!_isUserLoggedIn || !CanViewReports())
			return; 
			
		SwitchToPanel(SubPanel.Reports);
	}

	private void CashDrawerButton_Click(object sender, EventArgs e)
	{
		if (!_isUserLoggedIn || !CanManageCash())
			return;

		SwitchToPanel(SubPanel.CashDrawer);
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
		Close();
	}
        
	private void MainForm_Load(object sender, EventArgs e)
	{
		WindowState = FormWindowState.Maximized;
		ResizeWindowsButton.Image = Properties.Resources.restore_window_24px;
	}

	private async void OnUserLoggedIn(ILoggedInUser loggedInUser)
	{
		_loggedInUser = loggedInUser;

		LoggedInUserLabel.Text = $"User: {_loggedInUser.FirstName} {_loggedInUser.LastName}";

		_isUserLoggedIn = true;

		LogInButton.Text = "Log Out";

		SwitchToPanel(SubPanel.Sales);

		await ApplyStoreLayoutAsync();
	}

	private async Task ApplyStoreLayoutAsync()
	{
		// Hidden while the features load, so a store without PayLater never shows the ledger, even briefly.
		ApplyMenu(showAccountsReceivable: false, CanViewReports(), CanManageCash());

		ApplyMenu((await LoadTillLayoutAsync()).ShowAccountsReceivableMenu, CanViewReports(), CanManageCash());
	}

	private async Task<TillLayout> LoadTillLayoutAsync()
	{
		try
		{
			return TillLayout.For(await _storeFeatures.GetAsync());
		}
		catch (Exception ex)
		{
			// The sale panel already tells the cashier that the store settings did not load.
			Log.Warning(ex, "Could not load store features for the menu");
			return TillLayout.WhenFeaturesUnavailable;
		}
	}

	private void ApplyMenu(bool showAccountsReceivable, bool showReports, bool showCashDrawer)
	{
		AccountsReceivableButton.Visible = showAccountsReceivable;
		ReportsButton.Visible = showReports;
		CashDrawerButton.Visible = showCashDrawer;

		// Nine equal slots from the laid-out panel, so display scaling is already in them (Pond, 2026-10-19:
		// shorter buttons are fine while each icon and caption still fits; MenuLayoutTests checks that).
		var (tops, height) = MenuLayout.EvenSlots(ControlPanel.ClientSize.Height, AllMenuButtons().Length, MenuGap);
		foreach (var button in AllMenuButtons())
			button.Height = height;

		MenuLayout.Stack(ShownMenuButtons(showAccountsReceivable, showReports, showCashDrawer), tops);
	}

	private const int MenuGap = 6;

	private Control[] AllMenuButtons() =>
		[SaleButton, CashDrawerButton, InventoryButton, UsersButton, ReportsButton, AccountsReceivableButton,
		 SettingsButton, LogInButton, CloseApplicationButton];

	// Cashiers, managers and admins all run the drawer; the gate keeps any other role out (spec §6).
	private bool CanManageCash() =>
		_loggedInUser is not null && RoleCapabilities.HasCapability(_loggedInUser.RoleId, Capability.CashManage);

	// StoreHub refuses every report without reports.view, so a user without it would only see errors there.
	private bool CanViewReports() =>
		_loggedInUser is not null && RoleCapabilities.HasCapability(_loggedInUser.RoleId, Capability.ReportsView);

	private Control[] ShownMenuButtons(bool showAccountsReceivable, bool showReports, bool showCashDrawer) =>
		AllMenuButtons().Where(button => (button != AccountsReceivableButton || showAccountsReceivable)
										 && (button != ReportsButton || showReports)
										 && (button != CashDrawerButton || showCashDrawer))
						.ToArray();

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