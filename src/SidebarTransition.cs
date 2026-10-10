using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal sealed class SidebarTransition : DshControl
    {
        internal SidebarTransition() { TabStop = false; Enabled = false; }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;
            e.Graphics.Clear(Dsh.Canvas);
            Rectangle area = ClientRectangle;
            // Use the same blue accent as the app, with matching canvas colors
            // at both ends so this never becomes a grey separator strip.
            Color accent = Color.FromArgb(
                (Dsh.Canvas.R * 92 + Dsh.PrimaryFill.R * 8) / 100,
                (Dsh.Canvas.G * 92 + Dsh.PrimaryFill.G * 8) / 100,
                (Dsh.Canvas.B * 92 + Dsh.PrimaryFill.B * 8) / 100);
            using (var blend = new LinearGradientBrush(area, Dsh.Canvas, Dsh.Canvas, LinearGradientMode.Horizontal))
            {
                blend.InterpolationColors = new ColorBlend {
                    Colors = new Color[] { Dsh.Canvas, accent, Dsh.Canvas },
                    Positions = new float[] { 0, 0.45f, 1 }
                };
                e.Graphics.FillRectangle(blend, area);
            }
        }
    }
}
