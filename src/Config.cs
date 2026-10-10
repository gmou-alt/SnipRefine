using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SnapGrab
{
    public class Config
    {
        public string UiTheme = "light";
        public string UiLanguage = "zh-CN";

        public string Hotkey = "Ctrl+Alt+A";
        public string QuickCaptureHotkey = "Ctrl+Alt+S";
        public string FullscreenCaptureHotkey = "PrintScreen";
        public bool FullscreenOpenEditor = true;
        public bool KeepSavedHistory;

        public string SaveDir = "";

        public string FilenamePattern = "SnapGrab_{yyyy}{MM}{dd}_{HH}{mm}{ss}";

        public bool DateSubfolder;

        public string Format = "png";

        public int Quality = 92;

        public bool CopyToClipboard = true;

        public bool StartWithWindows = false;

        public bool ShowWindowOnStart = true;

        public bool MinimizeToTray = true;

        public bool ShowFloatingIcon = false;

        public string FloatingIconPos = "";

        public bool TrimWorkingSet = true;

        public string ProcessPriority = "normal";

        public bool DebugTiming;

        public bool OpenEditor = true;

        public string DrawColor = "red";

        public int DrawWidth = 3;

        public int MosaicBlock = 8;

        public List<AiProvider> AiProviders = new List<AiProvider>();

        public string AiActiveProviderId = "";

        public bool AiPanelHotkeyEnabled;

        public string AiPanelHotkey = "Ctrl+Alt+Q";

        public bool AiJsonMode = true;

        public int AiTemperature = 2;
        public int AiTimeoutSeconds = 120;
        public int AiMaxOutputTokens = 2048;
        public bool AiUseSystemProxy = true;
        public bool AiReviewOperations = true;

        public int AiImageMaxEdge = 1600;
        public string ImageEditProtocol="openai";
        public string ImageEditEndpoint="https://api.openai.com/v1/images/edits", ImageEditModel="gpt-image-1.5", ImageEditKey="";

        public string AiPreset = "";

        public string AiEndpoint = "";

        public string AiModel = "";

        public string AiApiKey = "";

        public bool CreatedNew;

        private static readonly KeyDef[] KeyTable = BuildKeyTable();

        public static string ConfigDir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnapGrab");
            }
        }

        public static string ConfigPath
        {
            get
            {
                return Path.Combine(ConfigDir, "SnapGrab.ini");
            }
        }

        public AiProvider ActiveProvider()
        {
            for (int i = 0; i < AiProviders.Count; i++)
            {
                if (string.Equals(AiProviders[i].Id, AiActiveProviderId, StringComparison.OrdinalIgnoreCase))
                {
                    return AiProviders[i];
                }
            }
            if (AiProviders.Count <= 0)
            {
                return null;
            }
            return AiProviders[0];
        }

        private static KeyDef[] BuildKeyTable()
        {
            List<KeyDef> list = new List<KeyDef>();
            for (int i = 65; i <= 90; i++)
            {
                list.Add(new KeyDef((uint)i, ((char)i).ToString(), ""));
            }
            for (int j = 48; j <= 57; j++)
            {
                list.Add(new KeyDef((uint)j, ((char)j).ToString(), ""));
            }
            for (int k = 1; k <= 12; k++)
            {
                list.Add(new KeyDef((uint)(112 + k - 1), "F" + k, ""));
            }
            for (int l = 0; l <= 9; l++)
            {
                list.Add(new KeyDef((uint)(96 + l), "Num" + l, "numpad" + l));
            }
            list.Add(new KeyDef(8u, "Backspace", "back"));
            list.Add(new KeyDef(9u, "Tab", ""));
            list.Add(new KeyDef(13u, "Enter", "return"));
            list.Add(new KeyDef(27u, "Esc", "escape"));
            list.Add(new KeyDef(32u, "Space", "spacebar"));
            list.Add(new KeyDef(33u, "PageUp", "pgup"));
            list.Add(new KeyDef(34u, "PageDown", "pgdn"));
            list.Add(new KeyDef(35u, "End", ""));
            list.Add(new KeyDef(36u, "Home", ""));
            list.Add(new KeyDef(37u, "Left", "leftarrow"));
            list.Add(new KeyDef(38u, "Up", "uparrow"));
            list.Add(new KeyDef(39u, "Right", "rightarrow"));
            list.Add(new KeyDef(40u, "Down", "downarrow"));
            list.Add(new KeyDef(44u, "PrintScreen", "prtsc,print"));
            list.Add(new KeyDef(45u, "Insert", "ins"));
            list.Add(new KeyDef(46u, "Delete", "del"));
            list.Add(new KeyDef(106u, "*", "multiply,numpadmultiply,numpad*"));
            list.Add(new KeyDef(107u, "+", "add,numpadadd,numpad+"));
            list.Add(new KeyDef(109u, "-", "subtract,numpadsubtract,numpad-"));
            list.Add(new KeyDef(110u, ".", "decimal,numpaddecimal"));
            list.Add(new KeyDef(111u, "/", "divide,numpaddivide,numpad/"));
            list.Add(new KeyDef(186u, ";", "semicolon"));
            list.Add(new KeyDef(187u, "=", "equals"));
            list.Add(new KeyDef(188u, ",", "comma"));
            list.Add(new KeyDef(189u, "-", "minus"));
            list.Add(new KeyDef(190u, ".", "period"));
            list.Add(new KeyDef(191u, "/", "slash"));
            list.Add(new KeyDef(192u, "`", "backtick,oemtilde"));
            list.Add(new KeyDef(219u, "[", "lbracket"));
            list.Add(new KeyDef(220u, "\\", "backslash"));
            list.Add(new KeyDef(221u, "]", "rbracket"));
            list.Add(new KeyDef(222u, "'", "quote"));
            return list.ToArray();
        }

        public string EffectiveSaveDir()
        {
            if (!string.IsNullOrEmpty(SaveDir))
            {
                return SaveDir;
            }
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }
            return Path.Combine(folderPath, "SnapGrab");
        }

        public string ResolveSaveDir()
        {
            string[] array = new string[5]
            {
                EffectiveSaveDir(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SnapGrab"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SnapGrab"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SnapGrab", "Shots"),
                Path.Combine(Path.GetTempPath(), "SnapGrab")
            };
            foreach (string text in array)
            {
                if (!string.IsNullOrEmpty(text) && IsWritableDir(text))
                {
                    return text;
                }
            }
            return Path.Combine(Path.GetTempPath(), "SnapGrab");
        }

        public static bool IsWritableDir(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, ".snapgrab_write_probe");
                File.WriteAllText(path, "snapgrab");
                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Config Load()
        {
            Config config = new Config();
            if (!File.Exists(ConfigPath))
            {
                try
                {
                    config.Save();
                    config.CreatedNew = true;
                }
                catch
                {
                }
                return config;
            }
            string[] array = null;
            try
            {
                array = File.ReadAllLines(ConfigPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Diag.Log("配置文件读取失败: " + ex.Message);
                return config;
            }
            for (int i = 0; i < array.Length; i++)
            {
                try
                {
                    ParseLine(config, array[i]);
                }
                catch (Exception ex2)
                {
                    Diag.Log("配置第 " + (i + 1) + " 行解析失败，已跳过: " + ex2.Message);
                }
            }
            MigrateAiProviders(config);
            try
            {
                if (config.AiProviders.Count > 0)
                {
                    config.Save();
                }
            }
            catch
            {
            }
            return config;
        }

        private static void MigrateAiProviders(Config c)
        {
            if (c.AiProviders.Count <= 0 && (!string.IsNullOrEmpty(c.AiEndpoint) || !string.IsNullOrEmpty(c.AiModel) || !string.IsNullOrEmpty(c.AiApiKey)))
            {
                AiPreset aiPreset = AiClient.FindPresetExact(c.AiPreset);
                AiProvider aiProvider = new AiProvider();
                aiProvider.Id = "default";
                aiProvider.Name = ((!string.IsNullOrEmpty(c.AiPreset)) ? c.AiPreset : "默认提供商");
                aiProvider.Protocol = "openai";
                aiProvider.ApiKey = c.AiApiKey;
                aiProvider.Endpoint = c.AiEndpoint;
                if (string.IsNullOrEmpty(aiProvider.Endpoint) && aiPreset != null)
                {
                    aiProvider.Endpoint = aiPreset.Endpoint;
                }
                string text = c.AiModel;
                if (string.IsNullOrEmpty(text) && aiPreset != null)
                {
                    text = aiPreset.Model;
                }
                if (!string.IsNullOrEmpty(text))
                {
                    aiProvider.Models.Add(text);
                }
                c.AiProviders.Add(aiProvider);
                c.AiActiveProviderId = aiProvider.Id;
            }
        }

        private static void ParseLine(Config c, string raw)
        {
            string text = (raw ?? "").Trim();
            if (text.Length == 0 || text.StartsWith(";") || text.StartsWith("#") || text.StartsWith("["))
            {
                return;
            }
            int num = text.IndexOf('=');
            if (num <= 0)
            {
                return;
            }
            string text2 = text.Substring(0, num).Trim().ToLowerInvariant();
            string text3 = text.Substring(num + 1).Trim();
            switch (text2)
            {
            case "quick_capture_hotkey": c.QuickCaptureHotkey = text3; return;
            case "fullscreen_capture_hotkey": c.FullscreenCaptureHotkey = text3; return;
            case "keep_saved_history": c.KeepSavedHistory = ToBool(text3, false); return;
            case "fullscreen_open_editor": c.FullscreenOpenEditor = ToBool(text3, true); return;
            case "hotkey":
                c.Hotkey = text3;
                return;
            case "save_dir":
                c.SaveDir = text3;
                return;
            case "filename_pattern":
                c.FilenamePattern = text3;
                return;
            case "save_date_subfolder":
                c.DateSubfolder = ToBool(text3, false);
                return;
            case "format":
                c.Format = text3.ToLowerInvariant();
                return;
            case "quality":
            {
                int result;
                if (int.TryParse(text3, out result))
                {
                    c.Quality = Math.Max(1, Math.Min(100, result));
                }
                return;
            }
            case "copy_to_clipboard":
                c.CopyToClipboard = ToBool(text3, true);
                return;
            case "start_with_windows":
                c.StartWithWindows = ToBool(text3, false);
                return;
            case "show_window_on_start":
                c.ShowWindowOnStart = ToBool(text3, true);
                return;
            case "minimize_to_tray":
                c.MinimizeToTray = ToBool(text3, true);
                return;
            case "show_floating_icon":
                c.ShowFloatingIcon = false;
                return;
            case "floating_icon_pos":
                c.FloatingIconPos = text3;
                return;
            case "ui_theme":
                c.UiTheme = text3 == "dark" ? "dark" : "light";
                return;
            case "image_edit_protocol": c.ImageEditProtocol=text3=="gemini" ? "gemini" : "openai";return;
            case "image_edit_endpoint": c.ImageEditEndpoint=text3;return;
            case "image_edit_model": c.ImageEditModel=text3;return;
            case "image_edit_key": c.ImageEditKey=AiClient.UnprotectKey(text3);return;
            case "ui_language":
                c.UiLanguage=UiText.NormalizeLanguage(text3);
                return;
            case "trim_working_set":
                c.TrimWorkingSet = ToBool(text3, true);
                return;
            case "process_priority":
                c.ProcessPriority = text3.ToLowerInvariant();
                return;
            case "debug_timing":
                c.DebugTiming = ToBool(text3, false);
                return;
            case "open_editor":
                c.OpenEditor = ToBool(text3, true);
                return;
            case "draw_color":
                c.DrawColor = text3.ToLowerInvariant();
                return;
            case "draw_width":
            {
                int result5;
                if (int.TryParse(text3, out result5))
                {
                    c.DrawWidth = Math.Max(1, Math.Min(20, result5));
                }
                return;
            }
            case "mosaic_block":
            {
                int result4;
                if (int.TryParse(text3, out result4))
                {
                    c.MosaicBlock = Math.Max(4, Math.Min(24, result4));
                }
                return;
            }
            case "ai_preset":
                c.AiPreset = text3;
                return;
            case "ai_endpoint":
                c.AiEndpoint = text3;
                return;
            case "ai_model":
                c.AiModel = text3;
                return;
            case "ai_api_key":
                c.AiApiKey = AiClient.UnprotectKey(text3);
                return;
            case "ai_active_provider":
                c.AiActiveProviderId = text3;
                return;
            case "ai_panel_hotkey_enabled":
                c.AiPanelHotkeyEnabled = ToBool(text3, false);
                return;
            case "ai_panel_hotkey":
                c.AiPanelHotkey = text3;
                return;
            case "ai_json_mode":
                c.AiJsonMode = ToBool(text3, true);
                return;
            case "ai_timeout_seconds":
                int seconds; if (int.TryParse(text3,out seconds)) c.AiTimeoutSeconds = Math.Max(10,Math.Min(300,seconds));
                return;
            case "ai_max_output_tokens":
                int tokens; if (int.TryParse(text3,out tokens)) c.AiMaxOutputTokens = Math.Max(256,Math.Min(8192,tokens));
                return;
            case "ai_review_operations":
                c.AiReviewOperations = ToBool(text3,false); return;
            case "ai_use_system_proxy":
                c.AiUseSystemProxy = ToBool(text3,true); return;
            case "ai_temperature":
            {
                int result3;
                if (int.TryParse(text3, out result3))
                {
                    c.AiTemperature = Math.Max(0, Math.Min(20, result3));
                }
                return;
            }
            case "ai_image_max_edge":
            {
                int result2;
                if (int.TryParse(text3, out result2))
                {
                    c.AiImageMaxEdge = Math.Max(512, Math.Min(4096, result2));
                }
                return;
            }
            }
            if (text2.StartsWith("ai_provider_", StringComparison.Ordinal))
            {
                AiProvider aiProvider = AiProvider.Deserialize(text3);
                if (aiProvider != null)
                {
                    c.AiProviders.Add(aiProvider);
                }
                else
                {
                    Diag.Log("提供商配置无效，已跳过: " + text2);
                }
            }
        }

        internal static Config Copy(Config source)
        {
            Config copy=new Config();
            foreach(System.Reflection.FieldInfo field in typeof(Config).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))field.SetValue(copy,field.GetValue(source));
            copy.AiProviders=new List<AiProvider>();foreach(AiProvider provider in source.AiProviders)copy.AiProviders.Add(provider.Snapshot());return copy;
        }
        internal static Config ReadSavedOr(Config fallback)
        {
            if(!File.Exists(ConfigPath))return Copy(fallback);
            try {Config saved=new Config();foreach(string line in File.ReadAllLines(ConfigPath,Encoding.UTF8))ParseLine(saved,line);return saved;}
            catch {throw new InvalidOperationException("无法读取已有设置，请检查配置文件权限后重试；未写入新设置。");}
        }

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                List<string> list = BuildLines();
                string text = ConfigPath + ".tmp";
                File.WriteAllLines(text, list.ToArray(), Encoding.UTF8);
                if (File.Exists(ConfigPath))
                {
                    // Atomic replacement preserves the previous configuration on failure.
                    File.Replace(text, ConfigPath, null);
                }
                else File.Move(text, ConfigPath);
                return true;
            }
            catch (Exception ex)
            {
                Diag.Log("配置保存失败: " + ex.Message);
                return false;
            }
        }

        private List<string> BuildLines()
        {
            List<string> list = new List<string>();
            list.Add("# SnapGrab 配置文件（界面里点“保存设置”会自动写入这里，也可手工编辑后重启程序）");
            list.Add("#");
            list.Add("# hotkey  全局热键，格式：修饰键+主键，修饰键可用 Ctrl / Shift / Alt / Win");
            list.Add("#         主键：A-Z、0-9、F1-F12、方向键、Space、Tab、Esc、Enter、PrintScreen、");
            list.Add("#               Insert、Delete、Home、End、PageUp、PageDown、小键盘数字等");
            list.Add("hotkey=" + Hotkey);
            list.Add("quick_capture_hotkey=" + QuickCaptureHotkey);
            list.Add("fullscreen_capture_hotkey=" + FullscreenCaptureHotkey);
            list.Add("keep_saved_history=" + (KeepSavedHistory ? "1" : "0"));
            list.Add("fullscreen_open_editor=" + (FullscreenOpenEditor ? "1" : "0"));
            list.Add("#");
            list.Add("# save_dir  截图保存目录；留空表示“图片”文件夹下的 SnapGrab 子目录");
            list.Add("save_dir=" + SaveDir);
            list.Add("#");
            list.Add("# filename_pattern  文件名模板，可用占位符：");
            list.Add("#   {yyyy} {yy} {MM} {dd} {HH} {mm} {ss} {fff} {date} {time} {w} {h}");
            list.Add("filename_pattern=" + FilenamePattern);
            list.Add("# 是否按日期建子文件夹（如 2026-10-06）：1=是 0=否");
            list.Add("save_date_subfolder=" + (DateSubfolder ? "1" : "0"));
            list.Add("#");
            list.Add("# format  png 或 jpg");
            list.Add("format=" + Format);
            list.Add("# jpg 质量 1-100");
            list.Add("quality=" + Quality);
            list.Add("# 截图后是否同时复制到剪贴板：1=是 0=否");
            list.Add("copy_to_clipboard=" + (CopyToClipboard ? "1" : "0"));
            list.Add("# 开机自动启动：1=是 0=否");
            list.Add("start_with_windows=" + (StartWithWindows ? "1" : "0"));
            list.Add("# 启动时显示主界面：1=是 0=否");
            list.Add("show_window_on_start=" + (ShowWindowOnStart ? "1" : "0"));
            list.Add("# 关闭窗口时最小化到托盘：1=是 0=否");
            list.Add("minimize_to_tray=" + (MinimizeToTray ? "1" : "0"));
            list.Add("# 在屏幕右下角显示可拖动的悬浮图标（Windows 可能把托盘图标藏进「^」里）：1=是 0=否");
            list.Add("show_floating_icon=" + (ShowFloatingIcon ? "1" : "0"));
            list.Add("# 悬浮图标位置 \"x,y\"；留空表示默认停在右下角");
            list.Add("floating_icon_pos=" + FloatingIconPos);
            list.Add("# 空闲时回收内存，降低后台占用（对游戏/大模型场景友好）：1=是 0=否");
            list.Add("ui_theme=" + (UiTheme == "dark" ? "dark" : "light"));
            list.Add("image_edit_protocol="+(ImageEditProtocol=="gemini" ? "gemini" : "openai"));
            list.Add("image_edit_endpoint="+ImageEditEndpoint);
            list.Add("image_edit_model="+ImageEditModel);
            list.Add("image_edit_key="+AiClient.ProtectKey(ImageEditKey));
            list.Add("ui_language=" + UiText.NormalizeLanguage(UiLanguage));
            list.Add("trim_working_set=" + (TrimWorkingSet ? "1" : "0"));
            list.Add("# 进程优先级：idle / below_normal / normal / above_normal / high");
            list.Add("# 默认 normal：空闲时几乎不占 CPU，满载时也能瞬间响应；");
            list.Add("# 编码与落盘在单独的 below_normal 线程里，不会和游戏/大模型抢 CPU");
            list.Add("process_priority=" + (string.IsNullOrEmpty(ProcessPriority) ? "normal" : ProcessPriority));
            list.Add("# 记录各阶段耗时到同目录 timing.log（排查性能问题时打开）：1=是 0=否");
            list.Add("debug_timing=" + (DebugTiming ? "1" : "0"));
            list.Add("#");
            list.Add("# open_editor  截图后是否打开编辑器（标注 / 裁剪 / 缩放）：1=是 0=否");
            list.Add("# 关掉后截图直接落盘，不走编辑器");
            list.Add("open_editor=" + (OpenEditor ? "1" : "0"));
            list.Add("# draw_color  画笔默认颜色：red / yellow / green / blue / black / white");
            list.Add("draw_color=" + (string.IsNullOrEmpty(DrawColor) ? "red" : DrawColor));
            list.Add("# draw_width  画笔默认粗细 1-20");
            list.Add("draw_width=" + DrawWidth);
            list.Add("# mosaic_block  马赛克默认粒度 4-24（越大越糊）");
            list.Add("mosaic_block=" + MosaicBlock);
            list.Add("#");
            list.Add("# ---- AI 接入（截图后可用 AI 给建议 / 自动标注 / OCR / 生成式改图）----");
            list.Add("# 支持配置多个模型提供商，每个一行，字段用 | 分隔：");
            list.Add("#   ai_provider_N = Id|显示名称|API地址|协议|API密钥|模型1,模型2");
            list.Add("# 协议 protocol：openai = OpenAI Chat Completions（大多数服务兼容）；");
            list.Add("#                anthropic = Anthropic Messages");
            list.Add("# 密钥用 Windows DPAPI 按当前用户加密（前缀 enc:），换用户或换机器后需重填。");
            for (int i = 0; i < AiProviders.Count; i++)
            {
                list.Add("ai_provider_" + i + "=" + AiProviders[i].Serialize());
            }
            list.Add("# 当前使用哪个提供商（填它的 Id）");
            list.Add("ai_active_provider=" + AiActiveProviderId);
            list.Add("# AI 对话侧边栏：也可以用全局热键滑出/收起（默认关闭，避免与截图热键抢注册）");
            list.Add("ai_panel_hotkey_enabled=" + (AiPanelHotkeyEnabled ? "1" : "0"));
            list.Add("ai_panel_hotkey=" + AiPanelHotkey);
            list.Add("# ai_json_mode  请求模型输出 JSON（标注/OCR 需要）。个别服务不支持就设 0");
            list.Add("ai_json_mode=" + (AiJsonMode ? "1" : "0"));
            list.Add("# ai_temperature  温度 ×10（2 = 0.2）。标注类任务要稳，建议 0-3");
            list.Add("ai_temperature=" + AiTemperature);
            list.Add("ai_timeout_seconds=" + AiTimeoutSeconds);
            list.Add("ai_max_output_tokens=" + AiMaxOutputTokens);
            list.Add("ai_review_operations=" + (AiReviewOperations ? "1" : "0"));
            list.Add("ai_use_system_proxy=" + (AiUseSystemProxy ? "1" : "0"));
            list.Add("# ai_image_max_edge  发给模型的图片长边上限（越小越快越省，坐标会自动换算回原图）");
            list.Add("ai_image_max_edge=" + AiImageMaxEdge);
            list.Add("# 以下四项用于配置迁移兼容，新配置请忽略");
            list.Add("ai_preset=" + AiPreset);
            list.Add("ai_endpoint=" + AiEndpoint);
            list.Add("ai_model=" + AiModel);
            list.Add("ai_api_key=" + AiClient.ProtectKey(AiApiKey));
            return list;
        }

        private static bool ToBool(string v, bool def)
        {
            if (string.IsNullOrEmpty(v))
            {
                return def;
            }
            v = v.Trim().ToLowerInvariant();
            switch (v)
            {
            case "1":
            case "true":
            case "yes":
            case "on":
                return true;
            case "0":
            case "false":
            case "no":
            case "off":
                return false;
            default:
                return def;
            }
        }

        public static string ExpandPattern(string pattern, DateTime now, int width, int height)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                pattern = "SnapGrab_{yyyy}{MM}{dd}_{HH}{mm}{ss}";
            }
            string text = pattern;
            text = text.Replace("{yyyy}", now.ToString("yyyy"));
            text = text.Replace("{yy}", now.ToString("yy"));
            text = text.Replace("{MM}", now.ToString("MM"));
            text = text.Replace("{dd}", now.ToString("dd"));
            text = text.Replace("{HH}", now.ToString("HH"));
            text = text.Replace("{mm}", now.ToString("mm"));
            text = text.Replace("{ss}", now.ToString("ss"));
            text = text.Replace("{fff}", now.ToString("fff"));
            text = text.Replace("{date}", now.ToString("yyyyMMdd"));
            text = text.Replace("{time}", now.ToString("HHmmss"));
            text = text.Replace("{w}", width.ToString());
            text = text.Replace("{h}", height.ToString());
            text = SanitizeFileName(text);
            if (text.Length == 0)
            {
                text = "SnapGrab_" + now.ToString("yyyyMMdd_HHmmss");
            }
            return text;
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            StringBuilder stringBuilder = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                bool flag = true;
                for (int j = 0; j < invalidFileNameChars.Length; j++)
                {
                    if (invalidFileNameChars[j] == c)
                    {
                        flag = false;
                        break;
                    }
                }
                stringBuilder.Append(flag ? c : '_');
            }
            return stringBuilder.ToString().Trim().TrimEnd(new char[] { '.' });
        }

        public static ProcessPriorityClass ParsePriority(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return ProcessPriorityClass.Normal;
            }
            switch (text.Trim().ToLowerInvariant())
            {
            case "idle":
                return ProcessPriorityClass.Idle;
            case "below_normal":
            case "belownormal":
                return ProcessPriorityClass.BelowNormal;
            case "above_normal":
            case "abovenormal":
                return ProcessPriorityClass.AboveNormal;
            case "high":
                return ProcessPriorityClass.High;
            default:
                return ProcessPriorityClass.Normal;
            }
        }

        public static string PriorityToText(string text)
        {
            switch (ParsePriority(text))
            {
            case ProcessPriorityClass.Idle:
                return "最低（idle）";
            case ProcessPriorityClass.BelowNormal:
                return "低于正常（省 CPU）";
            case ProcessPriorityClass.AboveNormal:
                return "高于正常";
            case ProcessPriorityClass.High:
                return "高";
            default:
                return "正常（推荐）";
            }
        }

        public static bool TryParseHotkey(string text, out uint mods, out uint vk, out string display)
        {
            mods = 0u;
            vk = 0u;
            display = null;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            string[] array = text.Split(new char[] { '+' });
            bool flag = false;
            string item = "";
            List<string> list = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                string text2 = array[i].Trim().ToLowerInvariant();
                if (text2.Length == 0)
                {
                    if (i > 0 && i < array.Length - 1 && !flag)
                    {
                        flag = true;
                        vk = 107u;
                        item = "+";
                    }
                    continue;
                }
                switch (text2)
                {
                case "ctrl":
                case "control":
                case "ctl":
                    if ((mods & 2) != 0)
                    {
                        return false;
                    }
                    mods |= 2u;
                    list.Add("Ctrl");
                    continue;
                case "shift":
                    if ((mods & 4) != 0)
                    {
                        return false;
                    }
                    mods |= 4u;
                    list.Add("Shift");
                    continue;
                case "alt":
                case "menu":
                    if ((mods & 1) != 0)
                    {
                        return false;
                    }
                    mods |= 1u;
                    list.Add("Alt");
                    continue;
                case "win":
                case "windows":
                case "super":
                    if ((mods & 8) != 0)
                    {
                        return false;
                    }
                    mods |= 8u;
                    list.Add("Win");
                    continue;
                }
                if (flag)
                {
                    return false;
                }
                flag = true;
                uint code = 0u;
                string label = "";
                if (!TryNamedKey(text2, out code, out label))
                {
                    return false;
                }
                vk = code;
                item = label;
            }
            if (!flag || vk == 0)
            {
                return false;
            }
            if (mods == 0 && vk != 44)
            {
                return false;
            }
            List<string> list2 = new List<string>(list);
            list2.Add(item);
            display = string.Join("+", list2.ToArray());
            return true;
        }

        public static string HotkeyForStorage(string display)
        {
            if (string.IsNullOrEmpty(display))
            {
                return display;
            }
            string[] array = display.Split(new char[] { '+' });
            List<string> list = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                string text = array[i].Trim();
                if (text.Length == 0)
                {
                    if (i > 0 && i < array.Length - 1)
                    {
                        list.Add("NumpadAdd");
                    }
                    continue;
                }
                switch (text)
                {
                case "+":
                    list.Add("NumpadAdd");
                    break;
                case "-":
                    list.Add("NumpadSubtract");
                    break;
                case "*":
                    list.Add("NumpadMultiply");
                    break;
                case "/":
                    list.Add("NumpadDivide");
                    break;
                default:
                    list.Add(text);
                    break;
                }
            }
            return string.Join("+", list.ToArray());
        }

        public static bool FormatHotkey(Keys keyData, out string text)
        {
            text = null;
            Keys keys = keyData & Keys.KeyCode;
            if (keys == Keys.None || keys == Keys.ControlKey || keys == Keys.ShiftKey || keys == Keys.Menu || keys == Keys.LWin || keys == Keys.RWin)
            {
                return false;
            }
            uint num = 0u;
            List<string> list = new List<string>();
            if ((keyData & Keys.Control) == Keys.Control)
            {
                num |= 2;
                list.Add("Ctrl");
            }
            if ((keyData & Keys.Shift) == Keys.Shift)
            {
                num |= 4;
                list.Add("Shift");
            }
            if ((keyData & Keys.Alt) == Keys.Alt)
            {
                num |= 1;
                list.Add("Alt");
            }
            if (num == 0 && keys != Keys.PrintScreen)
            {
                return false;
            }
            string text2 = LabelForVk((uint)keys);
            if (text2 == null)
            {
                return false;
            }
            list.Add(text2);
            text = string.Join("+", list.ToArray());
            return true;
        }

        public static bool TryNamedKey(string t, out uint code, out string label)
        {
            code = 0u;
            label = null;
            string text = t.Trim().ToLowerInvariant();
            if (text.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < KeyTable.Length; i++)
            {
                if (KeyTable[i].Label.ToLowerInvariant() == text)
                {
                    code = KeyTable[i].Vk;
                    label = KeyTable[i].Label;
                    return true;
                }
                if (KeyTable[i].Aliases.Length == 0)
                {
                    continue;
                }
                string[] array = KeyTable[i].Aliases.Split(new char[] { ',' });
                for (int j = 0; j < array.Length; j++)
                {
                    if (array[j].Length > 0 && array[j] == text)
                    {
                        code = KeyTable[i].Vk;
                        label = KeyTable[i].Label;
                        return true;
                    }
                }
            }
            return false;
        }

        public static string LabelForVk(uint vk)
        {
            for (int i = 0; i < KeyTable.Length; i++)
            {
                if (KeyTable[i].Vk == vk)
                {
                    return KeyTable[i].Label;
                }
            }
            return null;
        }
    }
}
