namespace IndyPOS.UI
{
    partial class UsersPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.UserDataView = new System.Windows.Forms.DataGridView();
            this.UserId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FirstName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UserRole = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateUpdated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel3 = new System.Windows.Forms.Panel();
            this.UpdateUserButton = new System.Windows.Forms.Button();
            this.UserRoleLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.LastNameTextBox = new ModernUI.ModernTextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.FirstNameTextBox = new ModernUI.ModernTextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.UserSecretTextBox = new ModernUI.ModernTextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.AddUserButton = new System.Windows.Forms.Button();
            this.UserRoleComboBox = new ModernUI.ModernComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UserDataView)).BeginInit();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel1.Controls.Add(this.UserDataView);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(3);
            this.panel1.Size = new System.Drawing.Size(1421, 697);
            this.panel1.TabIndex = 1;
            // 
            // UserDataView
            // 
            this.UserDataView.AllowUserToAddRows = false;
            this.UserDataView.AllowUserToDeleteRows = false;
            this.UserDataView.AllowUserToResizeColumns = false;
            this.UserDataView.AllowUserToResizeRows = false;
            this.UserDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.UserDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.UserDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Gray;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.UserDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.UserDataView.ColumnHeadersHeight = 50;
            this.UserDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.UserDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.UserId,
            this.FirstName,
            this.LastName,
            this.UserRole,
            this.DateCreated,
            this.DateUpdated});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.UserDataView.DefaultCellStyle = dataGridViewCellStyle2;
            this.UserDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UserDataView.EnableHeadersVisualStyles = false;
            this.UserDataView.GridColor = System.Drawing.Color.DimGray;
            this.UserDataView.Location = new System.Drawing.Point(3, 63);
            this.UserDataView.MultiSelect = false;
            this.UserDataView.Name = "UserDataView";
            this.UserDataView.RowHeadersVisible = false;
            this.UserDataView.RowHeadersWidth = 60;
            this.UserDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.UserDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.UserDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
            this.UserDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.UserDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
            this.UserDataView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.UserDataView.RowTemplate.Height = 35;
            this.UserDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.UserDataView.Size = new System.Drawing.Size(926, 631);
            this.UserDataView.TabIndex = 2;
            this.UserDataView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.UserDataView_CellClick);
            // 
            // UserId
            // 
            this.UserId.HeaderText = "หมายเลขผู้ใช้งาน";
            this.UserId.Name = "UserId";
            this.UserId.ReadOnly = true;
            this.UserId.Width = 170;
            // 
            // FirstName
            // 
            this.FirstName.HeaderText = "ชื่อ";
            this.FirstName.Name = "FirstName";
            this.FirstName.ReadOnly = true;
            this.FirstName.Width = 150;
            // 
            // LastName
            // 
            this.LastName.HeaderText = "นามสกุล";
            this.LastName.Name = "LastName";
            this.LastName.ReadOnly = true;
            this.LastName.Width = 150;
            // 
            // UserRole
            // 
            this.UserRole.HeaderText = "ประเภทผู้ใช้งาน";
            this.UserRole.Name = "UserRole";
            this.UserRole.ReadOnly = true;
            this.UserRole.Width = 150;
            // 
            // DateCreated
            // 
            this.DateCreated.HeaderText = "วันที่สร้าง";
            this.DateCreated.Name = "DateCreated";
            this.DateCreated.ReadOnly = true;
            this.DateCreated.Width = 150;
            // 
            // DateUpdated
            // 
            this.DateUpdated.HeaderText = "วันที่อัพเดท";
            this.DateUpdated.Name = "DateUpdated";
            this.DateUpdated.ReadOnly = true;
            this.DateUpdated.Width = 150;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.UpdateUserButton);
            this.panel3.Controls.Add(this.UserRoleLabel);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Controls.Add(this.LastNameTextBox);
            this.panel3.Controls.Add(this.label11);
            this.panel3.Controls.Add(this.FirstNameTextBox);
            this.panel3.Controls.Add(this.label10);
            this.panel3.Controls.Add(this.UserSecretTextBox);
            this.panel3.Controls.Add(this.label8);
            this.panel3.Controls.Add(this.label7);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(929, 63);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(489, 631);
            this.panel3.TabIndex = 4;
            // 
            // UpdateUserButton
            // 
            this.UpdateUserButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UpdateUserButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.UpdateUserButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UpdateUserButton.ForeColor = System.Drawing.Color.Gainsboro;
            this.UpdateUserButton.Image = global::IndyPOS.Properties.Resources.Check_35;
            this.UpdateUserButton.Location = new System.Drawing.Point(32, 285);
            this.UpdateUserButton.Name = "UpdateUserButton";
            this.UpdateUserButton.Size = new System.Drawing.Size(261, 50);
            this.UpdateUserButton.TabIndex = 70;
            this.UpdateUserButton.Text = "   อัพเดทผู้ใช้งาน";
            this.UpdateUserButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.UpdateUserButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.UpdateUserButton.UseVisualStyleBackColor = false;
            this.UpdateUserButton.Click += new System.EventHandler(this.UpdateUserButton_Click);
            // 
            // UserRoleLabel
            // 
            this.UserRoleLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserRoleLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserRoleLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.UserRoleLabel.Location = new System.Drawing.Point(171, 169);
            this.UserRoleLabel.Name = "UserRoleLabel";
            this.UserRoleLabel.Size = new System.Drawing.Size(272, 28);
            this.UserRoleLabel.TabIndex = 69;
            this.UserRoleLabel.Text = "ประเภทผู้ใช้งาน";
            this.UserRoleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(487, 52);
            this.label2.TabIndex = 68;
            this.label2.Text = "รายละเอียดผู้ใช้งาน";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // LastNameTextBox
            // 
            this.LastNameTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LastNameTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.LastNameTextBox.BorderSize = 1;
            this.LastNameTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LastNameTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.LastNameTextBox.Location = new System.Drawing.Point(171, 115);
            this.LastNameTextBox.Multiline = false;
            this.LastNameTextBox.Name = "LastNameTextBox";
            this.LastNameTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.LastNameTextBox.PasswordChar = false;
            this.LastNameTextBox.ReadOnly = false;
            this.LastNameTextBox.Size = new System.Drawing.Size(272, 39);
            this.LastNameTextBox.TabIndex = 66;
            this.LastNameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.LastNameTextBox.Texts = "";
            this.LastNameTextBox.UnderlinedStyle = true;
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Gainsboro;
            this.label11.Location = new System.Drawing.Point(28, 115);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(137, 39);
            this.label11.TabIndex = 65;
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
            this.FirstNameTextBox.Location = new System.Drawing.Point(171, 70);
            this.FirstNameTextBox.Multiline = false;
            this.FirstNameTextBox.Name = "FirstNameTextBox";
            this.FirstNameTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.FirstNameTextBox.PasswordChar = false;
            this.FirstNameTextBox.ReadOnly = false;
            this.FirstNameTextBox.Size = new System.Drawing.Size(272, 39);
            this.FirstNameTextBox.TabIndex = 64;
            this.FirstNameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.FirstNameTextBox.Texts = "";
            this.FirstNameTextBox.UnderlinedStyle = true;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Gainsboro;
            this.label10.Location = new System.Drawing.Point(28, 70);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(137, 39);
            this.label10.TabIndex = 63;
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
            this.UserSecretTextBox.Location = new System.Drawing.Point(171, 209);
            this.UserSecretTextBox.Multiline = false;
            this.UserSecretTextBox.Name = "UserSecretTextBox";
            this.UserSecretTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.UserSecretTextBox.PasswordChar = true;
            this.UserSecretTextBox.ReadOnly = false;
            this.UserSecretTextBox.Size = new System.Drawing.Size(272, 39);
            this.UserSecretTextBox.TabIndex = 62;
            this.UserSecretTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.UserSecretTextBox.Texts = "";
            this.UserSecretTextBox.UnderlinedStyle = true;
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label8.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.Gainsboro;
            this.label8.Location = new System.Drawing.Point(28, 209);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(137, 39);
            this.label8.TabIndex = 61;
            this.label8.Text = "รหัสผ่าน";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label7.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.Gainsboro;
            this.label7.Location = new System.Drawing.Point(28, 169);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(137, 28);
            this.label7.TabIndex = 60;
            this.label7.Text = "ประเภทผู้ใช้งาน";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel4.Controls.Add(this.panel2);
            this.panel4.Controls.Add(this.UserRoleComboBox);
            this.panel4.Controls.Add(this.label1);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(3, 3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1415, 60);
            this.panel4.TabIndex = 3;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.AddUserButton);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel2.Location = new System.Drawing.Point(1145, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(270, 60);
            this.panel2.TabIndex = 8;
            // 
            // AddUserButton
            // 
            this.AddUserButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddUserButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddUserButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddUserButton.ForeColor = System.Drawing.Color.Gainsboro;
            this.AddUserButton.Image = global::IndyPOS.Properties.Resources.Plus_35;
            this.AddUserButton.Location = new System.Drawing.Point(3, 4);
            this.AddUserButton.Name = "AddUserButton";
            this.AddUserButton.Size = new System.Drawing.Size(261, 50);
            this.AddUserButton.TabIndex = 7;
            this.AddUserButton.Text = "   เพิ่มผู้ใช้งาน";
            this.AddUserButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AddUserButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.AddUserButton.UseVisualStyleBackColor = false;
            this.AddUserButton.Click += new System.EventHandler(this.AddUserButton_Click);
            // 
            // UserRoleComboBox
            // 
            this.UserRoleComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserRoleComboBox.BorderColor = System.Drawing.Color.DimGray;
            this.UserRoleComboBox.BorderSize = 1;
            this.UserRoleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.UserRoleComboBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserRoleComboBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.UserRoleComboBox.IconColor = System.Drawing.Color.Gainsboro;
            this.UserRoleComboBox.ListBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UserRoleComboBox.ListTextColor = System.Drawing.Color.Gainsboro;
            this.UserRoleComboBox.Location = new System.Drawing.Point(184, 3);
            this.UserRoleComboBox.MinimumSize = new System.Drawing.Size(200, 35);
            this.UserRoleComboBox.Name = "UserRoleComboBox";
            this.UserRoleComboBox.Padding = new System.Windows.Forms.Padding(1);
            this.UserRoleComboBox.SelectedIndex = -1;
            this.UserRoleComboBox.SelectedItem = null;
            this.UserRoleComboBox.Size = new System.Drawing.Size(300, 54);
            this.UserRoleComboBox.TabIndex = 0;
            this.UserRoleComboBox.Texts = "เลือกประเภทผู้ใช้งาน";
            this.UserRoleComboBox.SelectedIndexChanged += new System.EventHandler(this.UserRoleComboBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Gainsboro;
            this.label1.Location = new System.Drawing.Point(3, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(175, 35);
            this.label1.TabIndex = 3;
            this.label1.Text = "ประเภทผู้ใช้งาน";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // UsersPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.Controls.Add(this.panel1);
            this.Name = "UsersPanel";
            this.Size = new System.Drawing.Size(1421, 697);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.UserDataView)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.DataGridView UserDataView;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Button AddUserButton;
		private ModernUI.ModernComboBox UserRoleComboBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel3;
		private ModernUI.ModernTextBox LastNameTextBox;
		private System.Windows.Forms.Label label11;
		private ModernUI.ModernTextBox FirstNameTextBox;
		private System.Windows.Forms.Label label10;
		private ModernUI.ModernTextBox UserSecretTextBox;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label UserRoleLabel;
		private System.Windows.Forms.DataGridViewTextBoxColumn UserId;
		private System.Windows.Forms.DataGridViewTextBoxColumn FirstName;
		private System.Windows.Forms.DataGridViewTextBoxColumn LastName;
		private System.Windows.Forms.DataGridViewTextBoxColumn UserRole;
		private System.Windows.Forms.DataGridViewTextBoxColumn DateCreated;
		private System.Windows.Forms.DataGridViewTextBoxColumn DateUpdated;
        private System.Windows.Forms.Button UpdateUserButton;
    }
}
