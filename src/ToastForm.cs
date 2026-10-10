using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class ToastForm : Form
    {
        private Timer timer;

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

        public ToastForm(string text, Point anchor, int durationMs)
        {
            base.FormBorderStyle = FormBorderStyle.None;
            base.ShowInTaskbar = false;
            base.TopMost = true;
            base.StartPosition = FormStartPosition.Manual;
            base.AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(24, 28, 36);
            Font font = SystemFonts.MessageBoxFont;
            if (font == null)
            {
                font = SystemFonts.DefaultFont;
            }
            Label label = new Label
            {
                Text = text,
                ForeColor = Color.White,
                Font = font,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(14, 11)
            };
            base.Controls.Add(label);
            Size preferredSize = label.PreferredSize;
            base.ClientSize = new Size(preferredSize.Width + 28, preferredSize.Height + 22);
            int num = anchor.X - base.ClientSize.Width;
            int num2 = anchor.Y - base.ClientSize.Height - 12;
            Rectangle virtualScreen = SystemInformation.VirtualScreen;
            if (num < virtualScreen.Left + 6)
            {
                num = virtualScreen.Left + 6;
            }
            if (num2 < virtualScreen.Top + 6)
            {
                num2 = virtualScreen.Top + 6;
            }
            if (num + base.ClientSize.Width > virtualScreen.Right - 6)
            {
                num = virtualScreen.Right - 6 - base.ClientSize.Width;
            }
            base.Location = new Point(num, num2);
            GraphicsPath graphicsPath = new GraphicsPath();
            int num3 = 16;
            Rectangle rectangle = new Rectangle(0, 0, base.ClientSize.Width, base.ClientSize.Height);
            graphicsPath.AddArc(rectangle.X, rectangle.Y, num3, num3, 180f, 90f);
            graphicsPath.AddArc(rectangle.Right - num3, rectangle.Y, num3, num3, 270f, 90f);
            graphicsPath.AddArc(rectangle.Right - num3, rectangle.Bottom - num3, num3, num3, 0f, 90f);
            graphicsPath.AddArc(rectangle.X, rectangle.Bottom - num3, num3, num3, 90f, 90f);
            graphicsPath.CloseFigure();
            base.Region = new Region(graphicsPath);
            graphicsPath.Dispose();
            timer = new Timer();
            timer.Interval = durationMs;
            timer.Tick += delegate
            {
                timer.Stop();
                Close();
            };
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            base.Dispose(disposing);
        }
    }
}
