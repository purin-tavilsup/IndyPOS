namespace IndyPOS.Windows.Forms.UI.Report
{
    partial class ReportsPanel
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
            this.panel2 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ShowSalesOverviewReportButton = new ModernUI.ModernButton();
            this.ShowInvoiceProductsButton = new ModernUI.ModernButton();
            this.ShowSalesHistoryButton = new ModernUI.ModernButton();
            this.ActivePanel = new System.Windows.Forms.Panel();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.panel1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1613, 44);
            this.panel2.TabIndex = 93;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Gainsboro;
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(809, 44);
            this.label4.TabIndex = 84;
            this.label4.Text = " รายงานผลประกอบการ";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ShowSalesOverviewReportButton);
            this.panel1.Controls.Add(this.ShowInvoiceProductsButton);
            this.panel1.Controls.Add(this.ShowSalesHistoryButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(809, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(804, 44);
            this.panel1.TabIndex = 85;
            // 
            // ShowSalesOverviewReportButton
            // 
            this.ShowSalesOverviewReportButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowSalesOverviewReportButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowSalesOverviewReportButton.BorderColor = System.Drawing.Color.Gray;
            this.ShowSalesOverviewReportButton.BorderRadius = 5;
            this.ShowSalesOverviewReportButton.BorderSize = 1;
            this.ShowSalesOverviewReportButton.FlatAppearance.BorderSize = 0;
            this.ShowSalesOverviewReportButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowSalesOverviewReportButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowSalesOverviewReportButton.ForeColor = System.Drawing.Color.White;
            this.ShowSalesOverviewReportButton.Location = new System.Drawing.Point(13, 3);
            this.ShowSalesOverviewReportButton.Name = "ShowSalesOverviewReportButton";
            this.ShowSalesOverviewReportButton.Size = new System.Drawing.Size(238, 37);
            this.ShowSalesOverviewReportButton.TabIndex = 13;
            this.ShowSalesOverviewReportButton.Text = "ผลประกอบการโดยรวม";
            this.ShowSalesOverviewReportButton.TextColor = System.Drawing.Color.White;
            this.ShowSalesOverviewReportButton.UseVisualStyleBackColor = false;
            this.ShowSalesOverviewReportButton.Click += new System.EventHandler(this.ShowSalesOverviewReportButton_Click);
            // 
            // ShowInvoiceProductsButton
            // 
            this.ShowInvoiceProductsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowInvoiceProductsButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowInvoiceProductsButton.BorderColor = System.Drawing.Color.Gray;
            this.ShowInvoiceProductsButton.BorderRadius = 5;
            this.ShowInvoiceProductsButton.BorderSize = 1;
            this.ShowInvoiceProductsButton.FlatAppearance.BorderSize = 0;
            this.ShowInvoiceProductsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowInvoiceProductsButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowInvoiceProductsButton.ForeColor = System.Drawing.Color.White;
            this.ShowInvoiceProductsButton.Location = new System.Drawing.Point(262, 3);
            this.ShowInvoiceProductsButton.Name = "ShowInvoiceProductsButton";
            this.ShowInvoiceProductsButton.Size = new System.Drawing.Size(238, 37);
            this.ShowInvoiceProductsButton.TabIndex = 12;
            this.ShowInvoiceProductsButton.Text = "รายการสินค้าที่ขายไป";
            this.ShowInvoiceProductsButton.TextColor = System.Drawing.Color.White;
            this.ShowInvoiceProductsButton.UseVisualStyleBackColor = false;
            this.ShowInvoiceProductsButton.Click += new System.EventHandler(this.ShowInvoiceProductsButton_Click);
            // 
            // ShowSalesHistoryButton
            // 
            this.ShowSalesHistoryButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowSalesHistoryButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowSalesHistoryButton.BorderColor = System.Drawing.Color.Gray;
            this.ShowSalesHistoryButton.BorderRadius = 5;
            this.ShowSalesHistoryButton.BorderSize = 1;
            this.ShowSalesHistoryButton.FlatAppearance.BorderSize = 0;
            this.ShowSalesHistoryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowSalesHistoryButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowSalesHistoryButton.ForeColor = System.Drawing.Color.White;
            this.ShowSalesHistoryButton.Location = new System.Drawing.Point(511, 3);
            this.ShowSalesHistoryButton.Name = "ShowSalesHistoryButton";
            this.ShowSalesHistoryButton.Size = new System.Drawing.Size(238, 37);
            this.ShowSalesHistoryButton.TabIndex = 11;
            this.ShowSalesHistoryButton.Text = "ประวัติการขาย";
            this.ShowSalesHistoryButton.TextColor = System.Drawing.Color.White;
            this.ShowSalesHistoryButton.UseVisualStyleBackColor = false;
            this.ShowSalesHistoryButton.Click += new System.EventHandler(this.ShowSalesHistoryButton_Click);
            // 
            // ActivePanel
            // 
            this.ActivePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ActivePanel.Location = new System.Drawing.Point(0, 44);
            this.ActivePanel.Name = "ActivePanel";
            this.ActivePanel.Size = new System.Drawing.Size(1613, 926);
            this.ActivePanel.TabIndex = 94;
            this.ActivePanel.VisibleChanged += new System.EventHandler(this.ActivePanel_VisibleChanged);
            // 
            // ReportsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.Controls.Add(this.ActivePanel);
            this.Controls.Add(this.panel2);
            this.Name = "ReportsPanel";
            this.Size = new System.Drawing.Size(1613, 970);
            this.panel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel ActivePanel;
        private System.Windows.Forms.Panel panel1;
        private ModernUI.ModernButton ShowSalesOverviewReportButton;
        private ModernUI.ModernButton ShowInvoiceProductsButton;
        private ModernUI.ModernButton ShowSalesHistoryButton;
    }
}
