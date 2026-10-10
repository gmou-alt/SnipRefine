using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class MainForm
    {
        private Panel recentHost;
        private void BuildRecentPage()
        {
            Panel page=NewPage(); PageHeader(page,"最近截图","默认保留本次 6 张；可选跨重启记录 20 张已保存图片，不额外保存图片");
            DshButton refresh=new DshButton(UiText.T("刷新")); refresh.SetBounds(32,100,100,34); refresh.Click+=delegate { RefreshRecentScreens(); }; page.Controls.Add(refresh);
            DshButton clear=new DshButton(UiText.T("清空记录")); clear.SetBounds(140,100,110,34); clear.Click+=delegate { app.ClearScreenshotCache(); RefreshRecentScreens(); }; page.Controls.Add(clear);
            DshButton recover=new DshButton(UiText.T("恢复保存失败图片")); recover.SetBounds(268,100,220,34); recover.Click+=delegate { app.RecoverFailedSave(); }; page.Controls.Add(recover);
            var remember=new DshToggle(UiText.T("跨重启保留已保存图片路径（默认关闭）")); recentRemember=remember;remember.Checked=cfg.KeepSavedHistory; remember.SetBounds(32,146,588,32); page.Controls.Add(remember);
            remember.CheckedChanged+=delegate {SetStatus("历史记录选项尚未保存，点击保存本页设置生效。");};
            recentHost=new Panel { AutoScroll=true, BackColor=Dsh.Canvas }; recentHost.SetBounds(32,190,588,568); page.Controls.Add(recentHost);
        }
        private void DisposeRecentUi()
        {
            if (recentHost==null) return;
            foreach (Control card in recentHost.Controls) foreach (Control child in card.Controls)
            { PictureBox picture=child as PictureBox; if (picture!=null && picture.Image!=null) { picture.Image.Dispose(); picture.Image=null; } }
            while (recentHost.Controls.Count>0) recentHost.Controls[0].Dispose();
        }
        internal void RefreshRecentScreens()
        {
            if (recentHost==null || IsDisposed) return; DisposeRecentUi();
            int row=0;
            foreach (RecentShot item in app.RecentCopies())
            {
                DshGlassCard card=new DshGlassCard(); card.SetBounds(0,row++*154,560,146); recentHost.Controls.Add(card);
                PictureBox picture=new PictureBox { Image=item.Thumbnail, SizeMode=PictureBoxSizeMode.Zoom, BackColor=Color.Transparent }; picture.SetBounds(12,12,220,122); card.Controls.Add(picture);
                DshLabel label=new DshLabel(item.Time.ToString("HH:mm:ss")+" · "+item.Size); label.SetBounds(248,16,300,28); card.Controls.Add(label);
                DshLabel detail=new DshLabel(item.Path==null ? "未保存原文件；仅保留缩略图" : Path.GetFileName(item.Path)); detail.Small=true; detail.SetBounds(248,48,300,28); card.Controls.Add(detail);
                string path=item.Path;
                DshButton folder=new DshButton(UiText.T("打开位置")); folder.Enabled=path!=null; folder.SetBounds(410,90,126,34); card.Controls.Add(folder);
                folder.Click+=delegate { try { if(File.Exists(path)) Process.Start("explorer.exe", "/select,\""+path+"\""); else SetStatus("文件已移动或删除。"); } catch(Exception ex) { SetStatus("打开失败："+ex.Message); } };
                DshButton open=new DshButton(UiText.T("打开原文件")); open.Enabled=path!=null; open.SetBounds(248,90,150,34); card.Controls.Add(open);
                open.Click+=delegate { try { if (File.Exists(path)) Process.Start(path); else SetStatus("文件已移动或删除。"); } catch (Exception ex) { SetStatus("打开失败："+ex.Message); } };
            }
        }
    }
}
