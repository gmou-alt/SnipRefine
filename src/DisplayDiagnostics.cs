using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
namespace SnapGrab
{
    internal static class DisplayDiagnostics
    {
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(Point point,uint flags);
        [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
        internal static int DpiAt(Point point)
        {
            try { uint x,y; if(GetDpiForMonitor(MonitorFromPoint(point,2),0,out x,out y)==0 && x>=96 && x<=384) return (int)x; }
            catch { }
            return 96;
        }
        internal static string Report()
        {
            StringBuilder report=new StringBuilder();
            report.AppendLine("SnipRefine "+Program.VersionText);
            report.AppendLine("OS: "+Environment.OSVersion.VersionString);
            report.AppendLine("CLR: "+Environment.Version);
            report.AppendLine("Display DPI values use the monitor API when available; otherwise 96 is a fallback.");
            Screen[] screens=Screen.AllScreens;
            for(int i=0;i<screens.Length;i++) {
                Screen s=screens[i];Point center=new Point(s.Bounds.Left+s.Bounds.Width/2,s.Bounds.Top+s.Bounds.Height/2);
                report.AppendLine("Display "+(i+1)+" primary="+s.Primary+" bounds="+s.Bounds+" work="+s.WorkingArea+" dpi="+DpiAt(center));
            }
            report.AppendLine("This report contains no API keys, provider addresses, user paths or screenshots.");
            return report.ToString();
        }
    }
    internal partial class MainForm
    {
        private void ShowDisplayDiagnostics()
        {
            using(ThemedDialog dialog=new ThemedDialog())
            {
                dialog.Text=UiText.T("显示环境诊断");dialog.ClientSize=new Size(660,400);dialog.StartPosition=FormStartPosition.CenterParent;
                TextBox report=new TextBox { Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Text=DisplayDiagnostics.Report() };
                report.SetBounds(16,16,628,270);dialog.ContentArea.Controls.Add(report);
                DshButton copy=new DshButton(UiText.T("复制诊断"));copy.SetBounds(16,300,180,36);dialog.ContentArea.Controls.Add(copy);
                DshLabel status=new DshLabel("");status.SetBounds(212,300,432,50);dialog.ContentArea.Controls.Add(status);
                copy.Click+=delegate { try { Clipboard.SetText(report.Text);status.Text=UiText.T("已复制诊断，不包含密钥和截图。"); } catch { status.Text=UiText.T("复制失败，请选中文本后复制。"); } };
                dialog.ShowDialog(this);
            }
        }
    }
}
