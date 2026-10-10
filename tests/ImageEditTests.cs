using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
namespace SnapGrab {
 internal static class ImageEditTests {
  private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  private static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
  internal static void MaskAndMerge(){
   using(Bitmap source=new Bitmap(140,100))
   using(Bitmap mask=new Bitmap(40,30))
   using(Bitmap generated=new Bitmap(1024,1024)){
    using(Graphics g=Graphics.FromImage(source))g.Clear(Color.Blue);
    using(Graphics g=Graphics.FromImage(mask)){g.Clear(Color.Transparent);g.FillRectangle(Brushes.Black,8,6,12,10);}
    using(Graphics g=Graphics.FromImage(generated))g.Clear(Color.Red);
    Rectangle region=new Rectangle(40,30,40,30);
    using(ImageEditPatch patch=ImageEditPatch.Create(source,region,mask))
    using(Bitmap merged=patch.Merge(source,generated,region,mask)){
     Check(patch.Image.Size==patch.Mask.Size&&patch.Content.Width==104&&patch.Context.X==8,"Context and mask alignment");
     Check(patch.Mask.GetPixel(0,0).A==255,"Letterbox padding editable");
     for(int y=0;y<100;y++)for(int x=0;x<140;x++){
      bool edit=region.Contains(x,y)&&mask.GetPixel(x-region.X,y-region.Y).A==0;
      Check(merged.GetPixel(x,y).ToArgb()==(edit ? Color.Red.ToArgb() : source.GetPixel(x,y).ToArgb()),"Protected or outside pixels changed at "+x+","+y+" edit="+edit+" actual="+merged.GetPixel(x,y)+" expected="+(edit ? Color.Red : source.GetPixel(x,y)));
     }
    }
    using(Graphics g=Graphics.FromImage(mask))g.Clear(Color.Black);
    bool rejected=false;try{using(var patch=ImageEditPatch.Create(source,region,mask)){} }catch(InvalidOperationException){rejected=true;}Check(rejected,"Fully protected selection submitted");
   }
  }
  internal static void Protocol(){
   using(Bitmap image=new Bitmap(1024,1024))using(Bitmap mask=new Bitmap(1024,1024)){
    byte[] body=ImageEditClient.Multipart("unit-boundary","image-model","erase text",image,mask);string header=Encoding.UTF8.GetString(body);
    Check(header.Contains("name=\"image\"; filename=\"image.png\"")&&header.Contains("name=\"mask\"; filename=\"mask.png\"")&&header.Contains("1024x1024")&&header.EndsWith("--unit-boundary--\r\n"),"Multipart request");
    string response=new JavaScriptSerializer {MaxJsonLength=48*1024*1024}.Serialize(new Dictionary<string,object>{{"data",new object[]{new Dictionary<string,object>{{"b64_json",Convert.ToBase64String(ImageEditClient.Png(image))}}}}});
    using(Bitmap decoded=ImageEditClient.Decode(response))Check(decoded.Size==image.Size,"Image decoding");
    bool rejected=false;try{ImageEditClient.Decode("{\"data\":[{\"url\":\"https://example.com/image\"}]}");}catch(InvalidOperationException){rejected=true;}Check(rejected,"External image URL fetched");
   }
   Config config=new Config {ImageEditEndpoint="https://example.com/v1/images/edits",ImageEditModel="edit-model",ImageEditKey="unit-key-for-edit"};
   var lines=(List<string>)typeof(Config).GetMethod("BuildLines",Private).Invoke(config,null);Config restored=new Config();
   foreach(string line in lines)typeof(Config).GetMethod("ParseLine",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{restored,line});
   Check(!string.Join("\n",lines.ToArray()).Contains("unit-key-for-edit") && restored.ImageEditKey==config.ImageEditKey && restored.ImageEditEndpoint==config.ImageEditEndpoint&&restored.ImageEditModel==config.ImageEditModel,"Separate edit service configuration lost");
  }
  internal static void BrushAndText(){
   using(Bitmap source=new Bitmap(120,90))using(Bitmap mask=new Bitmap(70,45)){
    using(Graphics g=Graphics.FromImage(source))g.Clear(Color.White);using(Graphics g=Graphics.FromImage(mask)){g.Clear(Color.Transparent);g.FillRectangle(Brushes.Black,20,0,15,45);}
    Rectangle region=new Rectangle(20,20,70,45);
    ImageEditPatch.DrawReplacement(source,region,mask,"Long replacement text",18,Color.Black);int changed=0;
    for(int y=0;y<90;y++)for(int x=0;x<120;x++){
     bool allowed=region.Contains(x,y)&&mask.GetPixel(x-region.X,y-region.Y).A==0;
     if(!allowed)Check(source.GetPixel(x,y).ToArgb()==Color.White.ToArgb(),"Replacement text leaked into protected/outside pixels");
     else if(source.GetPixel(x,y).ToArgb()!=Color.White.ToArgb())changed++;
    }
    Check(changed>0,"Replacement text missing");
    using(EditorForm editor=new EditorForm(null,source,new Config())){
     typeof(EditorForm).GetMethod("SetAiRegion",Private).Invoke(editor,new object[]{region});
     typeof(EditorForm).GetField("maskProtect",Private).SetValue(editor,true);
     typeof(EditorForm).GetMethod("PaintMask",Private).Invoke(editor,new object[]{new PointF(30,30),new PointF(35,30)});
     Bitmap permissions=(Bitmap)typeof(EditorForm).GetField("imageEditMask",Private).GetValue(editor);Check(permissions.GetPixel(12,10).A==255,"Protect brush failed");
     typeof(EditorForm).GetField("maskProtect",Private).SetValue(editor,false);typeof(EditorForm).GetMethod("PaintMask",Private).Invoke(editor,new object[]{new PointF(30,30),new PointF(35,30)});
     Check(permissions.GetPixel(12,10).A==0,"Restore brush failed");
    }
   }
  }
  internal static void PreflightAndBrushSize(){
   Check(ImageEditPatch.ReplacementFits("OK",new Size(200,80),18),"Short text rejected");
   Check(!ImageEditPatch.ReplacementFits(new string('W',400),new Size(40,20),24),"Overflow allowed");
   Check(!ImageEditPatch.ReplacementFits("text",new Size(4,10),18),"Tiny region allowed");
   using(Bitmap source=new Bitmap(160,120))using(EditorForm editor=new EditorForm(null,source,new Config())){
    Rectangle region=new Rectangle(10,10,120,90);typeof(EditorForm).GetMethod("SetAiRegion",Private).Invoke(editor,new object[]{region});
    typeof(EditorForm).GetField("maskProtect",Private).SetValue(editor,true);
    typeof(EditorForm).GetField("maskBrushSize",Private).SetValue(editor,4);
    var paint=typeof(EditorForm).GetMethod("PaintMask",Private);paint.Invoke(editor,new object[]{new PointF(70,50),new PointF(70,50)});
    Bitmap mask=(Bitmap)typeof(EditorForm).GetField("imageEditMask",Private).GetValue(editor);
    Check(mask.GetPixel(60,40).A==255&&mask.GetPixel(70,40).A==0,"Small brush diameter incorrect");
    typeof(EditorForm).GetField("maskBrushSize",Private).SetValue(editor,32);paint.Invoke(editor,new object[]{new PointF(70,50),new PointF(70,50)});
    Check(mask.GetPixel(70,40).A==255&&mask.GetPixel(80,40).A==0,"Large brush diameter incorrect");
    typeof(EditorForm).GetMethod("ClearImageEditProtection",Private).Invoke(editor,null);
    byte[] pixels=ImageEditPatch.ReadPixels(mask);for(int i=3;i<pixels.Length;i+=4)Check(pixels[i]==0,"Clear mask failed");
   }
  }
  private static System.Collections.Generic.IEnumerable<System.Windows.Forms.Control> Descendants(System.Windows.Forms.Control parent){foreach(System.Windows.Forms.Control child in parent.Controls){yield return child;foreach(System.Windows.Forms.Control nested in Descendants(child))yield return nested;}}
  internal static void ExclusiveToolbar(){
   using(Bitmap source=new Bitmap(180,120))using(EditorForm editor=new EditorForm(null,source,new Config())){
    editor.Show();System.Windows.Forms.Application.DoEvents();
    System.Windows.Forms.Control first=null,last=null,ai=null,protect=null,restore=null;
    foreach(System.Windows.Forms.Control button in Descendants(editor)){
     if(button.GetType().Name=="ToolButton"){if(first==null)first=button;last=button;}
     if(button.Text==UiText.T("AI 框选"))ai=button;
     if(button.Text==UiText.T("保护画笔"))protect=button;
     if(button.Text==UiText.T("恢复修改"))restore=button;
    }
    Check(first!=null&&last!=null&&ai!=null&&protect!=null&&restore!=null,"Toolbar controls missing");
    int repaints=0;first.Invalidated+=delegate {repaints++;};
    var click=typeof(System.Windows.Forms.Control).GetMethod("OnClick",Private);
    click.Invoke(first,new object[]{EventArgs.Empty});click.Invoke(last,new object[]{EventArgs.Empty});
    var active=typeof(EditorForm).GetMethod("IsDrawingToolActive",Private);
    var kind=first.GetType().GetField("Kind");
    Check(!(bool)active.Invoke(editor,new object[]{kind.GetValue(first)})&&(bool)active.Invoke(editor,new object[]{kind.GetValue(last)}),"Drawing tools not exclusive");
    Check(repaints>0,"Previous toolbar child not invalidated");
    click.Invoke(ai,new object[]{EventArgs.Empty});Check(!(bool)active.Invoke(editor,new object[]{kind.GetValue(last)}),"Drawing active during AI selection");
    typeof(EditorForm).GetMethod("SetAiRegion",Private).Invoke(editor,new object[]{new Rectangle(10,10,80,60)});
    click.Invoke(protect,new object[]{EventArgs.Empty});
    var predicate=protect.GetType().GetField("ActiveState");
    Check(((Func<bool>)predicate.GetValue(protect))()&&!((Func<bool>)predicate.GetValue(restore))()&&!((Func<bool>)predicate.GetValue(ai))(),"Protect tool not exclusive");
    click.Invoke(restore,new object[]{EventArgs.Empty});Check(!((Func<bool>)predicate.GetValue(protect))()&&((Func<bool>)predicate.GetValue(restore))(),"Restore retained old highlight");
    click.Invoke(first,new object[]{EventArgs.Empty});Check((bool)active.Invoke(editor,new object[]{kind.GetValue(first)})&&!((Func<bool>)predicate.GetValue(restore))(),"Drawing did not exit mask tool");
   }
  }
  internal static void SharpArrows(){
   PointF[] ends={new PointF(150,90),new PointF(90,150),new PointF(145,145),new PointF(30,35),new PointF(95,93)};
   foreach(float width in new float[]{2,8})foreach(PointF end in ends){
    using(Bitmap source=new Bitmap(190,190)){
     using(Graphics g=Graphics.FromImage(source))g.Clear(Color.White);
     using(EditorForm editor=new EditorForm(null,source,new Config())){
      PointF start=new PointF(90,90);if(end.X==150)start=new PointF(30,90);
      typeof(EditorForm).GetMethod("AddShapeTracked",Private).Invoke(editor,new object[]{new Shape {Kind=Shape.K.Arrow,Color=Color.Red,Width=width,Rect=new RectangleF(start.X,start.Y,end.X-start.X,end.Y-start.Y)}});
      using(Bitmap rendered=editor.RenderResult()){
       double dx=end.X-start.X,dy=end.Y-start.Y,length=Math.Sqrt(dx*dx+dy*dy);int colored=0;
       for(int y=0;y<190;y++)for(int x=0;x<190;x++){
        Color pixel=rendered.GetPixel(x,y);if(pixel.G<240){colored++;double projection=((x-start.X)*dx+(y-start.Y)*dy)/length;Check(projection<=length+1.5,"Round cap protruded beyond arrow tip");}
       }
       Check(colored>5,"Arrow not rendered");
      }
     }
    }
   }
  }
  internal static void DirectArrowDragging(){
   using(Bitmap source=new Bitmap(200,160))using(EditorForm editor=new EditorForm(null,source,new Config())){
    editor.ClientSize=new Size(1000,700);editor.Show();System.Windows.Forms.Application.DoEvents();
    Shape arrow=new Shape {Kind=Shape.K.Arrow,Color=Color.Red,Width=8,Rect=new RectangleF(30,30,100,80)};
    typeof(EditorForm).GetMethod("AddShapeTracked",Private).Invoke(editor,new object[]{arrow});
    typeof(EditorForm).GetField("tool",Private).SetValue(editor,Shape.K.Arrow);
    var hit=typeof(EditorForm).GetMethod("HitShape",Private);
    Check(!(bool)hit.Invoke(editor,new object[]{arrow,new PointF(35,100),6f}),"Empty arrow bounds captured drawing");
    foreach(float zoom in new float[]{.5f,1f,2f}){
     typeof(EditorForm).GetField("zoom",Private).SetValue(editor,zoom);
     RectangleF screen=(RectangleF)typeof(EditorForm).GetMethod("ImageRectToScreen",Private).Invoke(editor,new object[]{new RectangleF(80,70,20,15)});
     Point a=Point.Round(screen.Location),b=Point.Round(new PointF(screen.Right,screen.Bottom));
     var down=new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,a.X,a.Y,0);
     var up=new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,b.X,b.Y,0);
     typeof(EditorForm).GetMethod("OnMouseDown",Private).Invoke(editor,new object[]{down});
     Check((bool)typeof(EditorForm).GetField("selectionDragging",Private).GetValue(editor),"Arrow tool did not begin direct drag");
     typeof(EditorForm).GetMethod("OnMouseMove",Private).Invoke(editor,new object[]{up});typeof(EditorForm).GetMethod("OnMouseUp",Private).Invoke(editor,new object[]{up});
     var shapes=(List<Shape>)typeof(EditorForm).GetField("shapes",Private).GetValue(editor);
     Check(shapes.Count==1&&Math.Abs(shapes[0].Rect.X-50)<=1&&Math.Abs(shapes[0].Rect.Y-45)<=1,"Drag created arrow or moved incorrect coordinates");
     typeof(EditorForm).GetMethod("DoUndo",Private).Invoke(editor,null);Check(shapes[0].Rect.Location==new PointF(30,30),"Direct move undo failed");
     typeof(EditorForm).GetMethod("DoRedo",Private).Invoke(editor,null);Check(Math.Abs(shapes[0].Rect.X-50)<=1,"Direct move redo failed");
     typeof(EditorForm).GetMethod("DoUndo",Private).Invoke(editor,null);arrow=shapes[0];
    }
    Check(!(bool)typeof(EditorForm).GetMethod("TryBeginDirectDrag",Private).Invoke(editor,new object[]{new PointF(180,130)}),"Blank canvas starts move");
   }
  }
  internal static void History(){
   using(Bitmap source=new Bitmap(100,80))using(Bitmap result=new Bitmap(100,80)){
    using(Graphics g=Graphics.FromImage(source))g.Clear(Color.Blue);using(Graphics g=Graphics.FromImage(result))g.Clear(Color.Red);
    using(EditorForm editor=new EditorForm(null,source,new Config())){
     typeof(EditorForm).GetMethod("AddShapeTracked",Private).Invoke(editor,new object[]{new Shape {Kind=Shape.K.Redact,Color=Color.Black,Rect=new RectangleF(5,5,10,10)}});
     using(Bitmap before=editor.RenderResult()){
      typeof(EditorForm).GetMethod("ApplyGeneratedImage",Private).Invoke(editor,new object[]{result});
      using(Bitmap current=editor.RenderResult())Check(current.GetPixel(30,30).R>200,"Generated result not applied");
      typeof(EditorForm).GetMethod("DoUndo",Private).Invoke(editor,null);
      using(Bitmap current=editor.RenderResult()){Check(current.GetPixel(30,30).ToArgb()==before.GetPixel(30,30).ToArgb()&&current.GetPixel(7,7).ToArgb()==Color.Black.ToArgb(),"Generated undo lost source or annotations");}
      typeof(EditorForm).GetMethod("DoRedo",Private).Invoke(editor,null);using(Bitmap current=editor.RenderResult())Check(current.GetPixel(30,30).R>200,"Generated redo");
     }
    }
   }
  }
 }
}
