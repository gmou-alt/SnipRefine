using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace SnapGrab
{
    internal sealed class ImagePreviewSurface : Control
    {
        private readonly Image image;
        private float scale = 1;
        private PointF origin;
        private Point previous;
        private bool dragging, fit = true;
        internal event EventHandler ScaleChanged;
        internal float ScaleValue { get { return scale; } }
        internal ImagePreviewSurface(Image source)
        {
            image = source; BackColor = Dsh.Canvas; Cursor = Cursors.Hand; TabStop = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        internal void FitImage() { fit = true; Refit(); }
        internal void ActualSize() { fit = false; scale = 1; CenterImage(); Changed(); }
        private void CenterImage() { origin = new PointF((Width - image.Width * scale) / 2, (Height - image.Height * scale) / 2); }
        private void Refit()
        {
            scale = Math.Max(0.01f, Math.Min(1, Math.Min((float)Math.Max(1, Width - 24) / image.Width, (float)Math.Max(1, Height - 24) / image.Height)));
            CenterImage(); Changed();
        }
        private void Changed() { Invalidate(); if (ScaleChanged != null) ScaleChanged(this, EventArgs.Empty); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (fit) Refit(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = scale >= 1 ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(image, new RectangleF(origin.X, origin.Y, image.Width * scale, image.Height * scale));
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            float next = Math.Max(0.01f, Math.Min(8, scale * (float)Math.Pow(1.2, e.Delta / 120.0)));
            float factor = next / scale;
            origin = new PointF(e.X - (e.X - origin.X) * factor, e.Y - (e.Y - origin.Y) * factor);
            scale = next; fit = false; Changed(); base.OnMouseWheel(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); Focus();
            if (e.Button == MouseButtons.Left) { previous = e.Location; dragging = true; Capture = true; fit = false; }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e); if (!dragging) return;
            origin = new PointF(origin.X + e.X - previous.X, origin.Y + e.Y - previous.Y); previous = e.Location; Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Capture = false; }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragging = false; }
    }
    internal sealed class ImagePreviewForm : ThemedDialog
    {
        internal ImagePreviewForm(Image image)
        {
            Text = "图片预览 · " + image.Width + " × " + image.Height;
            StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(900, 620); MinimumSize = new Size(440, 300);
            BackColor = Dsh.Canvas; KeyPreview = true;
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8), BackColor = Dsh.SidebarFlat };
            var surface = new ImagePreviewSurface(image) { Dock = DockStyle.Fill };
            var fit = new DshButton("适应窗口") { Size = new Size(100, 30) };
            var actual = new DshButton("原尺寸") { Size = new Size(80, 30) };
            var copy = new DshButton("复制图片") { Size = new Size(90, 30) };
            var close = new DshButton(UiText.T("关闭")) { Size = new Size(70, 30) };
            var status = new DshLabel("") { Size = new Size(220, 30), Small = true };
            toolbar.Controls.Add(fit); toolbar.Controls.Add(actual); toolbar.Controls.Add(copy); toolbar.Controls.Add(close); toolbar.Controls.Add(status);
            ContentArea.Controls.Add(surface); ContentArea.Controls.Add(toolbar);
            ContentArea.Resize += delegate { toolbar.Height = ContentArea.Width < 620 ? 88 : 48; };
            fit.Click += delegate { surface.FitImage(); }; actual.Click += delegate { surface.ActualSize(); };
            copy.Click += delegate { try { Clipboard.SetImage(image); status.Text = "已复制原图"; } catch { status.Text = "复制失败，请稍后重试"; } };
            close.Click += delegate { Close(); };
            surface.ScaleChanged += delegate { status.Text = Math.Round(surface.ScaleValue * 100) + "% · 拖动平移"; };
            Shown += delegate { surface.FitImage(); surface.Focus(); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
        }
    }
}
