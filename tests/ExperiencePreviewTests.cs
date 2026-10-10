using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace SnapGrab {
 internal static class ExperiencePreviewTests {
  private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  internal static void Run(){
   using(Bitmap source=new Bitmap(300,200))using(Bitmap result=new Bitmap(300,200))using(EditorForm editor=new EditorForm(null,source,new Config())){
    typeof(EditorForm).GetField("aiRegion",Private).SetValue(editor,new Rectangle(50,50,80,40));
    typeof(EditorForm).GetField("aiBefore",Private).SetValue(editor,(Bitmap)source.Clone());
    bool examined=false;Exception failure=null;
    using(Timer timer=new Timer {Interval=30}){
     timer.Tick+=delegate {Form dialog=null;foreach(Form f in Application.OpenForms)if(f.Text=="预览图片编辑结果"){dialog=f;break;}if(dialog==null)return;timer.Stop();
      try {ThemedDialog d=(ThemedDialog)dialog;PictureBox view=null;DshButton compare=null,scope=null,apply=null,cancel=null;
       foreach(Control c in d.ContentArea.Controls){if(c is PictureBox)view=(PictureBox)c;if(c is DshButton){if(c.Text=="当前：修改后")compare=(DshButton)c;if(c.Text=="当前：选区放大")scope=(DshButton)c;if(c.Text=="应用修改")apply=(DshButton)c;if(c.Text=="保留原图")cancel=(DshButton)c;}}
       if(view==null||view.Image.Size!=new Size(80,40)||compare==null||scope==null||apply==null||cancel==null)throw new Exception("Result preview missing magnified selection or explicit actions");
       typeof(Control).GetMethod("OnClick",Private).Invoke(compare,new object[]{EventArgs.Empty});if(compare.Text!="当前：修改前"||view.Image.Size!=new Size(80,40))throw new Exception("Before comparison wrong");
       typeof(Control).GetMethod("OnClick",Private).Invoke(scope,new object[]{EventArgs.Empty});if(view.Image.Size!=source.Size)throw new Exception("Full-image comparison wrong");examined=true;
      }catch(Exception ex){failure=ex;}finally {dialog.DialogResult=DialogResult.Cancel;dialog.Close();}
     };timer.Start();bool accepted=(bool)typeof(EditorForm).GetMethod("ConfirmImageResult",Private).Invoke(editor,new object[]{result,"预览图片编辑结果"});if(accepted)throw new Exception("Rejected preview applied");
    }
    if(failure!=null)throw failure;if(!examined)throw new Exception("Preview was not examined");
   }
  }
 }
}
