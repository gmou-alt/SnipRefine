using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class TrayApp
    {
        private static string HistoryPath { get { return Path.Combine(Config.ConfigDir,"saved-history.txt"); } }
        private int historyGeneration;
        private void SaveHistory()
        {
            if(!cfg.KeepSavedHistory) return;
            try {
                var lines=new List<string>();
                lock(recentShots) foreach(RecentShot shot in recentShots) if(shot.Path!=null) lines.Add(shot.Time.Ticks+"|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(shot.Path)));
                Directory.CreateDirectory(Config.ConfigDir);
                string temp=HistoryPath+".tmp"; File.WriteAllLines(temp,lines,Encoding.UTF8);
                if(File.Exists(HistoryPath)) File.Replace(temp,HistoryPath,null); else File.Move(temp,HistoryPath);
            } catch(Exception ex) { Diag.Log("历史记录保存失败："+ex.Message); }
        }
        private void LoadHistory()
        {
            if(!cfg.KeepSavedHistory) return;
            int generation=historyGeneration;
            // File access and image decoding must not delay the first window.
            ThreadPool.QueueUserWorkItem(delegate {
                var loaded=new List<RecentShot>();
                try { foreach(string line in File.ReadLines(HistoryPath)) {
                    if(loaded.Count>=20) break;
                    try {
                        string[] fields=line.Split(new char[] { '|' }); if(fields.Length!=2) continue;
                        string path=Encoding.UTF8.GetString(Convert.FromBase64String(fields[1])); long ticks;
                        if(!long.TryParse(fields[0],out ticks) || !File.Exists(path)) continue;
                        using(Bitmap image=new Bitmap(path)) {
                            DateTime time=new DateTime(ticks);
                            RecentShot shot=CreateRecent(image); shot.Path=path; shot.Time=time; loaded.Add(shot);
                        }
                    } catch { }
                } } catch(Exception ex) { Diag.Log("历史记录加载失败："+ex.GetType().Name); }
                if(!Post(delegate { MergeLoadedHistory(loaded,generation); }))
                    foreach(RecentShot shot in loaded) shot.Thumbnail.Dispose();
            });
        }
        private void MergeLoadedHistory(List<RecentShot> loaded,int generation)
        {
            if(exiting || generation!=historyGeneration || !cfg.KeepSavedHistory) {
                foreach(RecentShot shot in loaded) shot.Thumbnail.Dispose(); return;
            }
            lock(recentShots) {
                foreach(RecentShot shot in loaded) {
                    if(recentShots.Exists(delegate(RecentShot current) { return string.Equals(current.Path,shot.Path,StringComparison.OrdinalIgnoreCase); })) shot.Thumbnail.Dispose();
                    else recentShots.Add(shot);
                }
                recentShots.Sort(delegate(RecentShot a,RecentShot b) { return b.Time.CompareTo(a.Time); });
                while(recentShots.Count>20) { recentShots[recentShots.Count-1].Thumbnail.Dispose(); recentShots.RemoveAt(recentShots.Count-1); }
            }
            if(mainForm!=null && !mainForm.IsDisposed) mainForm.RefreshRecentScreens();
        }
        internal void ApplyHistorySetting(bool enabled)
        {
            historyGeneration++;
            cfg.KeepSavedHistory=enabled;
            if(enabled) SaveHistory(); else DeleteHistoryFile();
        }
        private void DeleteHistoryFile()
        {
            try { if(File.Exists(HistoryPath)) File.Delete(HistoryPath); }
            catch(Exception ex) { ShowToast("历史索引清理失败："+ex.Message); }
        }
    }
}
