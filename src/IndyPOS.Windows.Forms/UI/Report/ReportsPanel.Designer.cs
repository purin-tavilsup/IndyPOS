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
            panel2 = new Panel();
            label4 = new Label();
            panel1 = new Panel();
            PayLaterPaymentsReportButton = new ModernUI.ModernButton();
            ShowSalesOverviewReportButton = new ModernUI.ModernButton();
            ShowInvoiceProductsButton = new ModernUI.ModernButton();
            ShowSalesHistoryButton = new ModernUI.ModernButton();
            ActivePanel = new Panel();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel2
            // 
            panel2.Controls.Add(label4);
            panel2.Controls.Add(panel1);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(1613, 44);
            panel2.TabIndex = 93;
            // 
            // label4
            // 
            label4.BackColor = Color.FromArgb(38, 38, 38);
            label4.Dock = DockStyle.Fill;
            label4.Font = new Font("FC Subject [Non-commercial] Reg", 20.25F, FontStyle.Regular, GraphicsUnit.Point);
            label4.ForeColor = Color.Gainsboro;
            label4.Location = new Point(0, 0);
            label4.Name = "label4";
            label4.Size = new Size(606, 44);
            label4.TabIndex = 84;
            label4.Text = " รายงานผลประกอบการ";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            panel1.Controls.Add(PayLaterPaymentsReportButton);
            panel1.Controls.Add(ShowSalesOverviewReportButton);
            panel1.Controls.Add(ShowInvoiceProductsButton);
            panel1.Controls.Add(ShowSalesHistoryButton);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(606, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1007, 44);
            panel1.TabIndex = 85;
            // 
            // PayLaterPaymentsReportButton
            // 
            PayLaterPaymentsReportButton.BackColor = Color.FromArgb(38, 38, 38);
            PayLaterPaymentsReportButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            PayLaterPaymentsReportButton.BorderColor = Color.Gray;
            PayLaterPaymentsReportButton.BorderRadius = 5;
            PayLaterPaymentsReportButton.BorderSize = 1;
            PayLaterPaymentsReportButton.FlatAppearance.BorderSize = 0;
            PayLaterPaymentsReportButton.FlatStyle = FlatStyle.Flat;
            PayLaterPaymentsReportButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            PayLaterPaymentsReportButton.ForeColor = Color.White;
            PayLaterPaymentsReportButton.Location = new Point(755, 3);
            PayLaterPaymentsReportButton.Name = "PayLaterPaymentsReportButton";
            PayLaterPaymentsReportButton.Size = new Size(238, 37);
            PayLaterPaymentsReportButton.TabIndex = 14;
            PayLaterPaymentsReportButton.Text = "ยอดการลงบัญชีค้างชำระ";
            PayLaterPaymentsReportButton.TextColor = Color.White;
            PayLaterPaymentsReportButton.UseVisualStyleBackColor = false;
            PayLaterPaymentsReportButton.Click += PayLaterPaymentsReportButton_Click;
            // 
            // ShowSalesOverviewReportButton
            // 
            ShowSalesOverviewReportButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowSalesOverviewReportButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowSalesOverviewReportButton.BorderColor = Color.Gray;
            ShowSalesOverviewReportButton.BorderRadius = 5;
            ShowSalesOverviewReportButton.BorderSize = 1;
            ShowSalesOverviewReportButton.FlatAppearance.BorderSize = 0;
            ShowSalesOverviewReportButton.FlatStyle = FlatStyle.Flat;
            ShowSalesOverviewReportButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ShowSalesOverviewReportButton.ForeColor = Color.White;
            ShowSalesOverviewReportButton.Location = new Point(13, 3);
            ShowSalesOverviewReportButton.Name = "ShowSalesOverviewReportButton";
            ShowSalesOverviewReportButton.Size = new Size(238, 37);
            ShowSalesOverviewReportButton.TabIndex = 13;
            ShowSalesOverviewReportButton.Text = "ผลประกอบการโดยรวม";
            ShowSalesOverviewReportButton.TextColor = Color.White;
            ShowSalesOverviewReportButton.UseVisualStyleBackColor = false;
            ShowSalesOverviewReportButton.Click += ShowSalesOverviewReportButton_Click;
            // 
            // ShowInvoiceProductsButton
            // 
            ShowInvoiceProductsButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowInvoiceProductsButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowInvoiceProductsButton.BorderColor = Color.Gray;
            ShowInvoiceProductsButton.BorderRadius = 5;
            ShowInvoiceProductsButton.BorderSize = 1;
            ShowInvoiceProductsButton.FlatAppearance.BorderSize = 0;
            ShowInvoiceProductsButton.FlatStyle = FlatStyle.Flat;
            ShowInvoiceProductsButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ShowInvoiceProductsButton.ForeColor = Color.White;
            ShowInvoiceProductsButton.Location = new Point(262, 3);
            ShowInvoiceProductsButton.Name = "ShowInvoiceProductsButton";
            ShowInvoiceProductsButton.Size = new Size(238, 37);
            ShowInvoiceProductsButton.TabIndex = 12;
            ShowInvoiceProductsButton.Text = "รายการสินค้าที่ขายไป";
            ShowInvoiceProductsButton.TextColor = Color.White;
            ShowInvoiceProductsButton.UseVisualStyleBackColor = false;
            ShowInvoiceProductsButton.Click += ShowInvoiceProductsButton_Click;
            // 
            // ShowSalesHistoryButton
            // 
            ShowSalesHistoryButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowSalesHistoryButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowSalesHistoryButton.BorderColor = Color.Gray;
            ShowSalesHistoryButton.BorderRadius = 5;
            ShowSalesHistoryButton.BorderSize = 1;
            ShowSalesHistoryButton.FlatAppearance.BorderSize = 0;
            ShowSalesHistoryButton.FlatStyle = FlatStyle.Flat;
            ShowSalesHistoryButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ShowSalesHistoryButton.ForeColor = Color.White;
            ShowSalesHistoryButton.Location = new Point(511, 3);
            ShowSalesHistoryButton.Name = "ShowSalesHistoryButton";
            ShowSalesHistoryButton.Size = new Size(238, 37);
            ShowSalesHistoryButton.TabIndex = 11;
            ShowSalesHistoryButton.Text = "ประวัติการขาย";
            ShowSalesHistoryButton.TextColor = Color.White;
            ShowSalesHistoryButton.UseVisualStyleBackColor = false;
            ShowSalesHistoryButton.Click += ShowSalesHistoryButton_Click;
            // 
            // ActivePanel
            // 
            ActivePanel.Dock = DockStyle.Fill;
            ActivePanel.Location = new Point(0, 44);
            ActivePanel.Name = "ActivePanel";
            ActivePanel.Size = new Size(1613, 926);
            ActivePanel.TabIndex = 94;
            ActivePanel.VisibleChanged += ActivePanel_VisibleChanged;
            // 
            // ReportsPanel
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(ActivePanel);
            Controls.Add(panel2);
            Name = "ReportsPanel";
            Size = new Size(1613, 970);
            panel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Panel panel2;
        private Label label4;
        private Panel ActivePanel;
        private Panel panel1;
        private ModernUI.ModernButton ShowSalesOverviewReportButton;
        private ModernUI.ModernButton ShowInvoiceProductsButton;
        private ModernUI.ModernButton ShowSalesHistoryButton;
        private ModernUI.ModernButton PayLaterPaymentsReportButton;
    }
}
