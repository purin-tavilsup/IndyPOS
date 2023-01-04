using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace IndyPOS.Windows.Forms.UI.ModernUI;

public class ModernComboBox : UserControl
{
	private Color _backColor = Color.FromArgb(38, 38, 38);
	private Color _iconColor = Color.Gainsboro;
	private Color _listBackColor = Color.FromArgb(38, 38, 38);
	private Color _listTextColor = Color.Gainsboro;
	private Color _borderColor = Color.DimGray;
	private int _borderSize = 1;

	private readonly ComboBox _cmbList;
	private readonly Label _lblText;
	private readonly Button _btnIcon;

	public event EventHandler SelectedIndexChanged;

	[Category("Modern UI")]
	public new Color BackColor
	{
		get { return _backColor; }
		set
		{
			_backColor = value;
			_lblText.BackColor = _backColor;
			_btnIcon.BackColor = _backColor;
			_cmbList.BackColor = _backColor;
		}
	}

	[Category("Modern UI")]
	public Color IconColor
	{
		get { return _iconColor; }
		set
		{
			_iconColor = value;
			_btnIcon.Invalidate(); //Redraw icon
		}
	}

	[Category("Modern UI")]
	public Color ListBackColor
	{
		get { return _listBackColor; }
		set
		{
			_listBackColor = value;
			_cmbList.BackColor = _listBackColor;
		}
	}

	[Category("Modern UI")]
	public Color ListTextColor
	{
		get { return _listTextColor; }
		set
		{
			_listTextColor = value;
			_cmbList.ForeColor = _listTextColor;
		}
	}

	[Category("Modern UI")]
	public Color BorderColor
	{
		get { return _borderColor; }
		set
		{
			_borderColor = value;
			base.BackColor = _borderColor; //Border Color
		}
	}

	[Category("Modern UI")]
	public int BorderSize
	{
		get { return _borderSize; }
		set
		{
			_borderSize = value;
			Padding = new Padding(_borderSize); //Border Size
			AdjustComboBoxDimensions();
		}
	}

	[Category("Modern UI")]
	public override Color ForeColor
	{
		get { return base.ForeColor; }
		set
		{
			base.ForeColor = value;
			_lblText.ForeColor = value;
		}
	}

	[Category("Modern UI")]
	public override Font Font
	{
		get { return base.Font; }
		set
		{
			base.Font = value;
			_lblText.Font = value;
			_cmbList.Font = value; //Optional
		}
	}

	[Category("Modern UI")]
	public string Texts
	{
		get { return _lblText.Text; }
		set { _lblText.Text = value; }
	}

	[Category("Modern UI")]
	public ComboBoxStyle DropDownStyle
	{
		get { return _cmbList.DropDownStyle; }
		set
		{
			if (_cmbList.DropDownStyle != ComboBoxStyle.Simple)
				_cmbList.DropDownStyle = value;
		}
	}

	[Category("Modern UI")]
	public ComboBox.ObjectCollection Items
	{
		get { return _cmbList.Items; }
	}

	[Category("Modern UI")]
	public object SelectedItem
	{
		get { return _cmbList.SelectedItem; }
		set { _cmbList.SelectedItem = value; }
	}

	[Category("Modern UI")]
	public int SelectedIndex
	{
		get { return _cmbList.SelectedIndex; }
		set { _cmbList.SelectedIndex = value; }
	}

	[Category("Modern UI")]
	[DefaultValue("")]
	public string DisplayMember
	{
		get { return _cmbList.DisplayMember; }
		set { _cmbList.DisplayMember = value; }
	}

	[Category("Modern UI")]
	[DefaultValue("")]
	public string ValueMember
	{
		get { return _cmbList.ValueMember; }
		set { _cmbList.ValueMember = value; }
	}

