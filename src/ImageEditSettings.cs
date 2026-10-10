using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab {
 internal static class ImageEditSettings {
  internal static void Show(IWin32Window owner,Config cfg) {
   using(ThemedDialog d=new ThemedDialog()) {
    d.Text=UiText.T("图片编辑服务（独立于看图模型）");d.ClientSize=new Size(620,520);d.StartPosition=FormStartPosition.CenterParent;d.ContentArea.AutoScroll=true;SavedSettingsSection section=null;
    DshSelect protocol=new DshSelect();protocol.Items.Add("OpenAI Images Edits / Compatible");protocol.Items.Add("Gemini Native (Experimental)");protocol.SelectedIndex=cfg.ImageEditProtocol=="gemini" ? 1 : 0;protocol.SetBounds(16,16,580,34);d.ContentArea.Controls.Add(protocol);
    string[] labels={"图片编辑完整地址","图片编辑模型 ID","图片编辑 API Key"};TextBox[] fields=new TextBox[3];string[] values={cfg.ImageEditEndpoint,cfg.ImageEditModel,cfg.ImageEditKey};
    for(int i=0;i<3;i++){DshLabel l=new DshLabel(labels[i]);l.SetBounds(16,64+i*76,580,24);d.ContentArea.Controls.Add(l);fields[i]=new TextBox {Text=values[i],UseSystemPasswordChar=i==2};fields[i].SetBounds(16,92+i*76,580,28);d.ContentArea.Controls.Add(fields[i]);}
    DshLabel note=new DshLabel("文本/看图连接不代表图片编辑可用；生成请求可能计费。密钥加密保存在本机。Gemini 为实验适配，须填写支持图片输出的模型与完整 generateContent 地址。");note.SetBounds(16,296,580,60);d.ContentArea.Controls.Add(note);
    DshLabel status=new DshLabel("");status.SetBounds(16,364,380,60);d.ContentArea.Controls.Add(status);
    DshButton save=new DshButton(UiText.T("保存"),DshButton.Kind.Primary);save.SetBounds(430,372,166,36);d.ContentArea.Controls.Add(save);
    save.Click+=delegate {
     string endpoint=fields[0].Text.Trim(),model=fields[1].Text.Trim(),key=fields[2].Text.Trim();
     AiProvider test=new AiProvider {Endpoint=endpoint,ApiKey=key};test.Models.Add(model);string error=test.ValidationError();if(error!=null){status.Text=error;return;}
     string selectedProtocol=protocol.SelectedIndex==1 ? "gemini" : "openai";
     string required=selectedProtocol=="gemini" ? "/models/"+Uri.EscapeDataString(model)+":generateContent" : "/images/edits";
     if(!new Uri(endpoint).AbsolutePath.TrimEnd(new char[]{'/'}).EndsWith(required,StringComparison.OrdinalIgnoreCase)){status.Text=UiText.T("接口地址格式不匹配")+" · "+required;return;}
     Config candidate;try {candidate=Config.ReadSavedOr(cfg);}catch(Exception ex){status.Text=ex.Message;return;}
     candidate.ImageEditProtocol=selectedProtocol;candidate.ImageEditEndpoint=endpoint;candidate.ImageEditModel=model;candidate.ImageEditKey=key;
     if(candidate.Save()){cfg.ImageEditProtocol=selectedProtocol;cfg.ImageEditEndpoint=endpoint;cfg.ImageEditModel=model;cfg.ImageEditKey=key;status.Text=UiText.T("配置已保存");section.Saved("图片编辑配置已写入本机；其他设置保持不变。摘要不显示密钥。");}else status.Text=UiText.T("配置保存失败，原配置已保留");
    };
    section=SavedSettingsSection.Wrap(d.ContentArea,0);
    DshButton done=new DshButton("完成 / 关闭");done.SetBounds(400,88,160,34);done.Click+=delegate {d.Close();};section.Summary.Controls.Add(done);done.BringToFront();
    section.OpenChanged+=delegate {done.Visible=!section.Open;};done.Visible=false;
    d.ShowDialog(owner);
   }
  }
 }
}
