using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshButton : DshControl
    {
        public enum Kind
        {
            Primary,
            Secondary,
            Ghost,
            Danger,
            OnDark
        }

        private Kind kind = Kind.Secondary;

        private bool hover;

        private bool down;
        internal bool StrongOutline;

        public Kind Style
        {
            get
            {
                return kind;
            }
            set
            {
                kind = value;
                Invalidate();
            }
        }

        public void PerformClick()
        {
            if (Enabled) OnClick(EventArgs.Empty);
        }

        public DshButton(string text)
            : this(text, Kind.Secondary)
        {
        }

        public DshButton(string text, Kind k)
        {
            Text = UiText.T(text);
            kind = k;
            base.Height = 34;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
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
            down = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            down = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            { down = true; Invalidate(); e.Handled = true; e.SuppressKeyPress = true; }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (Enabled && down && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            { down = false; Invalidate(); PerformClick(); e.Handled = true; e.SuppressKeyPress = true; }
            base.OnKeyUp(e);
        }

        protected override void OnLostFocus(EventArgs e) { down = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = Setup(e.Graphics);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            bool dark = ParentBack().GetBrightness() < 0.45f;
            bool primary = kind == Kind.Primary || kind == Kind.OnDark;
            GlassSurface.Button(g, r, dark, primary, hover, down, Enabled,StrongOutline);
            Color color = kind == Kind.Danger ? Dsh.DangerText : (primary ? Color.White : (dark ? Color.FromArgb(240, 242, 247) : Dsh.TextPrimary));
            if (!Enabled) color = Color.FromArgb(125, 130, 140);
            Dsh.DrawText(g, Text, primary ? Dsh.Medium : Dsh.Body, color,
                new Rectangle(0, down ? 1 : 0, Width, Height - 1), ContentAlignment.MiddleCenter);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -5, -5), color, ParentBack());
        }
    }
}
