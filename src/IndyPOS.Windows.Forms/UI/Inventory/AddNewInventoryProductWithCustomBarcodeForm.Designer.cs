namespace IndyPOS.Windows.Forms.UI.Inventory
{
    partial class AddNewInventoryProductWithCustomBarcodeForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddNewInventoryProductWithCustomBarcodeForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.IsTrackableCheckBox = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.BarcodePictureBox = new System.Windows.Forms.PictureBox();
            this.CategoryComboBox = new ModernUI.ModernComboBox();
            this.GroupPriceQuantityTextBox = new ModernUI.ModernTextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.GroupPriceTextBox = new ModernUI.ModernTextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.BrandTextBox = new ModernUI.ModernTextBox();
            this.ManufacturerTextBox = new ModernUI.ModernTextBox();
            this.UnitPriceTextBox = new ModernUI.ModernTextBox();
            this.QuantityTextBox = new ModernUI.ModernTextBox();
            this.DescriptionTextBox = new ModernUI.ModernTextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.CancelProductEntryButton = new ModernUI.ModernButton();
            this.SaveProductEntryButton = new ModernUI.ModernButton();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.BarcodeTextBox = new ModernUI.ModernTextBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BarcodePictureBox)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.panel1.Controls.Add(this.BarcodeTextBox);
            this.panel1.Controls.Add(this.IsTrackableCheckBox);
            this.panel1.Controls.Add(this.label12);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.BarcodePictureBox);
            this.panel1.Controls.Add(this.CategoryComboBox);
            this.panel1.Controls.Add(this.GroupPriceQuantityTextBox);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.GroupPriceTextBox);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.BrandTextBox);
            this.panel1.Controls.Add(this.ManufacturerTextBox);
            this.panel1.Controls.Add(this.UnitPriceTextBox);
            this.panel1.Controls.Add(this.QuantityTextBox);
            this.panel1.Controls.Add(this.DescriptionTextBox);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(1, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1112, 680);
            this.panel1.TabIndex = 0;
            // 
            // IsTrackableCheckBox
            // 
            this.IsTrackableCheckBox.AutoSize = true;
            this.IsTrackableCheckBox.Checked = true;
            this.IsTrackableCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.IsTrackableCheckBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IsTrackableCheckBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.IsTrackableCheckBox.Location = new System.Drawing.Point(250, 199);
            this.IsTrackableCheckBox.Name = "IsTrackableCheckBox";
            this.IsTrackableCheckBox.Size = new System.Drawing.Size(229, 28);
            this.IsTrackableCheckBox.TabIndex = 72;
            this.IsTrackableCheckBox.Text = "สินค้านับจำนวนในคลังสินค้าได้";
            this.IsTrackableCheckBox.UseVisualStyleBackColor = true;
            this.IsTrackableCheckBox.CheckedChanged += new System.EventHandler(this.IsTrackableCheckBox_CheckedChanged);
            // 
            // label12
            // 
            this.label12.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.label12.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.Gainsboro;
            this.label12.Location = new System.Drawing.Point(663, 362);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(167, 58);
            this.label12.TabIndex = 70;
            this.label12.Text = "รหัสสินค้า";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.label6.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.Gainsboro;
            this.label6.Location = new System.Drawing.Point(663, 86);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(400, 70);
            this.label6.TabIndex = 69;
            this.label6.Text = "Barcode";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BarcodePictureBox
            // 
            this.BarcodePictureBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(35)))));
            this.BarcodePictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BarcodePictureBox.Location = new System.Drawing.Point(663, 159);
            this.BarcodePictureBox.Name = "BarcodePictureBox";
            this.BarcodePictureBox.Size = new System.Drawing.Size(400, 200);
            this.BarcodePictureBox.TabIndex = 68;
            this.BarcodePictureBox.TabStop = false;
            // 
            // CategoryComboBox
            // 
            this.CategoryComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CategoryComboBox.BorderColor = System.Drawing.Color.DimGray;
            this.CategoryComboBox.BorderSize = 0;
            this.CategoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.CategoryComboBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CategoryComboBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.CategoryComboBox.IconColor = System.Drawing.Color.Gainsboro;
            this.CategoryComboBox.ListBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CategoryComboBox.ListTextColor = System.Drawing.Color.Gainsboro;
            this.CategoryComboBox.Location = new System.Drawing.Point(250, 99);
            this.CategoryComboBox.MinimumSize = new System.Drawing.Size(200, 35);
            this.CategoryComboBox.Name = "CategoryComboBox";
            this.CategoryComboBox.SelectedIndex = -1;
            this.CategoryComboBox.SelectedItem = null;
            this.CategoryComboBox.Size = new System.Drawing.Size(310, 35);
            this.CategoryComboBox.TabIndex = 59;
            this.CategoryComboBox.Texts = "เลือกประเภทสินค้า";
            this.CategoryComboBox.SelectedIndexChanged += new System.EventHandler(this.CategoryComboBox_SelectedIndexChanged);
            // 
            // GroupPriceQuantityTextBox
            // 
            this.GroupPriceQuantityTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.GroupPriceQuantityTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.GroupPriceQuantityTextBox.BorderSize = 1;
            this.GroupPriceQuantityTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GroupPriceQuantityTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.GroupPriceQuantityTextBox.Location = new System.Drawing.Point(250, 368);
            this.GroupPriceQuantityTextBox.Multiline = false;
            this.GroupPriceQuantityTextBox.Name = "GroupPriceQuantityTextBox";
            this.GroupPriceQuantityTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.GroupPriceQuantityTextBox.PasswordChar = false;
            this.GroupPriceQuantityTextBox.ReadOnly = false;
            this.GroupPriceQuantityTextBox.Size = new System.Drawing.Size(310, 39);
            this.GroupPriceQuantityTextBox.TabIndex = 58;
            this.GroupPriceQuantityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.GroupPriceQuantityTextBox.Texts = "";
            this.GroupPriceQuantityTextBox.UnderlinedStyle = true;
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label11.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Gainsboro;
            this.label11.Location = new System.Drawing.Point(107, 368);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(137, 39);
            this.label11.TabIndex = 57;
            this.label11.Text = "จำนวนต่อกลุ่ม";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // GroupPriceTextBox
            // 
            this.GroupPriceTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.GroupPriceTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.GroupPriceTextBox.BorderSize = 1;
            this.GroupPriceTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GroupPriceTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.GroupPriceTextBox.Location = new System.Drawing.Point(250, 323);
            this.GroupPriceTextBox.Multiline = false;
            this.GroupPriceTextBox.Name = "GroupPriceTextBox";
            this.GroupPriceTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.GroupPriceTextBox.PasswordChar = false;
            this.GroupPriceTextBox.ReadOnly = false;
            this.GroupPriceTextBox.Size = new System.Drawing.Size(310, 39);
            this.GroupPriceTextBox.TabIndex = 56;
            this.GroupPriceTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.GroupPriceTextBox.Texts = "";
            this.GroupPriceTextBox.UnderlinedStyle = true;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Gainsboro;
            this.label10.Location = new System.Drawing.Point(107, 323);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(137, 39);
            this.label10.TabIndex = 55;
            this.label10.Text = "ราคาขายต่อกลุ่ม";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // BrandTextBox
            // 
            this.BrandTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.BrandTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.BrandTextBox.BorderSize = 1;
            this.BrandTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BrandTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.BrandTextBox.Location = new System.Drawing.Point(250, 458);
            this.BrandTextBox.Multiline = false;
            this.BrandTextBox.Name = "BrandTextBox";
            this.BrandTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.BrandTextBox.PasswordChar = false;
            this.BrandTextBox.ReadOnly = false;
            this.BrandTextBox.Size = new System.Drawing.Size(310, 39);
            this.BrandTextBox.TabIndex = 54;
            this.BrandTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.BrandTextBox.Texts = "";
            this.BrandTextBox.UnderlinedStyle = true;
            // 
            // ManufacturerTextBox
            // 
            this.ManufacturerTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ManufacturerTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.ManufacturerTextBox.BorderSize = 1;
            this.ManufacturerTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ManufacturerTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.ManufacturerTextBox.Location = new System.Drawing.Point(250, 413);
            this.ManufacturerTextBox.Multiline = false;
            this.ManufacturerTextBox.Name = "ManufacturerTextBox";
            this.ManufacturerTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.ManufacturerTextBox.PasswordChar = false;
            this.ManufacturerTextBox.ReadOnly = false;
            this.ManufacturerTextBox.Size = new System.Drawing.Size(310, 39);
            this.ManufacturerTextBox.TabIndex = 53;
            this.ManufacturerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.ManufacturerTextBox.Texts = "";
            this.ManufacturerTextBox.UnderlinedStyle = true;
            // 
            // UnitPriceTextBox
            // 
            this.UnitPriceTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.UnitPriceTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.UnitPriceTextBox.BorderSize = 1;
            this.UnitPriceTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UnitPriceTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.UnitPriceTextBox.Location = new System.Drawing.Point(250, 278);
            this.UnitPriceTextBox.Multiline = false;
            this.UnitPriceTextBox.Name = "UnitPriceTextBox";
            this.UnitPriceTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.UnitPriceTextBox.PasswordChar = false;
            this.UnitPriceTextBox.ReadOnly = false;
            this.UnitPriceTextBox.Size = new System.Drawing.Size(310, 39);
            this.UnitPriceTextBox.TabIndex = 51;
            this.UnitPriceTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.UnitPriceTextBox.Texts = "";
            this.UnitPriceTextBox.UnderlinedStyle = true;
            // 
            // QuantityTextBox
            // 
            this.QuantityTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.QuantityTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.QuantityTextBox.BorderSize = 1;
            this.QuantityTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.QuantityTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.QuantityTextBox.Location = new System.Drawing.Point(250, 233);
            this.QuantityTextBox.Multiline = false;
            this.QuantityTextBox.Name = "QuantityTextBox";
            this.QuantityTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.QuantityTextBox.PasswordChar = false;
            this.QuantityTextBox.ReadOnly = false;
            this.QuantityTextBox.Size = new System.Drawing.Size(310, 39);
            this.QuantityTextBox.TabIndex = 50;
            this.QuantityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.QuantityTextBox.Texts = "1";
            this.QuantityTextBox.UnderlinedStyle = true;
            // 
            // DescriptionTextBox
            // 
            this.DescriptionTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.DescriptionTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.DescriptionTextBox.BorderSize = 1;
            this.DescriptionTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DescriptionTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.DescriptionTextBox.Location = new System.Drawing.Point(250, 140);
            this.DescriptionTextBox.Multiline = false;
            this.DescriptionTextBox.Name = "DescriptionTextBox";
            this.DescriptionTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.DescriptionTextBox.PasswordChar = false;
            this.DescriptionTextBox.ReadOnly = false;
            this.DescriptionTextBox.Size = new System.Drawing.Size(310, 39);
            this.DescriptionTextBox.TabIndex = 49;
            this.DescriptionTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.DescriptionTextBox.Texts = "";
            this.DescriptionTextBox.UnderlinedStyle = true;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.panel2.Controls.Add(this.CancelProductEntryButton);
            this.panel2.Controls.Add(this.SaveProductEntryButton);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 590);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1112, 90);
            this.panel2.TabIndex = 47;
            // 
            // CancelProductEntryButton
            // 
            this.CancelProductEntryButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CancelProductEntryButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CancelProductEntryButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(79)))), ((int)(((byte)(95)))));
            this.CancelProductEntryButton.BorderRadius = 18;
            this.CancelProductEntryButton.BorderSize = 1;
            this.CancelProductEntryButton.FlatAppearance.BorderSize = 0;
            this.CancelProductEntryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CancelProductEntryButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelProductEntryButton.ForeColor = System.Drawing.Color.White;
            this.CancelProductEntryButton.Location = new System.Drawing.Point(905, 18);
            this.CancelProductEntryButton.Name = "CancelProductEntryButton";
            this.CancelProductEntryButton.Size = new System.Drawing.Size(158, 53);
            this.CancelProductEntryButton.TabIndex = 11;
            this.CancelProductEntryButton.Text = "ยกเลิก";
            this.CancelProductEntryButton.TextColor = System.Drawing.Color.White;
            this.CancelProductEntryButton.UseVisualStyleBackColor = false;
            this.CancelProductEntryButton.Click += new System.EventHandler(this.CancelProductEntryButton_Click);
            // 
            // SaveProductEntryButton
            // 
            this.SaveProductEntryButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.SaveProductEntryButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.SaveProductEntryButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(190)))), ((int)(((byte)(166)))));
            this.SaveProductEntryButton.BorderRadius = 19;
            this.SaveProductEntryButton.BorderSize = 1;
            this.SaveProductEntryButton.FlatAppearance.BorderSize = 0;
            this.SaveProductEntryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SaveProductEntryButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaveProductEntryButton.ForeColor = System.Drawing.Color.White;
            this.SaveProductEntryButton.Location = new System.Drawing.Point(60, 18);
            this.SaveProductEntryButton.Name = "SaveProductEntryButton";
            this.SaveProductEntryButton.Size = new System.Drawing.Size(158, 53);
            this.SaveProductEntryButton.TabIndex = 10;
            this.SaveProductEntryButton.Text = "บันทึก";
            this.SaveProductEntryButton.TextColor = System.Drawing.Color.White;
            this.SaveProductEntryButton.UseVisualStyleBackColor = false;
            this.SaveProductEntryButton.Click += new System.EventHandler(this.SaveProductEntryButton_Click);
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label9.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.Gainsboro;
            this.label9.Location = new System.Drawing.Point(107, 458);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(137, 39);
            this.label9.TabIndex = 46;
            this.label9.Text = "ยี่ห้อ";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label8.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.Gainsboro;
            this.label8.Location = new System.Drawing.Point(107, 413);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(137, 39);
            this.label8.TabIndex = 45;
            this.label8.Text = "ผู้ผลิต";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label7.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.Gainsboro;
            this.label7.Location = new System.Drawing.Point(107, 106);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(137, 28);
            this.label7.TabIndex = 44;
            this.label7.Text = "ประเภทสินค้า";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label5.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Gainsboro;
            this.label5.Location = new System.Drawing.Point(107, 278);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(137, 39);
            this.label5.TabIndex = 42;
            this.label5.Text = "ราคาขาย";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Gainsboro;
            this.label4.Location = new System.Drawing.Point(107, 233);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(137, 39);
            this.label4.TabIndex = 41;
            this.label4.Text = "จำนวน";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.label3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Gainsboro;
            this.label3.Location = new System.Drawing.Point(107, 140);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(137, 39);
            this.label3.TabIndex = 40;
            this.label3.Text = "คำอธิบาย";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.DarkSlateGray;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1112, 39);
            this.label1.TabIndex = 38;
            this.label1.Text = "เพิ่มสินค้าใหม่";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BarcodeTextBox
            // 
            this.BarcodeTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.BarcodeTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.BarcodeTextBox.BorderSize = 1;
            this.BarcodeTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BarcodeTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.BarcodeTextBox.Location = new System.Drawing.Point(836, 381);
            this.BarcodeTextBox.Multiline = false;
            this.BarcodeTextBox.Name = "BarcodeTextBox";
            this.BarcodeTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.BarcodeTextBox.PasswordChar = false;
            this.BarcodeTextBox.ReadOnly = true;
            this.BarcodeTextBox.Size = new System.Drawing.Size(227, 39);
            this.BarcodeTextBox.TabIndex = 73;
            this.BarcodeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.BarcodeTextBox.Texts = "";
            this.BarcodeTextBox.UnderlinedStyle = true;
            // 
            // AddNewInventoryProductWithCustomBarcodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SlateGray;
            this.ClientSize = new System.Drawing.Size(1114, 682);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AddNewInventoryProductWithCustomBarcodeForm";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BarcodePictureBox)).EndInit();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

		#endregion

		private System.Windows.Forms.Panel panel1;
		private ModernUI.ModernComboBox CategoryComboBox;
		private ModernUI.ModernTextBox GroupPriceQuantityTextBox;
		private System.Windows.Forms.Label label11;
		private ModernUI.ModernTextBox GroupPriceTextBox;
		private System.Windows.Forms.Label label10;
		private ModernUI.ModernTextBox BrandTextBox;
		private ModernUI.ModernTextBox ManufacturerTextBox;
		private ModernUI.ModernTextBox UnitPriceTextBox;
		private ModernUI.ModernTextBox QuantityTextBox;
		private ModernUI.ModernTextBox DescriptionTextBox;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label1;
        private ModernUI.ModernButton CancelProductEntryButton;
        private ModernUI.ModernButton SaveProductEntryButton;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.PictureBox BarcodePictureBox;
        private System.Windows.Forms.CheckBox IsTrackableCheckBox;
        private ModernUI.ModernTextBox BarcodeTextBox;
    }
}