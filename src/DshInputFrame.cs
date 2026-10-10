using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab
{
    // Keep native editing/IME support while drawing a unified rounded outer surface.
    internal sealed class DshInputFrame : DshControl
    {
        private readonly TextBox editor;
        internal bool GlassLook;
        internal DshInputFrame(TextBox input)
        {
            editor = input;
            editor.BorderStyle = BorderStyle.None;
            Controls.Add(editor);
            editor.GotFocus += delegate { Invalidate(); };
            editor.LostFocus += delegate { Invalidate(); };
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GlassSurface.ApplyRoundedRegion(this);
            if (editor != null) editor.SetBounds(12, 9, Math.Max(1, Width - 24), Math.Max(1, Height - 18));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            graphics.Clear(ParentBack());
            if (Width <= 1 || Height <= 1) return;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            Dsh.FillRound(graphics, bounds, Dsh.ButtonRadius(bounds), editor.BackColor);
            if (GlassLook && bounds.Width > 24)
            {
                using (var shine = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0,0,Width,6),Color.FromArgb(40,Dsh.GlassHighlight),Color.FromArgb(0,Dsh.GlassHighlight),90f))
                    graphics.FillRectangle(shine,12,1,Math.Max(1,Width-24),5);
            }
            Dsh.StrokeRound(graphics, bounds, Dsh.ButtonRadius(bounds), editor.Focused ? Dsh.BorderFocus : Dsh.BorderInput, 1f);
        }
        protected override void OnClick(EventArgs e) { editor.Focus(); base.OnClick(e); }
    }
}
