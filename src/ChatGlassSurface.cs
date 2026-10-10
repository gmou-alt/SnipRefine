using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace SnapGrab
{
    internal sealed class ChatGlassSurface : DshControl
    {
        internal ChatGlassSurface() { BackColor = Dsh.Canvas; TabStop = false; }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = Setup(e.Graphics); g.Clear(BackColor);
            if (Width <= 1 || Height <= 1) return;
            Rectangle area = new Rectangle(0,0,Width-1,Height-1);
            Dsh.FillRound(g,area,16,Dsh.GlassFill);
            using (GraphicsPath clip = Dsh.Round(area,16))
            {
                GraphicsState state=g.Save();
                try
                {
                    g.SetClip(clip,CombineMode.Intersect);
                    using (var glow = new LinearGradientBrush(area,Color.FromArgb(32,Dsh.PrimaryFill),Color.FromArgb(0,Dsh.PrimaryFill),LinearGradientMode.Vertical))
                        g.FillRectangle(glow,area);
                }
                finally { g.Restore(state); }
            }
            Dsh.StrokeRound(g,area,16,Color.FromArgb(42,Dsh.PrimaryFill),1);
            using (Pen highlight = new Pen(Dsh.GlassHighlight,1)) g.DrawLine(highlight,16,1,Width-17,1);
        }
    }
}
