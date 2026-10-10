using System;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
namespace SnapGrab {
 internal static class EditorExperienceTests {
  const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  static object Call(EditorForm e,string method,params object[] args){return typeof(EditorForm).GetMethod(method,Private).Invoke(e,args);}
  internal static void Constraints(){
   PointF anchor=new PointF(50,50),point=new PointF(70,80);Size size=new Size(200,160);
   Check(EditorForm.ConstrainedBox(anchor,point,size,false,false)==new RectangleF(50,50,20,30),"Plain rectangle");
   Check(EditorForm.ConstrainedBox(anchor,point,size,true,false)==new RectangleF(50,50,30,30),"Shift circle");
   Check(EditorForm.ConstrainedBox(anchor,point,size,false,true)==new RectangleF(30,20,40,60),"Alt center ellipse");
   Check(EditorForm.ConstrainedBox(anchor,point,size,true,true)==new RectangleF(20,20,60,60),"Shift Alt center circle");
   RectangleF edge=EditorForm.ConstrainedBox(new PointF(5,6),new PointF(80,90),size,true,true);
   Check(edge.Width==edge.Height&&edge.Left==0&&edge.Top>=0,"Constrained shape crossed bounds");
  }
  internal static void BrushPixels(){
   BrushMouseInput();
   foreach(Shape.K kind in new Shape.K[]{Shape.K.Mosaic,Shape.K.Redact,Shape.K.Blur}){
    using(Bitmap source=new Bitmap(160,120)){
     for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)source.SetPixel(x,y,(x+y)%2==0 ? Color.White : Color.Black);
     using(EditorForm editor=new EditorForm(null,source,new Config())){
      Shape shape=new Shape {Kind=kind,Brush=true,Width=20,Block=12,Color=Color.Red};
      Call(editor,"UpdateEffectStroke",shape,new PointF(20,20));Call(editor,"UpdateEffectStroke",shape,new PointF(120,90));Call(editor,"AddShapeTracked",shape);
      using(Bitmap result=editor.RenderResult()){
       Check(result.GetPixel(80,20).ToArgb()==source.GetPixel(80,20).ToArgb(),"Brush edited blank area in bounding box");
       int changed=0;for(int y=0;y<120;y++)for(int x=0;x<160;x++)if(result.GetPixel(x,y).ToArgb()!=source.GetPixel(x,y).ToArgb())changed++;
       Check(changed>0,"Brush effect missing");
      }
      Call(editor,"DoUndo");using(Bitmap result=editor.RenderResult())Check(result.GetPixel(70,55).ToArgb()==source.GetPixel(70,55).ToArgb(),"Brush undo");
      Call(editor,"DoRedo");var list=(List<Shape>)typeof(EditorForm).GetField("shapes",Private).GetValue(editor);Check(list.Count==1&&list[0].Brush&&list[0].Points.Count==2,"Brush geometry lost in redo");
      Check((bool)Call(editor,"HitShape",list[0],new PointF(70,55),4f)&&!(bool)Call(editor,"HitShape",list[0],new PointF(80,20),4f),"Brush hit testing used rectangular bounds");
     }
    }
   }
  }
  internal static void BrushMouseInput(){
   using(Bitmap source=new Bitmap(160,120))using(EditorForm editor=new EditorForm(null,source,new Config())){
    editor.ClientSize=new Size(1000,700);editor.Show();Application.DoEvents();Call(editor,"EnsureZoomInit");
    typeof(EditorForm).GetField("tool",Private).SetValue(editor,Shape.K.Mosaic);typeof(EditorForm).GetField("effectBrush",Private).SetValue(editor,true);
    RectangleF screen=(RectangleF)Call(editor,"ImageRectToScreen",new RectangleF(30,30,60,40));
    Point a=Point.Round(screen.Location),b=Point.Round(new PointF(screen.Right,screen.Bottom));
    for(int i=0;i<2;i++){
     Call(editor,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,a.X,a.Y,0));
     Call(editor,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,1,b.X,b.Y,0));
     Call(editor,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,b.X,b.Y,0));
    }
    var list=(List<Shape>)typeof(EditorForm).GetField("shapes",Private).GetValue(editor);
    Check(list.Count==2&&list[0].Brush&&list[1].Brush&&list[0].Points.Count>=2,"Overlapping brush input dragged old stroke or lost points");
   }
  }
  internal static void EndpointsAndNudge(){
   using(Bitmap source=new Bitmap(200,160))using(EditorForm editor=new EditorForm(null,source,new Config())){
    Shape shape=new Shape {Kind=Shape.K.Arrow,Color=Color.Red,Width=8,Rect=new RectangleF(30,30,100,80)};Call(editor,"AddShapeTracked",shape);
    typeof(EditorForm).GetField("selectedShape",Private).SetValue(editor,shape);
    Check((bool)Call(editor,"TryBeginDirectDrag",new PointF(130,110)),"Endpoint not found");Call(editor,"MoveSelection",new PointF(140,80));Call(editor,"FinishSelectionDrag");
    Check(shape.Rect.Location==new PointF(30,30)&&shape.Rect.Right==140&&shape.Rect.Bottom==80,"Endpoint moved whole arrow");
    Check((bool)Call(editor,"NudgeSelected",Keys.Right)&&shape.Rect.X==31,"One pixel nudge");
    Check((bool)Call(editor,"NudgeSelected",Keys.Shift|Keys.Down)&&shape.Rect.Y==40,"Ten pixel nudge");
    Call(editor,"DoUndo");var list=(List<Shape>)typeof(EditorForm).GetField("shapes",Private).GetValue(editor);Check(list[0].Rect.Y==30,"Nudge undo");
    Call(editor,"DoUndo");Call(editor,"DoUndo");Check(list[0].Rect==new RectangleF(30,30,100,80),"Endpoint undo");
   }
  }
 }
}
