using IndyPOS.Constants;
using IndyPOS.Controllers;
using IndyPOS.Cryptography;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndyPOS.Users;

namespace IndyPOS.UI
{
    public partial class AddNewUserForm : Form
    {
        private readonly IUserController _userController;
        private readonly IReadOnlyDictionary<int, string> _userRoleDictionary;
        private readonly ICryptographyHelper _cryptographyHelper;
        private readonly MessageForm _messageForm;

        public AddNewUserForm(IUserController userController,
                                IStoreConstants storeConstants,
                                ICryptographyHelper cryptographyHelper,
                                MessageForm messageForm)
        {
            _userController = userController;
            _userRoleDictionary = storeConstants.UserRoles;
            _cryptographyHelper = cryptographyHelper;
            _messageForm = messageForm;

            InitializeComponent();
            InitializeProductCategories();
        }

        private void InitializeProductCategories()
        {
            UserRoleComboBox.Items.Clear();

            foreach (var item in _userRoleDictionary)
            {
                UserRoleComboBox.Items.Add(item.Value);
            }
        }

        private bool ValidateProductEntry()
        {
            if (string.IsNullOrWhiteSpace(UserSecretTextBox.Texts.Trim()))
            {
                _messageForm.Show("กรุณาสร้างรหัสผ่าน", "รหัสผ่านไม่ถูกต้อง");

                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Texts.Trim()))
            {
                _messageForm.Show("กรุณาใส่ชื่อของผู้ใช้", "ชื่อของผู้ใช้ไม่ถูกต้อง");

                return false;
            }

            if (!_userRoleDictionary.Values.Contains(UserRoleComboBox.Texts.Trim()))
            {
                _messageForm.Show("กรุณาเลือกประเภทผู้ใช้ให้ถูกต้อง", "ประเภทผู้ใช้ไม่ถูกต้อง");

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
            var encryptedSecret = _cryptographyHelper.Encrypt(UserSecretTextBox.Texts.Trim());

            var newUser = new User
            {
                FirstName = firstName,
                LastName = lastName,
                RoleId = roleId
            };

            _userController.AddNewUser(newUser, encryptedSecret);

            Close();
        }

        private void CancelUserEntryButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
