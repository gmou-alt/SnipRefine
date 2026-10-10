using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace SnapGrab {
 internal static class WorkspaceLayoutTests {
  private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  private static object Field(object owner,string name){return owner.GetType().GetField(name,Private).GetValue(owner);}
  private static object Call(object owner,string name,params object[] args){return owner.GetType().GetMethod(name,Private).Invoke(owner,args);}
  private static void Check(bool value,string message){if(!value)throw new Exception(message);}
  internal static void Editor(){
   using(Bitmap source=new Bitmap(180,120))using(EditorForm editor=new EditorForm(null,source,new Config())){
    editor.ClientSize=new Size(1200,800);editor.Show();Application.DoEvents();
    Check((int)Call(editor,"WindowHitTest",new Point(0,0))==13,"Window top-left resize missing");
    Check((int)Call(editor,"WindowHitTest",new Point(1199,799))==17,"Window bottom-right resize missing");
    Check((int)Call(editor,"WindowHitTest",new Point(500,15))==2,"Window title cannot drag");
    Check((int)Call(editor,"WindowHitTest",new Point(500,300))==1,"Canvas intercepted by resizing");
    Control penButton=null;foreach(Control button in (System.Collections.IEnumerable)Field(editor,"toolButtons"))if(button.Text==UiText.T("画笔"))penButton=button;
    Check(penButton!=null,"Pen button missing");
    Color activePixel,inactivePixel;
    using(Bitmap drawn=new Bitmap(penButton.Width,penButton.Height)){penButton.DrawToBitmap(drawn,penButton.ClientRectangle);activePixel=drawn.GetPixel(85,15);}
    typeof(EditorForm).GetField("tool",Private).SetValue(editor,Shape.K.Arrow);
    using(Bitmap drawn=new Bitmap(penButton.Width,penButton.Height)){penButton.DrawToBitmap(drawn,penButton.ClientRectangle);inactivePixel=drawn.GetPixel(85,15);}
    Check(activePixel.ToArgb()!=inactivePixel.ToArgb(),"Tool selection highlight missing after reparenting");
    typeof(EditorForm).GetField("tool",Private).SetValue(editor,Shape.K.Pen);
    Control swatch=null;foreach(Control button in (System.Collections.IEnumerable)Field(editor,"swatches")){swatch=button;break;}
    Check(swatch!=null,"Color swatch missing");Color swatchColor=(Color)swatch.GetType().GetField("Value").GetValue(swatch);
    Color originalColor=(Color)Field(editor,"color");
    using(Bitmap selected=new Bitmap(swatch.Width,swatch.Height))using(Bitmap unselected=new Bitmap(swatch.Width,swatch.Height)){
     typeof(EditorForm).GetField("color",Private).SetValue(editor,swatchColor);swatch.DrawToBitmap(selected,swatch.ClientRectangle);
     typeof(EditorForm).GetField("color",Private).SetValue(editor,Color.Lime);swatch.DrawToBitmap(unselected,swatch.ClientRectangle);
     bool changed=false;for(int row=0;row<swatch.Height;row++)for(int column=0;column<swatch.Width;column++)if(selected.GetPixel(column,row)!=unselected.GetPixel(column,row))changed=true;
     Check(changed,"Color selection outline missing after reparenting");
    }
    typeof(EditorForm).GetField("color",Private).SetValue(editor,originalColor);
    foreach(Size size in new[]{new Size(700,560),new Size(1200,800),new Size(1920,1080)}){
     editor.ClientSize=size;Application.DoEvents();Rectangle view=(Rectangle)typeof(EditorForm).GetProperty("ViewRect",Private).GetValue(editor,null);
     Panel tools=(Panel)Field(editor,"workspaceTools"),ai=(Panel)Field(editor,"aiBody"),commands=(Panel)Field(editor,"workspaceAiCommands");
     foreach(Control button in tools.Controls)if(button.Visible){Check(button.Region!=null&&!button.Region.IsVisible(0,0)&&button.Region.IsVisible(button.Width/2,button.Height/2),"Rounded tool exposes square corner");}
     Control inputFrame=(Control)Field(editor,"aiInputFrame");Check(inputFrame.Region!=null&&!inputFrame.Region.IsVisible(0,0),"Input frame exposes square corner");
     Check(tools.Right<=view.Left&&view.Right<=ai.Left&&view.Height>150&&view.Width>150,"Workspace overlaps or loses canvas");
     Check(commands.Parent==ai&&commands.Bottom<=((Control)Field(editor,"aiLogBox")).Top,"AI commands overlap result log");
     Control log=(Control)Field(editor,"aiLogBox"),status=(Control)Field(editor,"aiStatusLabel"),input=(Control)Field(editor,"aiInputFrame");
     Check(log.Bottom<=status.Top&&status.Bottom<=input.Top,"AI result status or input overlaps");
     foreach(Control button in (System.Collections.IEnumerable)Field(editor,"barButtons"))Check(button.Bottom<=view.Top&&button.Right<=editor.ClientSize.Width,"File controls overlap canvas");
     foreach(Control button in (System.Collections.IEnumerable)Field(editor,"toolButtons"))Check(button.Parent==tools,"Drawing tools did not move into palette");
     RectangleF mapped=(RectangleF)Call(editor,"ImageRectToScreen",new RectangleF(35,45,10,10));
     PointF roundTrip=(PointF)Call(editor,"ScreenToImage",Point.Round(new PointF(mapped.X,mapped.Y)));
     Check(Math.Abs(roundTrip.X-35)<2&&Math.Abs(roundTrip.Y-45)<2,"Canvas offset changes selected image coordinates: "+roundTrip+" zoom="+Field(editor,"zoom")+" mapped="+mapped);
     commands.AutoScrollPosition=new Point(0,100);Call(editor,"LayoutChrome");
     foreach(Control button in commands.Controls)if(button.Text==UiText.T("AI 框选"))Check(button.Top-commands.AutoScrollPosition.Y==30,"AI first command drifts on relayout");
     foreach(int requested in new[]{280,440,900}){
      typeof(EditorForm).GetField("requestedAiWidth",Private).SetValue(editor,requested);Call(editor,"LayoutChrome");
      Rectangle resizedView=(Rectangle)typeof(EditorForm).GetProperty("ViewRect",Private).GetValue(editor,null);
      Control divider=(Control)Field(editor,"workspaceDivider");
      Check(divider.Width==7&&resizedView.Right<=divider.Left&&divider.Right==ai.Left&&resizedView.Width>=220,"Resizable workspace bounds invalid");
      mapped=(RectangleF)Call(editor,"ImageRectToScreen",new RectangleF(35,45,10,10));roundTrip=(PointF)Call(editor,"ScreenToImage",Point.Round(new PointF(mapped.X,mapped.Y)));
      Check(Math.Abs(roundTrip.X-35)<2&&Math.Abs(roundTrip.Y-45)<2,"Resize changes image coordinate mapping");
     }
     typeof(EditorForm).GetField("requestedAiWidth",Private).SetValue(editor,0);Call(editor,"LayoutChrome");
     Call(editor,"CloseAiPanel");Application.DoEvents();Rectangle expanded=(Rectangle)typeof(EditorForm).GetProperty("ViewRect",Private).GetValue(editor,null);
     Check(expanded.Left==view.Left&&expanded.Width>view.Width,"AI collapse moves palette or does not expand canvas");
     Control toggle=(Control)Field(editor,"aiCloseButton");Check(toggle.Parent==editor&&toggle.Visible&&toggle.Right<=editor.ClientSize.Width,"AI toggle disappears when collapsed");
     Call(toggle,"OnClick",EventArgs.Empty);Check((bool)Field(editor,"aiOpen"),"Same button cannot reopen AI");
     Call(toggle,"OnClick",EventArgs.Empty);Check(!(bool)Field(editor,"aiOpen"),"Same button cannot collapse AI");Call(editor,"OpenAiPanel");
    }
   }
  }
  internal static void MainEntry(){
   TrayApp app=(TrayApp)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrayApp));typeof(TrayApp).GetField("cfg",Private).SetValue(app,new Config());typeof(TrayApp).GetField("appIcon",Private).SetValue(app,SystemIcons.Application);
   typeof(TrayApp).GetField("recentShots",Private).SetValue(app,new List<RecentShot>());
   app.ShowMainForm();Application.DoEvents();MainForm main=(MainForm)Field(app,"mainForm");
   try {Check(main!=null&&main.Visible,"Main settings window is not shown directly");app.ShowMainForm();Check(ReferenceEquals(main,Field(app,"mainForm")),"Reopening creates a separate settings window");Check(main.Controls.Count>0,"Main window content missing");
    SidebarMotion motion=(SidebarMotion)Field(main,"panelMotion");Control chat=(Control)Field(main,"chatPanel");var footer=(List<DshButton>)Field(main,"settingsFooterButtons");
    foreach(Size size in new[]{new Size(860,660),new Size(1266,910),new Size(1920,1080)})foreach(double progress in new[]{.25,.6,1.0,0.0}){
     main.ClientSize=size;motion.Jump(progress);Call(main,"LayoutChatPanel");
     foreach(DshButton button in footer)if(progress>0&&button.Bounds.IntersectsWith(chat.Bounds))Check(chat.Parent.Controls.GetChildIndex(chat)<button.Parent.Controls.GetChildIndex(button),"Footer overlays AI drawer");
     if(progress==0)foreach(DshButton button in footer)Check(button.Right<=main.ClientSize.Width,"Footer not restored after drawer close");
    }}
   finally {main.Dispose();}
  }
 }
}
