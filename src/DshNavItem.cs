using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshNavItem : DshControl
    {
        private bool hover;

        private bool active;

        public object Tag2;

        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (active != value)
                {
                    active = value;
                    Invalidate();
                }
            }
        }

        public void PerformClick()
        {
            OnClick(EventArgs.Empty);
        }

        public DshNavItem(string text)
        {
            Text = text;
            base.Height = 36;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = Setup(e.Graphics);
            Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
            if (active)
            {
                Dsh.FillRound(g, r, Dsh.ButtonRadius(r), Dsh.GlassOnDarkHover);
                Dsh.StrokeRound(g, r, Dsh.ButtonRadius(r), Dsh.GlassOnDarkEdge, 1f);
            }
            else if (hover)
            {
                Dsh.FillRound(g, r, Dsh.ButtonRadius(r), Dsh.GlassOnDark);
            }
            if (active)
            {
                Dsh.FillRound(g, new Rectangle(0, 9, 3, base.Height - 19), 2, Dsh.BorderFocus);
            }
            Color color = (active ? Dsh.OnDark : Dsh.OnDarkMuted);
            Dsh.DrawText(g, Text, active ? Dsh.Medium : Dsh.Body, color, new Rectangle(15, 0, base.Width - 23, base.Height), ContentAlignment.MiddleLeft);
        }
    }
}
