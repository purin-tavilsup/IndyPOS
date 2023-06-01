using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Application.UserCredentials;
using IndyPOS.Application.UserCredentials.Queries.GetUserCredentialById;
using IndyPOS.Application.Users;
using IndyPOS.Application.Users.Queries.GetUserById;
using IndyPOS.Application.Users.Queries.GetUsers;
using IndyPOS.Domain.Events;
using MediatR;
using Prism.Events;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UserCredentials.Commands.UpdateUserCredential;
using IndyPOS.Application.Users.Commands.DeleteUser;
using UserRoleEnum = IndyPOS.Application.Common.Enums.UserRole;

namespace IndyPOS.Windows.Forms.UI.User;

[ExcludeFromCodeCoverage]
public partial class UsersPanel : UserControl
{
	private readonly IMediator _mediator;
	private readonly IEventAggregator _eventAggregator;
	private readonly IReadOnlyDictionary<int, string> _userRoleDictionary;
	private readonly ICryptographyService _cryptographyService;
	private readonly AddNewUserForm _addNewUserForm;
	private readonly MessageForm _messageForm;
	private UserDto? _selectedUser;
	private ILoggedInUser? _loggedInUser;

	private int? _lastQueryRoleId;

	private enum UserColumn
	{
		UserId,
		FirstName,
		LastName,
		UserRole,
		DateCreated,
		DateUpdated
	}

	public UsersPanel(IMediator mediator,
					  IEventAggregator eventAggregator,
					  IStoreConstants storeConstants,
					  ICryptographyService cryptographyService,
					  AddNewUserForm addNewUserForm,
					  MessageForm messageForm)
	{
		_mediator = mediator;
		_eventAggregator = eventAggregator;
		_userRoleDictionary = storeConstants.UserRoles;
		_cryptographyService = cryptographyService;
		_addNewUserForm = addNewUserForm;
		_messageForm = messageForm;

		InitializeComponent();
		InitializeUserRoles();
		InitializeUserDataView();
		SubscribeEvents();
	}

	private void InitializeUserRoles()
	{
		UserRoleComboBox.Items.Clear();

		foreach (var item in _userRoleDictionary)
		{
			UserRoleComboBox.Items.Add(item.Value);
		}
	}

	private void SubscribeEvents()
	{
		_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
		_eventAggregator.GetEvent<UserLoggedOutEvent>().Subscribe(OnUserLoggedOut);
		_eventAggregator.GetEvent<UserAddedEvent>().Subscribe(UserChanged);
		_eventAggregator.GetEvent<UserRemovedEvent>().Subscribe(UserChanged);
	}

	private void InitializeUserDataView()
	{
		#region Initialize all columns

		UserDataView.Columns.Clear();
		UserDataView.ColumnCount = 6;

		UserDataView.Columns[(int)UserColumn.UserId].Name = "หมายเลขผู้ใช้งาน";
		UserDataView.Columns[(int)UserColumn.UserId].Width = 170;
		UserDataView.Columns[(int)UserColumn.UserId].ReadOnly = true;

		UserDataView.Columns[(int)UserColumn.FirstName].Name = "ชื่อ";
		UserDataView.Columns[(int)UserColumn.FirstName].Width = 150;
		UserDataView.Columns[(int)UserColumn.FirstName].ReadOnly = true;

		UserDataView.Columns[(int)UserColumn.LastName].Name = "นามสกุล";
		UserDataView.Columns[(int)UserColumn.LastName].Width = 150;
		UserDataView.Columns[(int)UserColumn.LastName].ReadOnly = true;

		UserDataView.Columns[(int)UserColumn.UserRole].Name = "ประเภทผู้ใช้งาน";
		UserDataView.Columns[(int)UserColumn.UserRole].Width = 150;
		UserDataView.Columns[(int)UserColumn.UserRole].ReadOnly = true;

		UserDataView.Columns[(int)UserColumn.DateCreated].Name = "วันที่สร้าง";
		UserDataView.Columns[(int)UserColumn.DateCreated].Width = 200;
		UserDataView.Columns[(int)UserColumn.DateCreated].ReadOnly = true;

		UserDataView.Columns[(int)UserColumn.DateUpdated].Name = "วันที่อัพเดท";
		UserDataView.Columns[(int)UserColumn.DateUpdated].Width = 200;
		UserDataView.Columns[(int)UserColumn.DateUpdated].ReadOnly = true;

		#endregion
	}

