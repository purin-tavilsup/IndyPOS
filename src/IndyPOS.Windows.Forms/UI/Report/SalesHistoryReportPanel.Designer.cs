namespace IndyPOS.Windows.Forms.UI.Report
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
            var dataGridViewCellStyle1 = new DataGridViewCellStyle();
            var dataGridViewCellStyle2 = new DataGridViewCellStyle();
            var dataGridViewCellStyle3 = new DataGridViewCellStyle();
            var dataGridViewCellStyle4 = new DataGridViewCellStyle();
            var dataGridViewCellStyle5 = new DataGridViewCellStyle();
            var dataGridViewCellStyle6 = new DataGridViewCellStyle();
            panel1 = new Panel();
            SaleInvoiceDataView = new DataGridView();
            InvoiceId = new DataGridViewTextBoxColumn();
            InvoiceTotal = new DataGridViewTextBoxColumn();
            label3 = new Label();
            panel7 = new Panel();
            ShowReportByDateRangeButton = new ModernUI.ModernButton();
            label11 = new Label();
            label9 = new Label();
            StartDatePicker = new DateTimePicker();
            label7 = new Label();
            EndDatePicker = new DateTimePicker();
            panel6 = new Panel();
            PeriodLabel = new Label();
            ShowReportByThisYearButton = new ModernUI.ModernButton();
            ShowReportByTodayButton = new ModernUI.ModernButton();
            ShowReportByThisMonthButton = new ModernUI.ModernButton();
            panel2 = new Panel();
            panel3 = new Panel();
            panel4 = new Panel();
            InvoiceProductsDataView = new DataGridView();
            Priority = new DataGridViewTextBoxColumn();
            ProductCode = new DataGridViewTextBoxColumn();
            Description = new DataGridViewTextBoxColumn();
            Quantity = new DataGridViewTextBoxColumn();
            UnitPrice = new DataGridViewTextBoxColumn();
            Total = new DataGridViewTextBoxColumn();
            label1 = new Label();
            panel5 = new Panel();
            PaymentDataView = new DataGridView();
            PaymentPriority = new DataGridViewTextBoxColumn();
            PaymentType = new DataGridViewTextBoxColumn();
            PaymentAmount = new DataGridViewTextBoxColumn();
            Note = new DataGridViewTextBoxColumn();
            label2 = new Label();
            PrintReceiptButton = new ModernUI.ModernButton();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)SaleInvoiceDataView).BeginInit();
            panel7.SuspendLayout();
            panel6.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)InvoiceProductsDataView).BeginInit();
            panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PaymentDataView).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(SaleInvoiceDataView);
            panel1.Controls.Add(label3);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 96);
            panel1.Name = "panel1";
            panel1.Size = new Size(373, 829);
            panel1.TabIndex = 0;
            // 
            // SaleInvoiceDataView
            // 
            SaleInvoiceDataView.AllowUserToAddRows = false;
            SaleInvoiceDataView.AllowUserToDeleteRows = false;
            SaleInvoiceDataView.AllowUserToResizeColumns = false;
            SaleInvoiceDataView.AllowUserToResizeRows = false;
            SaleInvoiceDataView.BackgroundColor = Color.FromArgb(38, 38, 38);
            SaleInvoiceDataView.BorderStyle = BorderStyle.None;
            SaleInvoiceDataView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            SaleInvoiceDataView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle1.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = Color.Gray;
            SaleInvoiceDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            SaleInvoiceDataView.ColumnHeadersHeight = 50;
            SaleInvoiceDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            SaleInvoiceDataView.Columns.AddRange(new DataGridViewColumn[] { InvoiceId, InvoiceTotal });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle2.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            SaleInvoiceDataView.DefaultCellStyle = dataGridViewCellStyle2;
            SaleInvoiceDataView.Dock = DockStyle.Fill;
            SaleInvoiceDataView.EnableHeadersVisualStyles = false;
            SaleInvoiceDataView.GridColor = Color.DimGray;
            SaleInvoiceDataView.Location = new Point(0, 40);
            SaleInvoiceDataView.MultiSelect = false;
            SaleInvoiceDataView.Name = "SaleInvoiceDataView";
            SaleInvoiceDataView.ReadOnly = true;
            SaleInvoiceDataView.RowHeadersVisible = false;
            SaleInvoiceDataView.RowHeadersWidth = 60;
            SaleInvoiceDataView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            SaleInvoiceDataView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            SaleInvoiceDataView.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            SaleInvoiceDataView.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            SaleInvoiceDataView.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            SaleInvoiceDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            SaleInvoiceDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            SaleInvoiceDataView.RowTemplate.Height = 35;
            SaleInvoiceDataView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            SaleInvoiceDataView.Size = new Size(371, 787);
            SaleInvoiceDataView.TabIndex = 50;
            SaleInvoiceDataView.CellClick += SaleInvoiceDataView_CellClick;
            // 
            // InvoiceId
            // 
            InvoiceId.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            InvoiceId.HeaderText = "Invoice ID";
            InvoiceId.Name = "InvoiceId";
            InvoiceId.ReadOnly = true;
            InvoiceId.Width = 200;
            // 
            // InvoiceTotal
            // 
            InvoiceTotal.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            InvoiceTotal.HeaderText = "ยอดขาย";
            InvoiceTotal.Name = "InvoiceTotal";
            InvoiceTotal.ReadOnly = true;
            InvoiceTotal.Width = 150;
            // 
            // label3
            // 
            label3.BackColor = Color.FromArgb(38, 38, 38);
            label3.Dock = DockStyle.Top;
            label3.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.Gainsboro;
            label3.Location = new Point(0, 0);
            label3.Margin = new Padding(0);
            label3.Name = "label3";
            label3.Size = new Size(371, 40);
            label3.TabIndex = 49;
            label3.Text = "รายการขาย";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel7
            // 
            panel7.BackColor = Color.FromArgb(38, 38, 38);
            panel7.BorderStyle = BorderStyle.FixedSingle;
            panel7.Controls.Add(ShowReportByDateRangeButton);
            panel7.Controls.Add(label11);
            panel7.Controls.Add(label9);
            panel7.Controls.Add(StartDatePicker);
            panel7.Controls.Add(label7);
            panel7.Controls.Add(EndDatePicker);
            panel7.Dock = DockStyle.Fill;
            panel7.Location = new Point(438, 0);
            panel7.Name = "panel7";
            panel7.Size = new Size(1172, 96);
            panel7.TabIndex = 109;
            // 
            // ShowReportByDateRangeButton
            // 
            ShowReportByDateRangeButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowReportByDateRangeButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowReportByDateRangeButton.BorderColor = Color.DarkGray;
            ShowReportByDateRangeButton.BorderRadius = 5;
            ShowReportByDateRangeButton.BorderSize = 1;
            ShowReportByDateRangeButton.FlatAppearance.BorderSize = 0;
            ShowReportByDateRangeButton.FlatStyle = FlatStyle.Flat;
            ShowReportByDateRangeButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ShowReportByDateRangeButton.ForeColor = Color.White;
            ShowReportByDateRangeButton.Location = new Point(561, 29);
            ShowReportByDateRangeButton.Name = "ShowReportByDateRangeButton";
            ShowReportByDateRangeButton.Size = new Size(180, 49);
            ShowReportByDateRangeButton.TabIndex = 103;
            ShowReportByDateRangeButton.Text = "แสดงผล";
            ShowReportByDateRangeButton.TextColor = Color.White;
            ShowReportByDateRangeButton.UseVisualStyleBackColor = false;
            ShowReportByDateRangeButton.Click += ShowReportByDateRangeButton_Click;
            // 
            // label11
            // 
            label11.BackColor = Color.FromArgb(38, 38, 38);
            label11.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label11.ForeColor = Color.Gainsboro;
            label11.Location = new Point(290, 44);
            label11.Name = "label11";
            label11.Size = new Size(56, 31);
            label11.TabIndex = 102;
            label11.Text = "ถึง";
            label11.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            label9.BackColor = Color.FromArgb(38, 38, 38);
            label9.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label9.ForeColor = Color.Gainsboro;
            label9.Location = new Point(57, 44);
            label9.Name = "label9";
            label9.Size = new Size(56, 31);
            label9.TabIndex = 101;
            label9.Text = "จาก";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // StartDatePicker
            // 
            StartDatePicker.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            StartDatePicker.Format = DateTimePickerFormat.Short;
            StartDatePicker.Location = new Point(119, 46);
            StartDatePicker.Name = "StartDatePicker";
            StartDatePicker.Size = new Size(153, 25);
            StartDatePicker.TabIndex = 100;
            // 
            // label7
            // 
            label7.BackColor = Color.FromArgb(38, 38, 38);
            label7.Dock = DockStyle.Top;
            label7.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.Gainsboro;
            label7.Location = new Point(0, 0);
            label7.Name = "label7";
            label7.Size = new Size(1170, 39);
            label7.TabIndex = 83;
            label7.Text = " เลือกช่วงเวลาแบบกำหนดเอง";
            label7.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // EndDatePicker
            // 
            EndDatePicker.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            EndDatePicker.Format = DateTimePickerFormat.Short;
            EndDatePicker.Location = new Point(352, 46);
            EndDatePicker.Name = "EndDatePicker";
            EndDatePicker.Size = new Size(153, 25);
            EndDatePicker.TabIndex = 84;
            // 
            // panel6
            // 
            panel6.BackColor = Color.FromArgb(38, 38, 38);
            panel6.BorderStyle = BorderStyle.FixedSingle;
            panel6.Controls.Add(PeriodLabel);
            panel6.Controls.Add(ShowReportByThisYearButton);
            panel6.Controls.Add(ShowReportByTodayButton);
            panel6.Controls.Add(ShowReportByThisMonthButton);
            panel6.Dock = DockStyle.Left;
            panel6.Location = new Point(0, 0);
            panel6.Name = "panel6";
            panel6.Size = new Size(438, 96);
            panel6.TabIndex = 108;
            // 
            // PeriodLabel
            // 
            PeriodLabel.BackColor = Color.FromArgb(38, 38, 38);
            PeriodLabel.Dock = DockStyle.Top;
            PeriodLabel.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PeriodLabel.ForeColor = Color.MediumAquamarine;
            PeriodLabel.Location = new Point(0, 0);
            PeriodLabel.Name = "PeriodLabel";
            PeriodLabel.Size = new Size(436, 39);
            PeriodLabel.TabIndex = 83;
            PeriodLabel.Text = "เลือกเวลา";
            PeriodLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ShowReportByThisYearButton
            // 
            ShowReportByThisYearButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowReportByThisYearButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowReportByThisYearButton.BorderColor = Color.Gray;
            ShowReportByThisYearButton.BorderRadius = 5;
            ShowReportByThisYearButton.BorderSize = 1;
            ShowReportByThisYearButton.FlatAppearance.BorderSize = 0;
            ShowReportByThisYearButton.FlatStyle = FlatStyle.Flat;
            ShowReportByThisYearButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ShowReportByThisYearButton.ForeColor = Color.White;
            ShowReportByThisYearButton.Location = new Point(286, 42);
            ShowReportByThisYearButton.Name = "ShowReportByThisYearButton";
            ShowReportByThisYearButton.Size = new Size(130, 36);
            ShowReportByThisYearButton.TabIndex = 97;
            ShowReportByThisYearButton.Text = "ปีนี้";
            ShowReportByThisYearButton.TextColor = Color.White;
            ShowReportByThisYearButton.UseVisualStyleBackColor = false;
            ShowReportByThisYearButton.Click += ShowReportByThisYearButton_Click;
            // 
            // ShowReportByTodayButton
            // 
            ShowReportByTodayButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowReportByTodayButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowReportByTodayButton.BorderColor = Color.Gray;
            ShowReportByTodayButton.BorderRadius = 5;
            ShowReportByTodayButton.BorderSize = 1;
            ShowReportByTodayButton.FlatAppearance.BorderSize = 0;
            ShowReportByTodayButton.FlatStyle = FlatStyle.Flat;
            ShowReportByTodayButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ShowReportByTodayButton.ForeColor = Color.White;
            ShowReportByTodayButton.Location = new Point(16, 42);
            ShowReportByTodayButton.Name = "ShowReportByTodayButton";
            ShowReportByTodayButton.Size = new Size(130, 36);
            ShowReportByTodayButton.TabIndex = 94;
            ShowReportByTodayButton.Text = "วันนี้";
            ShowReportByTodayButton.TextColor = Color.White;
            ShowReportByTodayButton.UseVisualStyleBackColor = false;
            ShowReportByTodayButton.Click += ShowReportByTodayButton_Click;
            // 
            // ShowReportByThisMonthButton
            // 
            ShowReportByThisMonthButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowReportByThisMonthButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowReportByThisMonthButton.BorderColor = Color.Gray;
            ShowReportByThisMonthButton.BorderRadius = 5;
            ShowReportByThisMonthButton.BorderSize = 1;
            ShowReportByThisMonthButton.FlatAppearance.BorderSize = 0;
            ShowReportByThisMonthButton.FlatStyle = FlatStyle.Flat;
            ShowReportByThisMonthButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ShowReportByThisMonthButton.ForeColor = Color.White;
            ShowReportByThisMonthButton.Location = new Point(151, 42);
            ShowReportByThisMonthButton.Name = "ShowReportByThisMonthButton";
            ShowReportByThisMonthButton.Size = new Size(130, 36);
            ShowReportByThisMonthButton.TabIndex = 96;
            ShowReportByThisMonthButton.Text = "เดือนนี้";
            ShowReportByThisMonthButton.TextColor = Color.White;
            ShowReportByThisMonthButton.UseVisualStyleBackColor = false;
            ShowReportByThisMonthButton.Click += ShowReportByThisMonthButton_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(panel7);
            panel2.Controls.Add(panel6);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(1610, 96);
            panel2.TabIndex = 110;
            // 
            // panel3
            // 
            panel3.Controls.Add(panel4);
            panel3.Controls.Add(panel5);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(373, 96);
            panel3.Name = "panel3";
            panel3.Size = new Size(1237, 829);
            panel3.TabIndex = 111;
            // 
            // panel4
            // 
            panel4.BorderStyle = BorderStyle.FixedSingle;
            panel4.Controls.Add(InvoiceProductsDataView);
            panel4.Controls.Add(label1);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(0, 0);
            panel4.Name = "panel4";
            panel4.Size = new Size(1237, 619);
            panel4.TabIndex = 0;
            // 
            // InvoiceProductsDataView
            // 
            InvoiceProductsDataView.AllowUserToAddRows = false;
            InvoiceProductsDataView.AllowUserToDeleteRows = false;
            InvoiceProductsDataView.AllowUserToResizeColumns = false;
            InvoiceProductsDataView.AllowUserToResizeRows = false;
            InvoiceProductsDataView.BackgroundColor = Color.FromArgb(38, 38, 38);
            InvoiceProductsDataView.BorderStyle = BorderStyle.None;
            InvoiceProductsDataView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            InvoiceProductsDataView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle3.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle3.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = Color.Gray;
            InvoiceProductsDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            InvoiceProductsDataView.ColumnHeadersHeight = 50;
            InvoiceProductsDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            InvoiceProductsDataView.Columns.AddRange(new DataGridViewColumn[] { Priority, ProductCode, Description, Quantity, UnitPrice, Total });
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle4.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle4.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            InvoiceProductsDataView.DefaultCellStyle = dataGridViewCellStyle4;
            InvoiceProductsDataView.Dock = DockStyle.Fill;
            InvoiceProductsDataView.EnableHeadersVisualStyles = false;
            InvoiceProductsDataView.GridColor = Color.DimGray;
            InvoiceProductsDataView.Location = new Point(0, 40);
            InvoiceProductsDataView.MultiSelect = false;
            InvoiceProductsDataView.Name = "InvoiceProductsDataView";
            InvoiceProductsDataView.ReadOnly = true;
            InvoiceProductsDataView.RowHeadersVisible = false;
            InvoiceProductsDataView.RowHeadersWidth = 60;
            InvoiceProductsDataView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            InvoiceProductsDataView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            InvoiceProductsDataView.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            InvoiceProductsDataView.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            InvoiceProductsDataView.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            InvoiceProductsDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            InvoiceProductsDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            InvoiceProductsDataView.RowTemplate.Height = 35;
            InvoiceProductsDataView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            InvoiceProductsDataView.Size = new Size(1235, 577);
            InvoiceProductsDataView.TabIndex = 51;
            // 
            // Priority
            // 
            Priority.HeaderText = "ลำดับ";
            Priority.Name = "Priority";
            Priority.ReadOnly = true;
            // 
            // ProductCode
            // 
            ProductCode.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            ProductCode.HeaderText = "รหัสสินค้า";
            ProductCode.Name = "ProductCode";
            ProductCode.ReadOnly = true;
            ProductCode.Width = 200;
            // 
            // Description
            // 
            Description.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Description.HeaderText = "คำอธิบาย";
            Description.Name = "Description";
            Description.ReadOnly = true;
            Description.Width = 350;
            // 
            // Quantity
            // 
            Quantity.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Quantity.HeaderText = "จำนวน";
            Quantity.Name = "Quantity";
            Quantity.ReadOnly = true;
            // 
            // UnitPrice
            // 
            UnitPrice.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            UnitPrice.HeaderText = "ราคาต่อหน่วย";
            UnitPrice.Name = "UnitPrice";
            UnitPrice.ReadOnly = true;
            UnitPrice.Width = 150;
            // 
            // Total
            // 
            Total.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Total.HeaderText = "ราคารวม";
            Total.Name = "Total";
            Total.ReadOnly = true;
            Total.Width = 150;
            // 
            // label1
            // 
            label1.BackColor = Color.FromArgb(38, 38, 38);
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Gainsboro;
            label1.Location = new Point(0, 0);
            label1.Margin = new Padding(0);
            label1.Name = "label1";
            label1.Size = new Size(1235, 40);
            label1.TabIndex = 50;
            label1.Text = "รายการสินค้า";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel5
            // 
            panel5.BorderStyle = BorderStyle.FixedSingle;
            panel5.Controls.Add(PrintReceiptButton);
            panel5.Controls.Add(PaymentDataView);
            panel5.Controls.Add(label2);
            panel5.Dock = DockStyle.Bottom;
            panel5.Location = new Point(0, 619);
            panel5.Name = "panel5";
            panel5.Size = new Size(1237, 210);
            panel5.TabIndex = 1;
            // 
            // PaymentDataView
            // 
            PaymentDataView.AllowUserToAddRows = false;
            PaymentDataView.AllowUserToDeleteRows = false;
            PaymentDataView.AllowUserToResizeColumns = false;
            PaymentDataView.AllowUserToResizeRows = false;
            PaymentDataView.BackgroundColor = Color.FromArgb(38, 38, 38);
            PaymentDataView.BorderStyle = BorderStyle.None;
            PaymentDataView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            PaymentDataView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle5.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle5.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle5.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle5.SelectionBackColor = Color.Gray;
            PaymentDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            PaymentDataView.ColumnHeadersHeight = 50;
            PaymentDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            PaymentDataView.Columns.AddRange(new DataGridViewColumn[] { PaymentPriority, PaymentType, PaymentAmount, Note });
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle6.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle6.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle6.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
            PaymentDataView.DefaultCellStyle = dataGridViewCellStyle6;
            PaymentDataView.Dock = DockStyle.Fill;
            PaymentDataView.EnableHeadersVisualStyles = false;
            PaymentDataView.GridColor = Color.DimGray;
            PaymentDataView.Location = new Point(0, 40);
            PaymentDataView.MultiSelect = false;
            PaymentDataView.Name = "PaymentDataView";
            PaymentDataView.ReadOnly = true;
            PaymentDataView.RowHeadersVisible = false;
            PaymentDataView.RowHeadersWidth = 60;
            PaymentDataView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            PaymentDataView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            PaymentDataView.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            PaymentDataView.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PaymentDataView.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            PaymentDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            PaymentDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            PaymentDataView.RowTemplate.Height = 35;
            PaymentDataView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            PaymentDataView.Size = new Size(1235, 168);
            PaymentDataView.TabIndex = 51;
            // 
            // PaymentPriority
            // 
            PaymentPriority.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            PaymentPriority.HeaderText = "ลำดับ";
            PaymentPriority.Name = "PaymentPriority";
            PaymentPriority.ReadOnly = true;
            // 
            // PaymentType
            // 
            PaymentType.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            PaymentType.HeaderText = "ประเภทการชำระเงิน";
            PaymentType.Name = "PaymentType";
            PaymentType.ReadOnly = true;
            PaymentType.Width = 300;
            // 
            // PaymentAmount
            // 
            PaymentAmount.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            PaymentAmount.HeaderText = "จำนวนเงิน";
            PaymentAmount.Name = "PaymentAmount";
            PaymentAmount.ReadOnly = true;
            PaymentAmount.Width = 250;
            // 
            // Note
            // 
            Note.HeaderText = "Note";
            Note.Name = "Note";
            Note.ReadOnly = true;
            Note.Width = 200;
            // 
            // label2
            // 
            label2.BackColor = Color.FromArgb(38, 38, 38);
            label2.Dock = DockStyle.Top;
            label2.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.Gainsboro;
            label2.Location = new Point(0, 0);
            label2.Margin = new Padding(0);
            label2.Name = "label2";
            label2.Size = new Size(1235, 40);
            label2.TabIndex = 50;
            label2.Text = "รายการเงิน";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // PrintReceiptButton
            // 
            PrintReceiptButton.BackColor = Color.FromArgb(38, 38, 38);
            PrintReceiptButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            PrintReceiptButton.BorderColor = Color.FromArgb(50, 190, 166);
            PrintReceiptButton.BorderRadius = 19;
            PrintReceiptButton.BorderSize = 1;
            PrintReceiptButton.FlatAppearance.BorderSize = 0;
            PrintReceiptButton.FlatStyle = FlatStyle.Flat;
            PrintReceiptButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PrintReceiptButton.ForeColor = Color.White;
            PrintReceiptButton.Image = Properties.Resources.Receipt_80;
            PrintReceiptButton.Location = new Point(1045, 40);
            PrintReceiptButton.Name = "PrintReceiptButton";
            PrintReceiptButton.Size = new Size(153, 137);
            PrintReceiptButton.TabIndex = 52;
            PrintReceiptButton.TextColor = Color.White;
            PrintReceiptButton.UseVisualStyleBackColor = false;
            PrintReceiptButton.Click += PrintReceiptButton_Click;
            // 
            // SalesHistoryReportPanel
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(panel3);
            Controls.Add(panel1);
            Controls.Add(panel2);
            Name = "SalesHistoryReportPanel";
            Size = new Size(1610, 925);
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)SaleInvoiceDataView).EndInit();
            panel7.ResumeLayout(false);
            panel6.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)InvoiceProductsDataView).EndInit();
            panel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PaymentDataView).EndInit();
            ResumeLayout(false);
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
        private ModernUI.ModernButton PrintReceiptButton;
    }
}
