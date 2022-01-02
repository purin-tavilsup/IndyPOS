using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Cryptography;
using IndyPOS.Extensions;
using IndyPOS.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using IndyPOS.Events;
using Prism.Events;

namespace IndyPOS.UI
{
	[ExcludeFromCodeCoverage]
    public partial class UsersPanel : UserControl
    {
        private readonly IUserController _userController;
		private readonly IEventAggregator _eventAggregator;
		private readonly IReadOnlyDictionary<int, string> _userRoleDictionary;
		private readonly ICryptographyHelper _cryptographyHelper;
		private readonly AddNewUserForm _addNewUserForm;
		private IUser _selectedUser;

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
						  IUserController userController,
						  IStoreConstants storeConstants,
						  ICryptographyHelper cryptographyHelper,
						  AddNewUserForm addNewUserForm)
		{
			_userController = userController;
			_eventAggregator = eventAggregator;
			_userRoleDictionary = storeConstants.UserRoles;
			_cryptographyHelper = cryptographyHelper;
			_addNewUserForm = addNewUserForm;

            InitializeComponent();

			InitializeProductCategories();
			InitializeUserDataView();

			SubscribeEvents();
		}

		private void InitializeProductCategories()
		{
			UserRoleComboBox.Items.Clear();

			foreach (var item in _userRoleDictionary)
			{
				UserRoleComboBox.Items.Add(item.Value);
			}
		}

		private void SubscribeEvents()
		{
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

		private void AddUserToUserDataView(IUser user)
        {
			var columnCount = UserDataView.ColumnCount;
			var userRow = new string[columnCount];

			var userRole = _userRoleDictionary.ContainsKey(user.RoleId) ?
				_userRoleDictionary[user.RoleId] :
				"Unknown";

			userRow[(int)UserColumn.UserId] = user.UserId.ToString();
			userRow[(int)UserColumn.FirstName] = user.FirstName;
			userRow[(int)UserColumn.LastName] = user.LastName;
			userRow[(int)UserColumn.UserRole] = userRole;
			userRow[(int)UserColumn.DateCreated] = user.DateCreated;
			userRow[(int)UserColumn.DateUpdated] = user.DateUpdated;

			UserDataView.Rows.Add(userRow);
        }

		private void ShowUsersByRoleId(int roleId)
        {
			var users = _userController.GetUsers()
									   .Where(x => x.RoleId == roleId)
									   .ToList();

			UserDataView.Rows.Clear();

			if (!users.Any())
				return;

			foreach (var user in users)
            {
				AddUserToUserDataView(user);
            }
        }

        private void UserRoleComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
			if (UserRoleComboBox.SelectedItem == null)
				return;

			var selectedRole = UserRoleComboBox.SelectedItem.ToString();
			var role = _userRoleDictionary.FirstOrDefault(x => x.Value == selectedRole);
			var roleId = role.Key;

			_lastQueryRoleId = roleId;

			ResetUserDetails();
			ShowUsersByRoleId(roleId);
		}

		private int GetUserIdFromSelectedUser()
        {
			if (UserDataView.SelectedCells.Count == 0)
				return -1;

			var selectedCell = UserDataView.SelectedCells[0];
			var rowIndex  = selectedCell.RowIndex;
			var selectedRow = UserDataView.Rows[rowIndex];
			var userId = selectedRow.Cells[(int)UserColumn.UserId].Value as string;

			return int.TryParse(userId, out var id) ? id : -1;
        }

        private void UserDataView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
			var userId = GetUserIdFromSelectedUser();

			ShowUserDetailsById(userId);
		}

		private void UserChanged()
        {
			if (!_lastQueryRoleId.HasValue)
				return;

			ResetUserDetails();
			ShowUsersByRoleId(_lastQueryRoleId.GetValueOrDefault());
		}

		private void ShowUserDetailsById(int userId)
        {
			_selectedUser = _userController.GetUserById(userId);
			
			if (_selectedUser == null)
				return;

			var userCredential = _userController.GetUserCredentialById(userId);
			var userRole = _userRoleDictionary.ContainsKey(_selectedUser.RoleId) 
							   ? _userRoleDictionary[_selectedUser.RoleId] 
							   : "Unknown";

			FirstNameLabel.Text = _selectedUser.FirstName;
			LastNameLabel.Text = _selectedUser.LastName;
			UserRoleLabel.Text = userRole;
			UsernameLabel.Text = userCredential.Username;
			UserSecretTextBox.Texts = _cryptographyHelper.Decrypt(userCredential.Password);
		}

		private void ResetUserDetails()
        {
			FirstNameLabel.Text = string.Empty;
			LastNameLabel.Text = string.Empty;
			UserRoleLabel.Text = string.Empty;
			UsernameLabel.Text = string.Empty;
			UserSecretTextBox.Texts = string.Empty;
        }

        private void UpdateUserButton_Click(object sender, EventArgs e)
        {
			if (!UserSecretTextBox.Texts.HasValue())
				return;

			var encryptedPassword = _cryptographyHelper.Encrypt(UserSecretTextBox.Texts.Trim());

			_userController.UpdateUserCredentialById(_selectedUser.UserId, encryptedPassword);
		}

        private void AddUserButton_Click(object sender, EventArgs e)
        {
			_addNewUserForm.ShowDialog();
		}

        private void PasswordVisibilityButton_Click(object sender, EventArgs e)
		{
			UserSecretTextBox.PasswordChar = !UserSecretTextBox.PasswordChar;

			PasswordVisibilityButton.Image = UserSecretTextBox.PasswordChar 
												 ? Properties.Resources.Visible_25 
												 : Properties.Resources.Hidden_25;
		}

        private void DeleteUserButton_Click(object sender, EventArgs e)
        {
			_userController.RemoveUserById(_selectedUser.UserId);
        }
    }
}