	private void AddUserToUserDataView(UserDto user)
	{
		var columnCount = UserDataView.ColumnCount;
		var userRow = new object[columnCount];

		var userRole = _userRoleDictionary.ContainsKey(user.RoleId) ?
						   _userRoleDictionary[user.RoleId] :
						   "Unknown";

		userRow[(int)UserColumn.UserId] = user.UserId;
		userRow[(int)UserColumn.FirstName] = user.FirstName;
		userRow[(int)UserColumn.LastName] = user.LastName;
		userRow[(int)UserColumn.UserRole] = userRole;
		userRow[(int)UserColumn.DateCreated] = user.DateCreated;
		userRow[(int)UserColumn.DateUpdated] = user.DateUpdated;

		UserDataView.Rows.Add(userRow);
	}

	private async Task ShowUsersByRoleId(int roleId)
	{
		var users = await GetUsersByRoleId(roleId);

		UserDataView.Rows.Clear();

		if (users.Count == 0)
			return;

		foreach (var user in users)
		{
			AddUserToUserDataView(user);
		}
	}

	private async Task<IList<UserDto>> GetUsersByRoleId(int roleId)
	{
		if (_loggedInUser is null)
			return new List<UserDto>();

		if (_loggedInUser.RoleId == (int) UserRoleEnum.Cashier)
		{
			var user = await GetUserByIdAsync(_loggedInUser.UserId);

			return new List<UserDto> { user };
		}

		var users = await GetUsersAsync();

		return users.Where(x => x.RoleId == roleId).ToList();
	}

	private async Task<IEnumerable<UserDto>> GetUsersAsync()
	{
		return await _mediator.Send(new GetUsersQuery());
	}

	private async Task<UserCredentialDto> GetUserCredentialByIdAsync(int id)
	{
		return await _mediator.Send(new GetUserCredentialByIdQuery(id));
	}

	private async Task<UserDto> GetUserByIdAsync(int id)
	{
		return await _mediator.Send(new GetUserByIdQuery(id));
	}

	private async Task UpdateUserCredential(int userId, string encryptedPassword)
	{
		var command = new UpdateUserCredentialCommand
		{
			UserId = userId,
			Password = encryptedPassword
		};

		_ = await _mediator.Send(command);
	}

	private async Task DeleteUserByIdAsync(int id)
	{
		_ = await _mediator.Send(new DeleteUserCommand(id));
	}

	private async void UserRoleComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		var selectedRole = UserRoleComboBox.SelectedItem.ToString();
		var role = _userRoleDictionary.FirstOrDefault(x => x.Value == selectedRole);
		var roleId = role.Key;

		_lastQueryRoleId = roleId;

		ResetUserDetails();

