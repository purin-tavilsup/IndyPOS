
namespace IndyPOS.UI
{
	partial class MessageForm
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
			this.panel1 = new System.Windows.Forms.Panel();
			this.CancelButton = new System.Windows.Forms.Button();
			this.AcceptButton = new System.Windows.Forms.Button();
			this.MessageTextBox = new ModernUI.ModernTextBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.CancelButton);
			this.panel1.Controls.Add(this.AcceptButton);
			this.panel1.Controls.Add(this.MessageTextBox);
			this.panel1.Controls.Add(this.pictureBox1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(612, 223);
			this.panel1.TabIndex = 0;
			// 
			// CancelButton
			// 
			this.CancelButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.CancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CancelButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CancelButton.ForeColor = System.Drawing.Color.DarkGray;
			this.CancelButton.Image = global::IndyPOS.Properties.Resources.Cross_35;
			this.CancelButton.Location = new System.Drawing.Point(442, 150);
			this.CancelButton.Name = "CancelButton";
			this.CancelButton.Size = new System.Drawing.Size(160, 60);
			this.CancelButton.TabIndex = 13;
			this.CancelButton.Text = "  ยกเลิก";
			this.CancelButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.CancelButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.CancelButton.UseVisualStyleBackColor = false;
			this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
			// 
			// AcceptButton
			// 
			this.AcceptButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.AcceptButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.AcceptButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AcceptButton.ForeColor = System.Drawing.Color.DarkGray;
			this.AcceptButton.Image = global::IndyPOS.Properties.Resources.Check_35;
			this.AcceptButton.Location = new System.Drawing.Point(442, 12);
			this.AcceptButton.Name = "AcceptButton";
			this.AcceptButton.Size = new System.Drawing.Size(160, 60);
			this.AcceptButton.TabIndex = 12;
			this.AcceptButton.Text = "  ตกลง";
			this.AcceptButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.AcceptButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.AcceptButton.UseVisualStyleBackColor = false;
			this.AcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
			// 
			// MessageTextBox
			// 
			this.MessageTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.MessageTextBox.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.MessageTextBox.BorderSize = 1;
			this.MessageTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MessageTextBox.ForeColor = System.Drawing.Color.Gainsboro;
			this.MessageTextBox.Location = new System.Drawing.Point(106, 12);
			this.MessageTextBox.Multiline = true;
			this.MessageTextBox.Name = "MessageTextBox";
			this.MessageTextBox.Padding = new System.Windows.Forms.Padding(7);
			this.MessageTextBox.PasswordChar = false;
			this.MessageTextBox.ReadOnly = true;
			this.MessageTextBox.Size = new System.Drawing.Size(330, 198);
			this.MessageTextBox.TabIndex = 11;
			this.MessageTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
			this.MessageTextBox.Texts = "Message";
			this.MessageTextBox.UnderlinedStyle = false;
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::IndyPOS.Properties.Resources.Attention_48;
			this.pictureBox1.Location = new System.Drawing.Point(26, 22);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(50, 50);
			this.pictureBox1.TabIndex = 10;
			this.pictureBox1.TabStop = false;
			// 
			// MessageForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
			this.ClientSize = new System.Drawing.Size(612, 223);
			this.Controls.Add(this.panel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MessageForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "MessageForm";
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button CancelButton;
		private System.Windows.Forms.Button AcceptButton;
		private ModernUI.ModernTextBox MessageTextBox;
		private System.Windows.Forms.PictureBox pictureBox1;
	}
}