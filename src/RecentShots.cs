using System;
using System.Collections.Generic;
using System.Drawing;
namespace SnapGrab
{
    internal sealed class RecentShot
    {
        internal Bitmap Thumbnail;
        internal string Path;
        internal DateTime Time;
        internal string Size;
    }
    internal partial class TrayApp
    {
        private readonly List<RecentShot> recentShots=new List<RecentShot>();
        private static RecentShot CreateRecent(Bitmap image)
        {
            float scale=Math.Min(1f,Math.Min(320f/image.Width,180f/image.Height));
            Bitmap thumbnail=new Bitmap(Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale)));
            using (Graphics graphics=Graphics.FromImage(thumbnail)) { graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; graphics.DrawImage(image,0,0,thumbnail.Width,thumbnail.Height); }
            RecentShot record=new RecentShot { Thumbnail=thumbnail, Time=DateTime.Now, Size=image.Width+" × "+image.Height };
            return record;
        }
        private RecentShot RecordRecent(Bitmap image)
        {
            RecentShot record=CreateRecent(image);
            lock (recentShots)
            {
                recentShots.Insert(0,record);
                while (recentShots.Count>(cfg.KeepSavedHistory ? 20 : 6)) { recentShots[recentShots.Count-1].Thumbnail.Dispose(); recentShots.RemoveAt(recentShots.Count-1); }
            }
            return record;
        }
        internal List<RecentShot> RecentCopies()
        {
            lock (recentShots)
            {
                List<RecentShot> copies=new List<RecentShot>();
                foreach (RecentShot item in recentShots) copies.Add(new RecentShot { Thumbnail=(Bitmap)item.Thumbnail.Clone(), Path=item.Path, Time=item.Time, Size=item.Size });
                return copies;
            }
        }
        internal void ClearScreenshotCache()
        {
            historyGeneration++;
            DeleteHistoryFile();
            ClearRecoveryImage();
            DisposeRecentShots();
            lock(lastShotLock) { if(lastShot!=null) { lastShot.Dispose(); lastShot=null; } }
            if(mainForm!=null && !mainForm.IsDisposed) mainForm.RefreshRecentScreens();
        }
        internal void DisposeRecentShots()
        {
            lock (recentShots) { foreach (RecentShot item in recentShots) item.Thumbnail.Dispose(); recentShots.Clear(); }
        }
    }
}
