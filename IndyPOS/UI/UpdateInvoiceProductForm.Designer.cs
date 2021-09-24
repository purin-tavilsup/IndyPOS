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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.QuantityTextBox = new System.Windows.Forms.TextBox();
			this.panel7 = new System.Windows.Forms.Panel();
			this.CancelUpdateProductButton = new System.Windows.Forms.Button();
			this.panel6 = new System.Windows.Forms.Panel();
			this.UpdateProductButton = new System.Windows.Forms.Button();
			this.panel8 = new System.Windows.Forms.Panel();
			this.RemoveProductButton = new System.Windows.Forms.Button();
			this.ProductCodeLabel = new System.Windows.Forms.Label();
			this.DescriptionLabel = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel7.SuspendLayout();
			this.panel6.SuspendLayout();
			this.panel8.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(468, 39);
			this.label1.TabIndex = 4;
			this.label1.Text = "อัพเดทสินค้า";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label2
			// 
			this.label2.BackColor = System.Drawing.Color.LightGray;
			this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(16, 52);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(137, 27);
			this.label2.TabIndex = 5;
			this.label2.Text = "รหัสสินค้า";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.Color.LightGray;
			this.label3.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(16, 84);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(137, 27);
			this.label3.TabIndex = 6;
			this.label3.Text = "คำอธิบาย";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.LightGray;
			this.label4.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(16, 116);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(137, 27);
			this.label4.TabIndex = 7;
			this.label4.Text = "จำนวน";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// QuantityTextBox
			// 
			this.QuantityTextBox.BackColor = System.Drawing.Color.WhiteSmoke;
			this.QuantityTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.QuantityTextBox.Location = new System.Drawing.Point(159, 116);
			this.QuantityTextBox.Name = "QuantityTextBox";
			this.QuantityTextBox.Size = new System.Drawing.Size(293, 25);
			this.QuantityTextBox.TabIndex = 15;
			this.QuantityTextBox.Text = "1";
			this.QuantityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// panel7
			// 
			this.panel7.BackColor = System.Drawing.Color.Silver;
			this.panel7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel7.Controls.Add(this.CancelUpdateProductButton);
			this.panel7.Location = new System.Drawing.Point(167, 12);
			this.panel7.Name = "panel7";
			this.panel7.Size = new System.Drawing.Size(133, 103);
			this.panel7.TabIndex = 22;
			// 
			// CancelUpdateProductButton
			// 
			this.CancelUpdateProductButton.BackColor = System.Drawing.Color.Gainsboro;
			this.CancelUpdateProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CancelUpdateProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CancelUpdateProductButton.Image = global::IndyPOS.Properties.Resources.Cross_50;
			this.CancelUpdateProductButton.Location = new System.Drawing.Point(3, 3);
			this.CancelUpdateProductButton.Name = "CancelUpdateProductButton";
			this.CancelUpdateProductButton.Size = new System.Drawing.Size(125, 95);
			this.CancelUpdateProductButton.TabIndex = 7;
			this.CancelUpdateProductButton.Text = "ยกเลิก";
			this.CancelUpdateProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.CancelUpdateProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.CancelUpdateProductButton.UseVisualStyleBackColor = false;
			this.CancelUpdateProductButton.Click += new System.EventHandler(this.CancelUpdateProductButton_Click);
			// 
			// panel6
			// 
			this.panel6.BackColor = System.Drawing.Color.Silver;
			this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel6.Controls.Add(this.UpdateProductButton);
			this.panel6.Location = new System.Drawing.Point(16, 12);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(133, 103);
			this.panel6.TabIndex = 21;
			// 
			// UpdateProductButton
			// 
			this.UpdateProductButton.BackColor = System.Drawing.Color.Gainsboro;
			this.UpdateProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.UpdateProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.UpdateProductButton.Image = global::IndyPOS.Properties.Resources.Check_50;
			this.UpdateProductButton.Location = new System.Drawing.Point(3, 3);
			this.UpdateProductButton.Name = "UpdateProductButton";
			this.UpdateProductButton.Size = new System.Drawing.Size(125, 95);
			this.UpdateProductButton.TabIndex = 7;
			this.UpdateProductButton.Text = "อัพเดทสินค้า";
			this.UpdateProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.UpdateProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.UpdateProductButton.UseVisualStyleBackColor = false;
			this.UpdateProductButton.Click += new System.EventHandler(this.UpdateProductButton_Click);
			// 
			// panel8
			// 
			this.panel8.BackColor = System.Drawing.Color.Silver;
			this.panel8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel8.Controls.Add(this.RemoveProductButton);
			this.panel8.Location = new System.Drawing.Point(319, 12);
			this.panel8.Name = "panel8";
			this.panel8.Size = new System.Drawing.Size(133, 103);
			this.panel8.TabIndex = 23;
			// 
			// RemoveProductButton
			// 
			this.RemoveProductButton.BackColor = System.Drawing.Color.Gainsboro;
			this.RemoveProductButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.RemoveProductButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RemoveProductButton.Image = global::IndyPOS.Properties.Resources.Trash_50;
			this.RemoveProductButton.Location = new System.Drawing.Point(3, 4);
			this.RemoveProductButton.Name = "RemoveProductButton";
			this.RemoveProductButton.Size = new System.Drawing.Size(125, 95);
			this.RemoveProductButton.TabIndex = 7;
			this.RemoveProductButton.Text = "ลบสินค้า";
			this.RemoveProductButton.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.RemoveProductButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.RemoveProductButton.UseVisualStyleBackColor = false;
			this.RemoveProductButton.Click += new System.EventHandler(this.RemoveProductButton_Click);
			// 
			// ProductCodeLabel
			// 
			this.ProductCodeLabel.BackColor = System.Drawing.Color.WhiteSmoke;
			this.ProductCodeLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ProductCodeLabel.Location = new System.Drawing.Point(159, 52);
			this.ProductCodeLabel.Name = "ProductCodeLabel";
			this.ProductCodeLabel.Size = new System.Drawing.Size(293, 27);
			this.ProductCodeLabel.TabIndex = 24;
			this.ProductCodeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// DescriptionLabel
			// 
			this.DescriptionLabel.BackColor = System.Drawing.Color.WhiteSmoke;
			this.DescriptionLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.DescriptionLabel.Location = new System.Drawing.Point(159, 84);
			this.DescriptionLabel.Name = "DescriptionLabel";
			this.DescriptionLabel.Size = new System.Drawing.Size(293, 27);
			this.DescriptionLabel.TabIndex = 25;
			this.DescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.panel1.Controls.Add(this.panel8);
			this.panel1.Controls.Add(this.panel6);
			this.panel1.Controls.Add(this.panel7);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 158);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(468, 128);
			this.panel1.TabIndex = 26;
			// 
			// UpdateInvoiceProductForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Gray;
			this.ClientSize = new System.Drawing.Size(468, 286);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.DescriptionLabel);
			this.Controls.Add(this.ProductCodeLabel);
			this.Controls.Add(this.QuantityTextBox);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "UpdateInvoiceProductForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.panel7.ResumeLayout(false);
			this.panel6.ResumeLayout(false);
			this.panel8.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox QuantityTextBox;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Button CancelUpdateProductButton;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Button UpdateProductButton;
		private System.Windows.Forms.Panel panel8;
		private System.Windows.Forms.Button RemoveProductButton;
		private System.Windows.Forms.Label ProductCodeLabel;
		private System.Windows.Forms.Label DescriptionLabel;
		private System.Windows.Forms.Panel panel1;
	}
}