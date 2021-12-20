using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ModernUI
{
    public class ModernButton : Button
    {
        private int _borderSize = 0;
        private int _borderRadius = 40;
        private Color _borderColor = Color.PaleVioletRed;

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
        public int BorderRadius 
        {
            get => _borderRadius;

            set
            {
                if (value <= Height)
                {
                    _borderRadius = value;
                }
                else
                {
                    _borderRadius = Height;
                }
                
                Invalidate();
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
        public Color BackgroundColor
        {
            get => BackColor;
            set => BackColor = value;
        }

        [Category("Modern UI")]
        public Color TextColor
        {
            get => ForeColor;
            set => ForeColor = value;
        }

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Size = new Size(150, 40);
            BackColor = Color.MediumSlateBlue;
            ForeColor = Color.White;
            Resize += new EventHandler(Button_Resize);
        }

        private GraphicsPath GetFigurePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Width - radius, rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rectSurface = new RectangleF(0, 0, Width, Height);
            var rectBorder = new RectangleF(1, 1, Width - 0.8F, Height - 1);

            // Round Button
            if (_borderRadius > 2)
            {
                using (var pathSurface = GetFigurePath(rectSurface, _borderRadius))
                using (var pathBorder = GetFigurePath(rectBorder, _borderRadius -1F))
                using (var penSurface = new Pen(Parent.BackColor, 2))
                using (var penBorder = new Pen(_borderColor, _borderSize))
                {
                    penBorder.Alignment = PenAlignment.Inset;
                    
                    // Button surface
                    Region = new Region(pathSurface);

                    // Draw surface border
                    pevent.Graphics.DrawPath(penSurface, pathSurface);

                    // Button border
                    if (_borderSize >= 1)
                        pevent.Graphics.DrawPath(penBorder, pathBorder);
                }
            }
            else // Normal Button
            {
                // Button surface
                Region = new Region(rectSurface);

                // Button border
                if (_borderSize >= 1)
                {
                    using (var penBorder = new Pen(_borderColor, _borderSize))
                    {
                        penBorder.Alignment = PenAlignment.Inset;
                        pevent.Graphics.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
                    }
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            Parent.BackColorChanged += new EventHandler(Container_BackColorChanged);
        }

        private void Container_BackColorChanged(object sender, EventArgs e)
        {
            if (DesignMode)
                Invalidate();
        }

        private void Button_Resize(object sender, EventArgs e)
        {
            if (_borderRadius > Height)
                BorderRadius = Height;
        }
    }
}
