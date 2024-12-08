using IndyPOS.Application.Common.Interfaces;
using MediatR;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.Users;
using IndyPOS.Application.UseCases.Users.Get;

namespace IndyPOS.Windows.Forms.UI.Login;

[ExcludeFromCodeCoverage]
public partial class UserLogInPanel : UserControl
{
	private readonly IUserLogInService _userLogInService;
	private readonly IMediator _mediator;
	private readonly ICryptographyService _cryptographyService;
	private readonly MessageForm _messageForm;
	private bool _isLoggedIn;
		
	public UserLogInPanel(IUserLogInService userLogInService,
						  IMediator mediator,
						  ICryptographyService cryptographyService,
						  MessageForm messageForm)
	{
		_userLogInService = userLogInService;
		_mediator = mediator;
		_cryptographyService = cryptographyService;
		_messageForm = messageForm;

		InitializeComponent();
		InitializeUsers();
	}

	private void InitializeUsers()
	{
		UsersComboBox.Items.Clear();

		var users = GetUsers();

		foreach (var user in users)
		{
			var username = $"{user.FirstName.ToLower()}.{user.LastName.ToLower()}";

			UsersComboBox.Items.Add(username);
		}
	}

	private IEnumerable<UserDto> GetUsers()
	{
		return _mediator.Send(new GetUsersQuery())
						.GetAwaiter()
						.GetResult();
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
		var username = UsersComboBox.SelectedItem?.ToString() ?? string.Empty;
		var password = _cryptographyService.Encrypt(UserSecretTextBox.Texts.Trim());

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