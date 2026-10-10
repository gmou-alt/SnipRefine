using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapGrab
{
    internal static class DshScrollTheme
    {
        private static readonly HashSet<Form> forms = new HashSet<Form>();
        public static void Install()
        {
            Application.Idle += delegate
            {
                foreach (Form form in Application.OpenForms)
                    if (forms.Add(form))
                    {
                        Watch(form);
                        form.Disposed += delegate { forms.Remove(form); };
                    }
            };
        }
        private static void Watch(Control control)
        {
            if (control is ScrollThumb) return;
            control.ControlAdded += delegate(object sender, ControlEventArgs args) { Watch(args.Control); };
            Control[] children = new Control[control.Controls.Count];
            control.Controls.CopyTo(children, 0);
            foreach (Control child in children) Watch(child);
            var scrolling = control as ScrollableControl;
            var text = control as TextBox;
            if ((scrolling != null && scrolling.AutoScroll) || (text != null && text.Multiline && text.ScrollBars != ScrollBars.None))
                new ScrollHook(control);
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct ScrollInfo
        {
            public int Size, Mask, Minimum, Maximum, Page, Position, Track;
        }
        [DllImport("user32.dll")] private static extern bool GetScrollInfo(IntPtr handle, int bar, ref ScrollInfo info);
        [DllImport("user32.dll")] private static extern bool ShowScrollBar(IntPtr handle, int bar, bool show);
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

        private sealed class ScrollHook : NativeWindow
        {
            private readonly Control target;
            private readonly ScrollThumb horizontal, vertical;
            private bool updating;
            public ScrollHook(Control control)
            {
                target = control;
                horizontal = new ScrollThumb(control, false);
                vertical = new ScrollThumb(control, true);
                target.HandleCreated += delegate { AssignHandle(target.Handle); Refresh(); };
                target.HandleDestroyed += delegate { ReleaseHandle(); };
                target.ParentChanged += delegate { Refresh(); };
                target.VisibleChanged += delegate { Refresh(); };
                target.LocationChanged += delegate { Refresh(); };
                target.Disposed += delegate { horizontal.Dispose(); vertical.Dispose(); ReleaseHandle(); };
                if (target.IsHandleCreated) AssignHandle(target.Handle);
                Refresh();
            }
            protected override void WndProc(ref Message message)
            {
                base.WndProc(ref message);
                if (message.Msg == 0xF || message.Msg == 5 || message.Msg == 0x114 || message.Msg == 0x115 || message.Msg == 0x20A || message.Msg == 0x100)
                    Refresh();
            }
            private void Refresh()
            {
                if (updating || target.IsDisposed || !target.IsHandleCreated || target.Parent == null) return;
                updating = true;
                try
                {
                    if (horizontal.Parent != target.Parent) target.Parent.Controls.Add(horizontal);
                    if (vertical.Parent != target.Parent) target.Parent.Controls.Add(vertical);
                    Update(horizontal, 0); Update(vertical, 1);
                    ShowScrollBar(target.Handle, 3, false);
                }
                finally { updating = false; }
            }
            private void Update(ScrollThumb bar, int axis)
            {
                ScrollInfo info = new ScrollInfo { Size = Marshal.SizeOf(typeof(ScrollInfo)), Mask = 0x17 };
                bool found = GetScrollInfo(target.Handle, axis, ref info);
                bar.Info = info;
                bar.Visible = target.Visible && found && info.Page > 0 && info.Maximum - info.Minimum + 1 > info.Page;
                Rectangle bounds = target.Bounds;
                bar.SetBounds(axis == 1 ? bounds.Right - 10 : bounds.Left + 4,
                    axis == 1 ? bounds.Top + 4 : bounds.Bottom - 10,
                    axis == 1 ? 8 : Math.Max(1, bounds.Width - 18),
                    axis == 1 ? Math.Max(1, bounds.Height - 18) : 8);
                if (bar.Visible) bar.BringToFront();
                bar.Invalidate();
            }
        }
        private sealed class ScrollThumb : DshControl
        {
            private readonly Control target;
            private readonly bool vertical;
            internal ScrollInfo Info;
            private bool hover, dragging;
            private int dragOffset;
            public ScrollThumb(Control control, bool axis)
            {
                target = control; vertical = axis; TabStop = false;
                Cursor = Cursors.Hand; Visible = false;
            }
            private int Length { get { return vertical ? Height : Width; } }
            private int ThumbLength { get { return Math.Min(Length, Math.Max(24, (int)((double)Length * Info.Page / Math.Max(1, Info.Maximum - Info.Minimum + 1)))); } }
            private int Range { get { return Math.Max(0, Info.Maximum - Info.Minimum - Math.Max(0, Info.Page - 1)); } }
            private int ThumbStart { get { return (int)((double)Math.Max(0, Length - ThumbLength) * (Info.Position - Info.Minimum) / Math.Max(1, Range)); } }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics graphics = Setup(e.Graphics); graphics.Clear(target.BackColor);
                Rectangle thumb = vertical ? new Rectangle(1, ThumbStart, 6, ThumbLength) : new Rectangle(ThumbStart, 1, ThumbLength, 6);
                if (thumb.Width > 0 && thumb.Height > 0)
                    Dsh.FillRound(graphics, thumb, 3, hover || dragging ? Dsh.TextSecondary : Dsh.BorderInput);
            }
            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e); if (e.Button != MouseButtons.Left) return;
                int point = vertical ? e.Y : e.X;
                dragOffset = point >= ThumbStart && point <= ThumbStart + ThumbLength ? point - ThumbStart : ThumbLength / 2;
                dragging = true; Capture = true; ScrollTo(point); Invalidate();
            }
            protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) ScrollTo(vertical ? e.Y : e.X); }
            protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Capture = false; Invalidate(); }
            protected override void OnMouseCaptureChanged(EventArgs e) { if (!Capture) dragging = false; base.OnMouseCaptureChanged(e); }
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                SendMessage(target.Handle, 0x20A, new IntPtr(e.Delta << 16), IntPtr.Zero);
                base.OnMouseWheel(e);
            }
            private void ScrollTo(int point)
            {
                int offset = Math.Max(0, Math.Min(Length - ThumbLength, point - dragOffset));
                int position = Info.Minimum + (int)Math.Round((double)offset * Range / Math.Max(1, Length - ThumbLength));
                var panel = target as ScrollableControl;
                if (panel != null)
                {
                    Point old = panel.AutoScrollPosition;
                    panel.AutoScrollPosition = vertical ? new Point(-old.X, position) : new Point(position, -old.Y);
                }
                else if (vertical)
                    SendMessage(target.Handle, 0xB6, IntPtr.Zero, new IntPtr(position - Info.Position));
                else
                    SendMessage(target.Handle, 0x114, new IntPtr(4 | (position << 16)), IntPtr.Zero);
                Info.Position = position; target.Invalidate(); Invalidate();
            }
        }
    }
}
