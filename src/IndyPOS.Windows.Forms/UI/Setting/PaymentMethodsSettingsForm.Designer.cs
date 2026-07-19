namespace IndyPOS.Windows.Forms.UI.Setting
{
    partial class PaymentMethodsSettingsForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            TitleLabel = new Label();
            BottomPanel = new Panel();
            CloseButton = new ModernUI.ModernButton();
            ContentPanel = new Panel();
            GridPanel = new Panel();
            PaymentMethodsGrid = new DataGridView();
            GridTitleLabel = new Label();
            AddCampaignPanel = new Panel();
            AddCampaignButton = new ModernUI.ModernButton();
            CampaignDisplayOrderTextBox = new ModernUI.ModernTextBox();
            CampaignDisplayOrderLabel = new Label();
            CampaignDisplayNameTextBox = new ModernUI.ModernTextBox();
            CampaignDisplayNameLabel = new Label();
            CampaignCodeTextBox = new ModernUI.ModernTextBox();
            CampaignCodeLabel = new Label();
            AddCampaignTitleLabel = new Label();
            BottomPanel.SuspendLayout();
            ContentPanel.SuspendLayout();
            GridPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PaymentMethodsGrid).BeginInit();
            AddCampaignPanel.SuspendLayout();
            SuspendLayout();
            //
            // TitleLabel
            //
            TitleLabel.BackColor = Color.DarkSlateGray;
            TitleLabel.Dock = DockStyle.Top;
            TitleLabel.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point);
            TitleLabel.ForeColor = Color.WhiteSmoke;
            TitleLabel.Location = new Point(0, 0);
            TitleLabel.Name = "TitleLabel";
            TitleLabel.Size = new Size(1100, 39);
            TitleLabel.TabIndex = 0;
            TitleLabel.Text = "จัดการวิธีการชำระเงิน";
            TitleLabel.TextAlign = ContentAlignment.MiddleCenter;
            //
            // BottomPanel
            //
            BottomPanel.BackColor = Color.FromArgb(25, 25, 25);
            BottomPanel.Controls.Add(CloseButton);
            BottomPanel.Dock = DockStyle.Bottom;
            BottomPanel.Location = new Point(0, 707);
            BottomPanel.Name = "BottomPanel";
            BottomPanel.Size = new Size(1100, 93);
            BottomPanel.TabIndex = 1;
            //
            // CloseButton
            //
            CloseButton.BackColor = Color.FromArgb(38, 38, 38);
            CloseButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            CloseButton.BorderColor = Color.FromArgb(224, 79, 95);
            CloseButton.BorderRadius = 19;
            CloseButton.BorderSize = 1;
            CloseButton.FlatAppearance.BorderSize = 0;
            CloseButton.FlatStyle = FlatStyle.Flat;
            CloseButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            CloseButton.ForeColor = Color.White;
            CloseButton.Location = new Point(920, 18);
            CloseButton.Name = "CloseButton";
            CloseButton.Size = new Size(158, 53);
            CloseButton.TabIndex = 0;
            CloseButton.Text = "ปิด";
            CloseButton.TextColor = Color.White;
            CloseButton.UseVisualStyleBackColor = false;
            CloseButton.Click += CloseButton_Click;
            //
            // ContentPanel
            //
            ContentPanel.Controls.Add(GridPanel);
            ContentPanel.Controls.Add(AddCampaignPanel);
            ContentPanel.Dock = DockStyle.Fill;
            ContentPanel.Location = new Point(0, 39);
            ContentPanel.Name = "ContentPanel";
            ContentPanel.Size = new Size(1100, 668);
            ContentPanel.TabIndex = 2;
            //
            // GridPanel
            //
            GridPanel.BackColor = Color.FromArgb(30, 30, 30);
            GridPanel.BorderStyle = BorderStyle.FixedSingle;
            GridPanel.Controls.Add(PaymentMethodsGrid);
            GridPanel.Controls.Add(GridTitleLabel);
            GridPanel.Dock = DockStyle.Fill;
            GridPanel.Location = new Point(0, 0);
            GridPanel.Name = "GridPanel";
            GridPanel.Size = new Size(1100, 518);
            GridPanel.TabIndex = 0;
            //
            // PaymentMethodsGrid
            //
            PaymentMethodsGrid.AllowUserToAddRows = false;
            PaymentMethodsGrid.AllowUserToDeleteRows = false;
            PaymentMethodsGrid.AllowUserToResizeColumns = false;
            PaymentMethodsGrid.AllowUserToResizeRows = false;
            PaymentMethodsGrid.AutoGenerateColumns = false;
            PaymentMethodsGrid.BackgroundColor = Color.FromArgb(38, 38, 38);
            PaymentMethodsGrid.BorderStyle = BorderStyle.None;
            PaymentMethodsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            PaymentMethodsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle1.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = Color.Gray;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            PaymentMethodsGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            PaymentMethodsGrid.ColumnHeadersHeight = 45;
            PaymentMethodsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle2.Font = new Font("FC Subject [Non-commercial] Reg", 11F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(70, 70, 70);
            dataGridViewCellStyle2.SelectionForeColor = Color.Gainsboro;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            PaymentMethodsGrid.DefaultCellStyle = dataGridViewCellStyle2;
            PaymentMethodsGrid.Dock = DockStyle.Fill;
            PaymentMethodsGrid.EnableHeadersVisualStyles = false;
            PaymentMethodsGrid.GridColor = Color.DimGray;
            PaymentMethodsGrid.Location = new Point(0, 40);
            PaymentMethodsGrid.MultiSelect = false;
            PaymentMethodsGrid.Name = "PaymentMethodsGrid";
            PaymentMethodsGrid.RowHeadersVisible = false;
            PaymentMethodsGrid.RowHeadersWidth = 40;
            PaymentMethodsGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            PaymentMethodsGrid.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            PaymentMethodsGrid.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            PaymentMethodsGrid.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 11F, FontStyle.Regular, GraphicsUnit.Point);
            PaymentMethodsGrid.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            PaymentMethodsGrid.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            PaymentMethodsGrid.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            PaymentMethodsGrid.RowTemplate.Height = 36;
            PaymentMethodsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            PaymentMethodsGrid.Size = new Size(1098, 476);
            PaymentMethodsGrid.TabIndex = 0;
            PaymentMethodsGrid.CellContentClick += PaymentMethodsGrid_CellContentClick;
            PaymentMethodsGrid.CellValueChanged += PaymentMethodsGrid_CellValueChanged;
            PaymentMethodsGrid.CurrentCellDirtyStateChanged += PaymentMethodsGrid_CurrentCellDirtyStateChanged;
            //
            // GridTitleLabel
            //
            GridTitleLabel.BackColor = Color.FromArgb(38, 38, 38);
            GridTitleLabel.Dock = DockStyle.Top;
            GridTitleLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            GridTitleLabel.ForeColor = Color.Gainsboro;
            GridTitleLabel.Location = new Point(0, 0);
            GridTitleLabel.Name = "GridTitleLabel";
            GridTitleLabel.Size = new Size(1098, 40);
            GridTitleLabel.TabIndex = 1;
            GridTitleLabel.Text = "รายการวิธีการชำระเงินทั้งหมด";
            GridTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // AddCampaignPanel
            //
            AddCampaignPanel.BackColor = Color.FromArgb(38, 38, 38);
            AddCampaignPanel.BorderStyle = BorderStyle.FixedSingle;
            AddCampaignPanel.Controls.Add(AddCampaignButton);
            AddCampaignPanel.Controls.Add(CampaignDisplayOrderTextBox);
            AddCampaignPanel.Controls.Add(CampaignDisplayOrderLabel);
            AddCampaignPanel.Controls.Add(CampaignDisplayNameTextBox);
            AddCampaignPanel.Controls.Add(CampaignDisplayNameLabel);
            AddCampaignPanel.Controls.Add(CampaignCodeTextBox);
            AddCampaignPanel.Controls.Add(CampaignCodeLabel);
            AddCampaignPanel.Controls.Add(AddCampaignTitleLabel);
            AddCampaignPanel.Dock = DockStyle.Bottom;
            AddCampaignPanel.Location = new Point(0, 518);
            AddCampaignPanel.Name = "AddCampaignPanel";
            AddCampaignPanel.Size = new Size(1100, 150);
            AddCampaignPanel.TabIndex = 1;
            //
            // AddCampaignButton
            //
            AddCampaignButton.BackColor = Color.FromArgb(38, 38, 38);
            AddCampaignButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddCampaignButton.BorderColor = Color.FromArgb(50, 190, 166);
            AddCampaignButton.BorderRadius = 19;
            AddCampaignButton.BorderSize = 1;
            AddCampaignButton.FlatAppearance.BorderSize = 0;
            AddCampaignButton.FlatStyle = FlatStyle.Flat;
            AddCampaignButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            AddCampaignButton.ForeColor = Color.White;
            AddCampaignButton.Image = Properties.Resources.Plus_35;
            AddCampaignButton.Location = new Point(890, 58);
            AddCampaignButton.Name = "AddCampaignButton";
            AddCampaignButton.Size = new Size(180, 53);
            AddCampaignButton.TabIndex = 7;
            AddCampaignButton.Text = "เพิ่ม";
            AddCampaignButton.TextColor = Color.White;
            AddCampaignButton.UseVisualStyleBackColor = false;
            AddCampaignButton.Click += AddCampaignButton_Click;
            //
            // CampaignDisplayOrderTextBox
            //
            CampaignDisplayOrderTextBox.BackColor = Color.FromArgb(38, 38, 38);
            CampaignDisplayOrderTextBox.BorderColor = Color.DimGray;
            CampaignDisplayOrderTextBox.BorderSize = 1;
            CampaignDisplayOrderTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            CampaignDisplayOrderTextBox.ForeColor = Color.Gainsboro;
            CampaignDisplayOrderTextBox.Location = new Point(740, 58);
            CampaignDisplayOrderTextBox.Multiline = false;
            CampaignDisplayOrderTextBox.Name = "CampaignDisplayOrderTextBox";
            CampaignDisplayOrderTextBox.Padding = new Padding(7);
            CampaignDisplayOrderTextBox.PasswordChar = false;
            CampaignDisplayOrderTextBox.PlaceholderColor = Color.DarkGray;
            CampaignDisplayOrderTextBox.PlaceholderText = "";
            CampaignDisplayOrderTextBox.ReadOnly = false;
            CampaignDisplayOrderTextBox.Size = new Size(100, 39);
            CampaignDisplayOrderTextBox.TabIndex = 6;
            CampaignDisplayOrderTextBox.TextAlign = HorizontalAlignment.Center;
            CampaignDisplayOrderTextBox.Texts = "";
            CampaignDisplayOrderTextBox.UnderlinedStyle = true;
            //
            // CampaignDisplayOrderLabel
            //
            CampaignDisplayOrderLabel.BackColor = Color.FromArgb(38, 38, 38);
            CampaignDisplayOrderLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            CampaignDisplayOrderLabel.ForeColor = Color.Gainsboro;
            CampaignDisplayOrderLabel.Location = new Point(650, 58);
            CampaignDisplayOrderLabel.Name = "CampaignDisplayOrderLabel";
            CampaignDisplayOrderLabel.Size = new Size(84, 39);
            CampaignDisplayOrderLabel.TabIndex = 5;
            CampaignDisplayOrderLabel.Text = "ลำดับ";
            CampaignDisplayOrderLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // CampaignDisplayNameTextBox
            //
            CampaignDisplayNameTextBox.BackColor = Color.FromArgb(38, 38, 38);
            CampaignDisplayNameTextBox.BorderColor = Color.DimGray;
            CampaignDisplayNameTextBox.BorderSize = 1;
            CampaignDisplayNameTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            CampaignDisplayNameTextBox.ForeColor = Color.Gainsboro;
            CampaignDisplayNameTextBox.Location = new Point(360, 58);
            CampaignDisplayNameTextBox.Multiline = false;
            CampaignDisplayNameTextBox.Name = "CampaignDisplayNameTextBox";
            CampaignDisplayNameTextBox.Padding = new Padding(7);
            CampaignDisplayNameTextBox.PasswordChar = false;
            CampaignDisplayNameTextBox.PlaceholderColor = Color.DarkGray;
            CampaignDisplayNameTextBox.PlaceholderText = "";
            CampaignDisplayNameTextBox.ReadOnly = false;
            CampaignDisplayNameTextBox.Size = new Size(270, 39);
            CampaignDisplayNameTextBox.TabIndex = 4;
            CampaignDisplayNameTextBox.TextAlign = HorizontalAlignment.Left;
            CampaignDisplayNameTextBox.Texts = "";
            CampaignDisplayNameTextBox.UnderlinedStyle = true;
            //
            // CampaignDisplayNameLabel
            //
            CampaignDisplayNameLabel.BackColor = Color.FromArgb(38, 38, 38);
            CampaignDisplayNameLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            CampaignDisplayNameLabel.ForeColor = Color.Gainsboro;
            CampaignDisplayNameLabel.Location = new Point(260, 58);
            CampaignDisplayNameLabel.Name = "CampaignDisplayNameLabel";
            CampaignDisplayNameLabel.Size = new Size(100, 39);
            CampaignDisplayNameLabel.TabIndex = 3;
            CampaignDisplayNameLabel.Text = "ชื่อที่แสดง";
            CampaignDisplayNameLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // CampaignCodeTextBox
            //
            CampaignCodeTextBox.BackColor = Color.FromArgb(38, 38, 38);
            CampaignCodeTextBox.BorderColor = Color.DimGray;
            CampaignCodeTextBox.BorderSize = 1;
            CampaignCodeTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            CampaignCodeTextBox.ForeColor = Color.Gainsboro;
            CampaignCodeTextBox.Location = new Point(100, 58);
            CampaignCodeTextBox.Multiline = false;
            CampaignCodeTextBox.Name = "CampaignCodeTextBox";
            CampaignCodeTextBox.Padding = new Padding(7);
            CampaignCodeTextBox.PasswordChar = false;
            CampaignCodeTextBox.PlaceholderColor = Color.DarkGray;
            CampaignCodeTextBox.PlaceholderText = "";
            CampaignCodeTextBox.ReadOnly = false;
            CampaignCodeTextBox.Size = new Size(150, 39);
            CampaignCodeTextBox.TabIndex = 2;
            CampaignCodeTextBox.TextAlign = HorizontalAlignment.Left;
            CampaignCodeTextBox.Texts = "";
            CampaignCodeTextBox.UnderlinedStyle = true;
            //
            // CampaignCodeLabel
            //
            CampaignCodeLabel.BackColor = Color.FromArgb(38, 38, 38);
            CampaignCodeLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            CampaignCodeLabel.ForeColor = Color.Gainsboro;
            CampaignCodeLabel.Location = new Point(17, 58);
            CampaignCodeLabel.Name = "CampaignCodeLabel";
            CampaignCodeLabel.Size = new Size(80, 39);
            CampaignCodeLabel.TabIndex = 1;
            CampaignCodeLabel.Text = "รหัส";
            CampaignCodeLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // AddCampaignTitleLabel
            //
            AddCampaignTitleLabel.BackColor = Color.FromArgb(48, 48, 48);
            AddCampaignTitleLabel.Dock = DockStyle.Top;
            AddCampaignTitleLabel.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            AddCampaignTitleLabel.ForeColor = Color.Gainsboro;
            AddCampaignTitleLabel.Location = new Point(0, 0);
            AddCampaignTitleLabel.Name = "AddCampaignTitleLabel";
            AddCampaignTitleLabel.Size = new Size(1098, 40);
            AddCampaignTitleLabel.TabIndex = 0;
            AddCampaignTitleLabel.Text = "เพิ่มวิธีการชำระเงินแคมเปญใหม่";
            AddCampaignTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // PaymentMethodsSettingsForm
            //
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(1100, 800);
            Controls.Add(ContentPanel);
            Controls.Add(BottomPanel);
            Controls.Add(TitleLabel);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "PaymentMethodsSettingsForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PaymentMethodsSettingsForm";
            BottomPanel.ResumeLayout(false);
            ContentPanel.ResumeLayout(false);
            GridPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PaymentMethodsGrid).EndInit();
            AddCampaignPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Label TitleLabel;
        private Panel BottomPanel;
        private ModernUI.ModernButton CloseButton;
        private Panel ContentPanel;
        private Panel GridPanel;
        private DataGridView PaymentMethodsGrid;
        private Label GridTitleLabel;
        private Panel AddCampaignPanel;
        private ModernUI.ModernButton AddCampaignButton;
        private ModernUI.ModernTextBox CampaignDisplayOrderTextBox;
        private Label CampaignDisplayOrderLabel;
        private ModernUI.ModernTextBox CampaignDisplayNameTextBox;
        private Label CampaignDisplayNameLabel;
        private ModernUI.ModernTextBox CampaignCodeTextBox;
        private Label CampaignCodeLabel;
        private Label AddCampaignTitleLabel;
    }
}
