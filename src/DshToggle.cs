using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshToggle : DshControl
    {
        private bool hover;

        private bool isChecked;

        public bool Checked
        {
            get
            {
                return isChecked;
            }
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    OnCheckedChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public event EventHandler CheckedChanged;

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            if (this.CheckedChanged != null)
            {
                this.CheckedChanged(this, e);
            }
        }

        public DshToggle(string text)
        {
            Text = text;
            base.Height = 26;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            isChecked = !isChecked;
            Invalidate();
            base.OnClick(e);
            OnCheckedChanged(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            Rectangle r = new Rectangle(0, (base.Height - 18) / 2, 18, 18);
            if (isChecked)
            {
                Dsh.FillRound(graphics, r, Dsh.ButtonRadius(r), Dsh.PrimaryFill);
            }
            else
            {
                Dsh.FillRound(graphics, r, Dsh.ButtonRadius(r), hover ? Dsh.GlassFill : Dsh.GlassFillStrong);
                Dsh.StrokeRound(graphics, r, Dsh.ButtonRadius(r), hover ? Dsh.BorderFocus : Dsh.BorderInput, 1f);
            }
            if (isChecked)
            {
                using (Pen pen = new Pen(Color.White, 1.9f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    graphics.DrawLines(pen, new Point[3]
                    {
                        new Point(r.X + 5, r.Y + 9),
                        new Point(r.X + 8, r.Y + 12),
                        new Point(r.X + 13, r.Y + 6)
                    });
                }
            }
            Dsh.DrawText(graphics, Text, Dsh.Body, Dsh.TextPrimary, new Rectangle(28, 0, base.Width - 30, base.Height), ContentAlignment.MiddleLeft);
        }
    }
}
