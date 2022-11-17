using System.ComponentModel;

namespace ModernUI;

public partial class ModernTextBox: UserControl
{
	private Color _borderColor = Color.MidnightBlue;
	private int _borderSize = 2;
	private bool _underlinedStyle = false;

	public ModernTextBox()
	{
		InitializeComponent();
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
		get => textBox1.Text;
		set => textBox1.Text = value;
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
}