namespace IndyPOS
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
            this.LogInButton = new System.Windows.Forms.Button();
            this.TitlePanel = new System.Windows.Forms.Panel();
            this.ActivePanel = new System.Windows.Forms.Panel();
            this.SettingsButton = new System.Windows.Forms.Button();
            this.CustomerAccountsButton = new System.Windows.Forms.Button();
            this.ReportsButton = new System.Windows.Forms.Button();
            this.UsersButton = new System.Windows.Forms.Button();
            this.InventoryButton = new System.Windows.Forms.Button();
            this.SaleButton = new System.Windows.Forms.Button();
            this.NavigationPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // NavigationPanel
            // 
            this.NavigationPanel.BackColor = System.Drawing.SystemColors.Control;
            this.NavigationPanel.Controls.Add(this.LogInButton);
            this.NavigationPanel.Controls.Add(this.SettingsButton);
            this.NavigationPanel.Controls.Add(this.CustomerAccountsButton);
            this.NavigationPanel.Controls.Add(this.ReportsButton);
            this.NavigationPanel.Controls.Add(this.UsersButton);
            this.NavigationPanel.Controls.Add(this.InventoryButton);
            this.NavigationPanel.Controls.Add(this.SaleButton);
            this.NavigationPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.NavigationPanel.Location = new System.Drawing.Point(0, 809);
            this.NavigationPanel.Name = "NavigationPanel";
            this.NavigationPanel.Size = new System.Drawing.Size(1884, 152);
            this.NavigationPanel.TabIndex = 0;
            // 
            // LogInButton
            // 
            this.LogInButton.BackColor = System.Drawing.Color.Wheat;
            this.LogInButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.LogInButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LogInButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogInButton.Image = global::IndyPOS.Properties.Resources.Login_50;
            this.LogInButton.Location = new System.Drawing.Point(948, 10);
            this.LogInButton.Name = "LogInButton";
            this.LogInButton.Size = new System.Drawing.Size(150, 130);
            this.LogInButton.TabIndex = 5;
            this.LogInButton.Text = "เข้าสู่ระบบ";
            this.LogInButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.LogInButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.LogInButton.UseVisualStyleBackColor = false;
            // 
            // TitlePanel
            // 
            this.TitlePanel.BackColor = System.Drawing.Color.Wheat;
            this.TitlePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TitlePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TitlePanel.Location = new System.Drawing.Point(0, 0);
            this.TitlePanel.Name = "TitlePanel";
            this.TitlePanel.Size = new System.Drawing.Size(1884, 52);
            this.TitlePanel.TabIndex = 1;
            // 
            // ActivePanel
            // 
            this.ActivePanel.BackColor = System.Drawing.SystemColors.Control;
            this.ActivePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ActivePanel.Location = new System.Drawing.Point(0, 52);
            this.ActivePanel.Name = "ActivePanel";
            this.ActivePanel.Size = new System.Drawing.Size(1884, 757);
            this.ActivePanel.TabIndex = 3;
            // 
            // SettingsButton
            // 
            this.SettingsButton.BackColor = System.Drawing.Color.Wheat;
            this.SettingsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.SettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SettingsButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SettingsButton.Image = global::IndyPOS.Properties.Resources.Settings_50;
            this.SettingsButton.Location = new System.Drawing.Point(792, 10);
            this.SettingsButton.Name = "SettingsButton";
            this.SettingsButton.Size = new System.Drawing.Size(150, 130);
            this.SettingsButton.TabIndex = 4;
            this.SettingsButton.Text = "การตั้งค่า";
            this.SettingsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.SettingsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.SettingsButton.UseVisualStyleBackColor = false;
            this.SettingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
            // 
            // CustomerAccountsButton
            // 
            this.CustomerAccountsButton.BackColor = System.Drawing.Color.Wheat;
            this.CustomerAccountsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.CustomerAccountsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CustomerAccountsButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CustomerAccountsButton.Image = global::IndyPOS.Properties.Resources.Customer_Acounts_50;
            this.CustomerAccountsButton.Location = new System.Drawing.Point(636, 10);
            this.CustomerAccountsButton.Name = "CustomerAccountsButton";
            this.CustomerAccountsButton.Size = new System.Drawing.Size(150, 130);
            this.CustomerAccountsButton.TabIndex = 3;
            this.CustomerAccountsButton.Text = "บัญชีลูกค้า";
            this.CustomerAccountsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.CustomerAccountsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.CustomerAccountsButton.UseVisualStyleBackColor = false;
            // 
            // ReportsButton
            // 
            this.ReportsButton.BackColor = System.Drawing.Color.Wheat;
            this.ReportsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ReportsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ReportsButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReportsButton.Image = global::IndyPOS.Properties.Resources.Reports_50;
            this.ReportsButton.Location = new System.Drawing.Point(480, 10);
            this.ReportsButton.Name = "ReportsButton";
            this.ReportsButton.Size = new System.Drawing.Size(150, 130);
            this.ReportsButton.TabIndex = 3;
            this.ReportsButton.Text = "รายงาน";
            this.ReportsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.ReportsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.ReportsButton.UseVisualStyleBackColor = false;
            this.ReportsButton.Click += new System.EventHandler(this.ReportsButton_Click);
            // 
            // UsersButton
            // 
            this.UsersButton.BackColor = System.Drawing.Color.Wheat;
            this.UsersButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.UsersButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.UsersButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UsersButton.Image = global::IndyPOS.Properties.Resources.Users_50;
            this.UsersButton.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.UsersButton.Location = new System.Drawing.Point(324, 10);
            this.UsersButton.Name = "UsersButton";
            this.UsersButton.Size = new System.Drawing.Size(150, 130);
            this.UsersButton.TabIndex = 2;
            this.UsersButton.Text = "การจัดการผู้ใช้";
            this.UsersButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.UsersButton.UseVisualStyleBackColor = false;
            this.UsersButton.Click += new System.EventHandler(this.UsersButton_Click);
            // 
            // InventoryButton
            // 
            this.InventoryButton.BackColor = System.Drawing.Color.Wheat;
            this.InventoryButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.InventoryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.InventoryButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InventoryButton.Image = global::IndyPOS.Properties.Resources.Inventory_50;
            this.InventoryButton.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.InventoryButton.Location = new System.Drawing.Point(168, 10);
            this.InventoryButton.Name = "InventoryButton";
            this.InventoryButton.Size = new System.Drawing.Size(150, 130);
            this.InventoryButton.TabIndex = 1;
            this.InventoryButton.Text = "การจัดการสินค้า";
            this.InventoryButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.InventoryButton.UseVisualStyleBackColor = false;
            this.InventoryButton.Click += new System.EventHandler(this.InventoryButton_Click);
            // 
            // SaleButton
            // 
            this.SaleButton.BackColor = System.Drawing.Color.Wheat;
            this.SaleButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.SaleButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SaleButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaleButton.Image = global::IndyPOS.Properties.Resources.ShoppingCart_50;
            this.SaleButton.Location = new System.Drawing.Point(12, 10);
            this.SaleButton.Name = "SaleButton";
            this.SaleButton.Size = new System.Drawing.Size(150, 130);
            this.SaleButton.TabIndex = 0;
            this.SaleButton.Text = "ขายสินค้า";
            this.SaleButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.SaleButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.SaleButton.UseVisualStyleBackColor = false;
            this.SaleButton.Click += new System.EventHandler(this.SaleButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightGray;
            this.ClientSize = new System.Drawing.Size(1884, 961);
            this.Controls.Add(this.ActivePanel);
            this.Controls.Add(this.TitlePanel);
            this.Controls.Add(this.NavigationPanel);
            this.Name = "MainForm";
            this.Text = "Indy POS";
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.NavigationPanel.ResumeLayout(false);
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
    }
}

