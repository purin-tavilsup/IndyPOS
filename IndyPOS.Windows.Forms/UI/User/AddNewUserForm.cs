using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Interfaces;
using IndyPOS.Application.Models;
using IndyPOS.Common.Extensions;
using IndyPOS.Common.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;

namespace IndyPOS.Windows.Forms.UI.User;

[ExcludeFromCodeCoverage]
public partial class AddNewUserForm : Form
{
	private readonly IUserController _userController;
	private readonly IReadOnlyDictionary<int, string> _userRoleDictionary;
	private readonly ICryptographyUtility _cryptographyUtility;
	private readonly MessageForm _messageForm;

	public AddNewUserForm(IUserController userController,
						  IStoreConstants storeConstants,
						  ICryptographyUtility cryptographyUtility,
						  MessageForm messageForm)
	{
		_userController = userController;
		_userRoleDictionary = storeConstants.UserRoles;
		_cryptographyUtility = cryptographyUtility;
		_messageForm = messageForm;

		InitializeComponent();
		InitializeUserRoles();
	}

	private void InitializeUserRoles()
	{
		UserRoleComboBox.Items.Clear();

		foreach (var item in _userRoleDictionary)
		{
			UserRoleComboBox.Items.Add(item.Value);
		}
	}

	private bool ValidateProductEntry()
	{
		if (!FirstNameTextBox.Texts.HasValue())
		{
			_messageForm.Show("กรุณาใส่ชื่อของผู้ใช้", "ชื่อของผู้ใช้ไม่ถูกต้อง");

			return false;
		}

		if (!LastNameTextBox.Texts.HasValue())
		{
			_messageForm.Show("กรุณาใส่นามสกุลของผู้ใช้", "นามสกุลของผู้ใช้ไม่ถูกต้อง");

			return false;
		}

		if (!_userRoleDictionary.Values.Contains(UserRoleComboBox.Texts.Trim()))
		{
			_messageForm.Show("กรุณาเลือกประเภทผู้ใช้ให้ถูกต้อง", "ประเภทผู้ใช้ไม่ถูกต้อง");

			return false;
		}

		if (!UserSecretTextBox.Texts.HasValue())
		{
			_messageForm.Show("กรุณาสร้างรหัสผ่าน", "รหัสผ่านไม่ถูกต้อง");

			return false;
		}

		return true;
	}

	private void SaveUserEntryButton_Click(object sender, EventArgs e)
	{
		if (!ValidateProductEntry())
			return;

		var firstName = FirstNameTextBox.Texts.Trim();
		var lastName = LastNameTextBox.Texts.Trim();
		var selectedRole = UserRoleComboBox.SelectedItem.ToString();
		var role = _userRoleDictionary.FirstOrDefault(x => x.Value == selectedRole);
		var roleId = role.Key;
		var username = UsernameLabel.Text;
		var encryptedPassword = _cryptographyUtility.Encrypt(UserSecretTextBox.Texts.Trim());

		var newUser = new UserAccount
		{
			FirstName = firstName,
			LastName = lastName,
			RoleId = roleId
		};

		try
		{
			_userController.AddNewUser(newUser, username, encryptedPassword);

			ClearUserEntry();

			Hide();
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังบันทึกผู้ใช้ใหม่ Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกผู้ใช้ใหม่");
		}
	}

	private void CancelUserEntryButton_Click(object sender, EventArgs e)
	{
		Hide();
	}

	private void ClearUserEntry()
	{
		FirstNameTextBox.Texts = string.Empty;
		LastNameTextBox.Texts = string.Empty;
		UserSecretTextBox.Texts = string.Empty;
	}

	private void FirstNameTextBox_Leave(object sender, EventArgs e)
	{
		if (!FirstNameTextBox.Texts.HasValue())
			return;

		GenerateUserName();
	}

	private void LastNameTextBox_Leave(object sender, EventArgs e)
	{
		if (!LastNameTextBox.Texts.HasValue())
			return;

		GenerateUserName();
	}

	private void GenerateUserName()
	{
		UsernameLabel.Text = $"{FirstNameTextBox.Texts.Trim().ToLower()}.{LastNameTextBox.Texts.Trim().ToLower()}";
	}

	private void PasswordVisibilityButton_Click(object sender, EventArgs e)
	{
		UserSecretTextBox.PasswordChar = !UserSecretTextBox.PasswordChar;

		PasswordVisibilityButton.Image = UserSecretTextBox.PasswordChar 
											 ? Properties.Resources.Visible_25 
											 : Properties.Resources.Hidden_25;
	}
}