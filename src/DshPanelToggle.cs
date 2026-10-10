using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshPanelToggle : DshControl
    {
        private bool hover;

        private bool down;

        private bool active;

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

        public DshPanelToggle()
        {
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
            base.TabStop = true;
        }

        public void PerformClick()
        {
            if (Enabled) OnClick(EventArgs.Empty);
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
            down = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                down = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            down = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            Rectangle rectangle = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
            GlassSurface.Button(graphics, rectangle, ParentBack().GetBrightness() < 0.45f, active, hover, down, Enabled);
            int num = Math.Min(18, Math.Min(base.Width, base.Height) - 10);
            if (num < 10)
            {
                num = 10;
            }
            Rectangle box = new Rectangle((base.Width - num) / 2, (base.Height - num) / 2, num, num);
            Color c = ((hover || down) ? Dsh.TextPrimary : Dsh.TextSecondary);
            DshIcons.PanelRight(graphics, box, c, 1.5f, active);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Return)
            {
                e.Handled = true;
                PerformClick();
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
    }
}
