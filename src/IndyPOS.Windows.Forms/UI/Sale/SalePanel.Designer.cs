namespace IndyPOS.Windows.Forms.UI.Sale
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
            var dataGridViewCellStyle1 = new DataGridViewCellStyle();
            var dataGridViewCellStyle2 = new DataGridViewCellStyle();
            var dataGridViewCellStyle3 = new DataGridViewCellStyle();
            var dataGridViewCellStyle4 = new DataGridViewCellStyle();
            InvoiceTotalCaptionLabel = new Label();
            InvoiceTotalLabel = new Label();
            PaymentsTotalLabel = new Label();
            label4 = new Label();
            ChangesLabel = new Label();
            InvoiceDataView = new DataGridView();
            Priority = new DataGridViewTextBoxColumn();
            ProductCode = new DataGridViewTextBoxColumn();
            Description = new DataGridViewTextBoxColumn();
            Quantity = new DataGridViewTextBoxColumn();
            UnitPrice = new DataGridViewTextBoxColumn();
            Total = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            panel2 = new Panel();
            panel8 = new Panel();
            CancelSaleInvoiceButton = new ModernUI.ModernButton();
            label3 = new Label();
            panel9 = new Panel();
            CashDrawerButton = new ModernUI.ModernButton();
            ClearAllPaymentsButton = new ModernUI.ModernButton();
            panel3 = new Panel();
            LookUpProductButton = new ModernUI.ModernButton();
            ProductLookUpTextBox = new ModernUI.ModernTextBox();
            AddHardwareProductButton = new ModernUI.ModernButton();
            AddGeneralGoodsProductButton = new ModernUI.ModernButton();
            PaymentDataView = new DataGridView();
            PaymentPriority = new DataGridViewTextBoxColumn();
            PaymentType = new DataGridViewTextBoxColumn();
            PaymentAmount = new DataGridViewTextBoxColumn();
            Note = new DataGridViewTextBoxColumn();
            label5 = new Label();
            panel1 = new Panel();
            PaymentsTotalCaptionLabel = new Label();
            GetPaymentButton = new Button();
            SaveSaleInvoiceButton = new Button();
            ((System.ComponentModel.ISupportInitialize)InvoiceDataView).BeginInit();
            panel2.SuspendLayout();
            panel8.SuspendLayout();
            panel9.SuspendLayout();
            panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PaymentDataView).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // InvoiceTotalCaptionLabel
            // 
            InvoiceTotalCaptionLabel.BackColor = Color.FromArgb(40, 40, 40);
            InvoiceTotalCaptionLabel.Font = new Font("FC Subject [Non-commercial] Reg", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            InvoiceTotalCaptionLabel.ForeColor = Color.Gainsboro;
            InvoiceTotalCaptionLabel.Location = new Point(3, 3);
            InvoiceTotalCaptionLabel.Name = "InvoiceTotalCaptionLabel";
            InvoiceTotalCaptionLabel.Size = new Size(360, 112);
            InvoiceTotalCaptionLabel.TabIndex = 1;
            InvoiceTotalCaptionLabel.Text = "ยอดที่ต้องชำระ";
            InvoiceTotalCaptionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // InvoiceTotalLabel
            // 
            InvoiceTotalLabel.BackColor = Color.FromArgb(40, 40, 40);
            InvoiceTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            InvoiceTotalLabel.ForeColor = Color.Salmon;
            InvoiceTotalLabel.Location = new Point(3, 117);
            InvoiceTotalLabel.Name = "InvoiceTotalLabel";
            InvoiceTotalLabel.Size = new Size(360, 112);
            InvoiceTotalLabel.TabIndex = 0;
            InvoiceTotalLabel.Text = "0.00";
            InvoiceTotalLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // PaymentsTotalLabel
            // 
            PaymentsTotalLabel.BackColor = Color.FromArgb(40, 40, 40);
            PaymentsTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PaymentsTotalLabel.ForeColor = Color.PaleGreen;
            PaymentsTotalLabel.Location = new Point(3, 345);
            PaymentsTotalLabel.Name = "PaymentsTotalLabel";
            PaymentsTotalLabel.Size = new Size(360, 112);
            PaymentsTotalLabel.TabIndex = 2;
            PaymentsTotalLabel.Text = "0.00";
            PaymentsTotalLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.BackColor = Color.FromArgb(40, 40, 40);
            label4.Font = new Font("FC Subject [Non-commercial] Reg", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.Gainsboro;
            label4.Location = new Point(3, 459);
            label4.Name = "label4";
            label4.Size = new Size(360, 112);
            label4.TabIndex = 5;
            label4.Text = "เงินทอน";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ChangesLabel
            // 
            ChangesLabel.BackColor = Color.FromArgb(40, 40, 40);
            ChangesLabel.Font = new Font("FC Subject [Non-commercial] Reg", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ChangesLabel.ForeColor = Color.CornflowerBlue;
            ChangesLabel.Location = new Point(2, 573);
            ChangesLabel.Name = "ChangesLabel";
            ChangesLabel.Size = new Size(360, 112);
            ChangesLabel.TabIndex = 4;
            ChangesLabel.Text = "0.00";
            ChangesLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // InvoiceDataView
            // 
            InvoiceDataView.AllowUserToAddRows = false;
            InvoiceDataView.AllowUserToDeleteRows = false;
            InvoiceDataView.AllowUserToResizeColumns = false;
            InvoiceDataView.AllowUserToResizeRows = false;
            InvoiceDataView.BackgroundColor = Color.FromArgb(38, 38, 38);
            InvoiceDataView.BorderStyle = BorderStyle.None;
            InvoiceDataView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            InvoiceDataView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle1.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = Color.Gray;
            InvoiceDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            InvoiceDataView.ColumnHeadersHeight = 50;
            InvoiceDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            InvoiceDataView.Columns.AddRange(new DataGridViewColumn[] { Priority, ProductCode, Description, Quantity, UnitPrice, Total, dataGridViewTextBoxColumn1 });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle2.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            InvoiceDataView.DefaultCellStyle = dataGridViewCellStyle2;
            InvoiceDataView.Dock = DockStyle.Fill;
            InvoiceDataView.EnableHeadersVisualStyles = false;
            InvoiceDataView.GridColor = Color.DimGray;
            InvoiceDataView.Location = new Point(3, 43);
            InvoiceDataView.MultiSelect = false;
            InvoiceDataView.Name = "InvoiceDataView";
            InvoiceDataView.ReadOnly = true;
            InvoiceDataView.RowHeadersVisible = false;
            InvoiceDataView.RowHeadersWidth = 60;
            InvoiceDataView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            InvoiceDataView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            InvoiceDataView.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            InvoiceDataView.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            InvoiceDataView.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            InvoiceDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            InvoiceDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            InvoiceDataView.RowTemplate.Height = 35;
            InvoiceDataView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            InvoiceDataView.Size = new Size(1428, 626);
            InvoiceDataView.TabIndex = 1;
            InvoiceDataView.DoubleClick += InvoiceDataView_DoubleClick;
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
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "Note";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 200;
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(30, 30, 30);
            panel2.Controls.Add(panel8);
            panel2.Controls.Add(panel9);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(1434, 970);
            panel2.TabIndex = 2;
            // 
            // panel8
            // 
            panel8.BackColor = Color.FromArgb(30, 30, 30);
            panel8.Controls.Add(CancelSaleInvoiceButton);
            panel8.Controls.Add(InvoiceDataView);
            panel8.Controls.Add(label3);
            panel8.Dock = DockStyle.Fill;
            panel8.Location = new Point(0, 0);
            panel8.Name = "panel8";
            panel8.Padding = new Padding(3);
            panel8.Size = new Size(1434, 672);
            panel8.TabIndex = 3;
            // 
            // CancelSaleInvoiceButton
            // 
            CancelSaleInvoiceButton.BackColor = Color.FromArgb(38, 38, 38);
            CancelSaleInvoiceButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            CancelSaleInvoiceButton.BorderColor = Color.FromArgb(224, 79, 95);
            CancelSaleInvoiceButton.BorderRadius = 18;
            CancelSaleInvoiceButton.BorderSize = 1;
            CancelSaleInvoiceButton.FlatAppearance.BorderSize = 0;
            CancelSaleInvoiceButton.FlatStyle = FlatStyle.Flat;
            CancelSaleInvoiceButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            CancelSaleInvoiceButton.ForeColor = Color.White;
            CancelSaleInvoiceButton.Location = new Point(1056, 3);
            CancelSaleInvoiceButton.Name = "CancelSaleInvoiceButton";
            CancelSaleInvoiceButton.Size = new Size(199, 37);
            CancelSaleInvoiceButton.TabIndex = 49;
            CancelSaleInvoiceButton.Text = "ยกเลิกการขาย";
            CancelSaleInvoiceButton.TextColor = Color.White;
            CancelSaleInvoiceButton.UseVisualStyleBackColor = false;
            CancelSaleInvoiceButton.Click += CancelSaleInvoiceButton_Click;
            // 
            // label3
            // 
            label3.BackColor = Color.FromArgb(38, 38, 38);
            label3.Dock = DockStyle.Top;
            label3.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.Gainsboro;
            label3.Location = new Point(3, 3);
            label3.Margin = new Padding(0);
            label3.Name = "label3";
            label3.Size = new Size(1428, 40);
            label3.TabIndex = 48;
            label3.Text = "รายการสินค้า";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel9
            // 
            panel9.BackColor = Color.FromArgb(38, 38, 38);
            panel9.Controls.Add(ClearAllPaymentsButton);
            panel9.Controls.Add(panel3);
            panel9.Controls.Add(PaymentDataView);
            panel9.Controls.Add(label5);
            panel9.Dock = DockStyle.Bottom;
            panel9.Location = new Point(0, 672);
            panel9.Name = "panel9";
            panel9.Padding = new Padding(3);
            panel9.Size = new Size(1434, 298);
            panel9.TabIndex = 4;
            // 
            // CashDrawerButton
            // 
            CashDrawerButton.BackColor = Color.FromArgb(38, 38, 38);
            CashDrawerButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            CashDrawerButton.BorderColor = Color.Turquoise;
            CashDrawerButton.BorderRadius = 5;
            CashDrawerButton.BorderSize = 1;
            CashDrawerButton.FlatAppearance.BorderSize = 0;
            CashDrawerButton.FlatStyle = FlatStyle.Flat;
            CashDrawerButton.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            CashDrawerButton.ForeColor = Color.White;
            CashDrawerButton.Location = new Point(262, 78);
            CashDrawerButton.Name = "CashDrawerButton";
            CashDrawerButton.Size = new Size(122, 166);
            CashDrawerButton.TabIndex = 51;
            CashDrawerButton.Text = "เปิดลิ้นชัก";
            CashDrawerButton.TextColor = Color.White;
            CashDrawerButton.UseVisualStyleBackColor = false;
            CashDrawerButton.Click += CashDrawerButton_Click;
            // 
            // ClearAllPaymentsButton
            // 
            ClearAllPaymentsButton.BackColor = Color.FromArgb(38, 38, 38);
            ClearAllPaymentsButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ClearAllPaymentsButton.BorderColor = Color.FromArgb(224, 79, 95);
            ClearAllPaymentsButton.BorderRadius = 18;
            ClearAllPaymentsButton.BorderSize = 1;
            ClearAllPaymentsButton.FlatAppearance.BorderSize = 0;
            ClearAllPaymentsButton.FlatStyle = FlatStyle.Flat;
            ClearAllPaymentsButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ClearAllPaymentsButton.ForeColor = Color.White;
            ClearAllPaymentsButton.Location = new Point(649, 3);
            ClearAllPaymentsButton.Name = "ClearAllPaymentsButton";
            ClearAllPaymentsButton.Size = new Size(199, 37);
            ClearAllPaymentsButton.TabIndex = 10;
            ClearAllPaymentsButton.Text = "ยกเลิกการชำระเงิน";
            ClearAllPaymentsButton.TextColor = Color.White;
            ClearAllPaymentsButton.UseVisualStyleBackColor = false;
            ClearAllPaymentsButton.Click += ClearAllPaymentsButton_Click;
            // 
            // panel3
            // 
            panel3.BackColor = Color.FromArgb(38, 38, 38);
            panel3.Controls.Add(CashDrawerButton);
            panel3.Controls.Add(LookUpProductButton);
            panel3.Controls.Add(ProductLookUpTextBox);
            panel3.Controls.Add(AddHardwareProductButton);
            panel3.Controls.Add(AddGeneralGoodsProductButton);
            panel3.Dock = DockStyle.Right;
            panel3.Location = new Point(1037, 43);
            panel3.Name = "panel3";
            panel3.Size = new Size(394, 252);
            panel3.TabIndex = 50;
            // 
            // LookUpProductButton
            // 
            LookUpProductButton.BackColor = Color.FromArgb(38, 38, 38);
            LookUpProductButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            LookUpProductButton.BorderColor = Color.Turquoise;
            LookUpProductButton.BorderRadius = 5;
            LookUpProductButton.BorderSize = 1;
            LookUpProductButton.FlatAppearance.BorderSize = 0;
            LookUpProductButton.FlatStyle = FlatStyle.Flat;
            LookUpProductButton.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LookUpProductButton.ForeColor = Color.White;
            LookUpProductButton.Image = Properties.Resources.Search_50;
            LookUpProductButton.Location = new Point(262, 10);
            LookUpProductButton.Name = "LookUpProductButton";
            LookUpProductButton.Size = new Size(122, 62);
            LookUpProductButton.TabIndex = 51;
            LookUpProductButton.TextAlign = ContentAlignment.MiddleRight;
            LookUpProductButton.TextColor = Color.White;
            LookUpProductButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            LookUpProductButton.UseVisualStyleBackColor = false;
            LookUpProductButton.Click += LookUpProductButton_Click;
            // 
            // ProductLookUpTextBox
            // 
            ProductLookUpTextBox.BackColor = Color.FromArgb(38, 38, 38);
            ProductLookUpTextBox.BorderColor = Color.DimGray;
            ProductLookUpTextBox.BorderSize = 1;
            ProductLookUpTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 20.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ProductLookUpTextBox.ForeColor = Color.Gainsboro;
            ProductLookUpTextBox.Location = new Point(9, 16);
            ProductLookUpTextBox.Multiline = false;
            ProductLookUpTextBox.Name = "ProductLookUpTextBox";
            ProductLookUpTextBox.Padding = new Padding(7);
            ProductLookUpTextBox.PasswordChar = false;
            ProductLookUpTextBox.PlaceholderColor = Color.DarkGray;
            ProductLookUpTextBox.PlaceholderText = "";
            ProductLookUpTextBox.ReadOnly = false;
            ProductLookUpTextBox.Size = new Size(247, 56);
            ProductLookUpTextBox.TabIndex = 49;
            ProductLookUpTextBox.TextAlign = HorizontalAlignment.Center;
            ProductLookUpTextBox.Texts = "";
            ProductLookUpTextBox.UnderlinedStyle = true;
            // 
            // AddHardwareProductButton
            // 
            AddHardwareProductButton.BackColor = Color.FromArgb(38, 38, 38);
            AddHardwareProductButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddHardwareProductButton.BorderColor = Color.Turquoise;
            AddHardwareProductButton.BorderRadius = 5;
            AddHardwareProductButton.BorderSize = 1;
            AddHardwareProductButton.FlatAppearance.BorderSize = 0;
            AddHardwareProductButton.FlatStyle = FlatStyle.Flat;
            AddHardwareProductButton.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F);
            AddHardwareProductButton.ForeColor = Color.White;
            AddHardwareProductButton.Image = Properties.Resources.Plus_50;
            AddHardwareProductButton.Location = new Point(9, 164);
            AddHardwareProductButton.Name = "AddHardwareProductButton";
            AddHardwareProductButton.Size = new Size(247, 80);
            AddHardwareProductButton.TabIndex = 10;
            AddHardwareProductButton.Text = " เพิ่มสินค้า ฮาร์ดแวร์";
            AddHardwareProductButton.TextAlign = ContentAlignment.MiddleRight;
            AddHardwareProductButton.TextColor = Color.White;
            AddHardwareProductButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            AddHardwareProductButton.UseVisualStyleBackColor = false;
            AddHardwareProductButton.Click += AddHardwareProductButton_Click;
            // 
            // AddGeneralGoodsProductButton
            // 
            AddGeneralGoodsProductButton.BackColor = Color.FromArgb(38, 38, 38);
            AddGeneralGoodsProductButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddGeneralGoodsProductButton.BorderColor = Color.Turquoise;
            AddGeneralGoodsProductButton.BorderRadius = 5;
            AddGeneralGoodsProductButton.BorderSize = 1;
            AddGeneralGoodsProductButton.FlatAppearance.BorderSize = 0;
            AddGeneralGoodsProductButton.FlatStyle = FlatStyle.Flat;
            AddGeneralGoodsProductButton.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F);
            AddGeneralGoodsProductButton.ForeColor = Color.White;
            AddGeneralGoodsProductButton.Image = Properties.Resources.Plus_50;
            AddGeneralGoodsProductButton.Location = new Point(9, 78);
            AddGeneralGoodsProductButton.Name = "AddGeneralGoodsProductButton";
            AddGeneralGoodsProductButton.Size = new Size(247, 80);
            AddGeneralGoodsProductButton.TabIndex = 9;
            AddGeneralGoodsProductButton.Text = " เพิ่มสินค้า เบ็ดเตล็ด";
            AddGeneralGoodsProductButton.TextAlign = ContentAlignment.MiddleRight;
            AddGeneralGoodsProductButton.TextColor = Color.White;
            AddGeneralGoodsProductButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            AddGeneralGoodsProductButton.UseVisualStyleBackColor = false;
            AddGeneralGoodsProductButton.Click += AddGeneralGoodsProductButton_Click;
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
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle3.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle3.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = Color.Gray;
            PaymentDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            PaymentDataView.ColumnHeadersHeight = 50;
            PaymentDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            PaymentDataView.Columns.AddRange(new DataGridViewColumn[] { PaymentPriority, PaymentType, PaymentAmount, Note });
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle4.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle4.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            PaymentDataView.DefaultCellStyle = dataGridViewCellStyle4;
            PaymentDataView.Dock = DockStyle.Left;
            PaymentDataView.EnableHeadersVisualStyles = false;
            PaymentDataView.GridColor = Color.DimGray;
            PaymentDataView.Location = new Point(3, 43);
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
            PaymentDataView.Size = new Size(855, 252);
            PaymentDataView.TabIndex = 2;
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
            // label5
            // 
            label5.BackColor = Color.FromArgb(38, 38, 38);
            label5.Dock = DockStyle.Top;
            label5.Font = new Font("FC Subject [Non-commercial] Reg", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.Gainsboro;
            label5.Location = new Point(3, 3);
            label5.Margin = new Padding(0);
            label5.Name = "label5";
            label5.Size = new Size(1428, 40);
            label5.TabIndex = 49;
            label5.Text = "รายการเงิน";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(30, 30, 30);
            panel1.Controls.Add(PaymentsTotalCaptionLabel);
            panel1.Controls.Add(GetPaymentButton);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(ChangesLabel);
            panel1.Controls.Add(PaymentsTotalLabel);
            panel1.Controls.Add(InvoiceTotalCaptionLabel);
            panel1.Controls.Add(InvoiceTotalLabel);
            panel1.Controls.Add(SaveSaleInvoiceButton);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(1434, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(366, 970);
            panel1.TabIndex = 3;
            // 
            // PaymentsTotalCaptionLabel
            // 
            PaymentsTotalCaptionLabel.BackColor = Color.FromArgb(40, 40, 40);
            PaymentsTotalCaptionLabel.Font = new Font("FC Subject [Non-commercial] Reg", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PaymentsTotalCaptionLabel.ForeColor = Color.Gainsboro;
            PaymentsTotalCaptionLabel.Location = new Point(3, 231);
            PaymentsTotalCaptionLabel.Name = "PaymentsTotalCaptionLabel";
            PaymentsTotalCaptionLabel.Size = new Size(360, 112);
            PaymentsTotalCaptionLabel.TabIndex = 8;
            PaymentsTotalCaptionLabel.Text = "ยอดที่รับมา";
            PaymentsTotalCaptionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // GetPaymentButton
            // 
            GetPaymentButton.BackColor = Color.FromArgb(38, 38, 38);
            GetPaymentButton.FlatStyle = FlatStyle.Flat;
            GetPaymentButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            GetPaymentButton.ForeColor = Color.DarkGray;
            GetPaymentButton.Image = Properties.Resources.Money_50;
            GetPaymentButton.Location = new Point(3, 688);
            GetPaymentButton.Name = "GetPaymentButton";
            GetPaymentButton.Size = new Size(360, 137);
            GetPaymentButton.TabIndex = 6;
            GetPaymentButton.Text = "รับเงิน";
            GetPaymentButton.TextAlign = ContentAlignment.BottomCenter;
            GetPaymentButton.TextImageRelation = TextImageRelation.ImageAboveText;
            GetPaymentButton.UseVisualStyleBackColor = false;
            GetPaymentButton.Click += GetPaymentButton_Click;
            // 
            // SaveSaleInvoiceButton
            // 
            SaveSaleInvoiceButton.BackColor = Color.FromArgb(38, 38, 38);
            SaveSaleInvoiceButton.FlatStyle = FlatStyle.Flat;
            SaveSaleInvoiceButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            SaveSaleInvoiceButton.ForeColor = Color.DarkGray;
            SaveSaleInvoiceButton.Image = Properties.Resources.Check_50;
            SaveSaleInvoiceButton.Location = new Point(3, 828);
            SaveSaleInvoiceButton.Name = "SaveSaleInvoiceButton";
            SaveSaleInvoiceButton.Size = new Size(360, 137);
            SaveSaleInvoiceButton.TabIndex = 7;
            SaveSaleInvoiceButton.Text = "บันทึกการขาย";
            SaveSaleInvoiceButton.TextAlign = ContentAlignment.BottomCenter;
            SaveSaleInvoiceButton.TextImageRelation = TextImageRelation.ImageAboveText;
            SaveSaleInvoiceButton.UseVisualStyleBackColor = false;
            SaveSaleInvoiceButton.Click += SaveSaleInvoiceButton_Click;
            // 
            // SalePanel
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "SalePanel";
            Size = new Size(1800, 970);
            ((System.ComponentModel.ISupportInitialize)InvoiceDataView).EndInit();
            panel2.ResumeLayout(false);
            panel8.ResumeLayout(false);
            panel9.ResumeLayout(false);
            panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PaymentDataView).EndInit();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
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
        private ModernUI.ModernButton CashDrawerButton;
    }
}
