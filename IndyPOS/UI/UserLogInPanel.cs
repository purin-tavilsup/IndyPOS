using IndyPOS.Controllers;
using IndyPOS.Cryptography;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
    public partial class UserLogInPanel : UserControl
    {
		private readonly IUserController _userController;
		private readonly ICryptographyHelper _cryptographyHelper;
		private readonly MessageForm _messageForm;
		
		public UserLogInPanel(IUserController userController,
							  ICryptographyHelper cryptographyHelper,
							  MessageForm messageForm)
		{
			_userController = userController;
			_cryptographyHelper = cryptographyHelper;
			_messageForm = messageForm;

			InitializeComponent();
		}

		private void LogInButton_Click(object sender, EventArgs e)
		{
			if (_userController.IsLoggedIn)
			{
				_userController.LogOut();

				ShowUserInput();

				return;
			}
			
			var username = UserNameTextBox.Texts.Trim();
			var password = _cryptographyHelper.Encrypt(UserSecretTextBox.Texts.Trim());

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

			UserInputPanel.Visible = true;
			LogInButton.Text = "Log In";
        }

		private void HideUserInput()
        {
			ClearUserInput();

			UserInputPanel.Visible = false;
			LogInButton.Text = "Log Out";
        }

		private void ClearUserInput()
        {
			UserNameTextBox.Texts = string.Empty;
			UserSecretTextBox.Texts = string.Empty;
        }
    }
}