	public ModernComboBox()
	{
		_cmbList = new ComboBox();
		_lblText = new Label();
		_btnIcon = new Button();
		SuspendLayout();

		_cmbList.BackColor = _backColor;
		_cmbList.Font = new Font(Font.Name, 12F);
		_cmbList.ForeColor = _listTextColor;
		_cmbList.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
		_cmbList.TextChanged += ComboBox_TextChanged;

		_btnIcon.Dock = DockStyle.Right;
		_btnIcon.FlatStyle = FlatStyle.Flat;
		_btnIcon.FlatAppearance.BorderSize = 0;
		_btnIcon.BackColor = _backColor;
		_btnIcon.Size = new Size(45, 35);
		_btnIcon.Cursor = Cursors.Hand;
		_btnIcon.Click += Icon_Click;
		_btnIcon.Paint += Icon_Paint;

		_lblText.Dock = DockStyle.Fill;
		_lblText.AutoSize = false;
		_lblText.BackColor = _backColor;
		_lblText.TextAlign = ContentAlignment.MiddleCenter;
		_lblText.Padding = new Padding(8, 0, 0, 0);
		_lblText.Font = new Font(Font.Name, 12F);
		_lblText.Click += Surface_Click;
		_lblText.MouseEnter += new EventHandler(Surface_MouseEnter);
		_lblText.MouseLeave += new EventHandler(Surface_MouseLeave);

		Controls.Add(_lblText);
		Controls.Add(_btnIcon);
		Controls.Add(_cmbList);
		MinimumSize = new Size(200, 35);
		Size = new Size(200, 35);
		ForeColor = _listTextColor;
		Padding = new Padding(_borderSize);
		Font = new Font(Font.Name, 12F);
		base.BackColor = _borderColor;
		ResumeLayout();

		AdjustComboBoxDimensions();
	}

	private void Surface_MouseLeave(object sender, EventArgs e)
	{
		OnMouseLeave(e);
	}

	private void Surface_MouseEnter(object sender, EventArgs e)
	{
		OnMouseEnter(e);
	}

	private void AdjustComboBoxDimensions()
	{
		_cmbList.Width = _lblText.Width;
		_cmbList.Location = new Point()
		{
			X = Width           - Padding.Right - _cmbList.Width,
			Y = _lblText.Bottom - _cmbList.Height
		};
	}

	private void Surface_Click(object sender, EventArgs e)
	{
		//Attach label click to user control click
		OnClick(e);

		_cmbList.Select();

		if (_cmbList.DropDownStyle == ComboBoxStyle.DropDownList)
		{
			// Open dropdown list
			_cmbList.DroppedDown = true;
		}
	}

	private void Icon_Paint(object sender, PaintEventArgs e)
	{
		int iconWidht = 14;
		int iconHeight = 6;
		var rectIcon = new Rectangle((_btnIcon.Width - iconWidht) / 2, (_btnIcon.Height - iconHeight) / 2, iconWidht, iconHeight);
		Graphics graph = e.Graphics;
			
		//Draw arrow down icon
		using (GraphicsPath path = new GraphicsPath())
		using (Pen pen = new Pen(_iconColor, 2))
		{
			graph.SmoothingMode = SmoothingMode.AntiAlias;
			path.AddLine(rectIcon.X, rectIcon.Y, rectIcon.X + (iconWidht / 2), rectIcon.Bottom);
			path.AddLine(rectIcon.X + (iconWidht / 2), rectIcon.Bottom, rectIcon.Right, rectIcon.Y);
			graph.DrawPath(pen, path);
		}
	}

	private void Icon_Click(object sender, EventArgs e)
	{
		_cmbList.Select();
		_cmbList.DroppedDown = true;
	}

	private void ComboBox_TextChanged(object sender, EventArgs e)
	{
		// Refresh text
		_lblText.Text = _cmbList.Text;
	}

	private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (SelectedIndexChanged != null)
			SelectedIndexChanged.Invoke(sender, e);

		// Refresh text
		_lblText.Text = _cmbList.Text;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		AdjustComboBoxDimensions();
	}
}