using IndyPOS.Interfaces;
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

			if (UsersComboBox.SelectedItem == null)
			{
				_messageForm.Show("กรุณาใส่ Username ที่ถูกต้อง", "LogIn เข้าระบบไม่สำเร็จ");

				return;
			}
			
			var username = UsersComboBox.SelectedItem.ToString();
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
			UsersComboBox.SelectedItem = null;
			UsersComboBox.Texts = "ผู้ใช้งาน";
		}
	}
}
