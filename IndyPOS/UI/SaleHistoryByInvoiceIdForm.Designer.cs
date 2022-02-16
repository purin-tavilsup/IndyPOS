namespace IndyPOS.UI
{
    partial class SaleHistoryByInvoiceIdForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            this.InvoiceIdLabel = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.CloseButton = new ModernUI.ModernButton();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.InvoiceProductsDataView = new System.Windows.Forms.DataGridView();
            this.Priority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label2 = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.PaymentDataView = new System.Windows.Forms.DataGridView();
            this.PaymentPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaymentType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PaymentAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Note = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.panel10 = new System.Windows.Forms.Panel();
            this.GeneralProductsTotalLabel = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.HardwareProductsTotalLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceProductsDataView)).BeginInit();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).BeginInit();
            this.panel1.SuspendLayout();
            this.panel10.SuspendLayout();
            this.panel6.SuspendLayout();
            this.SuspendLayout();
            // 
            // InvoiceIdLabel
            // 
            this.InvoiceIdLabel.BackColor = System.Drawing.Color.DarkSlateGray;
            this.InvoiceIdLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.InvoiceIdLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InvoiceIdLabel.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.InvoiceIdLabel.Location = new System.Drawing.Point(0, 0);
            this.InvoiceIdLabel.Name = "InvoiceIdLabel";
            this.InvoiceIdLabel.Size = new System.Drawing.Size(1700, 39);
            this.InvoiceIdLabel.TabIndex = 38;
            this.InvoiceIdLabel.Text = "Invoice ID :";
            this.InvoiceIdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.panel2.Controls.Add(this.CloseButton);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 907);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1700, 93);
            this.panel2.TabIndex = 47;
            // 
            // CloseButton
            // 
            this.CloseButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CloseButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CloseButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(190)))), ((int)(((byte)(166)))));
            this.CloseButton.BorderRadius = 19;
            this.CloseButton.BorderSize = 1;
            this.CloseButton.FlatAppearance.BorderSize = 0;
            this.CloseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CloseButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CloseButton.ForeColor = System.Drawing.Color.White;
            this.CloseButton.Location = new System.Drawing.Point(771, 18);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(158, 53);
            this.CloseButton.TabIndex = 12;
            this.CloseButton.Text = "ปิด";
            this.CloseButton.TextColor = System.Drawing.Color.White;
            this.CloseButton.UseVisualStyleBackColor = false;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.panel4);
            this.panel3.Controls.Add(this.panel5);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 39);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1700, 868);
            this.panel3.TabIndex = 112;
            // 
            // panel4
            // 
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.InvoiceProductsDataView);
            this.panel4.Controls.Add(this.label2);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1700, 658);
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
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle11.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.Color.Gray;
            this.InvoiceProductsDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle11;
            this.InvoiceProductsDataView.ColumnHeadersHeight = 50;
            this.InvoiceProductsDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.InvoiceProductsDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Priority,
            this.ProductCode,
            this.Description,
            this.Quantity,
            this.UnitPrice,
            this.Total});
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle12.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle12.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.InvoiceProductsDataView.DefaultCellStyle = dataGridViewCellStyle12;
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
            this.InvoiceProductsDataView.Size = new System.Drawing.Size(1698, 616);
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
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(1698, 40);
            this.label2.TabIndex = 50;
            this.label2.Text = "รายการสินค้า";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel5
            // 
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.PaymentDataView);
            this.panel5.Controls.Add(this.panel1);
            this.panel5.Controls.Add(this.label3);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel5.Location = new System.Drawing.Point(0, 658);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(1700, 210);
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
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle9.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle9.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.Color.Gray;
            this.PaymentDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle9;
            this.PaymentDataView.ColumnHeadersHeight = 50;
            this.PaymentDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.PaymentDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PaymentPriority,
            this.PaymentType,
            this.PaymentAmount,
            this.Note});
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle10.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PaymentDataView.DefaultCellStyle = dataGridViewCellStyle10;
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
            this.PaymentDataView.Size = new System.Drawing.Size(855, 168);
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
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.panel6);
            this.panel1.Controls.Add(this.panel10);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(855, 40);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(843, 168);
            this.panel1.TabIndex = 52;
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
            this.label3.Size = new System.Drawing.Size(1698, 40);
            this.label3.TabIndex = 50;
            this.label3.Text = "รายการเงิน";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel10
            // 
            this.panel10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(34)))), ((int)(((byte)(34)))));
            this.panel10.Controls.Add(this.GeneralProductsTotalLabel);
            this.panel10.Controls.Add(this.label18);
            this.panel10.Location = new System.Drawing.Point(16, 5);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(366, 75);
            this.panel10.TabIndex = 96;
            // 
            // GeneralProductsTotalLabel
            // 
            this.GeneralProductsTotalLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.GeneralProductsTotalLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GeneralProductsTotalLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GeneralProductsTotalLabel.ForeColor = System.Drawing.Color.MediumTurquoise;
            this.GeneralProductsTotalLabel.Location = new System.Drawing.Point(252, 0);
            this.GeneralProductsTotalLabel.Name = "GeneralProductsTotalLabel";
            this.GeneralProductsTotalLabel.Size = new System.Drawing.Size(114, 75);
            this.GeneralProductsTotalLabel.TabIndex = 88;
            this.GeneralProductsTotalLabel.Text = "0.00";
            this.GeneralProductsTotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label18
            // 
            this.label18.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label18.Dock = System.Windows.Forms.DockStyle.Left;
            this.label18.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.Color.Gainsboro;
            this.label18.Location = new System.Drawing.Point(0, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(252, 75);
            this.label18.TabIndex = 89;
            this.label18.Text = "ยอดลงบัญชี สินค้าเบ็ดเตล็ด =";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(34)))), ((int)(((byte)(34)))));
            this.panel6.Controls.Add(this.HardwareProductsTotalLabel);
            this.panel6.Controls.Add(this.label4);
            this.panel6.Location = new System.Drawing.Point(16, 86);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(366, 75);
            this.panel6.TabIndex = 97;
            // 
            // HardwareProductsTotalLabel
            // 
            this.HardwareProductsTotalLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.HardwareProductsTotalLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HardwareProductsTotalLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HardwareProductsTotalLabel.ForeColor = System.Drawing.Color.CornflowerBlue;
            this.HardwareProductsTotalLabel.Location = new System.Drawing.Point(252, 0);
            this.HardwareProductsTotalLabel.Name = "HardwareProductsTotalLabel";
            this.HardwareProductsTotalLabel.Size = new System.Drawing.Size(114, 75);
            this.HardwareProductsTotalLabel.TabIndex = 88;
            this.HardwareProductsTotalLabel.Text = "0.00";
            this.HardwareProductsTotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label4.Dock = System.Windows.Forms.DockStyle.Left;
            this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Gainsboro;
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(252, 75);
            this.label4.TabIndex = 89;
            this.label4.Text = "ยอดลงบัญชี สินค้าฮาร์ดแวร์ =";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // SaleHistoryByInvoiceIdForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.ClientSize = new System.Drawing.Size(1700, 1000);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.InvoiceIdLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SaleHistoryByInvoiceIdForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.InvoiceProductsDataView)).EndInit();
            this.panel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PaymentDataView)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel10.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label InvoiceIdLabel;
        private System.Windows.Forms.Panel panel2;
        private ModernUI.ModernButton CloseButton;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.DataGridView InvoiceProductsDataView;
        private System.Windows.Forms.DataGridViewTextBoxColumn Priority;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn Total;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.DataGridView PaymentDataView;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentPriority;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentType;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn Note;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.Label GeneralProductsTotalLabel;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label HardwareProductsTotalLabel;
        private System.Windows.Forms.Label label4;
    }
}