		try
		{
			await ShowUsersByRoleId(roleId);
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังค้นหาบัญชีผู้ใช้ (Role ID: {roleId}) ในระบบ Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังค้นหาบัญชีผู้ใช้ในระบบ");
		}
	}

	private int GetUserIdFromSelectedUser()
	{
		if (UserDataView.SelectedCells.Count == 0)
			return -1;

		var selectedCell = UserDataView.SelectedCells[0];
		var rowIndex  = selectedCell.RowIndex;
		var selectedRow = UserDataView.Rows[rowIndex];
		var userId = (int) selectedRow.Cells[(int)UserColumn.UserId].Value;

		return userId;
	}

	private async void UserDataView_CellClick(object sender, DataGridViewCellEventArgs e)
	{
		var userId = GetUserIdFromSelectedUser();

		try
		{
			await ShowUserDetailsByIdAsync(userId);
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังค้นหาบัญชีผู้ใช้ (User ID: {userId}) ในระบบ Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังค้นหาบัญชีผู้ใช้ในระบบ");
		}
	}

	private async void UserChanged()
	{
		if (!_lastQueryRoleId.HasValue)
			return;

		ResetUserDetails();

		var roleId = _lastQueryRoleId.GetValueOrDefault();

		try
		{
			await ShowUsersByRoleId(roleId);
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังค้นหาบัญชีผู้ใช้ (Role ID: {roleId}) ในระบบ Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังค้นหาบัญชีผู้ใช้ในระบบ");
		}
	}

	private async Task ShowUserDetailsByIdAsync(int userId)
	{
		if (_loggedInUser is null)
			return;

		_selectedUser = await GetUserByIdAsync(userId);

		var userCredential = await GetUserCredentialByIdAsync(userId);

		var userRole = _userRoleDictionary.ContainsKey(_selectedUser.RoleId) 
						   ? _userRoleDictionary[_selectedUser.RoleId] 
						   : "Unknown";
			
		var isVisibleToLoggedInUser = userId                   == _loggedInUser.UserId;
		var isVisibleToLoggedInUserRole = _loggedInUser.RoleId != (int) UserRoleEnum.Cashier;

		PasswordLabel.Visible = isVisibleToLoggedInUser;
		UserPasswordTextBox.Visible = isVisibleToLoggedInUser;
		PasswordVisibilityButton.Visible = isVisibleToLoggedInUser;
		UpdateUserButton.Visible = isVisibleToLoggedInUser;
		DeleteUserButton.Visible = isVisibleToLoggedInUserRole;

		FirstNameLabel.Text = _selectedUser.FirstName;
		LastNameLabel.Text = _selectedUser.LastName;
		UserRoleLabel.Text = userRole;
		UsernameLabel.Text = userCredential.Username;

		if (isVisibleToLoggedInUser) 
			UserPasswordTextBox.Texts = _cryptographyService.Decrypt(userCredential.Password);
	}

	private void ResetUserDetails()
	{
		FirstNameLabel.Text = string.Empty;
		LastNameLabel.Text = string.Empty;
		UserRoleLabel.Text = string.Empty;
		UsernameLabel.Text = string.Empty;
		UserPasswordTextBox.Texts = string.Empty;

		PasswordLabel.Visible = false;
		UserPasswordTextBox.Visible = false;
		PasswordVisibilityButton.Visible = false;
		UpdateUserButton.Visible = false;
		DeleteUserButton.Visible = false;
	}

	private async void UpdateUserButton_Click(object sender, EventArgs e)
	{
		if (!UserPasswordTextBox.Texts.HasValue() || _selectedUser is null)
			return;

		var userId = _selectedUser.UserId;

		try
		{
			var encryptedPassword = _cryptographyService.Encrypt(UserPasswordTextBox.Texts.Trim());

			await UpdateUserCredential(userId, encryptedPassword);
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังอัพเดทบัญชีผู้ใช้ (User ID: {userId}) ในระบบ Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังอัพเดทบัญชีผู้ใช้");
		}
	}

	private void AddUserButton_Click(object sender, EventArgs e)
	{
		_addNewUserForm.ShowDialog();
	}

	private void PasswordVisibilityButton_Click(object sender, EventArgs e)
	{
		UserPasswordTextBox.PasswordChar = !UserPasswordTextBox.PasswordChar;

		PasswordVisibilityButton.Image = UserPasswordTextBox.PasswordChar 
											 ? Properties.Resources.Visible_25 
											 : Properties.Resources.Hidden_25;
	}

	private async void DeleteUserButton_Click(object sender, EventArgs e)
	{
		if (_selectedUser is null)
			return;

		var userId = _selectedUser.UserId;

		try
		{
			await DeleteUserByIdAsync(userId);
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังลบบัญชีผู้ใช้ (User ID: {userId}) ในระบบ Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังลบบัญชีผู้ใช้");
		}
	}

	private void UsersPanel_VisibleChanged(object sender, EventArgs e)
	{
		if (_loggedInUser is null)
			return;

		if (_loggedInUser.RoleId == (int) UserRoleEnum.Cashier)
		{
			AddUserButton.Visible = false;
			UserRoleComboBox.SelectedIndex = _loggedInUser.RoleId - 1;
			UserRoleComboBox.Enabled = false;
		}
		else
		{
			AddUserButton.Visible = true;
			UserRoleComboBox.Enabled = true;
			UserRoleComboBox.SelectedIndex = _loggedInUser.RoleId - 1;
		}
	}

	private void OnUserLoggedIn(ILoggedInUser loggedInUser)
	{
		_loggedInUser = loggedInUser;
	}

	private void OnUserLoggedOut()
	{
		_loggedInUser = null;
	}
}