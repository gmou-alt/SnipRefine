using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapGrab
{
    internal sealed class DshHelpButton : DshControl
    {
        private bool hover;
        internal DshHelpButton()
        {
            Text = "?"; AccessibleName = "全部使用说明"; AccessibleRole = AccessibleRole.PushButton;
            Cursor = Cursors.Hand; TabStop = true;
            MouseEnter += delegate { hover = true; Invalidate(); };
            MouseLeave += delegate { hover = false; Invalidate(); };
            GotFocus += delegate { Invalidate(); }; LostFocus += delegate { Invalidate(); };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = Setup(e.Graphics);
            Rectangle circle = new Rectangle(2, 2, Math.Max(1, Width - 5), Math.Max(1, Height - 5));
            using (Brush fill = new SolidBrush(hover ? Dsh.GlassFillStrong : Dsh.SidebarFlat)) g.FillEllipse(fill, circle);
            using (Pen pen = new Pen(Focused ? Dsh.TextPrimary : Dsh.BorderInput, 1)) g.DrawEllipse(pen, circle);
            TextRenderer.DrawText(g, "?", Font, circle, Dsh.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }
    }

    internal partial class MainForm
    {
        private void ShowUserGuide() { using (var guide = new UserGuideForm()) guide.ShowDialog(this); }
    }

    internal sealed class UserGuideForm : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, IntPtr l);
        private readonly FlowLayoutPanel navigation;
        private readonly TextBox content;
        private readonly DshLabel heading;
        private readonly DshTextInput search;
        internal UserGuideForm()
        {
            Text = "截改 AI · 使用说明"; FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(920, 680);
            BackColor = Color.FromArgb(Dsh.BorderInput.R, Dsh.BorderInput.G, Dsh.BorderInput.B); Padding = new Padding(1); KeyPreview = true; Font = Dsh.Body;
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Dsh.Canvas }; Controls.Add(body);
            var top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Dsh.SidebarFlat };
            var title = new DshLabel("使用说明 · " + Program.VersionText); title.SetBounds(18, 12, 330, 30); top.Controls.Add(title);
            var close = new DshButton(UiText.T("关闭")); close.SetBounds(Width - 94, 10, 76, 32); close.Click += delegate { Close(); }; top.Controls.Add(close);
            MouseEventHandler drag = delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); } };
            title.MouseDown += drag; top.MouseDown += drag;
            var left = new Panel { Dock = DockStyle.Left, Width = 216, BackColor = Dsh.SidebarFlat };
            search = new DshTextInput { Placeholder = "搜索功能或问题" }; search.SetBounds(12, 12, 192, 36); left.Controls.Add(search);
            navigation = new FlowLayoutPanel { Location = new Point(8, 60), Size = new Size(200, 560), AutoScroll = true,
                FlowDirection = FlowDirection.TopDown, WrapContents = false, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };
            left.Controls.Add(navigation);
            var reading = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 12, 22, 18), BackColor = Dsh.Canvas };
            heading = new DshLabel("") { Dock = DockStyle.Top, Height = 44, Font = Dsh.F(17, FontStyle.Bold) };
            content = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Dsh.Canvas, ForeColor = Dsh.TextPrimary, Font = Dsh.F(11.5f, FontStyle.Regular), ScrollBars = ScrollBars.Vertical };
            reading.Controls.Add(content); reading.Controls.Add(heading);
            body.Controls.Add(reading); body.Controls.Add(left); body.Controls.Add(top);
            search.Inner.TextChanged += delegate { FilterTopics(); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; } };
            Shown += delegate { Rectangle area = Screen.FromControl(this).WorkingArea; Size = new Size(Math.Min(920, area.Width), Math.Min(680, area.Height)); Left = Math.Max(area.Left, Math.Min(Left, area.Right - Width)); Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)); close.Left = Width - 94; navigation.Height = Math.Max(120, left.Height - 72); };
            FilterTopics();
        }
        private void ShowTopic(HelpTopic topic) { heading.Text = topic.Title; content.Text = topic.Body; content.SelectionStart = 0; content.ScrollToCaret(); }
        private void FilterTopics()
        {
            navigation.SuspendLayout();
            while (navigation.Controls.Count > 0) navigation.Controls[0].Dispose();
            List<HelpTopic> topics = UserGuide.Search(search.Text);
            foreach (HelpTopic topic in topics)
            {
                HelpTopic selected = topic;
                var button = new DshButton(topic.Title) { Size = new Size(176, 34), Margin = new Padding(3, 3, 3, 3) };
                button.Click += delegate { ShowTopic(selected); }; navigation.Controls.Add(button);
            }
            navigation.ResumeLayout();
            if (topics.Count > 0) ShowTopic(topics[0]);
            else { heading.Text = "没有找到相关内容"; content.Text = "试试「截图」「模型」「保存」「快捷键」等关键词。"; }
        }
    }
}
