namespace IndyPOS.Windows.Forms.UI.PayLater
{
    partial class PayLaterPaymentPanel
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.LookUpByInvoiceIdTextBox = new ModernUI.ModernTextBox();
            this.LookUpByInvoiceIdButton = new ModernUI.ModernButton();
            this.ShowIncompleteOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.ShowPayLaterPaymentsButton = new ModernUI.ModernButton();
            this.panel2 = new System.Windows.Forms.Panel();
            this.UpdateButton = new ModernUI.ModernButton();
            this.AmountLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.InvoiceIdLabel = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.PaidAmountTextBox = new ModernUI.ModernTextBox();
            this.PasswordLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.PayLaterPaymentsDataView = new System.Windows.Forms.DataGridView();
            this.InvoiceId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ReceivableAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaidAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IsCompleted = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateUpdated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaymentIdLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PayLaterPaymentsDataView)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Controls.Add(this.ShowIncompleteOnlyCheckBox);
            this.panel1.Controls.Add(this.ShowPayLaterPaymentsButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1421, 60);
            this.panel1.TabIndex = 0;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.LookUpByInvoiceIdTextBox);
            this.panel4.Controls.Add(this.LookUpByInvoiceIdButton);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(1001, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(420, 60);
            this.panel4.TabIndex = 55;
            // 
            // LookUpByInvoiceIdTextBox
            // 
            this.LookUpByInvoiceIdTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LookUpByInvoiceIdTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.LookUpByInvoiceIdTextBox.BorderSize = 1;
            this.LookUpByInvoiceIdTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LookUpByInvoiceIdTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.LookUpByInvoiceIdTextBox.Location = new System.Drawing.Point(3, 13);
            this.LookUpByInvoiceIdTextBox.Multiline = false;
            this.LookUpByInvoiceIdTextBox.Name = "LookUpByInvoiceIdTextBox";
            this.LookUpByInvoiceIdTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.LookUpByInvoiceIdTextBox.PasswordChar = false;
            this.LookUpByInvoiceIdTextBox.ReadOnly = false;
            this.LookUpByInvoiceIdTextBox.Size = new System.Drawing.Size(160, 39);
            this.LookUpByInvoiceIdTextBox.TabIndex = 52;
            this.LookUpByInvoiceIdTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.LookUpByInvoiceIdTextBox.Texts = "";
            this.LookUpByInvoiceIdTextBox.UnderlinedStyle = true;
            // 
            // LookUpByInvoiceIdButton
            // 
            this.LookUpByInvoiceIdButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LookUpByInvoiceIdButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LookUpByInvoiceIdButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(182)))), ((int)(((byte)(210)))));
            this.LookUpByInvoiceIdButton.BorderRadius = 19;
            this.LookUpByInvoiceIdButton.BorderSize = 1;
            this.LookUpByInvoiceIdButton.FlatAppearance.BorderSize = 0;
            this.LookUpByInvoiceIdButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LookUpByInvoiceIdButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LookUpByInvoiceIdButton.ForeColor = System.Drawing.Color.White;
            this.LookUpByInvoiceIdButton.Location = new System.Drawing.Point(176, 4);
            this.LookUpByInvoiceIdButton.Name = "LookUpByInvoiceIdButton";
            this.LookUpByInvoiceIdButton.Size = new System.Drawing.Size(241, 50);
            this.LookUpByInvoiceIdButton.TabIndex = 53;
            this.LookUpByInvoiceIdButton.Text = "ค้นหารายการด้วย Invoice ID";
            this.LookUpByInvoiceIdButton.TextColor = System.Drawing.Color.White;
            this.LookUpByInvoiceIdButton.UseVisualStyleBackColor = false;
            this.LookUpByInvoiceIdButton.Click += new System.EventHandler(this.LookUpByInvoiceIdButton_Click);
            // 
            // ShowIncompleteOnlyCheckBox
            // 
            this.ShowIncompleteOnlyCheckBox.AutoSize = true;
            this.ShowIncompleteOnlyCheckBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ShowIncompleteOnlyCheckBox.ForeColor = System.Drawing.Color.White;
            this.ShowIncompleteOnlyCheckBox.Location = new System.Drawing.Point(291, 16);
            this.ShowIncompleteOnlyCheckBox.Name = "ShowIncompleteOnlyCheckBox";
            this.ShowIncompleteOnlyCheckBox.Size = new System.Drawing.Size(247, 28);
            this.ShowIncompleteOnlyCheckBox.TabIndex = 54;
            this.ShowIncompleteOnlyCheckBox.Text = "แสดงเฉพาะรายการที่ยังค้างชำระ";
            this.ShowIncompleteOnlyCheckBox.UseVisualStyleBackColor = true;
            this.ShowIncompleteOnlyCheckBox.Click += new System.EventHandler(this.ShowIncompleteOnlyCheckBox_Click);
            // 
            // ShowPayLaterPaymentsButton
            // 
            this.ShowPayLaterPaymentsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowPayLaterPaymentsButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowPayLaterPaymentsButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(182)))), ((int)(((byte)(210)))));
            this.ShowPayLaterPaymentsButton.BorderRadius = 19;
            this.ShowPayLaterPaymentsButton.BorderSize = 1;
            this.ShowPayLaterPaymentsButton.FlatAppearance.BorderSize = 0;
            this.ShowPayLaterPaymentsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowPayLaterPaymentsButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ShowPayLaterPaymentsButton.ForeColor = System.Drawing.Color.White;
            this.ShowPayLaterPaymentsButton.Location = new System.Drawing.Point(3, 4);
            this.ShowPayLaterPaymentsButton.Name = "ShowPayLaterPaymentsButton";
            this.ShowPayLaterPaymentsButton.Size = new System.Drawing.Size(264, 50);
            this.ShowPayLaterPaymentsButton.TabIndex = 10;
            this.ShowPayLaterPaymentsButton.Text = "แสดงรายการลงบัญชี";
            this.ShowPayLaterPaymentsButton.TextColor = System.Drawing.Color.White;
            this.ShowPayLaterPaymentsButton.UseVisualStyleBackColor = false;
            this.ShowPayLaterPaymentsButton.Click += new System.EventHandler(this.ShowPayLaterPaymentsButton_Click);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.PaymentIdLabel);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.UpdateButton);
            this.panel2.Controls.Add(this.AmountLabel);
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
            // UpdateButton
            // 
            this.UpdateButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UpdateButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UpdateButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(190)))), ((int)(((byte)(166)))));
            this.UpdateButton.BorderRadius = 19;
            this.UpdateButton.BorderSize = 1;
            this.UpdateButton.FlatAppearance.BorderSize = 0;
            this.UpdateButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.UpdateButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.UpdateButton.ForeColor = System.Drawing.Color.White;
            this.UpdateButton.Location = new System.Drawing.Point(72, 267);
            this.UpdateButton.Name = "UpdateButton";
            this.UpdateButton.Size = new System.Drawing.Size(272, 53);
            this.UpdateButton.TabIndex = 84;
            this.UpdateButton.Text = "Update";
            this.UpdateButton.TextColor = System.Drawing.Color.White;
            this.UpdateButton.UseVisualStyleBackColor = false;
            this.UpdateButton.Click += new System.EventHandler(this.UpdateArButton_Click);
            // 
            // AmountLabel
            // 
            this.AmountLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AmountLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.AmountLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.AmountLabel.Location = new System.Drawing.Point(138, 167);
            this.AmountLabel.Name = "AmountLabel";
            this.AmountLabel.Size = new System.Drawing.Size(168, 28);
            this.AmountLabel.TabIndex = 83;
            this.AmountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label3.ForeColor = System.Drawing.Color.Gainsboro;
            this.label3.Location = new System.Drawing.Point(22, 167);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(110, 28);
            this.label3.TabIndex = 82;
            this.label3.Text = "ยอดลงบัญชี";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.DescriptionLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.DescriptionLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.DescriptionLabel.Location = new System.Drawing.Point(138, 133);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(168, 28);
            this.DescriptionLabel.TabIndex = 81;
            this.DescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // InvoiceIdLabel
            // 
            this.InvoiceIdLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.InvoiceIdLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.InvoiceIdLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.InvoiceIdLabel.Location = new System.Drawing.Point(138, 99);
            this.InvoiceIdLabel.Name = "InvoiceIdLabel";
            this.InvoiceIdLabel.Size = new System.Drawing.Size(168, 28);
            this.InvoiceIdLabel.TabIndex = 80;
            this.InvoiceIdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label11.ForeColor = System.Drawing.Color.Gainsboro;
            this.label11.Location = new System.Drawing.Point(22, 133);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(110, 28);
            this.label11.TabIndex = 79;
            this.label11.Text = "คำอธิบาย";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label10.ForeColor = System.Drawing.Color.Gainsboro;
            this.label10.Location = new System.Drawing.Point(22, 99);
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
            this.PaidAmountTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.PaidAmountTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.PaidAmountTextBox.Location = new System.Drawing.Point(138, 195);
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
            this.PasswordLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.PasswordLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.PasswordLabel.Location = new System.Drawing.Point(22, 195);
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
            this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
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
            this.panel3.Controls.Add(this.PayLaterPaymentsDataView);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 60);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1007, 637);
            this.panel3.TabIndex = 2;
            // 
            // PayLaterPaymentsDataView
            // 
            this.PayLaterPaymentsDataView.AllowUserToAddRows = false;
            this.PayLaterPaymentsDataView.AllowUserToDeleteRows = false;
            this.PayLaterPaymentsDataView.AllowUserToResizeColumns = false;
            this.PayLaterPaymentsDataView.AllowUserToResizeRows = false;
            this.PayLaterPaymentsDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PayLaterPaymentsDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.PayLaterPaymentsDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.PayLaterPaymentsDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.Gray;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PayLaterPaymentsDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.PayLaterPaymentsDataView.ColumnHeadersHeight = 50;
            this.PayLaterPaymentsDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.PayLaterPaymentsDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.InvoiceId,
            this.Description,
            this.ReceivableAmount,
            this.PaidAmount,
            this.IsCompleted,
            this.DateCreated,
            this.DateUpdated});
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PayLaterPaymentsDataView.DefaultCellStyle = dataGridViewCellStyle4;
            this.PayLaterPaymentsDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PayLaterPaymentsDataView.EnableHeadersVisualStyles = false;
            this.PayLaterPaymentsDataView.GridColor = System.Drawing.Color.DimGray;
            this.PayLaterPaymentsDataView.Location = new System.Drawing.Point(0, 0);
            this.PayLaterPaymentsDataView.MultiSelect = false;
            this.PayLaterPaymentsDataView.Name = "PayLaterPaymentsDataView";
            this.PayLaterPaymentsDataView.RowHeadersVisible = false;
            this.PayLaterPaymentsDataView.RowHeadersWidth = 60;
            this.PayLaterPaymentsDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
            this.PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
            this.PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.PayLaterPaymentsDataView.RowTemplate.Height = 35;
            this.PayLaterPaymentsDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.PayLaterPaymentsDataView.Size = new System.Drawing.Size(1007, 637);
            this.PayLaterPaymentsDataView.TabIndex = 3;
            this.PayLaterPaymentsDataView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.PayLaterPaymentsDataView_CellClick);
            this.PayLaterPaymentsDataView.DoubleClick += new System.EventHandler(this.PayLaterPaymentsDataView_DoubleClick);
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
            // PaymentIdLabel
            // 
            this.PaymentIdLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PaymentIdLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.PaymentIdLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.PaymentIdLabel.Location = new System.Drawing.Point(138, 71);
            this.PaymentIdLabel.Name = "PaymentIdLabel";
            this.PaymentIdLabel.Size = new System.Drawing.Size(168, 28);
            this.PaymentIdLabel.TabIndex = 86;
            this.PaymentIdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label4.ForeColor = System.Drawing.Color.Gainsboro;
            this.label4.Location = new System.Drawing.Point(22, 71);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(110, 28);
            this.label4.TabIndex = 85;
            this.label4.Text = "Payment ID";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // PayLaterPaymentPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "PayLaterPaymentPanel";
            this.Size = new System.Drawing.Size(1421, 697);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PayLaterPaymentsDataView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.DataGridView PayLaterPaymentsDataView;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridViewTextBoxColumn InvoiceId;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReceivableAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaidAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn IsCompleted;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateUpdated;
        private ModernUI.ModernButton ShowPayLaterPaymentsButton;
        private System.Windows.Forms.Label AmountLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Label InvoiceIdLabel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private ModernUI.ModernTextBox PaidAmountTextBox;
        private System.Windows.Forms.Label PasswordLabel;
        private ModernUI.ModernButton UpdateButton;
        private ModernUI.ModernButton LookUpByInvoiceIdButton;
        private ModernUI.ModernTextBox LookUpByInvoiceIdTextBox;
        private System.Windows.Forms.CheckBox ShowIncompleteOnlyCheckBox;
        private System.Windows.Forms.Panel panel4;
        private Label PaymentIdLabel;
        private Label label4;
    }
}
