using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class MainForm
    {
        private void ShowAiSetupWizard()
        {
            using(ThemedDialog dialog=new ThemedDialog())
            {
                dialog.Text="AI 快速配置"; dialog.ClientSize=new Size(580,590); dialog.MinimumSize=new Size(540,480);
                dialog.StartPosition=FormStartPosition.CenterParent;
                SavedSettingsSection section=null;AiProvider savedProvider=null;
                Panel body=new Panel { Dock=DockStyle.Fill, AutoScroll=true, BackColor=Dsh.Canvas }; dialog.ContentArea.Controls.Add(body);
                var title=new DshLabel("1 选择服务 → 2 填写密钥与模型 → 3 测试 → 4 保存"); title.SetBounds(20,16,532,32); body.Controls.Add(title);
                var service=new DshSelect(); foreach(AiPreset preset in AiClient.Presets) service.Items.Add(preset.Name);
                service.SetBounds(20,58,532,36); body.Controls.Add(service);
                var keyLabel=new DshLabel("API 密钥（本机 Ollama 可留空）"); keyLabel.SetBounds(20,108,532,24); body.Controls.Add(keyLabel);
                var key=new DshTextInput(); key.Inner.UseSystemPasswordChar=true; key.SetBounds(20,140,532,36); body.Controls.Add(key);
                var modelLabel=new DshLabel("模型 ID：截图理解需要视觉模型，可按服务控制台修改"); modelLabel.SetBounds(20,190,532,32); body.Controls.Add(modelLabel);
                var model=new DshTextInput(); model.SetBounds(20,230,532,36); body.Controls.Add(model);
                var advanced=new DshButton(UiText.T("高级：接口地址与协议")); advanced.SetBounds(20,280,240,32); body.Controls.Add(advanced);
                var endpoint=new DshTextInput(); endpoint.SetBounds(20,322,364,34); endpoint.Visible=false; body.Controls.Add(endpoint);
                var protocol=new DshSelect(); protocol.Items.Add("OpenAI 兼容"); protocol.Items.Add("Anthropic"); protocol.SetBounds(394,322,158,34); protocol.Visible=false; body.Controls.Add(protocol);
                advanced.Click+=delegate { endpoint.Visible=!endpoint.Visible; protocol.Visible=endpoint.Visible; };
                var result=new TextBox { Multiline=true, ReadOnly=true, ScrollBars=ScrollBars.Vertical, BackColor=Dsh.GlassFill, ForeColor=Dsh.TextPrimary };
                result.SetBounds(20,370,532,74); result.Text="截图和手工编辑无需配置 AI。测试将调用所选服务，可能产生费用；看图测试使用生成的数字图片。"; body.Controls.Add(result);
                DshButton textTest=new DshButton(UiText.T("测试连接")), visionTest=new DshButton(UiText.T("测试看图")), save=new DshButton(UiText.T("保存配置"),DshButton.Kind.Primary);
                textTest.SetBounds(20,456,144,36); visionTest.SetBounds(176,456,144,36); save.SetBounds(332,456,220,36);
                body.Controls.Add(textTest); body.Controls.Add(visionTest); body.Controls.Add(save);
                var note=new DshLabel("保存不表示看图测试通过。图像标注、OCR 和翻译请先测试看图。"); note.Small=true; note.SetBounds(20,508,532,42); body.Controls.Add(note);
                Func<AiProvider> read=delegate {
                    AiProvider p=new AiProvider { Id="provider_"+Guid.NewGuid().ToString("N").Substring(0,12) };
                    AiClient.Presets[service.SelectedIndex].Fill(p); p.ApiKey=key.Text.Trim(); p.Endpoint=AiProvider.NormalizeEndpoint(endpoint.Text.Trim(),protocol.SelectedIndex==1);
                    p.Protocol=protocol.SelectedIndex==1 ? "anthropic" : "openai"; p.Models.Clear(); p.Models.Add(model.Text.Trim()); p.SelectedModel=model.Text.Trim(); return p;
                };
                service.SelectedIndexChanged+=delegate {
                    AiPreset p=AiClient.Presets[service.SelectedIndex]; key.Text=""; model.Text=p.Model; endpoint.Text=p.Endpoint; protocol.SelectedIndex=p.Protocol=="anthropic" ? 1 : 0;
                    result.Text="已选择 "+p.Name+"。填写该服务的密钥和可用模型；预设模型需按账号权限核对。";
                };
                service.SelectedIndex=0;
                int generation=0; bool busy=false;
                EventHandler invalidate=delegate { generation++; if(!busy) result.Text="配置已变更，请重新测试；之前的测试结果不适用于当前输入。"; };
                key.Inner.TextChanged+=invalidate; model.Inner.TextChanged+=invalidate; endpoint.Inner.TextChanged+=invalidate; protocol.SelectedIndexChanged+=invalidate;
                Action<bool> test=delegate(bool vision) {
                    if(busy) return;
                    AiProvider p=read(); string validation=p.ValidationError(); if(validation!=null) { result.Text=validation; return; }
                    int requestGeneration=generation; busy=true; textTest.Enabled=visionTest.Enabled=save.Enabled=false;
                    result.Text=vision ? "正在测试看图，使用本地生成的数字图…" : "正在测试文本连接…";
                    Thread worker=new Thread(delegate() {
                        string response=null,error=null,code=null;
                        try {
                            AiClient.UseSystemProxy=cfg.AiUseSystemProxy; AiClient.OutputTokens=cfg.AiMaxOutputTokens;
                            string image=null; float scale;
                            if(vision) { code=new Random().Next(10000,99999).ToString(); using(Bitmap b=new Bitmap(320,120)) using(Graphics g=Graphics.FromImage(b)) using(Font f=Dsh.F(36,FontStyle.Bold)) { g.Clear(Color.White); g.DrawString(code,f,Brushes.Black,30,24); image=AiClient.EncodeImage(b,512,out scale); } }
                            string json=AiClient.CallProvider(p,"",vision ? "仅返回图片中的五位数字，不推测。" : "回复：可用",image,0,false,cfg.AiTimeoutSeconds*1000);
                            response=AiClient.ExtractContentFor(p,json,out error);
                            if(error==null && string.IsNullOrWhiteSpace(response)) error="模型返回空回复";
                            if(error==null && vision && !response.Contains(code)) error="未识别测试图，请使用支持图片输入的模型";
                        } catch(Exception ex) { error=ex.Message; }
                        try { dialog.BeginInvoke((Action)delegate {
                            if(dialog.IsDisposed) return; busy=false; textTest.Enabled=visionTest.Enabled=save.Enabled=true;
                            if(generation!=requestGeneration) { result.Text="输入已变更，请重新测试当前配置。"; return; }
                            result.Text=error!=null ? AiTroubleshooting.Explain(error) : vision ? "看图测试通过；不代表 OCR 或坐标识别始终准确。点击保存配置完成设置。" : "文本连接成功。截图理解还需要测试看图；点击保存配置后生效。";
                        }); } catch(InvalidOperationException) { }
                    }); worker.IsBackground=true; try { worker.Start(); } catch(Exception ex) { busy=false; textTest.Enabled=visionTest.Enabled=save.Enabled=true; result.Text="无法开始测试："+ex.Message; }
                };
                textTest.Click+=delegate { test(false); }; visionTest.Click+=delegate { test(true); };
                save.Click+=delegate {
                    AiProvider p=read(); string validation=p.ValidationError(); if(validation!=null) { result.Text=validation; return; }
                    if(savedProvider!=null)p.Id=savedProvider.Id;
                    Config candidate;try {candidate=Config.ReadSavedOr(cfg);}catch(Exception ex){result.Text=ex.Message;return;}
                    int diskIndex=candidate.AiProviders.FindIndex(delegate(AiProvider item){return item.Id==p.Id;});if(diskIndex>=0)candidate.AiProviders[diskIndex]=p.Snapshot();else candidate.AiProviders.Add(p.Snapshot());candidate.AiActiveProviderId=p.Id;
                    if(!candidate.Save()){result.Text="保存失败，请检查配置目录权限并重试。";return;}
                    int existing=cfg.AiProviders.FindIndex(delegate(AiProvider item){return item.Id==p.Id;});if(existing>=0)cfg.AiProviders[existing]=p;else cfg.AiProviders.Add(p);cfg.AiActiveProviderId=p.Id;
                    savedProvider=p;RebuildProviderList();FillProviderForm(p);if(chatPanel!=null)chatPanel.RefreshModel();
                    SetStatus("AI 配置已保存");section.Saved("AI 配置已写入本机。保存不代表看图测试通过；可继续编辑或关闭窗口。");
                };
                section=SavedSettingsSection.Wrap(body,0);
                DshButton done=new DshButton("完成 / 关闭");done.SetBounds(350,88,160,34);done.Click+=delegate {dialog.Close();};section.Summary.Controls.Add(done);done.BringToFront();section.OpenChanged+=delegate {done.Visible=!section.Open;};done.Visible=false;
                dialog.ShowDialog(this);
            }
        }
    }
}
