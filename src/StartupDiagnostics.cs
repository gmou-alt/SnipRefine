using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace SnapGrab
{
    internal static class StartupDiagnostics
    {
        private static int reporting;
        internal static void Report(Exception error)
        {
            if(Interlocked.CompareExchange(ref reporting,1,0)!=0) return;
            if(error==null) error=new InvalidOperationException("Unknown error");
            try
            {
                string path=null;
                // Keep diagnostic content local; exclude exception messages and file paths.
                StringBuilder report=new StringBuilder("SnipRefine "+Program.VersionText+"\r\nCLR: "+Environment.Version+"\r\n");
                for(Exception e=error;e!=null;e=e.InnerException)
                {
                    report.AppendLine(e.GetType().FullName+" · "+e.HResult.ToString("X8"));
                    StackFrame[] frames=new StackTrace(e,false).GetFrames();
                    if(frames!=null) foreach(StackFrame frame in frames) { var method=frame.GetMethod(); if(method!=null) report.AppendLine((method.DeclaringType==null ? "" : method.DeclaringType.FullName+".")+method.Name); }
                }
                try { Directory.CreateDirectory(Config.ConfigDir); string destination=Path.Combine(Config.ConfigDir,"startup-error.log"); File.WriteAllText(destination,report.ToString(),Encoding.UTF8); path=destination; } catch { }
                try { MessageBox.Show("程序启动或运行发生错误："+error.GetType().Name+"\r\n"+(path==null ? "无法写入诊断文件。" : "诊断记录："+path)+"\r\n可用完整包中的 Start.cmd 在 Windows 本机重新编译启动。","SnipRefine 启动诊断",MessageBoxButtons.OK,MessageBoxIcon.Error); } catch { }
            }
            finally { Interlocked.Exchange(ref reporting,0); }
        }
    }
}
