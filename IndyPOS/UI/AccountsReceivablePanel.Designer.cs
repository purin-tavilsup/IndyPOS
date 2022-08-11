namespace IndyPOS.UI
{
    partial class AccountsReceivablePanel
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
            this.panel4 = new System.Windows.Forms.Panel();
            this.ArLookUpKeywordTextBox = new ModernUI.ModernTextBox();
            this.LookUpArByInvoiceIdButton = new ModernUI.ModernButton();
            this.ShowIncompleteOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.ShowArButton = new ModernUI.ModernButton();
            this.panel2 = new System.Windows.Forms.Panel();
            this.UpdateArButton = new ModernUI.ModernButton();
            this.ReceivableAmountLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.InvoiceIdLabel = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.PaidAmountTextBox = new ModernUI.ModernTextBox();
            this.PasswordLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.ArDataView = new System.Windows.Forms.DataGridView();
            this.InvoiceId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ReceivableAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaidAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IsCompleted = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateUpdated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ArDataView)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Controls.Add(this.ShowIncompleteOnlyCheckBox);
            this.panel1.Controls.Add(this.ShowArButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1421, 60);
            this.panel1.TabIndex = 0;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.ArLookUpKeywordTextBox);
            this.panel4.Controls.Add(this.LookUpArByInvoiceIdButton);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(1001, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(420, 60);
            this.panel4.TabIndex = 55;
            // 
            // ArLookUpKeywordTextBox
            // 
            this.ArLookUpKeywordTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ArLookUpKeywordTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.ArLookUpKeywordTextBox.BorderSize = 1;
            this.ArLookUpKeywordTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ArLookUpKeywordTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.ArLookUpKeywordTextBox.Location = new System.Drawing.Point(3, 13);
            this.ArLookUpKeywordTextBox.Multiline = false;
            this.ArLookUpKeywordTextBox.Name = "ArLookUpKeywordTextBox";
            this.ArLookUpKeywordTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.ArLookUpKeywordTextBox.PasswordChar = false;
            this.ArLookUpKeywordTextBox.ReadOnly = false;
            this.ArLookUpKeywordTextBox.Size = new System.Drawing.Size(160, 39);
            this.ArLookUpKeywordTextBox.TabIndex = 52;
            this.ArLookUpKeywordTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ArLookUpKeywordTextBox.Texts = "";
            this.ArLookUpKeywordTextBox.UnderlinedStyle = true;
            // 
            // LookUpArByInvoiceIdButton
            // 
            this.LookUpArByInvoiceIdButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LookUpArByInvoiceIdButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LookUpArByInvoiceIdButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(182)))), ((int)(((byte)(210)))));
            this.LookUpArByInvoiceIdButton.BorderRadius = 19;
            this.LookUpArByInvoiceIdButton.BorderSize = 1;
            this.LookUpArByInvoiceIdButton.FlatAppearance.BorderSize = 0;
            this.LookUpArByInvoiceIdButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LookUpArByInvoiceIdButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LookUpArByInvoiceIdButton.ForeColor = System.Drawing.Color.White;
            this.LookUpArByInvoiceIdButton.Location = new System.Drawing.Point(176, 4);
            this.LookUpArByInvoiceIdButton.Name = "LookUpArByInvoiceIdButton";
            this.LookUpArByInvoiceIdButton.Size = new System.Drawing.Size(241, 50);
            this.LookUpArByInvoiceIdButton.TabIndex = 53;
            this.LookUpArByInvoiceIdButton.Text = "ค้นหารายการด้วย Invoice ID";
            this.LookUpArByInvoiceIdButton.TextColor = System.Drawing.Color.White;
            this.LookUpArByInvoiceIdButton.UseVisualStyleBackColor = false;
            this.LookUpArByInvoiceIdButton.Click += new System.EventHandler(this.LookUpArByInvoiceIdButton_Click);
            // 
            // ShowIncompleteOnlyCheckBox
            // 
            this.ShowIncompleteOnlyCheckBox.AutoSize = true;
            this.ShowIncompleteOnlyCheckBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowIncompleteOnlyCheckBox.ForeColor = System.Drawing.Color.White;
            this.ShowIncompleteOnlyCheckBox.Location = new System.Drawing.Point(291, 16);
            this.ShowIncompleteOnlyCheckBox.Name = "ShowIncompleteOnlyCheckBox";
            this.ShowIncompleteOnlyCheckBox.Size = new System.Drawing.Size(247, 28);
            this.ShowIncompleteOnlyCheckBox.TabIndex = 54;
            this.ShowIncompleteOnlyCheckBox.Text = "แสดงเฉพาะรายการที่ยังค้างชำระ";
            this.ShowIncompleteOnlyCheckBox.UseVisualStyleBackColor = true;
            this.ShowIncompleteOnlyCheckBox.Click += new System.EventHandler(this.ShowIncompleteOnlyCheckBox_Click);
            // 
            // ShowArButton
            // 
            this.ShowArButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowArButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowArButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(182)))), ((int)(((byte)(210)))));
            this.ShowArButton.BorderRadius = 19;
            this.ShowArButton.BorderSize = 1;
            this.ShowArButton.FlatAppearance.BorderSize = 0;
            this.ShowArButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowArButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowArButton.ForeColor = System.Drawing.Color.White;
            this.ShowArButton.Location = new System.Drawing.Point(3, 4);
            this.ShowArButton.Name = "ShowArButton";
            this.ShowArButton.Size = new System.Drawing.Size(264, 50);
            this.ShowArButton.TabIndex = 10;
            this.ShowArButton.Text = "แสดงรายการลงบัญชี";
            this.ShowArButton.TextColor = System.Drawing.Color.White;
            this.ShowArButton.UseVisualStyleBackColor = false;
            this.ShowArButton.Click += new System.EventHandler(this.ShowArsButton_Click);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.UpdateArButton);
            this.panel2.Controls.Add(this.ReceivableAmountLabel);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.DescriptionLabel);
            this.panel2.Controls.Add(this.InvoiceIdLabel);
            this.panel2.Controls.Add(this.label11);
            this.panel2.Controls.Add(this.label10);
            this.panel2.Controls.Add(this.PaidAmountTextBox);
            this.panel2.Controls.Add(this.PasswordLabel);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel2.Location = new System.Drawing.Point(1007, 60);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(414, 637);
            this.panel2.TabIndex = 1;
            // 
            // UpdateArButton
            // 
            this.UpdateArButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UpdateArButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UpdateArButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(190)))), ((int)(((byte)(166)))));
            this.UpdateArButton.BorderRadius = 19;
            this.UpdateArButton.BorderSize = 1;
            this.UpdateArButton.FlatAppearance.BorderSize = 0;
            this.UpdateArButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.UpdateArButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UpdateArButton.ForeColor = System.Drawing.Color.White;
            this.UpdateArButton.Location = new System.Drawing.Point(72, 251);
            this.UpdateArButton.Name = "UpdateArButton";
            this.UpdateArButton.Size = new System.Drawing.Size(272, 53);
            this.UpdateArButton.TabIndex = 84;
            this.UpdateArButton.Text = "Update";
            this.UpdateArButton.TextColor = System.Drawing.Color.White;
            this.UpdateArButton.UseVisualStyleBackColor = false;
            this.UpdateArButton.Click += new System.EventHandler(this.UpdateArButton_Click);
            // 
            // ReceivableAmountLabel
            // 
            this.ReceivableAmountLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ReceivableAmountLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReceivableAmountLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.ReceivableAmountLabel.Location = new System.Drawing.Point(138, 151);
            this.ReceivableAmountLabel.Name = "ReceivableAmountLabel";
            this.ReceivableAmountLabel.Size = new System.Drawing.Size(168, 28);
            this.ReceivableAmountLabel.TabIndex = 83;
            this.ReceivableAmountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Gainsboro;
            this.label3.Location = new System.Drawing.Point(22, 151);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(110, 28);
            this.label3.TabIndex = 82;
            this.label3.Text = "ยอดลงบัญชี";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.DescriptionLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DescriptionLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.DescriptionLabel.Location = new System.Drawing.Point(138, 117);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(168, 28);
            this.DescriptionLabel.TabIndex = 81;
            this.DescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // InvoiceIdLabel
            // 
            this.InvoiceIdLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.InvoiceIdLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InvoiceIdLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.InvoiceIdLabel.Location = new System.Drawing.Point(138, 83);
            this.InvoiceIdLabel.Name = "InvoiceIdLabel";
            this.InvoiceIdLabel.Size = new System.Drawing.Size(168, 28);
            this.InvoiceIdLabel.TabIndex = 80;
            this.InvoiceIdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Gainsboro;
            this.label11.Location = new System.Drawing.Point(22, 117);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(110, 28);
            this.label11.TabIndex = 79;
            this.label11.Text = "คำอธิบาย";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Gainsboro;
            this.label10.Location = new System.Drawing.Point(22, 83);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(110, 28);
            this.label10.TabIndex = 78;
            this.label10.Text = "Invoice ID";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // PaidAmountTextBox
            // 
            this.PaidAmountTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PaidAmountTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.PaidAmountTextBox.BorderSize = 1;
            this.PaidAmountTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PaidAmountTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.PaidAmountTextBox.Location = new System.Drawing.Point(138, 179);
            this.PaidAmountTextBox.Multiline = false;
            this.PaidAmountTextBox.Name = "PaidAmountTextBox";
            this.PaidAmountTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.PaidAmountTextBox.PasswordChar = false;
            this.PaidAmountTextBox.ReadOnly = false;
            this.PaidAmountTextBox.Size = new System.Drawing.Size(168, 39);
            this.PaidAmountTextBox.TabIndex = 77;
            this.PaidAmountTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.PaidAmountTextBox.Texts = "";
            this.PaidAmountTextBox.UnderlinedStyle = true;
            // 
            // PasswordLabel
            // 
            this.PasswordLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PasswordLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PasswordLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.PasswordLabel.Location = new System.Drawing.Point(22, 179);
            this.PasswordLabel.Name = "PasswordLabel";
            this.PasswordLabel.Size = new System.Drawing.Size(110, 39);
            this.PasswordLabel.TabIndex = 76;
            this.PasswordLabel.Text = "ยอดชำระ";
            this.PasswordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(412, 52);
            this.label2.TabIndex = 69;
            this.label2.Text = "รายละเอียดการลงบัญชี";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.ArDataView);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 60);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1007, 637);
            this.panel3.TabIndex = 2;
            // 
            // ArDataView
            // 
            this.ArDataView.AllowUserToAddRows = false;
            this.ArDataView.AllowUserToDeleteRows = false;
            this.ArDataView.AllowUserToResizeColumns = false;
            this.ArDataView.AllowUserToResizeRows = false;
            this.ArDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ArDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ArDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.ArDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Gray;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.ArDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.ArDataView.ColumnHeadersHeight = 50;
            this.ArDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.ArDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.InvoiceId,
            this.Description,
            this.ReceivableAmount,
            this.PaidAmount,
            this.IsCompleted,
            this.DateCreated,
            this.DateUpdated});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.ArDataView.DefaultCellStyle = dataGridViewCellStyle2;
            this.ArDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ArDataView.EnableHeadersVisualStyles = false;
            this.ArDataView.GridColor = System.Drawing.Color.DimGray;
            this.ArDataView.Location = new System.Drawing.Point(0, 0);
            this.ArDataView.MultiSelect = false;
            this.ArDataView.Name = "ArDataView";
            this.ArDataView.RowHeadersVisible = false;
            this.ArDataView.RowHeadersWidth = 60;
            this.ArDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.ArDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.ArDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ArDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ArDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
            this.ArDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.ArDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
            this.ArDataView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ArDataView.RowTemplate.Height = 35;
            this.ArDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.ArDataView.Size = new System.Drawing.Size(1007, 637);
            this.ArDataView.TabIndex = 3;
            this.ArDataView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ArDataView_CellClick);
            this.ArDataView.DoubleClick += new System.EventHandler(this.ArDataView_DoubleClick);
            // 
            // InvoiceId
            // 
            this.InvoiceId.HeaderText = "Invoice ID";
            this.InvoiceId.Name = "InvoiceId";
            this.InvoiceId.ReadOnly = true;
            this.InvoiceId.Width = 150;
            // 
            // Description
            // 
            this.Description.HeaderText = "คำอธิบาย";
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 250;
            // 
            // ReceivableAmount
            // 
            this.ReceivableAmount.HeaderText = "ยอดลงบัญชี";
            this.ReceivableAmount.Name = "ReceivableAmount";
            this.ReceivableAmount.ReadOnly = true;
            this.ReceivableAmount.Width = 150;
            // 
            // PaidAmount
            // 
            this.PaidAmount.HeaderText = "ยอดชำระ";
            this.PaidAmount.Name = "PaidAmount";
            this.PaidAmount.Width = 150;
            // 
            // IsCompleted
            // 
            this.IsCompleted.HeaderText = "สถานะ";
            this.IsCompleted.Name = "IsCompleted";
            this.IsCompleted.Width = 150;
            // 
            // DateCreated
            // 
            this.DateCreated.HeaderText = "วันที่สร้าง";
            this.DateCreated.Name = "DateCreated";
            this.DateCreated.ReadOnly = true;
            this.DateCreated.Width = 200;
            // 
            // DateUpdated
            // 
            this.DateUpdated.HeaderText = "วันที่อัพเดท";
            this.DateUpdated.Name = "DateUpdated";
            this.DateUpdated.ReadOnly = true;
            this.DateUpdated.Width = 200;
            // 
            // AccountsReceivablePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "AccountsReceivablePanel";
            this.Size = new System.Drawing.Size(1421, 697);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ArDataView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.DataGridView ArDataView;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridViewTextBoxColumn InvoiceId;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReceivableAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaidAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn IsCompleted;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateUpdated;
        private ModernUI.ModernButton ShowArButton;
        private System.Windows.Forms.Label ReceivableAmountLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Label InvoiceIdLabel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private ModernUI.ModernTextBox PaidAmountTextBox;
        private System.Windows.Forms.Label PasswordLabel;
        private ModernUI.ModernButton UpdateArButton;
        private ModernUI.ModernButton LookUpArByInvoiceIdButton;
        private ModernUI.ModernTextBox ArLookUpKeywordTextBox;
        private System.Windows.Forms.CheckBox ShowIncompleteOnlyCheckBox;
        private System.Windows.Forms.Panel panel4;
    }
}
