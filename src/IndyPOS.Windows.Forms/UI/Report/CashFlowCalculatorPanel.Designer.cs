using IndyPOS.Windows.Forms.Properties;

namespace IndyPOS.Windows.Forms.UI.Report
{
    partial class CashFlowCalculatorPanel
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
            ChangesListView = new ListView();
            PayoutsListView = new ListView();
            ReceivedPayLaterPaymentsListView = new ListView();
            DateLabel = new Label();
            ResetCountingButton = new ModernUI.ModernButton();
            DiffCashPanel = new Panel();
            DiffCashLabel = new Label();
            DiffCashDescriptionLabel = new Label();
            label37 = new Label();
            panel10 = new Panel();
            CalculatedCashTotalLabel = new Label();
            label32 = new Label();
            panel9 = new Panel();
            ActualCashTotalLabel = new Label();
            label26 = new Label();
            panel7 = new Panel();
            Coin1CountTextBox = new ModernUI.ModernTextBox();
            Coin2CountTextBox = new ModernUI.ModernTextBox();
            Coin5CountTextBox = new ModernUI.ModernTextBox();
            Coin10CountTextBox = new ModernUI.ModernTextBox();
            label24 = new Label();
            Coin1CountTotalLabel = new Label();
            label15 = new Label();
            Coin2CountTotalLabel = new Label();
            label17 = new Label();
            Coin5CountTotalLabel = new Label();
            label19 = new Label();
            Coin10CountTotalLabel = new Label();
            label23 = new Label();
            label25 = new Label();
            label36 = new Label();
            label38 = new Label();
            panel1 = new Panel();
            BankNote20CountTextBox = new ModernUI.ModernTextBox();
            BankNote50CountTextBox = new ModernUI.ModernTextBox();
            BankNote100CountTextBox = new ModernUI.ModernTextBox();
            BankNote500CountTextBox = new ModernUI.ModernTextBox();
            BankNote1000CountTextBox = new ModernUI.ModernTextBox();
            label9 = new Label();
            label16 = new Label();
            label18 = new Label();
            label20 = new Label();
            label39 = new Label();
            label40 = new Label();
            label41 = new Label();
            label42 = new Label();
            label43 = new Label();
            BankNote1000CountTotalLabel = new Label();
            BankNote500CountTotalLabel = new Label();
            BankNote100CountTotalLabel = new Label();
            BankNote50CountTotalLabel = new Label();
            BankNote20CountTotalLabel = new Label();
            label49 = new Label();
            SaveToFileButton = new ModernUI.ModernButton();
            PullLatestDataFromDatabaseButton = new ModernUI.ModernButton();
            panel2 = new Panel();
            panel3 = new Panel();
            RemoveReceivedPayLaterPaymentButton = new ModernUI.ModernButton();
            AddReceivedPayLaterPaymentButton = new ModernUI.ModernButton();
            ReceivedPayLaterAmountTextBox = new ModernUI.ModernTextBox();
            PayLaterAccountNameTextBox = new ModernUI.ModernTextBox();
            label10 = new Label();
            label8 = new Label();
            label5 = new Label();
            ReceivedPayLaterPaymentsTotalLabel = new Label();
            label3 = new Label();
            panel4 = new Panel();
            RemovePayOutButton = new ModernUI.ModernButton();
            AddPayOutButton = new ModernUI.ModernButton();
            PayOutAmountTextBox = new ModernUI.ModernTextBox();
            PayOutItemTextBox = new ModernUI.ModernTextBox();
            label6 = new Label();
            label7 = new Label();
            label11 = new Label();
            PayoutsTotalLabel = new Label();
            label13 = new Label();
            label44 = new Label();
            panel5 = new Panel();
            RemoveChangeButton = new ModernUI.ModernButton();
            AddChangeButton = new ModernUI.ModernButton();
            ChangeAmountTextBox = new ModernUI.ModernTextBox();
            label4 = new Label();
            label45 = new Label();
            ChangesTotalLabel = new Label();
            label47 = new Label();
            panel6 = new Panel();
            GeneralGoodsSaleLabel = new Label();
            label14 = new Label();
            panel8 = new Panel();
            HardwareSaleLabel = new Label();
            label28 = new Label();
            panel11 = new Panel();
            CashPaymentLabel = new Label();
            label30 = new Label();
            panel12 = new Panel();
            MoneyTransferPaymentLabel = new Label();
            label34 = new Label();
            panel13 = new Panel();
            WelfareCardPaymentLabel = new Label();
            label46 = new Label();
            panel14 = new Panel();
            PayLaterForGeneralProductsLabel = new Label();
            label2 = new Label();
            panel15 = new Panel();
            PayLaterForHardwareProductsLabel = new Label();
            label21 = new Label();
            panel16 = new Panel();
            label22 = new Label();
            TotalPayLaterLabel = new Label();
            label29 = new Label();
            PayLaterPaymentsListView = new ListView();
            DiffCashPanel.SuspendLayout();
            panel10.SuspendLayout();
            panel9.SuspendLayout();
            panel7.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            panel5.SuspendLayout();
            panel6.SuspendLayout();
            panel8.SuspendLayout();
            panel11.SuspendLayout();
            panel12.SuspendLayout();
            panel13.SuspendLayout();
            panel14.SuspendLayout();
            panel15.SuspendLayout();
            panel16.SuspendLayout();
            SuspendLayout();
            // 
            // ChangesListView
            // 
            ChangesListView.BackColor = Color.FromArgb(38, 38, 38);
            ChangesListView.Font = new Font("FC Subject [Non-commercial] Reg", 9.749999F);
            ChangesListView.ForeColor = Color.Gainsboro;
            ChangesListView.LabelEdit = true;
            ChangesListView.Location = new Point(26, 122);
            ChangesListView.MultiSelect = false;
            ChangesListView.Name = "ChangesListView";
            ChangesListView.Size = new Size(275, 178);
            ChangesListView.TabIndex = 3;
            ChangesListView.UseCompatibleStateImageBehavior = false;
            ChangesListView.View = View.Details;
            ChangesListView.Click += ChangesListView_Click;
            // 
            // PayoutsListView
            // 
            PayoutsListView.BackColor = Color.FromArgb(38, 38, 38);
            PayoutsListView.Font = new Font("FC Subject [Non-commercial] Reg", 9.749999F);
            PayoutsListView.ForeColor = Color.Gainsboro;
            PayoutsListView.Location = new Point(26, 156);
            PayoutsListView.MultiSelect = false;
            PayoutsListView.Name = "PayoutsListView";
            PayoutsListView.Size = new Size(299, 279);
            PayoutsListView.TabIndex = 3;
            PayoutsListView.UseCompatibleStateImageBehavior = false;
            PayoutsListView.Click += PayOutListView_Click;
            // 
            // ReceivedPayLaterPaymentsListView
            // 
            ReceivedPayLaterPaymentsListView.BackColor = Color.FromArgb(38, 38, 38);
            ReceivedPayLaterPaymentsListView.Font = new Font("FC Subject [Non-commercial] Reg", 9.749999F);
            ReceivedPayLaterPaymentsListView.ForeColor = Color.Gainsboro;
            ReceivedPayLaterPaymentsListView.Location = new Point(26, 156);
            ReceivedPayLaterPaymentsListView.MultiSelect = false;
            ReceivedPayLaterPaymentsListView.Name = "ReceivedPayLaterPaymentsListView";
            ReceivedPayLaterPaymentsListView.Size = new Size(275, 221);
            ReceivedPayLaterPaymentsListView.TabIndex = 3;
            ReceivedPayLaterPaymentsListView.UseCompatibleStateImageBehavior = false;
            ReceivedPayLaterPaymentsListView.Click += ReceivedPayLaterPaymentsListView_Click;
            // 
            // DateLabel
            // 
            DateLabel.BackColor = Color.FromArgb(38, 38, 38);
            DateLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            DateLabel.ForeColor = Color.Gainsboro;
            DateLabel.Location = new Point(1291, 8);
            DateLabel.Name = "DateLabel";
            DateLabel.Size = new Size(277, 42);
            DateLabel.TabIndex = 9;
            DateLabel.Text = "Date";
            DateLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // ResetCountingButton
            // 
            ResetCountingButton.BackColor = Color.FromArgb(38, 38, 38);
            ResetCountingButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            ResetCountingButton.BorderColor = Color.DarkGray;
            ResetCountingButton.BorderRadius = 5;
            ResetCountingButton.BorderSize = 1;
            ResetCountingButton.FlatAppearance.BorderSize = 0;
            ResetCountingButton.FlatStyle = FlatStyle.Flat;
            ResetCountingButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            ResetCountingButton.ForeColor = Color.White;
            ResetCountingButton.Location = new Point(221, 10);
            ResetCountingButton.Name = "ResetCountingButton";
            ResetCountingButton.Size = new Size(180, 36);
            ResetCountingButton.TabIndex = 109;
            ResetCountingButton.Text = "เริ่มการนับใหม่";
            ResetCountingButton.TextColor = Color.White;
            ResetCountingButton.UseVisualStyleBackColor = false;
            ResetCountingButton.Click += ResetCountingButton_Click;
            // 
            // DiffCashPanel
            // 
            DiffCashPanel.BackColor = Color.FromArgb(38, 38, 38);
            DiffCashPanel.Controls.Add(DiffCashLabel);
            DiffCashPanel.Controls.Add(DiffCashDescriptionLabel);
            DiffCashPanel.Controls.Add(label37);
            DiffCashPanel.Location = new Point(1383, 309);
            DiffCashPanel.Name = "DiffCashPanel";
            DiffCashPanel.Size = new Size(213, 148);
            DiffCashPanel.TabIndex = 136;
            // 
            // DiffCashLabel
            // 
            DiffCashLabel.BackColor = Color.FromArgb(38, 38, 38);
            DiffCashLabel.Dock = DockStyle.Fill;
            DiffCashLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DiffCashLabel.ForeColor = Color.Lavender;
            DiffCashLabel.Location = new Point(0, 39);
            DiffCashLabel.Name = "DiffCashLabel";
            DiffCashLabel.Size = new Size(213, 68);
            DiffCashLabel.TabIndex = 87;
            DiffCashLabel.Text = "0.00";
            DiffCashLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // DiffCashDescriptionLabel
            // 
            DiffCashDescriptionLabel.Dock = DockStyle.Bottom;
            DiffCashDescriptionLabel.Font = new Font("FC Subject [Non-commercial] Reg", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DiffCashDescriptionLabel.ForeColor = Color.Gainsboro;
            DiffCashDescriptionLabel.Location = new Point(0, 107);
            DiffCashDescriptionLabel.Margin = new Padding(6, 0, 6, 0);
            DiffCashDescriptionLabel.Name = "DiffCashDescriptionLabel";
            DiffCashDescriptionLabel.Size = new Size(213, 41);
            DiffCashDescriptionLabel.TabIndex = 88;
            DiffCashDescriptionLabel.Text = "?";
            DiffCashDescriptionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label37
            // 
            label37.BackColor = Color.FromArgb(38, 38, 38);
            label37.Dock = DockStyle.Top;
            label37.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label37.ForeColor = Color.Gainsboro;
            label37.Location = new Point(0, 0);
            label37.Name = "label37";
            label37.Size = new Size(213, 39);
            label37.TabIndex = 83;
            label37.Text = " ผลต่าง";
            label37.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel10
            // 
            panel10.BackColor = Color.FromArgb(38, 38, 38);
            panel10.Controls.Add(CalculatedCashTotalLabel);
            panel10.Controls.Add(label32);
            panel10.Location = new Point(1382, 192);
            panel10.Name = "panel10";
            panel10.Size = new Size(214, 111);
            panel10.TabIndex = 137;
            // 
            // CalculatedCashTotalLabel
            // 
            CalculatedCashTotalLabel.BackColor = Color.FromArgb(38, 38, 38);
            CalculatedCashTotalLabel.Dock = DockStyle.Fill;
            CalculatedCashTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            CalculatedCashTotalLabel.ForeColor = Color.SkyBlue;
            CalculatedCashTotalLabel.Location = new Point(0, 39);
            CalculatedCashTotalLabel.Name = "CalculatedCashTotalLabel";
            CalculatedCashTotalLabel.Size = new Size(214, 72);
            CalculatedCashTotalLabel.TabIndex = 87;
            CalculatedCashTotalLabel.Text = "0.00";
            CalculatedCashTotalLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label32
            // 
            label32.BackColor = Color.FromArgb(38, 38, 38);
            label32.Dock = DockStyle.Top;
            label32.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label32.ForeColor = Color.Gainsboro;
            label32.Location = new Point(0, 0);
            label32.Name = "label32";
            label32.Size = new Size(214, 39);
            label32.TabIndex = 83;
            label32.Text = " ยอดรวมเงินสด [ คำนวณ ]";
            label32.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel9
            // 
            panel9.BackColor = Color.FromArgb(38, 38, 38);
            panel9.Controls.Add(ActualCashTotalLabel);
            panel9.Controls.Add(label26);
            panel9.Location = new Point(1382, 75);
            panel9.Name = "panel9";
            panel9.Size = new Size(214, 111);
            panel9.TabIndex = 138;
            // 
            // ActualCashTotalLabel
            // 
            ActualCashTotalLabel.BackColor = Color.FromArgb(38, 38, 38);
            ActualCashTotalLabel.Dock = DockStyle.Fill;
            ActualCashTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            ActualCashTotalLabel.ForeColor = Color.Turquoise;
            ActualCashTotalLabel.Location = new Point(0, 39);
            ActualCashTotalLabel.Name = "ActualCashTotalLabel";
            ActualCashTotalLabel.Size = new Size(214, 72);
            ActualCashTotalLabel.TabIndex = 87;
            ActualCashTotalLabel.Text = "0.00";
            ActualCashTotalLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label26
            // 
            label26.BackColor = Color.FromArgb(38, 38, 38);
            label26.Dock = DockStyle.Top;
            label26.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label26.ForeColor = Color.Gainsboro;
            label26.Location = new Point(0, 0);
            label26.Name = "label26";
            label26.Size = new Size(214, 39);
            label26.TabIndex = 83;
            label26.Text = " ยอดรวมเงินสด [ นับ ]";
            label26.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel7
            // 
            panel7.BackColor = Color.FromArgb(38, 38, 38);
            panel7.Controls.Add(Coin1CountTextBox);
            panel7.Controls.Add(Coin2CountTextBox);
            panel7.Controls.Add(Coin5CountTextBox);
            panel7.Controls.Add(Coin10CountTextBox);
            panel7.Controls.Add(label24);
            panel7.Controls.Add(Coin1CountTotalLabel);
            panel7.Controls.Add(label15);
            panel7.Controls.Add(Coin2CountTotalLabel);
            panel7.Controls.Add(label17);
            panel7.Controls.Add(Coin5CountTotalLabel);
            panel7.Controls.Add(label19);
            panel7.Controls.Add(Coin10CountTotalLabel);
            panel7.Controls.Add(label23);
            panel7.Controls.Add(label25);
            panel7.Controls.Add(label36);
            panel7.Controls.Add(label38);
            panel7.Location = new Point(1000, 463);
            panel7.Name = "panel7";
            panel7.Size = new Size(373, 286);
            panel7.TabIndex = 139;
            // 
            // Coin1CountTextBox
            // 
            Coin1CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            Coin1CountTextBox.BorderColor = Color.DimGray;
            Coin1CountTextBox.BorderSize = 1;
            Coin1CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Coin1CountTextBox.ForeColor = Color.Gainsboro;
            Coin1CountTextBox.Location = new Point(111, 214);
            Coin1CountTextBox.Multiline = false;
            Coin1CountTextBox.Name = "Coin1CountTextBox";
            Coin1CountTextBox.Padding = new Padding(7);
            Coin1CountTextBox.PasswordChar = false;
            Coin1CountTextBox.PlaceholderColor = Color.DarkGray;
            Coin1CountTextBox.PlaceholderText = "";
            Coin1CountTextBox.ReadOnly = false;
            Coin1CountTextBox.Size = new Size(100, 37);
            Coin1CountTextBox.TabIndex = 90;
            Coin1CountTextBox.TextAlign = HorizontalAlignment.Center;
            Coin1CountTextBox.Texts = "";
            Coin1CountTextBox.UnderlinedStyle = true;
            Coin1CountTextBox.ModernTextChanged += Coin1CountTextBox_TextChanged;
            Coin1CountTextBox.Leave += Coin1CountTextBox_Leave;
            // 
            // Coin2CountTextBox
            // 
            Coin2CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            Coin2CountTextBox.BorderColor = Color.DimGray;
            Coin2CountTextBox.BorderSize = 1;
            Coin2CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Coin2CountTextBox.ForeColor = Color.Gainsboro;
            Coin2CountTextBox.Location = new Point(111, 174);
            Coin2CountTextBox.Multiline = false;
            Coin2CountTextBox.Name = "Coin2CountTextBox";
            Coin2CountTextBox.Padding = new Padding(7);
            Coin2CountTextBox.PasswordChar = false;
            Coin2CountTextBox.PlaceholderColor = Color.DarkGray;
            Coin2CountTextBox.PlaceholderText = "";
            Coin2CountTextBox.ReadOnly = false;
            Coin2CountTextBox.Size = new Size(100, 37);
            Coin2CountTextBox.TabIndex = 89;
            Coin2CountTextBox.TextAlign = HorizontalAlignment.Center;
            Coin2CountTextBox.Texts = "";
            Coin2CountTextBox.UnderlinedStyle = true;
            Coin2CountTextBox.ModernTextChanged += Coin2CountTextBox_TextChanged;
            Coin2CountTextBox.Leave += Coin2CountTextBox_Leave;
            // 
            // Coin5CountTextBox
            // 
            Coin5CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            Coin5CountTextBox.BorderColor = Color.DimGray;
            Coin5CountTextBox.BorderSize = 1;
            Coin5CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Coin5CountTextBox.ForeColor = Color.Gainsboro;
            Coin5CountTextBox.Location = new Point(111, 134);
            Coin5CountTextBox.Multiline = false;
            Coin5CountTextBox.Name = "Coin5CountTextBox";
            Coin5CountTextBox.Padding = new Padding(7);
            Coin5CountTextBox.PasswordChar = false;
            Coin5CountTextBox.PlaceholderColor = Color.DarkGray;
            Coin5CountTextBox.PlaceholderText = "";
            Coin5CountTextBox.ReadOnly = false;
            Coin5CountTextBox.Size = new Size(100, 37);
            Coin5CountTextBox.TabIndex = 88;
            Coin5CountTextBox.TextAlign = HorizontalAlignment.Center;
            Coin5CountTextBox.Texts = "";
            Coin5CountTextBox.UnderlinedStyle = true;
            Coin5CountTextBox.ModernTextChanged += Coin5CountTextBox_TextChanged;
            Coin5CountTextBox.Leave += Coin5CountTextBox_Leave;
            // 
            // Coin10CountTextBox
            // 
            Coin10CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            Coin10CountTextBox.BorderColor = Color.DimGray;
            Coin10CountTextBox.BorderSize = 1;
            Coin10CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Coin10CountTextBox.ForeColor = Color.Gainsboro;
            Coin10CountTextBox.Location = new Point(114, 94);
            Coin10CountTextBox.Multiline = false;
            Coin10CountTextBox.Name = "Coin10CountTextBox";
            Coin10CountTextBox.Padding = new Padding(7);
            Coin10CountTextBox.PasswordChar = false;
            Coin10CountTextBox.PlaceholderColor = Color.DarkGray;
            Coin10CountTextBox.PlaceholderText = "";
            Coin10CountTextBox.ReadOnly = false;
            Coin10CountTextBox.Size = new Size(100, 37);
            Coin10CountTextBox.TabIndex = 87;
            Coin10CountTextBox.TextAlign = HorizontalAlignment.Center;
            Coin10CountTextBox.Texts = "";
            Coin10CountTextBox.UnderlinedStyle = true;
            Coin10CountTextBox.ModernTextChanged += Coin10CountTextBox_TextChanged;
            Coin10CountTextBox.Leave += Coin10CountTextBox_Leave;
            // 
            // label24
            // 
            label24.BackColor = Color.FromArgb(38, 38, 38);
            label24.Dock = DockStyle.Top;
            label24.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label24.ForeColor = Color.Gainsboro;
            label24.Location = new Point(0, 0);
            label24.Name = "label24";
            label24.Size = new Size(373, 39);
            label24.TabIndex = 86;
            label24.Text = " รายการ [ เหรียญ ]";
            label24.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Coin1CountTotalLabel
            // 
            Coin1CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            Coin1CountTotalLabel.ForeColor = Color.Gainsboro;
            Coin1CountTotalLabel.Location = new Point(223, 215);
            Coin1CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            Coin1CountTotalLabel.Name = "Coin1CountTotalLabel";
            Coin1CountTotalLabel.Size = new Size(118, 36);
            Coin1CountTotalLabel.TabIndex = 42;
            Coin1CountTotalLabel.Text = "0.00";
            Coin1CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label15
            // 
            label15.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label15.ForeColor = Color.Gainsboro;
            label15.Location = new Point(29, 55);
            label15.Margin = new Padding(6, 0, 6, 0);
            label15.Name = "label15";
            label15.Size = new Size(76, 24);
            label15.TabIndex = 36;
            label15.Text = "เหรีญ";
            label15.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Coin2CountTotalLabel
            // 
            Coin2CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            Coin2CountTotalLabel.ForeColor = Color.Gainsboro;
            Coin2CountTotalLabel.Location = new Point(223, 175);
            Coin2CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            Coin2CountTotalLabel.Name = "Coin2CountTotalLabel";
            Coin2CountTotalLabel.Size = new Size(118, 36);
            Coin2CountTotalLabel.TabIndex = 41;
            Coin2CountTotalLabel.Text = "0.00";
            Coin2CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label17
            // 
            label17.BorderStyle = BorderStyle.FixedSingle;
            label17.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label17.ForeColor = Color.Gainsboro;
            label17.Location = new Point(29, 135);
            label17.Margin = new Padding(6, 0, 6, 0);
            label17.Name = "label17";
            label17.Size = new Size(76, 36);
            label17.TabIndex = 25;
            label17.Text = "5";
            label17.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Coin5CountTotalLabel
            // 
            Coin5CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            Coin5CountTotalLabel.ForeColor = Color.Gainsboro;
            Coin5CountTotalLabel.Location = new Point(223, 135);
            Coin5CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            Coin5CountTotalLabel.Name = "Coin5CountTotalLabel";
            Coin5CountTotalLabel.Size = new Size(118, 36);
            Coin5CountTotalLabel.TabIndex = 40;
            Coin5CountTotalLabel.Text = "0.00";
            Coin5CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label19
            // 
            label19.BorderStyle = BorderStyle.FixedSingle;
            label19.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label19.ForeColor = Color.Gainsboro;
            label19.Location = new Point(29, 175);
            label19.Margin = new Padding(6, 0, 6, 0);
            label19.Name = "label19";
            label19.Size = new Size(76, 36);
            label19.TabIndex = 26;
            label19.Text = "2";
            label19.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Coin10CountTotalLabel
            // 
            Coin10CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            Coin10CountTotalLabel.ForeColor = Color.Gainsboro;
            Coin10CountTotalLabel.Location = new Point(223, 95);
            Coin10CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            Coin10CountTotalLabel.Name = "Coin10CountTotalLabel";
            Coin10CountTotalLabel.Size = new Size(118, 36);
            Coin10CountTotalLabel.TabIndex = 39;
            Coin10CountTotalLabel.Text = "0.00";
            Coin10CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label23
            // 
            label23.BorderStyle = BorderStyle.FixedSingle;
            label23.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label23.ForeColor = Color.Gainsboro;
            label23.Location = new Point(29, 95);
            label23.Margin = new Padding(6, 0, 6, 0);
            label23.Name = "label23";
            label23.Size = new Size(76, 36);
            label23.TabIndex = 24;
            label23.Text = "10";
            label23.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label25
            // 
            label25.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label25.ForeColor = Color.Gainsboro;
            label25.Location = new Point(223, 55);
            label25.Margin = new Padding(6, 0, 6, 0);
            label25.Name = "label25";
            label25.Size = new Size(118, 24);
            label25.TabIndex = 38;
            label25.Text = "ยอดรวม";
            label25.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label36
            // 
            label36.BorderStyle = BorderStyle.FixedSingle;
            label36.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label36.ForeColor = Color.Gainsboro;
            label36.Location = new Point(29, 215);
            label36.Margin = new Padding(6, 0, 6, 0);
            label36.Name = "label36";
            label36.Size = new Size(76, 36);
            label36.TabIndex = 27;
            label36.Text = "1";
            label36.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label38
            // 
            label38.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label38.ForeColor = Color.Gainsboro;
            label38.Location = new Point(111, 55);
            label38.Margin = new Padding(6, 0, 6, 0);
            label38.Name = "label38";
            label38.Size = new Size(140, 24);
            label38.TabIndex = 37;
            label38.Text = "จำนวนนับ";
            label38.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(38, 38, 38);
            panel1.Controls.Add(BankNote20CountTextBox);
            panel1.Controls.Add(BankNote50CountTextBox);
            panel1.Controls.Add(BankNote100CountTextBox);
            panel1.Controls.Add(BankNote500CountTextBox);
            panel1.Controls.Add(BankNote1000CountTextBox);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(label16);
            panel1.Controls.Add(label18);
            panel1.Controls.Add(label20);
            panel1.Controls.Add(label39);
            panel1.Controls.Add(label40);
            panel1.Controls.Add(label41);
            panel1.Controls.Add(label42);
            panel1.Controls.Add(label43);
            panel1.Controls.Add(BankNote1000CountTotalLabel);
            panel1.Controls.Add(BankNote500CountTotalLabel);
            panel1.Controls.Add(BankNote100CountTotalLabel);
            panel1.Controls.Add(BankNote50CountTotalLabel);
            panel1.Controls.Add(BankNote20CountTotalLabel);
            panel1.Location = new Point(1000, 117);
            panel1.Name = "panel1";
            panel1.Size = new Size(373, 340);
            panel1.TabIndex = 140;
            // 
            // BankNote20CountTextBox
            // 
            BankNote20CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            BankNote20CountTextBox.BorderColor = Color.DimGray;
            BankNote20CountTextBox.BorderSize = 1;
            BankNote20CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BankNote20CountTextBox.ForeColor = Color.Gainsboro;
            BankNote20CountTextBox.Location = new Point(114, 267);
            BankNote20CountTextBox.Multiline = false;
            BankNote20CountTextBox.Name = "BankNote20CountTextBox";
            BankNote20CountTextBox.Padding = new Padding(7);
            BankNote20CountTextBox.PasswordChar = false;
            BankNote20CountTextBox.PlaceholderColor = Color.DarkGray;
            BankNote20CountTextBox.PlaceholderText = "";
            BankNote20CountTextBox.ReadOnly = false;
            BankNote20CountTextBox.Size = new Size(100, 37);
            BankNote20CountTextBox.TabIndex = 105;
            BankNote20CountTextBox.TextAlign = HorizontalAlignment.Center;
            BankNote20CountTextBox.Texts = "";
            BankNote20CountTextBox.UnderlinedStyle = true;
            BankNote20CountTextBox.ModernTextChanged += BankNote20CountTextBox_TextChanged;
            BankNote20CountTextBox.Leave += BankNote20CountTextBox_Leave;
            // 
            // BankNote50CountTextBox
            // 
            BankNote50CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            BankNote50CountTextBox.BorderColor = Color.DimGray;
            BankNote50CountTextBox.BorderSize = 1;
            BankNote50CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BankNote50CountTextBox.ForeColor = Color.Gainsboro;
            BankNote50CountTextBox.Location = new Point(114, 224);
            BankNote50CountTextBox.Multiline = false;
            BankNote50CountTextBox.Name = "BankNote50CountTextBox";
            BankNote50CountTextBox.Padding = new Padding(7);
            BankNote50CountTextBox.PasswordChar = false;
            BankNote50CountTextBox.PlaceholderColor = Color.DarkGray;
            BankNote50CountTextBox.PlaceholderText = "";
            BankNote50CountTextBox.ReadOnly = false;
            BankNote50CountTextBox.Size = new Size(100, 37);
            BankNote50CountTextBox.TabIndex = 104;
            BankNote50CountTextBox.TextAlign = HorizontalAlignment.Center;
            BankNote50CountTextBox.Texts = "";
            BankNote50CountTextBox.UnderlinedStyle = true;
            BankNote50CountTextBox.ModernTextChanged += BankNote50CountTextBox_TextChanged;
            BankNote50CountTextBox.Leave += BankNote50CountTextBox_Leave;
            // 
            // BankNote100CountTextBox
            // 
            BankNote100CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            BankNote100CountTextBox.BorderColor = Color.DimGray;
            BankNote100CountTextBox.BorderSize = 1;
            BankNote100CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BankNote100CountTextBox.ForeColor = Color.Gainsboro;
            BankNote100CountTextBox.Location = new Point(114, 181);
            BankNote100CountTextBox.Multiline = false;
            BankNote100CountTextBox.Name = "BankNote100CountTextBox";
            BankNote100CountTextBox.Padding = new Padding(7);
            BankNote100CountTextBox.PasswordChar = false;
            BankNote100CountTextBox.PlaceholderColor = Color.DarkGray;
            BankNote100CountTextBox.PlaceholderText = "";
            BankNote100CountTextBox.ReadOnly = false;
            BankNote100CountTextBox.Size = new Size(100, 37);
            BankNote100CountTextBox.TabIndex = 103;
            BankNote100CountTextBox.TextAlign = HorizontalAlignment.Center;
            BankNote100CountTextBox.Texts = "";
            BankNote100CountTextBox.UnderlinedStyle = true;
            BankNote100CountTextBox.ModernTextChanged += BankNote100CountTextBox_TextChanged;
            BankNote100CountTextBox.Leave += BankNote100CountTextBox_Leave;
            // 
            // BankNote500CountTextBox
            // 
            BankNote500CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            BankNote500CountTextBox.BorderColor = Color.DimGray;
            BankNote500CountTextBox.BorderSize = 1;
            BankNote500CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BankNote500CountTextBox.ForeColor = Color.Gainsboro;
            BankNote500CountTextBox.Location = new Point(114, 138);
            BankNote500CountTextBox.Multiline = false;
            BankNote500CountTextBox.Name = "BankNote500CountTextBox";
            BankNote500CountTextBox.Padding = new Padding(7);
            BankNote500CountTextBox.PasswordChar = false;
            BankNote500CountTextBox.PlaceholderColor = Color.DarkGray;
            BankNote500CountTextBox.PlaceholderText = "";
            BankNote500CountTextBox.ReadOnly = false;
            BankNote500CountTextBox.Size = new Size(100, 37);
            BankNote500CountTextBox.TabIndex = 102;
            BankNote500CountTextBox.TextAlign = HorizontalAlignment.Center;
            BankNote500CountTextBox.Texts = "";
            BankNote500CountTextBox.UnderlinedStyle = true;
            BankNote500CountTextBox.ModernTextChanged += BankNote500CountTextBox_TextChanged;
            BankNote500CountTextBox.Leave += BankNote500CountTextBox_Leave;
            // 
            // BankNote1000CountTextBox
            // 
            BankNote1000CountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            BankNote1000CountTextBox.BorderColor = Color.DimGray;
            BankNote1000CountTextBox.BorderSize = 1;
            BankNote1000CountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BankNote1000CountTextBox.ForeColor = Color.Gainsboro;
            BankNote1000CountTextBox.Location = new Point(114, 95);
            BankNote1000CountTextBox.Multiline = false;
            BankNote1000CountTextBox.Name = "BankNote1000CountTextBox";
            BankNote1000CountTextBox.Padding = new Padding(7);
            BankNote1000CountTextBox.PasswordChar = false;
            BankNote1000CountTextBox.PlaceholderColor = Color.DarkGray;
            BankNote1000CountTextBox.PlaceholderText = "";
            BankNote1000CountTextBox.ReadOnly = false;
            BankNote1000CountTextBox.Size = new Size(100, 37);
            BankNote1000CountTextBox.TabIndex = 101;
            BankNote1000CountTextBox.TextAlign = HorizontalAlignment.Center;
            BankNote1000CountTextBox.Texts = "";
            BankNote1000CountTextBox.UnderlinedStyle = true;
            BankNote1000CountTextBox.ModernTextChanged += BankNote1000CountTextBox_TextChanged;
            BankNote1000CountTextBox.Leave += BankNote1000CountTextBox_Leave;
            // 
            // label9
            // 
            label9.BackColor = Color.FromArgb(38, 38, 38);
            label9.Dock = DockStyle.Top;
            label9.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label9.ForeColor = Color.Gainsboro;
            label9.Location = new Point(0, 0);
            label9.Name = "label9";
            label9.Size = new Size(373, 39);
            label9.TabIndex = 100;
            label9.Text = " รายการ [ ธนบัตร ]";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label16
            // 
            label16.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label16.ForeColor = Color.Gainsboro;
            label16.Location = new Point(29, 57);
            label16.Margin = new Padding(6, 0, 6, 0);
            label16.Name = "label16";
            label16.Size = new Size(76, 24);
            label16.TabIndex = 92;
            label16.Text = "ธนบัตร";
            label16.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label18
            // 
            label18.BorderStyle = BorderStyle.FixedSingle;
            label18.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label18.ForeColor = Color.Gainsboro;
            label18.Location = new Point(29, 139);
            label18.Margin = new Padding(6, 0, 6, 0);
            label18.Name = "label18";
            label18.Size = new Size(76, 36);
            label18.TabIndex = 88;
            label18.Text = "500";
            label18.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label20
            // 
            label20.BorderStyle = BorderStyle.FixedSingle;
            label20.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label20.ForeColor = Color.Gainsboro;
            label20.Location = new Point(29, 182);
            label20.Margin = new Padding(6, 0, 6, 0);
            label20.Name = "label20";
            label20.Size = new Size(76, 36);
            label20.TabIndex = 89;
            label20.Text = "100";
            label20.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label39
            // 
            label39.BorderStyle = BorderStyle.FixedSingle;
            label39.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label39.ForeColor = Color.Gainsboro;
            label39.Location = new Point(29, 96);
            label39.Margin = new Padding(6, 0, 6, 0);
            label39.Name = "label39";
            label39.Size = new Size(76, 36);
            label39.TabIndex = 87;
            label39.Text = "1,000";
            label39.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label40
            // 
            label40.BorderStyle = BorderStyle.FixedSingle;
            label40.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label40.ForeColor = Color.Gainsboro;
            label40.Location = new Point(29, 268);
            label40.Margin = new Padding(6, 0, 6, 0);
            label40.Name = "label40";
            label40.Size = new Size(76, 36);
            label40.TabIndex = 91;
            label40.Text = "20";
            label40.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label41
            // 
            label41.BorderStyle = BorderStyle.FixedSingle;
            label41.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label41.ForeColor = Color.Gainsboro;
            label41.Location = new Point(29, 225);
            label41.Margin = new Padding(6, 0, 6, 0);
            label41.Name = "label41";
            label41.Size = new Size(76, 36);
            label41.TabIndex = 90;
            label41.Text = "50";
            label41.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label42
            // 
            label42.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label42.ForeColor = Color.Gainsboro;
            label42.Location = new Point(111, 57);
            label42.Margin = new Padding(6, 0, 6, 0);
            label42.Name = "label42";
            label42.Size = new Size(121, 24);
            label42.TabIndex = 93;
            label42.Text = "จำนวนนับ";
            label42.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label43
            // 
            label43.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F);
            label43.ForeColor = Color.Gainsboro;
            label43.Location = new Point(241, 57);
            label43.Margin = new Padding(6, 0, 6, 0);
            label43.Name = "label43";
            label43.Size = new Size(100, 24);
            label43.TabIndex = 94;
            label43.Text = "ยอดรวม";
            label43.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // BankNote1000CountTotalLabel
            // 
            BankNote1000CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            BankNote1000CountTotalLabel.ForeColor = Color.Gainsboro;
            BankNote1000CountTotalLabel.Location = new Point(223, 98);
            BankNote1000CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            BankNote1000CountTotalLabel.Name = "BankNote1000CountTotalLabel";
            BankNote1000CountTotalLabel.Size = new Size(118, 34);
            BankNote1000CountTotalLabel.TabIndex = 95;
            BankNote1000CountTotalLabel.Text = "0.00";
            BankNote1000CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // BankNote500CountTotalLabel
            // 
            BankNote500CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            BankNote500CountTotalLabel.ForeColor = Color.Gainsboro;
            BankNote500CountTotalLabel.Location = new Point(223, 141);
            BankNote500CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            BankNote500CountTotalLabel.Name = "BankNote500CountTotalLabel";
            BankNote500CountTotalLabel.Size = new Size(118, 34);
            BankNote500CountTotalLabel.TabIndex = 96;
            BankNote500CountTotalLabel.Text = "0.00";
            BankNote500CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // BankNote100CountTotalLabel
            // 
            BankNote100CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            BankNote100CountTotalLabel.ForeColor = Color.Gainsboro;
            BankNote100CountTotalLabel.Location = new Point(223, 184);
            BankNote100CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            BankNote100CountTotalLabel.Name = "BankNote100CountTotalLabel";
            BankNote100CountTotalLabel.Size = new Size(118, 34);
            BankNote100CountTotalLabel.TabIndex = 97;
            BankNote100CountTotalLabel.Text = "0.00";
            BankNote100CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // BankNote50CountTotalLabel
            // 
            BankNote50CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            BankNote50CountTotalLabel.ForeColor = Color.Gainsboro;
            BankNote50CountTotalLabel.Location = new Point(223, 227);
            BankNote50CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            BankNote50CountTotalLabel.Name = "BankNote50CountTotalLabel";
            BankNote50CountTotalLabel.Size = new Size(118, 34);
            BankNote50CountTotalLabel.TabIndex = 98;
            BankNote50CountTotalLabel.Text = "0.00";
            BankNote50CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // BankNote20CountTotalLabel
            // 
            BankNote20CountTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 11.9999981F);
            BankNote20CountTotalLabel.ForeColor = Color.Gainsboro;
            BankNote20CountTotalLabel.Location = new Point(223, 270);
            BankNote20CountTotalLabel.Margin = new Padding(6, 0, 6, 0);
            BankNote20CountTotalLabel.Name = "BankNote20CountTotalLabel";
            BankNote20CountTotalLabel.Size = new Size(118, 34);
            BankNote20CountTotalLabel.TabIndex = 99;
            BankNote20CountTotalLabel.Text = "0.00";
            BankNote20CountTotalLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label49
            // 
            label49.BackColor = Color.FromArgb(38, 38, 38);
            label49.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label49.ForeColor = Color.Gainsboro;
            label49.Location = new Point(1000, 75);
            label49.Margin = new Padding(6, 0, 6, 0);
            label49.Name = "label49";
            label49.Size = new Size(373, 36);
            label49.TabIndex = 141;
            label49.Text = "เงินสด [ นับ ]";
            label49.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // SaveToFileButton
            // 
            SaveToFileButton.BackColor = Color.FromArgb(38, 38, 38);
            SaveToFileButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            SaveToFileButton.BorderColor = Color.DarkGray;
            SaveToFileButton.BorderRadius = 5;
            SaveToFileButton.BorderSize = 1;
            SaveToFileButton.FlatAppearance.BorderSize = 0;
            SaveToFileButton.FlatStyle = FlatStyle.Flat;
            SaveToFileButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            SaveToFileButton.ForeColor = Color.White;
            SaveToFileButton.Location = new Point(430, 10);
            SaveToFileButton.Name = "SaveToFileButton";
            SaveToFileButton.Size = new Size(180, 36);
            SaveToFileButton.TabIndex = 142;
            SaveToFileButton.Text = "บันทึก";
            SaveToFileButton.TextColor = Color.White;
            SaveToFileButton.UseVisualStyleBackColor = false;
            SaveToFileButton.Click += SaveToFileButton_Click;
            // 
            // PullLatestDataFromDatabaseButton
            // 
            PullLatestDataFromDatabaseButton.BackColor = Color.FromArgb(38, 38, 38);
            PullLatestDataFromDatabaseButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            PullLatestDataFromDatabaseButton.BorderColor = Color.DarkGray;
            PullLatestDataFromDatabaseButton.BorderRadius = 5;
            PullLatestDataFromDatabaseButton.BorderSize = 1;
            PullLatestDataFromDatabaseButton.FlatAppearance.BorderSize = 0;
            PullLatestDataFromDatabaseButton.FlatStyle = FlatStyle.Flat;
            PullLatestDataFromDatabaseButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            PullLatestDataFromDatabaseButton.ForeColor = Color.White;
            PullLatestDataFromDatabaseButton.Location = new Point(12, 10);
            PullLatestDataFromDatabaseButton.Name = "PullLatestDataFromDatabaseButton";
            PullLatestDataFromDatabaseButton.Size = new Size(180, 36);
            PullLatestDataFromDatabaseButton.TabIndex = 143;
            PullLatestDataFromDatabaseButton.Text = "ดึงข้อมูลล่าสุด";
            PullLatestDataFromDatabaseButton.TextColor = Color.White;
            PullLatestDataFromDatabaseButton.UseVisualStyleBackColor = false;
            PullLatestDataFromDatabaseButton.Click += PullLatestDataFromDatabaseButton_Click;
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(38, 38, 38);
            panel2.Controls.Add(PullLatestDataFromDatabaseButton);
            panel2.Controls.Add(DateLabel);
            panel2.Controls.Add(SaveToFileButton);
            panel2.Controls.Add(ResetCountingButton);
            panel2.Location = new Point(15, 12);
            panel2.Name = "panel2";
            panel2.Size = new Size(1581, 56);
            panel2.TabIndex = 144;
            // 
            // panel3
            // 
            panel3.BackColor = Color.FromArgb(38, 38, 38);
            panel3.Controls.Add(RemoveReceivedPayLaterPaymentButton);
            panel3.Controls.Add(AddReceivedPayLaterPaymentButton);
            panel3.Controls.Add(ReceivedPayLaterAmountTextBox);
            panel3.Controls.Add(PayLaterAccountNameTextBox);
            panel3.Controls.Add(label10);
            panel3.Controls.Add(label8);
            panel3.Controls.Add(label5);
            panel3.Controls.Add(ReceivedPayLaterPaymentsTotalLabel);
            panel3.Controls.Add(label3);
            panel3.Controls.Add(ReceivedPayLaterPaymentsListView);
            panel3.Location = new Point(665, 117);
            panel3.Name = "panel3";
            panel3.Size = new Size(329, 429);
            panel3.TabIndex = 145;
            // 
            // RemoveReceivedPayLaterPaymentButton
            // 
            RemoveReceivedPayLaterPaymentButton.BackColor = Color.FromArgb(38, 38, 38);
            RemoveReceivedPayLaterPaymentButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            RemoveReceivedPayLaterPaymentButton.BorderColor = Color.Salmon;
            RemoveReceivedPayLaterPaymentButton.BorderRadius = 5;
            RemoveReceivedPayLaterPaymentButton.BorderSize = 1;
            RemoveReceivedPayLaterPaymentButton.FlatAppearance.BorderSize = 0;
            RemoveReceivedPayLaterPaymentButton.FlatStyle = FlatStyle.Flat;
            RemoveReceivedPayLaterPaymentButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            RemoveReceivedPayLaterPaymentButton.ForeColor = Color.White;
            RemoveReceivedPayLaterPaymentButton.Location = new Point(221, 383);
            RemoveReceivedPayLaterPaymentButton.Name = "RemoveReceivedPayLaterPaymentButton";
            RemoveReceivedPayLaterPaymentButton.Size = new Size(80, 36);
            RemoveReceivedPayLaterPaymentButton.TabIndex = 144;
            RemoveReceivedPayLaterPaymentButton.Text = "ลบ";
            RemoveReceivedPayLaterPaymentButton.TextColor = Color.White;
            RemoveReceivedPayLaterPaymentButton.UseVisualStyleBackColor = false;
            RemoveReceivedPayLaterPaymentButton.Click += RemoveReceivedPayLaterPaymentButton_Click;
            // 
            // AddReceivedPayLaterPaymentButton
            // 
            AddReceivedPayLaterPaymentButton.BackColor = Color.FromArgb(38, 38, 38);
            AddReceivedPayLaterPaymentButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddReceivedPayLaterPaymentButton.BorderColor = Color.MediumSpringGreen;
            AddReceivedPayLaterPaymentButton.BorderRadius = 5;
            AddReceivedPayLaterPaymentButton.BorderSize = 1;
            AddReceivedPayLaterPaymentButton.FlatAppearance.BorderSize = 0;
            AddReceivedPayLaterPaymentButton.FlatStyle = FlatStyle.Flat;
            AddReceivedPayLaterPaymentButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            AddReceivedPayLaterPaymentButton.ForeColor = Color.White;
            AddReceivedPayLaterPaymentButton.Location = new Point(221, 113);
            AddReceivedPayLaterPaymentButton.Name = "AddReceivedPayLaterPaymentButton";
            AddReceivedPayLaterPaymentButton.Size = new Size(80, 36);
            AddReceivedPayLaterPaymentButton.TabIndex = 143;
            AddReceivedPayLaterPaymentButton.Text = "เพิ่ม";
            AddReceivedPayLaterPaymentButton.TextColor = Color.White;
            AddReceivedPayLaterPaymentButton.UseVisualStyleBackColor = false;
            AddReceivedPayLaterPaymentButton.Click += AddReceivedPayLaterPaymentButton_Click;
            // 
            // ReceivedPayLaterAmountTextBox
            // 
            ReceivedPayLaterAmountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            ReceivedPayLaterAmountTextBox.BorderColor = Color.DimGray;
            ReceivedPayLaterAmountTextBox.BorderSize = 1;
            ReceivedPayLaterAmountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ReceivedPayLaterAmountTextBox.ForeColor = Color.Gainsboro;
            ReceivedPayLaterAmountTextBox.Location = new Point(102, 112);
            ReceivedPayLaterAmountTextBox.Multiline = false;
            ReceivedPayLaterAmountTextBox.Name = "ReceivedPayLaterAmountTextBox";
            ReceivedPayLaterAmountTextBox.Padding = new Padding(7);
            ReceivedPayLaterAmountTextBox.PasswordChar = false;
            ReceivedPayLaterAmountTextBox.PlaceholderColor = Color.DarkGray;
            ReceivedPayLaterAmountTextBox.PlaceholderText = "";
            ReceivedPayLaterAmountTextBox.ReadOnly = false;
            ReceivedPayLaterAmountTextBox.Size = new Size(113, 37);
            ReceivedPayLaterAmountTextBox.TabIndex = 141;
            ReceivedPayLaterAmountTextBox.TextAlign = HorizontalAlignment.Center;
            ReceivedPayLaterAmountTextBox.Texts = "";
            ReceivedPayLaterAmountTextBox.UnderlinedStyle = true;
            // 
            // PayLaterAccountNameTextBox
            // 
            PayLaterAccountNameTextBox.BackColor = Color.FromArgb(38, 38, 38);
            PayLaterAccountNameTextBox.BorderColor = Color.DimGray;
            PayLaterAccountNameTextBox.BorderSize = 1;
            PayLaterAccountNameTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PayLaterAccountNameTextBox.ForeColor = Color.Gainsboro;
            PayLaterAccountNameTextBox.Location = new Point(102, 69);
            PayLaterAccountNameTextBox.Multiline = false;
            PayLaterAccountNameTextBox.Name = "PayLaterAccountNameTextBox";
            PayLaterAccountNameTextBox.Padding = new Padding(7);
            PayLaterAccountNameTextBox.PasswordChar = false;
            PayLaterAccountNameTextBox.PlaceholderColor = Color.DarkGray;
            PayLaterAccountNameTextBox.PlaceholderText = "";
            PayLaterAccountNameTextBox.ReadOnly = false;
            PayLaterAccountNameTextBox.Size = new Size(113, 37);
            PayLaterAccountNameTextBox.TabIndex = 140;
            PayLaterAccountNameTextBox.TextAlign = HorizontalAlignment.Center;
            PayLaterAccountNameTextBox.Texts = "";
            PayLaterAccountNameTextBox.UnderlinedStyle = true;
            // 
            // label10
            // 
            label10.BackColor = Color.FromArgb(38, 38, 38);
            label10.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label10.ForeColor = Color.Gainsboro;
            label10.Location = new Point(26, 121);
            label10.Name = "label10";
            label10.Size = new Size(70, 25);
            label10.TabIndex = 139;
            label10.Text = "จำนวน";
            label10.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            label8.BackColor = Color.FromArgb(38, 38, 38);
            label8.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label8.ForeColor = Color.Gainsboro;
            label8.Location = new Point(26, 79);
            label8.Name = "label8";
            label8.Size = new Size(70, 25);
            label8.TabIndex = 138;
            label8.Text = "ชื่อ";
            label8.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            label5.BackColor = Color.FromArgb(38, 38, 38);
            label5.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label5.ForeColor = Color.Gainsboro;
            label5.Location = new Point(26, 41);
            label5.Name = "label5";
            label5.Size = new Size(70, 25);
            label5.TabIndex = 137;
            label5.Text = "ยอดรวม";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // ReceivedPayLaterPaymentsTotalLabel
            // 
            ReceivedPayLaterPaymentsTotalLabel.BackColor = Color.FromArgb(38, 38, 38);
            ReceivedPayLaterPaymentsTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 18F);
            ReceivedPayLaterPaymentsTotalLabel.ForeColor = Color.SkyBlue;
            ReceivedPayLaterPaymentsTotalLabel.Location = new Point(102, 36);
            ReceivedPayLaterPaymentsTotalLabel.Name = "ReceivedPayLaterPaymentsTotalLabel";
            ReceivedPayLaterPaymentsTotalLabel.Size = new Size(199, 30);
            ReceivedPayLaterPaymentsTotalLabel.TabIndex = 136;
            ReceivedPayLaterPaymentsTotalLabel.Text = "0.00";
            ReceivedPayLaterPaymentsTotalLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.BackColor = Color.FromArgb(38, 38, 38);
            label3.Dock = DockStyle.Top;
            label3.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label3.ForeColor = Color.Gainsboro;
            label3.Location = new Point(0, 0);
            label3.Name = "label3";
            label3.Size = new Size(329, 39);
            label3.TabIndex = 87;
            label3.Text = "รายการ [ ลูกค้าชำระหนี้ ]";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel4
            // 
            panel4.BackColor = Color.FromArgb(38, 38, 38);
            panel4.Controls.Add(RemovePayOutButton);
            panel4.Controls.Add(AddPayOutButton);
            panel4.Controls.Add(PayOutAmountTextBox);
            panel4.Controls.Add(PayOutItemTextBox);
            panel4.Controls.Add(PayoutsListView);
            panel4.Controls.Add(label6);
            panel4.Controls.Add(label7);
            panel4.Controls.Add(label11);
            panel4.Controls.Add(PayoutsTotalLabel);
            panel4.Controls.Add(label13);
            panel4.Location = new Point(307, 117);
            panel4.Name = "panel4";
            panel4.Size = new Size(352, 494);
            panel4.TabIndex = 146;
            // 
            // RemovePayOutButton
            // 
            RemovePayOutButton.BackColor = Color.FromArgb(38, 38, 38);
            RemovePayOutButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            RemovePayOutButton.BorderColor = Color.Salmon;
            RemovePayOutButton.BorderRadius = 5;
            RemovePayOutButton.BorderSize = 1;
            RemovePayOutButton.FlatAppearance.BorderSize = 0;
            RemovePayOutButton.FlatStyle = FlatStyle.Flat;
            RemovePayOutButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            RemovePayOutButton.ForeColor = Color.White;
            RemovePayOutButton.Location = new Point(245, 441);
            RemovePayOutButton.Name = "RemovePayOutButton";
            RemovePayOutButton.Size = new Size(80, 36);
            RemovePayOutButton.TabIndex = 144;
            RemovePayOutButton.Text = "ลบ";
            RemovePayOutButton.TextColor = Color.White;
            RemovePayOutButton.UseVisualStyleBackColor = false;
            RemovePayOutButton.Click += RemovePayOutItemButton_Click;
            // 
            // AddPayOutButton
            // 
            AddPayOutButton.BackColor = Color.FromArgb(38, 38, 38);
            AddPayOutButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddPayOutButton.BorderColor = Color.MediumSpringGreen;
            AddPayOutButton.BorderRadius = 5;
            AddPayOutButton.BorderSize = 1;
            AddPayOutButton.FlatAppearance.BorderSize = 0;
            AddPayOutButton.FlatStyle = FlatStyle.Flat;
            AddPayOutButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            AddPayOutButton.ForeColor = Color.White;
            AddPayOutButton.Location = new Point(238, 112);
            AddPayOutButton.Name = "AddPayOutButton";
            AddPayOutButton.Size = new Size(87, 36);
            AddPayOutButton.TabIndex = 143;
            AddPayOutButton.Text = "เพิ่ม";
            AddPayOutButton.TextColor = Color.White;
            AddPayOutButton.UseVisualStyleBackColor = false;
            AddPayOutButton.Click += AddPayOutItemButton_Click;
            // 
            // PayOutAmountTextBox
            // 
            PayOutAmountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            PayOutAmountTextBox.BorderColor = Color.DimGray;
            PayOutAmountTextBox.BorderSize = 1;
            PayOutAmountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PayOutAmountTextBox.ForeColor = Color.Gainsboro;
            PayOutAmountTextBox.Location = new Point(95, 112);
            PayOutAmountTextBox.Multiline = false;
            PayOutAmountTextBox.Name = "PayOutAmountTextBox";
            PayOutAmountTextBox.Padding = new Padding(7);
            PayOutAmountTextBox.PasswordChar = false;
            PayOutAmountTextBox.PlaceholderColor = Color.DarkGray;
            PayOutAmountTextBox.PlaceholderText = "";
            PayOutAmountTextBox.ReadOnly = false;
            PayOutAmountTextBox.Size = new Size(137, 37);
            PayOutAmountTextBox.TabIndex = 141;
            PayOutAmountTextBox.TextAlign = HorizontalAlignment.Center;
            PayOutAmountTextBox.Texts = "";
            PayOutAmountTextBox.UnderlinedStyle = true;
            // 
            // PayOutItemTextBox
            // 
            PayOutItemTextBox.BackColor = Color.FromArgb(38, 38, 38);
            PayOutItemTextBox.BorderColor = Color.DimGray;
            PayOutItemTextBox.BorderSize = 1;
            PayOutItemTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PayOutItemTextBox.ForeColor = Color.Gainsboro;
            PayOutItemTextBox.Location = new Point(95, 69);
            PayOutItemTextBox.Multiline = false;
            PayOutItemTextBox.Name = "PayOutItemTextBox";
            PayOutItemTextBox.Padding = new Padding(7);
            PayOutItemTextBox.PasswordChar = false;
            PayOutItemTextBox.PlaceholderColor = Color.DarkGray;
            PayOutItemTextBox.PlaceholderText = "";
            PayOutItemTextBox.ReadOnly = false;
            PayOutItemTextBox.Size = new Size(137, 37);
            PayOutItemTextBox.TabIndex = 140;
            PayOutItemTextBox.TextAlign = HorizontalAlignment.Center;
            PayOutItemTextBox.Texts = "";
            PayOutItemTextBox.UnderlinedStyle = true;
            // 
            // label6
            // 
            label6.BackColor = Color.FromArgb(38, 38, 38);
            label6.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label6.ForeColor = Color.Gainsboro;
            label6.Location = new Point(19, 124);
            label6.Name = "label6";
            label6.Size = new Size(70, 25);
            label6.TabIndex = 139;
            label6.Text = "จำนวน";
            label6.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            label7.BackColor = Color.FromArgb(38, 38, 38);
            label7.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label7.ForeColor = Color.Gainsboro;
            label7.Location = new Point(19, 81);
            label7.Name = "label7";
            label7.Size = new Size(70, 25);
            label7.TabIndex = 138;
            label7.Text = "ชื่อ";
            label7.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label11
            // 
            label11.BackColor = Color.FromArgb(38, 38, 38);
            label11.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label11.ForeColor = Color.Gainsboro;
            label11.Location = new Point(19, 41);
            label11.Name = "label11";
            label11.Size = new Size(70, 25);
            label11.TabIndex = 137;
            label11.Text = "ยอดรวม";
            label11.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // PayoutsTotalLabel
            // 
            PayoutsTotalLabel.BackColor = Color.FromArgb(38, 38, 38);
            PayoutsTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 18F);
            PayoutsTotalLabel.ForeColor = Color.Tan;
            PayoutsTotalLabel.Location = new Point(95, 36);
            PayoutsTotalLabel.Name = "PayoutsTotalLabel";
            PayoutsTotalLabel.Size = new Size(223, 30);
            PayoutsTotalLabel.TabIndex = 136;
            PayoutsTotalLabel.Text = "0.00";
            PayoutsTotalLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label13
            // 
            label13.BackColor = Color.FromArgb(38, 38, 38);
            label13.Dock = DockStyle.Top;
            label13.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label13.ForeColor = Color.Gainsboro;
            label13.Location = new Point(0, 0);
            label13.Name = "label13";
            label13.Size = new Size(352, 39);
            label13.TabIndex = 87;
            label13.Text = "รายการ [ รายจ่าย ]";
            label13.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label44
            // 
            label44.BackColor = Color.FromArgb(38, 38, 38);
            label44.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label44.ForeColor = Color.Gainsboro;
            label44.Location = new Point(15, 75);
            label44.Margin = new Padding(6, 0, 6, 0);
            label44.Name = "label44";
            label44.Size = new Size(979, 36);
            label44.TabIndex = 147;
            label44.Text = "เงินสด [ คำนวณ ]";
            label44.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel5
            // 
            panel5.BackColor = Color.FromArgb(38, 38, 38);
            panel5.Controls.Add(RemoveChangeButton);
            panel5.Controls.Add(AddChangeButton);
            panel5.Controls.Add(ChangeAmountTextBox);
            panel5.Controls.Add(ChangesListView);
            panel5.Controls.Add(label4);
            panel5.Controls.Add(label45);
            panel5.Controls.Add(ChangesTotalLabel);
            panel5.Controls.Add(label47);
            panel5.Location = new Point(665, 552);
            panel5.Name = "panel5";
            panel5.Size = new Size(329, 354);
            panel5.TabIndex = 148;
            // 
            // RemoveChangeButton
            // 
            RemoveChangeButton.BackColor = Color.FromArgb(38, 38, 38);
            RemoveChangeButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            RemoveChangeButton.BorderColor = Color.Salmon;
            RemoveChangeButton.BorderRadius = 5;
            RemoveChangeButton.BorderSize = 1;
            RemoveChangeButton.FlatAppearance.BorderSize = 0;
            RemoveChangeButton.FlatStyle = FlatStyle.Flat;
            RemoveChangeButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            RemoveChangeButton.ForeColor = Color.White;
            RemoveChangeButton.Location = new Point(224, 306);
            RemoveChangeButton.Name = "RemoveChangeButton";
            RemoveChangeButton.Size = new Size(77, 36);
            RemoveChangeButton.TabIndex = 144;
            RemoveChangeButton.Text = "ลบ";
            RemoveChangeButton.TextColor = Color.White;
            RemoveChangeButton.UseVisualStyleBackColor = false;
            RemoveChangeButton.Click += RemoveChangeButton_Click;
            // 
            // AddChangeButton
            // 
            AddChangeButton.BackColor = Color.FromArgb(38, 38, 38);
            AddChangeButton.BackgroundColor = Color.FromArgb(38, 38, 38);
            AddChangeButton.BorderColor = Color.MediumSpringGreen;
            AddChangeButton.BorderRadius = 5;
            AddChangeButton.BorderSize = 1;
            AddChangeButton.FlatAppearance.BorderSize = 0;
            AddChangeButton.FlatStyle = FlatStyle.Flat;
            AddChangeButton.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            AddChangeButton.ForeColor = Color.White;
            AddChangeButton.Location = new Point(224, 80);
            AddChangeButton.Name = "AddChangeButton";
            AddChangeButton.Size = new Size(77, 36);
            AddChangeButton.TabIndex = 143;
            AddChangeButton.Text = "เพิ่ม";
            AddChangeButton.TextColor = Color.White;
            AddChangeButton.UseVisualStyleBackColor = false;
            AddChangeButton.Click += AddChangeButton_Click;
            // 
            // ChangeAmountTextBox
            // 
            ChangeAmountTextBox.BackColor = Color.FromArgb(38, 38, 38);
            ChangeAmountTextBox.BorderColor = Color.DimGray;
            ChangeAmountTextBox.BorderSize = 1;
            ChangeAmountTextBox.Font = new Font("FC Subject [Non-commercial] Reg", 11.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ChangeAmountTextBox.ForeColor = Color.Gainsboro;
            ChangeAmountTextBox.Location = new Point(89, 79);
            ChangeAmountTextBox.Multiline = false;
            ChangeAmountTextBox.Name = "ChangeAmountTextBox";
            ChangeAmountTextBox.Padding = new Padding(7);
            ChangeAmountTextBox.PasswordChar = false;
            ChangeAmountTextBox.PlaceholderColor = Color.DarkGray;
            ChangeAmountTextBox.PlaceholderText = "";
            ChangeAmountTextBox.ReadOnly = false;
            ChangeAmountTextBox.Size = new Size(129, 37);
            ChangeAmountTextBox.TabIndex = 141;
            ChangeAmountTextBox.TextAlign = HorizontalAlignment.Center;
            ChangeAmountTextBox.Texts = "";
            ChangeAmountTextBox.UnderlinedStyle = true;
            // 
            // label4
            // 
            label4.BackColor = Color.FromArgb(38, 38, 38);
            label4.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label4.ForeColor = Color.Gainsboro;
            label4.Location = new Point(26, 91);
            label4.Name = "label4";
            label4.Size = new Size(57, 25);
            label4.TabIndex = 139;
            label4.Text = "จำนวน";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label45
            // 
            label45.BackColor = Color.FromArgb(38, 38, 38);
            label45.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label45.ForeColor = Color.Gainsboro;
            label45.Location = new Point(26, 44);
            label45.Name = "label45";
            label45.Size = new Size(70, 25);
            label45.TabIndex = 137;
            label45.Text = "ยอดรวม";
            label45.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // ChangesTotalLabel
            // 
            ChangesTotalLabel.BackColor = Color.FromArgb(38, 38, 38);
            ChangesTotalLabel.Font = new Font("FC Subject [Non-commercial] Reg", 18F);
            ChangesTotalLabel.ForeColor = Color.Aquamarine;
            ChangesTotalLabel.Location = new Point(102, 39);
            ChangesTotalLabel.Name = "ChangesTotalLabel";
            ChangesTotalLabel.Size = new Size(199, 30);
            ChangesTotalLabel.TabIndex = 136;
            ChangesTotalLabel.Text = "0.00";
            ChangesTotalLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label47
            // 
            label47.BackColor = Color.FromArgb(38, 38, 38);
            label47.Dock = DockStyle.Top;
            label47.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label47.ForeColor = Color.Gainsboro;
            label47.Location = new Point(0, 0);
            label47.Name = "label47";
            label47.Size = new Size(329, 39);
            label47.TabIndex = 87;
            label47.Text = "รายการ [ เงินทอน ]";
            label47.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel6
            // 
            panel6.BackColor = Color.FromArgb(38, 38, 38);
            panel6.Controls.Add(GeneralGoodsSaleLabel);
            panel6.Controls.Add(label14);
            panel6.Location = new Point(15, 117);
            panel6.Name = "panel6";
            panel6.Size = new Size(286, 81);
            panel6.TabIndex = 149;
            // 
            // GeneralGoodsSaleLabel
            // 
            GeneralGoodsSaleLabel.BackColor = Color.FromArgb(38, 38, 38);
            GeneralGoodsSaleLabel.Dock = DockStyle.Fill;
            GeneralGoodsSaleLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            GeneralGoodsSaleLabel.ForeColor = Color.PaleGreen;
            GeneralGoodsSaleLabel.Location = new Point(0, 29);
            GeneralGoodsSaleLabel.Name = "GeneralGoodsSaleLabel";
            GeneralGoodsSaleLabel.Size = new Size(286, 52);
            GeneralGoodsSaleLabel.TabIndex = 87;
            GeneralGoodsSaleLabel.Text = "0.00";
            GeneralGoodsSaleLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // label14
            // 
            label14.BackColor = Color.FromArgb(38, 38, 38);
            label14.Dock = DockStyle.Top;
            label14.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label14.ForeColor = Color.Gainsboro;
            label14.Location = new Point(0, 0);
            label14.Name = "label14";
            label14.Size = new Size(286, 29);
            label14.TabIndex = 83;
            label14.Text = "[ ยอดขาย ] สินค้าเบ็ดเตล็ด";
            label14.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel8
            // 
            panel8.BackColor = Color.FromArgb(38, 38, 38);
            panel8.Controls.Add(HardwareSaleLabel);
            panel8.Controls.Add(label28);
            panel8.Location = new Point(15, 204);
            panel8.Name = "panel8";
            panel8.Size = new Size(286, 81);
            panel8.TabIndex = 150;
            // 
            // HardwareSaleLabel
            // 
            HardwareSaleLabel.BackColor = Color.FromArgb(38, 38, 38);
            HardwareSaleLabel.Dock = DockStyle.Fill;
            HardwareSaleLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            HardwareSaleLabel.ForeColor = Color.Khaki;
            HardwareSaleLabel.Location = new Point(0, 29);
            HardwareSaleLabel.Name = "HardwareSaleLabel";
            HardwareSaleLabel.Size = new Size(286, 52);
            HardwareSaleLabel.TabIndex = 87;
            HardwareSaleLabel.Text = "0.00";
            HardwareSaleLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // label28
            // 
            label28.BackColor = Color.FromArgb(38, 38, 38);
            label28.Dock = DockStyle.Top;
            label28.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label28.ForeColor = Color.Gainsboro;
            label28.Location = new Point(0, 0);
            label28.Name = "label28";
            label28.Size = new Size(286, 29);
            label28.TabIndex = 83;
            label28.Text = "[ ยอดขาย ] สินค้าฮาร์ดแวร์";
            label28.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel11
            // 
            panel11.BackColor = Color.FromArgb(38, 38, 38);
            panel11.Controls.Add(CashPaymentLabel);
            panel11.Controls.Add(label30);
            panel11.Location = new Point(15, 291);
            panel11.Name = "panel11";
            panel11.Size = new Size(286, 81);
            panel11.TabIndex = 151;
            // 
            // CashPaymentLabel
            // 
            CashPaymentLabel.BackColor = Color.FromArgb(38, 38, 38);
            CashPaymentLabel.Dock = DockStyle.Fill;
            CashPaymentLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            CashPaymentLabel.ForeColor = Color.PeachPuff;
            CashPaymentLabel.Location = new Point(0, 29);
            CashPaymentLabel.Name = "CashPaymentLabel";
            CashPaymentLabel.Size = new Size(286, 52);
            CashPaymentLabel.TabIndex = 87;
            CashPaymentLabel.Text = "0.00";
            CashPaymentLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // label30
            // 
            label30.BackColor = Color.FromArgb(38, 38, 38);
            label30.Dock = DockStyle.Top;
            label30.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label30.ForeColor = Color.Gainsboro;
            label30.Location = new Point(0, 0);
            label30.Name = "label30";
            label30.Size = new Size(286, 29);
            label30.TabIndex = 83;
            label30.Text = "[ รายรับ ] เงินสด";
            label30.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel12
            // 
            panel12.BackColor = Color.FromArgb(38, 38, 38);
            panel12.Controls.Add(MoneyTransferPaymentLabel);
            panel12.Controls.Add(label34);
            panel12.Location = new Point(15, 378);
            panel12.Name = "panel12";
            panel12.Size = new Size(286, 81);
            panel12.TabIndex = 152;
            // 
            // MoneyTransferPaymentLabel
            // 
            MoneyTransferPaymentLabel.BackColor = Color.FromArgb(38, 38, 38);
            MoneyTransferPaymentLabel.Dock = DockStyle.Fill;
            MoneyTransferPaymentLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            MoneyTransferPaymentLabel.ForeColor = Color.SandyBrown;
            MoneyTransferPaymentLabel.Location = new Point(0, 29);
            MoneyTransferPaymentLabel.Name = "MoneyTransferPaymentLabel";
            MoneyTransferPaymentLabel.Size = new Size(286, 52);
            MoneyTransferPaymentLabel.TabIndex = 87;
            MoneyTransferPaymentLabel.Text = "0.00";
            MoneyTransferPaymentLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // label34
            // 
            label34.BackColor = Color.FromArgb(38, 38, 38);
            label34.Dock = DockStyle.Top;
            label34.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label34.ForeColor = Color.Gainsboro;
            label34.Location = new Point(0, 0);
            label34.Name = "label34";
            label34.Size = new Size(286, 29);
            label34.TabIndex = 83;
            label34.Text = "[ รายรับ ] เงินโอน";
            label34.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel13
            // 
            panel13.BackColor = Color.FromArgb(38, 38, 38);
            panel13.Controls.Add(WelfareCardPaymentLabel);
            panel13.Controls.Add(label46);
            panel13.Location = new Point(15, 465);
            panel13.Name = "panel13";
            panel13.Size = new Size(286, 81);
            panel13.TabIndex = 153;
            // 
            // WelfareCardPaymentLabel
            // 
            WelfareCardPaymentLabel.BackColor = Color.FromArgb(38, 38, 38);
            WelfareCardPaymentLabel.Dock = DockStyle.Fill;
            WelfareCardPaymentLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            WelfareCardPaymentLabel.ForeColor = Color.DarkSalmon;
            WelfareCardPaymentLabel.Location = new Point(0, 29);
            WelfareCardPaymentLabel.Name = "WelfareCardPaymentLabel";
            WelfareCardPaymentLabel.Size = new Size(286, 52);
            WelfareCardPaymentLabel.TabIndex = 87;
            WelfareCardPaymentLabel.Text = "0.00";
            WelfareCardPaymentLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // label46
            // 
            label46.BackColor = Color.FromArgb(38, 38, 38);
            label46.Dock = DockStyle.Top;
            label46.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label46.ForeColor = Color.Gainsboro;
            label46.Location = new Point(0, 0);
            label46.Name = "label46";
            label46.Size = new Size(286, 29);
            label46.TabIndex = 83;
            label46.Text = "[ รายรับ ] บัตรสวัสดิการ";
            label46.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel14
            // 
            panel14.BackColor = Color.FromArgb(38, 38, 38);
            panel14.Controls.Add(PayLaterForGeneralProductsLabel);
            panel14.Controls.Add(label2);
            panel14.Location = new Point(15, 552);
            panel14.Name = "panel14";
            panel14.Size = new Size(286, 81);
            panel14.TabIndex = 154;
            // 
            // PayLaterForGeneralProductsLabel
            // 
            PayLaterForGeneralProductsLabel.BackColor = Color.FromArgb(38, 38, 38);
            PayLaterForGeneralProductsLabel.Dock = DockStyle.Fill;
            PayLaterForGeneralProductsLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            PayLaterForGeneralProductsLabel.ForeColor = Color.MediumOrchid;
            PayLaterForGeneralProductsLabel.Location = new Point(0, 29);
            PayLaterForGeneralProductsLabel.Name = "PayLaterForGeneralProductsLabel";
            PayLaterForGeneralProductsLabel.Size = new Size(286, 52);
            PayLaterForGeneralProductsLabel.TabIndex = 87;
            PayLaterForGeneralProductsLabel.Text = "0.00";
            PayLaterForGeneralProductsLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // label2
            // 
            label2.BackColor = Color.FromArgb(38, 38, 38);
            label2.Dock = DockStyle.Top;
            label2.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label2.ForeColor = Color.Gainsboro;
            label2.Location = new Point(0, 0);
            label2.Name = "label2";
            label2.Size = new Size(286, 29);
            label2.TabIndex = 83;
            label2.Text = "[ รายรับ ] ยอดลงบัญชี (สินค้าเบ็ดเตล็ด)";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel15
            // 
            panel15.BackColor = Color.FromArgb(38, 38, 38);
            panel15.Controls.Add(PayLaterForHardwareProductsLabel);
            panel15.Controls.Add(label21);
            panel15.Location = new Point(15, 639);
            panel15.Name = "panel15";
            panel15.Size = new Size(286, 81);
            panel15.TabIndex = 155;
            // 
            // PayLaterForHardwareProductsLabel
            // 
            PayLaterForHardwareProductsLabel.BackColor = Color.FromArgb(38, 38, 38);
            PayLaterForHardwareProductsLabel.Dock = DockStyle.Fill;
            PayLaterForHardwareProductsLabel.Font = new Font("FC Subject [Non-commercial] Reg", 20.2499981F);
            PayLaterForHardwareProductsLabel.ForeColor = Color.RoyalBlue;
            PayLaterForHardwareProductsLabel.Location = new Point(0, 29);
            PayLaterForHardwareProductsLabel.Name = "PayLaterForHardwareProductsLabel";
            PayLaterForHardwareProductsLabel.Size = new Size(286, 52);
            PayLaterForHardwareProductsLabel.TabIndex = 87;
            PayLaterForHardwareProductsLabel.Text = "0.00";
            PayLaterForHardwareProductsLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // label21
            // 
            label21.BackColor = Color.FromArgb(38, 38, 38);
            label21.Dock = DockStyle.Top;
            label21.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label21.ForeColor = Color.Gainsboro;
            label21.Location = new Point(0, 0);
            label21.Name = "label21";
            label21.Size = new Size(286, 29);
            label21.TabIndex = 83;
            label21.Text = "[ รายรับ ] ยอดลงบัญชี (สินค้าฮาร์ดแวร์)";
            label21.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel16
            // 
            panel16.BackColor = Color.FromArgb(38, 38, 38);
            panel16.Controls.Add(label22);
            panel16.Controls.Add(TotalPayLaterLabel);
            panel16.Controls.Add(label29);
            panel16.Controls.Add(PayLaterPaymentsListView);
            panel16.Location = new Point(307, 617);
            panel16.Name = "panel16";
            panel16.Size = new Size(352, 289);
            panel16.TabIndex = 156;
            // 
            // label22
            // 
            label22.BackColor = Color.FromArgb(38, 38, 38);
            label22.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label22.ForeColor = Color.Gainsboro;
            label22.Location = new Point(26, 50);
            label22.Name = "label22";
            label22.Size = new Size(70, 25);
            label22.TabIndex = 137;
            label22.Text = "ยอดรวม";
            label22.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // TotalPayLaterLabel
            // 
            TotalPayLaterLabel.BackColor = Color.FromArgb(38, 38, 38);
            TotalPayLaterLabel.Font = new Font("FC Subject [Non-commercial] Reg", 18F);
            TotalPayLaterLabel.ForeColor = Color.LightPink;
            TotalPayLaterLabel.Location = new Point(102, 45);
            TotalPayLaterLabel.Name = "TotalPayLaterLabel";
            TotalPayLaterLabel.Size = new Size(199, 30);
            TotalPayLaterLabel.TabIndex = 136;
            TotalPayLaterLabel.Text = "0.00";
            TotalPayLaterLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label29
            // 
            label29.BackColor = Color.FromArgb(38, 38, 38);
            label29.Dock = DockStyle.Top;
            label29.Font = new Font("FC Subject [Non-commercial] Reg", 12F);
            label29.ForeColor = Color.Gainsboro;
            label29.Location = new Point(0, 0);
            label29.Name = "label29";
            label29.Size = new Size(352, 39);
            label29.TabIndex = 87;
            label29.Text = "รายการ [ ลงบัญชี ]";
            label29.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // PayLaterPaymentsListView
            // 
            PayLaterPaymentsListView.BackColor = Color.FromArgb(38, 38, 38);
            PayLaterPaymentsListView.Font = new Font("FC Subject [Non-commercial] Reg", 9.749999F);
            PayLaterPaymentsListView.ForeColor = Color.Gainsboro;
            PayLaterPaymentsListView.Location = new Point(26, 85);
            PayLaterPaymentsListView.MultiSelect = false;
            PayLaterPaymentsListView.Name = "PayLaterPaymentsListView";
            PayLaterPaymentsListView.Size = new Size(299, 187);
            PayLaterPaymentsListView.TabIndex = 3;
            PayLaterPaymentsListView.UseCompatibleStateImageBehavior = false;
            // 
            // CashFlowCalculatorPanel
            // 
            AutoScaleDimensions = new SizeF(11F, 29F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(panel16);
            Controls.Add(panel15);
            Controls.Add(panel14);
            Controls.Add(panel13);
            Controls.Add(panel12);
            Controls.Add(panel11);
            Controls.Add(panel8);
            Controls.Add(panel6);
            Controls.Add(panel5);
            Controls.Add(label44);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(label49);
            Controls.Add(panel1);
            Controls.Add(panel7);
            Controls.Add(panel9);
            Controls.Add(panel10);
            Controls.Add(DiffCashPanel);
            Font = new Font("FC Subject [Non-commercial] Reg", 14.25F);
            Margin = new Padding(6, 7, 6, 7);
            Name = "CashFlowCalculatorPanel";
            Size = new Size(1746, 925);
            DiffCashPanel.ResumeLayout(false);
            panel10.ResumeLayout(false);
            panel9.ResumeLayout(false);
            panel7.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel6.ResumeLayout(false);
            panel8.ResumeLayout(false);
            panel11.ResumeLayout(false);
            panel12.ResumeLayout(false);
            panel13.ResumeLayout(false);
            panel14.ResumeLayout(false);
            panel15.ResumeLayout(false);
            panel16.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private ListView ChangesListView;
        private ListView PayoutsListView;
        private ListView ReceivedPayLaterPaymentsListView;
        private Label Coin1CountTotalLabel_;
        private Label Coin2CountTotalLabel_;
        private Label Coin5CountTotalLabel_;
        private Label Coin10CountTotalLabel_;
        private Label HardwareSaleLabel;
        private Label label28;
        private Label CashPaymentLabel;
        private TextBox Coin1CountTextBox_;
        private TextBox Coin2CountTextBox_;
        private TextBox Coin5CountTextBox_;
        private TextBox Coin10CountTextBox_;
        private Label label30;
        private Label MoneyTransferPaymentLabel;
        private Label label34;
        private Label DateLabel;
        private ModernUI.ModernButton ResetCountingButton;
        private Panel DiffCashPanel;
        private Label DiffCashLabel;
        private Label DiffCashDescriptionLabel;
        private Label label37;
        private Panel panel10;
        private Label CalculatedCashTotalLabel;
        private Label label32;
        private Panel panel9;
        private Label ActualCashTotalLabel;
        private Label label26;
        private Panel panel7;
        private Label label24;
        private Label Coin1CountTotalLabel;
        private Label label15;
        private Label Coin2CountTotalLabel;
        private Label label17;
        private Label Coin5CountTotalLabel;
        private Label label19;
        private Label Coin10CountTotalLabel;
        private Label label23;
        private Label label25;
        private Label label36;
        private Label label38;
        private ModernUI.ModernTextBox Coin1CountTextBox;
        private ModernUI.ModernTextBox Coin2CountTextBox;
        private ModernUI.ModernTextBox Coin5CountTextBox;
        private ModernUI.ModernTextBox Coin10CountTextBox;
        private Panel panel1;
        private ModernUI.ModernTextBox BankNote20CountTextBox;
        private ModernUI.ModernTextBox BankNote50CountTextBox;
        private ModernUI.ModernTextBox BankNote100CountTextBox;
        private ModernUI.ModernTextBox BankNote500CountTextBox;
        private ModernUI.ModernTextBox BankNote1000CountTextBox;
        private Label label9;
        private Label label16;
        private Label label18;
        private Label label20;
        private Label label39;
        private Label label40;
        private Label label41;
        private Label label42;
        private Label label43;
        private Label BankNote1000CountTotalLabel;
        private Label BankNote500CountTotalLabel;
        private Label BankNote100CountTotalLabel;
        private Label BankNote50CountTotalLabel;
        private Label BankNote20CountTotalLabel;
        private Label label49;
        private ModernUI.ModernButton SaveToFileButton;
        private ModernUI.ModernButton PullLatestDataFromDatabaseButton;
        private Panel panel2;
        private Panel panel3;
        private ModernUI.ModernButton RemoveReceivedPayLaterPaymentButton;
        private ModernUI.ModernButton AddReceivedPayLaterPaymentButton;
        private ModernUI.ModernTextBox ReceivedPayLaterAmountTextBox;
        private ModernUI.ModernTextBox PayLaterAccountNameTextBox;
        private Label label10;
        private Label label8;
        private Label label5;
        private Label ReceivedPayLaterPaymentsTotalLabel;
        private Label label3;
        private Panel panel4;
        private ModernUI.ModernButton RemovePayOutButton;
        private ModernUI.ModernButton AddPayOutButton;
        private ModernUI.ModernTextBox PayOutAmountTextBox;
        private ModernUI.ModernTextBox PayOutItemTextBox;
        private Label label6;
        private Label label7;
        private Label label11;
        private Label PayoutsTotalLabel;
        private Label label13;
        private Label label44;
        private Panel panel5;
        private ModernUI.ModernButton RemoveChangeButton;
        private ModernUI.ModernButton AddChangeButton;
        private ModernUI.ModernTextBox ChangeAmountTextBox;
        private Label label4;
        private Label label45;
        private Label ChangesTotalLabel;
        private Label label47;
        private Panel panel6;
        private Label GeneralGoodsSaleLabel;
        private Label label14;
        private Panel panel8;
        private Panel panel11;
        private Panel panel12;
        private Panel panel13;
        private Label WelfareCardPaymentLabel;
        private Label label46;
        private Panel panel14;
        private Label PayLaterForGeneralProductsLabel;
        private Label label2;
        private Panel panel15;
        private Label PayLaterForHardwareProductsLabel;
        private Label label21;
        private Panel panel16;
        private Label label22;
        private Label TotalPayLaterLabel;
        private Label label29;
        private ListView PayLaterPaymentsListView;
    }
}
