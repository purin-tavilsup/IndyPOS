
namespace IndyPOS.UI
{
	partial class PrintReceiptForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintReceiptForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.CloseFormButton = new ModernUI.ModernButton();
            this.PrintReceiptButton = new ModernUI.ModernButton();
            this.CaptionLabel = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.CloseFormButton);
            this.panel1.Controls.Add(this.PrintReceiptButton);
            this.panel1.Controls.Add(this.CaptionLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(640, 223);
            this.panel1.TabIndex = 0;
            // 
            // CloseFormButton
            // 
            this.CloseFormButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CloseFormButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.CloseFormButton.BorderColor = System.Drawing.Color.SteelBlue;
            this.CloseFormButton.BorderRadius = 18;
            this.CloseFormButton.BorderSize = 1;
            this.CloseFormButton.FlatAppearance.BorderSize = 0;
            this.CloseFormButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CloseFormButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CloseFormButton.ForeColor = System.Drawing.Color.White;
            this.CloseFormButton.Location = new System.Drawing.Point(392, 70);
            this.CloseFormButton.Name = "CloseFormButton";
            this.CloseFormButton.Size = new System.Drawing.Size(235, 140);
            this.CloseFormButton.TabIndex = 52;
            this.CloseFormButton.Text = "ปิด";
            this.CloseFormButton.TextColor = System.Drawing.Color.White;
            this.CloseFormButton.UseVisualStyleBackColor = false;
            this.CloseFormButton.Click += new System.EventHandler(this.CloseFormButton_Click);
            // 
            // PrintReceiptButton
            // 
            this.PrintReceiptButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PrintReceiptButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PrintReceiptButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(190)))), ((int)(((byte)(166)))));
            this.PrintReceiptButton.BorderRadius = 19;
            this.PrintReceiptButton.BorderSize = 1;
            this.PrintReceiptButton.FlatAppearance.BorderSize = 0;
            this.PrintReceiptButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PrintReceiptButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PrintReceiptButton.ForeColor = System.Drawing.Color.White;
            this.PrintReceiptButton.Image = global::IndyPOS.Properties.Resources.Receipt_80;
            this.PrintReceiptButton.Location = new System.Drawing.Point(11, 70);
            this.PrintReceiptButton.Name = "PrintReceiptButton";
            this.PrintReceiptButton.Size = new System.Drawing.Size(235, 140);
            this.PrintReceiptButton.TabIndex = 51;
            this.PrintReceiptButton.TextColor = System.Drawing.Color.White;
            this.PrintReceiptButton.UseVisualStyleBackColor = false;
            this.PrintReceiptButton.Click += new System.EventHandler(this.PrintReceiptButton_Click);
            // 
            // CaptionLabel
            // 
            this.CaptionLabel.BackColor = System.Drawing.Color.DarkSlateGray;
            this.CaptionLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.CaptionLabel.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CaptionLabel.ForeColor = System.Drawing.Color.Gainsboro;
            this.CaptionLabel.Location = new System.Drawing.Point(0, 0);
            this.CaptionLabel.Margin = new System.Windows.Forms.Padding(0);
            this.CaptionLabel.Name = "CaptionLabel";
            this.CaptionLabel.Size = new System.Drawing.Size(638, 56);
            this.CaptionLabel.TabIndex = 49;
            this.CaptionLabel.Text = "บันทึกการขายสำเร็จ";
            this.CaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PrintReceiptForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.ClientSize = new System.Drawing.Size(640, 223);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintReceiptForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MessageForm";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label CaptionLabel;
        private ModernUI.ModernButton PrintReceiptButton;
        private ModernUI.ModernButton CloseFormButton;
    }
}