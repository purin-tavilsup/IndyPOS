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
            var dataGridViewCellStyle3 = new DataGridViewCellStyle();
            var dataGridViewCellStyle4 = new DataGridViewCellStyle();
            panel1 = new Panel();
            panel4 = new Panel();
            SearchByKeywordTextBox = new ModernUI.ModernTextBox();
            SearchByKeywordButton = new ModernUI.ModernButton();
            ShowIncompleteOnlyCheckBox = new CheckBox();
            ShowPayLaterPaymentsButton = new ModernUI.ModernButton();
            panel2 = new Panel();
            PaymentIdLabel = new Label();
            label4 = new Label();
            UpdateButton = new ModernUI.ModernButton();
            AmountLabel = new Label();
            label3 = new Label();
            DescriptionLabel = new Label();
            InvoiceIdLabel = new Label();
            label11 = new Label();
            label10 = new Label();
            PaidAmountTextBox = new ModernUI.ModernTextBox();
            PasswordLabel = new Label();
            label2 = new Label();
            ActivePanel = new Panel();
            PayLaterPaymentsDataView = new DataGridView();
            InvoiceId = new DataGridViewTextBoxColumn();
            Description = new DataGridViewTextBoxColumn();
            ReceivableAmount = new DataGridViewTextBoxColumn();
            PaidAmount = new DataGridViewTextBoxColumn();
            IsCompleted = new DataGridViewTextBoxColumn();
            DateCreated = new DataGridViewTextBoxColumn();
            DateUpdated = new DataGridViewTextBoxColumn();
            panel1.SuspendLayout();
            panel4.SuspendLayout();
            panel2.SuspendLayout();
            ActivePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PayLaterPaymentsDataView).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(panel4);
            panel1.Controls.Add(ShowIncompleteOnlyCheckBox);
            panel1.Controls.Add(ShowPayLaterPaymentsButton);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1421, 60);
            panel1.TabIndex = 0;
            // 
            // panel4
            // 
            panel4.Controls.Add(SearchByKeywordTextBox);
            panel4.Controls.Add(SearchByKeywordButton);
            panel4.Dock = DockStyle.Right;
            panel4.Location = new Point(1001, 0);
            panel4.Name = "panel4";
            panel4.Size = new Size(420, 60);
            panel4.TabIndex = 55;
            // 
            // SearchByKeywordTextBox
            // 
            SearchByKeywordTextBox.BackColor = Color.FromArgb(38, 38, 38);
            SearchByKeywordTextBox.BorderColor = Color.DimGray;
            SearchByKeywordTextBox.BorderSize = 1;
            SearchByKeywordTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            SearchByKeywordTextBox.ForeColor = Color.Gainsboro;
            SearchByKeywordTextBox.Location = new Point(3, 13);
            SearchByKeywordTextBox.Multiline = false;
            SearchByKeywordTextBox.Name = "SearchByKeywordTextBox";
            SearchByKeywordTextBox.Padding = new Padding(7);
            SearchByKeywordTextBox.PasswordChar = false;
            SearchByKeywordTextBox.ReadOnly = false;
            SearchByKeywordTextBox.Size = new Size(160, 39);
            SearchByKeywordTextBox.TabIndex = 52;
            SearchByKeywordTextBox.TextAlign = HorizontalAlignment.Center;
            SearchByKeywordTextBox.Texts = "";
            SearchByKeywordTextBox.UnderlinedStyle = true;
            // 
            // SearchByKeywordButton
            // 
            SearchByKeywordButton.BackColor = Color.FromArgb(38, 38, 38);
            SearchByKeywordButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            SearchByKeywordButton.BorderColor = Color.FromArgb(37, 182, 210);
            SearchByKeywordButton.BorderRadius = 19;
            SearchByKeywordButton.BorderSize = 1;
            SearchByKeywordButton.FlatAppearance.BorderSize = 0;
            SearchByKeywordButton.FlatStyle = FlatStyle.Flat;
            SearchByKeywordButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            SearchByKeywordButton.ForeColor = Color.White;
            SearchByKeywordButton.Location = new Point(176, 4);
            SearchByKeywordButton.Name = "SearchByKeywordButton";
            SearchByKeywordButton.Size = new Size(241, 50);
            SearchByKeywordButton.TabIndex = 53;
            SearchByKeywordButton.Text = "ค้นหารายการด้วย คำอธิบาย";
            SearchByKeywordButton.TextColor = Color.White;
            SearchByKeywordButton.UseVisualStyleBackColor = false;
            SearchByKeywordButton.Click += SearchByKeywordButton_Click;
            // 
            // ShowIncompleteOnlyCheckBox
            // 
            ShowIncompleteOnlyCheckBox.AutoSize = true;
            ShowIncompleteOnlyCheckBox.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ShowIncompleteOnlyCheckBox.ForeColor = Color.White;
            ShowIncompleteOnlyCheckBox.Location = new Point(291, 16);
            ShowIncompleteOnlyCheckBox.Name = "ShowIncompleteOnlyCheckBox";
            ShowIncompleteOnlyCheckBox.Size = new Size(247, 28);
            ShowIncompleteOnlyCheckBox.TabIndex = 54;
            ShowIncompleteOnlyCheckBox.Text = "แสดงเฉพาะรายการที่ยังค้างชำระ";
            ShowIncompleteOnlyCheckBox.UseVisualStyleBackColor = true;
            ShowIncompleteOnlyCheckBox.Click += ShowIncompleteOnlyCheckBox_Click;
            // 
            // ShowPayLaterPaymentsButton
            // 
            ShowPayLaterPaymentsButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowPayLaterPaymentsButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowPayLaterPaymentsButton.BorderColor = Color.FromArgb(37, 182, 210);
            ShowPayLaterPaymentsButton.BorderRadius = 19;
            ShowPayLaterPaymentsButton.BorderSize = 1;
            ShowPayLaterPaymentsButton.FlatAppearance.BorderSize = 0;
            ShowPayLaterPaymentsButton.FlatStyle = FlatStyle.Flat;
            ShowPayLaterPaymentsButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ShowPayLaterPaymentsButton.ForeColor = Color.White;
            ShowPayLaterPaymentsButton.Location = new Point(3, 4);
            ShowPayLaterPaymentsButton.Name = "ShowPayLaterPaymentsButton";
            ShowPayLaterPaymentsButton.Size = new Size(264, 50);
            ShowPayLaterPaymentsButton.TabIndex = 10;
            ShowPayLaterPaymentsButton.Text = "แสดงรายการลงบัญชี";
            ShowPayLaterPaymentsButton.TextColor = Color.White;
            ShowPayLaterPaymentsButton.UseVisualStyleBackColor = false;
            ShowPayLaterPaymentsButton.Click += ShowPayLaterPaymentsButton_Click;
            // 
            // panel2
            // 
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(PaymentIdLabel);
            panel2.Controls.Add(label4);
            panel2.Controls.Add(UpdateButton);
            panel2.Controls.Add(AmountLabel);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(DescriptionLabel);
            panel2.Controls.Add(InvoiceIdLabel);
            panel2.Controls.Add(label11);
            panel2.Controls.Add(label10);
            panel2.Controls.Add(PaidAmountTextBox);
            panel2.Controls.Add(PasswordLabel);
            panel2.Controls.Add(label2);
            panel2.Dock = DockStyle.Right;
            panel2.Location = new Point(1007, 60);
            panel2.Name = "panel2";
            panel2.Size = new Size(414, 637);
            panel2.TabIndex = 1;
            // 
            // PaymentIdLabel
            // 
            PaymentIdLabel.BackColor = Color.FromArgb(38, 38, 38);
            PaymentIdLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            PaymentIdLabel.ForeColor = Color.Gainsboro;
            PaymentIdLabel.Location = new Point(138, 71);
            PaymentIdLabel.Name = "PaymentIdLabel";
            PaymentIdLabel.Size = new Size(168, 28);
            PaymentIdLabel.TabIndex = 86;
            PaymentIdLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.BackColor = Color.FromArgb(38, 38, 38);
            label4.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label4.ForeColor = Color.Gainsboro;
            label4.Location = new Point(22, 71);
            label4.Name = "label4";
            label4.Size = new Size(110, 28);
            label4.TabIndex = 85;
            label4.Text = "Payment ID";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // UpdateButton
            // 
            UpdateButton.BackColor = Color.FromArgb(38, 38, 38);
            UpdateButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            UpdateButton.BorderColor = Color.FromArgb(50, 190, 166);
            UpdateButton.BorderRadius = 19;
            UpdateButton.BorderSize = 1;
            UpdateButton.FlatAppearance.BorderSize = 0;
            UpdateButton.FlatStyle = FlatStyle.Flat;
            UpdateButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            UpdateButton.ForeColor = Color.White;
            UpdateButton.Location = new Point(72, 267);
            UpdateButton.Name = "UpdateButton";
            UpdateButton.Size = new Size(272, 53);
            UpdateButton.TabIndex = 84;
            UpdateButton.Text = "Update";
            UpdateButton.TextColor = Color.White;
            UpdateButton.UseVisualStyleBackColor = false;
            UpdateButton.Click += UpdateArButton_Click;
            // 
            // AmountLabel
            // 
            AmountLabel.BackColor = Color.FromArgb(38, 38, 38);
            AmountLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            AmountLabel.ForeColor = Color.Gainsboro;
            AmountLabel.Location = new Point(138, 167);
            AmountLabel.Name = "AmountLabel";
            AmountLabel.Size = new Size(168, 28);
            AmountLabel.TabIndex = 83;
            AmountLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.BackColor = Color.FromArgb(38, 38, 38);
            label3.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label3.ForeColor = Color.Gainsboro;
            label3.Location = new Point(22, 167);
            label3.Name = "label3";
            label3.Size = new Size(110, 28);
            label3.TabIndex = 82;
            label3.Text = "ยอดลงบัญชี";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // DescriptionLabel
            // 
            DescriptionLabel.BackColor = Color.FromArgb(38, 38, 38);
            DescriptionLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            DescriptionLabel.ForeColor = Color.Gainsboro;
            DescriptionLabel.Location = new Point(138, 133);
            DescriptionLabel.Name = "DescriptionLabel";
            DescriptionLabel.Size = new Size(168, 28);
            DescriptionLabel.TabIndex = 81;
            DescriptionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // InvoiceIdLabel
            // 
            InvoiceIdLabel.BackColor = Color.FromArgb(38, 38, 38);
            InvoiceIdLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            InvoiceIdLabel.ForeColor = Color.Gainsboro;
            InvoiceIdLabel.Location = new Point(138, 99);
            InvoiceIdLabel.Name = "InvoiceIdLabel";
            InvoiceIdLabel.Size = new Size(168, 28);
            InvoiceIdLabel.TabIndex = 80;
            InvoiceIdLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            label11.BackColor = Color.FromArgb(38, 38, 38);
            label11.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label11.ForeColor = Color.Gainsboro;
            label11.Location = new Point(22, 133);
            label11.Name = "label11";
            label11.Size = new Size(110, 28);
            label11.TabIndex = 79;
            label11.Text = "คำอธิบาย";
            label11.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            label10.BackColor = Color.FromArgb(38, 38, 38);
            label10.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label10.ForeColor = Color.Gainsboro;
            label10.Location = new Point(22, 99);
            label10.Name = "label10";
            label10.Size = new Size(110, 28);
            label10.TabIndex = 78;
            label10.Text = "Invoice ID";
            label10.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // PaidAmountTextBox
            // 
            PaidAmountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            PaidAmountTextBox.BorderColor = Color.DimGray;
            PaidAmountTextBox.BorderSize = 1;
            PaidAmountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            PaidAmountTextBox.ForeColor = Color.Gainsboro;
            PaidAmountTextBox.Location = new Point(138, 195);
            PaidAmountTextBox.Multiline = false;
            PaidAmountTextBox.Name = "PaidAmountTextBox";
            PaidAmountTextBox.Padding = new Padding(7);
            PaidAmountTextBox.PasswordChar = false;
            PaidAmountTextBox.ReadOnly = false;
            PaidAmountTextBox.Size = new Size(168, 39);
            PaidAmountTextBox.TabIndex = 77;
            PaidAmountTextBox.TextAlign = HorizontalAlignment.Center;
            PaidAmountTextBox.Texts = "";
            PaidAmountTextBox.UnderlinedStyle = true;
            // 
            // PasswordLabel
            // 
            PasswordLabel.BackColor = Color.FromArgb(38, 38, 38);
            PasswordLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            PasswordLabel.ForeColor = Color.Gainsboro;
            PasswordLabel.Location = new Point(22, 195);
            PasswordLabel.Name = "PasswordLabel";
            PasswordLabel.Size = new Size(110, 39);
            PasswordLabel.TabIndex = 76;
            PasswordLabel.Text = "ยอดชำระ";
            PasswordLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            label2.BackColor = Color.FromArgb(48, 48, 48);
            label2.Dock = DockStyle.Top;
            label2.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            label2.ForeColor = Color.Gainsboro;
            label2.Location = new Point(0, 0);
            label2.Name = "label2";
            label2.Size = new Size(412, 52);
            label2.TabIndex = 69;
            label2.Text = "รายละเอียดการลงบัญชี";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ActivePanel
            // 
            ActivePanel.Controls.Add(PayLaterPaymentsDataView);
            ActivePanel.Dock = DockStyle.Fill;
            ActivePanel.Location = new Point(0, 60);
            ActivePanel.Name = "ActivePanel";
            ActivePanel.Size = new Size(1007, 637);
            ActivePanel.TabIndex = 2;
            // 
            // PayLaterPaymentsDataView
            // 
            PayLaterPaymentsDataView.AllowUserToAddRows = false;
            PayLaterPaymentsDataView.AllowUserToDeleteRows = false;
            PayLaterPaymentsDataView.AllowUserToResizeColumns = false;
            PayLaterPaymentsDataView.AllowUserToResizeRows = false;
            PayLaterPaymentsDataView.BackgroundColor = Color.FromArgb(38, 38, 38);
            PayLaterPaymentsDataView.BorderStyle = BorderStyle.None;
            PayLaterPaymentsDataView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            PayLaterPaymentsDataView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle3.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle3.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle3.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = Color.Gray;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            PayLaterPaymentsDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            PayLaterPaymentsDataView.ColumnHeadersHeight = 50;
            PayLaterPaymentsDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            PayLaterPaymentsDataView.Columns.AddRange(new DataGridViewColumn[] { InvoiceId, Description, ReceivableAmount, PaidAmount, IsCompleted, DateCreated, DateUpdated });
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle4.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle4.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle4.SelectionBackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle4.SelectionForeColor = Color.Gainsboro;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            PayLaterPaymentsDataView.DefaultCellStyle = dataGridViewCellStyle4;
            PayLaterPaymentsDataView.Dock = DockStyle.Fill;
            PayLaterPaymentsDataView.EnableHeadersVisualStyles = false;
            PayLaterPaymentsDataView.GridColor = Color.DimGray;
            PayLaterPaymentsDataView.Location = new Point(0, 0);
            PayLaterPaymentsDataView.MultiSelect = false;
            PayLaterPaymentsDataView.Name = "PayLaterPaymentsDataView";
            PayLaterPaymentsDataView.RowHeadersVisible = false;
            PayLaterPaymentsDataView.RowHeadersWidth = 60;
            PayLaterPaymentsDataView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            PayLaterPaymentsDataView.RowTemplate.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            PayLaterPaymentsDataView.RowTemplate.Height = 35;
            PayLaterPaymentsDataView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            PayLaterPaymentsDataView.Size = new Size(1007, 637);
            PayLaterPaymentsDataView.TabIndex = 3;
            PayLaterPaymentsDataView.CellClick += PayLaterPaymentsDataView_CellClick;
            PayLaterPaymentsDataView.DoubleClick += PayLaterPaymentsDataView_DoubleClick;
            // 
            // InvoiceId
            // 
            InvoiceId.HeaderText = "Invoice ID";
            InvoiceId.Name = "InvoiceId";
            InvoiceId.ReadOnly = true;
            InvoiceId.Width = 150;
            // 
            // Description
            // 
            Description.HeaderText = "คำอธิบาย";
            Description.Name = "Description";
            Description.ReadOnly = true;
            Description.Width = 250;
            // 
            // ReceivableAmount
            // 
            ReceivableAmount.HeaderText = "ยอดลงบัญชี";
            ReceivableAmount.Name = "ReceivableAmount";
            ReceivableAmount.ReadOnly = true;
            ReceivableAmount.Width = 150;
            // 
            // PaidAmount
            // 
            PaidAmount.HeaderText = "ยอดชำระ";
            PaidAmount.Name = "PaidAmount";
            PaidAmount.Width = 150;
            // 
            // IsCompleted
            // 
            IsCompleted.HeaderText = "สถานะ";
            IsCompleted.Name = "IsCompleted";
            IsCompleted.Width = 150;
            // 
            // DateCreated
            // 
            DateCreated.HeaderText = "วันที่สร้าง";
            DateCreated.Name = "DateCreated";
            DateCreated.ReadOnly = true;
            DateCreated.Width = 200;
            // 
            // DateUpdated
            // 
            DateUpdated.HeaderText = "วันที่อัพเดท";
            DateUpdated.Name = "DateUpdated";
            DateUpdated.ReadOnly = true;
            DateUpdated.Width = 200;
            // 
            // PayLaterPaymentPanel
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(38, 38, 38);
            Controls.Add(ActivePanel);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "PayLaterPaymentPanel";
            Size = new Size(1421, 697);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel4.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ActivePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PayLaterPaymentsDataView).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private Panel ActivePanel;
        private DataGridView PayLaterPaymentsDataView;
        private Label label2;
        private DataGridViewTextBoxColumn InvoiceId;
        private DataGridViewTextBoxColumn Description;
        private DataGridViewTextBoxColumn ReceivableAmount;
        private DataGridViewTextBoxColumn PaidAmount;
        private DataGridViewTextBoxColumn IsCompleted;
        private DataGridViewTextBoxColumn DateCreated;
        private DataGridViewTextBoxColumn DateUpdated;
        private ModernUI.ModernButton ShowPayLaterPaymentsButton;
        private Label AmountLabel;
        private Label label3;
        private Label DescriptionLabel;
        private Label InvoiceIdLabel;
        private Label label11;
        private Label label10;
        private ModernUI.ModernTextBox PaidAmountTextBox;
        private Label PasswordLabel;
        private ModernUI.ModernButton UpdateButton;
        private ModernUI.ModernButton SearchByKeywordButton;
        private ModernUI.ModernTextBox SearchByKeywordTextBox;
        private CheckBox ShowIncompleteOnlyCheckBox;
        private Panel panel4;
        private Label PaymentIdLabel;
        private Label label4;
    }
}
