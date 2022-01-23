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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.InvoiceTotalCaptionLabel = new System.Windows.Forms.Label();
            this.InvoiceTotalLabel = new System.Windows.Forms.Label();
            this.PaymentsTotalLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.ChangesLabel = new System.Windows.Forms.Label();
            this.InvoiceDataView = new System.Windows.Forms.DataGridView();
            this.Priority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel8 = new System.Windows.Forms.Panel();
            this.CancelSaleInvoiceButton = new ModernUI.ModernButton();
            this.label3 = new System.Windows.Forms.Label();
            this.panel9 = new System.Windows.Forms.Panel();
            this.ClearAllPaymentsButton = new ModernUI.ModernButton();
            this.panel3 = new System.Windows.Forms.Panel();
            this.LookUpProductButton = new ModernUI.ModernButton();
            this.ProductLookUpTextBox = new ModernUI.ModernTextBox();
            this.AddHardwareProductButton = new ModernUI.ModernButton();
            this.AddGeneralGoodsProductButton = new ModernUI.ModernButton();
            this.PaymentDataView = new System.Windows.Forms.DataGridView();
            this.PaymentPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaymentType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaymentAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Note = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label5 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.PaymentsTotalCaptionLabel = new System.Windows.Forms.Label();
            this.GetPaymentButton = new System.Windows.Forms.Button();
            this.SaveSaleInvoiceButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel8.SuspendLayout();
            this.panel9.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // InvoiceTotalCaptionLabel
            // 
            this.InvoiceTotalCaptionLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.InvoiceTotalCaptionLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InvoiceTotalCaptionLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.InvoiceTotalCaptionLabel.Location = new System.Drawing.Point(3, 3);
            this.InvoiceTotalCaptionLabel.Name = "InvoiceTotalCaptionLabel";
            this.InvoiceTotalCaptionLabel.Size = new System.Drawing.Size(360, 112);
            this.InvoiceTotalCaptionLabel.TabIndex = 1;
            this.InvoiceTotalCaptionLabel.Text = "ยอดที่ต้องชำระ";
            this.InvoiceTotalCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // InvoiceTotalLabel
            // 
            this.InvoiceTotalLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.InvoiceTotalLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InvoiceTotalLabel.ForeColor = System.Drawing.Color.Salmon;
            this.InvoiceTotalLabel.Location = new System.Drawing.Point(3, 117);
            this.InvoiceTotalLabel.Name = "InvoiceTotalLabel";
            this.InvoiceTotalLabel.Size = new System.Drawing.Size(360, 112);
            this.InvoiceTotalLabel.TabIndex = 0;
            this.InvoiceTotalLabel.Text = "0.00";
            this.InvoiceTotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PaymentsTotalLabel
            // 
            this.PaymentsTotalLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.PaymentsTotalLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PaymentsTotalLabel.ForeColor = System.Drawing.Color.PaleGreen;
            this.PaymentsTotalLabel.Location = new System.Drawing.Point(3, 345);
            this.PaymentsTotalLabel.Name = "PaymentsTotalLabel";
            this.PaymentsTotalLabel.Size = new System.Drawing.Size(360, 112);
            this.PaymentsTotalLabel.TabIndex = 2;
            this.PaymentsTotalLabel.Text = "0.00";
            this.PaymentsTotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Gainsboro;
            this.label4.Location = new System.Drawing.Point(3, 459);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(360, 112);
            this.label4.TabIndex = 5;
            this.label4.Text = "เงินทอน";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ChangesLabel
            // 
            this.ChangesLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.ChangesLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesLabel.ForeColor = System.Drawing.Color.CornflowerBlue;
            this.ChangesLabel.Location = new System.Drawing.Point(2, 573);
            this.ChangesLabel.Name = "ChangesLabel";
            this.ChangesLabel.Size = new System.Drawing.Size(360, 112);
            this.ChangesLabel.TabIndex = 4;
            this.ChangesLabel.Text = "0.00";
            this.ChangesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Gray;
            this.InvoiceDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.InvoiceDataView.ColumnHeadersHeight = 50;
            this.InvoiceDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.InvoiceDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Priority,
            this.ProductCode,
            this.Description,
            this.Quantity,
            this.UnitPrice,
            this.Total,
            this.dataGridViewTextBoxColumn1});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.InvoiceDataView.DefaultCellStyle = dataGridViewCellStyle2;
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
            this.InvoiceDataView.Size = new System.Drawing.Size(1428, 626);
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
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Note";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 200;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel2.Controls.Add(this.panel8);
            this.panel2.Controls.Add(this.panel9);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1434, 970);
            this.panel2.TabIndex = 2;
            // 
            // panel8
            // 
            this.panel8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel8.Controls.Add(this.CancelSaleInvoiceButton);
            this.panel8.Controls.Add(this.InvoiceDataView);
            this.panel8.Controls.Add(this.label3);
            this.panel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel8.Location = new System.Drawing.Point(0, 0);
            this.panel8.Name = "panel8";
            this.panel8.Padding = new System.Windows.Forms.Padding(3);
            this.panel8.Size = new System.Drawing.Size(1434, 672);
            this.panel8.TabIndex = 3;
            // 
            // CancelSaleInvoiceButton
            // 
            this.CancelSaleInvoiceButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CancelSaleInvoiceButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CancelSaleInvoiceButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(79)))), ((int)(((byte)(95)))));
            this.CancelSaleInvoiceButton.BorderRadius = 18;
            this.CancelSaleInvoiceButton.BorderSize = 1;
            this.CancelSaleInvoiceButton.FlatAppearance.BorderSize = 0;
            this.CancelSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CancelSaleInvoiceButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelSaleInvoiceButton.ForeColor = System.Drawing.Color.White;
            this.CancelSaleInvoiceButton.Location = new System.Drawing.Point(1056, 3);
            this.CancelSaleInvoiceButton.Name = "CancelSaleInvoiceButton";
            this.CancelSaleInvoiceButton.Size = new System.Drawing.Size(199, 37);
            this.CancelSaleInvoiceButton.TabIndex = 49;
            this.CancelSaleInvoiceButton.Text = "ยกเลิกการขาย";
            this.CancelSaleInvoiceButton.TextColor = System.Drawing.Color.White;
            this.CancelSaleInvoiceButton.UseVisualStyleBackColor = false;
            this.CancelSaleInvoiceButton.Click += new System.EventHandler(this.CancelSaleInvoiceButton_Click);
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
            this.label3.Size = new System.Drawing.Size(1428, 40);
            this.label3.TabIndex = 48;
            this.label3.Text = "รายการสินค้า";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel9
            // 
            this.panel9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel9.Controls.Add(this.ClearAllPaymentsButton);
            this.panel9.Controls.Add(this.panel3);
            this.panel9.Controls.Add(this.PaymentDataView);
            this.panel9.Controls.Add(this.label5);
            this.panel9.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel9.Location = new System.Drawing.Point(0, 672);
            this.panel9.Name = "panel9";
            this.panel9.Padding = new System.Windows.Forms.Padding(3);
            this.panel9.Size = new System.Drawing.Size(1434, 298);
            this.panel9.TabIndex = 4;
            // 
            // ClearAllPaymentsButton
            // 
            this.ClearAllPaymentsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ClearAllPaymentsButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ClearAllPaymentsButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(79)))), ((int)(((byte)(95)))));
            this.ClearAllPaymentsButton.BorderRadius = 18;
            this.ClearAllPaymentsButton.BorderSize = 1;
            this.ClearAllPaymentsButton.FlatAppearance.BorderSize = 0;
            this.ClearAllPaymentsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ClearAllPaymentsButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ClearAllPaymentsButton.ForeColor = System.Drawing.Color.White;
            this.ClearAllPaymentsButton.Location = new System.Drawing.Point(649, 3);
            this.ClearAllPaymentsButton.Name = "ClearAllPaymentsButton";
            this.ClearAllPaymentsButton.Size = new System.Drawing.Size(199, 37);
            this.ClearAllPaymentsButton.TabIndex = 10;
            this.ClearAllPaymentsButton.Text = "ยกเลิกการชำระเงิน";
            this.ClearAllPaymentsButton.TextColor = System.Drawing.Color.White;
            this.ClearAllPaymentsButton.UseVisualStyleBackColor = false;
            this.ClearAllPaymentsButton.Click += new System.EventHandler(this.ClearAllPaymentsButton_Click);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel3.Controls.Add(this.LookUpProductButton);
            this.panel3.Controls.Add(this.ProductLookUpTextBox);
            this.panel3.Controls.Add(this.AddHardwareProductButton);
            this.panel3.Controls.Add(this.AddGeneralGoodsProductButton);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(1037, 43);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(394, 252);
            this.panel3.TabIndex = 50;
            // 
            // LookUpProductButton
            // 
            this.LookUpProductButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LookUpProductButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LookUpProductButton.BorderColor = System.Drawing.Color.Turquoise;
            this.LookUpProductButton.BorderRadius = 5;
            this.LookUpProductButton.BorderSize = 1;
            this.LookUpProductButton.FlatAppearance.BorderSize = 0;
            this.LookUpProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LookUpProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LookUpProductButton.ForeColor = System.Drawing.Color.White;
            this.LookUpProductButton.Image = global::IndyPOS.Properties.Resources.Search_50;
            this.LookUpProductButton.Location = new System.Drawing.Point(239, 10);
            this.LookUpProductButton.Name = "LookUpProductButton";
            this.LookUpProductButton.Size = new System.Drawing.Size(145, 62);
            this.LookUpProductButton.TabIndex = 51;
            this.LookUpProductButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.LookUpProductButton.TextColor = System.Drawing.Color.White;
            this.LookUpProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.LookUpProductButton.UseVisualStyleBackColor = false;
            this.LookUpProductButton.Click += new System.EventHandler(this.LookUpProductButton_Click);
            // 
            // ProductLookUpTextBox
            // 
            this.ProductLookUpTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ProductLookUpTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.ProductLookUpTextBox.BorderSize = 1;
            this.ProductLookUpTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProductLookUpTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.ProductLookUpTextBox.Location = new System.Drawing.Point(9, 16);
            this.ProductLookUpTextBox.Multiline = false;
            this.ProductLookUpTextBox.Name = "ProductLookUpTextBox";
            this.ProductLookUpTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.ProductLookUpTextBox.PasswordChar = false;
            this.ProductLookUpTextBox.ReadOnly = false;
            this.ProductLookUpTextBox.Size = new System.Drawing.Size(224, 56);
            this.ProductLookUpTextBox.TabIndex = 49;
            this.ProductLookUpTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ProductLookUpTextBox.Texts = "";
            this.ProductLookUpTextBox.UnderlinedStyle = true;
            // 
            // AddHardwareProductButton
            // 
            this.AddHardwareProductButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddHardwareProductButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddHardwareProductButton.BorderColor = System.Drawing.Color.Turquoise;
            this.AddHardwareProductButton.BorderRadius = 5;
            this.AddHardwareProductButton.BorderSize = 1;
            this.AddHardwareProductButton.FlatAppearance.BorderSize = 0;
            this.AddHardwareProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddHardwareProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddHardwareProductButton.ForeColor = System.Drawing.Color.White;
            this.AddHardwareProductButton.Image = global::IndyPOS.Properties.Resources.Plus_50;
            this.AddHardwareProductButton.Location = new System.Drawing.Point(9, 164);
            this.AddHardwareProductButton.Name = "AddHardwareProductButton";
            this.AddHardwareProductButton.Size = new System.Drawing.Size(375, 80);
            this.AddHardwareProductButton.TabIndex = 10;
            this.AddHardwareProductButton.Text = " เพิ่มสินค้า ฮาร์ดแวร์";
            this.AddHardwareProductButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AddHardwareProductButton.TextColor = System.Drawing.Color.White;
            this.AddHardwareProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.AddHardwareProductButton.UseVisualStyleBackColor = false;
            this.AddHardwareProductButton.Click += new System.EventHandler(this.AddHardwareProductButton_Click);
            // 
            // AddGeneralGoodsProductButton
            // 
            this.AddGeneralGoodsProductButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddGeneralGoodsProductButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddGeneralGoodsProductButton.BorderColor = System.Drawing.Color.Turquoise;
            this.AddGeneralGoodsProductButton.BorderRadius = 5;
            this.AddGeneralGoodsProductButton.BorderSize = 1;
            this.AddGeneralGoodsProductButton.FlatAppearance.BorderSize = 0;
            this.AddGeneralGoodsProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddGeneralGoodsProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddGeneralGoodsProductButton.ForeColor = System.Drawing.Color.White;
            this.AddGeneralGoodsProductButton.Image = global::IndyPOS.Properties.Resources.Plus_50;
            this.AddGeneralGoodsProductButton.Location = new System.Drawing.Point(9, 78);
            this.AddGeneralGoodsProductButton.Name = "AddGeneralGoodsProductButton";
            this.AddGeneralGoodsProductButton.Size = new System.Drawing.Size(375, 80);
            this.AddGeneralGoodsProductButton.TabIndex = 9;
            this.AddGeneralGoodsProductButton.Text = " เพิ่มสินค้า เบ็ดเตล็ด";
            this.AddGeneralGoodsProductButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AddGeneralGoodsProductButton.TextColor = System.Drawing.Color.White;
            this.AddGeneralGoodsProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.AddGeneralGoodsProductButton.UseVisualStyleBackColor = false;
            this.AddGeneralGoodsProductButton.Click += new System.EventHandler(this.AddGeneralGoodsProductButton_Click);
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
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.Gray;
            this.PaymentDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.PaymentDataView.ColumnHeadersHeight = 50;
            this.PaymentDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.PaymentDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PaymentPriority,
            this.PaymentType,
            this.PaymentAmount,
            this.Note});
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PaymentDataView.DefaultCellStyle = dataGridViewCellStyle4;
            this.PaymentDataView.Dock = System.Windows.Forms.DockStyle.Left;
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
            this.PaymentDataView.Size = new System.Drawing.Size(855, 252);
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
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label5.Dock = System.Windows.Forms.DockStyle.Top;
            this.label5.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Gainsboro;
            this.label5.Location = new System.Drawing.Point(3, 3);
            this.label5.Margin = new System.Windows.Forms.Padding(0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(1428, 40);
            this.label5.TabIndex = 49;
            this.label5.Text = "รายการเงิน";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel1.Controls.Add(this.PaymentsTotalCaptionLabel);
            this.panel1.Controls.Add(this.GetPaymentButton);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.ChangesLabel);
            this.panel1.Controls.Add(this.PaymentsTotalLabel);
            this.panel1.Controls.Add(this.InvoiceTotalCaptionLabel);
            this.panel1.Controls.Add(this.InvoiceTotalLabel);
            this.panel1.Controls.Add(this.SaveSaleInvoiceButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(1434, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(366, 970);
            this.panel1.TabIndex = 3;
            // 
            // PaymentsTotalCaptionLabel
            // 
            this.PaymentsTotalCaptionLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.PaymentsTotalCaptionLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PaymentsTotalCaptionLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.PaymentsTotalCaptionLabel.Location = new System.Drawing.Point(3, 231);
            this.PaymentsTotalCaptionLabel.Name = "PaymentsTotalCaptionLabel";
            this.PaymentsTotalCaptionLabel.Size = new System.Drawing.Size(360, 112);
            this.PaymentsTotalCaptionLabel.TabIndex = 8;
            this.PaymentsTotalCaptionLabel.Text = "ยอดที่รับมา";
            this.PaymentsTotalCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // GetPaymentButton
            // 
            this.GetPaymentButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.GetPaymentButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.GetPaymentButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GetPaymentButton.ForeColor = System.Drawing.Color.DarkGray;
            this.GetPaymentButton.Image = global::IndyPOS.Properties.Resources.Money_50;
            this.GetPaymentButton.Location = new System.Drawing.Point(3, 688);
            this.GetPaymentButton.Name = "GetPaymentButton";
            this.GetPaymentButton.Size = new System.Drawing.Size(360, 137);
            this.GetPaymentButton.TabIndex = 6;
            this.GetPaymentButton.Text = "รับเงิน";
            this.GetPaymentButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.GetPaymentButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.GetPaymentButton.UseVisualStyleBackColor = false;
            this.GetPaymentButton.Click += new System.EventHandler(this.GetPaymentButton_Click);
            // 
            // SaveSaleInvoiceButton
            // 
            this.SaveSaleInvoiceButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.SaveSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SaveSaleInvoiceButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaveSaleInvoiceButton.ForeColor = System.Drawing.Color.DarkGray;
            this.SaveSaleInvoiceButton.Image = global::IndyPOS.Properties.Resources.Check_50;
            this.SaveSaleInvoiceButton.Location = new System.Drawing.Point(3, 828);
            this.SaveSaleInvoiceButton.Name = "SaveSaleInvoiceButton";
            this.SaveSaleInvoiceButton.Size = new System.Drawing.Size(360, 137);
            this.SaveSaleInvoiceButton.TabIndex = 7;
            this.SaveSaleInvoiceButton.Text = "บันทึกการขาย";
            this.SaveSaleInvoiceButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.SaveSaleInvoiceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.SaveSaleInvoiceButton.UseVisualStyleBackColor = false;
            this.SaveSaleInvoiceButton.Click += new System.EventHandler(this.SaveSaleInvoiceButton_Click);
            // 
            // SalePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "SalePanel";
            this.Size = new System.Drawing.Size(1800, 970);
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel8.ResumeLayout(false);
            this.panel9.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label InvoiceTotalCaptionLabel;
        private System.Windows.Forms.Label InvoiceTotalLabel;
        private System.Windows.Forms.Button GetPaymentButton;
        private System.Windows.Forms.Label PaymentsTotalLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.Button SaveSaleInvoiceButton;
        private System.Windows.Forms.DataGridView InvoiceDataView;
        private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.DataGridView PaymentDataView;
		private System.Windows.Forms.Panel panel9;
		private System.Windows.Forms.Panel panel8;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label PaymentsTotalCaptionLabel;
        private ModernUI.ModernButton ClearAllPaymentsButton;
        private ModernUI.ModernButton AddGeneralGoodsProductButton;
        private ModernUI.ModernButton AddHardwareProductButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentPriority;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentType;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn Note;
        private System.Windows.Forms.DataGridViewTextBoxColumn Priority;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn Total;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private ModernUI.ModernButton LookUpProductButton;
        private ModernUI.ModernTextBox ProductLookUpTextBox;
        private ModernUI.ModernButton CancelSaleInvoiceButton;
    }
}
