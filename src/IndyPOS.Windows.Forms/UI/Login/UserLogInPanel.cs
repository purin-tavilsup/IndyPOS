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
	private readonly IUserLogInService _userLogInService;
	private readonly MessageForm _messageForm;
	private bool _isLoggedIn;

	public UserLogInPanel(IUserLogInService userLogInService,
						  MessageForm messageForm)
	{
		_userLogInService = userLogInService;
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

		await TryLogInAsync();
	}

	private async Task TryLogInAsync()
	{
		var username = UsersComboBox.Texts?.Trim() ?? string.Empty;
		var password = UserSecretTextBox.Texts.Trim();

		_isLoggedIn = await _userLogInService.LogInAsync(username, password);

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
		_userLogInService.LogOut();

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
