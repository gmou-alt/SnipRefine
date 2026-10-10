using System;using System.Drawing;using System.Reflection;using System.Collections.Generic;using System.Windows.Forms;
namespace SnapGrab {internal static class EraserTests {
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static object Call(object obj,string name,params object[] args){return obj.GetType().GetMethod(name,Private).Invoke(obj,args);}static void Check(bool value,string message){if(!value)throw new Exception(message);}
 internal static void Run(){
  foreach(bool transparent in new[]{false,true})using(Bitmap source=new Bitmap(160,120)){
   using(Graphics g=Graphics.FromImage(source))g.Clear(transparent ? Color.Transparent : Color.White);
   using(EditorForm editor=new EditorForm(null,source,new Config())){
    editor.ClientSize=new Size(1000,700);editor.Show();Application.DoEvents();
    Call(editor,"AddShapeTracked",new Shape {Kind=Shape.K.Redact,Color=Color.Red,Rect=new RectangleF(10,10,130,90)});
    typeof(EditorForm).GetField("tool",Private).SetValue(editor,Shape.K.Eraser);
    RectangleF mapped=(RectangleF)Call(editor,"ImageRectToScreen",new RectangleF(40,40,40,20));Point start=Point.Round(mapped.Location),end=Point.Round(new PointF(mapped.Right,mapped.Bottom));
    Call(editor,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,start.X,start.Y,0));Call(editor,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,1,end.X,end.Y,0));Call(editor,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,end.X,end.Y,0));
    List<Shape> shapes=(List<Shape>)typeof(EditorForm).GetField("shapes",Private).GetValue(editor);Check(shapes.Count==2&&shapes[1].Kind==Shape.K.Eraser,"Eraser dragged annotation or did not commit");
    using(Bitmap result=editor.RenderResult()){Check(result.GetPixel(60,50).ToArgb()==source.GetPixel(60,50).ToArgb(),"Eraser does not restore source alpha/pixels");Check(result.GetPixel(120,80).R==255&&result.GetPixel(120,80).G==0,"Eraser removed untouched annotation");}
    Call(editor,"DoUndo");using(Bitmap result=editor.RenderResult())Check(result.GetPixel(60,50).G==0&&result.GetPixel(60,50).A==255,"Eraser undo did not restore annotation");
    Call(editor,"DoRedo");using(Bitmap result=editor.RenderResult())Check(result.GetPixel(60,50).ToArgb()==source.GetPixel(60,50).ToArgb(),"Eraser redo lost geometry");
    Call(editor,"AddShapeTracked",new Shape {Kind=Shape.K.Redact,Color=Color.Blue,Rect=new RectangleF(55,45,10,10)});using(Bitmap result=editor.RenderResult())Check(result.GetPixel(60,50).B==255&&result.GetPixel(60,50).A==255,"New annotation cannot draw over erasure");
   }
  }
 }
}}
