namespace IndyPOS.UI
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ProductDataView = new System.Windows.Forms.DataGridView();
            this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.QuantityInStock = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.GroupPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.GroupPriceQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Category = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Manufacturer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Brand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateUpdated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.AddProductWithoutBarcodeButton = new ModernUI.ModernButton();
            this.CategoryComboBox = new ModernUI.ModernComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.modernButton1 = new ModernUI.ModernButton();
            this.AddProductWithBarcodeButton = new ModernUI.ModernButton();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ProductDataView)).BeginInit();
            this.panel4.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel1.Controls.Add(this.ProductDataView);
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(3);
            this.panel1.Size = new System.Drawing.Size(1421, 697);
            this.panel1.TabIndex = 0;
            // 
            // ProductDataView
            // 
            this.ProductDataView.AllowUserToAddRows = false;
            this.ProductDataView.AllowUserToDeleteRows = false;
            this.ProductDataView.AllowUserToResizeColumns = false;
            this.ProductDataView.AllowUserToResizeRows = false;
            this.ProductDataView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ProductDataView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ProductDataView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.ProductDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle7.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.Gray;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.ProductDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.ProductDataView.ColumnHeadersHeight = 50;
            this.ProductDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.ProductDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProductCode,
            this.Description,
            this.QuantityInStock,
            this.UnitPrice,
            this.GroupPrice,
            this.GroupPriceQuantity,
            this.Category,
            this.Manufacturer,
            this.Brand,
            this.DateCreated,
            this.DateUpdated});
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            dataGridViewCellStyle8.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.ProductDataView.DefaultCellStyle = dataGridViewCellStyle8;
            this.ProductDataView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProductDataView.EnableHeadersVisualStyles = false;
            this.ProductDataView.GridColor = System.Drawing.Color.DimGray;
            this.ProductDataView.Location = new System.Drawing.Point(3, 63);
            this.ProductDataView.MultiSelect = false;
            this.ProductDataView.Name = "ProductDataView";
            this.ProductDataView.RowHeadersVisible = false;
            this.ProductDataView.RowHeadersWidth = 60;
            this.ProductDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.ProductDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.ProductDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ProductDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProductDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;
            this.ProductDataView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.ProductDataView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Gainsboro;
            this.ProductDataView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ProductDataView.RowTemplate.Height = 35;
            this.ProductDataView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.ProductDataView.Size = new System.Drawing.Size(1415, 631);
            this.ProductDataView.TabIndex = 2;
            this.ProductDataView.DoubleClick += new System.EventHandler(this.ProductDataView_DoubleClick);
            // 
            // ProductCode
            // 
            this.ProductCode.HeaderText = "รหัสสินค้า";
            this.ProductCode.Name = "ProductCode";
            this.ProductCode.ReadOnly = true;
            this.ProductCode.Width = 150;
            // 
            // Description
            // 
            this.Description.HeaderText = "คำอธิบาย";
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 300;
            // 
            // QuantityInStock
            // 
            this.QuantityInStock.HeaderText = "จำนวนในคลัง";
            this.QuantityInStock.Name = "QuantityInStock";
            this.QuantityInStock.ReadOnly = true;
            this.QuantityInStock.Width = 150;
            // 
            // UnitPrice
            // 
            this.UnitPrice.HeaderText = "ราคาขาย";
            this.UnitPrice.Name = "UnitPrice";
            this.UnitPrice.ReadOnly = true;
            this.UnitPrice.Width = 150;
            // 
            // GroupPrice
            // 
            this.GroupPrice.HeaderText = "ราคาขายต่อกลุ่ม";
            this.GroupPrice.Name = "GroupPrice";
            this.GroupPrice.ReadOnly = true;
            this.GroupPrice.Width = 170;
            // 
            // GroupPriceQuantity
            // 
            this.GroupPriceQuantity.HeaderText = "จำนวนต่อกลุ่ม";
            this.GroupPriceQuantity.Name = "GroupPriceQuantity";
            this.GroupPriceQuantity.ReadOnly = true;
            this.GroupPriceQuantity.Width = 150;
            // 
            // Category
            // 
            this.Category.HeaderText = "ประเภท";
            this.Category.Name = "Category";
            this.Category.ReadOnly = true;
            this.Category.Width = 200;
            // 
            // Manufacturer
            // 
            this.Manufacturer.HeaderText = "ผู้ผลิต";
            this.Manufacturer.Name = "Manufacturer";
            this.Manufacturer.ReadOnly = true;
            this.Manufacturer.Width = 200;
            // 
            // Brand
            // 
            this.Brand.HeaderText = "ยี่ห้อ";
            this.Brand.Name = "Brand";
            this.Brand.ReadOnly = true;
            this.Brand.Width = 200;
            // 
            // DateCreated
            // 
            this.DateCreated.HeaderText = "วันที่นำเข้า";
            this.DateCreated.Name = "DateCreated";
            this.DateCreated.ReadOnly = true;
            this.DateCreated.Width = 150;
            // 
            // DateUpdated
            // 
            this.DateUpdated.HeaderText = "วันที่อัพเดท";
            this.DateUpdated.Name = "DateUpdated";
            this.DateUpdated.ReadOnly = true;
            this.DateUpdated.Width = 150;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel4.Controls.Add(this.panel2);
            this.panel4.Controls.Add(this.CategoryComboBox);
            this.panel4.Controls.Add(this.label1);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(3, 3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1415, 60);
            this.panel4.TabIndex = 3;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.AddProductWithBarcodeButton);
            this.panel2.Controls.Add(this.modernButton1);
            this.panel2.Controls.Add(this.AddProductWithoutBarcodeButton);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel2.Location = new System.Drawing.Point(571, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(844, 60);
            this.panel2.TabIndex = 8;
            // 
            // AddProductWithoutBarcodeButton
            // 
            this.AddProductWithoutBarcodeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddProductWithoutBarcodeButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddProductWithoutBarcodeButton.BorderColor = System.Drawing.Color.DarkSalmon;
            this.AddProductWithoutBarcodeButton.BorderRadius = 19;
            this.AddProductWithoutBarcodeButton.BorderSize = 1;
            this.AddProductWithoutBarcodeButton.FlatAppearance.BorderSize = 0;
            this.AddProductWithoutBarcodeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddProductWithoutBarcodeButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddProductWithoutBarcodeButton.ForeColor = System.Drawing.Color.White;
            this.AddProductWithoutBarcodeButton.Location = new System.Drawing.Point(273, 4);
            this.AddProductWithoutBarcodeButton.Name = "AddProductWithoutBarcodeButton";
            this.AddProductWithoutBarcodeButton.Size = new System.Drawing.Size(293, 50);
            this.AddProductWithoutBarcodeButton.TabIndex = 9;
            this.AddProductWithoutBarcodeButton.Text = "เพิ่มรายการสินค้าไม่มี Barcode";
            this.AddProductWithoutBarcodeButton.TextColor = System.Drawing.Color.White;
            this.AddProductWithoutBarcodeButton.UseVisualStyleBackColor = false;
            this.AddProductWithoutBarcodeButton.Click += new System.EventHandler(this.AddProductButton_Click);
            // 
            // CategoryComboBox
            // 
            this.CategoryComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CategoryComboBox.BorderColor = System.Drawing.Color.DimGray;
            this.CategoryComboBox.BorderSize = 1;
            this.CategoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.CategoryComboBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CategoryComboBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.CategoryComboBox.IconColor = System.Drawing.Color.Gainsboro;
            this.CategoryComboBox.ListBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CategoryComboBox.ListTextColor = System.Drawing.Color.Gainsboro;
            this.CategoryComboBox.Location = new System.Drawing.Point(184, 3);
            this.CategoryComboBox.MinimumSize = new System.Drawing.Size(200, 35);
            this.CategoryComboBox.Name = "CategoryComboBox";
            this.CategoryComboBox.Padding = new System.Windows.Forms.Padding(1);
            this.CategoryComboBox.SelectedIndex = -1;
            this.CategoryComboBox.SelectedItem = null;
            this.CategoryComboBox.Size = new System.Drawing.Size(300, 54);
            this.CategoryComboBox.TabIndex = 0;
            this.CategoryComboBox.Texts = "เลือกประเภทสินค้า";
            this.CategoryComboBox.SelectedIndexChanged += new System.EventHandler(this.CategoryComboBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Gainsboro;
            this.label1.Location = new System.Drawing.Point(3, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(175, 35);
            this.label1.TabIndex = 3;
            this.label1.Text = "ประเภทสินค้า";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // modernButton1
            // 
            this.modernButton1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.modernButton1.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.modernButton1.BorderColor = System.Drawing.Color.Violet;
            this.modernButton1.BorderRadius = 19;
            this.modernButton1.BorderSize = 1;
            this.modernButton1.FlatAppearance.BorderSize = 0;
            this.modernButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.modernButton1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.modernButton1.ForeColor = System.Drawing.Color.White;
            this.modernButton1.Location = new System.Drawing.Point(572, 4);
            this.modernButton1.Name = "modernButton1";
            this.modernButton1.Size = new System.Drawing.Size(264, 50);
            this.modernButton1.TabIndex = 10;
            this.modernButton1.Text = "เปิด Barcodes Folder";
            this.modernButton1.TextColor = System.Drawing.Color.White;
            this.modernButton1.UseVisualStyleBackColor = false;
            this.modernButton1.Click += new System.EventHandler(this.ModernButton1_Click);
            // 
            // AddProductWithBarcodeButton
            // 
            this.AddProductWithBarcodeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddProductWithBarcodeButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.AddProductWithBarcodeButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(182)))), ((int)(((byte)(210)))));
            this.AddProductWithBarcodeButton.BorderRadius = 19;
            this.AddProductWithBarcodeButton.BorderSize = 1;
            this.AddProductWithBarcodeButton.FlatAppearance.BorderSize = 0;
            this.AddProductWithBarcodeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddProductWithBarcodeButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddProductWithBarcodeButton.ForeColor = System.Drawing.Color.White;
            this.AddProductWithBarcodeButton.Location = new System.Drawing.Point(3, 4);
            this.AddProductWithBarcodeButton.Name = "AddProductWithBarcodeButton";
            this.AddProductWithBarcodeButton.Size = new System.Drawing.Size(264, 50);
            this.AddProductWithBarcodeButton.TabIndex = 11;
            this.AddProductWithBarcodeButton.Text = "เพิ่มรายการสินค้ามี Barcode";
            this.AddProductWithBarcodeButton.TextColor = System.Drawing.Color.White;
            this.AddProductWithBarcodeButton.UseVisualStyleBackColor = false;
            this.AddProductWithBarcodeButton.Click += new System.EventHandler(this.AddProductWithBarcodeButton_Click);
            // 
            // InventoryPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.Controls.Add(this.panel1);
            this.Name = "InventoryPanel";
            this.Size = new System.Drawing.Size(1421, 697);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ProductDataView)).EndInit();
            this.panel4.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView ProductDataView;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel4;
		private ModernUI.ModernComboBox CategoryComboBox;
		private System.Windows.Forms.Panel panel2;
        private ModernUI.ModernButton AddProductWithoutBarcodeButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn QuantityInStock;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn GroupPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn GroupPriceQuantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn Category;
        private System.Windows.Forms.DataGridViewTextBoxColumn Manufacturer;
        private System.Windows.Forms.DataGridViewTextBoxColumn Brand;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateUpdated;
        private ModernUI.ModernButton modernButton1;
        private ModernUI.ModernButton AddProductWithBarcodeButton;
    }
}
