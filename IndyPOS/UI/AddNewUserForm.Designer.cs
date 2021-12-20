namespace IndyPOS.UI
{
    partial class AddNewUserForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel2 = new System.Windows.Forms.Panel();
            this.CancelUserEntryButton = new ModernUI.ModernButton();
            this.SaveUserEntryButton = new ModernUI.ModernButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.LastNameTextBox = new ModernUI.ModernTextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.FirstNameTextBox = new ModernUI.ModernTextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.UserSecretTextBox = new ModernUI.ModernTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.UserRoleComboBox = new ModernUI.ModernComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.panel2.Controls.Add(this.CancelUserEntryButton);
            this.panel2.Controls.Add(this.SaveUserEntryButton);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 278);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(679, 85);
            this.panel2.TabIndex = 47;
            // 
            // CancelUserEntryButton
            // 
            this.CancelUserEntryButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CancelUserEntryButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CancelUserEntryButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(79)))), ((int)(((byte)(95)))));
            this.CancelUserEntryButton.BorderRadius = 18;
            this.CancelUserEntryButton.BorderSize = 1;
            this.CancelUserEntryButton.FlatAppearance.BorderSize = 0;
            this.CancelUserEntryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CancelUserEntryButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelUserEntryButton.ForeColor = System.Drawing.Color.White;
            this.CancelUserEntryButton.Location = new System.Drawing.Point(457, 14);
            this.CancelUserEntryButton.Name = "CancelUserEntryButton";
            this.CancelUserEntryButton.Size = new System.Drawing.Size(158, 53);
            this.CancelUserEntryButton.TabIndex = 9;
            this.CancelUserEntryButton.Text = "ยกเลิก";
            this.CancelUserEntryButton.TextColor = System.Drawing.Color.White;
            this.CancelUserEntryButton.UseVisualStyleBackColor = false;
            this.CancelUserEntryButton.Click += new System.EventHandler(this.CancelUserEntryButton_Click);
            // 
            // SaveUserEntryButton
            // 
            this.SaveUserEntryButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.SaveUserEntryButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.SaveUserEntryButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(190)))), ((int)(((byte)(166)))));
            this.SaveUserEntryButton.BorderRadius = 19;
            this.SaveUserEntryButton.BorderSize = 1;
            this.SaveUserEntryButton.FlatAppearance.BorderSize = 0;
            this.SaveUserEntryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SaveUserEntryButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaveUserEntryButton.ForeColor = System.Drawing.Color.White;
            this.SaveUserEntryButton.Location = new System.Drawing.Point(62, 14);
            this.SaveUserEntryButton.Name = "SaveUserEntryButton";
            this.SaveUserEntryButton.Size = new System.Drawing.Size(158, 53);
            this.SaveUserEntryButton.TabIndex = 8;
            this.SaveUserEntryButton.Text = "บันทึก";
            this.SaveUserEntryButton.TextColor = System.Drawing.Color.White;
            this.SaveUserEntryButton.UseVisualStyleBackColor = false;
            this.SaveUserEntryButton.Click += new System.EventHandler(this.SaveUserEntryButton_Click);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel1.Controls.Add(this.LastNameTextBox);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.FirstNameTextBox);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.UserSecretTextBox);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.UserRoleComboBox);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(1, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(679, 363);
            this.panel1.TabIndex = 1;
            // 
            // LastNameTextBox
            // 
            this.LastNameTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LastNameTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.LastNameTextBox.BorderSize = 1;
            this.LastNameTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LastNameTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.LastNameTextBox.Location = new System.Drawing.Point(269, 101);
            this.LastNameTextBox.Multiline = false;
            this.LastNameTextBox.Name = "LastNameTextBox";
            this.LastNameTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.LastNameTextBox.PasswordChar = false;
            this.LastNameTextBox.ReadOnly = false;
            this.LastNameTextBox.Size = new System.Drawing.Size(272, 39);
            this.LastNameTextBox.TabIndex = 72;
            this.LastNameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.LastNameTextBox.Texts = "";
            this.LastNameTextBox.UnderlinedStyle = true;
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Gainsboro;
            this.label11.Location = new System.Drawing.Point(126, 101);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(137, 39);
            this.label11.TabIndex = 71;
            this.label11.Text = "นามสกุล";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FirstNameTextBox
            // 
            this.FirstNameTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.FirstNameTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.FirstNameTextBox.BorderSize = 1;
            this.FirstNameTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FirstNameTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.FirstNameTextBox.Location = new System.Drawing.Point(269, 56);
            this.FirstNameTextBox.Multiline = false;
            this.FirstNameTextBox.Name = "FirstNameTextBox";
            this.FirstNameTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.FirstNameTextBox.PasswordChar = false;
            this.FirstNameTextBox.ReadOnly = false;
            this.FirstNameTextBox.Size = new System.Drawing.Size(272, 39);
            this.FirstNameTextBox.TabIndex = 70;
            this.FirstNameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.FirstNameTextBox.Texts = "";
            this.FirstNameTextBox.UnderlinedStyle = true;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Gainsboro;
            this.label10.Location = new System.Drawing.Point(126, 56);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(137, 39);
            this.label10.TabIndex = 69;
            this.label10.Text = "ชื่อ";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // UserSecretTextBox
            // 
            this.UserSecretTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserSecretTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.UserSecretTextBox.BorderSize = 1;
            this.UserSecretTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserSecretTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.UserSecretTextBox.Location = new System.Drawing.Point(269, 203);
            this.UserSecretTextBox.Multiline = false;
            this.UserSecretTextBox.Name = "UserSecretTextBox";
            this.UserSecretTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.UserSecretTextBox.PasswordChar = true;
            this.UserSecretTextBox.ReadOnly = false;
            this.UserSecretTextBox.Size = new System.Drawing.Size(272, 39);
            this.UserSecretTextBox.TabIndex = 68;
            this.UserSecretTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.UserSecretTextBox.Texts = "";
            this.UserSecretTextBox.UnderlinedStyle = true;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(126, 203);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(137, 39);
            this.label2.TabIndex = 67;
            this.label2.Text = "รหัสผ่าน";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // UserRoleComboBox
            // 
            this.UserRoleComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserRoleComboBox.BorderColor = System.Drawing.Color.DimGray;
            this.UserRoleComboBox.BorderSize = 0;
            this.UserRoleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.UserRoleComboBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserRoleComboBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.UserRoleComboBox.IconColor = System.Drawing.Color.Gainsboro;
            this.UserRoleComboBox.ListBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserRoleComboBox.ListTextColor = System.Drawing.Color.Gainsboro;
            this.UserRoleComboBox.Location = new System.Drawing.Point(269, 154);
            this.UserRoleComboBox.MinimumSize = new System.Drawing.Size(200, 35);
            this.UserRoleComboBox.Name = "UserRoleComboBox";
            this.UserRoleComboBox.SelectedIndex = -1;
            this.UserRoleComboBox.SelectedItem = null;
            this.UserRoleComboBox.Size = new System.Drawing.Size(272, 35);
            this.UserRoleComboBox.TabIndex = 59;
            this.UserRoleComboBox.Texts = "เลือกประเภทผู้ใช้งาน";
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label7.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.Gainsboro;
            this.label7.Location = new System.Drawing.Point(126, 161);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(137, 28);
            this.label7.TabIndex = 44;
            this.label7.Text = "ประเภทผู้ใช้งาน";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(679, 39);
            this.label1.TabIndex = 38;
            this.label1.Text = "เพิ่มผู้ใช้งาน";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // AddNewUserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SlateGray;
            this.ClientSize = new System.Drawing.Size(681, 365);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "AddNewUserForm";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AddNewUserForm";
            this.panel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private ModernUI.ModernComboBox UserRoleComboBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label1;
        private ModernUI.ModernTextBox LastNameTextBox;
        private System.Windows.Forms.Label label11;
        private ModernUI.ModernTextBox FirstNameTextBox;
        private System.Windows.Forms.Label label10;
        private ModernUI.ModernTextBox UserSecretTextBox;
        private System.Windows.Forms.Label label2;
        private ModernUI.ModernButton SaveUserEntryButton;
        private ModernUI.ModernButton CancelUserEntryButton;
    }
}