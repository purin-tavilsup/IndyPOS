using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Events;
using IndyPOS.Domain.Events;
using System.Diagnostics.CodeAnalysis;
using UserRoleEnum = IndyPOS.Application.Common.Enums.UserRole;

namespace IndyPOS.Windows.Forms.UI.User;

/// <summary>
/// User management panel.
/// NOTE: This panel is disabled in StoreHub mode - user management is done via CloudAPI.
/// The panel remains for backward compatibility but returns empty data.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class UsersPanel : UserControl
{
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

	public UsersPanel(IEventAggregator eventAggregator,
					  IStoreConstants storeConstants,
					  ICryptographyService cryptographyService,
					  AddNewUserForm addNewUserForm,
					  MessageForm messageForm)
	{
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
		userRow[(int)UserColumn.DateUpdated] = "-";

		UserDataView.Rows.Add(userRow);
	}

	private Task ShowUsersByRoleId(int roleId)
	{
		// StoreHub mode: User management disabled, return empty list
		UserDataView.Rows.Clear();
		_messageForm.ShowDialog("การจัดการผู้ใช้ถูกปิดในโหมด StoreHub กรุณาใช้ CloudAPI", "ฟังก์ชันนี้ไม่พร้อมใช้งาน");
		return Task.CompletedTask;
	}

	private void UserRoleComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		var selectedRole = UserRoleComboBox.SelectedItem?.ToString();
		if (string.IsNullOrEmpty(selectedRole)) return;

		var role = _userRoleDictionary.FirstOrDefault(x => x.Value == selectedRole);
		var roleId = role.Key;

		_lastQueryRoleId = roleId;

		ResetUserDetails();

		_ = ShowUsersByRoleId(roleId);
	}

	private void UserDataView_CellClick(object sender, DataGridViewCellEventArgs e)
	{
		// StoreHub mode: User details not available
	}

	private void UserChanged()
	{
		// StoreHub mode: User management disabled
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

	private void UpdateUserButton_Click(object sender, EventArgs e)
	{
		// StoreHub mode: User update disabled
		_messageForm.ShowDialog("การอัพเดทผู้ใช้ถูกปิดในโหมด StoreHub", "ฟังก์ชันนี้ไม่พร้อมใช้งาน");
	}

	private void AddUserButton_Click(object sender, EventArgs e)
	{
		// StoreHub mode: User creation disabled
		_messageForm.ShowDialog("การเพิ่มผู้ใช้ใหม่ถูกปิดในโหมด StoreHub กรุณาใช้ CloudAPI", "ฟังก์ชันนี้ไม่พร้อมใช้งาน");
	}

	private void PasswordVisibilityButton_Click(object sender, EventArgs e)
	{
		UserPasswordTextBox.PasswordChar = !UserPasswordTextBox.PasswordChar;

		PasswordVisibilityButton.Image = UserPasswordTextBox.PasswordChar
											 ? Properties.Resources.Visible_25
											 : Properties.Resources.Hidden_25;
	}

	private void DeleteUserButton_Click(object sender, EventArgs e)
	{
		// StoreHub mode: User deletion disabled
		_messageForm.ShowDialog("การลบผู้ใช้ถูกปิดในโหมด StoreHub", "ฟังก์ชันนี้ไม่พร้อมใช้งาน");
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
