namespace IndyPOS.UI.Reports
{
    partial class SalesHistoryReportPanel
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SaleInvoiceDataView = new System.Windows.Forms.DataGridView();
            this.InvoiceId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.InvoiceTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label3 = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.ShowReportByDateRangeButton = new ModernUI.ModernButton();
            this.label11 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.StartDatePicker = new System.Windows.Forms.DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.EndDatePicker = new System.Windows.Forms.DateTimePicker();
            this.panel6 = new System.Windows.Forms.Panel();
            this.PeriodLabel = new System.Windows.Forms.Label();
            this.ShowReportByThisYearButton = new ModernUI.ModernButton();
            this.ShowReportByTodayButton = new ModernUI.ModernButton();
            this.ShowReportByThisMonthButton = new ModernUI.ModernButton();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.InvoiceProductsDataView = new System.Windows.Forms.DataGridView();
            this.Priority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.PaymentDataView = new System.Windows.Forms.DataGridView();
            this.PaymentPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaymentType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaymentAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Note = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SaleInvoiceDataView)).BeginInit();
            this.panel7.SuspendLayout();
            this.panel6.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceProductsDataView)).BeginInit();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.SaleInvoiceDataView);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 96);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(373, 829);
            this.panel1.TabIndex = 0;
            // 
            // SaleInvoiceDataView
            // 
            this.SaleInvoiceDataView.AllowUserToAddRows = false;
            this.SaleInvoiceDataView.AllowUserToDeleteRows = false;
            this.SaleInvoiceDataView.AllowUserToResizeColumns = false;
            this.SaleInvoiceDataView.AllowUserToResizeRows = false;
            this.SaleInvoiceDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.SaleInvoiceDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.SaleInvoiceDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.SaleInvoiceDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Gray;
            this.SaleInvoiceDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.SaleInvoiceDataView.ColumnHeadersHeight = 50;
            this.SaleInvoiceDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.SaleInvoiceDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.InvoiceId,
            this.InvoiceTotal});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.SaleInvoiceDataView.DefaultCellStyle = dataGridViewCellStyle2;
            this.SaleInvoiceDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SaleInvoiceDataView.EnableHeadersVisualStyles = false;
            this.SaleInvoiceDataView.GridColor = System.Drawing.Color.DimGray;
            this.SaleInvoiceDataView.Location = new System.Drawing.Point(0, 40);
            this.SaleInvoiceDataView.MultiSelect = false;
            this.SaleInvoiceDataView.Name = "SaleInvoiceDataView";
            this.SaleInvoiceDataView.ReadOnly = true;
            this.SaleInvoiceDataView.RowHeadersVisible = false;
            this.SaleInvoiceDataView.RowHeadersWidth = 60;
            this.SaleInvoiceDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.SaleInvoiceDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.SaleInvoiceDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.SaleInvoiceDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaleInvoiceDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
            this.SaleInvoiceDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.SaleInvoiceDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
            this.SaleInvoiceDataView.RowTemplate.Height = 35;
            this.SaleInvoiceDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.SaleInvoiceDataView.Size = new System.Drawing.Size(371, 787);
            this.SaleInvoiceDataView.TabIndex = 50;
            this.SaleInvoiceDataView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.SaleInvoiceDataView_CellClick);
            // 
            // InvoiceId
            // 
            this.InvoiceId.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.InvoiceId.HeaderText = "Invoice ID";
            this.InvoiceId.Name = "InvoiceId";
            this.InvoiceId.ReadOnly = true;
            this.InvoiceId.Width = 200;
            // 
            // InvoiceTotal
            // 
            this.InvoiceTotal.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.InvoiceTotal.HeaderText = "ยอดขาย";
            this.InvoiceTotal.Name = "InvoiceTotal";
            this.InvoiceTotal.ReadOnly = true;
            this.InvoiceTotal.Width = 150;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Gainsboro;
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(371, 40);
            this.label3.TabIndex = 49;
            this.label3.Text = "รายการขาย";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel7
            // 
            this.panel7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel7.Controls.Add(this.ShowReportByDateRangeButton);
            this.panel7.Controls.Add(this.label11);
            this.panel7.Controls.Add(this.label9);
            this.panel7.Controls.Add(this.StartDatePicker);
            this.panel7.Controls.Add(this.label7);
            this.panel7.Controls.Add(this.EndDatePicker);
            this.panel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel7.Location = new System.Drawing.Point(438, 0);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(1172, 96);
            this.panel7.TabIndex = 109;
            // 
            // ShowReportByDateRangeButton
            // 
            this.ShowReportByDateRangeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByDateRangeButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByDateRangeButton.BorderColor = System.Drawing.Color.DarkGray;
            this.ShowReportByDateRangeButton.BorderRadius = 5;
            this.ShowReportByDateRangeButton.BorderSize = 1;
            this.ShowReportByDateRangeButton.FlatAppearance.BorderSize = 0;
            this.ShowReportByDateRangeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowReportByDateRangeButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowReportByDateRangeButton.ForeColor = System.Drawing.Color.White;
            this.ShowReportByDateRangeButton.Location = new System.Drawing.Point(804, 29);
            this.ShowReportByDateRangeButton.Name = "ShowReportByDateRangeButton";
            this.ShowReportByDateRangeButton.Size = new System.Drawing.Size(180, 49);
            this.ShowReportByDateRangeButton.TabIndex = 103;
            this.ShowReportByDateRangeButton.Text = "แสดงผล";
            this.ShowReportByDateRangeButton.TextColor = System.Drawing.Color.White;
            this.ShowReportByDateRangeButton.UseVisualStyleBackColor = false;
            this.ShowReportByDateRangeButton.Click += new System.EventHandler(this.ShowReportByDateRangeButton_Click);
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Gainsboro;
            this.label11.Location = new System.Drawing.Point(426, 44);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(56, 31);
            this.label11.TabIndex = 102;
            this.label11.Text = "ถึง";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label9.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.Gainsboro;
            this.label9.Location = new System.Drawing.Point(57, 44);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(56, 31);
            this.label9.TabIndex = 101;
            this.label9.Text = "จาก";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // StartDatePicker
            // 
            this.StartDatePicker.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StartDatePicker.Location = new System.Drawing.Point(119, 46);
            this.StartDatePicker.Name = "StartDatePicker";
            this.StartDatePicker.Size = new System.Drawing.Size(301, 25);
            this.StartDatePicker.TabIndex = 100;
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label7.Dock = System.Windows.Forms.DockStyle.Top;
            this.label7.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.Gainsboro;
            this.label7.Location = new System.Drawing.Point(0, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(1170, 39);
            this.label7.TabIndex = 83;
            this.label7.Text = " เลือกช่วงเวลาแบบกำหนดเอง";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // EndDatePicker
            // 
            this.EndDatePicker.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EndDatePicker.Location = new System.Drawing.Point(488, 46);
            this.EndDatePicker.Name = "EndDatePicker";
            this.EndDatePicker.Size = new System.Drawing.Size(301, 25);
            this.EndDatePicker.TabIndex = 84;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel6.Controls.Add(this.PeriodLabel);
            this.panel6.Controls.Add(this.ShowReportByThisYearButton);
            this.panel6.Controls.Add(this.ShowReportByTodayButton);
            this.panel6.Controls.Add(this.ShowReportByThisMonthButton);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel6.Location = new System.Drawing.Point(0, 0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(438, 96);
            this.panel6.TabIndex = 108;
            // 
            // PeriodLabel
            // 
            this.PeriodLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PeriodLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.PeriodLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PeriodLabel.ForeColor = System.Drawing.Color.MediumAquamarine;
            this.PeriodLabel.Location = new System.Drawing.Point(0, 0);
            this.PeriodLabel.Name = "PeriodLabel";
            this.PeriodLabel.Size = new System.Drawing.Size(436, 39);
            this.PeriodLabel.TabIndex = 83;
            this.PeriodLabel.Text = "เลือกเวลา";
            this.PeriodLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ShowReportByThisYearButton
            // 
            this.ShowReportByThisYearButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByThisYearButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByThisYearButton.BorderColor = System.Drawing.Color.Gray;
            this.ShowReportByThisYearButton.BorderRadius = 5;
            this.ShowReportByThisYearButton.BorderSize = 1;
            this.ShowReportByThisYearButton.FlatAppearance.BorderSize = 0;
            this.ShowReportByThisYearButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowReportByThisYearButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowReportByThisYearButton.ForeColor = System.Drawing.Color.White;
            this.ShowReportByThisYearButton.Location = new System.Drawing.Point(286, 42);
            this.ShowReportByThisYearButton.Name = "ShowReportByThisYearButton";
            this.ShowReportByThisYearButton.Size = new System.Drawing.Size(130, 36);
            this.ShowReportByThisYearButton.TabIndex = 97;
            this.ShowReportByThisYearButton.Text = "ปีนี้";
            this.ShowReportByThisYearButton.TextColor = System.Drawing.Color.White;
            this.ShowReportByThisYearButton.UseVisualStyleBackColor = false;
            this.ShowReportByThisYearButton.Click += new System.EventHandler(this.ShowReportByThisYearButton_Click);
            // 
            // ShowReportByTodayButton
            // 
            this.ShowReportByTodayButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByTodayButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByTodayButton.BorderColor = System.Drawing.Color.Gray;
            this.ShowReportByTodayButton.BorderRadius = 5;
            this.ShowReportByTodayButton.BorderSize = 1;
            this.ShowReportByTodayButton.FlatAppearance.BorderSize = 0;
            this.ShowReportByTodayButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowReportByTodayButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowReportByTodayButton.ForeColor = System.Drawing.Color.White;
            this.ShowReportByTodayButton.Location = new System.Drawing.Point(16, 42);
            this.ShowReportByTodayButton.Name = "ShowReportByTodayButton";
            this.ShowReportByTodayButton.Size = new System.Drawing.Size(130, 36);
            this.ShowReportByTodayButton.TabIndex = 94;
            this.ShowReportByTodayButton.Text = "วันนี้";
            this.ShowReportByTodayButton.TextColor = System.Drawing.Color.White;
            this.ShowReportByTodayButton.UseVisualStyleBackColor = false;
            this.ShowReportByTodayButton.Click += new System.EventHandler(this.ShowReportByTodayButton_Click);
            // 
            // ShowReportByThisMonthButton
            // 
            this.ShowReportByThisMonthButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByThisMonthButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowReportByThisMonthButton.BorderColor = System.Drawing.Color.Gray;
            this.ShowReportByThisMonthButton.BorderRadius = 5;
            this.ShowReportByThisMonthButton.BorderSize = 1;
            this.ShowReportByThisMonthButton.FlatAppearance.BorderSize = 0;
            this.ShowReportByThisMonthButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowReportByThisMonthButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowReportByThisMonthButton.ForeColor = System.Drawing.Color.White;
            this.ShowReportByThisMonthButton.Location = new System.Drawing.Point(151, 42);
            this.ShowReportByThisMonthButton.Name = "ShowReportByThisMonthButton";
            this.ShowReportByThisMonthButton.Size = new System.Drawing.Size(130, 36);
            this.ShowReportByThisMonthButton.TabIndex = 96;
            this.ShowReportByThisMonthButton.Text = "เดือนนี้";
            this.ShowReportByThisMonthButton.TextColor = System.Drawing.Color.White;
            this.ShowReportByThisMonthButton.UseVisualStyleBackColor = false;
            this.ShowReportByThisMonthButton.Click += new System.EventHandler(this.ShowReportByThisMonthButton_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel7);
            this.panel2.Controls.Add(this.panel6);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1610, 96);
            this.panel2.TabIndex = 110;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.panel4);
            this.panel3.Controls.Add(this.panel5);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(373, 96);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1237, 829);
            this.panel3.TabIndex = 111;
            // 
            // panel4
            // 
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.InvoiceProductsDataView);
            this.panel4.Controls.Add(this.label1);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1237, 619);
            this.panel4.TabIndex = 0;
            // 
            // InvoiceProductsDataView
            // 
            this.InvoiceProductsDataView.AllowUserToAddRows = false;
            this.InvoiceProductsDataView.AllowUserToDeleteRows = false;
            this.InvoiceProductsDataView.AllowUserToResizeColumns = false;
            this.InvoiceProductsDataView.AllowUserToResizeRows = false;
            this.InvoiceProductsDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.InvoiceProductsDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.InvoiceProductsDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.InvoiceProductsDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.Gray;
            this.InvoiceProductsDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.InvoiceProductsDataView.ColumnHeadersHeight = 50;
            this.InvoiceProductsDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.InvoiceProductsDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Priority,
            this.ProductCode,
            this.Description,
            this.Quantity,
            this.UnitPrice,
            this.Total});
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.InvoiceProductsDataView.DefaultCellStyle = dataGridViewCellStyle4;
            this.InvoiceProductsDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InvoiceProductsDataView.EnableHeadersVisualStyles = false;
            this.InvoiceProductsDataView.GridColor = System.Drawing.Color.DimGray;
            this.InvoiceProductsDataView.Location = new System.Drawing.Point(0, 40);
            this.InvoiceProductsDataView.MultiSelect = false;
            this.InvoiceProductsDataView.Name = "InvoiceProductsDataView";
            this.InvoiceProductsDataView.ReadOnly = true;
            this.InvoiceProductsDataView.RowHeadersVisible = false;
            this.InvoiceProductsDataView.RowHeadersWidth = 60;
            this.InvoiceProductsDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.InvoiceProductsDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.InvoiceProductsDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.InvoiceProductsDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InvoiceProductsDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
            this.InvoiceProductsDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.InvoiceProductsDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
            this.InvoiceProductsDataView.RowTemplate.Height = 35;
            this.InvoiceProductsDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.InvoiceProductsDataView.Size = new System.Drawing.Size(1235, 577);
            this.InvoiceProductsDataView.TabIndex = 51;
            // 
            // Priority
            // 
            this.Priority.HeaderText = "ลำดับ";
            this.Priority.Name = "Priority";
            this.Priority.ReadOnly = true;
            // 
            // ProductCode
            // 
            this.ProductCode.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.ProductCode.HeaderText = "รหัสสินค้า";
            this.ProductCode.Name = "ProductCode";
            this.ProductCode.ReadOnly = true;
            this.ProductCode.Width = 200;
            // 
            // Description
            // 
            this.Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Description.HeaderText = "คำอธิบาย";
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 350;
            // 
            // Quantity
            // 
            this.Quantity.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Quantity.HeaderText = "จำนวน";
            this.Quantity.Name = "Quantity";
            this.Quantity.ReadOnly = true;
            // 
            // UnitPrice
            // 
            this.UnitPrice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.UnitPrice.HeaderText = "ราคาต่อหน่วย";
            this.UnitPrice.Name = "UnitPrice";
            this.UnitPrice.ReadOnly = true;
            this.UnitPrice.Width = 150;
            // 
            // Total
            // 
            this.Total.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Total.HeaderText = "ราคารวม";
            this.Total.Name = "Total";
            this.Total.ReadOnly = true;
            this.Total.Width = 150;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Gainsboro;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1235, 40);
            this.label1.TabIndex = 50;
            this.label1.Text = "รายการสินค้า";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel5
            // 
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.PaymentDataView);
            this.panel5.Controls.Add(this.label2);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel5.Location = new System.Drawing.Point(0, 619);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(1237, 210);
            this.panel5.TabIndex = 1;
            // 
            // PaymentDataView
            // 
            this.PaymentDataView.AllowUserToAddRows = false;
            this.PaymentDataView.AllowUserToDeleteRows = false;
            this.PaymentDataView.AllowUserToResizeColumns = false;
            this.PaymentDataView.AllowUserToResizeRows = false;
            this.PaymentDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PaymentDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.PaymentDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.PaymentDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle5.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.Gray;
            this.PaymentDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.PaymentDataView.ColumnHeadersHeight = 50;
            this.PaymentDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.PaymentDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PaymentPriority,
            this.PaymentType,
            this.PaymentAmount,
            this.Note});
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle6.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PaymentDataView.DefaultCellStyle = dataGridViewCellStyle6;
            this.PaymentDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PaymentDataView.EnableHeadersVisualStyles = false;
            this.PaymentDataView.GridColor = System.Drawing.Color.DimGray;
            this.PaymentDataView.Location = new System.Drawing.Point(0, 40);
            this.PaymentDataView.MultiSelect = false;
            this.PaymentDataView.Name = "PaymentDataView";
            this.PaymentDataView.ReadOnly = true;
            this.PaymentDataView.RowHeadersVisible = false;
            this.PaymentDataView.RowHeadersWidth = 60;
            this.PaymentDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.PaymentDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.PaymentDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PaymentDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PaymentDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
            this.PaymentDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.PaymentDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
            this.PaymentDataView.RowTemplate.Height = 35;
            this.PaymentDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.PaymentDataView.Size = new System.Drawing.Size(1235, 168);
            this.PaymentDataView.TabIndex = 51;
            // 
            // PaymentPriority
            // 
            this.PaymentPriority.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.PaymentPriority.HeaderText = "ลำดับ";
            this.PaymentPriority.Name = "PaymentPriority";
            this.PaymentPriority.ReadOnly = true;
            // 
            // PaymentType
            // 
            this.PaymentType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.PaymentType.HeaderText = "ประเภทการชำระเงิน";
            this.PaymentType.Name = "PaymentType";
            this.PaymentType.ReadOnly = true;
            this.PaymentType.Width = 300;
            // 
            // PaymentAmount
            // 
            this.PaymentAmount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.PaymentAmount.HeaderText = "จำนวนเงิน";
            this.PaymentAmount.Name = "PaymentAmount";
            this.PaymentAmount.ReadOnly = true;
            this.PaymentAmount.Width = 250;
            // 
            // Note
            // 
            this.Note.HeaderText = "Note";
            this.Note.Name = "Note";
            this.Note.ReadOnly = true;
            this.Note.Width = 200;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(1235, 40);
            this.label2.TabIndex = 50;
            this.label2.Text = "รายการเงิน";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SalesHistoryReportPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel2);
            this.Name = "SalesHistoryReportPanel";
            this.Size = new System.Drawing.Size(1610, 925);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SaleInvoiceDataView)).EndInit();
            this.panel7.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceProductsDataView)).EndInit();
            this.panel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel7;
        private ModernUI.ModernButton ShowReportByDateRangeButton;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DateTimePicker StartDatePicker;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DateTimePicker EndDatePicker;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label PeriodLabel;
        private ModernUI.ModernButton ShowReportByThisYearButton;
        private ModernUI.ModernButton ShowReportByTodayButton;
        private ModernUI.ModernButton ShowReportByThisMonthButton;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView SaleInvoiceDataView;
        private System.Windows.Forms.DataGridView InvoiceProductsDataView;
        private System.Windows.Forms.DataGridViewTextBoxColumn Priority;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn Total;
        private System.Windows.Forms.DataGridView PaymentDataView;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentPriority;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentType;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn Note;
        private System.Windows.Forms.DataGridViewTextBoxColumn InvoiceId;
        private System.Windows.Forms.DataGridViewTextBoxColumn InvoiceTotal;
    }
}
