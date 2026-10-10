using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshSidebar : DshControl
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, Dsh.SidebarTop, Dsh.SidebarBottom, LinearGradientMode.Vertical))
            {
                graphics.FillRectangle(brush, rect);
            }
            using (LinearGradientBrush brush2 = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, base.Width), Math.Max(1, base.Height / 3)), Color.FromArgb(14, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
            {
                graphics.FillRectangle(brush2, new Rectangle(0, 0, base.Width, base.Height / 3));
            }
            using (SolidBrush brush3 = new SolidBrush(Dsh.SidebarEdge))
            {
                graphics.FillRectangle(brush3, new Rectangle(base.Width - 1, 0, 1, base.Height));
            }
        }
    }
}
