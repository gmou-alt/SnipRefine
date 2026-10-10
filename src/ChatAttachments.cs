using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class ChatPanel
    {
        private Bitmap selectedAttachment;
        private ChatGlassSurface attachmentCard;
        private PictureBox attachmentPreview;
        private DshLabel attachmentCaption;
        private DshButton recentAttachmentButton, fileAttachmentButton, removeAttachmentButton;
        private void OpenImageEditing()
        {
            if (AiChat.Busy) { SetStatus("请先结束当前请求。"); return; }
            if (app == null) return;
            Bitmap image = selectedAttachment != null ? (Bitmap)selectedAttachment.Clone() :
                chkIncludeShot.Checked ? app.GetLastShotCopy() : null;
            if (image == null) { SetStatus("先选择图片或最近截图，再点击图片修改。"); return; }
            string prompt = inputBox.Text.Trim();
            if (prompt.Length == 0) prompt = "请根据我的要求修改这张图片。支持标注、添加文字、打码、模糊和裁剪。";
            app.EditChatImage(image, prompt);
            SetStatus("图片编辑已结束；输出由编辑器中的复制或保存按钮确认。");
        }
        private void BuildAttachmentUi()
        {
            attachmentCard = new ChatGlassSurface(); bottomBar.Controls.Add(attachmentCard);
            attachmentPreview = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, BackColor = Dsh.GlassFill };
            attachmentPreview.Cursor = Cursors.Hand;
            attachmentPreview.DoubleClick += delegate
            {
                using (Bitmap image = selectedAttachment != null ? (Bitmap)selectedAttachment.Clone() :
                    chkIncludeShot.Checked && app != null ? app.GetLastShotCopy() : null)
                {
                    if (image == null) { SetStatus("暂无可查看的图片，请先选择图片。"); return; }
                    ShowAttachmentImage(image);
                }
            };
            attachmentCaption = new DshLabel("未附带图片"); attachmentCaption.Small = true;
            recentAttachmentButton = new DshButton(UiText.T("最近截图"));
            recentAttachmentButton.Click += delegate { ChooseRecentAttachment(); };
            fileAttachmentButton = new DshButton(UiText.T("选择图片"));
            fileAttachmentButton.Click += delegate
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*";
                    if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                    try { using (var image = Image.FromFile(dialog.FileName)) SetAttachment(new Bitmap(image), Path.GetFileName(dialog.FileName)); }
                    catch { SetStatus("无法读取图片，请选择 PNG、JPEG 或 BMP 图片。"); }
                }
            };
            removeAttachmentButton = new DshButton(UiText.T("移除"));
            removeAttachmentButton.Click += delegate
            {
                if (selectedAttachment != null) { selectedAttachment.Dispose(); selectedAttachment = null; }
                chkIncludeShot.Checked = false; RefreshAttachmentPreview();
            };
            attachmentCard.Controls.Add(attachmentPreview); attachmentCard.Controls.Add(attachmentCaption);
            attachmentCard.Controls.Add(recentAttachmentButton); attachmentCard.Controls.Add(fileAttachmentButton); attachmentCard.Controls.Add(removeAttachmentButton);
        }
        private void LayoutAttachmentUi(int width)
        {
            attachmentCard.SetBounds(14, 72, Math.Max(1,width-28), 72);
            attachmentPreview.SetBounds(8, 9, 56, 54);
            attachmentCaption.SetBounds(76, 5, Math.Max(1, width - 116), 24);
            recentAttachmentButton.SetBounds(76, 36, 92, 28);
            fileAttachmentButton.SetBounds(174, 36, 92, 28);
            removeAttachmentButton.SetBounds(272, 36, 64, 28);
        }
        private void SetAttachment(Bitmap image, string caption)
        {
            Bitmap old = selectedAttachment; selectedAttachment = image;
            if (old != null) old.Dispose();
            chkIncludeShot.Checked = false;
            SetPreview(image); attachmentCaption.Text = caption;
        }
        private void SetPreview(Image source)
        {
            Image old = attachmentPreview.Image;
            Bitmap preview = null;
            if (source != null)
            {
                double factor = Math.Min(128.0 / source.Width, 128.0 / source.Height);
                preview = new Bitmap(Math.Max(1, (int)(source.Width * factor)), Math.Max(1, (int)(source.Height * factor)));
                using (Graphics g = Graphics.FromImage(preview))
                { g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; g.DrawImage(source, 0, 0, preview.Width, preview.Height); }
            }
            attachmentPreview.Image = preview; if (old != null) old.Dispose();
            removeAttachmentButton.Enabled = source != null;
        }
        private void RefreshAttachmentPreview()
        {
            if (attachmentPreview == null) return;
            if (selectedAttachment != null) { SetPreview(selectedAttachment); return; }
            using (Bitmap latest = chkIncludeShot != null && chkIncludeShot.Checked && app != null ? app.GetLastShotCopy() : null)
            {
                SetPreview(latest);
                attachmentCaption.Text = latest == null ? "未附带图片" : "最新截图 · " + latest.Width + " × " + latest.Height;
            }
        }
        private void ChooseRecentAttachment()
        {
            if (app == null) return;
            List<RecentShot> records = app.RecentCopies();
            Bitmap latestCopy = app.GetLastShotCopy();
            try
            {
                if (records.Count == 0) { SetStatus("暂无最近截图，可以点击选择图片。"); return; }
                using (var dialog = new ThemedDialog())
                {
                    dialog.Text = "选择最近截图"; dialog.StartPosition = FormStartPosition.CenterParent;
                    dialog.ClientSize = new Size(520, 484); dialog.BackColor = Dsh.Canvas;
                    dialog.MinimizeBox = false; dialog.MaximizeBox = false;
                    var list = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };
                    dialog.ContentArea.Controls.Add(list);
                    foreach (RecentShot record in records)
                    {
                        RecentShot item = record;
                        bool latestOriginal = records.IndexOf(record) == 0 && latestCopy != null;
                        bool previewOnly = string.IsNullOrEmpty(item.Path) && !latestOriginal;
                        var card = new Panel { Width = 232, Height = 182, Margin = new Padding(6), BackColor = Dsh.GlassFill };
                        var picture = new PictureBox { Image = item.Thumbnail, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(8, 8, 216, 124) };
                        var select = new DshButton(item.Time.ToString("HH:mm:ss") + (previewOnly ? " · 预览图" : " · 原图"));
                        select.SetBounds(8, 140, 216, 32);
                        select.Click += delegate
                        {
                            try
                            {
                                Bitmap image;
                                if (!string.IsNullOrEmpty(item.Path))
                                { using (var original = Image.FromFile(item.Path)) image = new Bitmap(original); }
                                else image = latestOriginal ? (Bitmap)latestCopy.Clone() : (Bitmap)item.Thumbnail.Clone();
                                SetAttachment(image, "截图 " + item.Time.ToString("HH:mm:ss") + (previewOnly ? "（低分辨率预览）" : " · " + item.Size));
                                dialog.DialogResult = DialogResult.OK; dialog.Close();
                            }
                            catch { SetStatus("原截图文件无法读取，请选择其他图片。"); }
                        };
                        card.Controls.Add(picture); card.Controls.Add(select); list.Controls.Add(card);
                    }
                    dialog.ShowDialog(FindForm());
                }
            }
            finally { foreach (RecentShot record in records) record.Thumbnail.Dispose(); if (latestCopy != null) latestCopy.Dispose(); }
        }
        private void DisposeAttachmentImages()
        {
            if (selectedAttachment != null) { selectedAttachment.Dispose(); selectedAttachment = null; }
            if (attachmentPreview != null && attachmentPreview.Image != null)
            { attachmentPreview.Image.Dispose(); attachmentPreview.Image = null; }
        }
    }
}
