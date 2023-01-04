namespace IndyPOS.Windows.Forms.UI.Report
{
    partial class InvoiceProductsReportPanel
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel8 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.HardwareProductsOnlyButton = new System.Windows.Forms.RadioButton();
            this.GeneralProductsOnlyButton = new System.Windows.Forms.RadioButton();
            this.AllProductGroupsButton = new System.Windows.Forms.RadioButton();
            this.StartDatePicker = new System.Windows.Forms.DateTimePicker();
            this.InvoiceProductsDataView = new System.Windows.Forms.DataGridView();
            this.Priority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label3 = new System.Windows.Forms.Label();
            this.EndDatePicker = new System.Windows.Forms.DateTimePicker();
            this.label9 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.ShowProductsByDateRangeButton = new ModernUI.ModernButton();
            this.panel8.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceProductsDataView)).BeginInit();
            this.SuspendLayout();
            // 
            // panel8
            // 
            this.panel8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel8.Controls.Add(this.ShowProductsByDateRangeButton);
            this.panel8.Controls.Add(this.label11);
            this.panel8.Controls.Add(this.label9);
            this.panel8.Controls.Add(this.EndDatePicker);
            this.panel8.Controls.Add(this.groupBox1);
            this.panel8.Controls.Add(this.StartDatePicker);
            this.panel8.Controls.Add(this.InvoiceProductsDataView);
            this.panel8.Controls.Add(this.label3);
            this.panel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel8.Location = new System.Drawing.Point(0, 0);
            this.panel8.Name = "panel8";
            this.panel8.Padding = new System.Windows.Forms.Padding(3);
            this.panel8.Size = new System.Drawing.Size(1610, 925);
            this.panel8.TabIndex = 4;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.groupBox1.Controls.Add(this.HardwareProductsOnlyButton);
            this.groupBox1.Controls.Add(this.GeneralProductsOnlyButton);
            this.groupBox1.Controls.Add(this.AllProductGroupsButton);
            this.groupBox1.Location = new System.Drawing.Point(614, 3);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(369, 34);
            this.groupBox1.TabIndex = 102;
            this.groupBox1.TabStop = false;
            // 
            // HardwareProductsOnlyButton
            // 
            this.HardwareProductsOnlyButton.AutoSize = true;
            this.HardwareProductsOnlyButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HardwareProductsOnlyButton.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.HardwareProductsOnlyButton.Location = new System.Drawing.Point(214, 11);
            this.HardwareProductsOnlyButton.Name = "HardwareProductsOnlyButton";
            this.HardwareProductsOnlyButton.Size = new System.Drawing.Size(144, 20);
            this.HardwareProductsOnlyButton.TabIndex = 2;
            this.HardwareProductsOnlyButton.TabStop = true;
            this.HardwareProductsOnlyButton.Text = "Hardware Products Only";
            this.HardwareProductsOnlyButton.UseVisualStyleBackColor = true;
            this.HardwareProductsOnlyButton.Click += new System.EventHandler(this.HardwareProductsOnlyButton_Click);
            // 
            // GeneralProductsOnlyButton
            // 
            this.GeneralProductsOnlyButton.AutoSize = true;
            this.GeneralProductsOnlyButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GeneralProductsOnlyButton.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.GeneralProductsOnlyButton.Location = new System.Drawing.Point(72, 11);
            this.GeneralProductsOnlyButton.Name = "GeneralProductsOnlyButton";
            this.GeneralProductsOnlyButton.Size = new System.Drawing.Size(136, 20);
            this.GeneralProductsOnlyButton.TabIndex = 1;
            this.GeneralProductsOnlyButton.TabStop = true;
            this.GeneralProductsOnlyButton.Text = "General Products Only";
            this.GeneralProductsOnlyButton.UseVisualStyleBackColor = true;
            this.GeneralProductsOnlyButton.Click += new System.EventHandler(this.GeneralProductsOnlyButton_Click);
            // 
            // AllProductGroupsButton
            // 
            this.AllProductGroupsButton.AutoSize = true;
            this.AllProductGroupsButton.Checked = true;
            this.AllProductGroupsButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AllProductGroupsButton.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.AllProductGroupsButton.Location = new System.Drawing.Point(16, 11);
            this.AllProductGroupsButton.Name = "AllProductGroupsButton";
            this.AllProductGroupsButton.Size = new System.Drawing.Size(38, 20);
            this.AllProductGroupsButton.TabIndex = 0;
            this.AllProductGroupsButton.TabStop = true;
            this.AllProductGroupsButton.Text = "All";
            this.AllProductGroupsButton.UseVisualStyleBackColor = true;
            this.AllProductGroupsButton.Click += new System.EventHandler(this.AllProductGroupsButton_Click);
            // 
            // StartDatePicker
            // 
            this.StartDatePicker.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StartDatePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.StartDatePicker.Location = new System.Drawing.Point(231, 11);
            this.StartDatePicker.Name = "StartDatePicker";
            this.StartDatePicker.Size = new System.Drawing.Size(144, 25);
            this.StartDatePicker.TabIndex = 101;
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
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle9.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle9.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.Color.Gray;
            this.InvoiceProductsDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle9;
            this.InvoiceProductsDataView.ColumnHeadersHeight = 50;
            this.InvoiceProductsDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.InvoiceProductsDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Priority,
            this.ProductCode,
            this.Description,
            this.Quantity,
            this.UnitPrice,
            this.Total});
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle10.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.InvoiceProductsDataView.DefaultCellStyle = dataGridViewCellStyle10;
            this.InvoiceProductsDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InvoiceProductsDataView.EnableHeadersVisualStyles = false;
            this.InvoiceProductsDataView.GridColor = System.Drawing.Color.DimGray;
            this.InvoiceProductsDataView.Location = new System.Drawing.Point(3, 43);
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
            this.InvoiceProductsDataView.Size = new System.Drawing.Size(1604, 879);
            this.InvoiceProductsDataView.TabIndex = 1;
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
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Gainsboro;
            this.label3.Location = new System.Drawing.Point(3, 3);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(1604, 40);
            this.label3.TabIndex = 48;
            this.label3.Text = "รายการสินค้า";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // EndDatePicker
            // 
            this.EndDatePicker.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EndDatePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.EndDatePicker.Location = new System.Drawing.Point(443, 11);
            this.EndDatePicker.Name = "EndDatePicker";
            this.EndDatePicker.Size = new System.Drawing.Size(144, 25);
            this.EndDatePicker.TabIndex = 103;
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label9.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.Gainsboro;
            this.label9.Location = new System.Drawing.Point(169, 9);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(56, 31);
            this.label9.TabIndex = 104;
            this.label9.Text = "จาก";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Gainsboro;
            this.label11.Location = new System.Drawing.Point(381, 9);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(56, 31);
            this.label11.TabIndex = 105;
            this.label11.Text = "ถึง";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ShowProductsByDateRangeButton
            // 
            this.ShowProductsByDateRangeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowProductsByDateRangeButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ShowProductsByDateRangeButton.BorderColor = System.Drawing.Color.DarkGray;
            this.ShowProductsByDateRangeButton.BorderRadius = 5;
            this.ShowProductsByDateRangeButton.BorderSize = 1;
            this.ShowProductsByDateRangeButton.FlatAppearance.BorderSize = 0;
            this.ShowProductsByDateRangeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowProductsByDateRangeButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShowProductsByDateRangeButton.ForeColor = System.Drawing.Color.White;
            this.ShowProductsByDateRangeButton.Location = new System.Drawing.Point(1012, 3);
            this.ShowProductsByDateRangeButton.Name = "ShowProductsByDateRangeButton";
            this.ShowProductsByDateRangeButton.Size = new System.Drawing.Size(180, 34);
            this.ShowProductsByDateRangeButton.TabIndex = 106;
            this.ShowProductsByDateRangeButton.Text = "แสดงผล";
            this.ShowProductsByDateRangeButton.TextColor = System.Drawing.Color.White;
            this.ShowProductsByDateRangeButton.UseVisualStyleBackColor = false;
            this.ShowProductsByDateRangeButton.Click += new System.EventHandler(this.ShowProductsByDateRangeButton_Click);
            // 
            // InvoiceProductsReportPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.Controls.Add(this.panel8);
            this.Name = "InvoiceProductsReportPanel";
            this.Size = new System.Drawing.Size(1610, 925);
            this.panel8.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceProductsDataView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.DataGridView InvoiceProductsDataView;
        private System.Windows.Forms.DataGridViewTextBoxColumn Priority;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn Total;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker StartDatePicker;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton HardwareProductsOnlyButton;
        private System.Windows.Forms.RadioButton GeneralProductsOnlyButton;
        private System.Windows.Forms.RadioButton AllProductGroupsButton;
        private System.Windows.Forms.DateTimePicker EndDatePicker;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label11;
        private ModernUI.ModernButton ShowProductsByDateRangeButton;
    }
}
