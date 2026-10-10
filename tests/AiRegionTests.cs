using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
namespace SnapGrab {
 internal static class AiRegionTests {
  private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  private static void Check(bool value,string message) {if(!value) throw new Exception(message);}
  private static object Call(EditorForm e,string method,params object[] args) {return typeof(EditorForm).GetMethod(method,Private).Invoke(e,args);}
  internal static void UploadAndMapping() {
   using(Bitmap image=new Bitmap(301,203)) {
    using(Graphics g=Graphics.FromImage(image)) {g.Clear(Color.Red);g.FillRectangle(Brushes.Blue,101,50,101,79);}
    Size size;string data=AiRegionRequest.Encode(image,new Rectangle(101,50,101,79),60,out size);
    Check(data.StartsWith("data:image/png;base64,"),"Regional upload must use PNG");
    using(MemoryStream stream=new MemoryStream(Convert.FromBase64String(data.Substring(data.IndexOf(',')+1))))
    using(Bitmap local=new Bitmap(stream)) {
     Check(local.Size==size && size.Width==60 && size.Height==47,"Rounded upload dimensions");
     Check(local.GetPixel(30,23).B>200 && local.GetPixel(30,23).R<20,"Uploaded outside region");
    }
    AiReply reply=new AiReply {Ops=new List<AiOp>{new AiOp {Kind="redact",X=0,Y=0,W=60,H=47}}};
    AiRegionRequest.MapAndValidate(reply,new Rectangle(101,50,101,79),size);
    Check(reply.Ops[0].X==101 && reply.Ops[0].Y==50 && Math.Abs(reply.Ops[0].W-101)<.001 && Math.Abs(reply.Ops[0].H-79)<.001,"Independent X/Y mapping");
   }
  }
  internal static void RejectInvalid() {
   foreach(AiOp invalid in new AiOp[]{new AiOp {Kind="crop"},new AiOp {Kind="redact",X=-1},new AiOp {Kind="text",X=float.NaN},new AiOp {Kind="arrow",X2=101},new AiOp {Kind="redact",W=101},new AiOp {Kind="script"}}) {
    AiOp valid=new AiOp {Kind="text",X=2,Y=3,Text="ok"};
    var reply=new AiReply {Ops=new List<AiOp>{valid,invalid}};bool rejected=false;
    try {AiRegionRequest.MapAndValidate(reply,new Rectangle(30,20,100,80),new Size(100,80));} catch(InvalidOperationException){rejected=true;}
    Check(rejected && valid.X==2 && valid.Y==3,"Invalid batch partially transformed");
   }
  }
  internal static void MouseCoordinates() {
   using(Bitmap source=new Bitmap(160,120))
   using(EditorForm editor=new EditorForm(null,source,new Config())) {
    editor.ClientSize=new Size(940,620);editor.Show();System.Windows.Forms.Application.DoEvents();Call(editor,"EnsureZoomInit");
    foreach(float zoom in new float[]{0.5f,1f,2f}) {
     typeof(EditorForm).GetField("zoom",Private).SetValue(editor,zoom);
     typeof(EditorForm).GetField("pan",Private).SetValue(editor,new Point(5,-7));
     typeof(EditorForm).GetField("aiRegionTool",Private).SetValue(editor,true);
     RectangleF screen=(RectangleF)Call(editor,"ImageRectToScreen",new RectangleF(20,20,60,40));
     var start=Point.Round(new PointF(screen.Left,screen.Top));var end=Point.Round(new PointF(screen.Right,screen.Bottom));
     Call(editor,"OnMouseDown",new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,start.X,start.Y,0));
     Call(editor,"OnMouseMove",new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,end.X,end.Y,0));
     Call(editor,"OnMouseUp",new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,end.X,end.Y,0));
     Rectangle region=(Rectangle)typeof(EditorForm).GetField("aiRegion",Private).GetValue(editor);
     Check(Math.Abs(region.X-20)<=1 && Math.Abs(region.Y-20)<=1 && Math.Abs(region.Width-60)<=1 && Math.Abs(region.Height-40)<=1,"Zoom/pan changes selected source coordinates: "+zoom+" "+region);
     using(Bitmap preview=(Bitmap)Call(editor,"BuildAiTargetPreview",new AiReply {Ops=new List<AiOp>{new AiOp {Kind="ocr",X=region.X+2,Y=region.Y+2,W=20,H=10,Text="sample"}}})) Check(preview.Size==region.Size,"Target preview is not local crop");
    }
   }
  }
  internal static void ClipAndUndo() {
   using(Bitmap source=new Bitmap(100,80)) {
    using(Graphics g=Graphics.FromImage(source)) g.Clear(Color.White);
    using(EditorForm editor=new EditorForm(null,source,new Config()))
    using(Bitmap baseline=editor.RenderResult()) {
     Rectangle region=new Rectangle(20,20,40,30);
     typeof(EditorForm).GetField("activeAiRegion",Private).SetValue(editor,region);
     Call(editor,"AiHandleReply",new AiReply {Say="test",Ops=new List<AiOp>{new AiOp {Kind="redact",X=20,Y=20,W=40,H=30,Color="black"},new AiOp {Kind="text",X=55,Y=40,Text="long overflowing text",Color="red"}}},null);
     using(Bitmap output=editor.RenderResult()) {
      Check(output.GetPixel(30,30).R<20,"Regional edit not applied: "+string.Join(" | ",((List<string>)typeof(EditorForm).GetField("aiHistory",Private).GetValue(editor)).ToArray()));
      for(int y=0;y<80;y++) for(int x=0;x<100;x++) if(!region.Contains(x,y)) Check(output.GetPixel(x,y).ToArgb()==baseline.GetPixel(x,y).ToArgb(),"Pixels changed outside region");
     }
     Call(editor,"DoUndo");using(Bitmap restored=editor.RenderResult()) Check(restored.GetPixel(30,30).ToArgb()==baseline.GetPixel(30,30).ToArgb(),"Regional undo");
     Call(editor,"DoRedo");using(Bitmap restored=editor.RenderResult()) Check(restored.GetPixel(30,30).R<20 && restored.GetPixel(65,42).ToArgb()==baseline.GetPixel(65,42).ToArgb(),"Redo lost clipping");
     // Selection overlays never enter export; coordinates remain in source pixels.
     Call(editor,"SetAiRegion",new Rectangle(5,6,15,16));using(Bitmap output=editor.RenderResult()) Check(output.GetPixel(5,6).ToArgb()==baseline.GetPixel(5,6).ToArgb(),"AI selection exported");
    }
   }
  }
 }
}
