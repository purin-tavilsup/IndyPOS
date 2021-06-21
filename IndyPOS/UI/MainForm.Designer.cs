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
			this.panel2 = new System.Windows.Forms.Panel();
			this.InventoryButton = new System.Windows.Forms.Button();
			this.panel6 = new System.Windows.Forms.Panel();
			this.LogInButton = new System.Windows.Forms.Button();
			this.panel7 = new System.Windows.Forms.Panel();
			this.SettingsButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.SaleButton = new System.Windows.Forms.Button();
			this.panel5 = new System.Windows.Forms.Panel();
			this.CustomerAccountsButton = new System.Windows.Forms.Button();
			this.panel3 = new System.Windows.Forms.Panel();
			this.UsersButton = new System.Windows.Forms.Button();
			this.panel4 = new System.Windows.Forms.Panel();
			this.ReportsButton = new System.Windows.Forms.Button();
			this.TitlePanel = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.ActivePanel = new System.Windows.Forms.Panel();
			this.NavigationPanel.SuspendLayout();
			this.panel2.SuspendLayout();
			this.panel6.SuspendLayout();
			this.panel7.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel5.SuspendLayout();
			this.panel3.SuspendLayout();
			this.panel4.SuspendLayout();
			this.TitlePanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// NavigationPanel
			// 
			this.NavigationPanel.BackColor = System.Drawing.Color.DarkGray;
			this.NavigationPanel.Controls.Add(this.panel2);
			this.NavigationPanel.Controls.Add(this.panel6);
			this.NavigationPanel.Controls.Add(this.panel7);
			this.NavigationPanel.Controls.Add(this.panel1);
			this.NavigationPanel.Controls.Add(this.panel5);
			this.NavigationPanel.Controls.Add(this.panel3);
			this.NavigationPanel.Controls.Add(this.panel4);
			this.NavigationPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.NavigationPanel.Location = new System.Drawing.Point(0, 801);
			this.NavigationPanel.Name = "NavigationPanel";
			this.NavigationPanel.Size = new System.Drawing.Size(1412, 160);
			this.NavigationPanel.TabIndex = 0;
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.Silver;
			this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel2.Controls.Add(this.InventoryButton);
			this.panel2.Location = new System.Drawing.Point(203, 10);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(185, 139);
			this.panel2.TabIndex = 0;
			// 
			// InventoryButton
			// 
			this.InventoryButton.BackColor = System.Drawing.Color.Gainsboro;
			this.InventoryButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.InventoryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.InventoryButton.Font = new System.Drawing.Font("Leelawadee UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.InventoryButton.Image = global::IndyPOS.Properties.Resources.Inventory_50;
			this.InventoryButton.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.InventoryButton.Location = new System.Drawing.Point(3, 3);
			this.InventoryButton.Name = "InventoryButton";
			this.InventoryButton.Size = new System.Drawing.Size(177, 130);
			this.InventoryButton.TabIndex = 1;
			this.InventoryButton.Text = "การจัดการสินค้า";
			this.InventoryButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.InventoryButton.UseVisualStyleBackColor = false;
			this.InventoryButton.Click += new System.EventHandler(this.InventoryButton_Click);
			// 
			// panel6
			// 
			this.panel6.BackColor = System.Drawing.Color.Silver;
			this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel6.Controls.Add(this.LogInButton);
			this.panel6.Location = new System.Drawing.Point(1158, 10);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(185, 139);
			this.panel6.TabIndex = 4;
			// 
			// LogInButton
			// 
			this.LogInButton.BackColor = System.Drawing.Color.Gainsboro;
			this.LogInButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.LogInButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.LogInButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.LogInButton.Image = global::IndyPOS.Properties.Resources.Login_50;
			this.LogInButton.Location = new System.Drawing.Point(3, 3);
			this.LogInButton.Name = "LogInButton";
			this.LogInButton.Size = new System.Drawing.Size(177, 130);
			this.LogInButton.TabIndex = 5;
			this.LogInButton.Text = "เข้าสู่ระบบ";
			this.LogInButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.LogInButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.LogInButton.UseVisualStyleBackColor = false;
			// 
			// panel7
			// 
			this.panel7.BackColor = System.Drawing.Color.Silver;
			this.panel7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel7.Controls.Add(this.SettingsButton);
			this.panel7.Location = new System.Drawing.Point(967, 10);
			this.panel7.Name = "panel7";
			this.panel7.Size = new System.Drawing.Size(185, 139);
			this.panel7.TabIndex = 5;
			// 
			// SettingsButton
			// 
			this.SettingsButton.BackColor = System.Drawing.Color.Gainsboro;
			this.SettingsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.SettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.SettingsButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SettingsButton.Image = global::IndyPOS.Properties.Resources.Settings_50;
			this.SettingsButton.Location = new System.Drawing.Point(3, 3);
			this.SettingsButton.Name = "SettingsButton";
			this.SettingsButton.Size = new System.Drawing.Size(177, 130);
			this.SettingsButton.TabIndex = 4;
			this.SettingsButton.Text = "การตั้งค่า";
			this.SettingsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.SettingsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.SettingsButton.UseVisualStyleBackColor = false;
			this.SettingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.Silver;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.SaleButton);
			this.panel1.Location = new System.Drawing.Point(12, 10);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(185, 139);
			this.panel1.TabIndex = 6;
			// 
			// SaleButton
			// 
			this.SaleButton.BackColor = System.Drawing.Color.Gainsboro;
			this.SaleButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.SaleButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.SaleButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SaleButton.Image = global::IndyPOS.Properties.Resources.Cashier_50;
			this.SaleButton.Location = new System.Drawing.Point(3, 3);
			this.SaleButton.Name = "SaleButton";
			this.SaleButton.Size = new System.Drawing.Size(177, 130);
			this.SaleButton.TabIndex = 0;
			this.SaleButton.Text = "ขายสินค้า";
			this.SaleButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.SaleButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.SaleButton.UseVisualStyleBackColor = false;
			this.SaleButton.Click += new System.EventHandler(this.SaleButton_Click);
			// 
			// panel5
			// 
			this.panel5.BackColor = System.Drawing.Color.Silver;
			this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel5.Controls.Add(this.CustomerAccountsButton);
			this.panel5.Location = new System.Drawing.Point(776, 10);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(185, 139);
			this.panel5.TabIndex = 3;
			// 
			// CustomerAccountsButton
			// 
			this.CustomerAccountsButton.BackColor = System.Drawing.Color.Gainsboro;
			this.CustomerAccountsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.CustomerAccountsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CustomerAccountsButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CustomerAccountsButton.Image = global::IndyPOS.Properties.Resources.Customer_Acounts_50;
			this.CustomerAccountsButton.Location = new System.Drawing.Point(3, 3);
			this.CustomerAccountsButton.Name = "CustomerAccountsButton";
			this.CustomerAccountsButton.Size = new System.Drawing.Size(177, 130);
			this.CustomerAccountsButton.TabIndex = 3;
			this.CustomerAccountsButton.Text = "บัญชีลูกค้า";
			this.CustomerAccountsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.CustomerAccountsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.CustomerAccountsButton.UseVisualStyleBackColor = false;
			// 
			// panel3
			// 
			this.panel3.BackColor = System.Drawing.Color.Silver;
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.UsersButton);
			this.panel3.Location = new System.Drawing.Point(394, 10);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(185, 139);
			this.panel3.TabIndex = 1;
			// 
			// UsersButton
			// 
			this.UsersButton.BackColor = System.Drawing.Color.Gainsboro;
			this.UsersButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.UsersButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.UsersButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.UsersButton.Image = global::IndyPOS.Properties.Resources.User_50;
			this.UsersButton.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.UsersButton.Location = new System.Drawing.Point(3, 3);
			this.UsersButton.Name = "UsersButton";
			this.UsersButton.Size = new System.Drawing.Size(177, 130);
			this.UsersButton.TabIndex = 2;
			this.UsersButton.Text = "การจัดการผู้ใช้";
			this.UsersButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.UsersButton.UseVisualStyleBackColor = false;
			this.UsersButton.Click += new System.EventHandler(this.UsersButton_Click);
			// 
			// panel4
			// 
			this.panel4.BackColor = System.Drawing.Color.Silver;
			this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel4.Controls.Add(this.ReportsButton);
			this.panel4.Location = new System.Drawing.Point(585, 10);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(185, 139);
			this.panel4.TabIndex = 2;
			// 
			// ReportsButton
			// 
			this.ReportsButton.BackColor = System.Drawing.Color.Gainsboro;
			this.ReportsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.ReportsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.ReportsButton.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ReportsButton.Image = global::IndyPOS.Properties.Resources.Reports_50;
			this.ReportsButton.Location = new System.Drawing.Point(3, 3);
			this.ReportsButton.Name = "ReportsButton";
			this.ReportsButton.Size = new System.Drawing.Size(177, 130);
			this.ReportsButton.TabIndex = 3;
			this.ReportsButton.Text = "รายงาน";
			this.ReportsButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.ReportsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.ReportsButton.UseVisualStyleBackColor = false;
			this.ReportsButton.Click += new System.EventHandler(this.ReportsButton_Click);
			// 
			// TitlePanel
			// 
			this.TitlePanel.BackColor = System.Drawing.Color.DarkGray;
			this.TitlePanel.Controls.Add(this.label1);
			this.TitlePanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.TitlePanel.ForeColor = System.Drawing.Color.Black;
			this.TitlePanel.Location = new System.Drawing.Point(0, 0);
			this.TitlePanel.Name = "TitlePanel";
			this.TitlePanel.Size = new System.Drawing.Size(1412, 52);
			this.TitlePanel.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.Font = new System.Drawing.Font("Leelawadee UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 48);
			this.label1.TabIndex = 0;
			this.label1.Text = "รุ่งรัศมิ์";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ActivePanel
			// 
			this.ActivePanel.BackColor = System.Drawing.Color.DarkGray;
			this.ActivePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ActivePanel.Location = new System.Drawing.Point(0, 52);
			this.ActivePanel.Name = "ActivePanel";
			this.ActivePanel.Size = new System.Drawing.Size(1412, 749);
			this.ActivePanel.TabIndex = 3;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.LightGray;
			this.ClientSize = new System.Drawing.Size(1412, 961);
			this.Controls.Add(this.ActivePanel);
			this.Controls.Add(this.TitlePanel);
			this.Controls.Add(this.NavigationPanel);
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Indy POS";
			this.Shown += new System.EventHandler(this.MainForm_Shown);
			this.NavigationPanel.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel6.ResumeLayout(false);
			this.panel7.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel5.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.panel4.ResumeLayout(false);
			this.TitlePanel.ResumeLayout(false);
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
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
    }
}

