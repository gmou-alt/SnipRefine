using System;
using System.Threading;
using System.Windows.Forms;

namespace SnapGrab
{
    public static class Program
    {
        public const string VersionText = "1.22.1";

        public static string LauncherCommand;
        public static bool BackgroundStartup;

        [STAThread]
        public static void Main()
        {
            Start();
        }

        public static void Start()
        {
            ThreadExceptionEventHandler uiErrors=delegate(object sender,ThreadExceptionEventArgs e) { StartupDiagnostics.Report(e.Exception); };
            UnhandledExceptionEventHandler fatalErrors=delegate(object sender,UnhandledExceptionEventArgs e) { StartupDiagnostics.Report(e.ExceptionObject as Exception ?? new Exception("Unhandled startup error")); };
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += uiErrors;
                AppDomain.CurrentDomain.UnhandledException += fatalErrors;
                StartCore();
            }
            catch(Exception ex) { Environment.ExitCode=1; StartupDiagnostics.Report(ex); }
            finally { Application.ThreadException-=uiErrors; AppDomain.CurrentDomain.UnhandledException-=fatalErrors; }
        }

        internal static bool BackgroundLaunch()
        {
            if(BackgroundStartup) return true;
            foreach(string argument in Environment.GetCommandLineArgs()) if(argument=="--background") return true;
            return false;
        }

        private static void StartCore()
        {
            AiClient.ConfigureTls();
            Native.TrySetDpiAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool createdNew = false;
            Mutex mutex = new Mutex(true, "SnapGrab_SingleInstance_v1", out createdNew);
            try
            {
                if (!createdNew)
                {
                    bool notified=false;
                    try { using(EventWaitHandle signal=EventWaitHandle.OpenExisting("SnipRefine_ShowWindow_v1")) { signal.Set(); notified=true; } } catch(WaitHandleCannotBeOpenedException) { }
                    if(!notified) MessageBox.Show("软件已在运行，请双击右下角托盘图标打开窗口。若是旧版仍驻留后台，请先从托盘退出旧版，再启动新版。","SnipRefine",MessageBoxButtons.OK,MessageBoxIcon.Information);
                    return;
                }
                Config c = Config.Load();
                UiText.SetLanguage(c.UiLanguage);
                Dsh.ApplyTheme(c.UiTheme != "dark",null);
                DshMenuTheme.Install();
                TrayApp trayApp=null;
                EventWaitHandle showSignal=null;
                RegisteredWaitHandle listener=null;
                try
                {
                    // Publish the wake signal before creating windows so an early
                    // second launch can be handled once the UI is ready.
                    try { showSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"SnipRefine_ShowWindow_v1"); }
                    catch(Exception ex) { Diag.Log("实例唤醒通道不可用："+ex.GetType().Name); }
                    trayApp = new TrayApp(c);
                    if(showSignal!=null) {
                        try { listener=ThreadPool.RegisterWaitForSingleObject(showSignal,delegate(object state,bool timedOut) { trayApp.Post(delegate { trayApp.ShowMainForm(); }); },null,Timeout.Infinite,false); }
                        catch(Exception ex) { showSignal.Dispose(); showSignal=null; Diag.Log("实例唤醒监听不可用："+ex.GetType().Name); }
                    }
                    DshScrollTheme.Install();
                    Application.Run(trayApp);
                }
                finally
                {
                    if(listener!=null) listener.Unregister(null);
                    if(showSignal!=null) showSignal.Dispose();
                    if(trayApp!=null) trayApp.Dispose();
                }
            }
            finally
            {
                if (createdNew)
                {
                    try
                    {
                        mutex.ReleaseMutex();
                    }
                    catch
                    {
                    }
                }
                mutex.Close();
            }
        }
    }
}
