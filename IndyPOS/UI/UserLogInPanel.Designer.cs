﻿namespace IndyPOS.UI
{
    partial class UserLogInPanel
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
            this.PasswordVisibilityButton = new ModernUI.ModernButton();
            this.UserNameTextBox = new ModernUI.ModernTextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.UserSecretTextBox = new ModernUI.ModernTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.LogInButton = new ModernUI.ModernButton();
            this.UserInputPanel = new System.Windows.Forms.Panel();
            this.UserInputPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // PasswordVisibilityButton
            // 
            this.PasswordVisibilityButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.PasswordVisibilityButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.PasswordVisibilityButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.PasswordVisibilityButton.BorderRadius = 19;
            this.PasswordVisibilityButton.BorderSize = 1;
            this.PasswordVisibilityButton.FlatAppearance.BorderSize = 0;
            this.PasswordVisibilityButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PasswordVisibilityButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PasswordVisibilityButton.ForeColor = System.Drawing.Color.White;
            this.PasswordVisibilityButton.Image = global::IndyPOS.Properties.Resources.Visible_25;
            this.PasswordVisibilityButton.Location = new System.Drawing.Point(441, 63);
            this.PasswordVisibilityButton.Name = "PasswordVisibilityButton";
            this.PasswordVisibilityButton.Size = new System.Drawing.Size(39, 39);
            this.PasswordVisibilityButton.TabIndex = 84;
            this.PasswordVisibilityButton.TextColor = System.Drawing.Color.White;
            this.PasswordVisibilityButton.UseVisualStyleBackColor = false;
            this.PasswordVisibilityButton.Click += new System.EventHandler(this.PasswordVisibilityButton_Click);
            // 
            // UserNameTextBox
            // 
            this.UserNameTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.UserNameTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.UserNameTextBox.BorderSize = 1;
            this.UserNameTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserNameTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.UserNameTextBox.Location = new System.Drawing.Point(163, 18);
            this.UserNameTextBox.Multiline = false;
            this.UserNameTextBox.Name = "UserNameTextBox";
            this.UserNameTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.UserNameTextBox.PasswordChar = false;
            this.UserNameTextBox.ReadOnly = false;
            this.UserNameTextBox.Size = new System.Drawing.Size(272, 39);
            this.UserNameTextBox.TabIndex = 83;
            this.UserNameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.UserNameTextBox.Texts = "";
            this.UserNameTextBox.UnderlinedStyle = true;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.label10.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Gainsboro;
            this.label10.Location = new System.Drawing.Point(20, 18);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(137, 39);
            this.label10.TabIndex = 82;
            this.label10.Text = "Username";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // UserSecretTextBox
            // 
            this.UserSecretTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.UserSecretTextBox.BorderColor = System.Drawing.Color.DimGray;
            this.UserSecretTextBox.BorderSize = 1;
            this.UserSecretTextBox.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UserSecretTextBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.UserSecretTextBox.Location = new System.Drawing.Point(163, 63);
            this.UserSecretTextBox.Multiline = false;
            this.UserSecretTextBox.Name = "UserSecretTextBox";
            this.UserSecretTextBox.Padding = new System.Windows.Forms.Padding(7);
            this.UserSecretTextBox.PasswordChar = true;
            this.UserSecretTextBox.ReadOnly = false;
            this.UserSecretTextBox.Size = new System.Drawing.Size(272, 39);
            this.UserSecretTextBox.TabIndex = 81;
            this.UserSecretTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.UserSecretTextBox.Texts = "";
            this.UserSecretTextBox.UnderlinedStyle = true;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.label2.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(20, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(137, 39);
            this.label2.TabIndex = 80;
            this.label2.Text = "Password";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LogInButton
            // 
            this.LogInButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LogInButton.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.LogInButton.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(190)))), ((int)(((byte)(166)))));
            this.LogInButton.BorderRadius = 19;
            this.LogInButton.BorderSize = 1;
            this.LogInButton.FlatAppearance.BorderSize = 0;
            this.LogInButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LogInButton.Font = new System.Drawing.Font("FC Subject [Non-commercial] Reg", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogInButton.ForeColor = System.Drawing.Color.White;
            this.LogInButton.Location = new System.Drawing.Point(578, 362);
            this.LogInButton.Name = "LogInButton";
            this.LogInButton.Size = new System.Drawing.Size(174, 53);
            this.LogInButton.TabIndex = 79;
            this.LogInButton.Text = "Login";
            this.LogInButton.TextColor = System.Drawing.Color.White;
            this.LogInButton.UseVisualStyleBackColor = false;
            this.LogInButton.Click += new System.EventHandler(this.LogInButton_Click);
            // 
            // UserInputPanel
            // 
            this.UserInputPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.UserInputPanel.Controls.Add(this.label10);
            this.UserInputPanel.Controls.Add(this.PasswordVisibilityButton);
            this.UserInputPanel.Controls.Add(this.label2);
            this.UserInputPanel.Controls.Add(this.UserNameTextBox);
            this.UserInputPanel.Controls.Add(this.UserSecretTextBox);
            this.UserInputPanel.Location = new System.Drawing.Point(415, 218);
            this.UserInputPanel.Name = "UserInputPanel";
            this.UserInputPanel.Size = new System.Drawing.Size(500, 138);
            this.UserInputPanel.TabIndex = 85;
            // 
            // UserLogInPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.Controls.Add(this.UserInputPanel);
            this.Controls.Add(this.LogInButton);
            this.Name = "UserLogInPanel";
            this.Size = new System.Drawing.Size(1421, 697);
            this.VisibleChanged += new System.EventHandler(this.UserLogInPanel_VisibleChanged);
            this.UserInputPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private ModernUI.ModernButton PasswordVisibilityButton;
        private ModernUI.ModernTextBox UserNameTextBox;
        private System.Windows.Forms.Label label10;
        private ModernUI.ModernTextBox UserSecretTextBox;
        private System.Windows.Forms.Label label2;
        private ModernUI.ModernButton LogInButton;
        private System.Windows.Forms.Panel UserInputPanel;
    }
}
