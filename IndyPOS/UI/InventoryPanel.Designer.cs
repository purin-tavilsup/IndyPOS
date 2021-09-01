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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			this.panel1 = new System.Windows.Forms.Panel();
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
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel9 = new System.Windows.Forms.Panel();
			this.AddProductButton = new System.Windows.Forms.Button();
			this.panel3 = new System.Windows.Forms.Panel();
			this.GetProductsByCategoryButton = new System.Windows.Forms.Button();
			this.ProductCategoryListBox = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ProductDataView)).BeginInit();
			this.panel2.SuspendLayout();
			this.panel9.SuspendLayout();
			this.panel3.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.DarkGray;
			this.panel1.Controls.Add(this.ProductDataView);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(1137, 697);
			this.panel1.TabIndex = 0;
			// 
			// ProductDataView
			// 
			this.ProductDataView.AllowUserToResizeColumns = false;
			this.ProductDataView.AllowUserToResizeRows = false;
			this.ProductDataView.BackgroundColor = System.Drawing.Color.DarkGray;
			this.ProductDataView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle4.BackColor = System.Drawing.Color.Silver;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.Gray;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.ProductDataView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.ProductDataView.ColumnHeadersHeight = 40;
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
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.ProductDataView.DefaultCellStyle = dataGridViewCellStyle5;
			this.ProductDataView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProductDataView.EnableHeadersVisualStyles = false;
			this.ProductDataView.GridColor = System.Drawing.Color.DimGray;
			this.ProductDataView.Location = new System.Drawing.Point(0, 0);
			this.ProductDataView.MultiSelect = false;
			this.ProductDataView.Name = "ProductDataView";
			this.ProductDataView.RowHeadersVisible = false;
			this.ProductDataView.RowHeadersWidth = 60;
			this.ProductDataView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle6.BackColor = System.Drawing.Color.Gainsboro;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ProductDataView.RowsDefaultCellStyle = dataGridViewCellStyle6;
			this.ProductDataView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.ProductDataView.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.Color.Gainsboro;
			this.ProductDataView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ProductDataView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ProductDataView.RowTemplate.Height = 35;
			this.ProductDataView.Size = new System.Drawing.Size(1137, 697);
			this.ProductDataView.TabIndex = 2;
			this.ProductDataView.DoubleClick += new System.EventHandler(this.ProductDataView_DoubleClick);
			// 
			// ProductCode
			// 
			this.ProductCode.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.ProductCode.HeaderText = "รหัสสินค้า";
			this.ProductCode.Name = "ProductCode";
			this.ProductCode.ReadOnly = true;
			this.ProductCode.Width = 130;
			// 
			// Description
			// 
			this.Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.Description.HeaderText = "คำอธิบาย";
			this.Description.Name = "Description";
			this.Description.ReadOnly = true;
			this.Description.Width = 250;
			// 
			// QuantityInStock
			// 
			this.QuantityInStock.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.QuantityInStock.HeaderText = "จำนวนในคลัง";
			this.QuantityInStock.Name = "QuantityInStock";
			this.QuantityInStock.ReadOnly = true;
			// 
			// UnitPrice
			// 
			this.UnitPrice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.UnitPrice.HeaderText = "ราคาขาย";
			this.UnitPrice.Name = "UnitPrice";
			this.UnitPrice.ReadOnly = true;
			// 
			// UnitCost
			// 
			this.UnitCost.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.UnitCost.HeaderText = "ราคาซื้อ";
			this.UnitCost.Name = "UnitCost";
			this.UnitCost.ReadOnly = true;
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
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.DarkGray;
			this.panel2.Controls.Add(this.panel9);
			this.panel2.Controls.Add(this.panel3);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel2.Location = new System.Drawing.Point(1137, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(284, 697);
			this.panel2.TabIndex = 1;
			// 
			// panel9
			// 
			this.panel9.BackColor = System.Drawing.Color.Silver;
			this.panel9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel9.Controls.Add(this.AddProductButton);
			this.panel9.Location = new System.Drawing.Point(11, 336);
			this.panel9.Name = "panel9";
			this.panel9.Size = new System.Drawing.Size(261, 103);
			this.panel9.TabIndex = 21;
			// 
			// AddProductButton
			// 
			this.AddProductButton.BackColor = System.Drawing.Color.Gainsboro;
			this.AddProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.AddProductButton.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AddProductButton.ForeColor = System.Drawing.Color.Black;
			this.AddProductButton.Image = global::IndyPOS.Properties.Resources.Plus_50;
			this.AddProductButton.Location = new System.Drawing.Point(3, 3);
			this.AddProductButton.Name = "AddProductButton";
			this.AddProductButton.Size = new System.Drawing.Size(253, 95);
			this.AddProductButton.TabIndex = 7;
			this.AddProductButton.Text = "เพิ่มรายการสินค้า";
			this.AddProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.AddProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.AddProductButton.UseVisualStyleBackColor = false;
			this.AddProductButton.Click += new System.EventHandler(this.AddProductButton_Click);
			// 
			// panel3
			// 
			this.panel3.BackColor = System.Drawing.Color.Silver;
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.GetProductsByCategoryButton);
			this.panel3.Controls.Add(this.ProductCategoryListBox);
			this.panel3.Controls.Add(this.label1);
			this.panel3.Location = new System.Drawing.Point(11, 15);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(261, 315);
			this.panel3.TabIndex = 20;
			// 
			// GetProductsByCategoryButton
			// 
			this.GetProductsByCategoryButton.BackColor = System.Drawing.Color.Gainsboro;
			this.GetProductsByCategoryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.GetProductsByCategoryButton.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.GetProductsByCategoryButton.ForeColor = System.Drawing.Color.Black;
			this.GetProductsByCategoryButton.Image = global::IndyPOS.Properties.Resources.Search_50;
			this.GetProductsByCategoryButton.Location = new System.Drawing.Point(7, 210);
			this.GetProductsByCategoryButton.Name = "GetProductsByCategoryButton";
			this.GetProductsByCategoryButton.Size = new System.Drawing.Size(242, 95);
			this.GetProductsByCategoryButton.TabIndex = 4;
			this.GetProductsByCategoryButton.Text = "แสดงสินค้า";
			this.GetProductsByCategoryButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.GetProductsByCategoryButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.GetProductsByCategoryButton.UseVisualStyleBackColor = false;
			this.GetProductsByCategoryButton.Click += new System.EventHandler(this.GetProductsByCategoryButton_Click);
			// 
			// ProductCategoryListBox
			// 
			this.ProductCategoryListBox.BackColor = System.Drawing.Color.LightGray;
			this.ProductCategoryListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.ProductCategoryListBox.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ProductCategoryListBox.FormattingEnabled = true;
			this.ProductCategoryListBox.ItemHeight = 20;
			this.ProductCategoryListBox.Items.AddRange(new object[] {
            "อาหาร",
            "เครื่องดื่ม",
            "ขนม",
            "เครื่องดื่มแอลกอฮอล์",
            "วัสดุและอุปกรณ์ทั่วไป",
            "วัสดุและอุปกรณ์การเกษตร"});
			this.ProductCategoryListBox.Location = new System.Drawing.Point(8, 42);
			this.ProductCategoryListBox.Name = "ProductCategoryListBox";
			this.ProductCategoryListBox.Size = new System.Drawing.Size(242, 162);
			this.ProductCategoryListBox.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Silver;
			this.label1.Font = new System.Drawing.Font("Leelawadee UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(8, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(255, 39);
			this.label1.TabIndex = 3;
			this.label1.Text = "ประเภทสินค้า";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// InventoryPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.DarkGray;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panel2);
			this.Name = "InventoryPanel";
			this.Size = new System.Drawing.Size(1421, 697);
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.ProductDataView)).EndInit();
			this.panel2.ResumeLayout(false);
			this.panel9.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView ProductDataView;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Button AddProductButton;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button GetProductsByCategoryButton;
        private System.Windows.Forms.ListBox ProductCategoryListBox;
        private System.Windows.Forms.Label label1;
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
	}
}
