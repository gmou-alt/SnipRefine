using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace SnapGrab {
 internal static class SettingsFeedbackTests {
  private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  internal static void MainPageButtons(){
   TrayApp app=(TrayApp)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrayApp));Config config=new Config();
   typeof(TrayApp).GetField("cfg",Private).SetValue(app,config);typeof(TrayApp).GetField("appIcon",Private).SetValue(app,SystemIcons.Application);
   using(MainForm form=new MainForm(app)){
    var buttons=(DshButton[])typeof(MainForm).GetField("pageSaveButtons",Private).GetValue(form);
    var sections=(SavedSettingsSection[])typeof(MainForm).GetField("savedSections",Private).GetValue(form);
    Check(buttons.Length==8,"Settings pages missing save buttons");foreach(DshButton button in buttons)Check(button!=null&&!button.Visible&&button.Text=="已保存"&&button.Parent.Controls.GetChildIndex(button)==0,"Page save button absent");
    ((DshSelect)typeof(MainForm).GetField("appearanceLanguage",Private).GetValue(form)).SelectedIndex=1;
    ((DshToggle)typeof(MainForm).GetField("recentRemember",Private).GetValue(form)).Checked=true;
    Check(config.UiLanguage=="zh-CN"&&!config.KeepSavedHistory,"Settings saved implicitly on changing controls");
    typeof(MainForm).GetMethod("ShowSavedSettings",Private).Invoke(form,new object[]{0});
    Check(!sections[0].Open&&sections[0].Receipt.Text.Contains("已保存"),"Saved page did not summarize");
    var footer=(System.Collections.Generic.List<DshButton>)typeof(MainForm).GetField("settingsFooterButtons",Private).GetValue(form);
    Check(!footer[0].Enabled&&footer[0].Text=="已保存","Footer reports stale save state");
    typeof(MainForm).GetMethod("SwitchPage",Private).Invoke(form,new object[]{1});Check(!footer[0].Enabled&&footer[0].Text=="已保存","Switching page keeps stale receipt");
    typeof(MainForm).GetMethod("SwitchPage",Private).Invoke(form,new object[]{0});sections[0].SetOpen(true);Check(!footer[0].Enabled,"Unchanged page enables redundant save");for(int i=1;i<sections.Length;i++)Check(sections[i].Open,"Saving one page collapsed another");
   }
  }
  internal static void InvalidDialogSave(){
   Config config=new Config {ImageEditEndpoint="invalid-address",ImageEditKey="unit-test-key"};bool examined=false;Exception failure=null;
   using(Timer timer=new Timer {Interval=30}){
    timer.Tick+=delegate {
     Form target=null;foreach(Form form in Application.OpenForms)if(form.Text=="图片编辑服务（独立于看图模型）"){target=form;break;}if(target==null)return;
     timer.Stop();try {
      SavedSettingsSection section=FindSection(target);DshButton save=null;foreach(Control control in section.Body.Controls)if(control is DshButton && control.Text==UiText.T("保存"))save=(DshButton)control;
      Check(save!=null,"Dialog save button missing");typeof(Control).GetMethod("OnClick",Private).Invoke(save,new object[]{EventArgs.Empty});
      Check(section.Open&&config.ImageEditEndpoint=="invalid-address"&&section.Receipt.Text=="","Invalid save closed or reported success");examined=true;
     }catch(Exception ex){failure=ex;}finally {target.Close();}
    };
    timer.Start();ImageEditSettings.Show(null,config);
   }
   if(failure!=null)throw failure;Check(examined,"Dialog not examined");
  }
  private static SavedSettingsSection FindSection(Control parent){foreach(Control child in parent.Controls){SavedSettingsSection section=child as SavedSettingsSection;if(section!=null)return section;section=FindSection(child);if(section!=null)return section;}return null;}
  internal static void SmallWindowContent(){
   TrayApp app=(TrayApp)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrayApp));typeof(TrayApp).GetField("cfg",Private).SetValue(app,new Config());typeof(TrayApp).GetField("appIcon",Private).SetValue(app,SystemIcons.Application);
   using(MainForm form=new MainForm(app)){
    form.Show();Application.DoEvents();
    foreach(Size size in new Size[]{new Size(860,910),new Size(740,640),new Size(1100,820),new Size(1920,1080),new Size(860,910)}){
     form.ClientSize=size;Application.DoEvents();typeof(MainForm).GetMethod("LayoutChatPanel",Private).Invoke(form,null);
     var sections=(SavedSettingsSection[])typeof(MainForm).GetField("savedSections",Private).GetValue(form);
     var buttons=(DshButton[])typeof(MainForm).GetField("pageSaveButtons",Private).GetValue(form);
     var footer=(System.Collections.Generic.List<DshButton>)typeof(MainForm).GetField("settingsFooterButtons",Private).GetValue(form);
     Check(footer[1].Right<footer[2].Left&&footer[2].Right<footer[0].Left,"Footer actions overlap or wrong order");
     Check(footer[0].Right==208+buttons[0].Right,"Footer save does not align with page save");
     Check(!((Panel)sections[0].Parent).HorizontalScroll.Visible||sections[0].Parent.ClientSize.Width-SystemInformation.VerticalScrollBarWidth<652,"Unneeded horizontal scroll in wide window "+size+" page="+sections[0].Parent.ClientSize+" section="+sections[0].Bounds+" scroll="+((Panel)sections[0].Parent).AutoScrollMinSize);
     for(int i=0;i<sections.Length;i++){
      Check(sections[i].Width>=652&&sections[i].Body.Width==sections[i].Width,"Small window clipped fold body");
      foreach(Control control in sections[i].Body.Controls)if(control is DshGlassCard)Check(control.Right<=sections[i].Body.Width,"Card clipped by body");
      if(buttons[i].Parent.Visible)Check(buttons[i].Right<=buttons[i].Parent.ClientSize.Width-52,"Save button covered by sidebar toggle");
     }
    }
   }
  }
  internal static void PageIsolation(){
   TrayApp app=(TrayApp)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrayApp));Config runtime=new Config();typeof(TrayApp).GetField("cfg",Private).SetValue(app,runtime);typeof(TrayApp).GetField("appIcon",Private).SetValue(app,SystemIcons.Application);
   using(MainForm form=new MainForm(app)){
    ((DshSelect)typeof(MainForm).GetField("appearanceLanguage",Private).GetValue(form)).SelectedIndex=1;
    ((DshToggle)typeof(MainForm).GetField("recentRemember",Private).GetValue(form)).Checked=true;
    Config baseline=new Config {SaveDir="baseline-directory",AiTimeoutSeconds=77,ImageEditKey="unit-key",ImageEditModel="image-unit-model"};
    var build=typeof(MainForm).GetMethod("BuildPageConfig",Private);
    Config appearance=(Config)build.Invoke(form,new object[]{4,baseline});
    Check(appearance.UiLanguage=="zh-CN"&&!appearance.KeepSavedHistory&&appearance.SaveDir==baseline.SaveDir&&appearance.AiTimeoutSeconds==77&&appearance.ImageEditKey=="unit-key","Appearance save captured unrelated settings");
    Config language=(Config)build.Invoke(form,new object[]{7,baseline});Check(language.UiLanguage=="en"&&language.UiTheme==baseline.UiTheme&&!language.KeepSavedHistory&&language.ImageEditKey==baseline.ImageEditKey,"Language save captured other drafts");
    Config history=(Config)build.Invoke(form,new object[]{6,baseline});Check(history.KeepSavedHistory&&history.UiLanguage=="zh-CN"&&history.ImageEditModel==baseline.ImageEditModel,"History save captured appearance or service settings");
    Config paths=(Config)build.Invoke(form,new object[]{1,baseline});Check(!paths.KeepSavedHistory&&paths.UiLanguage=="zh-CN"&&paths.AiTimeoutSeconds==77,"Path save captured other drafts");
   }
  }
  internal static void SaveValidationAndMessages(){
   string root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"SnipRefine-save-validation-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(root);
   try {string file=System.IO.Path.Combine(root,"not-a-directory");System.IO.File.WriteAllText(file,"unit");
    var validate=typeof(MainForm).GetMethod("ValidateSaveDirectory",BindingFlags.NonPublic|BindingFlags.Static);
    Check(validate.Invoke(null,new object[]{new Config {SaveDir=file}})!=null,"File path accepted as save directory");
    Check(validate.Invoke(null,new object[]{new Config {SaveDir=System.IO.Path.Combine(root,"valid")}})==null,"Valid save directory rejected");
   }finally {System.IO.Directory.Delete(root,true);}
   TrayApp app=(TrayApp)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrayApp));Config cfg=new Config {UiLanguage=UiText.Language=="en" ? "en" : "zh-CN"};typeof(TrayApp).GetField("cfg",Private).SetValue(app,cfg);typeof(TrayApp).GetField("appIcon",Private).SetValue(app,SystemIcons.Application);
   using(MainForm form=new MainForm(app)){var message=typeof(MainForm).GetMethod("SavedPageMessage",Private);
    Check(!((string)message.Invoke(form,new object[]{4})).Contains("重启后生效"),"Theme-only save claims language restart");cfg.UiLanguage=cfg.UiLanguage=="en" ? "zh-CN" : "en";
    Check(((string)message.Invoke(form,new object[]{7})).Contains("重启后生效"),"Changed language lacks restart hint");
    Check(((string)message.Invoke(form,new object[]{3})).Contains("单独测试"),"Saving AI config claims service availability");
   }
  }
  internal static void DraftState(){
   TrayApp app=(TrayApp)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrayApp));Config cfg=new Config();typeof(TrayApp).GetField("cfg",Private).SetValue(app,cfg);typeof(TrayApp).GetField("appIcon",Private).SetValue(app,SystemIcons.Application);
   using(MainForm form=new MainForm(app)){
    var refresh=typeof(MainForm).GetMethod("RefreshSettingsDrafts",Private);var dirty=(bool[])typeof(MainForm).GetField("settingsDirty",Private).GetValue(form);
    DshToggle remember=(DshToggle)typeof(MainForm).GetField("recentRemember",Private).GetValue(form);
    remember.Checked=true;refresh.Invoke(form,null);Check(dirty[6]&&!dirty[4]&&!cfg.KeepSavedHistory,"Draft state not scoped or persisted implicitly");
    remember.Checked=false;refresh.Invoke(form,null);Check(!dirty[6],"Reverting original value remains dirty");
    ((DshSelect)typeof(MainForm).GetField("appearanceLanguage",Private).GetValue(form)).SelectedIndex=1;refresh.Invoke(form,null);Check(dirty[7]&&!dirty[4],"Language draft absent");
    typeof(MainForm).GetMethod("SwitchPage",Private).Invoke(form,new object[]{7});var footer=(System.Collections.Generic.List<DshButton>)typeof(MainForm).GetField("settingsFooterButtons",Private).GetValue(form);Check(footer[0].Enabled&&footer[0].Text.Contains("未保存"),"Active dirty page footer stale");
    typeof(MainForm).GetMethod("MarkSettingsPageSaved",Private).Invoke(form,new object[]{6});Check(dirty[7]&&!dirty[6],"Saving another page clears pending changes");
   }
  }
  internal static void FoldAndRestore(){
   using(Form form=new Form()){
    Panel page=new Panel {Dock=DockStyle.Fill,AutoScroll=true};form.Controls.Add(page);form.ClientSize=new Size(620,550);
    TextBox input=new TextBox {Text="unsaved draft"};input.SetBounds(16,140,400,28);page.Controls.Add(input);
    using(SavedSettingsSection fold=SavedSettingsSection.Wrap(page,100)){
     Check(input.Parent==fold.Body&&input.Top==40&&fold.Open,"Settings controls not retained");
     fold.Saved("Saved successfully");Check(!fold.Open&&fold.Height==136&&fold.Receipt.Text.Contains("已保存"),"Saved summary absent");
     fold.SetOpen(true);Check(fold.Open&&input.Text=="unsaved draft","Reopening lost input");
     form.Show();Application.DoEvents();
     fold.Saved("Saved successfully");var motion=(SidebarMotion)typeof(SavedSettingsSection).GetField("motion",Private).GetValue(fold);
     for(int i=0;i<50;i++){motion.Advance(.015);typeof(SavedSettingsSection).GetMethod("ApplyFrame",Private).Invoke(fold,null);}
     Check(fold.Height==136&&!fold.Body.Visible&&fold.Summary.Visible,"Animated close did not settle");
     fold.SetOpen(true);motion.Advance(.04);typeof(SavedSettingsSection).GetMethod("ApplyFrame",Private).Invoke(fold,null);
     fold.Saved("Saved again");fold.SetOpen(true);
     for(int i=0;i<50;i++){motion.Advance(.015);typeof(SavedSettingsSection).GetMethod("ApplyFrame",Private).Invoke(fold,null);}
     Check(fold.Body.Visible&&!fold.Summary.Visible&&input.Text=="unsaved draft","Rapid reversal broke settings");
    }
   }
  }
 }
}
