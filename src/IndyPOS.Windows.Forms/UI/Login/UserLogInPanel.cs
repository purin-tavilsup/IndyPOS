using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Windows.Forms.UI.Login;

/// <summary>
/// User login panel.
/// In StoreHub mode, user list is not pre-populated - users type their username directly.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class UserLogInPanel : UserControl
{
	private readonly IFirstLoginCoordinator _coordinator;
	private readonly MessageForm _messageForm;
	private bool _isLoggedIn;

	public UserLogInPanel(IFirstLoginCoordinator coordinator,
						  MessageForm messageForm)
	{
		_coordinator = coordinator;
		_messageForm = messageForm;

		InitializeComponent();
	}

	private async void LogInButton_Click(object sender, EventArgs e)
	{
		if (_isLoggedIn)
		{
			LogOut();

			return;
		}

		// Disable while the async login is in flight so a second click or
		// Enter press can't re-enter and show a second modal on the shared
		// MessageForm (which would throw "Form that is already visible").
		LogInButton.Enabled = false;

		try
		{
			await TryLogInAsync();
		}
		finally
		{
			LogInButton.Enabled = true;
		}
	}

	private async Task TryLogInAsync()
	{
		var username = UsersComboBox.Texts?.Trim() ?? string.Empty;
		var password = UserSecretTextBox.Texts.Trim();

		_isLoggedIn = await _coordinator.LogInAsync(username, password);

		if (_isLoggedIn)
		{
			HideUserInput();
		}
		else
		{
			_messageForm.ShowDialog("กรุณาใส่ Username และ Password ที่ถูกต้อง", "LogIn เข้าระบบไม่สำเร็จ");
		}
	}

	private void LogOut()
	{
		_coordinator.LogOut();

		_isLoggedIn = false;

		ShowUserInput();
	}

	private void PasswordVisibilityButton_Click(object sender, EventArgs e)
	{
		UserSecretTextBox.PasswordChar = !UserSecretTextBox.PasswordChar;

		PasswordVisibilityButton.Image = UserSecretTextBox.PasswordChar
											 ? Properties.Resources.Visible_25
											 : Properties.Resources.Hidden_25;
	}

	private void UserLogInPanel_VisibleChanged(object sender, EventArgs e)
	{
		if (!Visible)
			return;

		if (_isLoggedIn)
		{
			HideUserInput();
		}
		else
		{
			ShowUserInput();
		}
	}

	private void ShowUserInput()
	{
		ClearUserInput();

		UsersComboBox.Visible = true;
		UserInputPanel.Visible = true;
		LogInButton.Text = "Log In";
	}

	private void HideUserInput()
	{
		ClearUserInput();

		UsersComboBox.Visible = false;
		UserInputPanel.Visible = false;
		LogInButton.Text = "Log Out";
	}

	private void ClearUserInput()
	{
		UserSecretTextBox.Texts = string.Empty;
		UsersComboBox.Texts = "ผู้ใช้งาน";
	}
}
