﻿namespace IndyPOS
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.TotalLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.GetPaymentButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.SaveSaleInvoiceButton = new System.Windows.Forms.Button();
            this.panel7 = new System.Windows.Forms.Panel();
            this.CancelSaleInvoiceButton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.InvoiceDataView = new System.Windows.Forms.DataGridView();
            this.TestGetAllProductsButton = new System.Windows.Forms.Button();
            this.panel8 = new System.Windows.Forms.Panel();
            this.RemoveProductButton = new System.Windows.Forms.Button();
            this.panel9 = new System.Windows.Forms.Panel();
            this.AddProductButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.Line = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel6.SuspendLayout();
            this.panel7.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).BeginInit();
            this.panel8.SuspendLayout();
            this.panel9.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.Silver;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.label1);
            this.panel3.Controls.Add(this.TotalLabel);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Location = new System.Drawing.Point(6, 13);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(224, 121);
            this.panel3.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.LightYellow;
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label1.Font = new System.Drawing.Font("Leelawadee", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(216, 60);
            this.label1.TabIndex = 1;
            this.label1.Text = "รวมราคาสินค้า";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TotalLabel
            // 
            this.TotalLabel.BackColor = System.Drawing.Color.White;
            this.TotalLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TotalLabel.Font = new System.Drawing.Font("Leelawadee", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TotalLabel.ForeColor = System.Drawing.Color.MediumBlue;
            this.TotalLabel.Location = new System.Drawing.Point(3, 66);
            this.TotalLabel.Name = "TotalLabel";
            this.TotalLabel.Size = new System.Drawing.Size(159, 50);
            this.TotalLabel.TabIndex = 0;
            this.TotalLabel.Text = "0.00";
            this.TotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.Font = new System.Drawing.Font("Leelawadee", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.MediumBlue;
            this.label2.Location = new System.Drawing.Point(160, 66);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 50);
            this.label2.TabIndex = 8;
            this.label2.Text = "บาท";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.Silver;
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.GetPaymentButton);
            this.panel4.Controls.Add(this.label3);
            this.panel4.Controls.Add(this.label6);
            this.panel4.Location = new System.Drawing.Point(6, 140);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(224, 192);
            this.panel4.TabIndex = 12;
            // 
            // GetPaymentButton
            // 
            this.GetPaymentButton.BackColor = System.Drawing.Color.Wheat;
            this.GetPaymentButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.GetPaymentButton.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GetPaymentButton.Image = global::IndyPOS.Properties.Resources.Money_50;
            this.GetPaymentButton.Location = new System.Drawing.Point(3, 3);
            this.GetPaymentButton.Name = "GetPaymentButton";
            this.GetPaymentButton.Size = new System.Drawing.Size(216, 130);
            this.GetPaymentButton.TabIndex = 6;
            this.GetPaymentButton.Text = "รับเงิน";
            this.GetPaymentButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.GetPaymentButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.GetPaymentButton.UseVisualStyleBackColor = false;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.White;
            this.label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label3.Font = new System.Drawing.Font("Leelawadee", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.MediumBlue;
            this.label3.Location = new System.Drawing.Point(3, 136);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(159, 50);
            this.label3.TabIndex = 2;
            this.label3.Text = "0.00";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.White;
            this.label6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label6.Font = new System.Drawing.Font("Leelawadee", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.MediumBlue;
            this.label6.Location = new System.Drawing.Point(160, 136);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 50);
            this.label6.TabIndex = 9;
            this.label6.Text = "บาท";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.Silver;
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.label4);
            this.panel5.Controls.Add(this.label5);
            this.panel5.Controls.Add(this.label7);
            this.panel5.Location = new System.Drawing.Point(6, 338);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(224, 121);
            this.panel5.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.LightYellow;
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label4.Font = new System.Drawing.Font("Leelawadee", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(3, 3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(216, 60);
            this.label4.TabIndex = 5;
            this.label4.Text = "เงินทอน";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.White;
            this.label5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label5.Font = new System.Drawing.Font("Leelawadee", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.MediumBlue;
            this.label5.Location = new System.Drawing.Point(3, 65);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(159, 50);
            this.label5.TabIndex = 4;
            this.label5.Text = "0.00";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.White;
            this.label7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label7.Font = new System.Drawing.Font("Leelawadee", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.MediumBlue;
            this.label7.Location = new System.Drawing.Point(160, 65);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(59, 50);
            this.label7.TabIndex = 10;
            this.label7.Text = "บาท";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.Silver;
            this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel6.Controls.Add(this.SaveSaleInvoiceButton);
            this.panel6.Location = new System.Drawing.Point(6, 465);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(224, 139);
            this.panel6.TabIndex = 14;
            // 
            // SaveSaleInvoiceButton
            // 
            this.SaveSaleInvoiceButton.BackColor = System.Drawing.Color.Wheat;
            this.SaveSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SaveSaleInvoiceButton.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaveSaleInvoiceButton.Image = global::IndyPOS.Properties.Resources.Save_50;
            this.SaveSaleInvoiceButton.Location = new System.Drawing.Point(3, 3);
            this.SaveSaleInvoiceButton.Name = "SaveSaleInvoiceButton";
            this.SaveSaleInvoiceButton.Size = new System.Drawing.Size(216, 130);
            this.SaveSaleInvoiceButton.TabIndex = 7;
            this.SaveSaleInvoiceButton.Text = "บันทึกการขาย";
            this.SaveSaleInvoiceButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SaveSaleInvoiceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.SaveSaleInvoiceButton.UseVisualStyleBackColor = false;
            // 
            // panel7
            // 
            this.panel7.BackColor = System.Drawing.Color.Silver;
            this.panel7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel7.Controls.Add(this.CancelSaleInvoiceButton);
            this.panel7.Location = new System.Drawing.Point(6, 610);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(224, 84);
            this.panel7.TabIndex = 15;
            // 
            // CancelSaleInvoiceButton
            // 
            this.CancelSaleInvoiceButton.BackColor = System.Drawing.Color.Wheat;
            this.CancelSaleInvoiceButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CancelSaleInvoiceButton.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelSaleInvoiceButton.Image = global::IndyPOS.Properties.Resources.RemoveCircle_50;
            this.CancelSaleInvoiceButton.Location = new System.Drawing.Point(3, 3);
            this.CancelSaleInvoiceButton.Name = "CancelSaleInvoiceButton";
            this.CancelSaleInvoiceButton.Size = new System.Drawing.Size(216, 75);
            this.CancelSaleInvoiceButton.TabIndex = 7;
            this.CancelSaleInvoiceButton.Text = "ยกเลิกการขาย";
            this.CancelSaleInvoiceButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CancelSaleInvoiceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.CancelSaleInvoiceButton.UseVisualStyleBackColor = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel7);
            this.panel1.Controls.Add(this.panel6);
            this.panel1.Controls.Add(this.panel5);
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(1553, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(247, 700);
            this.panel1.TabIndex = 1;
            // 
            // InvoiceDataView
            // 
            this.InvoiceDataView.AllowUserToResizeColumns = false;
            this.InvoiceDataView.AllowUserToResizeRows = false;
            this.InvoiceDataView.BackgroundColor = System.Drawing.Color.WhiteSmoke;
            this.InvoiceDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.InvoiceDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.InvoiceDataView.ColumnHeadersHeight = 50;
            this.InvoiceDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.InvoiceDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Line,
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
            this.InvoiceDataView.RowHeadersVisible = false;
            this.InvoiceDataView.RowHeadersWidth = 60;
            this.InvoiceDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle6.Padding = new System.Windows.Forms.Padding(0, 0, 20, 0);
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.InvoiceDataView.RowsDefaultCellStyle = dataGridViewCellStyle6;
            this.InvoiceDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.InvoiceDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            this.InvoiceDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InvoiceDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.WindowText;
            this.InvoiceDataView.RowTemplate.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(0, 0, 20, 0);
            this.InvoiceDataView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.InvoiceDataView.RowTemplate.Height = 35;
            this.InvoiceDataView.Size = new System.Drawing.Size(1223, 674);
            this.InvoiceDataView.TabIndex = 1;
            // 
            // TestGetAllProductsButton
            // 
            this.TestGetAllProductsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TestGetAllProductsButton.Location = new System.Drawing.Point(1243, 305);
            this.TestGetAllProductsButton.Name = "TestGetAllProductsButton";
            this.TestGetAllProductsButton.Size = new System.Drawing.Size(139, 65);
            this.TestGetAllProductsButton.TabIndex = 2;
            this.TestGetAllProductsButton.Text = "ทดสอบ";
            this.TestGetAllProductsButton.UseVisualStyleBackColor = true;
            this.TestGetAllProductsButton.Click += new System.EventHandler(this.TestGetAllProductsButton_Click);
            // 
            // panel8
            // 
            this.panel8.BackColor = System.Drawing.Color.Silver;
            this.panel8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel8.Controls.Add(this.RemoveProductButton);
            this.panel8.Location = new System.Drawing.Point(1243, 160);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(224, 139);
            this.panel8.TabIndex = 15;
            // 
            // RemoveProductButton
            // 
            this.RemoveProductButton.BackColor = System.Drawing.Color.Wheat;
            this.RemoveProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RemoveProductButton.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveProductButton.Image = global::IndyPOS.Properties.Resources.RemoveRectangle_50;
            this.RemoveProductButton.Location = new System.Drawing.Point(3, 3);
            this.RemoveProductButton.Name = "RemoveProductButton";
            this.RemoveProductButton.Size = new System.Drawing.Size(216, 130);
            this.RemoveProductButton.TabIndex = 7;
            this.RemoveProductButton.Text = "ลบรายการสินค้า";
            this.RemoveProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.RemoveProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.RemoveProductButton.UseVisualStyleBackColor = false;
            // 
            // panel9
            // 
            this.panel9.BackColor = System.Drawing.Color.Silver;
            this.panel9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel9.Controls.Add(this.AddProductButton);
            this.panel9.Location = new System.Drawing.Point(1243, 15);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(224, 139);
            this.panel9.TabIndex = 16;
            // 
            // AddProductButton
            // 
            this.AddProductButton.BackColor = System.Drawing.Color.Wheat;
            this.AddProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddProductButton.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddProductButton.Image = global::IndyPOS.Properties.Resources.PlusRectangle_50;
            this.AddProductButton.Location = new System.Drawing.Point(3, 3);
            this.AddProductButton.Name = "AddProductButton";
            this.AddProductButton.Size = new System.Drawing.Size(216, 130);
            this.AddProductButton.TabIndex = 7;
            this.AddProductButton.Text = "เพิ่มรายการสินค้า";
            this.AddProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.AddProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.AddProductButton.UseVisualStyleBackColor = false;
            this.AddProductButton.Click += new System.EventHandler(this.AddProductButton_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel9);
            this.panel2.Controls.Add(this.panel8);
            this.panel2.Controls.Add(this.TestGetAllProductsButton);
            this.panel2.Controls.Add(this.InvoiceDataView);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1553, 700);
            this.panel2.TabIndex = 2;
            // 
            // Line
            // 
            this.Line.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.Line.DefaultCellStyle = dataGridViewCellStyle2;
            this.Line.HeaderText = "รายการ";
            this.Line.Name = "Line";
            // 
            // ProductCode
            // 
            this.ProductCode.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.ProductCode.HeaderText = "รหัสสินค้า";
            this.ProductCode.Name = "ProductCode";
            this.ProductCode.Width = 200;
            // 
            // Description
            // 
            this.Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.Description.DefaultCellStyle = dataGridViewCellStyle3;
            this.Description.HeaderText = "คำอธิบาย";
            this.Description.Name = "Description";
            this.Description.Width = 500;
            // 
            // Quantity
            // 
            this.Quantity.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Quantity.HeaderText = "จำนวน";
            this.Quantity.Name = "Quantity";
            // 
            // UnitPrice
            // 
            this.UnitPrice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.UnitPrice.DefaultCellStyle = dataGridViewCellStyle4;
            this.UnitPrice.HeaderText = "ราคาต่อหน่วย";
            this.UnitPrice.Name = "UnitPrice";
            this.UnitPrice.Width = 170;
            // 
            // Total
            // 
            this.Total.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.Total.DefaultCellStyle = dataGridViewCellStyle5;
            this.Total.HeaderText = "ราคารวม";
            this.Total.Name = "Total";
            this.Total.Width = 150;
            // 
            // SalePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "SalePanel";
            this.Size = new System.Drawing.Size(1800, 700);
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.panel7.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceDataView)).EndInit();
            this.panel8.ResumeLayout(false);
            this.panel9.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label TotalLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button GetPaymentButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Button SaveSaleInvoiceButton;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Button CancelSaleInvoiceButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView InvoiceDataView;
        private System.Windows.Forms.Button TestGetAllProductsButton;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.Button RemoveProductButton;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Button AddProductButton;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Line;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn Total;
    }
}
