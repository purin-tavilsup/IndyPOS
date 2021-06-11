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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel9 = new System.Windows.Forms.Panel();
            this.AddProductButton = new System.Windows.Forms.Button();
            this.panel4 = new System.Windows.Forms.Panel();
            this.SearchProductButton = new System.Windows.Forms.Button();
            this.SearchProductTextBox = new System.Windows.Forms.TextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.GetProductsByCategoryButton = new System.Windows.Forms.Button();
            this.ProductCategoryListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.ProductDataView = new System.Windows.Forms.DataGridView();
            this.ProductCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.QuantityInStock = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UnitCost = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Category = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Manufacturer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Brand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DateUpdated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SearchByKeywordRadioButton = new System.Windows.Forms.RadioButton();
            this.SearchByBarcodeRadioButton = new System.Windows.Forms.RadioButton();
            this.panel1.SuspendLayout();
            this.panel9.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ProductDataView)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.DarkGray;
            this.panel1.Controls.Add(this.panel9);
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(275, 700);
            this.panel1.TabIndex = 0;
            // 
            // panel9
            // 
            this.panel9.BackColor = System.Drawing.Color.Silver;
            this.panel9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel9.Controls.Add(this.AddProductButton);
            this.panel9.Location = new System.Drawing.Point(8, 465);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(261, 139);
            this.panel9.TabIndex = 18;
            // 
            // AddProductButton
            // 
            this.AddProductButton.BackColor = System.Drawing.Color.Gainsboro;
            this.AddProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddProductButton.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddProductButton.Image = global::IndyPOS.Properties.Resources.PlusRectangle_50;
            this.AddProductButton.Location = new System.Drawing.Point(3, 3);
            this.AddProductButton.Name = "AddProductButton";
            this.AddProductButton.Size = new System.Drawing.Size(253, 130);
            this.AddProductButton.TabIndex = 7;
            this.AddProductButton.Text = "เพิ่มรายการสินค้า";
            this.AddProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.AddProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.AddProductButton.UseVisualStyleBackColor = false;
            this.AddProductButton.Click += new System.EventHandler(this.AddProductButton_Click);
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.Silver;
            this.panel4.Controls.Add(this.groupBox1);
            this.panel4.Controls.Add(this.SearchProductButton);
            this.panel4.Controls.Add(this.SearchProductTextBox);
            this.panel4.Location = new System.Drawing.Point(8, 234);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(261, 225);
            this.panel4.TabIndex = 3;
            // 
            // SearchProductButton
            // 
            this.SearchProductButton.BackColor = System.Drawing.Color.Gainsboro;
            this.SearchProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SearchProductButton.Font = new System.Drawing.Font("Leelawadee UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SearchProductButton.Location = new System.Drawing.Point(8, 152);
            this.SearchProductButton.Name = "SearchProductButton";
            this.SearchProductButton.Size = new System.Drawing.Size(242, 65);
            this.SearchProductButton.TabIndex = 5;
            this.SearchProductButton.Text = "ค้นหาสินค้า";
            this.SearchProductButton.UseVisualStyleBackColor = false;
            this.SearchProductButton.Click += new System.EventHandler(this.SearchProduct_Click);
            // 
            // SearchProductTextBox
            // 
            this.SearchProductTextBox.Font = new System.Drawing.Font("Leelawadee UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SearchProductTextBox.Location = new System.Drawing.Point(8, 12);
            this.SearchProductTextBox.Name = "SearchProductTextBox";
            this.SearchProductTextBox.Size = new System.Drawing.Size(242, 29);
            this.SearchProductTextBox.TabIndex = 3;
            this.SearchProductTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.Silver;
            this.panel3.Controls.Add(this.GetProductsByCategoryButton);
            this.panel3.Controls.Add(this.ProductCategoryListBox);
            this.panel3.Controls.Add(this.label1);
            this.panel3.Location = new System.Drawing.Point(8, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(261, 225);
            this.panel3.TabIndex = 3;
            // 
            // GetProductsByCategoryButton
            // 
            this.GetProductsByCategoryButton.BackColor = System.Drawing.Color.Gainsboro;
            this.GetProductsByCategoryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.GetProductsByCategoryButton.Font = new System.Drawing.Font("Leelawadee UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GetProductsByCategoryButton.Location = new System.Drawing.Point(8, 150);
            this.GetProductsByCategoryButton.Name = "GetProductsByCategoryButton";
            this.GetProductsByCategoryButton.Size = new System.Drawing.Size(242, 65);
            this.GetProductsByCategoryButton.TabIndex = 4;
            this.GetProductsByCategoryButton.Text = "แสดงสินค้า";
            this.GetProductsByCategoryButton.UseVisualStyleBackColor = false;
            this.GetProductsByCategoryButton.Click += new System.EventHandler(this.GetProductsByCategoryButton_Click);
            // 
            // ProductCategoryListBox
            // 
            this.ProductCategoryListBox.BackColor = System.Drawing.Color.Silver;
            this.ProductCategoryListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ProductCategoryListBox.Font = new System.Drawing.Font("Leelawadee UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProductCategoryListBox.FormattingEnabled = true;
            this.ProductCategoryListBox.ItemHeight = 25;
            this.ProductCategoryListBox.Items.AddRange(new object[] {
            "อาหาร",
            "เครื่องดื่ม",
            "ขนม",
            "เครื่องดื่มแอลกอฮอล์",
            "วัสดุและอุปกรณ์ทั่วไป",
            "วัสดุและอุปกรณ์การเกษตร"});
            this.ProductCategoryListBox.Location = new System.Drawing.Point(8, 42);
            this.ProductCategoryListBox.Name = "ProductCategoryListBox";
            this.ProductCategoryListBox.Size = new System.Drawing.Size(242, 102);
            this.ProductCategoryListBox.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Silver;
            this.label1.Font = new System.Drawing.Font("Leelawadee UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(255, 39);
            this.label1.TabIndex = 3;
            this.label1.Text = "ประเภทสินค้า";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.DarkGray;
            this.panel2.Controls.Add(this.ProductDataView);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(275, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1375, 700);
            this.panel2.TabIndex = 1;
            // 
            // ProductDataView
            // 
            this.ProductDataView.AllowUserToResizeColumns = false;
            this.ProductDataView.AllowUserToResizeRows = false;
            this.ProductDataView.BackgroundColor = System.Drawing.Color.DarkGray;
            this.ProductDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.ProductDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.ProductDataView.ColumnHeadersHeight = 50;
            this.ProductDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.ProductDataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProductCode,
            this.Description,
            this.QuantityInStock,
            this.UnitPrice,
            this.UnitCost,
            this.Category,
            this.Manufacturer,
            this.Brand,
            this.DateCreated,
            this.DateUpdated});
            this.ProductDataView.EnableHeadersVisualStyles = false;
            this.ProductDataView.GridColor = System.Drawing.Color.DimGray;
            this.ProductDataView.Location = new System.Drawing.Point(6, 3);
            this.ProductDataView.MultiSelect = false;
            this.ProductDataView.Name = "ProductDataView";
            this.ProductDataView.RowHeadersVisible = false;
            this.ProductDataView.RowHeadersWidth = 60;
            this.ProductDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(0, 0, 20, 0);
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ProductDataView.RowsDefaultCellStyle = dataGridViewCellStyle5;
            this.ProductDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.ProductDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            this.ProductDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Leelawadee", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProductDataView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.WindowText;
            this.ProductDataView.RowTemplate.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(0, 0, 20, 0);
            this.ProductDataView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ProductDataView.RowTemplate.Height = 35;
            this.ProductDataView.Size = new System.Drawing.Size(1005, 674);
            this.ProductDataView.TabIndex = 2;
            this.ProductDataView.DoubleClick += new System.EventHandler(this.ProductDataView_DoubleClick);
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
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Leelawadee", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.Description.DefaultCellStyle = dataGridViewCellStyle2;
            this.Description.HeaderText = "คำอธิบาย";
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 500;
            // 
            // QuantityInStock
            // 
            this.QuantityInStock.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.QuantityInStock.HeaderText = "จำนวนในคลัง";
            this.QuantityInStock.Name = "QuantityInStock";
            this.QuantityInStock.ReadOnly = true;
            this.QuantityInStock.Width = 150;
            // 
            // UnitPrice
            // 
            this.UnitPrice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.UnitPrice.DefaultCellStyle = dataGridViewCellStyle3;
            this.UnitPrice.HeaderText = "ราคาขาย";
            this.UnitPrice.Name = "UnitPrice";
            this.UnitPrice.ReadOnly = true;
            this.UnitPrice.Width = 150;
            // 
            // UnitCost
            // 
            this.UnitCost.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.UnitCost.DefaultCellStyle = dataGridViewCellStyle4;
            this.UnitCost.HeaderText = "ราคาซื้อ";
            this.UnitCost.Name = "UnitCost";
            this.UnitCost.ReadOnly = true;
            this.UnitCost.Width = 150;
            // 
            // Category
            // 
            this.Category.HeaderText = "ประเภท";
            this.Category.Name = "Category";
            this.Category.ReadOnly = true;
            this.Category.Width = 250;
            // 
            // Manufacturer
            // 
            this.Manufacturer.HeaderText = "ผู้ผลิต";
            this.Manufacturer.Name = "Manufacturer";
            this.Manufacturer.ReadOnly = true;
            this.Manufacturer.Width = 150;
            // 
            // Brand
            // 
            this.Brand.HeaderText = "ยี่ห้อ";
            this.Brand.Name = "Brand";
            this.Brand.ReadOnly = true;
            this.Brand.Width = 150;
            // 
            // DateCreated
            // 
            this.DateCreated.HeaderText = "วันที่นำเข้า";
            this.DateCreated.Name = "DateCreated";
            this.DateCreated.ReadOnly = true;
            this.DateCreated.Width = 250;
            // 
            // DateUpdated
            // 
            this.DateUpdated.HeaderText = "วันที่อัพเดท";
            this.DateUpdated.Name = "DateUpdated";
            this.DateUpdated.ReadOnly = true;
            this.DateUpdated.Width = 250;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.SearchByBarcodeRadioButton);
            this.groupBox1.Controls.Add(this.SearchByKeywordRadioButton);
            this.groupBox1.Font = new System.Drawing.Font("Leelawadee UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(8, 47);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(242, 99);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "ค้นหาสินค้าด้วย";
            // 
            // SearchByKeywordRadioButton
            // 
            this.SearchByKeywordRadioButton.AutoSize = true;
            this.SearchByKeywordRadioButton.Checked = true;
            this.SearchByKeywordRadioButton.Location = new System.Drawing.Point(18, 28);
            this.SearchByKeywordRadioButton.Name = "SearchByKeywordRadioButton";
            this.SearchByKeywordRadioButton.Size = new System.Drawing.Size(179, 29);
            this.SearchByKeywordRadioButton.TabIndex = 3;
            this.SearchByKeywordRadioButton.TabStop = true;
            this.SearchByKeywordRadioButton.Text = "ชื่อสินค้า (คำอธิบาย)";
            this.SearchByKeywordRadioButton.UseVisualStyleBackColor = true;
            // 
            // SearchByBarcodeRadioButton
            // 
            this.SearchByBarcodeRadioButton.AutoSize = true;
            this.SearchByBarcodeRadioButton.Location = new System.Drawing.Point(18, 59);
            this.SearchByBarcodeRadioButton.Name = "SearchByBarcodeRadioButton";
            this.SearchByBarcodeRadioButton.Size = new System.Drawing.Size(175, 29);
            this.SearchByBarcodeRadioButton.TabIndex = 4;
            this.SearchByBarcodeRadioButton.Text = "บาร์โค้ด (รหัสสินค้า)";
            this.SearchByBarcodeRadioButton.UseVisualStyleBackColor = true;
            // 
            // InventoryPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SlateGray;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "InventoryPanel";
            this.Size = new System.Drawing.Size(1650, 700);
            this.panel1.ResumeLayout(false);
            this.panel9.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ProductDataView)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView ProductDataView;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox ProductCategoryListBox;
        private System.Windows.Forms.Button GetProductsByCategoryButton;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button SearchProductButton;
        private System.Windows.Forms.TextBox SearchProductTextBox;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Button AddProductButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn QuantityInStock;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn UnitCost;
        private System.Windows.Forms.DataGridViewTextBoxColumn Category;
        private System.Windows.Forms.DataGridViewTextBoxColumn Manufacturer;
        private System.Windows.Forms.DataGridViewTextBoxColumn Brand;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateUpdated;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton SearchByBarcodeRadioButton;
        private System.Windows.Forms.RadioButton SearchByKeywordRadioButton;
    }
}
