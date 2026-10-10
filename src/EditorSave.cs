using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class EditorForm
    {
        private string lastExplicitSave;
        private int savedImageRevision=-1;
        private string ImageSaveState(){if(string.IsNullOrEmpty(lastExplicitSave))return "未保存";return savedImageRevision!=editRevision||draft!=null||annotationInput!=null||selectionDragging ? "有未保存修改" : "已保存";}

        private void SaveEditedImage(bool choosePath)
        {
            if (aiBusy || compareBefore) { SetStatus("请等待 AI 完成并切回修改后，再保存。"); return; }
            CommitOpenTextInput();
            using (Bitmap image = RenderResult())
            {
                try
                {
                    string path;
                    if (choosePath)
                    {
                        using (SaveFileDialog dialog = new SaveFileDialog())
                        {
                            dialog.Title="另存为截图"; dialog.Filter="PNG 图片|*.png|JPEG 图片|*.jpg";
                            dialog.FilterIndex=cfg.Format=="jpg" || cfg.Format=="jpeg" ? 2 : 1;
                            dialog.FileName="SnipRefine_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            dialog.InitialDirectory=cfg.ResolveSaveDir(); dialog.AddExtension=true; dialog.OverwritePrompt=true;
                            if (dialog.ShowDialog(this)!=DialogResult.OK) return;
                            path=dialog.FileName;
                            string extension=Path.GetExtension(path).ToLowerInvariant();
                            bool jpeg=extension==".jpg" || extension==".jpeg";
                            if (!jpeg && extension!=".png") { SetStatus("请选择 .png 或 .jpg 文件名。"); return; }
                            // Stage bytes in the same directory: encoding failures cannot truncate an existing file.
                            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
                            try
                            {
                                if (jpeg)
                                {
                                    ImageCodecInfo codec=null;
                                    foreach (ImageCodecInfo candidate in ImageCodecInfo.GetImageEncoders()) if(candidate.MimeType=="image/jpeg") codec=candidate;
                                    if(codec==null) throw new InvalidOperationException("JPEG 编码器不可用");
                                    using (EncoderParameters parameters=new EncoderParameters(1))
                                    { parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,(long)cfg.Quality); image.Save(temp,codec,parameters); }
                                }
                                else image.Save(temp,ImageFormat.Png);
                                if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path);
                            }
                            finally { if(File.Exists(temp)) File.Delete(temp); }
                        }
                    }
                    else path=TrayApp.SaveImageFile(image,cfg.EffectiveSaveDir(),cfg.Format=="jpg" || cfg.Format=="jpeg",cfg.Quality,cfg);
                    lastExplicitSave=path;savedImageRevision=editRevision; if(app!=null) app.RecordSavedImage(image,path);
                    SetStatus("已保存 · 未复制 · "+path+" · 编辑器保持打开");
                }
                catch(Exception ex) { SetStatus("保存失败，图片仍在编辑器："+ex.Message+"；可重试或另存为。"); }
            }
        }
        private void AddSavedFileActions()
        {
            WideButton open=new WideButton { Text=UiText.T("打开已保存图片") };
            open.Click+=delegate { OpenSavedFile(false); }; wideButtons.Add(open); Controls.Add(open);
            WideButton folder=new WideButton { Text=UiText.T("打开保存位置") };
            folder.Click+=delegate { OpenSavedFile(true); }; wideButtons.Add(folder); Controls.Add(folder);
        }
        private void OpenSavedFile(bool folder)
        {
            if(string.IsNullOrEmpty(lastExplicitSave) || !File.Exists(lastExplicitSave)) { SetStatus("请先保存图片；文件也可能已移动或删除。"); return; }
            try { if(folder) Process.Start("explorer.exe","/select,\""+lastExplicitSave+"\""); else Process.Start(lastExplicitSave); }
            catch(Exception ex) { SetStatus("打开失败："+ex.Message); }
        }
    }
    internal partial class TrayApp
    {
        private readonly object recoveryGate=new object();
        private Bitmap recoveryImage;
        private void KeepRecoveryImage(Bitmap image)
        {
            lock(recoveryGate)
            {
                Bitmap next; try { next=(Bitmap)image.Clone(); } catch(Exception ex) { Diag.Log("恢复图片暂存失败："+ex.Message); return; }
                if(recoveryImage!=null) recoveryImage.Dispose(); recoveryImage=next;
            }
        }
        private void ClearRecoveryImage()
        {
            lock(recoveryGate) { if(recoveryImage!=null) recoveryImage.Dispose(); recoveryImage=null; }
        }
        internal void RecoverFailedSave()
        {
            Bitmap copy;
            lock(recoveryGate) { copy=recoveryImage==null ? null : (Bitmap)recoveryImage.Clone(); }
            if(copy==null) { ShowToast("暂无可恢复的保存失败图片"); return; }
            using(copy) RunEditor(copy,copy.Width+" × "+copy.Height,Stopwatch.StartNew());
        }
        internal void RecordSavedImage(Bitmap image,string path)
        {
            RecordRecent(image).Path=path; SaveHistory();
            if(mainForm!=null && !mainForm.IsDisposed) mainForm.RefreshRecentScreens();
        }
    }
}
