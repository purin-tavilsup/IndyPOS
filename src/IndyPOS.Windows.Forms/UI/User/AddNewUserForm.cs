using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UserCredentials.Commands.CreateUserCredential;
using IndyPOS.Application.Users.Commands.CreateUser;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.User;

[ExcludeFromCodeCoverage]
public partial class AddNewUserForm : Form
{
	private readonly IMediator _mediator;
	private readonly IReadOnlyDictionary<int, string> _userRoleDictionary;
	private readonly ICryptographyService _cryptographyService;
	private readonly MessageForm _messageForm;

	public AddNewUserForm(IMediator mediator,
						  IStoreConstants storeConstants,
						  ICryptographyService cryptographyService,
						  MessageForm messageForm)
	{
		_mediator = mediator;
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

	private async void SaveUserEntryButton_Click(object sender, EventArgs e)
	{
		if (!ValidateProductEntry())
			return;

		try
		{
			var userId = await CreateUserAsync();

			await CreateUserCredentialAsync(userId);

			ClearUserEntry();

			Hide();
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังบันทึกผู้ใช้ใหม่ Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกผู้ใช้ใหม่");
		}
	}

	private CreateUserCommand CreateCommandForCreateUser()
	{
		var selectedRole = UserRoleComboBox.SelectedItem.ToString();
		var role = _userRoleDictionary.FirstOrDefault(x => x.Value == selectedRole);

		return new CreateUserCommand
		{
			FirstName = FirstNameTextBox.Texts.Trim(),
			LastName = LastNameTextBox.Texts.Trim(),
			RoleId = role.Key
		};
	}

	private CreateUserCredentialCommand CreateCommandForCreateUserCredential(int userId)
	{
		return new CreateUserCredentialCommand
		{
			UserId = userId,
			Username = UsernameLabel.Text,
			Password = _cryptographyService.Encrypt(UserSecretTextBox.Texts.Trim())
		};
	}

	private async Task<int> CreateUserAsync()
	{
		var command = CreateCommandForCreateUser();

		return await _mediator.Send(command);
	}

	private async Task CreateUserCredentialAsync(int userId)
	{
		var command = CreateCommandForCreateUserCredential(userId);

		await _mediator.Send(command);
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