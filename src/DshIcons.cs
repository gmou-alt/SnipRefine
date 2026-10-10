using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SnapGrab
{
    internal static class DshIcons
    {
        public static void PanelRight(Graphics g, Rectangle box, Color c, float stroke, bool active)
        {
            RectangleF r = new RectangleF((float)box.X + stroke / 2f, (float)box.Y + stroke / 2f, (float)box.Width - stroke, (float)box.Height - stroke);
            float num = Math.Max(2f, (float)box.Width * 0.16f);
            float num2 = r.X + r.Width * 0.62f;
            using (Pen pen = new Pen(c, stroke))
            {
                pen.LineJoin = LineJoin.Round;
                using (GraphicsPath path = RoundRect(r, num))
                {
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, num2, r.Y + 1f, num2, r.Bottom - 1f);
            }
            if (!active)
            {
                return;
            }
            RectangleF r2 = new RectangleF(num2 + stroke / 2f, r.Y + stroke / 2f, r.Right - num2 - stroke, r.Height - stroke);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(46, c)))
            {
                using (GraphicsPath path2 = RoundRect(r2, Math.Max(1f, num - stroke)))
                {
                    g.FillPath(brush, path2);
                }
            }
        }

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            GraphicsPath graphicsPath = new GraphicsPath();
            float num = Math.Max(1f, radius * 2f);
            if (num > r.Width)
            {
                num = r.Width;
            }
            if (num > r.Height)
            {
                num = r.Height;
            }
            graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
            graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
            graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
            graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
            graphicsPath.CloseFigure();
            return graphicsPath;
        }
    }
}
