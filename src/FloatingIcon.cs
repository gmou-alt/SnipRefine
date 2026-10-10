using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class FloatingIcon : Form
    {
        public const int IconSize = 52;

        private TrayApp app;

        private Bitmap art;

        private ToolTip tip;

        private bool dragging;

        private bool moved;

        private bool hover;

        private Point dragOffset;

        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= 128;
                createParams.ExStyle |= 134217728;
                return createParams;
            }
        }

        public FloatingIcon(TrayApp owner, Point location)
        {
            app = owner;
            base.FormBorderStyle = FormBorderStyle.None;
            base.ShowInTaskbar = false;
            base.TopMost = true;
            base.StartPosition = FormStartPosition.Manual;
            base.AutoScaleMode = AutoScaleMode.None;
            base.ClientSize = new Size(52, 52);
            base.Location = location;
            BackColor = Color.FromArgb(30, 36, 45);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            GraphicsPath graphicsPath = new GraphicsPath();
            int num = 24;
            Rectangle rectangle = new Rectangle(0, 0, 52, 52);
            graphicsPath.AddArc(rectangle.X, rectangle.Y, num, num, 180f, 90f);
            graphicsPath.AddArc(rectangle.Right - num, rectangle.Y, num, num, 270f, 90f);
            graphicsPath.AddArc(rectangle.Right - num, rectangle.Bottom - num, num, num, 0f, 90f);
            graphicsPath.AddArc(rectangle.X, rectangle.Bottom - num, num, num, 90f, 90f);
            graphicsPath.CloseFigure();
            base.Region = new Region(graphicsPath);
            graphicsPath.Dispose();
            art = TrayApp.CreateAppBitmap(32);
            tip = new ToolTip();
            tip.SetToolTip(this, "SnipRefine\n左键：唤出 / 收起 AI 对话\n右键：功能菜单（含打开设置界面）\n按住拖动：换个位置");
        }

        public static Point DefaultLocation()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int num = workingArea.Right - 52 - 18;
            int num2 = workingArea.Bottom - 52 - 18;
            if (num < workingArea.Left)
            {
                num = workingArea.Left;
            }
            if (num2 < workingArea.Top)
            {
                num2 = workingArea.Top;
            }
            return new Point(num, num2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color color = (hover ? Color.FromArgb(255, 46, 54, 66) : Color.FromArgb(255, 30, 36, 45));
            using (SolidBrush brush = new SolidBrush(color))
            {
                graphics.FillRectangle(brush, base.ClientRectangle);
            }
            using (Pen pen = new Pen(Color.FromArgb(hover ? 235 : 150, 60, 150, 255), 2f))
            {
                graphics.DrawRectangle(pen, 1, 1, base.ClientSize.Width - 3, base.ClientSize.Height - 3);
            }
            graphics.DrawImage(art, (base.ClientSize.Width - 32) / 2, (base.ClientSize.Height - 32) / 2, 32, 32);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                moved = false;
                dragOffset = new Point(Cursor.Position.X - base.Left, Cursor.Position.Y - base.Top);
                base.Capture = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                Point position = Cursor.Position;
                int x = position.X - dragOffset.X;
                int y = position.Y - dragOffset.Y;
                if (Math.Abs(x - base.Left) > 2 || Math.Abs(y - base.Top) > 2)
                {
                    moved = true;
                }
                if (moved)
                {
                    ClampToScreen(ref x, ref y);
                    base.Location = new Point(x, y);
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragging)
            {
                dragging = false;
                base.Capture = false;
                if (moved)
                {
                    app.SaveFloatingIconPos(base.Location);
                }
                else
                {
                    app.ToggleChatPanel();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                app.ShowTrayMenu(Cursor.Position);
            }
            base.OnMouseUp(e);
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

        private void ClampToScreen(ref int x, ref int y)
        {
            Rectangle virtualScreen = SystemInformation.VirtualScreen;
            if (x < virtualScreen.Left)
            {
                x = virtualScreen.Left;
            }
            if (y < virtualScreen.Top)
            {
                y = virtualScreen.Top;
            }
            if (x + base.Width > virtualScreen.Right)
            {
                x = virtualScreen.Right - base.Width;
            }
            if (y + base.Height > virtualScreen.Bottom)
            {
                y = virtualScreen.Bottom - base.Height;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (tip != null)
                {
                    tip.Dispose();
                    tip = null;
                }
                if (art != null)
                {
                    art.Dispose();
                    art = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
