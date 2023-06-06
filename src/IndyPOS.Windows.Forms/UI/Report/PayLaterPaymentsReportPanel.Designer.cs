namespace IndyPOS.Windows.Forms.UI.Report
{
    partial class PayLaterPaymentsReportPanel
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
            var dataGridViewCellStyle5 = new DataGridViewCellStyle();
            var dataGridViewCellStyle6 = new DataGridViewCellStyle();
            PayLaterPaymentsSummaryDataView = new DataGridView();
            Description = new DataGridViewTextBoxColumn();
            ReceivableAmountTotal = new DataGridViewTextBoxColumn();
            RemainingAmountTotal = new DataGridViewTextBoxColumn();
            panel6 = new Panel();
            PeriodLabel = new Label();
            ShowReportByThisYearButton = new ModernUI.ModernButton();
            ShowReportByTodayButton = new ModernUI.ModernButton();
            ShowReportByThisMonthButton = new ModernUI.ModernButton();
            panel1 = new Panel();
            ShowAllPayLaterPaymentsButton = new ModernUI.ModernButton();
            ((System.ComponentModel.ISupportInitialize)PayLaterPaymentsSummaryDataView).BeginInit();
            panel6.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // PayLaterPaymentsSummaryDataView
            // 
            PayLaterPaymentsSummaryDataView.AllowUserToAddRows = false;
            PayLaterPaymentsSummaryDataView.AllowUserToDeleteRows = false;
            PayLaterPaymentsSummaryDataView.AllowUserToResizeColumns = false;
            PayLaterPaymentsSummaryDataView.AllowUserToResizeRows = false;
            PayLaterPaymentsSummaryDataView.BackgroundColor = Color.FromArgb(38, 38, 38);
            PayLaterPaymentsSummaryDataView.BorderStyle = BorderStyle.None;
            PayLaterPaymentsSummaryDataView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            PayLaterPaymentsSummaryDataView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle5.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle5.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle5.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle5.SelectionBackColor = Color.Gray;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.False;
            PayLaterPaymentsSummaryDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            PayLaterPaymentsSummaryDataView.ColumnHeadersHeight = 50;
            PayLaterPaymentsSummaryDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            PayLaterPaymentsSummaryDataView.Columns.AddRange(new DataGridViewColumn[] { Description, ReceivableAmountTotal, RemainingAmountTotal });
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle6.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle6.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle6.SelectionForeColor = Color.Gainsboro;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
            PayLaterPaymentsSummaryDataView.DefaultCellStyle = dataGridViewCellStyle6;
            PayLaterPaymentsSummaryDataView.Dock = DockStyle.Left;
            PayLaterPaymentsSummaryDataView.EnableHeadersVisualStyles = false;
            PayLaterPaymentsSummaryDataView.GridColor = Color.DimGray;
            PayLaterPaymentsSummaryDataView.Location = new Point(0, 96);
            PayLaterPaymentsSummaryDataView.MultiSelect = false;
            PayLaterPaymentsSummaryDataView.Name = "PayLaterPaymentsSummaryDataView";
            PayLaterPaymentsSummaryDataView.RowHeadersVisible = false;
            PayLaterPaymentsSummaryDataView.RowHeadersWidth = 60;
            PayLaterPaymentsSummaryDataView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            PayLaterPaymentsSummaryDataView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            PayLaterPaymentsSummaryDataView.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            PayLaterPaymentsSummaryDataView.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            PayLaterPaymentsSummaryDataView.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            PayLaterPaymentsSummaryDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            PayLaterPaymentsSummaryDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            PayLaterPaymentsSummaryDataView.RowTemplate.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            PayLaterPaymentsSummaryDataView.RowTemplate.Height = 35;
            PayLaterPaymentsSummaryDataView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            PayLaterPaymentsSummaryDataView.Size = new Size(758, 829);
            PayLaterPaymentsSummaryDataView.TabIndex = 4;
            // 
            // Description
            // 
            Description.HeaderText = "คำอธิบาย";
            Description.Name = "Description";
            Description.ReadOnly = true;
            Description.Width = 250;
            // 
            // ReceivableAmountTotal
            // 
            ReceivableAmountTotal.HeaderText = "ยอดลงบัญชีสะสม";
            ReceivableAmountTotal.Name = "ReceivableAmountTotal";
            ReceivableAmountTotal.ReadOnly = true;
            ReceivableAmountTotal.Width = 250;
            // 
            // RemainingAmountTotal
            // 
            RemainingAmountTotal.HeaderText = "ยอดค้างชำระสะสม";
            RemainingAmountTotal.Name = "RemainingAmountTotal";
            RemainingAmountTotal.Width = 250;
            // 
            // panel6
            // 
            panel6.BackColor = Color.FromArgb(38, 38, 38);
            panel6.Controls.Add(ShowAllPayLaterPaymentsButton);
            panel6.Controls.Add(PeriodLabel);
            panel6.Controls.Add(ShowReportByThisYearButton);
            panel6.Controls.Add(ShowReportByTodayButton);
            panel6.Controls.Add(ShowReportByThisMonthButton);
            panel6.Location = new Point(3, 3);
            panel6.Name = "panel6";
            panel6.Size = new Size(574, 92);
            panel6.TabIndex = 107;
            // 
            // PeriodLabel
            // 
            PeriodLabel.BackColor = Color.FromArgb(38, 38, 38);
            PeriodLabel.Dock = DockStyle.Top;
            PeriodLabel.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            PeriodLabel.ForeColor = Color.MediumAquamarine;
            PeriodLabel.Location = new Point(0, 0);
            PeriodLabel.Name = "PeriodLabel";
            PeriodLabel.Size = new Size(574, 39);
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
            ShowReportByThisYearButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
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
            ShowReportByTodayButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
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
            ShowReportByThisMonthButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
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
            // panel1
            // 
            panel1.Controls.Add(panel6);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1610, 96);
            panel1.TabIndex = 108;
            // 
            // ShowAllPayLaterPaymentsButton
            // 
            ShowAllPayLaterPaymentsButton.BackColor = Color.FromArgb(38, 38, 38);
            ShowAllPayLaterPaymentsButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ShowAllPayLaterPaymentsButton.BorderColor = Color.Gray;
            ShowAllPayLaterPaymentsButton.BorderRadius = 5;
            ShowAllPayLaterPaymentsButton.BorderSize = 1;
            ShowAllPayLaterPaymentsButton.FlatAppearance.BorderSize = 0;
            ShowAllPayLaterPaymentsButton.FlatStyle = FlatStyle.Flat;
            ShowAllPayLaterPaymentsButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ShowAllPayLaterPaymentsButton.ForeColor = Color.White;
            ShowAllPayLaterPaymentsButton.Location = new Point(422, 42);
            ShowAllPayLaterPaymentsButton.Name = "ShowAllPayLaterPaymentsButton";
            ShowAllPayLaterPaymentsButton.Size = new Size(130, 36);
            ShowAllPayLaterPaymentsButton.TabIndex = 98;
            ShowAllPayLaterPaymentsButton.Text = "ทั้งหมด";
            ShowAllPayLaterPaymentsButton.TextColor = Color.White;
            ShowAllPayLaterPaymentsButton.UseVisualStyleBackColor = false;
            ShowAllPayLaterPaymentsButton.Click += ShowAllPayLaterPaymentsButton_Click;
            // 
            // PayLaterPaymentsReportPanel
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(38, 38, 38);
            Controls.Add(PayLaterPaymentsSummaryDataView);
            Controls.Add(panel1);
            Name = "PayLaterPaymentsReportPanel";
            Size = new Size(1610, 925);
            ((System.ComponentModel.ISupportInitialize)PayLaterPaymentsSummaryDataView).EndInit();
            panel6.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView PayLaterPaymentsSummaryDataView;
        private DataGridViewTextBoxColumn Description;
        private DataGridViewTextBoxColumn ReceivableAmountTotal;
        private DataGridViewTextBoxColumn RemainingAmountTotal;
        private Panel panel6;
        private Label PeriodLabel;
        private ModernUI.ModernButton ShowReportByThisYearButton;
        private ModernUI.ModernButton ShowReportByTodayButton;
        private ModernUI.ModernButton ShowReportByThisMonthButton;
        private ModernUI.ModernButton ShowAllPayLaterPaymentsButton;
        private Panel panel1;
    }
}
