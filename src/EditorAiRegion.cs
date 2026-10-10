using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace SnapGrab {
 internal partial class EditorForm {
  private Rectangle aiRegion,activeAiRegion;
  private bool aiRegionTool,aiRegionDrawing;
  private PointF aiRegionAnchor;
  private int aiRegionRevision;
  private List<AiOp> aiTargets;
  private void BuildAiRegionControls() {
   BuildImageEditControls();
   WideButton select=new WideButton {Text=UiText.T("AI 框选"),ActiveState=delegate {return aiRegionTool;}};
   select.Click+=delegate { if(aiBusy) {AiSetStatus(UiText.T("请先取消当前请求"));return;} CommitOpenTextInput();SetAiPanel(true);maskTool=false;maskDrawing=false;aiRegionTool=true;compareBefore=false;LayoutToolProperties();AiSetStatus(UiText.T("拖动框选分析区域；图片重绘还会发送周围上下文"));Invalidate(); };
   wideButtons.Add(select);Controls.Add(select);
   WideButton clear=new WideButton {Text=UiText.T("清除AI选区")};
   clear.Click+=delegate {if(aiBusy) {AiSetStatus(UiText.T("请先取消当前请求"));return;} aiRegionTool=false;SetAiRegion(Rectangle.Empty);LayoutToolProperties();AiSetStatus(UiText.T("发送范围：整张当前图片"));};
   wideButtons.Add(clear);Controls.Add(clear);
  }
  private void UpdateAiUploadTitle(){if(aiTitle!=null)aiTitle.Text=UiText.T(aiRegion.IsEmpty ? "AI · 分析上传整图" : "AI · 分析仅上传选区");}
  private void SetAiRegion(Rectangle region) {ResetImageEditMask();aiRegion=region;aiRegionRevision++;UpdateAiUploadTitle();Invalidate();}
  private void BeginAiRegion(PointF point) {aiRegionAnchor=point;aiRegionDrawing=true;SetAiRegion(Rectangle.Empty);Capture=true;}
  private void MoveAiRegion(PointF point) {
   int left=(int)Math.Floor(Math.Min(point.X,aiRegionAnchor.X)),top=(int)Math.Floor(Math.Min(point.Y,aiRegionAnchor.Y));
   int right=(int)Math.Ceiling(Math.Max(point.X,aiRegionAnchor.X)),bottom=(int)Math.Ceiling(Math.Max(point.Y,aiRegionAnchor.Y));
   Rectangle region=Rectangle.Intersect(Rectangle.FromLTRB(left,top,right,bottom),new Rectangle(Point.Empty,Src.Size));
   SetAiRegion(region.Width>=2 && region.Height>=2 ? region : Rectangle.Empty);
  }
  private void DrawAiRegion(Graphics g) {
   DrawProtection(g);
   if(!aiRegion.IsEmpty) {
    RectangleF r=ImageRectToScreen(aiRegion);
    using(Pen outer=new Pen(Color.Black,4)) g.DrawRectangle(outer,r.X,r.Y,r.Width,r.Height);
    using(Pen inner=new Pen(Color.Cyan,2)) {inner.DashStyle=DashStyle.Dash;g.DrawRectangle(inner,r.X,r.Y,r.Width,r.Height);}
   }
   if(aiTargets==null) return;
   int index=0;
   foreach(AiOp op in aiTargets) {
    index++;if(op==null) continue;
    RectangleF bounds=new RectangleF(op.X,op.Y,Math.Max(8,op.W),Math.Max(8,op.H));
    if(op.Kind=="line"||op.Kind=="arrow") bounds=RectangleF.FromLTRB(Math.Min(op.X,op.X2),Math.Min(op.Y,op.Y2),Math.Max(op.X,op.X2),Math.Max(op.Y,op.Y2));
    RectangleF r=ImageRectToScreen(bounds);
    using(Pen pen=new Pen(Color.Orange,3)) g.DrawRectangle(pen,r.X,r.Y,Math.Max(2,r.Width),Math.Max(2,r.Height));
    using(SolidBrush brush=new SolidBrush(Color.Orange)) g.DrawString(index.ToString(),Font,brush,r.Location);
   }
  }
  private Bitmap BuildAiTargetPreview(AiReply reply) {
   if(aiRegion.IsEmpty) return null;
   using(Bitmap full=RenderResult()) {
    Bitmap preview=full.Clone(aiRegion,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    try {
     using(Graphics g=Graphics.FromImage(preview)) {
      int index=0;
      foreach(AiOp op in reply.Ops) {
       index++;if(op==null) continue;
       RectangleF bounds=new RectangleF(op.X-aiRegion.X,op.Y-aiRegion.Y,Math.Max(8,op.W),Math.Max(8,op.H));
       if(op.Kind=="arrow"||op.Kind=="line") bounds=RectangleF.FromLTRB(Math.Min(op.X,op.X2)-aiRegion.X,Math.Min(op.Y,op.Y2)-aiRegion.Y,Math.Max(op.X,op.X2)-aiRegion.X,Math.Max(op.Y,op.Y2)-aiRegion.Y);
       using(Pen pen=new Pen(Color.Orange,2)) g.DrawRectangle(pen,bounds.X,bounds.Y,Math.Max(2,bounds.Width),Math.Max(2,bounds.Height));
       using(SolidBrush brush=new SolidBrush(Color.Orange)) g.DrawString(index.ToString(),Font,brush,bounds.Location);
      }
     }
     return preview;
    } catch {preview.Dispose();throw;}
   }
  }
  private bool ReviewRegionReply(AiReply reply,Rectangle region) {
   aiTargets=reply.Ops;Invalidate();Update();
   try {return ReviewAiOperations(reply);} finally {aiTargets=null;Invalidate();}
  }
 }
}
