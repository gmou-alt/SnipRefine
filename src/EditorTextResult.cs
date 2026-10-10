using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class EditorForm
    {
        private bool requestingTranslation;
        private string latestTranslation="";
        private void ShowTextResult()
        {
            string initial=string.IsNullOrWhiteSpace(latestTranslation) ? latestOcr : latestTranslation;
            if(string.IsNullOrWhiteSpace(initial)) { AiSetStatus(UiText.T("请先识别文字或翻译，再查看结果。")); return; }
            using(ThemedDialog dialog=new ThemedDialog())
            {
                dialog.Text=UiText.T("OCR / 翻译结果"); dialog.ClientSize=new Size(620,470); dialog.MinimumSize=new Size(480,330); dialog.StartPosition=FormStartPosition.CenterParent;
                var hint=new DshLabel("请与原图核对；文本可编辑，图片不会随文本修改。"); hint.SetBounds(16,12,588,32); hint.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right; dialog.ContentArea.Controls.Add(hint);
                var content=new TextBox { Multiline=true, ScrollBars=ScrollBars.Both, WordWrap=false, Text=initial, Font=Dsh.Body, BackColor=Dsh.GlassFill, ForeColor=Dsh.TextPrimary };
                content.SetBounds(16,52,588,290); content.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right; dialog.ContentArea.Controls.Add(content);
                var copy=new DshButton("复制文本",DshButton.Kind.Primary); copy.SetBounds(330,360,130,36); copy.Anchor=AnchorStyles.Bottom|AnchorStyles.Right; dialog.ContentArea.Controls.Add(copy);
                var close=new DshButton(UiText.T("关闭")); close.SetBounds(472,360,132,36); close.Anchor=copy.Anchor; dialog.ContentArea.Controls.Add(close);
                copy.Click+=delegate { try { if(content.TextLength>0) { Clipboard.SetText(content.Text); hint.Text=UiText.T("文本已复制；可继续编辑并重新复制。"); } } catch(Exception ex) { hint.Text=UiText.T("复制失败：")+ex.Message; } };
                close.Click+=delegate { dialog.Close(); }; dialog.ShowDialog(this);
            }
        }
    }
}
