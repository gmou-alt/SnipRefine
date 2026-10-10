using System;using System.Drawing;using System.Reflection;using System.Windows.Forms;using System.IO;
namespace SnapGrab {internal static class TrialExperienceTests {
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static object Field(object o,string name){return o.GetType().GetField(name,Private).GetValue(o);}static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,Private).Invoke(o,args);}static void Check(bool value,string message){if(!value)throw new Exception(message);}
 internal static void Properties(){using(Bitmap source=new Bitmap(120,80))using(EditorForm editor=new EditorForm(null,source,new Config())){
  editor.ClientSize=new Size(700,560);editor.Show();Application.DoEvents();Panel properties=(Panel)Field(editor,"workspaceProperties");
  foreach(Shape.K kind in new[]{Shape.K.Eraser,Shape.K.Arrow,Shape.K.Mosaic,Shape.K.Select}){
   typeof(EditorForm).GetField("tool",Private).SetValue(editor,kind);Call(editor,"LayoutChrome");
   bool color=false,width=false;foreach(Control button in properties.Controls){if(button.GetType().Name=="ColorSwatch"&&button.Visible)color=true;if(button.GetType().Name=="ZoomButton"&&button.Visible)width=true;if(button.Visible)Check(button.Bottom<=properties.ClientSize.Height&&button.Right<=properties.ClientSize.Width,"Tool parameter outside strip");}
   Check(color==(kind==Shape.K.Arrow)&&width==(kind==Shape.K.Arrow),"Irrelevant color/line width shown");Check(((Control)Field(editor,"mosaicProperty")).Visible==(kind==Shape.K.Mosaic),"Mosaic block parameter visibility incorrect");
   Check(((Control)Field(editor,"effectSizeButton")).Visible==(kind==Shape.K.Eraser),"Brush diameter visibility incorrect");
  }
 }}
 internal static void SaveState(){string directory=Path.Combine(Path.GetTempPath(),"sniprefine-trial-"+Guid.NewGuid().ToString("N"));try {using(Bitmap source=new Bitmap(120,80))using(EditorForm editor=new EditorForm(null,source,new Config {SaveDir=directory})){
  Check((string)Call(editor,"ImageSaveState")=="未保存","Initial image incorrectly saved");Call(editor,"SaveEditedImage",false);Check((string)Call(editor,"ImageSaveState")=="已保存"&&File.Exists((string)Field(editor,"lastExplicitSave")),"Save receipt does not correspond to file");
  Call(editor,"AddShapeTracked",new Shape {Kind=Shape.K.Rect,Rect=new RectangleF(10,10,30,20),Color=Color.Red,Width=3});Check((string)Call(editor,"ImageSaveState")=="有未保存修改","Editing did not mark dirty");Call(editor,"SaveEditedImage",false);Check((string)Call(editor,"ImageSaveState")=="已保存","Saving did not clear dirty");
  typeof(EditorForm).GetField("cfg",Private).SetValue(editor,new Config {SaveDir=Path.Combine((string)Field(editor,"lastExplicitSave"),"invalid")});Call(editor,"AddShapeTracked",new Shape {Kind=Shape.K.Rect,Rect=new RectangleF(40,10,30,20),Color=Color.Blue,Width=3});Call(editor,"SaveEditedImage",false);Check((string)Call(editor,"ImageSaveState")=="有未保存修改","Failed save incorrectly clears dirty");
 }}finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}}
}}
