using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SnapGrab
{
    internal partial class TrayApp : ApplicationContext
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private Config cfg;

        private Bitmap lastShot;

        private readonly object lastShotLock = new object();

        private NotifyIcon tray;

        private ContextMenuStrip menu;

        private HotkeyWindow hot;

        private ToolStripMenuItem miHotkey;

        private ToolStripMenuItem miAutostart;


        private MainForm mainForm;

        private FloatingIcon floating;

        private Form marshaller;

        private OverlayForm overlay;

        private bool capturing;

        private bool exiting;

        private bool syncingAutostart;


        private bool warnedHidden;

        private bool hotkeyRegistered;

        private int pendingSaves;
        private readonly object saveGate = new object();
        private readonly Stopwatch trimClock = Stopwatch.StartNew();
        private long lastTrim = -30000;

        private string hotkeyText = "";

        private Icon appIcon;

        internal Config Settings
        {
            get
            {
                return cfg;
            }
        }

        internal Icon AppIcon
        {
            get
            {
                return appIcon;
            }
        }

        internal bool HotkeyRegistered
        {
            get
            {
                return hotkeyRegistered;
            }
        }

        internal string HotkeyText
        {
            get
            {
                return hotkeyText;
            }
        }

        internal void KeepLastShot(Image src)
        {
            if (src == null)
            {
                return;
            }
            Bitmap bitmap = null;
            try
            {
                int width = src.Width;
                int height = src.Height;
                int num = Math.Max(width, height);
                int num2 = 1600;
                float num3 = ((num > num2) ? ((float)num2 / (float)num) : 1f);
                int width2 = Math.Max(1, (int)Math.Round((float)width * num3));
                int height2 = Math.Max(1, (int)Math.Round((float)height * num3));
                bitmap = new Bitmap(width2, height2, PixelFormat.Format24bppRgb);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(src, 0, 0, width2, height2);
                }
            }
            catch
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                    bitmap = null;
                }
            }
            lock (lastShotLock)
            {
                Bitmap bitmap2 = lastShot;
                lastShot = bitmap;
                if (bitmap2 != null)
                {
                    bitmap2.Dispose();
                }
            }
        }

        internal Bitmap GetLastShotCopy()
        {
            lock (lastShotLock)
            {
                if (lastShot == null)
                {
                    return null;
                }
                try
                {
                    return lastShot.Clone(new Rectangle(0, 0, lastShot.Width, lastShot.Height), lastShot.PixelFormat);
                }
                catch
                {
                    return null;
                }
            }
        }

        public TrayApp(Config c)
        {
            try
            {
            cfg = c;
            Diag.Enabled = cfg.DebugTiming;
            Diag.Reset();
            Diag.Log("--- SnipRefine " + Program.VersionText + " 启动 ---");
            ApplyProcessPriority();
            Native.BoostUiThread();
            CreateMarshalWindow();
            hot = new HotkeyWindow();
            hot.Pressed += OnHotkeyPressed;
            quickHot = new HotkeyWindow(); fullHot = new HotkeyWindow();
            quickHot.Pressed += delegate { if (!RecordingCaptureKey()) CaptureNow(CaptureMode.Quick); };
            fullHot.Pressed += delegate { if (!RecordingCaptureKey()) CaptureNow(CaptureMode.Fullscreen); };
            appIcon = CreateTrayIcon();
            menu = new DshContextMenu();
            Font font = Dsh.Body;
            if (font == null)
            {
                font = SystemFonts.DefaultFont;
            }
            menu.Font = font;
            ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(UiText.T("打开主界面(&O)"));
            toolStripMenuItem.Font = new Font(font, FontStyle.Bold);
            toolStripMenuItem.Click += delegate
            {
                ShowMainForm();
            };
            menu.Items.Add(toolStripMenuItem);
            ToolStripMenuItem toolStripMenuItem2 = new ToolStripMenuItem(UiText.T("框选截图 · 编辑"));
            toolStripMenuItem2.Click += delegate
            {
                CaptureNow();
            };
            menu.Items.Add(toolStripMenuItem2);
            miHotkey = new ToolStripMenuItem("");
            miHotkey.Enabled = false;
            menu.Items.Add(miHotkey);
            var quickMenu = new ToolStripMenuItem(UiText.T("快速截图 · 直接保存")); quickMenu.Click += delegate { CaptureNow(CaptureMode.Quick); }; menu.Items.Add(quickMenu);
            var fullMenu = new ToolStripMenuItem(UiText.T("全屏截图")); fullMenu.Click += delegate { CaptureNow(CaptureMode.Fullscreen); }; menu.Items.Add(fullMenu);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem toolStripMenuItem3 = new ToolStripMenuItem(UiText.T("打开保存文件夹"));
            toolStripMenuItem3.Click += delegate
            {
                OpenSaveDir();
            };
            menu.Items.Add(toolStripMenuItem3);
            miAutostart = new ToolStripMenuItem(UiText.T("开机自动启动"));
            miAutostart.CheckOnClick = true;
            miAutostart.CheckedChanged += delegate
            {
                if (!syncingAutostart)
                {
                    ApplyAutostart(miAutostart.Checked, false);
                }
            };
            menu.Items.Add(miAutostart);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem toolStripMenuItem4 = new ToolStripMenuItem(UiText.T("退出"));
            toolStripMenuItem4.Click += delegate
            {
                ExitApp();
            };
            menu.Items.Add(toolStripMenuItem4);
            tray = new NotifyIcon();
            tray.Icon = appIcon;
            tray.Text = "截改 AI SnipRefine 截图";
            tray.ContextMenuStrip = menu;
            tray.MouseUp += OnTrayMouseUp;
            tray.Visible = true;
            ApplyHotkeyStartup();
            RegisterExtraCaptureHotkeys();
            // 普通启动只读取自启状态，不注册或改写当前路径。
            SyncAutostartCheck();
            ApplyFloatingIconSetting();
            try
            {
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            }
            catch (Exception ex)
            {
                Diag.Log("订阅显示器变化事件失败: " + ex.Message);
            }
            WarmUp();
            if (!Program.BackgroundLaunch() || cfg.ShowWindowOnStart || cfg.CreatedNew)
            {
                ShowMainForm();
            }
            else
            {
                NotifyStartupBalloon();
            }
            TrimWorkingSet();
            LoadHistory();
            }
            catch { try { Dispose(); } catch { } throw; }
        }

        private void ApplyProcessPriority()
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    process.PriorityClass = Config.ParsePriority(cfg.ProcessPriority);
                }
            }
            catch
            {
            }
        }

        private void CreateMarshalWindow()
        {
            try
            {
                marshaller = new Form();
                marshaller.ShowInTaskbar = false;
                marshaller.FormBorderStyle = FormBorderStyle.None;
                marshaller.StartPosition = FormStartPosition.Manual;
                marshaller.Size = new Size(1, 1);
                marshaller.Location = new Point(-32000, -32000);
                marshaller.Show();
            }
            catch
            {
            }
        }

        private void WarmUp()
        {
            try
            {
                overlay = new OverlayForm();
                using (Bitmap bitmap = new Bitmap(8, 8, PixelFormat.Format32bppArgb))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        bitmap.Save(stream, ImageFormat.Png);
                    }
                }
                Diag.Log("预热完成（遮罩窗口 + 编码器）");
            }
            catch
            {
            }
        }

        internal bool Post(Action action)
        {
            if (action == null)
            {
                return false;
            }
            try
            {
                if (marshaller != null && marshaller.IsHandleCreated)
                {
                    marshaller.BeginInvoke(action);
                    return true;
                }
                Diag.Log("Post 失败：UI 消息通道不可用，回调已丢弃");
                return false;
            }
            catch (Exception ex)
            {
                Diag.Log("Post 失败: " + ex.Message);
                return false;
            }
        }

        internal void TrimWorkingSet()
        {
            if (!cfg.TrimWorkingSet || Volatile.Read(ref pendingSaves) > 0 ||
                trimClock.ElapsedMilliseconds - lastTrim < 30000) return;
            lastTrim = trimClock.ElapsedMilliseconds;
            Native.TrimWorkingSet();
            Diag.Log("已整理工作集；不强制触发完整 GC");
        }

        internal bool ApplyHotkeyText(string text, out string error)
        {
            error = null;
            uint mods;
            uint vk;
            string display;
            if (!Config.TryParseHotkey(text, out mods, out vk, out display))
            {
                error = "快捷键无效：必须包含 Ctrl / Shift / Alt 修饰键，且主键要受支持。";
                return false;
            }
            if (!hot.Register(mods, vk))
            {
                SyncHotkeyState(display);
                error = "快捷键 " + display + " 已被其它程序占用，请换一个组合。";
                return false;
            }
            hotkeyRegistered = true;
            hotkeyText = display;
            cfg.Hotkey = display;
            miHotkey.Text = "当前热键：" + display;
            tray.Text = "截改 AI SnipRefine 截图 · " + display;
            return true;
        }

        private void SyncHotkeyState(string attemptedDisplay)
        {
            uint mods;
            uint vk;
            hot.Current(out mods, out vk);
            if (hot.Registered && (mods != 0 || vk != 0))
            {
                Config.LabelForVk(vk);
                hotkeyRegistered = true;
                miHotkey.Text = "当前热键：" + hotkeyText;
                tray.Text = "截改 AI SnipRefine 截图 · " + hotkeyText;
                Diag.Log("热键状态：换 " + attemptedDisplay + " 失败，仍使用 " + hotkeyText);
            }
            else
            {
                hotkeyRegistered = false;
                miHotkey.Text = "当前热键：未生效";
                tray.Text = "截改 AI SnipRefine 截图（热键未生效）";
                Diag.Log("热键状态：换 " + attemptedDisplay + " 失败，且无可用热键");
            }
        }

        internal bool ApplyAutostartSetting(bool enable)
        {
            // 保存其他设置时，未改变自启开关就不改写已有安装路径。
            if (enable == cfg.StartWithWindows)
            {
                SyncAutostartCheck();
                return true;
            }
            return ApplyAutostart(enable, false, false);
        }

        internal void ApplyProcessPrioritySetting()
        {
            ApplyProcessPriority();
        }

        internal void CaptureNow() { CaptureNow(CaptureMode.Adjustable); }

        internal void CaptureNow(CaptureMode mode)
        {
            if (capturing)
            {
                return;
            }
            capturing = true;
            Stopwatch stopwatch = Stopwatch.StartNew();
            Diag.Log("收到截图请求");
            Bitmap bitmap = null;
            try
            {
                Screen[] displays=Screen.AllScreens;
                Rectangle[] displayBounds=new Rectangle[displays.Length];
                for(int i=0;i<displays.Length;i++) displayBounds[i]=displays[i].Bounds;
                Rectangle virtualScreen = DesktopGeometry.Bounds(displayBounds);
                bitmap = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format32bppArgb);
                try
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(virtualScreen.X, virtualScreen.Y, 0, 0, virtualScreen.Size, CopyPixelOperation.SourceCopy);
                    }
                }
                catch (Exception ex)
                {
                    Balloon("截取屏幕失败：" + ex.Message, ToolTipIcon.Error);
                    return;
                }
                Diag.Log("屏幕已抓取 (" + stopwatch.ElapsedMilliseconds + " ms)");
                if (overlay == null || overlay.IsDisposed)
                {
                    overlay = new OverlayForm();
                }
                if (mode == CaptureMode.Fullscreen)
                { Finish(bitmap, new Rectangle(Point.Empty, virtualScreen.Size), stopwatch, !cfg.FullscreenOpenEditor); return; }
                overlay.Adjustable = mode == CaptureMode.Adjustable;
                overlay.Prepare(bitmap, virtualScreen, stopwatch);
                overlay.ShowDialog();
                Diag.Log("遮罩交互结束 (" + stopwatch.ElapsedMilliseconds + " ms)");
                if (overlay.Accepted && overlay.Selection.Width > 0 && overlay.Selection.Height > 0)
                {
                    Finish(bitmap, overlay.Selection, stopwatch, mode == CaptureMode.Quick);
                }
            }
            catch (Exception ex2)
            {
                Diag.Log("截图异常: " + ex2.Message);
            }
            finally
            {
                try
                {
                    if (overlay != null && !overlay.IsDisposed)
                    {
                        overlay.ClearSnapshot();
                    }
                }
                catch
                {
                }
                if (bitmap != null)
                {
                    try
                    {
                        bitmap.Dispose();
                    }
                    catch
                    {
                    }
                }
                capturing = false;
            }
        }

        internal void ReleaseMainForm()
        {
            MainForm f = mainForm;
            mainForm = null;
            if (f == null)
            {
                return;
            }
            Post(delegate
            {
                try
                {
                    if (!f.IsDisposed)
                    {
                        f.Dispose();
                    }
                }
                catch
                {
                }
                TrimWorkingSet();
            });
        }

        internal void ShowMainForm()
        {
            if (!exiting)
            {
                if (mainForm == null || mainForm.IsDisposed)
                {
                    mainForm = new MainForm(this);
                }
                if (!mainForm.Visible)
                {
                    mainForm.Show();
                }
                if (mainForm.WindowState == FormWindowState.Minimized)
                {
                    mainForm.WindowState = FormWindowState.Normal;
                }
                mainForm.Activate();
                mainForm.BringToFront();
            }
        }

        internal void ShowChatWindow()
        {
            ToggleChatPanel(false);
        }

        internal void ToggleChatPanel()
        {
            ToggleChatPanel(true);
        }

        private void ToggleChatPanel(bool allowToggle)
        {
            if (exiting)
            {
                return;
            }
            AiChat.Init();
            ShowMainForm();
            if (mainForm == null || mainForm.IsDisposed)
            {
                return;
            }
            bool chatPanel = !allowToggle || !mainForm.ChatPanelOpen;
            try
            {
                mainForm.SetChatPanel(chatPanel);
            }
            catch (Exception ex)
            {
                Diag.Log("切换 AI 侧边栏失败: " + ex.Message);
            }
        }

        internal void NotifyHiddenToTray()
        {
            if (!warnedHidden)
            {
                warnedHidden = true;
                Balloon("SnipRefine 仍在后台运行。\n按 " + ((hotkeyText.Length > 0) ? hotkeyText : cfg.Hotkey) + " 随时截图；双击右下角托盘图标或点击右下角悬浮图标可重新打开本窗口。", ToolTipIcon.Info);
            }
        }

        internal void ApplyFloatingIconSetting()
        {
            try
            {
                cfg.ShowFloatingIcon = false;
                if (floating != null && !floating.IsDisposed)
                {
                    floating.Close();
                    floating.Dispose();
                    floating = null;
                }
            }
            catch
            {
            }
        }

        private Point ResolveFloatingPos()
        {
            Point p;
            if (TryParsePoint(cfg.FloatingIconPos, out p) && IsIconPosUsable(p))
            {
                return ClampIconToScreen(p);
            }
            return FloatingIcon.DefaultLocation();
        }

        private static bool IsIconPosUsable(Point p)
        {
            Rectangle a = new Rectangle(p.X, p.Y, 52, 52);
            Screen[] allScreens = Screen.AllScreens;
            for (int i = 0; i < allScreens.Length; i++)
            {
                Rectangle workingArea = allScreens[i].WorkingArea;
                Rectangle rectangle = Rectangle.Intersect(a, workingArea);
                if (rectangle.Width * rectangle.Height >= 1352)
                {
                    return true;
                }
            }
            return false;
        }

        private static Point ClampIconToScreen(Point p)
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int num = p.X;
            int num2 = p.Y;
            if (num + 52 > workingArea.Right)
            {
                num = workingArea.Right - 52;
            }
            if (num2 + 52 > workingArea.Bottom)
            {
                num2 = workingArea.Bottom - 52;
            }
            if (num < workingArea.Left)
            {
                num = workingArea.Left;
            }
            if (num2 < workingArea.Top)
            {
                num2 = workingArea.Top;
            }
            return new Point(num, num2);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            Post(delegate
            {
                try
                {
                    if (floating != null && !floating.IsDisposed)
                    {
                        Point location = floating.Location;
                        if (!IsIconPosUsable(location))
                        {
                            Point point = ClampIconToScreen(location);
                            floating.Location = point;
                            SaveFloatingIconPos(point);
                            Diag.Log("显示器布局变化：悬浮图标已移回 " + point);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Diag.Log("处理显示器变化失败: " + ex.Message);
                }
            });
        }

        internal void SaveFloatingIconPos(Point p)
        {
            cfg.FloatingIconPos = p.X + "," + p.Y;
            try {Config saved=Config.ReadSavedOr(cfg);saved.FloatingIconPos=cfg.FloatingIconPos;saved.Save();}
            catch(Exception ex){Diag.Log("悬浮图标位置未保存："+ex.Message);}
        }

        private static bool TryParsePoint(string s, out Point p)
        {
            p = Point.Empty;
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }
            string[] array = s.Split(new char[] { ',' });
            if (array.Length != 2)
            {
                return false;
            }
            int result;
            if (!int.TryParse(array[0].Trim(), out result))
            {
                return false;
            }
            int result2;
            if (!int.TryParse(array[1].Trim(), out result2))
            {
                return false;
            }
            p = new Point(result, result2);
            return true;
        }

        internal void ShowTrayMenu(Point screenPos)
        {
            SyncAutostartCheck();
            if (menu != null)
            {
                menu.Show(screenPos);
            }
        }

        internal void ShowToast(string text)
        {
            try
            {
                Point anchor;
                if (floating != null && !floating.IsDisposed)
                {
                    anchor = new Point(floating.Left + floating.Width, floating.Top);
                }
                else
                {
                    Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                    anchor = new Point(workingArea.Right - 20, workingArea.Bottom - 20);
                }
                ToastForm toastForm = new ToastForm(text, anchor, 2600);
                toastForm.Show();
            }
            catch
            {
            }
        }

        internal void RequestExit()
        {
            if (!exiting)
            {
                exiting = true;
                TryExitThread();
            }
        }

        internal void ExitApp()
        {
            if(mainForm!=null&&!mainForm.IsDisposed&&!mainForm.ConfirmDiscardSettingsDrafts())return;
            exiting = true;
            if (tray != null)
            {
                tray.Visible = false;
            }
            if (mainForm != null && !mainForm.IsDisposed)
            {
                mainForm.AllowExit = true;
                mainForm.Close();
            }
            TryExitThread();
        }

        private void TryExitThread()
        {
            try
            {
                ExitThread();
            }
            catch
            {
            }
        }

        private void ApplyHotkeyStartup()
        {
            string error;
            if (!ApplyHotkeyText(cfg.Hotkey, out error))
            {
                hotkeyRegistered = false;
                hotkeyText = cfg.Hotkey;
                miHotkey.Text = "热键未生效：" + cfg.Hotkey;
                tray.Text = "截改 AI SnipRefine 截图（热键未生效）";
                Balloon("快捷键 " + cfg.Hotkey + " 无法注册。\n" + error + "\n可在主界面中换一个快捷键；现在仍可从托盘菜单截图。", ToolTipIcon.Warning);
            }
        }

        private bool RecordingCaptureKey() { return mainForm != null && !mainForm.IsDisposed && mainForm.RecordingCaptureHotkey; }

        private void OnHotkeyPressed(object sender, EventArgs e)
        {
            if (RecordingCaptureKey()) return;
            CaptureNow();
        }

        private void Balloon(string text, ToolTipIcon icon)
        {
            if (tray != null)
            {
                tray.ShowBalloonTip(4000, "SnipRefine", text, icon);
            }
        }

        private void NotifyStartupBalloon()
        {
            Balloon("SnipRefine 已在托盘运行。\n按 " + ((hotkeyText.Length > 0) ? hotkeyText : cfg.Hotkey) + " 即可框选截图。", ToolTipIcon.Info);
        }

        private void OnTrayMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainForm();
            }
        }

        private void Finish(Bitmap snap, Rectangle sel, Stopwatch sw, bool direct = false)
        {
            Bitmap bitmap;
            try
            {
                bitmap = snap.Clone(sel, PixelFormat.Format32bppArgb);
            }
            catch (Exception ex)
            {
                Balloon("裁剪选区失败：" + ex.Message, ToolTipIcon.Warning);
                return;
            }
            Diag.Log("已裁剪 " + sel.Width + "x" + sel.Height + " (" + sw.ElapsedMilliseconds + " ms)");
            string size = sel.Width + " × " + sel.Height;
            if (direct) { RecentShot recent = RecordRecent(bitmap); KeepLastShot(bitmap);
                bool copied = cfg.CopyToClipboard && CopyToClipboard(bitmap);
                ShowToast("正在保存截图 " + size + (copied ? "，已复制" : cfg.CopyToClipboard ? "，复制失败" : ""));
                StartBackgroundSave(bitmap, size, sw, recent); return; }
            if (!RunEditor(bitmap, size, sw))
            {
                bitmap.Dispose();
                ShowToast("已放弃这张截图");
            }
        }

        private void Deliver(Bitmap crop, string size, Stopwatch sw, bool copyToClipboard)
        {
            KeepLastShot(crop);
            bool flag = false;
            if (copyToClipboard && cfg.CopyToClipboard)
            {
                flag = CopyToClipboard(crop);
            }
            Diag.Log("剪贴板就绪 (" + sw.ElapsedMilliseconds + " ms) copied=" + flag + " 自动复制开关=" + cfg.CopyToClipboard);
            string text = (flag ? "，已复制到剪贴板" : (cfg.CopyToClipboard ? "（复制到剪贴板失败）" : "（设置里关闭了自动复制）"));
            ShowToast("已截图 " + size + text);
            StartBackgroundSave(crop, size, sw);
        }

        internal bool EditChatImage(Bitmap image, string prompt)
        {
            using (image) return RunEditor(image, image.Width + " × " + image.Height, Stopwatch.StartNew(), prompt);
        }

        private bool RunEditor(Bitmap image, string size, Stopwatch sw, string initialPrompt = null)
        {
            EditorForm editorForm = null;
            try
            {
                editorForm = new EditorForm(this, image, cfg);
                if (initialPrompt!=null) editorForm.Shown += delegate { editorForm.BeginChatImageEdit(initialPrompt); };
                editorForm.ClientSize = EditorForm.SuggestedWindowSize(image.Size);
                editorForm.Location = CenteredOnScreen(editorForm.ClientSize);
                Form form = FindOwnWindow();
                if (form != null && form.Visible)
                {
                    editorForm.ShowDialog(form);
                }
                else
                {
                    editorForm.ShowDialog();
                }
                if (editorForm.ExitMode == EditorForm.EditorExit.Cancel)
                {
                    return false;
                }
                bool flag = editorForm.ExitMode == EditorForm.EditorExit.Save;
                Bitmap bitmap = editorForm.RenderResult();
                size = bitmap.Width + " × " + bitmap.Height;
                KeepLastShot(bitmap);
                RecentShot recent = RecordRecent(bitmap);
                bool flag2 = CopyToClipboard(bitmap);
                if(!flag2) KeepRecoveryImage(bitmap);
                Diag.Log("剪贴板就绪 (" + sw.ElapsedMilliseconds + " ms) copied=" + flag2);
                if (flag)
                {
                    ShowToast("正在保存 " + size + (flag2 ? "，已复制到剪贴板" : "，但复制到剪贴板失败"));
                    StartBackgroundSave(bitmap, size, sw, recent);
                }
                else
                {
                    bitmap.Dispose();
                    ShowToast(flag2 ? ("已复制到剪贴板 " + size) : "复制失败，完整图片已暂存；可在最近截图页恢复后重试");
                    if (Volatile.Read(ref pendingSaves) <= 0)
                    {
                        TrimWorkingSet();
                    }
                }
                Diag.Log("编辑器处理完成 (" + sw.ElapsedMilliseconds + " ms)");
                return true;
            }
            catch (Exception ex)
            {
                Diag.Log("编辑器异常: " + ex.Message);
                try
                {
                    Balloon("打开编辑器失败：" + ex.Message, ToolTipIcon.Warning);
                }
                catch
                {
                }
                return false;
            }
            finally
            {
                if (editorForm != null)
                {
                    try
                    {
                        editorForm.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static Point CenteredOnScreen(Size win)
        {
            Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
            return new Point(workingArea.X + (workingArea.Width - win.Width) / 2, workingArea.Y + (workingArea.Height - win.Height) / 2);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder text, int count);

        private static Form FindOwnWindow()
        {
            IntPtr found = IntPtr.Zero;
            string title = "截改 AI SnipRefine 截图工具  v" + Program.VersionText;
            try
            {
                EnumWindows(delegate(IntPtr h, IntPtr l)
                {
                    StringBuilder stringBuilder = new StringBuilder(256);
                    GetWindowTextW(h, stringBuilder, 256);
                    if (stringBuilder.ToString() == title)
                    {
                        found = h;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }
            if (!(found != IntPtr.Zero))
            {
                return null;
            }
            return (Form)Control.FromHandle(found);
        }

        private void StartBackgroundSave(Bitmap crop, string size, Stopwatch sw, RecentShot recent = null)
        {
            Config saveConfig = new Config();
            saveConfig.SaveDir = cfg.SaveDir;
            saveConfig.Format = cfg.Format;
            saveConfig.Quality = cfg.Quality;
            saveConfig.DateSubfolder = cfg.DateSubfolder;
            saveConfig.FilenamePattern = cfg.FilenamePattern;
            Interlocked.Increment(ref pendingSaves);
            Thread thread = new Thread((ThreadStart)delegate
            {
                string text = null;
                string text2 = null;
                string text3 = null;
                string text4 = text3;
                bool asJpg = saveConfig.Format == "jpg" || saveConfig.Format == "jpeg";
                try
                {
                    text3 = saveConfig.EffectiveSaveDir();
                    text4 = text3;
                    lock (saveGate)
                    {
                    try
                    {
                        text = SaveImageFile(crop, text3, asJpg, saveConfig.Quality, saveConfig);
                    }
                    catch (Exception ex)
                    {
                        text2 = ex.Message;
                        text4 = saveConfig.ResolveSaveDir();
                        if (string.Equals(text4, text3, StringComparison.OrdinalIgnoreCase))
                        {
                            text = null;
                        }
                        else
                        {
                            text = SaveImageFile(crop, text4, asJpg, saveConfig.Quality, saveConfig);
                            text2 = null;
                        }
                    }
                    }
                }
                catch (Exception ex2)
                {
                    text = null;
                    text2 = ex2.Message;
                }
                finally
                {
                    if (text == null) KeepRecoveryImage(crop);
                    crop.Dispose();
                    Interlocked.Decrement(ref pendingSaves);
                }
                Diag.Log("已落盘 (" + sw.ElapsedMilliseconds + " ms) " + ((text != null) ? text : ("失败：" + text2)));
                string p = text;
                string err = text2;
                string used = text4;
                string wanted = text3;
                Post(delegate
                {
                    if (recent != null) { recent.Path = p; if(p!=null) SaveHistory(); }
                    OnSaveFinished(p, err, used, wanted, size);
                    if (mainForm != null && !mainForm.IsDisposed) mainForm.RefreshRecentScreens();
                    if (Volatile.Read(ref pendingSaves) <= 0)
                    {
                        TrimWorkingSet();
                    }
                });
            });
            thread.IsBackground = true;
            try
            {
                thread.Start();
            }
            catch (Exception ex)
            {
                KeepRecoveryImage(crop);
                crop.Dispose();
                Interlocked.Decrement(ref pendingSaves);
                OnSaveFinished(null, ex.Message, null, null, size);
            }
        }

        private void OnSaveFinished(string path, string error, string usedDir, string wantedDir, string size)
        {
            bool flag = !string.Equals(usedDir, wantedDir, StringComparison.OrdinalIgnoreCase);
            if (path != null)
            {
                string text = (flag ? "\n（原设定目录不可写，已自动改用此目录）" : "");
                ShowToast("已保存 " + size);
                Balloon("已截图 " + size + "，保存到\n" + path + text, ToolTipIcon.Info);
                if (mainForm != null && !mainForm.IsDisposed && mainForm.Visible)
                {
                    mainForm.RefreshStatus("最近截图：" + size + " → " + path);
                }
            }
            else
            {
                ShowToast("截图 " + size + "，保存失败");
                Balloon("保存失败：" + error + "\n完整图片已暂存，可在最近截图页选择「恢复保存失败图片」重试；退出前请保存。", ToolTipIcon.Warning);
                if (mainForm != null && !mainForm.IsDisposed && mainForm.Visible)
                {
                    mainForm.RefreshStatus("最近截图：" + size + "，保存失败：" + error);
                }
            }
        }

        internal static string SaveImageFile(Image img, string dir, bool asJpg, int quality, Config cfg)
        {
            if (cfg.DateSubfolder)
            {
                dir = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd"));
            }
            Directory.CreateDirectory(dir);
            string text = (asJpg ? ".jpg" : ".png");
            string text2 = Config.ExpandPattern(cfg.FilenamePattern, DateTime.Now, img.Width, img.Height);
            string text3 = Path.Combine(dir, text2 + text);
            int num = 1;
            while (File.Exists(text3) && num < 10000)
            {
                text3 = Path.Combine(dir, text2 + "_" + num + text);
                num++;
            }
            if (asJpg)
            {
                SaveJpeg(img, text3, quality);
            }
            else
            {
                img.Save(text3, ImageFormat.Png);
            }
            return text3;
        }

        private static void SaveJpeg(Image img, string path, int quality)
        {
            ImageCodecInfo imageCodecInfo = null;
            ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < imageEncoders.Length; i++)
            {
                if (imageEncoders[i].FormatID == ImageFormat.Jpeg.Guid)
                {
                    imageCodecInfo = imageEncoders[i];
                    break;
                }
            }
            if (imageCodecInfo == null)
            {
                img.Save(path, ImageFormat.Jpeg);
                return;
            }
            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            {
                using (EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality))
                {
                    encoderParameters.Param[0] = encoderParameter;
                    img.Save(path, imageCodecInfo, encoderParameters);
                }
            }
        }

        internal static bool CopyToClipboard(Image img)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Diag.Log("剪贴板写入被跳过：当前线程不是 STA");
                return false;
            }
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    DataObject dataObject = new DataObject();
                    dataObject.SetImage(img);
                    Clipboard.SetDataObject(dataObject, true);
                    return true;
                }
                catch (Exception ex)
                {
                    Diag.Log("剪贴板写入失败(第 " + (i + 1) + " 次): " + ex.GetType().Name + " " + ex.Message);
                    if (!IsClipboardBusy(ex))
                    {
                        return false;
                    }
                    Application.DoEvents();
                    Thread.Sleep(40);
                }
            }
            return false;
        }

        private static bool IsClipboardBusy(Exception ex)
        {
            ExternalException ex2 = ex as ExternalException;
            if (ex2 == null)
            {
                return false;
            }
            if (ex2.ErrorCode != -2147221040)
            {
                return ex2.ErrorCode == -2147221038;
            }
            return true;
        }

        private void OpenSaveDir()
        {
            try
            {
                string text = cfg.ResolveSaveDir();
                Process.Start("explorer.exe", "\"" + text + "\"");
            }
            catch (Exception ex)
            {
                Balloon("无法打开文件夹：" + ex.Message, ToolTipIcon.Warning);
            }
        }

        internal bool ApplyAutostart(bool enable, bool silent, bool persist=true)
        {
            try
            {
                using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
                {
                    if (registryKey == null)
                    {
                        throw new Exception("无法打开注册表 Run 键");
                    }
                    if (enable)
                    {
                        string value = AutostartCommand();
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new Exception("当前模式无法设置开机自启，请先运行 scripts\\install.cmd 生成 SnipRefine.exe");
                        }
                        registryKey.SetValue("SnapGrab", value);
                    }
                    else
                    {
                        registryKey.DeleteValue("SnapGrab", false);
                    }
                }
                if (cfg.StartWithWindows != enable)
                {
                    cfg.StartWithWindows = enable;
                    if(persist){Config saved=Config.ReadSavedOr(cfg);saved.StartWithWindows=enable;if(!saved.Save())return false;}
                }
                return true;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    Balloon("设置开机自启失败：" + ex.Message, ToolTipIcon.Warning);
                }
                return false;
            }
            finally
            {
                SyncAutostartCheck();
            }
        }

        private void SyncAutostartCheck()
        {
            try
            {
                bool flag = false;
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    if (registryKey != null)
                    {
                        object value = registryKey.GetValue("SnapGrab");
                        flag = value != null && !string.IsNullOrEmpty(value.ToString());
                    }
                }
                cfg.StartWithWindows = flag;
                syncingAutostart = true;
                try
                {
                    miAutostart.Checked = flag;
                }
                finally
                {
                    syncingAutostart = false;
                }
            }
            catch
            {
            }
        }

        private string AutostartCommand()
        {
            string location = typeof(Program).Assembly.Location;
            if (!string.IsNullOrEmpty(location) && location.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return "\"" + location + "\" --background";
            }
            if (!string.IsNullOrEmpty(Program.LauncherCommand))
            {
                return Program.LauncherCommand + " -Background";
            }
            return null;
        }

        internal static Bitmap CreateAppBitmap(int size)
        {
            Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                float num = (float)size * 0.03f;
                Rectangle r = new Rectangle((int)num, (int)num, size - (int)(num * 2f), size - (int)(num * 2f));
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 28, 122, 255)))
                {
                    FillRoundRect(graphics, r, Math.Max(3, size / 6), brush);
                }
                float num2 = (float)size * 0.24f;
                float num3 = (float)size * 0.16f;
                float width = Math.Max(2f, (float)size * 0.075f);
                using (Pen pen = new Pen(Color.White, width))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    graphics.DrawLines(pen, new PointF[3]
                    {
                        new PointF(num2 + num3, num2),
                        new PointF(num2, num2),
                        new PointF(num2, num2 + num3)
                    });
                    graphics.DrawLines(pen, new PointF[3]
                    {
                        new PointF((float)size - num2 - num3, num2),
                        new PointF((float)size - num2, num2),
                        new PointF((float)size - num2, num2 + num3)
                    });
                    graphics.DrawLines(pen, new PointF[3]
                    {
                        new PointF((float)size - num2 - num3, (float)size - num2),
                        new PointF((float)size - num2, (float)size - num2),
                        new PointF((float)size - num2, (float)size - num2 - num3)
                    });
                    graphics.DrawLines(pen, new PointF[3]
                    {
                        new PointF(num2 + num3, (float)size - num2),
                        new PointF(num2, (float)size - num2),
                        new PointF(num2, (float)size - num2 - num3)
                    });
                }
                float num4 = Math.Max(1.6f, (float)size * 0.075f);
                using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(255, 255, 82, 82)))
                {
                    graphics.FillEllipse(brush2, (float)size * 0.63f, (float)size * 0.3f, num4 * 3f, num4 * 3f);
                    return bitmap;
                }
            }
        }

        private static void FillRoundRect(Graphics g, Rectangle r, int radius, Brush brush)
        {
            GraphicsPath graphicsPath = new GraphicsPath();
            int num = radius * 2;
            if (num > r.Width)
            {
                num = r.Width;
            }
            if (num > r.Height)
            {
                num = r.Height;
            }
            if (num < 2)
            {
                num = 2;
            }
            graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
            graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
            graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
            graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
            graphicsPath.CloseFigure();
            g.FillPath(brush, graphicsPath);
            graphicsPath.Dispose();
        }

        private static Icon CreateTrayIcon()
        {
            try
            {
                Icon packaged = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (packaged != null) return packaged;
            }
            catch { }

            Bitmap bitmap = CreateAppBitmap(32);
            IntPtr hicon = bitmap.GetHicon();
            try
            {
                return (Icon)Icon.FromHandle(hicon).Clone();
            }
            finally
            {
                Native.DestroyIcon(hicon);
                bitmap.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                historyGeneration++;
                if (quickHot != null) { quickHot.Shutdown(); quickHot = null; }
                if (fullHot != null) { fullHot.Shutdown(); fullHot = null; }
                if (hot != null)
                {
                    hot.Shutdown();
                    hot = null;
                }
                try
                {
                    SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                }
                catch
                {
                }
                if (tray != null)
                {
                    tray.Visible = false;
                    tray.Dispose();
                    tray = null;
                }
                if (menu != null)
                {
                    menu.Dispose();
                    menu = null;
                }
                if(appIcon!=null) { appIcon.Dispose(); appIcon=null; }
                if (floating != null && !floating.IsDisposed)
                {
                    floating.Close();
                    floating.Dispose();
                    floating = null;
                }
                if (overlay != null && !overlay.IsDisposed)
                {
                    overlay.Dispose();
                    overlay = null;
                }
                if (mainForm != null && !mainForm.IsDisposed)
                {
                    mainForm.Dispose();
                    mainForm = null;
                }
                if (marshaller != null && !marshaller.IsDisposed)
                {
                    marshaller.Dispose();
                    marshaller = null;
                }
                ClearRecoveryImage();
                DisposeRecentShots();
                lock (lastShotLock)
                {
                    if (lastShot != null)
                    {
                        lastShot.Dispose();
                        lastShot = null;
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
