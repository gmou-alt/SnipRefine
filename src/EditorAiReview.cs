using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class EditorForm
    {
        private Bitmap aiBefore;
        private bool compareBefore;
        private int aiAppliedRevision;
        private string latestOcr = "";
        private AiRequestCancellation aiCancellation;
        private GlassButton aiCompareButton, aiAcceptButton, aiUndoButton, aiCancelButton;

        private void BuildAiReviewControls()
        {
            BuildAiRegionControls();
            aiCompareButton = ReviewButton("修改前/后", delegate {
                if (aiBusy || aiBefore == null) { AiSetStatus(UiText.T("完成 AI 修改后可对比")); return; }
                compareBefore = !compareBefore; aiCompareButton.Text = UiText.T(compareBefore ? "查看修改后" : "查看修改前");
                AiSetStatus(compareBefore ? "显示本次修改前 · 编辑与输出请切回修改后" : "显示当前修改结果"); Invalidate();
            });
            aiAcceptButton = ReviewButton("结束对比", delegate {
                if (aiBusy) return;
                ClearAiComparison(); AiSetStatus(UiText.T("已结束对比；结果仍可撤销，保存或复制后才输出")); Invalidate();
            });
            aiUndoButton = ReviewButton("撤销本次", delegate {
                if (aiBusy || undoSteps.Count == 0 || undoSteps[undoSteps.Count-1].Kind != UndoStep.T.Ai || aiAppliedRevision != editRevision)
                { AiSetStatus(UiText.T("请先撤销后续手工操作，再撤销 AI 修改")); return; }
                DoUndo(); ClearAiComparison(); AiSetStatus(UiText.T("已撤销本次 AI 修改"));
            });
            aiCancelButton = ReviewButton("取消请求", delegate { CancelAiRequest(); });
            WideButton translate = new WideButton { Text=UiText.T("翻译") };
            translate.Click += delegate { if(aiBusy) return; requestingTranslation=true; AiSend("识别截图中的文字并翻译为中文；仅在 say 中返回原文与对应翻译，ops=[]，不要修改图片。"); };
            wideButtons.Add(translate); Controls.Add(translate);
            WideButton ocrCopy = new WideButton { Text = UiText.T("复制OCR") };
            ocrCopy.Click += delegate {
                if (string.IsNullOrWhiteSpace(latestOcr)) { AiSetStatus(UiText.T("请先识别文字")); return; }
                try { Clipboard.SetText(latestOcr.Trim()); AiSetStatus(UiText.T("识别文字已复制")); }
                catch (Exception ex) { AiSetStatus("复制失败：" + ex.Message); }
            };
            wideButtons.Add(ocrCopy); Controls.Add(ocrCopy);
            WideButton result=new WideButton { Text=UiText.T("查看识别/译文") }; result.Click+=delegate { ShowTextResult(); }; wideButtons.Add(result); Controls.Add(result);
        }
        private GlassButton ReviewButton(string caption, EventHandler click)
        {
            GlassButton button = new GlassButton { Text = UiText.T(caption) }; button.Click += click; aiBody.Controls.Add(button); return button;
        }
        private void LayoutAiReviewControls(int width)
        {
            int size = (width-30)/2;
            aiCompareButton.SetBounds(12,112,size,28); aiAcceptButton.SetBounds(18+size,112,size,28);
            aiUndoButton.SetBounds(12,144,size,28); aiCancelButton.SetBounds(18+size,144,size,28);
        }
        private void ClearAiComparison()
        {
            compareBefore = false;
            if (aiBefore != null) { aiBefore.Dispose(); aiBefore = null; }
            if (aiCompareButton != null) aiCompareButton.Text = UiText.T("修改前/后");
        }
        private bool ReviewAiOperations(AiReply reply)
        {
            if (reply.Ops == null || reply.Ops.Count == 0) return true;
            bool changesImage=!aiRegion.IsEmpty; foreach(AiOp op in reply.Ops) if(op!=null && op.Kind!="ocr") changesImage=true;
            if(!changesImage) return true;
            if (reply.Ops.Count > 64) { AiSetStatus(UiText.T("AI 操作过多，请缩小任务范围。")); return false; }
            using (Bitmap targetPreview=BuildAiTargetPreview(reply))
            using (ThemedDialog dialog = new ThemedDialog())
            using (FlowLayoutPanel list = new FlowLayoutPanel())
            using (DshButton apply = new DshButton("应用勾选项",DshButton.Kind.Primary))
            using (DshButton cancel = new DshButton(UiText.T("放弃")))
            {
                bool regional=targetPreview!=null;
                dialog.Text=UiText.T("确认本次 AI 操作"); dialog.ClientSize=new Size(560,regional ? 620 : 420); dialog.MinimumSize=new Size(460,regional ? 580 : 340);
                if(regional) {
                    PictureBox preview=new PictureBox {Image=targetPreview,SizeMode=PictureBoxSizeMode.Zoom};preview.SetBounds(12,12,534,184);preview.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;dialog.ContentArea.Controls.Add(preview);
                    DshLabel hint=new DshLabel("橙色编号对应下方操作；核对目标和文字，可取消后重新框选。");hint.SetBounds(12,200,534,32);dialog.ContentArea.Controls.Add(hint);
                }
                dialog.StartPosition=FormStartPosition.CenterParent;
                list.AutoScroll=true; list.FlowDirection=FlowDirection.TopDown; list.WrapContents=false;
                list.SetBounds(12,regional ? 238 : 12,534,regional ? 170 : 296); list.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
                var choices = new System.Collections.Generic.List<DshToggle>();
                foreach (AiOp op in reply.Ops)
                {
                    var choice = new DshToggle(op == null ? "无效操作" : (choices.Count+1)+" · "+op.Kind+" · "+op.Text+" · "+op.Note+" · "+op.X+","+op.Y+" / "+op.W+"×"+op.H);
                    choice.Size = new Size(500,40); choice.Checked = op != null; choice.Enabled = op != null;
                    choice.KeyDown += delegate(object sender,KeyEventArgs e) { if (e.KeyCode==Keys.Space) { var item=(DshToggle)sender; item.Checked=!item.Checked; e.Handled=true; } };
                    choices.Add(choice); list.Controls.Add(choice);
                }
                list.Resize += delegate { foreach (DshToggle choice in choices) choice.Width=Math.Max(100,list.ClientSize.Width-28); };
                if(regional) {
                    System.Text.StringBuilder text=new System.Text.StringBuilder();int number=0;
                    foreach(AiOp op in reply.Ops) {number++;if(op!=null && !string.IsNullOrEmpty(op.Text)) text.AppendLine(number+" · "+op.Text);}
                    TextBox recognized=new TextBox {ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false,Text=text.ToString()};recognized.SetBounds(12,410,534,100);recognized.Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;dialog.ContentArea.Controls.Add(recognized);
                }
                dialog.ContentArea.Controls.Add(list); apply.SetBounds(310,regional ? 526 : 326,138,34); cancel.SetBounds(456,regional ? 526 : 326,90,34);
                apply.Anchor=cancel.Anchor=AnchorStyles.Bottom|AnchorStyles.Right;
                dialog.ContentArea.Controls.Add(apply); dialog.ContentArea.Controls.Add(cancel);
                apply.Click+=delegate { dialog.DialogResult=DialogResult.OK; }; cancel.Click+=delegate { dialog.DialogResult=DialogResult.Cancel; };
                dialog.KeyPreview=true; dialog.KeyDown+=delegate(object sender,KeyEventArgs e) { if (e.KeyCode==Keys.Escape) dialog.DialogResult=DialogResult.Cancel; };
                if (dialog.ShowDialog(this)!=DialogResult.OK) return false;
                var accepted=new System.Collections.Generic.List<AiOp>();
                for (int i=0;i<reply.Ops.Count;i++) if (choices[i].Checked) accepted.Add(reply.Ops[i]);
                reply.Ops=accepted; return true;
            }
        }

        private void DrawSoftBlur(Graphics g,Shape shape)
        {
            Rectangle source=Rectangle.Round(shape.Rect); source.Intersect(new Rectangle(0,0,Src.Width,Src.Height));
            if (source.Width<1 || source.Height<1) return;
            using (Bitmap small=new Bitmap(Math.Max(1,source.Width/12),Math.Max(1,source.Height/12)))
            using (Graphics sample=Graphics.FromImage(small))
            {
                sample.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                sample.DrawImage(UnderlyingBitmap(),new Rectangle(0,0,small.Width,small.Height),source,GraphicsUnit.Pixel);
                System.Drawing.Drawing2D.GraphicsState state=g.Save();
                try { g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear; g.DrawImage(small,source); }
                finally { g.Restore(state); }
            }
        }

        private void CancelAiRequest()
        {
            if (!aiBusy) { AiSetStatus(UiText.T("当前没有进行中的请求")); return; }
            aiRequestId++; aiBusy = false; StopAiProgress();
            if (aiCancellation != null) { aiCancellation.Cancel(); aiCancellation = null; }
            aiSendButton.Enabled = true;
            foreach (GlassButton button in aiQuickButtons) button.Enabled = true;
            ClearAiComparison(); AiSetStatus(UiText.T("请求已取消，迟到回复不会应用"));
        }
    }
}
