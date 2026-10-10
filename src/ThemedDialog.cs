using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace SnapGrab
{
    // Shared themed chrome for new preview and AI confirmation windows.
    internal class ThemedDialog : Form
    {
        internal readonly Panel ContentArea;
        private readonly DshLabel caption;
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, IntPtr l);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int size);
        internal ThemedDialog()
        {
            FormBorderStyle = FormBorderStyle.None; Padding = new Padding(1); BackColor = Color.FromArgb(Dsh.BorderInput.R, Dsh.BorderInput.G, Dsh.BorderInput.B);
            ContentArea = new Panel { Dock = DockStyle.Fill, BackColor = Dsh.Canvas }; Controls.Add(ContentArea);
            var title = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Dsh.SidebarFlat };
            caption = new DshLabel(Text); caption.SetBounds(14, 8, 300, 28); title.Controls.Add(caption);
            var close = new DshButton("×", DshButton.Kind.Ghost); close.Click += delegate { Close(); }; title.Controls.Add(close);
            title.Resize += delegate { close.SetBounds(title.Width - 46, 7, 38, 30); caption.Width = Math.Max(1, title.Width - 70); };
            MouseEventHandler drag = delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); } };
            title.MouseDown += drag; caption.MouseDown += drag; Controls.Add(title);
        }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); if (caption != null) caption.Text = Text; }
        protected override void OnShown(EventArgs e)
        {
            Rectangle area = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width), Math.Min(MinimumSize.Height, area.Height));
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
            Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)), Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)));
            base.OnShown(e);
        }
        protected override CreateParams CreateParams { get { CreateParams value = base.CreateParams; value.Style |= 0x40000; return value; } }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int disabled = 1; DwmSetWindowAttribute(Handle, 2, ref disabled, sizeof(int)); }
            catch (DllNotFoundException) { } catch (EntryPointNotFoundException) { }
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x83 || message.Msg == 0x85 || message.Msg == 0x86)
            { message.Result = message.Msg == 0x86 ? new IntPtr(1) : IntPtr.Zero; return; }
            base.WndProc(ref message);
            if (message.Msg != 0x84 || WindowState != FormWindowState.Normal) return;
            long packed = message.LParam.ToInt64();
            Point p = PointToClient(new Point((short)(packed & 0xffff), (short)((packed >> 16) & 0xffff)));
            bool left = p.X < 6, right = p.X >= ClientSize.Width - 6, top = p.Y < 6, bottom = p.Y >= ClientSize.Height - 6;
            int hit = top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 0;
            if (hit != 0) message.Result = new IntPtr(hit);
        }
    }
}
