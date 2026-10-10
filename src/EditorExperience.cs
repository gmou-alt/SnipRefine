using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace SnapGrab {
 internal partial class EditorForm {
  private Point brushCursor;
  protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);mouseIn=false;Invalidate(ViewRect);}
  private void DrawBrushCursor(Graphics graphics){if(!mouseIn||aiRegionTool||maskTool||compareBefore||(tool!=Shape.K.Eraser&&!(effectBrush&&EffectTool(tool))))return;float size=Math.Max(4,effectBrushSize*zoom);RectangleF circle=new RectangleF(brushCursor.X-size/2,brushCursor.Y-size/2,size,size);using(Pen edge=new Pen(Color.White,3))graphics.DrawEllipse(edge,circle);using(Pen ink=new Pen(Dsh.PrimaryFill,1.5f))graphics.DrawEllipse(ink,circle);}
  private bool effectBrush;
  private WideButton effectBrushButton,effectSizeButton;
  private bool IsWorkspaceBrush(Control button){return button==effectBrushButton||button==effectSizeButton;}
  private void RefreshEffectBrushLabel(){if(effectBrushButton!=null)effectBrushButton.Text=UiText.T(tool==Shape.K.Redact ? "遮挡画笔" : tool==Shape.K.Blur ? "模糊画笔" : "马赛克画笔");}
  private int effectBrushSize=32,selectionEndpoint;
  private static bool EffectTool(Shape.K kind){return kind==Shape.K.Mosaic||kind==Shape.K.Redact||kind==Shape.K.Blur;}
  private void BuildExperienceControls(){
   ImageEditButton("马赛克画笔",delegate {if(!EffectTool(tool)){tool=Shape.K.Mosaic;effectBrush=true;}else effectBrush=!effectBrush;aiRegionTool=false;maskTool=false;RefreshEffectBrushLabel();LayoutChrome();SetStatus(effectBrush ? "马赛克/遮挡/模糊：涂抹模式 · 选择工具可移动笔画" : "马赛克/遮挡/模糊：框选模式");Invalidate();},delegate {return effectBrush&&EffectTool(tool)&&!aiRegionTool&&!maskTool;});
   effectBrushButton=wideButtons[wideButtons.Count-1];
   ImageEditButton("画笔直径：32px",delegate(object sender,EventArgs e){int[] sizes={8,16,32,64,96};effectBrushSize=sizes[(Array.IndexOf(sizes,effectBrushSize)+1)%sizes.Length];((WideButton)sender).Text="画笔直径："+effectBrushSize+"px";LayoutChrome();Invalidate();});
   effectSizeButton=wideButtons[wideButtons.Count-1];
  }
  private string CurrentToolLabel(){
   if(aiRegionTool)return "AI 框选";
   if(maskTool)return maskProtect ? "保护画笔" : "恢复画笔";
   foreach(ToolButton button in toolButtons)if(button.Kind==tool)return button.Text+(EffectTool(tool) ? (effectBrush ? " · 涂抹" : " · 框选") : "");
   return tool.ToString();
  }
  internal static RectangleF ConstrainedBox(PointF anchor,PointF point,Size image,bool square,bool center){
   float dx=point.X-anchor.X,dy=point.Y-anchor.Y,w=Math.Abs(dx),h=Math.Abs(dy);
   float maxW=center ? Math.Min(anchor.X,image.Width-anchor.X) : (dx<0 ? anchor.X : image.Width-anchor.X);
   float maxH=center ? Math.Min(anchor.Y,image.Height-anchor.Y) : (dy<0 ? anchor.Y : image.Height-anchor.Y);
   if(square)w=h=Math.Min(Math.Max(w,h),Math.Min(maxW,maxH));else {w=Math.Min(w,maxW);h=Math.Min(h,maxH);}
   if(center)return new RectangleF(anchor.X-w,anchor.Y-h,w*2,h*2);
   return new RectangleF(dx<0 ? anchor.X-w : anchor.X,dy<0 ? anchor.Y-h : anchor.Y,w,h);
  }
  private static Region BrushRegion(Shape shape){
   Region region=new Region();region.MakeEmpty();float radius=shape.Width/2;
   for(int i=0;i<shape.Points.Count;i++){
    PointF b=shape.Points[i];using(GraphicsPath dab=new GraphicsPath()){dab.AddEllipse(b.X-radius,b.Y-radius,radius*2,radius*2);region.Union(dab);}
    if(i==0)continue;PointF a=shape.Points[i-1];float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);if(length<.01f)continue;
    float x=-dy/length*radius,y=dx/length*radius;
    using(GraphicsPath segment=new GraphicsPath()){segment.AddPolygon(new PointF[]{new PointF(a.X+x,a.Y+y),new PointF(b.X+x,b.Y+y),new PointF(b.X-x,b.Y-y),new PointF(a.X-x,a.Y-y)});region.Union(segment);}
   }
   return region;
  }
  private void UpdateEffectStroke(Shape shape,PointF p){
   if(shape.Points.Count==0||SegmentDistance(p,shape.Points[shape.Points.Count-1],shape.Points[shape.Points.Count-1])>=1)shape.Points.Add(p);
   float left=p.X,top=p.Y,right=p.X,bottom=p.Y;
   foreach(PointF q in shape.Points){left=Math.Min(left,q.X);top=Math.Min(top,q.Y);right=Math.Max(right,q.X);bottom=Math.Max(bottom,q.Y);}
   float radius=shape.Width/2;
   shape.Rect=RectangleF.Intersect(RectangleF.FromLTRB((float)Math.Floor(left-radius),(float)Math.Floor(top-radius),(float)Math.Ceiling(right+radius),(float)Math.Ceiling(bottom+radius)),new RectangleF(PointF.Empty,Src.Size));
  }
  private bool NudgeSelected(Keys key){
   Keys code=key&Keys.KeyCode;if(code!=Keys.Left&&code!=Keys.Right&&code!=Keys.Up&&code!=Keys.Down)return false;
   if((key&(Keys.Control|Keys.Alt))!=0||selectedShape==null||!shapes.Contains(selectedShape)||compareBefore||aiRegionTool||maskTool||draft!=null||selectionDragging)return false;
   float step=(key&Keys.Shift)!=0 ? 10 : 1,dx=code==Keys.Left ? -step : code==Keys.Right ? step : 0,dy=code==Keys.Up ? -step : code==Keys.Down ? step : 0;
   RectangleF bounds=ShapeBounds(selectedShape);dx=Math.Max(-bounds.Left,Math.Min(Src.Width-bounds.Right,dx));dy=Math.Max(-bounds.Top,Math.Min(Src.Height-bounds.Bottom,dy));
   if(dx==0&&dy==0)return true;
   UndoStep state=CaptureEditState();RectangleF rect=selectedShape.Rect;rect.Offset(dx,dy);selectedShape.Rect=rect;
   for(int i=0;i<selectedShape.Points.Count;i++)selectedShape.Points[i]=new PointF(selectedShape.Points[i].X+dx,selectedShape.Points[i].Y+dy);
   CommitEditState(state);return true;
  }
  private void UpdateToolCursor(PointF p){
   if(!mouseIn||aiRegionTool||maskTool||(effectBrush&&EffectTool(tool))||tool==Shape.K.Eraser){Cursor=mouseIn ? Cursors.Cross : Cursors.Default;return;}
   if(selectedShape!=null&&(selectedShape.Kind==Shape.K.Arrow||selectedShape.Kind==Shape.K.Line)){
    float tolerance=7f/Math.Max(.1f,zoom);
    if(SegmentDistance(p,selectedShape.Rect.Location,selectedShape.Rect.Location)<=tolerance||SegmentDistance(p,new PointF(selectedShape.Rect.Right,selectedShape.Rect.Bottom),new PointF(selectedShape.Rect.Right,selectedShape.Rect.Bottom))<=tolerance){Cursor=Cursors.SizeNWSE;return;}
   }
   for(int i=shapes.Count-1;i>=0;i--)if(HitShape(shapes[i],p,6f/Math.Max(.1f,zoom))){Cursor=Cursors.SizeAll;return;}
   Cursor=Cursors.Cross;
  }
 }
}
