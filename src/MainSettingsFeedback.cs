using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
namespace SnapGrab {
 internal partial class MainForm {
  private readonly SavedSettingsSection[] savedSections=new SavedSettingsSection[8];
  private readonly DshButton[] pageSaveButtons=new DshButton[8];
  private readonly List<DshButton> settingsFooterButtons=new List<DshButton>();
  private Config persistedSettings;
  private int currentSettingsPage;
  private DshSelect appearanceLanguage,appearanceTheme;
  private DshToggle recentRemember;
  private void BuildSettingsFeedback(){
   for(int i=0;i<pages.Count;i++){
    int index=i;Panel page=pages[i];SavedSettingsSection section=SavedSettingsSection.Wrap(page,100);savedSections[i]=section;section.ManagedWidth=true;
    DshButton save=new DshButton("保存本页设置",DshButton.Kind.Primary);save.SetBounds(440,24,180,30);save.Click+=delegate {SaveSettingsPage(index);};page.Controls.Add(save);save.BringToFront();pageSaveButtons[i]=save;save.Visible=false;
    foreach(Control header in page.Controls)if(header is DshLabel&&header.Top==30)header.Width=380;
    section.OpenChanged+=delegate {RefreshSettingsDrafts();SyncFooterSaveState();};
   }
  }
  private static readonly string[][] PageFields={
   new string[]{"Hotkey","QuickCaptureHotkey","FullscreenCaptureHotkey","FullscreenOpenEditor"},
   new string[]{"SaveDir","FilenamePattern","DateSubfolder","Format","Quality","CopyToClipboard"},
   new string[]{"StartWithWindows","ShowWindowOnStart","MinimizeToTray","ShowFloatingIcon","TrimWorkingSet","ProcessPriority"},
   new string[]{"AiProviders","AiActiveProviderId","AiImageMaxEdge","AiPreset","AiEndpoint","AiModel","AiApiKey"},
   new string[]{"UiTheme"},
   new string[]{"AiTimeoutSeconds","AiMaxOutputTokens","AiTemperature","AiUseSystemProxy","AiReviewOperations"},
   new string[]{"KeepSavedHistory"},
   new string[]{"UiLanguage"}
  };
  private Config BuildPageConfig(int index,Config saved){
   Config candidate=Config.Copy(saved);
   switch(index){
    case 0:candidate.Hotkey=string.IsNullOrEmpty(pendingHotkey) ? cfg.Hotkey : pendingHotkey;candidate.QuickCaptureHotkey=txtQuickCapture.Text;candidate.FullscreenCaptureHotkey=txtFullscreenCapture.Text;candidate.FullscreenOpenEditor=fullscreenEditor.Checked;break;
    case 1:candidate.SaveDir=txtDir.Text.Trim();candidate.FilenamePattern=txtPattern.Text.Trim();if(candidate.FilenamePattern.Length==0)candidate.FilenamePattern="SnipRefine_{yyyy}{MM}{dd}_{HH}{mm}{ss}";candidate.DateSubfolder=chkDateFolder.Checked;candidate.Format=cboFormat.SelectedIndex==1 ? "jpg" : "png";candidate.Quality=numQuality.Value;candidate.CopyToClipboard=chkClipboard.Checked;break;
    case 2:candidate.StartWithWindows=chkAutostart.Checked;candidate.ShowWindowOnStart=chkShowOnStart.Checked;candidate.MinimizeToTray=chkMinimizeToTray.Checked;candidate.ShowFloatingIcon=false;candidate.TrimWorkingSet=chkTrim.Checked;candidate.ProcessPriority=PriorityFromIndex(cboPriority.SelectedIndex);break;
    case 3:if(!CommitProviderForm())return null;candidate.AiProviders=Config.Copy(cfg).AiProviders;candidate.AiActiveProviderId=cfg.AiActiveProviderId;candidate.AiImageMaxEdge=numAiEdge.Value;candidate.AiPreset=candidate.AiEndpoint=candidate.AiModel=candidate.AiApiKey="";break;
    case 4:candidate.UiTheme=appearanceTheme.SelectedIndex==1 ? "dark" : "light";break;
    case 5:candidate.AiTimeoutSeconds=requestTimeout.Value;candidate.AiMaxOutputTokens=requestTokens.Value;candidate.AiTemperature=requestTemperature.Value;candidate.AiUseSystemProxy=requestProxy.Checked;candidate.AiReviewOperations=requestReview.Checked;break;
    case 6:candidate.KeepSavedHistory=recentRemember.Checked;break;
    case 7:candidate.UiLanguage=UiText.LanguageCode(appearanceLanguage.SelectedIndex);break;
    default:return null;
   }
   return candidate;
  }
  private void SaveSettingsPage(int index){
   if(index<0||index>=savedSections.Length||!savedSections[index].Open)return;
   RefreshSettingsDrafts();if(settingsDraftTimer!=null&&!settingsDirty[index])return;
   Config candidate;
   try {candidate=BuildPageConfig(index,Config.ReadSavedOr(persistedSettings));}catch(Exception ex){SetStatus(ex.Message);return;}
   if(candidate==null)return;
   if(index==1){string directoryError=ValidateSaveDirectory(candidate);if(directoryError!=null){SetStatus(directoryError);return;}}
   Config before=Config.Copy(cfg);string error;
   if(index==0&&!app.ApplyCaptureHotkeys(candidate.Hotkey,candidate.QuickCaptureHotkey,candidate.FullscreenCaptureHotkey,out error)){SetStatus("快捷键未保存："+error);return;}
   if(index==2&&!app.ApplyAutostartSetting(candidate.StartWithWindows)){SetStatus("启动设置未保存：开机自启应用失败。");return;}
   if(!candidate.Save()){
    if(index==0)app.ApplyCaptureHotkeys(before.Hotkey,before.QuickCaptureHotkey,before.FullscreenCaptureHotkey,out error);
    if(index==2)app.ApplyAutostartSetting(before.StartWithWindows);
    SetStatus("保存失败，设置内容保持展开；请检查配置文件权限或磁盘空间后重试。");return;
   }
   foreach(string name in PageFields[index]){var field=typeof(Config).GetField(name);field.SetValue(cfg,field.GetValue(candidate));}
   persistedSettings=Config.Copy(candidate);
   if(index==0){pendingHotkey=cfg.Hotkey;lblHotkeyState.Text="已保存并生效："+app.HotkeyText;lblHotkeyState.ForeColor=Dsh.SuccessText;}
   if(index==2){app.ApplyFloatingIconSetting();app.ApplyProcessPrioritySetting();}
   if(index==3){RebuildProviderList();if(chatPanel!=null)chatPanel.RefreshModel();}
   if(index==6){app.ApplyHistorySetting(cfg.KeepSavedHistory);RefreshRecentScreens();}
   ShowSavedSettings(index);
  }
  private static string ValidateSaveDirectory(Config candidate){
   try {string path=candidate.EffectiveSaveDir();System.IO.Directory.CreateDirectory(path);if(!Config.IsWritableDir(path))return "截图设置未保存：目录不可写，请选择其他目录。";return null;}
   catch {return "截图设置未保存：无法创建或访问保存目录，请检查路径与权限。";}
  }
  private void SyncFooterSaveState(){
   if(settingsFooterButtons.Count==0||currentSettingsPage<0||currentSettingsPage>=savedSections.Length)return;
   SavedSettingsSection section=savedSections[currentSettingsPage];if(section==null)return;
   bool dirty=settingsDraftTimer==null ? section.Open : settingsDirty[currentSettingsPage];
   settingsFooterButtons[0].Enabled=section.Open&&dirty;
   settingsFooterButtons[0].Text=dirty ? "保存修改 · 未保存" : "已保存";
  }
  private string SavedPageMessage(int index){
   switch(index){
    case 0:return "快捷键已保存并生效："+cfg.Hotkey+" / "+cfg.QuickCaptureHotkey+" / "+cfg.FullscreenCaptureHotkey;
    case 1:return "截图输出已保存："+cfg.Format.ToUpperInvariant()+" · "+cfg.EffectiveSaveDir();
    case 2:return "启动与后台设置已保存；启动时显示窗口的选项在下次启动时使用。";
    case 3:return "AI 服务配置已保存；连接和模型能力请单独测试。";
    case 4:return "主题已保存并预览。";
    case 7:return UiText.NormalizeLanguage(cfg.UiLanguage)!=UiText.NormalizeLanguage(UiText.Language) ? "语言已保存；重启后生效。" : "语言已保存，当前界面已使用此语言。";
    case 5:return "AI 请求参数已保存，将用于之后的新请求。";
    case 6:return cfg.KeepSavedHistory ? "历史记录设置已保存：记住截图历史。" : "历史记录设置已保存：不记住截图历史。";
    default:return "本页设置已保存。";
   }
  }
  private void ShowSavedSettings(int index){
   MarkSettingsPageSaved(index);
   string message=SavedPageMessage(index);
   SetStatus(message+" · "+DateTime.Now.ToString("HH:mm:ss"));
   pages[index].AutoScrollPosition=Point.Empty;
   savedSections[index].Saved(message+"\n仅保存本页，其他页未保存的修改保持原样。");
   SyncFooterSaveState();
  }
 }
}
