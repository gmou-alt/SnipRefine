using System;
using System.Drawing;
using System.Reflection;
namespace SnapGrab {
 internal static class ReleaseReadinessTests {
  private static void Check(bool ok,string name) { if(!ok) throw new Exception(name); }
  internal static void Geometry() {
   Rectangle[] displays={new Rectangle(-1920,0,1920,1080),new Rectangle(0,0,2560,1440),new Rectangle(0,-1920,1080,1920)};
   Rectangle desktop=DesktopGeometry.Bounds(displays);
   Check(desktop==new Rectangle(-1920,-1920,4480,3360),"Virtual desktop union");
   foreach(Rectangle screen in displays) {
    Rectangle local=DesktopGeometry.ToSnapshot(screen,desktop);
    Check(DesktopGeometry.ToDesktop(local,desktop)==screen,"Physical coordinate roundtrip");
   }
   Check(DesktopGeometry.ToSnapshot(new Rectangle(-2000,-2000,100,100),desktop)==new Rectangle(0,0,20,20),"Edge clipping");
   Check(DesktopGeometry.ToSnapshot(new Rectangle(9000,9000,20,20),desktop)==Rectangle.Empty,"Outside selection");
   Check(DesktopGeometry.Pixels(8,96)==8 && DesktopGeometry.Pixels(8,120)==10 && DesktopGeometry.Pixels(8,144)==12 && DesktopGeometry.Pixels(8,192)==16,"DPI target size");
   bool rejected=false;try{DesktopGeometry.Bounds(new Rectangle[0]);}catch(ArgumentException){rejected=true;}
   Check(rejected,"Invalid display list");
  }
  internal static void Metadata() {
   var assembly=typeof(Program).Assembly;
   var product=(AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly,typeof(AssemblyProductAttribute));
   var file=(AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(assembly,typeof(AssemblyFileVersionAttribute));
   Check(product!=null && product.Product=="SnipRefine","Product metadata");
   Check(file!=null && file.Version==Program.VersionText+".0","File version metadata");
  }
  internal static void Language() {
   string previous=UiText.Language;
   try {
    var config=new Config { UiLanguage="en" };
    var lines=(System.Collections.Generic.List<string>)typeof(Config).GetMethod("BuildLines",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(config,null);
    var restored=new Config();
    foreach(string line in lines) typeof(Config).GetMethod("ParseLine",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[] { restored,line });
    Check(restored.UiLanguage=="en","Language config roundtrip");
    UiText.SetLanguage("en");
    Check(UiText.T("保存")=="Save","Save translation");
    Check(UiText.T("用户自己输入的文本")=="用户自己输入的文本","Unmapped user content preserved");
    var spanishConfig=new Config {UiLanguage="es",ImageEditProtocol="gemini"};
    var spanishLines=(System.Collections.Generic.List<string>)typeof(Config).GetMethod("BuildLines",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(spanishConfig,null);
    var spanishRestored=new Config();foreach(string line in spanishLines)typeof(Config).GetMethod("ParseLine",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{spanishRestored,line});
    Check(spanishRestored.UiLanguage=="es"&&spanishRestored.ImageEditProtocol=="gemini","Spanish or image protocol setting lost");
    UiText.SetLanguage("es");Check(UiText.T("保存")=="Guardar"&&UiText.T("箭头")=="Flecha","Spanish core translations");
    Check(UiText.T("用户自己输入的文本")=="用户自己输入的文本","User text translated");
    using(var label=new DshLabel("保存")){Check(label.Text=="Guardar","Dynamic label bypasses localization");}
    foreach(string language in new[]{"en","es"}){
     UiText.SetLanguage(language);using(var source=new System.Drawing.Bitmap(200,120))using(var editor=new EditorForm(null,source,new Config())){
      editor.ClientSize=new System.Drawing.Size(1000,700);editor.Show();System.Windows.Forms.Application.DoEvents();
      var privateFlags=BindingFlags.Instance|BindingFlags.NonPublic;
      foreach(System.Windows.Forms.Control button in (System.Collections.IEnumerable)typeof(EditorForm).GetField("toolButtons",privateFlags).GetValue(editor))Check(!System.Text.RegularExpressions.Regex.IsMatch(button.Text,"[\u4e00-\u9fff]"),"Drawing tool untranslated");
      var commands=(System.Windows.Forms.Panel)typeof(EditorForm).GetField("workspaceAiCommands",privateFlags).GetValue(editor);
      foreach(System.Windows.Forms.Control control in commands.Controls)if(control is DshLabel)Check(!System.Text.RegularExpressions.Regex.IsMatch(control.Text,"[\u4e00-\u9fff]"),"AI group title untranslated");
      bool ocrCopy=false;foreach(System.Windows.Forms.Control button in commands.Controls)if(button.Text==UiText.T("复制OCR"))ocrCopy=true;Check(ocrCopy,"Localized OCR copy button escaped AI group");
      foreach(string field in new[]{"aiCompareButton","aiAcceptButton","aiUndoButton","aiCancelButton"}){var button=(System.Windows.Forms.Control)typeof(EditorForm).GetField(field,privateFlags).GetValue(editor);Check(!System.Text.RegularExpressions.Regex.IsMatch(button.Text,"[\u4e00-\u9fff]"),"AI review control untranslated");}
      var aiTitle=(System.Windows.Forms.Label)typeof(EditorForm).GetField("aiTitle",privateFlags).GetValue(editor);Check(!System.Text.RegularExpressions.Regex.IsMatch(aiTitle.Text,"[\u4e00-\u9fff]"),"Upload scope untranslated");
     }
    }
    UiText.SetLanguage("unknown");Check(UiText.Language=="zh-CN","Language fallback");
    UiText.SetLanguage("en");Check(UserGuide.Search("privacy").Count>0,"English offline help");
   } finally {UiText.SetLanguage(previous);}
  }
 }
}
