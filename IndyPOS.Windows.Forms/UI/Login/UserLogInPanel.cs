using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;

namespace IndyPOS.Windows.Forms.UI.Login;

[ExcludeFromCodeCoverage]
public partial class UserLogInPanel : UserControl
{
	private readonly IUserController _userController;
	private readonly ICryptographyService _cryptographyUtility;
	private readonly MessageForm _messageForm;
		
	public UserLogInPanel(IUserController userController,
						  ICryptographyService cryptographyUtility,
						  MessageForm messageForm)
	{
		_userController = userController;
		_cryptographyUtility = cryptographyUtility;
		_messageForm = messageForm;

		InitializeComponent();
		InitializeProductCategories();
	}

	private void InitializeProductCategories()
	{
		UsersComboBox.Items.Clear();

		var users = _userController.GetUsers();

		foreach (var user in users)
		{
			var username = $"{user.FirstName.ToLower()}.{user.LastName.ToLower()}";

			UsersComboBox.Items.Add(username);
		}
	}

	private void LogInButton_Click(object sender, EventArgs e)
	{
		if (_userController.IsLoggedIn)
		{
			_userController.LogOut();

			ShowUserInput();

			return;
		}
			
		var username = UsersComboBox.SelectedItem?.ToString() ?? string.Empty;
		var password = _cryptographyUtility.Encrypt(UserSecretTextBox.Texts.Trim());

		if (_userController.LogIn(username, password))
		{
			HideUserInput();
		}
		else
		{
			_messageForm.Show("กรุณาใส่ Username และ Password ที่ถูกต้อง", "LogIn เข้าระบบไม่สำเร็จ");
		}
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

		if (_userController.IsLoggedIn)
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