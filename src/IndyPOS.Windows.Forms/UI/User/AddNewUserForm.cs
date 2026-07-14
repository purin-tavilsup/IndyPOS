using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.User;

/// <summary>
/// Form for adding new users.
/// NOTE: This form is disabled in StoreHub mode - user management is done via CloudAPI.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class AddNewUserForm : Form
{
	private readonly IReadOnlyDictionary<int, string> _userRoleDictionary;
	private readonly ICryptographyService _cryptographyService;
	private readonly MessageForm _messageForm;

	public AddNewUserForm(IStoreConstants storeConstants,
						  ICryptographyService cryptographyService,
						  MessageForm messageForm)
	{
		_userRoleDictionary = storeConstants.UserRoles;
		_cryptographyService = cryptographyService;
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
			_messageForm.ShowDialog("กรุณาใส่ชื่อของผู้ใช้", "ชื่อของผู้ใช้ไม่ถูกต้อง");

			return false;
		}

		if (!LastNameTextBox.Texts.HasValue())
		{
			_messageForm.ShowDialog("กรุณาใส่นามสกุลของผู้ใช้", "นามสกุลของผู้ใช้ไม่ถูกต้อง");

			return false;
		}

		if (!_userRoleDictionary.Values.Contains(UserRoleComboBox.Texts.Trim()))
		{
			_messageForm.ShowDialog("กรุณาเลือกประเภทผู้ใช้ให้ถูกต้อง", "ประเภทผู้ใช้ไม่ถูกต้อง");

			return false;
		}

		if (!UserSecretTextBox.Texts.HasValue())
		{
			_messageForm.ShowDialog("กรุณาสร้างรหัสผ่าน", "รหัสผ่านไม่ถูกต้อง");

			return false;
		}

		return true;
	}

	private void SaveUserEntryButton_Click(object sender, EventArgs e)
	{
		// StoreHub mode: User creation disabled
		_messageForm.ShowDialog("การเพิ่มผู้ใช้ใหม่ถูกปิดในโหมด StoreHub กรุณาใช้ CloudAPI", "ฟังก์ชันนี้ไม่พร้อมใช้งาน");
		Hide();
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
