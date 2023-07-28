namespace IndyPOS.Windows.Forms.UI.Inventory
{
    partial class InventoryPanel
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
            panel1 = new Panel();
            ProductDataView = new DataGridView();
            ProductCode = new DataGridViewTextBoxColumn();
            Description = new DataGridViewTextBoxColumn();
            QuantityInStock = new DataGridViewTextBoxColumn();
            UnitPrice = new DataGridViewTextBoxColumn();
            GroupPrice = new DataGridViewTextBoxColumn();
            GroupPriceQuantity = new DataGridViewTextBoxColumn();
            Category = new DataGridViewTextBoxColumn();
            Manufacturer = new DataGridViewTextBoxColumn();
            Brand = new DataGridViewTextBoxColumn();
            DateCreated = new DataGridViewTextBoxColumn();
            DateUpdated = new DataGridViewTextBoxColumn();
            panel4 = new Panel();
            panel2 = new Panel();
            AddProductWithBarcodeButton = new ModernUI.ModernButton();
            AddProductWithoutBarcodeButton = new ModernUI.ModernButton();
            CategoryComboBox = new ModernUI.ModernComboBox();
            label1 = new Label();
            SearchByKeywordTextBox = new ModernUI.ModernTextBox();
            groupBox1 = new GroupBox();
            SearchByDescriptionKeywordRadioButton = new RadioButton();
            SearchByBrandKeywordRadioButton = new RadioButton();
            SearchByKeywordButton = new ModernUI.ModernButton();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ProductDataView).BeginInit();
            panel4.SuspendLayout();
            panel2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(30, 30, 30);
            panel1.Controls.Add(ProductDataView);
            panel1.Controls.Add(panel4);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(3);
            panel1.Size = new Size(1610, 697);
            panel1.TabIndex = 0;
            // 
            // ProductDataView
            // 
            ProductDataView.AllowUserToAddRows = false;
            ProductDataView.AllowUserToDeleteRows = false;
            ProductDataView.AllowUserToResizeColumns = false;
            ProductDataView.AllowUserToResizeRows = false;
            ProductDataView.BackgroundColor = Color.FromArgb(38, 38, 38);
            ProductDataView.BorderStyle = BorderStyle.None;
            ProductDataView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ProductDataView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle5.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle5.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle5.Padding = new Padding(10, 0, 0, 0);
            dataGridViewCellStyle5.SelectionBackColor = Color.Gray;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.False;
            ProductDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            ProductDataView.ColumnHeadersHeight = 50;
            ProductDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ProductDataView.Columns.AddRange(new DataGridViewColumn[] { ProductCode, Description, QuantityInStock, UnitPrice, GroupPrice, GroupPriceQuantity, Category, Manufacturer, Brand, DateCreated, DateUpdated });
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(38, 38, 38);
            dataGridViewCellStyle6.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle6.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle6.SelectionForeColor = Color.Gainsboro;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
            ProductDataView.DefaultCellStyle = dataGridViewCellStyle6;
            ProductDataView.Dock = DockStyle.Fill;
            ProductDataView.EnableHeadersVisualStyles = false;
            ProductDataView.GridColor = Color.DimGray;
            ProductDataView.Location = new Point(3, 63);
            ProductDataView.MultiSelect = false;
            ProductDataView.Name = "ProductDataView";
            ProductDataView.RowHeadersVisible = false;
            ProductDataView.RowHeadersWidth = 60;
            ProductDataView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            ProductDataView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ProductDataView.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            ProductDataView.RowTemplate.DefaultCellStyle.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ProductDataView.RowTemplate.DefaultCellStyle.ForeColor = Color.Gainsboro;
            ProductDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            ProductDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
            ProductDataView.RowTemplate.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            ProductDataView.RowTemplate.Height = 35;
            ProductDataView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            ProductDataView.Size = new Size(1604, 631);
            ProductDataView.TabIndex = 2;
            ProductDataView.DoubleClick += ProductDataView_DoubleClick;
            // 
            // ProductCode
            // 
            ProductCode.HeaderText = "รหัสสินค้า";
            ProductCode.Name = "ProductCode";
            ProductCode.ReadOnly = true;
            ProductCode.Width = 150;
            // 
            // Description
            // 
            Description.HeaderText = "คำอธิบาย";
            Description.Name = "Description";
            Description.ReadOnly = true;
            Description.Width = 300;
            // 
            // QuantityInStock
            // 
            QuantityInStock.HeaderText = "จำนวนในคลัง";
            QuantityInStock.Name = "QuantityInStock";
            QuantityInStock.ReadOnly = true;
            QuantityInStock.Width = 150;
            // 
            // UnitPrice
            // 
            UnitPrice.HeaderText = "ราคาขาย";
            UnitPrice.Name = "UnitPrice";
            UnitPrice.ReadOnly = true;
            UnitPrice.Width = 150;
            // 
            // GroupPrice
            // 
            GroupPrice.HeaderText = "ราคาขายต่อกลุ่ม";
            GroupPrice.Name = "GroupPrice";
            GroupPrice.ReadOnly = true;
            GroupPrice.Width = 170;
            // 
            // GroupPriceQuantity
            // 
            GroupPriceQuantity.HeaderText = "จำนวนต่อกลุ่ม";
            GroupPriceQuantity.Name = "GroupPriceQuantity";
            GroupPriceQuantity.ReadOnly = true;
            GroupPriceQuantity.Width = 150;
            // 
            // Category
            // 
            Category.HeaderText = "ประเภท";
            Category.Name = "Category";
            Category.ReadOnly = true;
            Category.Width = 200;
            // 
            // Manufacturer
            // 
            Manufacturer.HeaderText = "ผู้ผลิต";
            Manufacturer.Name = "Manufacturer";
            Manufacturer.ReadOnly = true;
            Manufacturer.Width = 200;
            // 
            // Brand
            // 
            Brand.HeaderText = "ยี่ห้อ";
            Brand.Name = "Brand";
            Brand.ReadOnly = true;
            Brand.Width = 200;
            // 
            // DateCreated
            // 
            DateCreated.HeaderText = "วันที่นำเข้า";
            DateCreated.Name = "DateCreated";
            DateCreated.ReadOnly = true;
            DateCreated.Width = 150;
            // 
            // DateUpdated
            // 
            DateUpdated.HeaderText = "วันที่อัพเดท";
            DateUpdated.Name = "DateUpdated";
            DateUpdated.ReadOnly = true;
            DateUpdated.Width = 150;
            // 
            // panel4
            // 
            panel4.BackColor = Color.FromArgb(38, 38, 38);
            panel4.Controls.Add(SearchByKeywordButton);
            panel4.Controls.Add(groupBox1);
            panel4.Controls.Add(SearchByKeywordTextBox);
            panel4.Controls.Add(panel2);
            panel4.Controls.Add(CategoryComboBox);
            panel4.Controls.Add(label1);
            panel4.Dock = DockStyle.Top;
            panel4.Location = new Point(3, 3);
            panel4.Name = "panel4";
            panel4.Size = new Size(1604, 60);
            panel4.TabIndex = 3;
            // 
            // panel2
            // 
            panel2.Controls.Add(AddProductWithBarcodeButton);
            panel2.Controls.Add(AddProductWithoutBarcodeButton);
            panel2.Dock = DockStyle.Right;
            panel2.Location = new Point(1021, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(583, 60);
            panel2.TabIndex = 8;
            // 
            // AddProductWithBarcodeButton
            // 
            AddProductWithBarcodeButton.BackColor = Color.FromArgb(38, 38, 38);
            AddProductWithBarcodeButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddProductWithBarcodeButton.BorderColor = Color.FromArgb(37, 182, 210);
            AddProductWithBarcodeButton.BorderRadius = 19;
            AddProductWithBarcodeButton.BorderSize = 1;
            AddProductWithBarcodeButton.FlatAppearance.BorderSize = 0;
            AddProductWithBarcodeButton.FlatStyle = FlatStyle.Flat;
            AddProductWithBarcodeButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            AddProductWithBarcodeButton.ForeColor = Color.Gainsboro;
            AddProductWithBarcodeButton.Location = new Point(3, 4);
            AddProductWithBarcodeButton.Name = "AddProductWithBarcodeButton";
            AddProductWithBarcodeButton.Size = new Size(264, 50);
            AddProductWithBarcodeButton.TabIndex = 11;
            AddProductWithBarcodeButton.Text = "เพิ่มรายการสินค้ามี Barcode";
            AddProductWithBarcodeButton.TextColor = Color.Gainsboro;
            AddProductWithBarcodeButton.UseVisualStyleBackColor = false;
            AddProductWithBarcodeButton.Click += AddProductWithBarcodeButton_Click;
            // 
            // AddProductWithoutBarcodeButton
            // 
            AddProductWithoutBarcodeButton.BackColor = Color.FromArgb(38, 38, 38);
            AddProductWithoutBarcodeButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddProductWithoutBarcodeButton.BorderColor = Color.DarkSalmon;
            AddProductWithoutBarcodeButton.BorderRadius = 19;
            AddProductWithoutBarcodeButton.BorderSize = 1;
            AddProductWithoutBarcodeButton.FlatAppearance.BorderSize = 0;
            AddProductWithoutBarcodeButton.FlatStyle = FlatStyle.Flat;
            AddProductWithoutBarcodeButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            AddProductWithoutBarcodeButton.ForeColor = Color.Gainsboro;
            AddProductWithoutBarcodeButton.Location = new Point(273, 4);
            AddProductWithoutBarcodeButton.Name = "AddProductWithoutBarcodeButton";
            AddProductWithoutBarcodeButton.Size = new Size(293, 50);
            AddProductWithoutBarcodeButton.TabIndex = 9;
            AddProductWithoutBarcodeButton.Text = "เพิ่มรายการสินค้าไม่มี Barcode";
            AddProductWithoutBarcodeButton.TextColor = Color.Gainsboro;
            AddProductWithoutBarcodeButton.UseVisualStyleBackColor = false;
            AddProductWithoutBarcodeButton.Click += AddProductButton_Click;
            // 
            // CategoryComboBox
            // 
            CategoryComboBox.BackColor = Color.FromArgb(38, 38, 38);
            CategoryComboBox.BorderColor = Color.DimGray;
            CategoryComboBox.BorderSize = 1;
            CategoryComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            CategoryComboBox.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            CategoryComboBox.ForeColor = Color.Gainsboro;
            CategoryComboBox.IconColor = Color.Gainsboro;
            CategoryComboBox.ListBackColor = Color.FromArgb(38, 38, 38);
            CategoryComboBox.ListTextColor = Color.Gainsboro;
            CategoryComboBox.Location = new Point(184, 3);
            CategoryComboBox.MinimumSize = new Size(200, 35);
            CategoryComboBox.Name = "CategoryComboBox";
            CategoryComboBox.Padding = new Padding(1);
            CategoryComboBox.SelectedIndex = -1;
            CategoryComboBox.SelectedItem = null;
            CategoryComboBox.Size = new Size(300, 54);
            CategoryComboBox.TabIndex = 0;
            CategoryComboBox.Texts = "เลือกประเภทสินค้า";
            CategoryComboBox.SelectedIndexChanged += CategoryComboBox_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.BackColor = Color.FromArgb(38, 38, 38);
            label1.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label1.ForeColor = Color.Gainsboro;
            label1.Location = new Point(3, 10);
            label1.Name = "label1";
            label1.Size = new Size(175, 35);
            label1.TabIndex = 3;
            label1.Text = "ประเภทสินค้า";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // SearchByKeywordTextBox
            // 
            SearchByKeywordTextBox.BackColor = Color.FromArgb(38, 38, 38);
            SearchByKeywordTextBox.BorderColor = Color.DimGray;
            SearchByKeywordTextBox.BorderSize = 1;
            SearchByKeywordTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F, FontStyle.Regular, GraphicsUnit.Point);
            SearchByKeywordTextBox.ForeColor = Color.Gainsboro;
            SearchByKeywordTextBox.Location = new Point(502, 10);
            SearchByKeywordTextBox.Multiline = false;
            SearchByKeywordTextBox.Name = "SearchByKeywordTextBox";
            SearchByKeywordTextBox.Padding = new Padding(7);
            SearchByKeywordTextBox.PasswordChar = false;
            SearchByKeywordTextBox.ReadOnly = false;
            SearchByKeywordTextBox.Size = new Size(120, 39);
            SearchByKeywordTextBox.TabIndex = 50;
            SearchByKeywordTextBox.TextAlign = HorizontalAlignment.Center;
            SearchByKeywordTextBox.Texts = "";
            SearchByKeywordTextBox.UnderlinedStyle = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(SearchByBrandKeywordRadioButton);
            groupBox1.Controls.Add(SearchByDescriptionKeywordRadioButton);
            groupBox1.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            groupBox1.ForeColor = Color.Silver;
            groupBox1.Location = new Point(631, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(194, 46);
            groupBox1.TabIndex = 51;
            groupBox1.TabStop = false;
            // 
            // SearchByDescriptionKeywordRadioButton
            // 
            SearchByDescriptionKeywordRadioButton.AutoSize = true;
            SearchByDescriptionKeywordRadioButton.Checked = true;
            SearchByDescriptionKeywordRadioButton.Font = new Font("FC Subject [Non-commercial] Reg", 9.749999F, FontStyle.Regular, GraphicsUnit.Point);
            SearchByDescriptionKeywordRadioButton.ForeColor = Color.Gainsboro;
            SearchByDescriptionKeywordRadioButton.Location = new Point(23, 16);
            SearchByDescriptionKeywordRadioButton.Name = "SearchByDescriptionKeywordRadioButton";
            SearchByDescriptionKeywordRadioButton.Size = new Size(78, 23);
            SearchByDescriptionKeywordRadioButton.TabIndex = 0;
            SearchByDescriptionKeywordRadioButton.TabStop = true;
            SearchByDescriptionKeywordRadioButton.Text = "คำอธิบาย";
            SearchByDescriptionKeywordRadioButton.UseVisualStyleBackColor = true;
            // 
            // SearchByBrandKeywordRadioButton
            // 
            SearchByBrandKeywordRadioButton.AutoSize = true;
            SearchByBrandKeywordRadioButton.Font = new Font("FC Subject [Non-commercial] Reg", 9.749999F, FontStyle.Regular, GraphicsUnit.Point);
            SearchByBrandKeywordRadioButton.ForeColor = Color.Gainsboro;
            SearchByBrandKeywordRadioButton.Location = new Point(125, 15);
            SearchByBrandKeywordRadioButton.Name = "SearchByBrandKeywordRadioButton";
            SearchByBrandKeywordRadioButton.Size = new Size(51, 23);
            SearchByBrandKeywordRadioButton.TabIndex = 1;
            SearchByBrandKeywordRadioButton.Text = "ยี่ห้อ";
            SearchByBrandKeywordRadioButton.UseVisualStyleBackColor = true;
            // 
            // SearchByKeywordButton
            // 
            SearchByKeywordButton.BackColor = Color.FromArgb(38, 38, 38);
            SearchByKeywordButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            SearchByKeywordButton.BorderColor = Color.FromArgb(70, 160, 160);
            SearchByKeywordButton.BorderRadius = 19;
            SearchByKeywordButton.BorderSize = 1;
            SearchByKeywordButton.FlatAppearance.BorderSize = 0;
            SearchByKeywordButton.FlatStyle = FlatStyle.Flat;
            SearchByKeywordButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F, FontStyle.Regular, GraphicsUnit.Point);
            SearchByKeywordButton.ForeColor = Color.Gainsboro;
            SearchByKeywordButton.Location = new Point(836, 4);
            SearchByKeywordButton.Name = "SearchByKeywordButton";
            SearchByKeywordButton.Size = new Size(148, 50);
            SearchByKeywordButton.TabIndex = 52;
            SearchByKeywordButton.Text = "ค้นหาสินค้า";
            SearchByKeywordButton.TextColor = Color.Gainsboro;
            SearchByKeywordButton.UseVisualStyleBackColor = false;
            SearchByKeywordButton.Click += SearchByKeywordButton_Click;
            // 
            // InventoryPanel
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.DarkGray;
            Controls.Add(panel1);
            Name = "InventoryPanel";
            Size = new Size(1610, 697);
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ProductDataView).EndInit();
            panel4.ResumeLayout(false);
            panel2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private DataGridView ProductDataView;
        private Label label1;
        private Panel panel4;
        private ModernUI.ModernComboBox CategoryComboBox;
        private Panel panel2;
        private ModernUI.ModernButton AddProductWithoutBarcodeButton;
        private DataGridViewTextBoxColumn ProductCode;
        private DataGridViewTextBoxColumn Description;
        private DataGridViewTextBoxColumn QuantityInStock;
        private DataGridViewTextBoxColumn UnitPrice;
        private DataGridViewTextBoxColumn GroupPrice;
        private DataGridViewTextBoxColumn GroupPriceQuantity;
        private DataGridViewTextBoxColumn Category;
        private DataGridViewTextBoxColumn Manufacturer;
        private DataGridViewTextBoxColumn Brand;
        private DataGridViewTextBoxColumn DateCreated;
        private DataGridViewTextBoxColumn DateUpdated;
        private ModernUI.ModernButton AddProductWithBarcodeButton;
        private ModernUI.ModernTextBox SearchByKeywordTextBox;
        private GroupBox groupBox1;
        private RadioButton SearchByBrandKeywordRadioButton;
        private RadioButton SearchByDescriptionKeywordRadioButton;
        private ModernUI.ModernButton SearchByKeywordButton;
    }
}
