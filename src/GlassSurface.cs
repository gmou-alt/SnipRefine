using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal static class GlassSurface
    {
        internal static void ApplyRoundedRegion(Control control)
        {
            if(control.Width<2||control.Height<2)return;
            Rectangle bounds=new Rectangle(0,0,control.Width,control.Height);
            Region old=control.Region;
            using(GraphicsPath path=Dsh.Round(bounds,Dsh.ButtonRadius(bounds)))control.Region=new Region(path);
            if(old!=null)old.Dispose();
        }
        internal static bool MotionEnabled
        {
            get { try { return SystemInformation.UIEffectsEnabled; } catch { return true; } }
        }

        internal static void Button(Graphics g, Rectangle bounds, bool dark, bool active, bool hover, bool pressed, bool enabled, bool strongOutline=false)
        {
            if (bounds.Width < 8 || bounds.Height < 8) return;
            Rectangle r = Rectangle.Inflate(bounds, -1, -1);

            int radius = Dsh.ButtonRadius(r);
            Color tint = active ? Color.FromArgb(70, 146, 211) : (dark ? Color.White : Color.FromArgb(119, 151, 182));
            int intensity = enabled ? (pressed ? 24 : hover ? 40 : 30) : 16;
            Color top = Color.FromArgb(active && !dark ? (enabled ? 235 : 95) : (active ? intensity + 82 : intensity), tint);
            Color bottom = Color.FromArgb(active && !dark ? (enabled ? 222 : 85) : (active ? intensity + 66 : Math.Max(10, intensity - 9)), tint);
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Dsh.Round(r, radius))
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical))
                    g.FillPath(fill, path);
                using (LinearGradientBrush highlight = new LinearGradientBrush(r,
                    Color.Transparent, Color.Transparent, LinearGradientMode.Vertical))
                {
                    // Full-height smooth falloff avoids the half-height gradient edge.
                    ColorBlend blend = new ColorBlend(4);
                    blend.Positions = new float[] { 0f, 0.28f, 0.72f, 1f };
                    blend.Colors = new Color[] {
                        Color.FromArgb(hover ? 12 : 8, Color.White),
                        Color.FromArgb(hover ? 8 : 5, Color.White),
                        Color.FromArgb(2, Color.White),
                        Color.FromArgb(0, Color.White) };
                    highlight.InterpolationColors = blend;
                    highlight.WrapMode = WrapMode.TileFlipY;
                    g.FillPath(highlight, path);
                }
                Color edgeColor=strongOutline && enabled ?
                    (active ? (dark ? Color.FromArgb(103,194,255) : Color.FromArgb(35,111,205)) :
                        (dark ? Color.FromArgb(155,178,210) : Color.FromArgb(86,107,138))) :
                    Color.FromArgb(enabled ? (hover ? 65 : 40) : 20,dark ? Color.White : Color.FromArgb(91,117,148));
                using (Pen edge = new Pen(edgeColor,strongOutline && enabled ? (hover || active ? 2f : 1.5f) : 1f))
                    g.DrawPath(edge, path);
            }
            g.SmoothingMode = old;
        }
    }

    // Fully custom painting avoids native Button theme/background repainting.
    internal class GlassButton : Control
    {
        internal bool Primary;
        private bool hover;
        private bool pressed;

        internal GlassButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.Selectable |
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Dsh.SidebarFlat;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            ForeColor = Dsh.TextPrimary;

            Cursor = Cursors.Hand;
            Font = Dsh.F(11.5f, FontStyle.Regular);
        }

        // Paint only our backing surface. Native transparent Button background
        // painting can reprint parent labels inside every child button.
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color background = BackColor;
            Control parent = Parent;
            while (parent != null)
            {
                if (parent.BackColor.A == 255) { background = parent.BackColor; break; }
                parent = parent.Parent;
            }
            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); GlassSurface.ApplyRoundedRegion(this); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) Focus(); pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { pressed = true; Invalidate(); } base.OnKeyDown(e); }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && pressed)
            { pressed = false; Invalidate(); PerformClick(); e.Handled = true; }
            base.OnKeyUp(e);
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Enter) { PerformClick(); return true; }
            return base.ProcessDialogKey(keyData);
        }
        protected override void OnLostFocus(EventArgs e) { pressed = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { hover = pressed = false; Invalidate(); base.OnEnabledChanged(e); }
        internal void PerformClick() { if (Enabled) OnClick(EventArgs.Empty); }

        protected override void OnPaint(PaintEventArgs e)
        {
            OnPaintBackground(e);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            GlassSurface.Button(e.Graphics, bounds, !Dsh.LightTheme, Primary, hover, pressed, Enabled, true);
            Dsh.DrawText(e.Graphics, Text, Font, Enabled ? (Primary ? Dsh.PrimaryText : Dsh.TextPrimary) : Dsh.TextTertiary,
                new Rectangle(0, pressed ? 1 : 0, Width, Height - 1), ContentAlignment.MiddleCenter);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -5, -5), ForeColor, BackColor);
        }
    }
}
