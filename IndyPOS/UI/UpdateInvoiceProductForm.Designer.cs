namespace IndyPOS.UI
{
    partial class UpdateInvoiceProductForm
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
			this.ProductCodeLabel = new System.Windows.Forms.Label();
			this.DescriptionLabel = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.RemoveProductButton = new System.Windows.Forms.Button();
			this.CancelUpdateProductButton = new System.Windows.Forms.Button();
			this.UpdateProductButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.DecreaseQuantityPicBox = new System.Windows.Forms.PictureBox();
			this.IncreaseQuantityPicBox = new System.Windows.Forms.PictureBox();
			this.QuantityTextBox = new ModernUI.ModernTextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.panel2.SuspendLayout();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DecreaseQuantityPicBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.IncreaseQuantityPicBox)).BeginInit();
			this.SuspendLayout();
			// 
			// ProductCodeLabel
			// 
			this.ProductCodeLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.ProductCodeLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ProductCodeLabel.ForeColor = System.Drawing.Color.Gainsboro;
			this.ProductCodeLabel.Location = new System.Drawing.Point(339, 65);
			this.ProductCodeLabel.Name = "ProductCodeLabel";
			this.ProductCodeLabel.Size = new System.Drawing.Size(310, 39);
			this.ProductCodeLabel.TabIndex = 24;
			this.ProductCodeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// DescriptionLabel
			// 
			this.DescriptionLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.DescriptionLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.DescriptionLabel.ForeColor = System.Drawing.Color.Gainsboro;
			this.DescriptionLabel.Location = new System.Drawing.Point(339, 107);
			this.DescriptionLabel.Name = "DescriptionLabel";
			this.DescriptionLabel.Size = new System.Drawing.Size(310, 39);
			this.DescriptionLabel.TabIndex = 25;
			this.DescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.panel2.Controls.Add(this.panel1);
			this.panel2.Controls.Add(this.label1);
			this.panel2.Controls.Add(this.DecreaseQuantityPicBox);
			this.panel2.Controls.Add(this.IncreaseQuantityPicBox);
			this.panel2.Controls.Add(this.QuantityTextBox);
			this.panel2.Controls.Add(this.label4);
			this.panel2.Controls.Add(this.label3);
			this.panel2.Controls.Add(this.label2);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(1, 1);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(658, 358);
			this.panel2.TabIndex = 30;
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
			this.panel1.Controls.Add(this.RemoveProductButton);
			this.panel1.Controls.Add(this.CancelUpdateProductButton);
			this.panel1.Controls.Add(this.UpdateProductButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 230);
			this.panel1.MinimumSize = new System.Drawing.Size(39, 39);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(658, 128);
			this.panel1.TabIndex = 34;
			// 
			// RemoveProductButton
			// 
			this.RemoveProductButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.RemoveProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.RemoveProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RemoveProductButton.ForeColor = System.Drawing.Color.Gainsboro;
			this.RemoveProductButton.Image = global::IndyPOS.Properties.Resources.Trash_50;
			this.RemoveProductButton.Location = new System.Drawing.Point(513, 16);
			this.RemoveProductButton.Name = "RemoveProductButton";
			this.RemoveProductButton.Size = new System.Drawing.Size(125, 95);
			this.RemoveProductButton.TabIndex = 7;
			this.RemoveProductButton.Text = "ลบสินค้า";
			this.RemoveProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.RemoveProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.RemoveProductButton.UseVisualStyleBackColor = false;
			this.RemoveProductButton.Click += new System.EventHandler(this.RemoveProductButton_Click);
			// 
			// CancelUpdateProductButton
			// 
			this.CancelUpdateProductButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.CancelUpdateProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CancelUpdateProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CancelUpdateProductButton.ForeColor = System.Drawing.Color.Gainsboro;
			this.CancelUpdateProductButton.Image = global::IndyPOS.Properties.Resources.Cross_50;
			this.CancelUpdateProductButton.Location = new System.Drawing.Point(359, 16);
			this.CancelUpdateProductButton.Name = "CancelUpdateProductButton";
			this.CancelUpdateProductButton.Size = new System.Drawing.Size(125, 95);
			this.CancelUpdateProductButton.TabIndex = 7;
			this.CancelUpdateProductButton.Text = "ยกเลิก";
			this.CancelUpdateProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.CancelUpdateProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.CancelUpdateProductButton.UseVisualStyleBackColor = false;
			this.CancelUpdateProductButton.Click += new System.EventHandler(this.CancelUpdateProductButton_Click);
			// 
			// UpdateProductButton
			// 
			this.UpdateProductButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.UpdateProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.UpdateProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.UpdateProductButton.ForeColor = System.Drawing.Color.Gainsboro;
			this.UpdateProductButton.Image = global::IndyPOS.Properties.Resources.Check_50;
			this.UpdateProductButton.Location = new System.Drawing.Point(16, 16);
			this.UpdateProductButton.Name = "UpdateProductButton";
			this.UpdateProductButton.Size = new System.Drawing.Size(125, 95);
			this.UpdateProductButton.TabIndex = 7;
			this.UpdateProductButton.Text = "อัพเดทสินค้า";
			this.UpdateProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.UpdateProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.UpdateProductButton.UseVisualStyleBackColor = false;
			this.UpdateProductButton.Click += new System.EventHandler(this.UpdateProductButton_Click);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(658, 39);
			this.label1.TabIndex = 30;
			this.label1.Text = "อัพเดทสินค้า";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// DecreaseQuantityPicBox
			// 
			this.DecreaseQuantityPicBox.Image = global::IndyPOS.Properties.Resources.Minus_35;
			this.DecreaseQuantityPicBox.Location = new System.Drawing.Point(500, 147);
			this.DecreaseQuantityPicBox.MaximumSize = new System.Drawing.Size(39, 39);
			this.DecreaseQuantityPicBox.MinimumSize = new System.Drawing.Size(39, 39);
			this.DecreaseQuantityPicBox.Name = "DecreaseQuantityPicBox";
			this.DecreaseQuantityPicBox.Size = new System.Drawing.Size(39, 39);
			this.DecreaseQuantityPicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.DecreaseQuantityPicBox.TabIndex = 37;
			this.DecreaseQuantityPicBox.TabStop = false;
			this.DecreaseQuantityPicBox.Click += new System.EventHandler(this.DecreaseQuantityPicBox_Click);
			// 
			// IncreaseQuantityPicBox
			// 
			this.IncreaseQuantityPicBox.Image = global::IndyPOS.Properties.Resources.Plus_35;
			this.IncreaseQuantityPicBox.Location = new System.Drawing.Point(233, 148);
			this.IncreaseQuantityPicBox.MaximumSize = new System.Drawing.Size(39, 39);
			this.IncreaseQuantityPicBox.MinimumSize = new System.Drawing.Size(39, 39);
			this.IncreaseQuantityPicBox.Name = "IncreaseQuantityPicBox";
			this.IncreaseQuantityPicBox.Size = new System.Drawing.Size(39, 39);
			this.IncreaseQuantityPicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.IncreaseQuantityPicBox.TabIndex = 36;
			this.IncreaseQuantityPicBox.TabStop = false;
			this.IncreaseQuantityPicBox.Click += new System.EventHandler(this.IncreaseQuantityPicBox_Click);
			// 
			// QuantityTextBox
			// 
			this.QuantityTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.QuantityTextBox.BorderColor = System.Drawing.Color.DimGray;
			this.QuantityTextBox.BorderSize = 1;
			this.QuantityTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.QuantityTextBox.ForeColor = System.Drawing.Color.Gainsboro;
			this.QuantityTextBox.Location = new System.Drawing.Point(278, 148);
			this.QuantityTextBox.Multiline = false;
			this.QuantityTextBox.Name = "QuantityTextBox";
			this.QuantityTextBox.Padding = new System.Windows.Forms.Padding(7);
			this.QuantityTextBox.PasswordChar = false;
			this.QuantityTextBox.ReadOnly = false;
			this.QuantityTextBox.Size = new System.Drawing.Size(216, 39);
			this.QuantityTextBox.TabIndex = 35;
			this.QuantityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.QuantityTextBox.Texts = "";
			this.QuantityTextBox.UnderlinedStyle = true;
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.Gainsboro;
			this.label4.Location = new System.Drawing.Point(106, 148);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(117, 39);
			this.label4.TabIndex = 33;
			this.label4.Text = "จำนวน";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.label3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.ForeColor = System.Drawing.Color.Gainsboro;
			this.label3.Location = new System.Drawing.Point(106, 105);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(117, 39);
			this.label3.TabIndex = 32;
			this.label3.Text = "คำอธิบาย";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label2
			// 
			this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.ForeColor = System.Drawing.Color.Gainsboro;
			this.label2.Location = new System.Drawing.Point(106, 63);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(117, 39);
			this.label2.TabIndex = 31;
			this.label2.Text = "รหัสสินค้า";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// UpdateInvoiceProductForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.SlateGray;
			this.ClientSize = new System.Drawing.Size(660, 360);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.DescriptionLabel);
			this.Controls.Add(this.ProductCodeLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "UpdateInvoiceProductForm";
			this.Padding = new System.Windows.Forms.Padding(1);
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.panel2.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.DecreaseQuantityPicBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.IncreaseQuantityPicBox)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion
		private System.Windows.Forms.Label ProductCodeLabel;
		private System.Windows.Forms.Label DescriptionLabel;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button RemoveProductButton;
		private System.Windows.Forms.Button CancelUpdateProductButton;
		private System.Windows.Forms.Button UpdateProductButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.PictureBox DecreaseQuantityPicBox;
		private System.Windows.Forms.PictureBox IncreaseQuantityPicBox;
		private ModernUI.ModernTextBox QuantityTextBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
	}
}