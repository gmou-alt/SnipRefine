using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class MainForm
    {
        private DshNumber requestTimeout, requestTokens, requestTemperature;
        private DshToggle requestProxy, requestReview;
        private void BuildAiRequestPage()
        {
            Panel page = NewPage();
            PageHeader(page,"AI 请求","设置超时、输出长度与代理；保存后用于后续请求");
            DshGlassCard card = NewCard(page,32,100,588,380);
            requestTimeout = RequestNumber(card,"请求超时（秒）",24,10,300,cfg.AiTimeoutSeconds);
            requestTokens = RequestNumber(card,"最大输出 tokens",84,256,8192,cfg.AiMaxOutputTokens);
            requestTemperature = RequestNumber(card,"温度（十分之一；2 = 0.2）",144,0,20,cfg.AiTemperature);
            requestProxy = new DshToggle("使用 Windows 系统代理（关闭则直接连接）");
            requestProxy.Checked = cfg.AiUseSystemProxy; requestProxy.SetBounds(24,216,540,30); card.Controls.Add(requestProxy);
            requestReview = new DshToggle("应用 AI 修改前逐项勾选确认");
            requestReview.Checked=cfg.AiReviewOperations; requestReview.SetBounds(24,256,540,30); card.Controls.Add(requestReview);
            DshLabel hint = new DshLabel("接口可能有不同参数限制；出现参数错误时按提供商要求调整。");
            hint.Small = true; hint.Tertiary = true; hint.SetBounds(24,300,540,44); card.Controls.Add(hint);
        }
        private DshNumber RequestNumber(Control parent,string caption,int y,int min,int max,int value)
        {
            DshLabel label = new DshLabel(caption); label.SetBounds(24,y,380,34); parent.Controls.Add(label);
            DshNumber number = new DshNumber { Minimum=min, Maximum=max, Value=value }; number.SetBounds(420,y,144,34); parent.Controls.Add(number); return number;
        }
        private void SaveAiRequestSettings()
        {
            cfg.AiTimeoutSeconds=requestTimeout.Value; cfg.AiMaxOutputTokens=requestTokens.Value;
            cfg.AiTemperature=requestTemperature.Value; cfg.AiUseSystemProxy=requestProxy.Checked; cfg.AiReviewOperations=requestReview.Checked;
        }
        private void BeginProviderAction(bool vision,DshButton button)
        {
            if (!CommitProviderForm()) return;
            AiProvider active=cfg.ActiveProvider();
            if (active == null) { SetAiTestResult("请先配置提供商"); return; }
            AiProvider provider=active.Snapshot();
            string error=provider.ValidationError(vision);
            if (error != null) { SetAiTestResult(error); return; }
            string formModels=txtProvModels.Text, formEndpoint=txtProvEndpoint.Text, formProtocol=Convert.ToString(cboProvProtocol.SelectedIndex);
            int timeout=cfg.AiTimeoutSeconds*1000, tokens=cfg.AiMaxOutputTokens; bool proxy=cfg.AiUseSystemProxy;
            button.Enabled=false; SetAiTestResult(vision ? "正在测试模型看图能力，使用本地生成的测试图…" : "正在获取模型列表…");
            Thread worker=new Thread(delegate()
            {
                string failure=null; bool seen=false; List<string> models=null;
                try
                {
                    AiClient.UseSystemProxy=proxy; AiClient.OutputTokens=tokens;
                    if (!vision) models=AiProviderDiscovery.Fetch(provider,timeout,proxy);
                    else
                    {
                        string code=new Random().Next(10000,99999).ToString(); string image; float scale;
                        using (Bitmap bitmap=new Bitmap(320,120))
                        using (Graphics graphics=Graphics.FromImage(bitmap))
                        using (Font font=Dsh.F(36,FontStyle.Bold))
                        { graphics.Clear(Color.White); graphics.DrawString(code,font,Brushes.Black,30,24); image=AiClient.EncodeImage(bitmap,512,out scale); }
                        string response=AiClient.CallProvider(provider,"只读取图片，不推测图片内容。","仅返回图片中的五位数字。",image,0,false,timeout);
                        string extractError; string content=AiClient.ExtractContentFor(provider,response,out extractError);
                        if (extractError != null) throw new InvalidOperationException(extractError);
                        seen=content != null && content.Contains(code);
                    }
                }
                catch (Exception ex) { failure=ex.Message; }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (IsDisposed) return; button.Enabled=true;
                        if (failure != null) { SetAiTestResult("测试失败："+AiTroubleshooting.Explain(failure)); return; }
                        if (vision) { SetAiTestResult(seen ? "看图测试通过：成功识别测试数字；不代表标注坐标准确率。" : "已响应，但未识别测试图；请检查视觉模型和图片格式兼容性。"); return; }
                        if (cfg.ActiveProvider()!=active || txtProvModels.Text!=formModels || txtProvEndpoint.Text!=formEndpoint || Convert.ToString(cboProvProtocol.SelectedIndex)!=formProtocol)
                        { SetAiTestResult("配置已变更，本次模型列表未覆盖输入。"); return; }
                        foreach (string model in models) if (!active.Models.Contains(model)) active.Models.Add(model);
                        FillProviderForm(active); SetAiTestResult("已获取 "+models.Count+" 个模型，请选择支持看图的模型并保存设置。");
                    });
                }
                catch (InvalidOperationException) { }
            });
            worker.IsBackground=true;
            try { worker.Start(); } catch (Exception ex) { button.Enabled=true; SetAiTestResult("无法开始请求："+ex.Message); }
        }
    }
}
