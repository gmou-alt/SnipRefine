using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.IO;
namespace SnapGrab
{
    internal static class ExperienceTests
    {
        private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        internal static void Run()
        {
            CheckHistoryMerge();
            Config source=new Config { FullscreenOpenEditor=false, KeepSavedHistory=true };
            var lines=(List<string>)typeof(Config).GetMethod("BuildLines",Private).Invoke(source,null);
            Config restored=new Config();
            foreach(string line in lines) typeof(Config).GetMethod("ParseLine",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[] {restored,line});
            if(restored.FullscreenOpenEditor || !restored.KeepSavedHistory) throw new Exception("New capture/history settings did not survive config round trip");
            if(!new Config().FullscreenOpenEditor || new Config().KeepSavedHistory) throw new Exception("Unsafe initial capture/history defaults");
            if(!AiTroubleshooting.Explain("HTTP 401").Contains("密钥") || !AiTroubleshooting.Explain("HTTP 429").Contains("额度")) throw new Exception("Failure recovery instructions missing");
            string temp=Path.Combine(Path.GetTempPath(),"SnipRefine-save-check-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                Config settings=new Config { SaveDir=temp, Format="png" };
                using(Bitmap sourceImage=new Bitmap(24,18))
                using(EditorForm editor=new EditorForm(null,sourceImage,settings))
                {
                    typeof(EditorForm).GetMethod("SaveEditedImage",Private).Invoke(editor,new object[] { false });
                    string saved=(string)typeof(EditorForm).GetField("lastExplicitSave",Private).GetValue(editor);
                    if(!File.Exists(saved) || editor.IsDisposed || editor.DialogResult!=System.Windows.Forms.DialogResult.None) throw new Exception("Save-only must write a file without closing editor");
                    using(Bitmap exported=new Bitmap(saved)) if(exported.Width!=24 || exported.Height!=18) throw new Exception("Save-only changed screenshot dimensions");
                    string blocked=Path.Combine(temp,"blocked"); File.WriteAllText(blocked,"not a directory"); settings.SaveDir=blocked;
                    typeof(EditorForm).GetMethod("SaveEditedImage",Private).Invoke(editor,new object[] { false });
                    using(Bitmap retry=editor.RenderResult()) if(retry.Width!=24 || editor.IsDisposed) throw new Exception("Failed save lost editable image");
                }
            }
            finally { Directory.Delete(temp,true); }
            using(Bitmap image=new Bitmap(800,600))
            using(EditorForm editor=new EditorForm(null,image,new Config()))
            {
                editor.ClientSize=new Size(640,480);
                typeof(EditorForm).GetMethod("LayoutChrome",Private).Invoke(editor,null);
                var buttons=(System.Collections.IEnumerable)typeof(EditorForm).GetField("barButtons",Private).GetValue(editor);
                foreach(System.Windows.Forms.Control button in buttons)
                    if(button.Left<0 || button.Right>editor.ClientSize.Width || button.Bottom>editor.ClientSize.Height) throw new Exception("Output action falls outside small editor");
                bool review=(bool)typeof(EditorForm).GetMethod("ReviewAiOperations",Private).Invoke(editor,new object[] { new AiReply { Ops=new List<AiOp> { new AiOp { Kind="ocr",Text="example" } } } });
                if(!review) throw new Exception("Text-only OCR prompted for image modification");
            }
        }
        private static void CheckHistoryMerge()
        {
            // Isolate history merging from Windows tray/hotkey initialization.
            TrayApp app=(TrayApp)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrayApp));
            List<RecentShot> recent=new List<RecentShot>();
            typeof(TrayApp).GetField("recentShots",Private).SetValue(app,recent);
            typeof(TrayApp).GetField("cfg",Private).SetValue(app,new Config { KeepSavedHistory=true });
            DateTime now=DateTime.Now;
            recent.Add(new RecentShot { Path="new.png",Time=now,Thumbnail=new Bitmap(2,2) });
            List<RecentShot> loaded=new List<RecentShot>();
            loaded.Add(new RecentShot { Path="NEW.PNG",Time=now.AddDays(-1),Thumbnail=new Bitmap(2,2) });
            for(int i=0;i<22;i++) loaded.Add(new RecentShot { Path=i+".png",Time=now.AddMinutes(-i-1),Thumbnail=new Bitmap(2,2) });
            MethodInfo merge=typeof(TrayApp).GetMethod("MergeLoadedHistory",Private);
            try {
                merge.Invoke(app,new object[] { loaded,0 });
                if(recent.Count!=20 || recent[0].Path!="new.png" || recent[19].Path!="18.png") throw new Exception("History merge lost newest screenshot, duplicated paths or exceeded budget");
                typeof(TrayApp).GetField("historyGeneration",Private).SetValue(app,1);
                loaded=new List<RecentShot> { new RecentShot { Path="stale.png",Time=now.AddHours(1),Thumbnail=new Bitmap(2,2) } };
                merge.Invoke(app,new object[] { loaded,0 });
                if(recent.Count!=20 || recent[0].Path!="new.png") throw new Exception("Cleared history was restored by a stale background load");
            }
            finally { foreach(RecentShot shot in recent) shot.Thumbnail.Dispose(); recent.Clear(); }
        }
    }
}
