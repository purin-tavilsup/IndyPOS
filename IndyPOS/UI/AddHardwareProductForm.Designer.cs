namespace IndyPOS.UI
{
    partial class AddHardwareProductForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddHardwareProductForm));
            this.CancelProductEntryButton = new ModernUI.ModernButton();
            this.label1 = new System.Windows.Forms.Label();
            this.modernButton1 = new ModernUI.ModernButton();
            this.SuspendLayout();
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
            this.CancelProductEntryButton.Location = new System.Drawing.Point(1610, 883);
            this.CancelProductEntryButton.Name = "CancelProductEntryButton";
            this.CancelProductEntryButton.Size = new System.Drawing.Size(158, 53);
            this.CancelProductEntryButton.TabIndex = 13;
            this.CancelProductEntryButton.Text = "ยกเลิก";
            this.CancelProductEntryButton.TextColor = System.Drawing.Color.White;
            this.CancelProductEntryButton.UseVisualStyleBackColor = false;
            this.CancelProductEntryButton.Click += new System.EventHandler(this.CancelProductEntryButton_Click);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.DarkSlateGray;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1800, 39);
            this.label1.TabIndex = 39;
            this.label1.Text = "เพิ่มสินค้า ฮาร์ดแวร์";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // modernButton1
            // 
            this.modernButton1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.modernButton1.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.modernButton1.BorderColor = System.Drawing.Color.SteelBlue;
            this.modernButton1.BorderRadius = 5;
            this.modernButton1.BorderSize = 1;
            this.modernButton1.FlatAppearance.BorderSize = 0;
            this.modernButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.modernButton1.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.modernButton1.ForeColor = System.Drawing.Color.White;
            this.modernButton1.Location = new System.Drawing.Point(12, 54);
            this.modernButton1.Name = "modernButton1";
            this.modernButton1.Size = new System.Drawing.Size(320, 76);
            this.modernButton1.TabIndex = 86;
            this.modernButton1.Text = "สินค้าไม่มีบาร์โค้ด";
            this.modernButton1.TextColor = System.Drawing.Color.White;
            this.modernButton1.UseVisualStyleBackColor = false;
            this.modernButton1.Click += new System.EventHandler(this.AddNonTrackableProductButton_Click);
            // 
            // AddHardwareProductForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(1800, 970);
            this.Controls.Add(this.modernButton1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CancelProductEntryButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AddHardwareProductForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AddInvoiceProductForm";
            this.ResumeLayout(false);

        }

        #endregion

        private ModernUI.ModernButton CancelProductEntryButton;
        private System.Windows.Forms.Label label1;
        private ModernUI.ModernButton modernButton1;
    }
}