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
			this.panel3 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.TotalLabel = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.panel4 = new System.Windows.Forms.Panel();
			this.GetPaymentButton = new System.Windows.Forms.Button();
			this.TotalPaymentsLabel = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.panel5 = new System.Windows.Forms.Panel();
			this.label4 = new System.Windows.Forms.Label();
			this.ChangesLabel = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.panel6 = new System.Windows.Forms.Panel();
			this.SaveSaleInvoiceButton = new System.Windows.Forms.Button();
			this.panel7 = new System.Windows.Forms.Panel();
			this.CancelSaleInvoiceButton = new System.Windows.Forms.Button();
			this.InvoiceDataView = new System.Windows.Forms.DataGridView();
			this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel3.SuspendLayout();
			this.panel4.SuspendLayout();
			this.panel5.SuspendLayout();
			this.panel6.SuspendLayout();
			this.panel7.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).BeginInit();
			this.panel2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel3
			// 
			this.panel3.BackColor = System.Drawing.Color.Gray;
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.label1);
			this.panel3.Controls.Add(this.TotalLabel);
			this.panel3.Controls.Add(this.label2);
			this.panel3.Location = new System.Drawing.Point(9, 3);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(368, 192);
			this.panel3.TabIndex = 11;
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
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
			this.TotalLabel.Location = new System.Drawing.Point(3, 107);
			this.TotalLabel.Name = "TotalLabel";
			this.TotalLabel.Size = new System.Drawing.Size(300, 79);
			this.TotalLabel.TabIndex = 0;
			this.TotalLabel.Text = "0.00";
			this.TotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label2
			// 
			this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.ForeColor = System.Drawing.Color.PaleGreen;
			this.label2.Location = new System.Drawing.Point(304, 107);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(59, 79);
			this.label2.TabIndex = 8;
			this.label2.Text = "บาท";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel4
			// 
			this.panel4.BackColor = System.Drawing.Color.Gray;
			this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel4.Controls.Add(this.GetPaymentButton);
			this.panel4.Controls.Add(this.TotalPaymentsLabel);
			this.panel4.Controls.Add(this.label6);
			this.panel4.Location = new System.Drawing.Point(9, 201);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(368, 254);
			this.panel4.TabIndex = 12;
			// 
			// GetPaymentButton
			// 
			this.GetPaymentButton.BackColor = System.Drawing.Color.Silver;
			this.GetPaymentButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.GetPaymentButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.GetPaymentButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.GetPaymentButton.Image = global::IndyPOS.Properties.Resources.Money_50;
			this.GetPaymentButton.Location = new System.Drawing.Point(3, 3);
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
			this.TotalPaymentsLabel.Location = new System.Drawing.Point(3, 170);
			this.TotalPaymentsLabel.Name = "TotalPaymentsLabel";
			this.TotalPaymentsLabel.Size = new System.Drawing.Size(300, 79);
			this.TotalPaymentsLabel.TabIndex = 2;
			this.TotalPaymentsLabel.Text = "0.00";
			this.TotalPaymentsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label6
			// 
			this.label6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.label6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.label6.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.ForeColor = System.Drawing.Color.PaleGreen;
			this.label6.Location = new System.Drawing.Point(304, 170);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(59, 79);
			this.label6.TabIndex = 9;
			this.label6.Text = "บาท";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel5
			// 
			this.panel5.BackColor = System.Drawing.Color.Gray;
			this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel5.Controls.Add(this.label4);
			this.panel5.Controls.Add(this.ChangesLabel);
			this.panel5.Controls.Add(this.label7);
			this.panel5.Location = new System.Drawing.Point(9, 461);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(368, 192);
			this.panel5.TabIndex = 13;
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.Gainsboro;
			this.label4.Location = new System.Drawing.Point(3, 3);
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
			this.ChangesLabel.Location = new System.Drawing.Point(3, 107);
			this.ChangesLabel.Name = "ChangesLabel";
			this.ChangesLabel.Size = new System.Drawing.Size(300, 79);
			this.ChangesLabel.TabIndex = 4;
			this.ChangesLabel.Text = "0.00";
			this.ChangesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label7
			// 
			this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.label7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.label7.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label7.ForeColor = System.Drawing.Color.PaleGreen;
			this.label7.Location = new System.Drawing.Point(304, 107);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(59, 79);
			this.label7.TabIndex = 10;
			this.label7.Text = "บาท";
			this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel6
			// 
			this.panel6.BackColor = System.Drawing.Color.Gray;
			this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel6.Controls.Add(this.SaveSaleInvoiceButton);
			this.panel6.Location = new System.Drawing.Point(9, 669);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(154, 154);
			this.panel6.TabIndex = 14;
			// 
			// SaveSaleInvoiceButton
			// 
			this.SaveSaleInvoiceButton.BackColor = System.Drawing.Color.Gainsboro;
			this.SaveSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.SaveSaleInvoiceButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SaveSaleInvoiceButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.SaveSaleInvoiceButton.Image = global::IndyPOS.Properties.Resources.Check_50;
			this.SaveSaleInvoiceButton.Location = new System.Drawing.Point(3, 3);
			this.SaveSaleInvoiceButton.Name = "SaveSaleInvoiceButton";
			this.SaveSaleInvoiceButton.Size = new System.Drawing.Size(146, 146);
			this.SaveSaleInvoiceButton.TabIndex = 7;
			this.SaveSaleInvoiceButton.Text = "บันทึกการขาย";
			this.SaveSaleInvoiceButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.SaveSaleInvoiceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.SaveSaleInvoiceButton.UseVisualStyleBackColor = false;
			this.SaveSaleInvoiceButton.Click += new System.EventHandler(this.SaveSaleInvoiceButton_Click);
			// 
			// panel7
			// 
			this.panel7.BackColor = System.Drawing.Color.Gray;
			this.panel7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel7.Controls.Add(this.CancelSaleInvoiceButton);
			this.panel7.Location = new System.Drawing.Point(223, 669);
			this.panel7.Name = "panel7";
			this.panel7.Size = new System.Drawing.Size(154, 154);
			this.panel7.TabIndex = 15;
			// 
			// CancelSaleInvoiceButton
			// 
			this.CancelSaleInvoiceButton.BackColor = System.Drawing.Color.Gainsboro;
			this.CancelSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CancelSaleInvoiceButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CancelSaleInvoiceButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.CancelSaleInvoiceButton.Image = global::IndyPOS.Properties.Resources.Cross_50;
			this.CancelSaleInvoiceButton.Location = new System.Drawing.Point(3, 3);
			this.CancelSaleInvoiceButton.Name = "CancelSaleInvoiceButton";
			this.CancelSaleInvoiceButton.Size = new System.Drawing.Size(146, 146);
			this.CancelSaleInvoiceButton.TabIndex = 7;
			this.CancelSaleInvoiceButton.Text = "ยกเลิกการขาย";
			this.CancelSaleInvoiceButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.CancelSaleInvoiceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.CancelSaleInvoiceButton.UseVisualStyleBackColor = false;
			this.CancelSaleInvoiceButton.Click += new System.EventHandler(this.CancelSaleInvoiceButton_Click);
			// 
			// InvoiceDataView
			// 
			this.InvoiceDataView.AllowUserToResizeColumns = false;
			this.InvoiceDataView.AllowUserToResizeRows = false;
			this.InvoiceDataView.BackgroundColor = System.Drawing.Color.Silver;
			this.InvoiceDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle1.BackColor = System.Drawing.Color.Silver;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Gray;
			this.InvoiceDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.InvoiceDataView.ColumnHeadersHeight = 50;
			this.InvoiceDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.InvoiceDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProductCode,
            this.Description,
            this.Quantity,
            this.UnitPrice,
            this.Total});
			this.InvoiceDataView.EnableHeadersVisualStyles = false;
			this.InvoiceDataView.GridColor = System.Drawing.Color.DimGray;
			this.InvoiceDataView.Location = new System.Drawing.Point(14, 15);
			this.InvoiceDataView.MultiSelect = false;
			this.InvoiceDataView.Name = "InvoiceDataView";
			this.InvoiceDataView.ReadOnly = true;
			this.InvoiceDataView.RowHeadersVisible = false;
			this.InvoiceDataView.RowHeadersWidth = 60;
			this.InvoiceDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.Gainsboro;
			this.InvoiceDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.InvoiceDataView.RowTemplate.Height = 40;
			this.InvoiceDataView.Size = new System.Drawing.Size(855, 808);
			this.InvoiceDataView.TabIndex = 1;
			this.InvoiceDataView.DoubleClick += new System.EventHandler(this.InvoiceDataView_DoubleClick);
			// 
			// ProductCode
			// 
			this.ProductCode.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.ProductCode.HeaderText = "รหัสสินค้า";
			this.ProductCode.Name = "ProductCode";
			this.ProductCode.ReadOnly = true;
			this.ProductCode.Width = 150;
			// 
			// Description
			// 
			this.Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.Description.HeaderText = "คำอธิบาย";
			this.Description.Name = "Description";
			this.Description.ReadOnly = true;
			this.Description.Width = 300;
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
			this.panel2.BackColor = System.Drawing.Color.DarkGray;
			this.panel2.Controls.Add(this.InvoiceDataView);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(1035, 835);
			this.panel2.TabIndex = 2;
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.panel1.Controls.Add(this.panel7);
			this.panel1.Controls.Add(this.panel3);
			this.panel1.Controls.Add(this.panel6);
			this.panel1.Controls.Add(this.panel4);
			this.panel1.Controls.Add(this.panel5);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel1.Location = new System.Drawing.Point(1035, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(386, 835);
			this.panel1.TabIndex = 3;
			// 
			// SalePanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.DarkGray;
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Name = "SalePanel";
			this.Size = new System.Drawing.Size(1421, 835);
			this.panel3.ResumeLayout(false);
			this.panel4.ResumeLayout(false);
			this.panel5.ResumeLayout(false);
			this.panel6.ResumeLayout(false);
			this.panel7.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).EndInit();
			this.panel2.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label TotalLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button GetPaymentButton;
        private System.Windows.Forms.Label TotalPaymentsLabel;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Button SaveSaleInvoiceButton;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Button CancelSaleInvoiceButton;
        private System.Windows.Forms.DataGridView InvoiceDataView;
        private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
		private System.Windows.Forms.DataGridViewTextBoxColumn Description;
		private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
		private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
		private System.Windows.Forms.DataGridViewTextBoxColumn Total;
	}
}
