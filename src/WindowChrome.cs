using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapGrab
{
    internal partial class MainForm
    {
        private Panel windowBody;
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // WS_THICKFRAME provides native resize behavior, but DWM must not
            // paint its grey frame around the custom themed title bar.
            try
            {
                int disabled = 1; // DWMNCRP_DISABLED
                DwmSetWindowAttribute(Handle, 2, ref disabled, sizeof(int));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
        private void BuildWindowChrome()
        {
            SuspendLayout();
            Padding = new Padding(1);
            BackColor = Dsh.SidebarFlat;
            Control[] content = new Control[Controls.Count];
            Controls.CopyTo(content, 0);
            windowBody = new Panel { Dock = DockStyle.Fill, BackColor = Dsh.Canvas };
            foreach (Control control in content) windowBody.Controls.Add(control);
            Controls.Add(windowBody);
            var title = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Dsh.SidebarFlat };
            var caption = new DshLabel(UiText.T("截改 AI · SnipRefine"));
            caption.SetBounds(18, 8, 210, 28);
            title.Controls.Add(caption);
            var help = new DshHelpButton(); help.SetBounds(238, 8, 28, 28);
            help.Click += delegate { ShowUserGuide(); }; title.Controls.Add(help);
            MouseEventHandler drag = delegate(object sender, MouseEventArgs args)
            {
                if (args.Button != MouseButtons.Left || fullscreen) return;
                ReleaseCapture(); SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
            };
            title.MouseDown += drag; caption.MouseDown += drag;
            EventHandler maximize = delegate
            {
                if (fullscreen) { ToggleFullscreen(); return; }
                MaximizedBounds = Screen.FromControl(this).WorkingArea;
                WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            };
            title.DoubleClick += maximize; caption.DoubleClick += maximize;
            var minimize = new DshButton("−", DshButton.Kind.Ghost);
            var zoom = new DshButton("□", DshButton.Kind.Ghost);
            var close = new DshButton("×", DshButton.Kind.Ghost);
            minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            zoom.Click += maximize; close.Click += delegate { Close(); };
            title.Controls.Add(minimize); title.Controls.Add(zoom); title.Controls.Add(close);
            title.Resize += delegate
            {
                minimize.SetBounds(title.Width - 140, 7, 40, 30);
                zoom.SetBounds(title.Width - 94, 7, 40, 30);
                close.SetBounds(title.Width - 48, 7, 40, 30);
            };
            Controls.Add(title);
            windowBody.Resize += delegate { LayoutChatPanel(); };
            ResumeLayout(true);
            LayoutChatPanel();
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                // Retain native resize/keyboard system commands without a native caption.
                parameters.Style |= 0x80000 | 0x20000 | 0x10000;
                if (fullscreen) parameters.Style &= ~0x40000;
                else parameters.Style |= 0x40000;
                return parameters;
            }
        }
        protected override void WndProc(ref Message message)
        {
            // Keep native resize capability, but remove the differently colored
            // non-client strip. Our themed client surface owns the entire frame.
            // Also suppress the classic non-client renderer when DWM is unavailable.
            if (message.Msg == 0x85 || message.Msg == 0x86)
            {
                message.Result = message.Msg == 0x86 ? new IntPtr(1) : IntPtr.Zero;
                return;
            }
            if (message.Msg == 0x83)
            {
                message.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref message);
            if (message.Msg != 0x84 || fullscreen || WindowState != FormWindowState.Normal) return;
            long packed = message.LParam.ToInt64();
            Point point = PointToClient(new Point((short)(packed & 0xffff), (short)((packed >> 16) & 0xffff)));
            int edge = Math.Max(6, (int)Math.Round(6 * DeviceScale()));
            bool left = point.X < edge, right = point.X >= ClientSize.Width - edge;
            bool top = point.Y < edge, bottom = point.Y >= ClientSize.Height - edge;
            int hit = top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 0;
            if (hit != 0) message.Result = new IntPtr(hit);
        }
        private float DeviceScale() { using (Graphics g = CreateGraphics()) return g.DpiX / 96f; }
    }

    internal partial class ChatPanel
    {
        private void ShowAttachmentImage(Bitmap image)
        {
            using (var dialog = new ImagePreviewForm(image)) dialog.ShowDialog(FindForm());
        }
    }
}
