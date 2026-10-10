using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal static class Dsh
    {
        public const int RadiusXs = 4;

        public const int RadiusSm = 8;

        public const int RadiusMd = 10;

        public const int RadiusLg = 16;

        public const int RadiusPanel = 28;

        public static Color Canvas = Color.FromArgb(36, 39, 45);

        public static Color SidebarTop = Color.FromArgb(29, 32, 38);

        public static Color SidebarBottom = Color.FromArgb(25, 28, 33);

        public static Color SidebarFlat = Color.FromArgb(29, 32, 38);

        public static Color SidebarEdge = Color.FromArgb(50, 65, 87);

        public static Color TextPrimary = Color.FromArgb(238, 243, 252);

        public static Color TextSecondary = Color.FromArgb(188, 201, 222);

        public static Color TextTertiary = Color.FromArgb(163, 178, 201);

        public static Color TextCaption = Color.FromArgb(162, 177, 201);

        public static Color OnDark = Color.FromArgb(238, 243, 252);

        public static Color OnDarkMuted = Color.FromArgb(173, 188, 210);

        public static Color OnDarkFaint = Color.FromArgb(143, 159, 184);

        public static Color GlassFill = Color.FromArgb(45, 49, 57);

        public static Color GlassFillStrong = Color.FromArgb(54, 59, 69);

        public static Color GlassHighlight = Color.FromArgb(52, 220, 235, 255);

        public static Color GlassOnDark = Color.FromArgb(25, 140, 185, 255);

        public static Color GlassOnDarkHover = Color.FromArgb(45, 140, 185, 255);

        public static Color GlassOnDarkEdge = Color.FromArgb(65, 170, 200, 245);

        public static Color Hairline = Color.FromArgb(60, 145, 169, 205);

        public static Color HairlineSoft = Color.FromArgb(35, 145, 169, 205);

        public static Color BorderInput = Color.FromArgb(110, 125, 151, 189);

        public static Color BorderFocus = Color.FromArgb(140, 185, 255);

        public static Color Hover = Color.FromArgb(24, 140, 185, 255);

        public static Color Pressed = Color.FromArgb(40, 140, 185, 255);

        public static Color PrimaryFill = Color.FromArgb(60, 106, 184);

        public static Color PrimaryHover = Color.FromArgb(77, 124, 205);

        public static Color PrimaryText = Color.FromArgb(255, 255, 255);

        public static Color DangerText = Color.FromArgb(255, 150, 154);

        public static Color SuccessText = Color.FromArgb(119, 218, 177);

        public static bool LightTheme { get; private set; }

        public static void ApplyTheme(bool light, Control surface)
        {
            System.Collections.Generic.Dictionary<int, Color> colors =
                new System.Collections.Generic.Dictionary<int, Color>();
            colors[Canvas.ToArgb()] = light ? Color.FromArgb(245, 246, 248) : Color.FromArgb(36, 39, 45);
            colors[SidebarTop.ToArgb()] = light ? Color.FromArgb(239, 241, 245) : Color.FromArgb(29, 32, 38);
            colors[SidebarBottom.ToArgb()] = light ? Color.FromArgb(232, 235, 240) : Color.FromArgb(25, 28, 33);
            colors[SidebarFlat.ToArgb()] = light ? Color.FromArgb(238, 240, 244) : Color.FromArgb(29, 32, 38);
            colors[SidebarEdge.ToArgb()] = light ? Color.FromArgb(197, 212, 232) : Color.FromArgb(50, 65, 87);
            colors[TextPrimary.ToArgb()] = light ? Color.FromArgb(30, 47, 71) : Color.FromArgb(238, 243, 252);
            colors[TextSecondary.ToArgb()] = light ? Color.FromArgb(65, 85, 113) : Color.FromArgb(188, 201, 222);
            colors[TextTertiary.ToArgb()] = light ? Color.FromArgb(91, 110, 135) : Color.FromArgb(163, 178, 201);
            colors[TextCaption.ToArgb()] = light ? Color.FromArgb(91, 110, 135) : Color.FromArgb(162, 177, 201);
            colors[OnDark.ToArgb()] = light ? Color.FromArgb(30, 47, 71) : Color.FromArgb(238, 243, 252);
            colors[OnDarkMuted.ToArgb()] = light ? Color.FromArgb(65, 85, 113) : Color.FromArgb(173, 188, 210);
            colors[OnDarkFaint.ToArgb()] = light ? Color.FromArgb(91, 110, 135) : Color.FromArgb(143, 159, 184);
            colors[GlassFill.ToArgb()] = light ? Color.FromArgb(245, 249, 254) : Color.FromArgb(45, 49, 57);
            colors[GlassFillStrong.ToArgb()] = light ? Color.FromArgb(250, 252, 255) : Color.FromArgb(54, 59, 69);
            colors[GlassHighlight.ToArgb()] = light ? Color.FromArgb(90, 255, 255, 255) : Color.FromArgb(52, 220, 235, 255);
            colors[GlassOnDark.ToArgb()] = light ? Color.FromArgb(22, 83, 138, 205) : Color.FromArgb(25, 140, 185, 255);
            colors[GlassOnDarkHover.ToArgb()] = light ? Color.FromArgb(38, 83, 138, 205) : Color.FromArgb(45, 140, 185, 255);
            colors[GlassOnDarkEdge.ToArgb()] = light ? Color.FromArgb(80, 95, 138, 189) : Color.FromArgb(65, 170, 200, 245);
            colors[Hairline.ToArgb()] = light ? Color.FromArgb(80, 141, 164, 193) : Color.FromArgb(60, 145, 169, 205);
            colors[HairlineSoft.ToArgb()] = light ? Color.FromArgb(45, 141, 164, 193) : Color.FromArgb(35, 145, 169, 205);
            colors[BorderInput.ToArgb()] = light ? Color.FromArgb(110, 143, 166, 195) : Color.FromArgb(110, 125, 151, 189);
            colors[BorderFocus.ToArgb()] = light ? Color.FromArgb(52, 116, 198) : Color.FromArgb(140, 185, 255);
            colors[Hover.ToArgb()] = light ? Color.FromArgb(22, 70, 126, 196) : Color.FromArgb(24, 140, 185, 255);
            colors[Pressed.ToArgb()] = light ? Color.FromArgb(40, 70, 126, 196) : Color.FromArgb(40, 140, 185, 255);
            colors[PrimaryFill.ToArgb()] = light ? Color.FromArgb(49, 109, 189) : Color.FromArgb(60, 106, 184);
            colors[PrimaryHover.ToArgb()] = light ? Color.FromArgb(62, 127, 211) : Color.FromArgb(77, 124, 205);
            colors[PrimaryText.ToArgb()] = light ? Color.FromArgb(255, 255, 255) : Color.FromArgb(255, 255, 255);
            colors[DangerText.ToArgb()] = light ? Color.FromArgb(181, 51, 67) : Color.FromArgb(255, 150, 154);
            colors[SuccessText.ToArgb()] = light ? Color.FromArgb(30, 124, 89) : Color.FromArgb(119, 218, 177);
            Canvas = light ? Color.FromArgb(245, 246, 248) : Color.FromArgb(36, 39, 45);
            SidebarTop = light ? Color.FromArgb(239, 241, 245) : Color.FromArgb(29, 32, 38);
            SidebarBottom = light ? Color.FromArgb(232, 235, 240) : Color.FromArgb(25, 28, 33);
            SidebarFlat = light ? Color.FromArgb(238, 240, 244) : Color.FromArgb(29, 32, 38);
            SidebarEdge = light ? Color.FromArgb(197, 212, 232) : Color.FromArgb(50, 65, 87);
            TextPrimary = light ? Color.FromArgb(30, 47, 71) : Color.FromArgb(238, 243, 252);
            TextSecondary = light ? Color.FromArgb(65, 85, 113) : Color.FromArgb(188, 201, 222);
            TextTertiary = light ? Color.FromArgb(91, 110, 135) : Color.FromArgb(163, 178, 201);
            TextCaption = light ? Color.FromArgb(91, 110, 135) : Color.FromArgb(162, 177, 201);
            OnDark = light ? Color.FromArgb(30, 47, 71) : Color.FromArgb(238, 243, 252);
            OnDarkMuted = light ? Color.FromArgb(65, 85, 113) : Color.FromArgb(173, 188, 210);
            OnDarkFaint = light ? Color.FromArgb(91, 110, 135) : Color.FromArgb(143, 159, 184);
            GlassFill = light ? Color.FromArgb(245, 249, 254) : Color.FromArgb(45, 49, 57);
            GlassFillStrong = light ? Color.FromArgb(250, 252, 255) : Color.FromArgb(54, 59, 69);
            GlassHighlight = light ? Color.FromArgb(90, 255, 255, 255) : Color.FromArgb(52, 220, 235, 255);
            GlassOnDark = light ? Color.FromArgb(22, 83, 138, 205) : Color.FromArgb(25, 140, 185, 255);
            GlassOnDarkHover = light ? Color.FromArgb(38, 83, 138, 205) : Color.FromArgb(45, 140, 185, 255);
            GlassOnDarkEdge = light ? Color.FromArgb(80, 95, 138, 189) : Color.FromArgb(65, 170, 200, 245);
            Hairline = light ? Color.FromArgb(80, 141, 164, 193) : Color.FromArgb(60, 145, 169, 205);
            HairlineSoft = light ? Color.FromArgb(45, 141, 164, 193) : Color.FromArgb(35, 145, 169, 205);
            BorderInput = light ? Color.FromArgb(110, 143, 166, 195) : Color.FromArgb(110, 125, 151, 189);
            BorderFocus = light ? Color.FromArgb(52, 116, 198) : Color.FromArgb(140, 185, 255);
            Hover = light ? Color.FromArgb(22, 70, 126, 196) : Color.FromArgb(24, 140, 185, 255);
            Pressed = light ? Color.FromArgb(40, 70, 126, 196) : Color.FromArgb(40, 140, 185, 255);
            PrimaryFill = light ? Color.FromArgb(49, 109, 189) : Color.FromArgb(60, 106, 184);
            PrimaryHover = light ? Color.FromArgb(62, 127, 211) : Color.FromArgb(77, 124, 205);
            PrimaryText = light ? Color.FromArgb(255, 255, 255) : Color.FromArgb(255, 255, 255);
            DangerText = light ? Color.FromArgb(181, 51, 67) : Color.FromArgb(255, 150, 154);
            SuccessText = light ? Color.FromArgb(30, 124, 89) : Color.FromArgb(119, 218, 177);
            LightTheme = light;
            if (surface != null) RefreshTheme(surface, colors);
        }

        private static void RefreshTheme(Control control, System.Collections.Generic.Dictionary<int, Color> colors)
        {
            Color replacement;
            if (colors.TryGetValue(control.BackColor.ToArgb(), out replacement)) control.BackColor = replacement;
            if (colors.TryGetValue(control.ForeColor.ToArgb(), out replacement)) control.ForeColor = replacement;
            DshLabel label = control as DshLabel;
            if (label != null) label.RefreshThemeInk(colors);
            foreach (Control child in control.Controls) RefreshTheme(child, colors);
            control.Invalidate();
        }

        private static string family = ResolveFamily();

        public static string Family
        {
            get
            {
                return family;
            }
        }

        public static Font Body
        {
            get
            {
                return F(13f, FontStyle.Regular);
            }
        }

        public static Font Medium
        {
            get
            {
                return F(13f, FontStyle.Bold);
            }
        }

        public static Font Small
        {
            get
            {
                return F(12f, FontStyle.Regular);
            }
        }

        public static Font Caption
        {
            get
            {
                return F(11f, FontStyle.Regular);
            }
        }

        public static Font H1
        {
            get
            {
                return F(18f, FontStyle.Bold);
            }
        }

        public static Font H2
        {
            get
            {
                return F(14f, FontStyle.Bold);
            }
        }

        public static Font KeyFont
        {
            get
            {
                return F(16f, FontStyle.Bold);
            }
        }

        public static Font Brand
        {
            get
            {
                return F(14f, FontStyle.Bold);
            }
        }

        private static string ResolveFamily()
        {
            string[] array = new string[3] { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI" };
            string[] array2 = array;
            foreach (string text in array2)
            {
                try
                {
                    using (Font font = new Font(text, 9f))
                    {
                        if (string.Equals(font.Name, text, StringComparison.OrdinalIgnoreCase))
                        {
                            return text;
                        }
                    }
                }
                catch
                {
                }
            }
            return SystemFonts.MessageBoxFont.FontFamily.Name;
        }

        public static Font F(float px, FontStyle style)
        {
            return new Font(family, px * 72f / 96f, style, GraphicsUnit.Point);
        }

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath graphicsPath = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return graphicsPath;
            if (radius <= 0)
            {
                graphicsPath.AddRectangle(r);
                return graphicsPath;
            }
            int num = (int)Math.Min((long)radius * 2, Math.Min(r.Width, r.Height));
            graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
            graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
            graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
            graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
            graphicsPath.CloseFigure();
            return graphicsPath;
        }

        public static int ButtonRadius(Rectangle bounds)
        {
            int shortSide = Math.Min(bounds.Width, bounds.Height);
            if (shortSide < 2) return 0;
            return Math.Max(1, Math.Min(shortSide / 2,
                (int)Math.Round(shortSide * 0.5 * 0.618, MidpointRounding.AwayFromZero)));
        }

        public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle r, ContentAlignment align)
        {
            if(string.IsNullOrEmpty(text) || r.Width<=0 || r.Height<=0) return;
            using(Matrix transform=g.Transform)
            {
                // GDI TextRenderer does not consistently honor transformed graphics.
                // Use GDI+ for this path so translation, scale and clipping agree.
                if(!transform.IsIdentity)
                {
                    using(StringFormat format=new StringFormat())
                    using(SolidBrush brush=new SolidBrush(color))
                    {
                        format.FormatFlags=StringFormatFlags.NoWrap;
                        format.Trimming=StringTrimming.EllipsisCharacter;
                        format.HotkeyPrefix=System.Drawing.Text.HotkeyPrefix.Show;
                        format.Alignment=(align==ContentAlignment.TopCenter || align==ContentAlignment.MiddleCenter || align==ContentAlignment.BottomCenter) ? StringAlignment.Center :
                            (align==ContentAlignment.TopRight || align==ContentAlignment.MiddleRight || align==ContentAlignment.BottomRight) ? StringAlignment.Far : StringAlignment.Near;
                        format.LineAlignment=(align==ContentAlignment.MiddleLeft || align==ContentAlignment.MiddleCenter || align==ContentAlignment.MiddleRight) ? StringAlignment.Center :
                            (align==ContentAlignment.BottomLeft || align==ContentAlignment.BottomCenter || align==ContentAlignment.BottomRight) ? StringAlignment.Far : StringAlignment.Near;
                        GraphicsState state=g.Save();
                        try { g.SetClip(r,CombineMode.Intersect); g.DrawString(text,font,brush,r,format); }
                        finally { g.Restore(state); }
                    }
                    return;
                }
            }
            TextFormatFlags textFormatFlags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.PreserveGraphicsTranslateTransform;
            switch (align)
            {
            case ContentAlignment.TopLeft:
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.BottomLeft:
                break;
            case ContentAlignment.TopCenter:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.BottomCenter:
                textFormatFlags |= TextFormatFlags.HorizontalCenter;
                break;
            default:
                textFormatFlags |= TextFormatFlags.Right;
                break;
            }
            switch (align)
            {
            case ContentAlignment.TopLeft:
            case ContentAlignment.TopCenter:
            case ContentAlignment.TopRight:
                break;
            case ContentAlignment.BottomLeft:
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomRight:
                textFormatFlags |= TextFormatFlags.Bottom;
                break;
            default:
                textFormatFlags |= TextFormatFlags.VerticalCenter;
                break;
            }
            TextRenderer.DrawText(g, text, font, r, color, textFormatFlags);
        }

        public static void DrawGlass(Graphics g, Rectangle r, int radius, bool strong, bool shadow)
        {
            // Collapse animation passes through zero/one-pixel client heights.
            if (r.Width <= 0 || r.Height <= 0) return;
            radius = Math.Max(0, Math.Min(radius, Math.Min(r.Width, r.Height) / 2));
            if (shadow && r.Width > 2)
            {
                Rectangle r2 = new Rectangle(r.X + 1, r.Y + 3, r.Width - 2, r.Height + 3);
                using (GraphicsPath path = Round(r2, radius))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(r2.X, r2.Y + r2.Height / 2, Math.Max(1, r2.Width), Math.Max(1, r2.Height / 2 + 1)), Color.FromArgb(18, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), LinearGradientMode.Vertical))
                    {
                        g.FillPath(brush, path);
                    }
                }
            }
            using (GraphicsPath path2 = Round(r, radius))
            {
                using (SolidBrush brush2 = new SolidBrush(strong ? GlassFillStrong : GlassFill))
                {
                    g.FillPath(brush2, path2);
                }
            }
            SmoothingMode smoothingMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using (GraphicsPath graphicsPath = new GraphicsPath())
            {
                int num = radius * 2;
                if (num > 0)
                {
                    graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
                    graphicsPath.AddLine(r.X + num / 2, r.Y, r.Right - num / 2, r.Y);
                    graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
                }
                else graphicsPath.AddLine(r.Left, r.Top, r.Right, r.Top);
                using (Pen pen = new Pen(GlassHighlight, 1f))
                {
                    g.DrawPath(pen, graphicsPath);
                }
            }
            using (GraphicsPath path3 = Round(r, radius))
            {
                using (Pen pen2 = new Pen(Hairline, 1f))
                {
                    g.DrawPath(pen2, path3);
                }
            }
            g.SmoothingMode = smoothingMode;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color fill)
        {
            if (fill.A == 0)
            {
                return;
            }
            using (GraphicsPath path = Round(r, radius))
            {
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        public static void StrokeRound(Graphics g, Rectangle r, int radius, Color color, float width)
        {
            SmoothingMode smoothingMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using (GraphicsPath path = Round(r, radius))
            {
                using (Pen pen = new Pen(color, width))
                {
                    g.DrawPath(pen, path);
                }
            }
            g.SmoothingMode = smoothingMode;
        }

        public static void HairLine(Graphics g, int x, int y, int width, Color color)
        {
            SmoothingMode smoothingMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y, width, 1);
            }
            g.SmoothingMode = smoothingMode;
        }
    }
}
