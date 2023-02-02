using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Events;
using IndyPOS.Windows.Forms.Interfaces;
using Prism.Events;
using System.Diagnostics.CodeAnalysis;
using UserRoleEnum = IndyPOS.Application.Common.Enums.UserRole;

namespace IndyPOS.Windows.Forms.UI.User
{
	[ExcludeFromCodeCoverage]
    public partial class UsersPanel : UserControl
    {
        private readonly IUserController _userController;
		private readonly IEventAggregator _eventAggregator;
		private readonly IReadOnlyDictionary<int, string> _userRoleDictionary;
		private readonly ICryptographyService _cryptographyService;
		private readonly AddNewUserForm _addNewUserForm;
		private IUserAccount _selectedUser;

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
						  ICryptographyService cryptographyService,
						  AddNewUserForm addNewUserForm)
		{
			_userController = userController;
			_eventAggregator = eventAggregator;
			_userRoleDictionary = storeConstants.UserRoles;
			_cryptographyService = cryptographyService;
			_addNewUserForm = addNewUserForm;

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

		private void AddUserToUserDataView(IUserAccount user)
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

		private void ShowUsersByRoleId(int roleId)
        {
			var users = GetUsersByRoleId(roleId);

			UserDataView.Rows.Clear();

			if (!users.Any())
				return;

			foreach (var user in users)
            {
				AddUserToUserDataView(user);
            }
        }

		private IList<IUserAccount> GetUsersByRoleId(int roleId)
        {
			var loggedInUser = _userController.LoggedInUser;

			if (loggedInUser.RoleId == (int) UserRoleEnum.Cashier)
				return new List<IUserAccount> { loggedInUser };

			return _userController.GetUsers()
								  .Where(x => x.RoleId == roleId)
								  .ToList();
		}

        private void UserRoleComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
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
			var userId = (int) selectedRow.Cells[(int)UserColumn.UserId].Value;

			return userId;
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

			var loggedInUser = _userController.LoggedInUser;
			var isVisibleToLoggedInUser = userId == loggedInUser.UserId;
			var isVisibleToLoggedInUserRole = loggedInUser.RoleId != (int) UserRoleEnum.Cashier;

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

        private void UpdateUserButton_Click(object sender, EventArgs e)
        {
			if (!UserPasswordTextBox.Texts.HasValue())
				return;

			var encryptedPassword = _cryptographyService.Encrypt(UserPasswordTextBox.Texts.Trim());

			_userController.UpdateUserCredentialById(_selectedUser.UserId, encryptedPassword);
		}

        private void AddUserButton_Click(object sender, EventArgs e)
        {
			_addNewUserForm.ShowDialog();
		}

        private void PasswordVisibilityButton_Click(object sender, EventArgs e)
		{
			UserPasswordTextBox.PasswordChar = !UserPasswordTextBox.PasswordChar;

			PasswordVisibilityButton.Image = UserPasswordTextBox.PasswordChar 
												 ? IndyPOS.Windows.Forms.Properties.Resources.Visible_25 
												 : IndyPOS.Windows.Forms.Properties.Resources.Hidden_25;
		}

        private void DeleteUserButton_Click(object sender, EventArgs e)
        {
			_userController.RemoveUserById(_selectedUser.UserId);
        }

        private void UsersPanel_VisibleChanged(object sender, EventArgs e)
		{
			if (!_userController.IsLoggedIn)
				return;

			var loggedInUser = _userController.LoggedInUser;

			if (loggedInUser.RoleId == (int) UserRoleEnum.Cashier)
			{
				AddUserButton.Visible = false;
				UserRoleComboBox.SelectedIndex = loggedInUser.RoleId - 1;
				UserRoleComboBox.Enabled = false;
			}
			else
			{
				AddUserButton.Visible = true;
				UserRoleComboBox.Enabled = true;
				UserRoleComboBox.SelectedIndex = loggedInUser.RoleId - 1;
			}

		}
    }
}