using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace SnapGrab
{
    internal static class ResourceStress
    {
        [DllImport("user32.dll")] private static extern uint GetGuiResources(IntPtr process,uint flags);
        private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static void Call(EditorForm editor,string name,params object[] args) { typeof(EditorForm).GetMethod(name,Private).Invoke(editor,args); }
        private static void Cycle()
        {
            using (EditorForm editor=new EditorForm(null,new Bitmap(1920,1080),new Config()))
            {
                Call(editor,"AddShapeTracked",new Shape { Kind=Shape.K.Rect, Rect=new RectangleF(100,100,300,200), Color=Color.Red, Width=3 });
                Call(editor,"CommitCrop",new RectangleF(50,50,1000,650));
                Call(editor,"DoUndo"); Call(editor,"DoRedo");
                using (Bitmap result=editor.RenderResult()) { if (result.Width!=1000) throw new InvalidOperationException("Stress crop dimensions changed"); }
            }
        }
        private static string Measure(string stage)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            using (Process process=Process.GetCurrentProcess())
            { process.Refresh(); return stage+","+process.PrivateMemorySize64+","+process.WorkingSet64+","+GetGuiResources(process.Handle,0)+","+GetGuiResources(process.Handle,1); }
        }
        internal static int Run()
        {
            try
            {
                for (int i=0;i<5;i++) Cycle();
                string report="stage,private_bytes,working_set_bytes,gdi_handles,user_handles\r\n"+Measure("warmup")+"\r\n";
                for (int batch=1;batch<=4;batch++) { for (int i=0;i<10;i++) Cycle(); report+=Measure("batch_"+batch)+"\r\n"; }
                string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SnipRefine-stress.csv"); File.WriteAllText(path,report);
                Console.WriteLine(report); Console.WriteLine("Diagnostic report: "+path); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
