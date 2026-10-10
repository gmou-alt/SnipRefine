using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal partial class EditorForm
    {
        private Shape selectedShape;
        private Shape selectionOriginal;
        private UndoStep selectionCheckpoint;
        private PointF selectionStart;
        private bool selectionDragging, selectionResize, selectionChanged;

        private UndoStep CaptureEditState()
        {
            UndoStep state = new UndoStep { Kind = UndoStep.T.Edit, HadSource = srcMutable != null,
                Snapshot = srcMutable == null ? null : (Bitmap)srcMutable.Clone() };
            foreach (Shape shape in shapes) state.Items.Add(shape.Clone());
            return state;
        }

        private void CommitEditState(UndoStep state)
        {
            ClearRedoHistory(); undoSteps.Add(state); BoundHistory();
            editRevision++; commitVersion++; hasCrop = false;
            SetStatus("已修改标注 · 可撤销"); Invalidate();
        }

        private RectangleF ShapeBounds(Shape shape)
        {
            RectangleF r = shape.Rect;
            if (shape.Kind == Shape.K.Pen && !shape.Brush && shape.Points.Count > 0)
            {
                float left = shape.Points[0].X, top = shape.Points[0].Y, right = left, bottom = top;
                foreach (PointF p in shape.Points) { left = Math.Min(left,p.X); top = Math.Min(top,p.Y); right = Math.Max(right,p.X); bottom = Math.Max(bottom,p.Y); }
                r = new RectangleF(left,top,Math.Max(1,right-left),Math.Max(1,bottom-top));
            }
            else if (shape.Kind == Shape.K.Text)
            {
                using (Font font = Dsh.F(Math.Max(6,shape.FontPx), FontStyle.Bold))
                using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddString(shape.Text ?? "",font.FontFamily,1,font.Size*96f/72f,PointF.Empty,StringFormat.GenericTypographic);
                    RectangleF textBounds=path.GetBounds();
                    r=new RectangleF(shape.Rect.X+textBounds.X,shape.Rect.Y+textBounds.Y,Math.Max(8,textBounds.Width),Math.Max(8,textBounds.Height));
                }
            }
            if (r.Width < 0) r = new RectangleF(r.Right,r.Y,-r.Width,r.Height);
            if (r.Height < 0) r = new RectangleF(r.X,r.Bottom,r.Width,-r.Height);
            return r;
        }

        private void BeginSelectionDrag(PointF p) { if(selectedShape!=null&&(selectedShape.Kind==Shape.K.Line||selectedShape.Kind==Shape.K.Arrow)&&TryBeginDirectDrag(p))return;BeginSelectionDragCore(p,true,null); }

        private bool TryBeginDirectDrag(PointF p)
        {
            float tolerance=6f/Math.Max(.1f,zoom);
            if(selectedShape!=null&&shapes.Contains(selectedShape)&&(selectedShape.Kind==Shape.K.Line||selectedShape.Kind==Shape.K.Arrow)){
                PointF a=selectedShape.Rect.Location,b=new PointF(selectedShape.Rect.Right,selectedShape.Rect.Bottom);
                int endpoint=SegmentDistance(p,a,a)<=tolerance ? 1 : SegmentDistance(p,b,b)<=tolerance ? 2 : 0;
                if(endpoint!=0){BeginSelectionDragCore(p,false,selectedShape);selectionEndpoint=endpoint;return true;}
            }
            for(int i=shapes.Count-1;i>=0;i--)
                if(HitShape(shapes[i],p,tolerance)) {BeginSelectionDragCore(p,false,shapes[i]);return true;}
            selectedShape=null;return false;
        }

        private static float SegmentDistance(PointF p,PointF a,PointF b)
        {
            float dx=b.X-a.X,dy=b.Y-a.Y,length=dx*dx+dy*dy;
            float t=length<=.001f ? 0 : Math.Max(0,Math.Min(1,((p.X-a.X)*dx+(p.Y-a.Y)*dy)/length));
            dx=p.X-a.X-t*dx;dy=p.Y-a.Y-t*dy;return (float)Math.Sqrt(dx*dx+dy*dy);
        }

        private bool HitShape(Shape shape,PointF p,float tolerance)
        {
            if(shape.Kind==Shape.K.Eraser)return false;
            if(!shape.AiClip.IsEmpty && !shape.AiClip.Contains(p))return false;
            float radius=Math.Max(tolerance,shape.Width/2f+2f/Math.Max(.1f,zoom));
            if(shape.Kind==Shape.K.Line || shape.Kind==Shape.K.Arrow){
                PointF a=shape.Rect.Location,b=new PointF(shape.Rect.Right,shape.Rect.Bottom);
                if(SegmentDistance(p,a,b)<=radius)return true;
                if(shape.Kind==Shape.K.Arrow){
                    float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                    if(length>.01f){
                        float ux=dx/length,uy=dy/length,head=Math.Min(Math.Max(10,shape.Width*3.6f),length*.8f),half=head*.45f;
                        PointF c=new PointF(b.X-ux*head,b.Y-uy*head);
                        using(var path=new System.Drawing.Drawing2D.GraphicsPath()){
                            path.AddPolygon(new PointF[]{b,new PointF(c.X-uy*half,c.Y+ux*half),new PointF(c.X+uy*half,c.Y-ux*half)});
                            if(path.IsVisible(p))return true;
                        }
                    }
                }
                return false;
            }
            if(shape.Kind==Shape.K.Pen || shape.Brush){
                if(shape.Points.Count==1)return SegmentDistance(p,shape.Points[0],shape.Points[0])<=radius;
                for(int i=1;i<shape.Points.Count;i++)if(SegmentDistance(p,shape.Points[i-1],shape.Points[i])<=radius)return true;
                return false;
            }
            RectangleF bounds=ShapeBounds(shape);
            if(shape.Kind==Shape.K.Rect || shape.Kind==Shape.K.Ellipse){
                using(var path=new System.Drawing.Drawing2D.GraphicsPath())using(Pen hitPen=new Pen(Color.Black,radius*2)){
                    if(bounds.Width<=0 || bounds.Height<=0)return false;
                    if(shape.Kind==Shape.K.Rect)path.AddRectangle(bounds);else path.AddEllipse(bounds);
                    return path.IsOutlineVisible(p,hitPen);
                }
            }
            bounds.Inflate(tolerance,tolerance);return bounds.Contains(p);
        }

        private void BeginSelectionDragCore(PointF p,bool allowResize,Shape target)
        {
            selectionEndpoint=0;
            float tolerance = 8f / Math.Max(0.1f,zoom);
            selectionResize = false;
            if (allowResize && selectedShape != null && shapes.Contains(selectedShape))
            {
                RectangleF bounds = ShapeBounds(selectedShape);
                selectionResize = Math.Abs(p.X-bounds.Right) <= tolerance && Math.Abs(p.Y-bounds.Bottom) <= tolerance;
            }
            if (!selectionResize)
            {
                selectedShape = target;
                for (int i = shapes.Count-1; selectedShape==null && i >= 0; i--)
                {
                    RectangleF hit = ShapeBounds(shapes[i]); hit.Inflate(tolerance,tolerance);
                    if (hit.Contains(p)) { selectedShape = shapes[i]; break; }
                }
            }
            if (selectedShape == null) { LayoutToolProperties();Invalidate(); return; }
            LayoutToolProperties();
            selectionOriginal = selectedShape.Clone(); selectionStart = p;
            selectionCheckpoint = CaptureEditState(); selectionChanged = false;
            selectionDragging = true; Capture = true;
            SetStatus(allowResize ? "拖动移动 · 右下角缩放 · 颜色/线宽可修改 · 双击文字编辑 · Delete 删除" : "拖动移动标注 · Ctrl+Z 撤销 · Shift 可在标注上绘制 · 选择工具可缩放");
            Invalidate();
        }

        private void MoveSelection(PointF p)
        {
            if (selectedShape == null || selectionOriginal == null) return;
            if(selectionEndpoint!=0){
                PointF a=selectionOriginal.Rect.Location,b=new PointF(selectionOriginal.Rect.Right,selectionOriginal.Rect.Bottom);
                if(selectionEndpoint==1)a=ClampToImage(p);else b=ClampToImage(p);
                selectedShape.Rect=new RectangleF(a.X,a.Y,b.X-a.X,b.Y-a.Y);selectionChanged=selectedShape.Rect!=selectionOriginal.Rect;commitVersion++;Invalidate();return;
            }
            RectangleF bounds = ShapeBounds(selectionOriginal);
            float dx = p.X-selectionStart.X, dy = p.Y-selectionStart.Y;
            float sx = 1f, sy = 1f;
            if (selectionResize)
            {
                sx = Math.Max(2, Math.Min(Src.Width-bounds.Left,bounds.Width+dx)) / Math.Max(1,bounds.Width);
                sy = Math.Max(2, Math.Min(Src.Height-bounds.Top,bounds.Height+dy)) / Math.Max(1,bounds.Height);
                dx = dy = 0;
            }
            else
            {
                dx = Math.Max(-bounds.Left, Math.Min(Src.Width-bounds.Right,dx));
                dy = Math.Max(-bounds.Top, Math.Min(Src.Height-bounds.Bottom,dy));
            }
            RectangleF original = selectionOriginal.Rect;
            selectedShape.Rect = new RectangleF(bounds.Left+(original.X-bounds.Left)*sx+dx,
                bounds.Top+(original.Y-bounds.Top)*sy+dy, original.Width*sx, original.Height*sy);
            selectedShape.Points.Clear();
            foreach (PointF point in selectionOriginal.Points)
                selectedShape.Points.Add(new PointF(bounds.Left+(point.X-bounds.Left)*sx+dx,bounds.Top+(point.Y-bounds.Top)*sy+dy));
            if (selectedShape.Kind == Shape.K.Text && selectionResize)
                selectedShape.FontPx = Math.Max(8,Math.Min(160,selectionOriginal.FontPx*Math.Min(sx,sy)));
            selectionChanged = Math.Abs(dx)>0.01f || Math.Abs(dy)>0.01f || Math.Abs(sx-1)>0.001f || Math.Abs(sy-1)>0.001f;
            commitVersion++; Invalidate();
        }

        private void FinishSelectionDrag()
        {
            selectionDragging = false; Capture = false;
            UndoStep state = selectionCheckpoint; selectionCheckpoint = null;
            if (selectionChanged) CommitEditState(state); else state.DisposeSnap();
        }

        private void CancelSelectionDrag()
        {
            selectionDragging = false;
            if (selectionCheckpoint != null) { SwapSnapshotState(selectionCheckpoint); selectionCheckpoint.DisposeSnap(); selectionCheckpoint = null; }
            commitVersion++; Invalidate();
        }

        private void EditSelectedStyle()
        {
            if (tool != Shape.K.Select || selectedShape == null || compareBefore || !shapes.Contains(selectedShape)) return;
            UndoStep state = CaptureEditState();
            selectedShape.Color = color; selectedShape.Width = strokeW;
            CommitEditState(state);
        }

        private void DeleteSelected()
        {
            UndoStep state = CaptureEditState(); shapes.Remove(selectedShape); selectedShape = null; CommitEditState(state);LayoutToolProperties();
        }

        private void DrawSelection(Graphics g)
        {
            if (selectedShape == null || !shapes.Contains(selectedShape) || compareBefore || aiRegionTool || maskTool) return;
            if(selectedShape.Kind==Shape.K.Line || selectedShape.Kind==Shape.K.Arrow){
                RectangleF endpoints=ImageRectToScreen(selectedShape.Rect);
                using(SolidBrush brush=new SolidBrush(Color.FromArgb(110,190,255))){g.FillRectangle(brush,endpoints.Left-4,endpoints.Top-4,8,8);g.FillRectangle(brush,endpoints.Right-4,endpoints.Bottom-4,8,8);}
                return;
            }
            RectangleF r = ImageRectToScreen(ShapeBounds(selectedShape));
            using (Pen pen = new Pen(Color.FromArgb(110,190,255),1)) { pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash; g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height); }
            if(tool==Shape.K.Select) using (SolidBrush brush = new SolidBrush(Color.FromArgb(110,190,255))) g.FillRectangle(brush,r.Right-4,r.Bottom-4,8,8);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (tool != Shape.K.Select || selectedShape == null || selectedShape.Kind != Shape.K.Text || compareBefore) return;
            if (selectionDragging) FinishSelectionDrag();
            using (ThemedDialog dialog = new ThemedDialog())
            using (TextBox input = new TextBox())
            using (DshButton ok = new DshButton(UiText.T("确定"),DshButton.Kind.Primary))
            {
                dialog.Text = "编辑文字"; dialog.ClientSize = new Size(420,204); dialog.StartPosition = FormStartPosition.CenterParent;
                input.Multiline = true; input.SetBounds(12,12,396,92); input.Text = selectedShape.Text; input.BackColor=Dsh.GlassFillStrong; input.ForeColor=Dsh.TextPrimary; input.BorderStyle=BorderStyle.None;
                var frame=new DshInputFrame(input); frame.SetBounds(12,12,396,92); dialog.ContentArea.Controls.Add(frame);
                ok.Text = UiText.T("确定"); ok.SetBounds(320,116,88,30); ok.Click += delegate { dialog.DialogResult=DialogResult.OK; }; dialog.ContentArea.Controls.Add(ok);
                input.KeyDown += delegate(object sender,KeyEventArgs args) { if (args.Control && args.KeyCode==Keys.Enter) { args.SuppressKeyPress=true; dialog.DialogResult=DialogResult.OK; } };
                if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(input.Text))
                { UndoStep state = CaptureEditState(); selectedShape.Text = input.Text; CommitEditState(state); }
            }
        }
    }
}
