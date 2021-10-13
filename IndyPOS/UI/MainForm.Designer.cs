namespace IndyPOS.UI
{
    partial class MainForm
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
			this.NavigationPanel = new System.Windows.Forms.Panel();
			this.CloseButton = new System.Windows.Forms.Button();
			this.InventoryButton = new System.Windows.Forms.Button();
			this.LogInButton = new System.Windows.Forms.Button();
			this.SettingsButton = new System.Windows.Forms.Button();
			this.SaleButton = new System.Windows.Forms.Button();
			this.CustomerAccountsButton = new System.Windows.Forms.Button();
			this.UsersButton = new System.Windows.Forms.Button();
			this.ReportsButton = new System.Windows.Forms.Button();
			this.TitlePanel = new System.Windows.Forms.Panel();
			this.panel9 = new System.Windows.Forms.Panel();
			this.ResizeWindows = new System.Windows.Forms.PictureBox();
			this.MinimizeWindows = new System.Windows.Forms.PictureBox();
			this.CloseWindows = new System.Windows.Forms.PictureBox();
			this.label1 = new System.Windows.Forms.Label();
			this.ActivePanel = new System.Windows.Forms.Panel();
			this.ControlPanel = new System.Windows.Forms.Panel();
			this.TitlePanel.SuspendLayout();
			this.panel9.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ResizeWindows)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MinimizeWindows)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CloseWindows)).BeginInit();
			this.ControlPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// NavigationPanel
			// 
			this.NavigationPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
			this.NavigationPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.NavigationPanel.Location = new System.Drawing.Point(0, 1024);
			this.NavigationPanel.Name = "NavigationPanel";
			this.NavigationPanel.Size = new System.Drawing.Size(1412, 41);
			this.NavigationPanel.TabIndex = 0;
			// 
			// CloseButton
			// 
			this.CloseButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.CloseButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.CloseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CloseButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CloseButton.ForeColor = System.Drawing.Color.DarkGray;
			this.CloseButton.Image = global::IndyPOS.Properties.Resources.CloseWindows_50;
			this.CloseButton.Location = new System.Drawing.Point(3, 850);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(292, 115);
			this.CloseButton.TabIndex = 5;
			this.CloseButton.Text = "ออกจากโปรแกรม";
			this.CloseButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.CloseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.CloseButton.UseVisualStyleBackColor = false;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// InventoryButton
			// 
			this.InventoryButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.InventoryButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.InventoryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.InventoryButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.InventoryButton.ForeColor = System.Drawing.Color.DarkGray;
			this.InventoryButton.Image = global::IndyPOS.Properties.Resources.Inventory_50;
			this.InventoryButton.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.InventoryButton.Location = new System.Drawing.Point(3, 124);
			this.InventoryButton.Name = "InventoryButton";
			this.InventoryButton.Size = new System.Drawing.Size(292, 115);
			this.InventoryButton.TabIndex = 1;
			this.InventoryButton.Text = "การจัดการสินค้า";
			this.InventoryButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.InventoryButton.UseVisualStyleBackColor = false;
			this.InventoryButton.Click += new System.EventHandler(this.InventoryButton_Click);
			// 
			// LogInButton
			// 
			this.LogInButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.LogInButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.LogInButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.LogInButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.LogInButton.ForeColor = System.Drawing.Color.DarkGray;
			this.LogInButton.Image = global::IndyPOS.Properties.Resources.Login_50;
			this.LogInButton.Location = new System.Drawing.Point(3, 729);
			this.LogInButton.Name = "LogInButton";
			this.LogInButton.Size = new System.Drawing.Size(292, 115);
			this.LogInButton.TabIndex = 5;
			this.LogInButton.Text = "เข้าสู่ระบบ";
			this.LogInButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.LogInButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.LogInButton.UseVisualStyleBackColor = false;
			// 
			// SettingsButton
			// 
			this.SettingsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.SettingsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.SettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.SettingsButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SettingsButton.ForeColor = System.Drawing.Color.DarkGray;
			this.SettingsButton.Image = global::IndyPOS.Properties.Resources.Settings_50;
			this.SettingsButton.Location = new System.Drawing.Point(3, 608);
			this.SettingsButton.Name = "SettingsButton";
			this.SettingsButton.Size = new System.Drawing.Size(292, 115);
			this.SettingsButton.TabIndex = 4;
			this.SettingsButton.Text = "การตั้งค่า";
			this.SettingsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.SettingsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.SettingsButton.UseVisualStyleBackColor = false;
			this.SettingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
			// 
			// SaleButton
			// 
			this.SaleButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.SaleButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.SaleButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.SaleButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SaleButton.ForeColor = System.Drawing.Color.DarkGray;
			this.SaleButton.Image = global::IndyPOS.Properties.Resources.Cashier_50;
			this.SaleButton.Location = new System.Drawing.Point(3, 3);
			this.SaleButton.Name = "SaleButton";
			this.SaleButton.Size = new System.Drawing.Size(292, 115);
			this.SaleButton.TabIndex = 0;
			this.SaleButton.Text = "ขายสินค้า";
			this.SaleButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.SaleButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.SaleButton.UseVisualStyleBackColor = false;
			this.SaleButton.Click += new System.EventHandler(this.SaleButton_Click);
			// 
			// CustomerAccountsButton
			// 
			this.CustomerAccountsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.CustomerAccountsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.CustomerAccountsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CustomerAccountsButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CustomerAccountsButton.ForeColor = System.Drawing.Color.DarkGray;
			this.CustomerAccountsButton.Image = global::IndyPOS.Properties.Resources.Customer_Acounts_50;
			this.CustomerAccountsButton.Location = new System.Drawing.Point(3, 487);
			this.CustomerAccountsButton.Name = "CustomerAccountsButton";
			this.CustomerAccountsButton.Size = new System.Drawing.Size(292, 115);
			this.CustomerAccountsButton.TabIndex = 3;
			this.CustomerAccountsButton.Text = "บัญชีลูกค้า";
			this.CustomerAccountsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.CustomerAccountsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.CustomerAccountsButton.UseVisualStyleBackColor = false;
			this.CustomerAccountsButton.Click += new System.EventHandler(this.CustomerAccountsButton_Click);
			// 
			// UsersButton
			// 
			this.UsersButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.UsersButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.UsersButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.UsersButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.UsersButton.ForeColor = System.Drawing.Color.DarkGray;
			this.UsersButton.Image = global::IndyPOS.Properties.Resources.User_50;
			this.UsersButton.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.UsersButton.Location = new System.Drawing.Point(3, 245);
			this.UsersButton.Name = "UsersButton";
			this.UsersButton.Size = new System.Drawing.Size(292, 115);
			this.UsersButton.TabIndex = 2;
			this.UsersButton.Text = "การจัดการผู้ใช้";
			this.UsersButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.UsersButton.UseVisualStyleBackColor = false;
			this.UsersButton.Click += new System.EventHandler(this.UsersButton_Click);
			// 
			// ReportsButton
			// 
			this.ReportsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.ReportsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.ReportsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.ReportsButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ReportsButton.ForeColor = System.Drawing.Color.DarkGray;
			this.ReportsButton.Image = global::IndyPOS.Properties.Resources.Reports_50;
			this.ReportsButton.Location = new System.Drawing.Point(3, 366);
			this.ReportsButton.Name = "ReportsButton";
			this.ReportsButton.Size = new System.Drawing.Size(292, 115);
			this.ReportsButton.TabIndex = 3;
			this.ReportsButton.Text = "รายงาน";
			this.ReportsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.ReportsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.ReportsButton.UseVisualStyleBackColor = false;
			this.ReportsButton.Click += new System.EventHandler(this.ReportsButton_Click);
			// 
			// TitlePanel
			// 
			this.TitlePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
			this.TitlePanel.Controls.Add(this.panel9);
			this.TitlePanel.Controls.Add(this.label1);
			this.TitlePanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.TitlePanel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TitlePanel.ForeColor = System.Drawing.Color.LightGreen;
			this.TitlePanel.Location = new System.Drawing.Point(0, 0);
			this.TitlePanel.Name = "TitlePanel";
			this.TitlePanel.Size = new System.Drawing.Size(1412, 52);
			this.TitlePanel.TabIndex = 1;
			// 
			// panel9
			// 
			this.panel9.Controls.Add(this.ResizeWindows);
			this.panel9.Controls.Add(this.MinimizeWindows);
			this.panel9.Controls.Add(this.CloseWindows);
			this.panel9.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel9.Location = new System.Drawing.Point(1233, 0);
			this.panel9.Name = "panel9";
			this.panel9.Size = new System.Drawing.Size(179, 52);
			this.panel9.TabIndex = 4;
			// 
			// ResizeWindows
			// 
			this.ResizeWindows.Image = global::IndyPOS.Properties.Resources.maximize_window_24px;
			this.ResizeWindows.Location = new System.Drawing.Point(78, 13);
			this.ResizeWindows.Name = "ResizeWindows";
			this.ResizeWindows.Size = new System.Drawing.Size(24, 24);
			this.ResizeWindows.TabIndex = 2;
			this.ResizeWindows.TabStop = false;
			this.ResizeWindows.Click += new System.EventHandler(this.ResizeWindows_Click);
			// 
			// MinimizeWindows
			// 
			this.MinimizeWindows.Image = global::IndyPOS.Properties.Resources.minimize_window_24px;
			this.MinimizeWindows.Location = new System.Drawing.Point(14, 13);
			this.MinimizeWindows.Name = "MinimizeWindows";
			this.MinimizeWindows.Size = new System.Drawing.Size(24, 24);
			this.MinimizeWindows.TabIndex = 3;
			this.MinimizeWindows.TabStop = false;
			this.MinimizeWindows.Click += new System.EventHandler(this.MinimizeWindows_Click);
			// 
			// CloseWindows
			// 
			this.CloseWindows.Image = global::IndyPOS.Properties.Resources.close_window_24px;
			this.CloseWindows.Location = new System.Drawing.Point(142, 13);
			this.CloseWindows.Name = "CloseWindows";
			this.CloseWindows.Size = new System.Drawing.Size(24, 24);
			this.CloseWindows.TabIndex = 1;
			this.CloseWindows.TabStop = false;
			this.CloseWindows.Click += new System.EventHandler(this.CloseWindows_Click);
			// 
			// label1
			// 
			this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.label1.Location = new System.Drawing.Point(3, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 39);
			this.label1.TabIndex = 0;
			this.label1.Text = "รุ่งรัศมิ์";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ActivePanel
			// 
			this.ActivePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
			this.ActivePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ActivePanel.Location = new System.Drawing.Point(298, 52);
			this.ActivePanel.Name = "ActivePanel";
			this.ActivePanel.Size = new System.Drawing.Size(1114, 972);
			this.ActivePanel.TabIndex = 3;
			// 
			// ControlPanel
			// 
			this.ControlPanel.Controls.Add(this.CloseButton);
			this.ControlPanel.Controls.Add(this.LogInButton);
			this.ControlPanel.Controls.Add(this.SettingsButton);
			this.ControlPanel.Controls.Add(this.CustomerAccountsButton);
			this.ControlPanel.Controls.Add(this.ReportsButton);
			this.ControlPanel.Controls.Add(this.UsersButton);
			this.ControlPanel.Controls.Add(this.InventoryButton);
			this.ControlPanel.Controls.Add(this.SaleButton);
			this.ControlPanel.Dock = System.Windows.Forms.DockStyle.Left;
			this.ControlPanel.Location = new System.Drawing.Point(0, 52);
			this.ControlPanel.Name = "ControlPanel";
			this.ControlPanel.Size = new System.Drawing.Size(298, 972);
			this.ControlPanel.TabIndex = 4;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
			this.ClientSize = new System.Drawing.Size(1412, 1065);
			this.Controls.Add(this.ActivePanel);
			this.Controls.Add(this.ControlPanel);
			this.Controls.Add(this.TitlePanel);
			this.Controls.Add(this.NavigationPanel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Indy POS";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Shown += new System.EventHandler(this.MainForm_Shown);
			this.TitlePanel.ResumeLayout(false);
			this.panel9.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.ResizeWindows)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MinimizeWindows)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CloseWindows)).EndInit();
			this.ControlPanel.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel NavigationPanel;
        private System.Windows.Forms.Panel TitlePanel;
        private System.Windows.Forms.Panel ActivePanel;
        private System.Windows.Forms.Button SaleButton;
        private System.Windows.Forms.Button InventoryButton;
        private System.Windows.Forms.Button UsersButton;
        private System.Windows.Forms.Button SettingsButton;
        private System.Windows.Forms.Button ReportsButton;
        private System.Windows.Forms.Button CustomerAccountsButton;
        private System.Windows.Forms.Button LogInButton;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.PictureBox MinimizeWindows;
		private System.Windows.Forms.PictureBox ResizeWindows;
		private System.Windows.Forms.PictureBox CloseWindows;
		private System.Windows.Forms.Panel panel9;
		private System.Windows.Forms.Panel ControlPanel;
	}
}

