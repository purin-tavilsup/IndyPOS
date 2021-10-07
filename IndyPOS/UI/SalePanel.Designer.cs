namespace IndyPOS.UI
{
    partial class SalePanel
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
			this.label1 = new System.Windows.Forms.Label();
			this.TotalLabel = new System.Windows.Forms.Label();
			this.GetPaymentButton = new System.Windows.Forms.Button();
			this.TotalPaymentsLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.ChangesLabel = new System.Windows.Forms.Label();
			this.SaveSaleInvoiceButton = new System.Windows.Forms.Button();
			this.CancelSaleInvoiceButton = new System.Windows.Forms.Button();
			this.InvoiceDataView = new System.Windows.Forms.DataGridView();
			this.Priority = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel8 = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.panel9 = new System.Windows.Forms.Panel();
			this.PaymentDataView = new System.Windows.Forms.DataGridView();
			this.PaymentPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PaymentType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PaymentAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.label5 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).BeginInit();
			this.panel2.SuspendLayout();
			this.panel8.SuspendLayout();
			this.panel9.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.Gainsboro;
			this.label1.Location = new System.Drawing.Point(3, 3);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(360, 101);
			this.label1.TabIndex = 1;
			this.label1.Text = "ยอดที่ต้องชำระ";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// TotalLabel
			// 
			this.TotalLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.TotalLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.TotalLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TotalLabel.ForeColor = System.Drawing.Color.PaleGreen;
			this.TotalLabel.Location = new System.Drawing.Point(3, 104);
			this.TotalLabel.Name = "TotalLabel";
			this.TotalLabel.Size = new System.Drawing.Size(360, 79);
			this.TotalLabel.TabIndex = 0;
			this.TotalLabel.Text = "0.00";
			this.TotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// GetPaymentButton
			// 
			this.GetPaymentButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.GetPaymentButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.GetPaymentButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.GetPaymentButton.ForeColor = System.Drawing.Color.Gainsboro;
			this.GetPaymentButton.Image = global::IndyPOS.Properties.Resources.Money_50;
			this.GetPaymentButton.Location = new System.Drawing.Point(3, 188);
			this.GetPaymentButton.Name = "GetPaymentButton";
			this.GetPaymentButton.Size = new System.Drawing.Size(360, 164);
			this.GetPaymentButton.TabIndex = 6;
			this.GetPaymentButton.Text = "รับเงิน";
			this.GetPaymentButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.GetPaymentButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.GetPaymentButton.UseVisualStyleBackColor = false;
			this.GetPaymentButton.Click += new System.EventHandler(this.GetPaymentButton_Click);
			// 
			// TotalPaymentsLabel
			// 
			this.TotalPaymentsLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.TotalPaymentsLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.TotalPaymentsLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TotalPaymentsLabel.ForeColor = System.Drawing.Color.PaleGreen;
			this.TotalPaymentsLabel.Location = new System.Drawing.Point(3, 352);
			this.TotalPaymentsLabel.Name = "TotalPaymentsLabel";
			this.TotalPaymentsLabel.Size = new System.Drawing.Size(360, 79);
			this.TotalPaymentsLabel.TabIndex = 2;
			this.TotalPaymentsLabel.Text = "0.00";
			this.TotalPaymentsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.label4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.Gainsboro;
			this.label4.Location = new System.Drawing.Point(3, 436);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(360, 101);
			this.label4.TabIndex = 5;
			this.label4.Text = "เงินทอน";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChangesLabel
			// 
			this.ChangesLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.ChangesLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.ChangesLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChangesLabel.ForeColor = System.Drawing.Color.PaleGreen;
			this.ChangesLabel.Location = new System.Drawing.Point(3, 537);
			this.ChangesLabel.Name = "ChangesLabel";
			this.ChangesLabel.Size = new System.Drawing.Size(360, 79);
			this.ChangesLabel.TabIndex = 4;
			this.ChangesLabel.Text = "0.00";
			this.ChangesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// SaveSaleInvoiceButton
			// 
			this.SaveSaleInvoiceButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.SaveSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.SaveSaleInvoiceButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SaveSaleInvoiceButton.ForeColor = System.Drawing.Color.Gainsboro;
			this.SaveSaleInvoiceButton.Image = global::IndyPOS.Properties.Resources.Check_50;
			this.SaveSaleInvoiceButton.Location = new System.Drawing.Point(3, 619);
			this.SaveSaleInvoiceButton.Name = "SaveSaleInvoiceButton";
			this.SaveSaleInvoiceButton.Size = new System.Drawing.Size(360, 106);
			this.SaveSaleInvoiceButton.TabIndex = 7;
			this.SaveSaleInvoiceButton.Text = "บันทึกการขาย";
			this.SaveSaleInvoiceButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.SaveSaleInvoiceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.SaveSaleInvoiceButton.UseVisualStyleBackColor = false;
			this.SaveSaleInvoiceButton.Click += new System.EventHandler(this.SaveSaleInvoiceButton_Click);
			// 
			// CancelSaleInvoiceButton
			// 
			this.CancelSaleInvoiceButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.CancelSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CancelSaleInvoiceButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CancelSaleInvoiceButton.ForeColor = System.Drawing.Color.Gainsboro;
			this.CancelSaleInvoiceButton.Image = global::IndyPOS.Properties.Resources.Cross_50;
			this.CancelSaleInvoiceButton.Location = new System.Drawing.Point(3, 726);
			this.CancelSaleInvoiceButton.Name = "CancelSaleInvoiceButton";
			this.CancelSaleInvoiceButton.Size = new System.Drawing.Size(360, 106);
			this.CancelSaleInvoiceButton.TabIndex = 7;
			this.CancelSaleInvoiceButton.Text = "ยกเลิกการขาย";
			this.CancelSaleInvoiceButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.CancelSaleInvoiceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.CancelSaleInvoiceButton.UseVisualStyleBackColor = false;
			this.CancelSaleInvoiceButton.Click += new System.EventHandler(this.CancelSaleInvoiceButton_Click);
			// 
			// InvoiceDataView
			// 
			this.InvoiceDataView.AllowUserToAddRows = false;
			this.InvoiceDataView.AllowUserToDeleteRows = false;
			this.InvoiceDataView.AllowUserToResizeColumns = false;
			this.InvoiceDataView.AllowUserToResizeRows = false;
			this.InvoiceDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.InvoiceDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.InvoiceDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
			this.InvoiceDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
			dataGridViewCellStyle5.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.Color.Gainsboro;
			dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.Gray;
			this.InvoiceDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
			this.InvoiceDataView.ColumnHeadersHeight = 50;
			this.InvoiceDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.InvoiceDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Priority,
            this.ProductCode,
            this.Description,
            this.Quantity,
            this.UnitPrice,
            this.Total});
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			dataGridViewCellStyle6.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.Color.Gainsboro;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.InvoiceDataView.DefaultCellStyle = dataGridViewCellStyle6;
			this.InvoiceDataView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.InvoiceDataView.EnableHeadersVisualStyles = false;
			this.InvoiceDataView.GridColor = System.Drawing.Color.DimGray;
			this.InvoiceDataView.Location = new System.Drawing.Point(3, 43);
			this.InvoiceDataView.MultiSelect = false;
			this.InvoiceDataView.Name = "InvoiceDataView";
			this.InvoiceDataView.ReadOnly = true;
			this.InvoiceDataView.RowHeadersVisible = false;
			this.InvoiceDataView.RowHeadersWidth = 60;
			this.InvoiceDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
			this.InvoiceDataView.RowTemplate.Height = 35;
			this.InvoiceDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.InvoiceDataView.Size = new System.Drawing.Size(1049, 491);
			this.InvoiceDataView.TabIndex = 1;
			this.InvoiceDataView.DoubleClick += new System.EventHandler(this.InvoiceDataView_DoubleClick);
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
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
			this.panel2.Controls.Add(this.panel8);
			this.panel2.Controls.Add(this.panel9);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(1055, 835);
			this.panel2.TabIndex = 2;
			// 
			// panel8
			// 
			this.panel8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
			this.panel8.Controls.Add(this.InvoiceDataView);
			this.panel8.Controls.Add(this.label3);
			this.panel8.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel8.Location = new System.Drawing.Point(0, 0);
			this.panel8.Name = "panel8";
			this.panel8.Padding = new System.Windows.Forms.Padding(3);
			this.panel8.Size = new System.Drawing.Size(1055, 537);
			this.panel8.TabIndex = 3;
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
			this.label3.Size = new System.Drawing.Size(1049, 40);
			this.label3.TabIndex = 48;
			this.label3.Text = "รายการสินค้า";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel9
			// 
			this.panel9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
			this.panel9.Controls.Add(this.PaymentDataView);
			this.panel9.Controls.Add(this.label5);
			this.panel9.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel9.Location = new System.Drawing.Point(0, 537);
			this.panel9.Name = "panel9";
			this.panel9.Padding = new System.Windows.Forms.Padding(3);
			this.panel9.Size = new System.Drawing.Size(1055, 298);
			this.panel9.TabIndex = 4;
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
			dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
			dataGridViewCellStyle7.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle7.ForeColor = System.Drawing.Color.Gainsboro;
			dataGridViewCellStyle7.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
			dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.Gray;
			this.PaymentDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
			this.PaymentDataView.ColumnHeadersHeight = 50;
			this.PaymentDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.PaymentDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PaymentPriority,
            this.PaymentType,
            this.PaymentAmount});
			dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			dataGridViewCellStyle8.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle8.ForeColor = System.Drawing.Color.Gainsboro;
			dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.PaymentDataView.DefaultCellStyle = dataGridViewCellStyle8;
			this.PaymentDataView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PaymentDataView.EnableHeadersVisualStyles = false;
			this.PaymentDataView.GridColor = System.Drawing.Color.DimGray;
			this.PaymentDataView.Location = new System.Drawing.Point(3, 43);
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
			this.PaymentDataView.Size = new System.Drawing.Size(1049, 252);
			this.PaymentDataView.TabIndex = 2;
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
			this.PaymentAmount.HeaderText = "จำนวนเงินที่ชำระ";
			this.PaymentAmount.Name = "PaymentAmount";
			this.PaymentAmount.ReadOnly = true;
			this.PaymentAmount.Width = 250;
			// 
			// label5
			// 
			this.label5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.label5.Dock = System.Windows.Forms.DockStyle.Top;
			this.label5.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.ForeColor = System.Drawing.Color.Gainsboro;
			this.label5.Location = new System.Drawing.Point(3, 3);
			this.label5.Margin = new System.Windows.Forms.Padding(0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(1049, 40);
			this.label5.TabIndex = 49;
			this.label5.Text = "รายการเงินที่ชำระ";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.ChangesLabel);
			this.panel1.Controls.Add(this.GetPaymentButton);
			this.panel1.Controls.Add(this.TotalPaymentsLabel);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.TotalLabel);
			this.panel1.Controls.Add(this.SaveSaleInvoiceButton);
			this.panel1.Controls.Add(this.CancelSaleInvoiceButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel1.Location = new System.Drawing.Point(1055, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(366, 835);
			this.panel1.TabIndex = 3;
			// 
			// SalePanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Name = "SalePanel";
			this.Size = new System.Drawing.Size(1421, 835);
			((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).EndInit();
			this.panel2.ResumeLayout(false);
			this.panel8.ResumeLayout(false);
			this.panel9.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).EndInit();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label TotalLabel;
        private System.Windows.Forms.Button GetPaymentButton;
        private System.Windows.Forms.Label TotalPaymentsLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.Button SaveSaleInvoiceButton;
        private System.Windows.Forms.Button CancelSaleInvoiceButton;
        private System.Windows.Forms.DataGridView InvoiceDataView;
        private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.DataGridView PaymentDataView;
		private System.Windows.Forms.Panel panel9;
		private System.Windows.Forms.Panel panel8;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.DataGridViewTextBoxColumn Priority;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
		private System.Windows.Forms.DataGridViewTextBoxColumn Description;
		private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
		private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
		private System.Windows.Forms.DataGridViewTextBoxColumn Total;
		private System.Windows.Forms.DataGridViewTextBoxColumn PaymentPriority;
		private System.Windows.Forms.DataGridViewTextBoxColumn PaymentType;
		private System.Windows.Forms.DataGridViewTextBoxColumn PaymentAmount;
	}
}
