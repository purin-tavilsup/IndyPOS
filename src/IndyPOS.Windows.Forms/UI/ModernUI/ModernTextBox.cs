using System.ComponentModel;
using System.Runtime.Versioning;

namespace IndyPOS.Windows.Forms.UI.ModernUI;

[type:SupportedOSPlatform("windows")]
[DefaultEvent("ModernTextChanged")]
public partial class ModernTextBox: UserControl
{
	private Color _borderColor = Color.MidnightBlue;
	private int _borderSize = 2;
	private bool _underlinedStyle = false;
	private Color _borderFocusColor = Color.HotPink;
	private bool _isFocused = false;

	private int _borderRadius = 0;
	private Color _placeholderColor = Color.DarkGray;
	private string _placeholderText = "";
	private bool _isPlaceholder = false;
	private bool _isPasswordChar = false;

	//Events
	public event EventHandler ModernTextChanged;

	public ModernTextBox()
	{
		InitializeComponent();
	}

	[Category("Modern UI")]
	public Color PlaceholderColor
	{
		get { return _placeholderColor; }
		set
		{
			_placeholderColor = value;
			if (_isPlaceholder)
				textBox1.ForeColor = value;
		}
	}

	[Category("Modern UI")]
	public string PlaceholderText
	{
		get { return _placeholderText; }
		set
		{
			_placeholderText = value;
			textBox1.Text = "";
			SetPlaceholder();
		}
	}

	[Category("Modern UI")]
	public Color BorderColor
	{
		get => _borderColor;

		set
		{
			_borderColor = value;
			Invalidate();
		}
	}

	[Category("Modern UI")]
	public int BorderSize
	{
		get => _borderSize;

		set
		{
			_borderSize = value;
			Invalidate();
		}
	}

	[Category("Modern UI")]
	public bool UnderlinedStyle
	{
		get => _underlinedStyle;

		set
		{
			_underlinedStyle = value;
			Invalidate();
		}
	}

	[Category("Modern UI")]
	public bool PasswordChar
	{
		get => textBox1.UseSystemPasswordChar;
		set => textBox1.UseSystemPasswordChar = value;
	}

	[Category("Modern UI")]
	public bool Multiline
	{
		get => textBox1.Multiline;
		set => textBox1.Multiline = value;
	}

	[Category("Modern UI")]
	public override Color BackColor
	{
		get => base.BackColor;

		set
		{
			base.BackColor = value;
			textBox1.BackColor = value;
		}
	}

	[Category("Modern UI")]
	public override Color ForeColor
	{
		get => base.ForeColor;

		set
		{
			base.ForeColor = value;
			textBox1.ForeColor = value;
		}
	}

	[Category("Modern UI")]
	public override Font Font
	{
		get => base.Font;

		set
		{
			base.Font = value;
			textBox1.Font = value;

			if (DesignMode)
				UpdateControlHeight();
		}
	}

	[Category("Modern UI")]
	public string Texts
	{
		get
		{
			if (_isPlaceholder)
			{
				return "";
			}

			return textBox1.Text;
		}
		set
		{
			textBox1.Text = value;

			SetPlaceholder();
		}
	}

	[Category("Modern UI")]
	public bool ReadOnly
	{
		get => textBox1.ReadOnly;
		set => textBox1.ReadOnly = value;
	}

	[Category("Modern UI")]
	public HorizontalAlignment TextAlign
	{
		get => textBox1.TextAlign;
		set => textBox1.TextAlign = value;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Graphics graph = e.Graphics;

		using (Pen penBorder = new Pen(_borderColor, _borderSize))
		{
			penBorder.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

			if (_underlinedStyle)
			{
				graph.DrawLine(penBorder, 0, Height - 1, Width, Height - 1);
			}
			else
			{
				graph.DrawRectangle(penBorder, 0, 0, Width - 0.5F, Height - 0.5F);
			}
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);

		if (DesignMode)
			UpdateControlHeight();
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		UpdateControlHeight();
	}

	private void SetPlaceholder()
	{
		if (string.IsNullOrWhiteSpace(textBox1.Text) && _placeholderText != "")
		{
			_isPlaceholder = true;
			textBox1.Text = _placeholderText;
			textBox1.ForeColor = _placeholderColor;
			if (_isPasswordChar)
				textBox1.UseSystemPasswordChar = false;
		}
	}
	private void RemovePlaceholder()
	{
		if (_isPlaceholder && _placeholderText != "")
		{
			_isPlaceholder = false;
			textBox1.Text = "";
			textBox1.ForeColor = this.ForeColor;
			if (_isPasswordChar)
				textBox1.UseSystemPasswordChar = true;
		}
	}      

	private void UpdateControlHeight()
	{
		if (textBox1.Multiline == false)
		{
			var textHeight = TextRenderer.MeasureText("Text", Font).Height + 1;

			textBox1.Multiline = true;
			textBox1.MinimumSize = new Size(0, textHeight);
			textBox1.Multiline = false;

			Height = textBox1.Height + Padding.Top + Padding.Bottom;
		}
	}

	private void textBox1_TextChanged(object sender, EventArgs e)
	{
		if (ModernTextChanged is not null)
		{
			ModernTextChanged.Invoke(sender, e);
		}
	}
	private void textBox1_Click(object sender, EventArgs e)
	{
		OnClick(e);
	}
	private void textBox1_MouseEnter(object sender, EventArgs e)
	{
		OnMouseEnter(e);
	}
	private void textBox1_MouseLeave(object sender, EventArgs e)
	{
		OnMouseLeave(e);
	}
	private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
	{
		OnKeyPress(e);
	}

	private void textBox1_Enter(object sender, EventArgs e)
	{
		_isFocused = true;
		Invalidate();
		RemovePlaceholder();
	}
	private void textBox1_Leave(object sender, EventArgs e)
	{
		_isFocused = false;
		Invalidate();
		SetPlaceholder();
	}
}