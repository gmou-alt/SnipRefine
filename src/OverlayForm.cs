using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class OverlayForm : Form
    {
        private Bitmap snap;

        private Rectangle virt;

        private Point start;

        private Point cur;

        private bool dragging;
        public bool Adjustable;
        private int dragKind;
        private Rectangle dragSelection;
        private DshButton confirmSelection, cancelSelection;
        private int overlayDpi=96;
        private int Ui(int pixels) { return DesktopGeometry.Pixels(pixels,overlayDpi); }

        private bool accepted;

        private Rectangle selection;

        private Rectangle prevSelection;

        private Font hintFont;

        private Font sizeFont;

        private Stopwatch watch;

        private SolidBrush brushDim;

        private SolidBrush brushAccent;

        private SolidBrush brushBox;

        private Pen penOuter;

        private Pen penInner;

        public bool Accepted
        {
            get
            {
                return accepted;
            }
        }

        public Rectangle Selection
        {
            get
            {
                return selection;
            }
        }

        public OverlayForm()
        {
            selection = Rectangle.Empty;
            prevSelection = Rectangle.Empty;
            hintFont = new Font("Microsoft YaHei UI", 9.5f);
            sizeFont = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            brushDim = new SolidBrush(Color.FromArgb(115, 0, 0, 0));
            brushAccent = new SolidBrush(Color.FromArgb(255, 70, 170, 255));
            brushBox = new SolidBrush(Color.FromArgb(215, 24, 28, 36));
            penOuter = new Pen(Color.FromArgb(255, 255, 255, 255), 1f);
            penInner = new Pen(Color.FromArgb(255, 70, 170, 255), 2f);
            base.FormBorderStyle = FormBorderStyle.None;
            base.StartPosition = FormStartPosition.Manual;
            base.ShowInTaskbar = false;
            base.TopMost = true;
            base.AutoScaleMode = AutoScaleMode.None;
            base.KeyPreview = true;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            Text = "SnipRefine";
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            confirmSelection = new DshButton(UiText.T("确认截图"), DshButton.Kind.Primary);
            cancelSelection = new DshButton(UiText.T("取消"));
            confirmSelection.StrongOutline=cancelSelection.StrongOutline=true;
            confirmSelection.Visible = cancelSelection.Visible = false;
            confirmSelection.Click += delegate { ConfirmSelection(); };
            cancelSelection.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(confirmSelection); Controls.Add(cancelSelection);
        }

        public void Prepare(Bitmap snapshot, Rectangle bounds, Stopwatch stopwatch)
        {
            if(snapshot==null || snapshot.Size!=bounds.Size) throw new ArgumentException("Snapshot and physical desktop dimensions must match");
            snap = snapshot;
            overlayDpi=DisplayDiagnostics.DpiAt(Cursor.Position);
            virt = bounds;
            watch = stopwatch;
            selection = Rectangle.Empty;
            prevSelection = Rectangle.Empty;
            if (confirmSelection != null) confirmSelection.Visible = cancelSelection.Visible = false;
            dragKind = 0;
            accepted = false;
            dragging = false;
            start = Point.Empty;
            cur = Point.Empty;
            base.Bounds = bounds;
            Cursor = Cursors.Cross;
        }

        public void ClearSnapshot()
        {
            snap = null;
            selection = Rectangle.Empty;
            prevSelection = Rectangle.Empty;
        }

        protected override void OnShown(EventArgs e)
        {
            base.Bounds = virt;
            base.OnShown(e);
            if (watch != null)
            {
                Diag.Log("遮罩已显示 (" + watch.ElapsedMilliseconds + " ms)");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            if (snap == null)
            {
                graphics.FillRectangle(brushDim, base.ClientRectangle);
                return;
            }
            graphics.DrawImage(snap, new Rectangle(0, 0, virt.Width, virt.Height), new Rectangle(0, 0, snap.Width, snap.Height), GraphicsUnit.Pixel);
            bool flag = !selection.IsEmpty && selection.Width > 0 && selection.Height > 0;
            if (flag)
            {
                graphics.ExcludeClip(selection);
            }
            graphics.FillRectangle(brushDim, base.ClientRectangle);
            if (flag)
            {
                graphics.ResetClip();
            }
            if (flag)
            {
                Rectangle rectangle = selection;
                graphics.DrawRectangle(penOuter, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                graphics.DrawRectangle(penInner, rectangle.X - 1, rectangle.Y - 1, rectangle.Width + 2, rectangle.Height + 2);
                int num = Ui(6);
                graphics.FillRectangle(brushAccent, rectangle.X - num / 2, rectangle.Y - num / 2, num, num);
                graphics.FillRectangle(brushAccent, rectangle.Right - num / 2, rectangle.Y - num / 2, num, num);
                graphics.FillRectangle(brushAccent, rectangle.X - num / 2, rectangle.Bottom - num / 2, num, num);
                graphics.FillRectangle(brushAccent, rectangle.Right - num / 2, rectangle.Bottom - num / 2, num, num);
                if (Adjustable) {
                    graphics.FillRectangle(brushAccent, rectangle.Left + rectangle.Width/2-num/2, rectangle.Top-num/2,num,num);
                    graphics.FillRectangle(brushAccent, rectangle.Left + rectangle.Width/2-num/2, rectangle.Bottom-num/2,num,num);
                    graphics.FillRectangle(brushAccent, rectangle.Left-num/2, rectangle.Top+rectangle.Height/2-num/2,num,num);
                    graphics.FillRectangle(brushAccent, rectangle.Right-num/2, rectangle.Top+rectangle.Height/2-num/2,num,num);
                }
                DrawSizeLabel(graphics);
            }
            DrawHint(graphics);
        }

        private void DrawHint(Graphics g)
        {
            string text = Adjustable ? "拖动框选 · 框内移动 / 边缘调整大小 · Enter / 双击确认 · Esc / 右键取消" : "快速截图：拖动框选，松开即保存 · Esc / 右键取消";
            text=UiText.T(text);
            SizeF sizeF = g.MeasureString(text, hintFont);
            int num = (int)sizeF.Width + 30;
            int height = (int)sizeF.Height + 16;
            int num2 = (base.ClientSize.Width - num) / 2;
            int num3 = 18;
            if (num2 < 8)
            {
                num2 = 8;
            }
            FillRounded(g, new Rectangle(num2, num3, num, height), 8, brushBox);
            g.DrawString(text, hintFont, Brushes.White, num2 + 15, num3 + 8);
        }

        private void DrawSizeLabel(Graphics g)
        {
            string text = selection.Width + " × " + selection.Height;
            SizeF sizeF = g.MeasureString(text, sizeFont);
            int num = (int)sizeF.Width + 18;
            int num2 = (int)sizeF.Height + 12;
            int num3 = selection.Right + 10;
            int num4 = selection.Bottom + 10;
            if (num3 + num > base.ClientSize.Width - 4)
            {
                num3 = selection.Left - 10 - num;
            }
            if (num4 + num2 > base.ClientSize.Height - 4)
            {
                num4 = selection.Top - 10 - num2;
            }
            if (num3 < 4)
            {
                num3 = 4;
            }
            if (num4 < 4)
            {
                num4 = 4;
            }
            FillRounded(g, new Rectangle(num3, num4, num, num2), 6, brushBox);
            g.DrawString(text, sizeFont, Brushes.White, num3 + 9, num4 + 6);
        }

        private static void FillRounded(Graphics g, Rectangle r, int radius, Brush brush)
        {
            if (r.Width > 0 && r.Height > 0)
            {
                GraphicsPath graphicsPath = new GraphicsPath();
                int num = radius * 2;
                if (num > r.Width)
                {
                    num = r.Width;
                }
                if (num > r.Height)
                {
                    num = r.Height;
                }
                if (num < 2)
                {
                    num = 2;
                }
                graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
                graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
                graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
                graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
                graphicsPath.CloseFigure();
                g.FillPath(brush, graphicsPath);
                graphicsPath.Dispose();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                start = e.Location;
                dragSelection = selection;
                dragKind = Adjustable ? SelectionGeometry.Hit(selection, e.Location, Ui(8)) : 0;
                cur = e.Location;
                prevSelection = selection;
                if (dragKind == 0) UpdateSelection();
                if (confirmSelection != null) confirmSelection.Visible = cancelSelection.Visible = false;
                base.Capture = true;
                RepaintSelection();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                cur = e.Location;
                UpdateSelection();
                RepaintSelection();
            }
            else if (Adjustable) Cursor = CursorForSelection(SelectionGeometry.Hit(selection, e.Location, Ui(8)));
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragging)
            {
                dragging = false;
                base.Capture = false;
                cur = e.Location;
                UpdateSelection();
                if (selection.Width >= 3 && selection.Height >= 3)
                {
                    if (Adjustable) { PositionConfirmation(); Invalidate(); return; }
                    accepted = true;
                    base.DialogResult = DialogResult.OK;
                    Close();
                    return;
                }
                selection = Rectangle.Empty;
                RepaintSelection();
            }
            else if (e.Button == MouseButtons.Right)
            {
                dragging = false;
                base.Capture = false;
                accepted = false;
                base.DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            base.OnMouseUp(e);
        }

        private void RepaintSelection()
        {
            Rectangle rc = (prevSelection.IsEmpty ? selection : ((!selection.IsEmpty) ? Rectangle.Union(prevSelection, selection) : prevSelection));
            prevSelection = selection;
            if (rc.IsEmpty)
            {
                Invalidate();
                return;
            }
            rc.Inflate(96, 64);
            rc.Intersect(base.ClientRectangle);
            if (rc.Width <= 0 || rc.Height <= 0)
            {
                Invalidate();
            }
            else
            {
                Invalidate(rc);
            }
        }

        private void UpdateSelection()
        {
            if (dragKind != 0) { selection = SelectionGeometry.Change(dragSelection, start, cur, dragKind, new Size(virt.Width,virt.Height)); return; }
            int num = Math.Min(start.X, cur.X);
            int num2 = Math.Min(start.Y, cur.Y);
            int num3 = Math.Max(start.X, cur.X);
            int num4 = Math.Max(start.Y, cur.Y);
            if (num < 0)
            {
                num = 0;
            }
            if (num2 < 0)
            {
                num2 = 0;
            }
            if (num3 > virt.Width)
            {
                num3 = virt.Width;
            }
            if (num4 > virt.Height)
            {
                num4 = virt.Height;
            }
            selection = Rectangle.FromLTRB(num, num2, num3, num4);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
            case Keys.Escape:
                accepted = false;
                base.DialogResult = DialogResult.Cancel;
                Close();
                return true;
            case Keys.Enter:
                if (Adjustable) { ConfirmSelection(); return true; }
                return base.ProcessCmdKey(ref msg,keyData);
            case Keys.Space:
                selection = new Rectangle(0, 0, virt.Width, virt.Height);
                accepted = true;
                base.DialogResult = DialogResult.OK;
                Close();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private static Cursor CursorForSelection(int hit)
        {
            if (hit==16) return Cursors.SizeAll;
            if (hit==5 || hit==10) return Cursors.SizeNWSE;
            if (hit==6 || hit==9) return Cursors.SizeNESW;
            if ((hit&3)!=0) return Cursors.SizeWE;
            if ((hit&12)!=0) return Cursors.SizeNS;
            return Cursors.Cross;
        }
        private void ConfirmSelection()
        {
            if (selection.Width < 3 || selection.Height < 3) return;
            dragging=false; Capture=false; accepted=true; DialogResult=DialogResult.OK; Close();
        }
        private void PositionConfirmation()
        {
            overlayDpi=DisplayDiagnostics.DpiAt(new Point(virt.Left+selection.Right,virt.Top+selection.Bottom));
            int y=selection.Bottom+Ui(14);
            if (y+Ui(36)>ClientSize.Height) y=Math.Max(Ui(4),selection.Top-Ui(48));
            int x=Math.Max(Ui(4),Math.Min(selection.Right-Ui(204),ClientSize.Width-Ui(208)));
            confirmSelection.SetBounds(x,y,Ui(116),Ui(34)); cancelSelection.SetBounds(x+Ui(124),y,Ui(80),Ui(34));
            confirmSelection.Visible=cancelSelection.Visible=true;
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (Adjustable && e.Button==MouseButtons.Left && selection.Contains(e.Location)) ConfirmSelection();
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture && dragging) { dragging=false; if (Adjustable && selection.Width>=3 && selection.Height>=3) PositionConfirmation(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (hintFont != null)
                {
                    hintFont.Dispose();
                    hintFont = null;
                }
                if (sizeFont != null)
                {
                    sizeFont.Dispose();
                    sizeFont = null;
                }
                if (brushDim != null)
                {
                    brushDim.Dispose();
                    brushDim = null;
                }
                if (brushAccent != null)
                {
                    brushAccent.Dispose();
                    brushAccent = null;
                }
                if (brushBox != null)
                {
                    brushBox.Dispose();
                    brushBox = null;
                }
                if (penOuter != null)
                {
                    penOuter.Dispose();
                    penOuter = null;
                }
                if (penInner != null)
                {
                    penInner.Dispose();
                    penInner = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
