using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
namespace SnapGrab {
 internal partial class MainForm {
  private readonly string[] savedPageSignatures=new string[8];
  private readonly bool[] settingsDirty=new bool[8];
  private readonly string[] settingsNavTitles=new string[8];
  private Timer settingsDraftTimer;
  private void InitializeSettingsDrafts(){
   for(int i=0;i<pages.Count;i++){settingsNavTitles[i]=navItems[i].Text;savedPageSignatures[i]=SettingsSignature(i);}
   settingsDraftTimer=new Timer {Interval=180};settingsDraftTimer.Tick+=delegate {RefreshSettingsDrafts();};settingsDraftTimer.Start();RefreshSettingsDrafts();
  }
  private static void AppendSettingsValues(Control parent,StringBuilder text){
   foreach(Control c in parent.Controls){
    DshSelect select=c as DshSelect;DshToggle toggle=c as DshToggle;DshNumber number=c as DshNumber;TextBox box=c as TextBox;
    if(select!=null)text.Append("select:").Append(select.SelectedIndex).Append(';');
    else if(toggle!=null)text.Append("toggle:").Append(toggle.Checked).Append(';');
    else if(number!=null)text.Append("number:").Append(number.Value).Append(';');
    else if(box!=null)text.Append("text:").Append(box.Text.Length).Append(':').Append(box.Text).Append(';');
    else AppendSettingsValues(c,text);
   }
  }
  private string SettingsSignature(int index){
   StringBuilder text=new StringBuilder();AppendSettingsValues(savedSections[index].Body,text);
   if(index==0)text.Append("hotkey:").Append(pendingHotkey);
   if(index==3){text.Append(cfg.AiActiveProviderId);foreach(AiProvider p in cfg.AiProviders){foreach(string value in new string[]{p.Id,p.Name,p.Endpoint,p.ApiKey,p.Protocol,p.SelectedModel})text.Append(value==null?0:value.Length).Append(':').Append(value);foreach(string model in p.Models)text.Append(model.Length).Append(':').Append(model);}}
   using(SHA256 hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
  }
  private void RefreshSettingsDrafts(){
   if(settingsDraftTimer==null)return;
   for(int i=0;i<pages.Count;i++){
    bool dirty=SettingsSignature(i)!=savedPageSignatures[i];bool wasDirty=settingsDirty[i];settingsDirty[i]=dirty;
    if(i==currentSettingsPage&&dirty!=wasDirty)SetStatus(dirty ? "本页有未保存修改，请点击保存修改。" : "本页内容与已保存设置一致。");
    pageSaveButtons[i].Enabled=dirty&&savedSections[i].Open;
    pageSaveButtons[i].Text=dirty ? "保存修改 · 未保存" : "已保存";
    navItems[i].Text=settingsNavTitles[i]+(dirty ? "  •" : "");
   }
   SyncFooterSaveState();
  }
  private void MarkSettingsPageSaved(int index){if(settingsDraftTimer!=null){savedPageSignatures[index]=SettingsSignature(index);RefreshSettingsDrafts();}}
  internal bool ConfirmDiscardSettingsDrafts(){
   RefreshSettingsDrafts();List<string> dirty=new List<string>();for(int i=0;i<settingsDirty.Length;i++)if(settingsDirty[i])dirty.Add(settingsNavTitles[i]);
   if(dirty.Count==0)return true;
   if(MessageBox.Show(this,"这些页面有未保存修改："+string.Join("、",dirty.ToArray())+"。\n\n是否放弃修改并离开？选择“否”可继续编辑并分别保存。","未保存的设置",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return false;
   try {Config saved=Config.ReadSavedOr(persistedSettings);cfg.AiProviders=Config.Copy(saved).AiProviders;cfg.AiActiveProviderId=saved.AiActiveProviderId;Dsh.ApplyTheme(cfg.UiTheme!="dark",this);}
   catch {SetStatus("无法读取已保存配置，保留当前修改；请重试。");return false;}
   for(int i=0;i<pages.Count;i++)savedPageSignatures[i]=SettingsSignature(i);RefreshSettingsDrafts();return true;
  }
 }
}
