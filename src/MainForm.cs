using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SnapGrab
{
    internal partial class MainForm : Form
    {
        private class ToggleBar : Panel
        {
            public ToggleBar()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                DoubleBuffered = true;
                BackColor = Color.Transparent;
            }
        }

        private const int WindowW = 860;

        private const int WindowH = 910;

        private const int SidebarW = 208;

        private const int FooterH = 70;

        private const int StatusH = 30;

        private const int PadX = 32;

        private const int ContentX = 240;

        private const int ContentW = 588;

        private const int PanelW = 406;

        private const int AnimeMs = 200;

        private const int AnimeInterval = 15;

        private const int PageTopOffset = 0;

        private const int ToggleSize = 38;

        private const int ToggleMargin = 14;

        private const int ToggleBarH = 88;

        private DshGlassCard providerDetails;
        private DshButton providerDetailsToggle;
        private readonly SidebarMotion providerDetailsMotion = new SidebarMotion();
        private readonly System.Diagnostics.Stopwatch providerDetailsWatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer providerDetailsTimer;
        private bool providerDetailsOpen = true;

        private void SetProviderDetails(bool open)
        {
            if (providerDetails == null) return;
            providerDetailsOpen = open;
            providerDetailsToggle.Text = open ? "收起模型设置" : "编辑模型设置";
            providerDetailsMotion.MoveTo(open ? 1 : 0);
            if (!GlassSurface.MotionEnabled || !Visible)
            {
                providerDetailsMotion.Jump(open ? 1 : 0);
                ApplyProviderDetailsFrame();
                return;
            }
            providerDetails.Visible = true;
            providerDetailsWatch.Restart();
            providerDetailsTimer.Start();
        }

        private void ApplyProviderDetailsFrame()
        {
            int height = (int)Math.Round((810 - 336 - 14) * providerDetailsMotion.Position);
            // Hide before reducing below paintable size, including reverse transitions.
            if (height <= 1) providerDetails.Visible = false;
            providerDetails.Height = height;
            if (height > 1) providerDetails.Visible = true;
            if (!providerDetailsMotion.Moving)
            {
                providerDetailsTimer.Stop();
                providerDetailsWatch.Stop();
            }
        }

        private int WinW = 860;

        private ChatPanel chatPanel;

        private System.Windows.Forms.Timer animeTimer;

        private readonly SidebarMotion panelMotion = new SidebarMotion();

        private readonly Stopwatch animeWatch = new Stopwatch();

        private bool panelOpen;

        private bool panelAnimating;

        private TrayApp app;

        private Config cfg;

        private bool allowExit;

        private string pendingHotkey;

        private DshTextInput txtHotkey;

        private DshLabel lblHotkeyState;

        private DshTextInput txtDir;

        private DshTextInput txtPattern;

        private DshSelect cboFormat;

        private DshNumber numQuality;

        private DshLabel lblQualityHint;

        private DshToggle chkClipboard;

        private DshToggle chkDateFolder;

        private DshToggle chkAutostart;

        private DshToggle chkShowOnStart;

        private DshToggle chkMinimizeToTray;


        private DshToggle chkTrim;

        private DshSelect cboPriority;

        private DshLabel lblStatus;

        private Panel aiProviderHost;

        private readonly List<AiProviderRow> providerRows = new List<AiProviderRow>();

        private DshTextInput txtProvName;

        private DshTextInput txtProvId;

        private DshTextInput txtProvEndpoint;
        private DshSelect cboDefaultModel;

        private DshTextInput txtProvKey;

        private DshTextInput txtProvModels;

        private DshSelect cboProvProtocol;

        private DshNumber numAiEdge;

        private TextBox lblAiHint;

        private Panel pageHost;

        private readonly List<Panel> pages = new List<Panel>();

        private DshSidebar sidebar;

        private Panel footerLine;

        private Panel statusLine;

        private Panel statusBar;

        private DshPanelToggle btnPanelToggle;

        private Panel panelToggleBar;

        private readonly List<DshNavItem> navItems = new List<DshNavItem>();

        private Font titleFont;

        private Font boldFont;

        private PictureBox picIcon;

        private Image brandImage;

        public bool AllowExit
        {
            get
            {
                return allowExit;
            }
            set
            {
                allowExit = value;
            }
        }

        public bool ChatPanelOpen
        {
            get
            {
                return panelOpen;
            }
        }

        public MainForm(TrayApp owner)
        {
            app = owner;
            cfg = owner.Settings;persistedSettings=Config.Copy(cfg);
            Dsh.ApplyTheme(cfg.UiTheme != "dark", null);
            pendingHotkey = cfg.Hotkey;
            BuildUi();
            BuildWindowChrome();
            LoadSettings();
            InitializeSettingsDrafts();
            SwitchPage(InitialPageFromArgs());
        }

        private static int InitialPageFromArgs()
        {
            try
            {
                string[] commandLineArgs = Environment.GetCommandLineArgs();
                foreach (string text in commandLineArgs)
                {
                    int num = text.IndexOf('=');
                    int result;
                    if (num > 0 && text.Substring(0, num).Trim().ToLowerInvariant() == "--page" && int.TryParse(text.Substring(num + 1).Trim(), out result) && result >= 0 && result <= 3)
                    {
                        return result;
                    }
                }
            }
            catch
            {
            }
            return 0;
        }

        private void BuildSidebar()
        {
            DshSidebar dshSidebar = new DshSidebar();
            dshSidebar.SetBounds(0, 0, 208, 910);
            dshSidebar.BackColor = Dsh.SidebarFlat;
            sidebar = dshSidebar;
            base.Controls.Add(dshSidebar);
            picIcon = new PictureBox();
            picIcon.SetBounds(22, 26, 28, 28);
            picIcon.SizeMode = PictureBoxSizeMode.Zoom;
            picIcon.BackColor = Color.Transparent;
            try
            {
                brandImage = app.AppIcon.ToBitmap();
                picIcon.Image = brandImage;
            }
            catch
            {
            }
            dshSidebar.Controls.Add(picIcon);
            DshLabel dshLabel = new DshLabel("截改 AI");
            dshLabel.OnDark = true;
            dshLabel.Font = Dsh.Brand;
            dshLabel.SetBounds(60, 28, 138, 24);
            dshSidebar.Controls.Add(dshLabel);
            DshLabel dshLabel2 = new DshLabel("区域截图工具  v" + Program.VersionText);
            dshLabel2.OnDark = true;
            dshLabel2.Small = true;
            dshLabel2.Tertiary = true;
            dshLabel2.SetBounds(22, 62, 168, 18);
            dshSidebar.Controls.Add(dshLabel2);
            string[] array = new string[8] { "快捷键", "截图与保存", "启动与后台", "AI 助手", "外观", "AI 请求", "最近截图", "语言" };
            int num = 104;
            for (int i = 0; i < array.Length; i++)
            {
                DshNavItem dshNavItem = new DshNavItem(UiText.T(array[i]));
                dshNavItem.SetBounds(14, num, 180, 36);
                dshNavItem.Tag2 = i;
                int index = i;
                dshNavItem.Click += delegate
                {
                    SwitchPage(index);
                };
                dshSidebar.Controls.Add(dshNavItem);
                navItems.Add(dshNavItem);
                num += 42;
            }
            DshButton dshButton = new DshButton("打开图片修改", DshButton.Kind.Primary);
            dshButton.SetBounds(12, dshSidebar.Height - 100, 182, 32);
            dshButton.Click += delegate
            {
                OpenImageForEditing();
            };
            dshButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            dshSidebar.Controls.Add(dshButton);
            DshButton dshButton2 = new DshButton("立即截图", DshButton.Kind.Primary);
            dshButton2.SetBounds(12, dshSidebar.Height - 62, 182, 40);
            dshButton2.Click += OnShotClick;
            dshButton2.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            dshSidebar.Controls.Add(dshButton2);
            DshButton fullscreenButton = new DshButton("全屏 · F11", DshButton.Kind.Ghost);
            fullscreenButton.SetBounds(12, dshSidebar.Height - 140, 182, 32);
            fullscreenButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            fullscreenButton.Click += delegate { ToggleFullscreen(); };
            dshSidebar.Controls.Add(fullscreenButton);
        }

        private void BuildContent()
        {
            pageHost = new Panel();
            pageHost.SetBounds(208, 0, 652, 810);
            pageHost.BackColor = Dsh.Canvas;
            base.Controls.Add(pageHost);
            BuildHotkeyPage();
            BuildSavePage();
            BuildStartupPage();
            BuildAiPage();
            BuildAppearancePage();
            BuildAiRequestPage();
            BuildRecentPage();
            BuildLanguagePage();
            BuildSettingsFeedback();
        }

        private Panel NewPage()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.AutoScrollMinSize = new Size(652, 810);
            panel.BackColor = Dsh.Canvas;
            panel.Visible = false;
            pageHost.Controls.Add(panel);
            pages.Add(panel);
            return panel;
        }

        private DshGlassCard NewCard(Panel page, int x, int y, int w, int h)
        {
            DshGlassCard dshGlassCard = new DshGlassCard();
            dshGlassCard.SetBounds(x, y, w, h);
            page.Controls.Add(dshGlassCard);
            dshGlassCard.ControlAdded += delegate(object sender, ControlEventArgs args)
            {
                if (args.Control.Width >= 400)
                    args.Control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            };
            return dshGlassCard;
        }

        private void PageHeader(Panel page, string title, string sub)
        {
            DshLabel dshLabel = new DshLabel(title);
            dshLabel.Font = Dsh.H1;
            dshLabel.SetBounds(32, 30, 588, 30);
            page.Controls.Add(dshLabel);
            DshLabel dshLabel2 = new DshLabel(sub);
            dshLabel2.Small = true;
            dshLabel2.Tertiary = true;
            dshLabel2.SetBounds(32, 60, 588, 20);
            page.Controls.Add(dshLabel2);
        }

        private void SwitchPage(int index)
        {
            currentSettingsPage=index;
            if (index == 6) RefreshRecentScreens();
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].Visible = i == index;
            }
            for (int j = 0; j < navItems.Count; j++)
            {
                navItems[j].Active = j == index;
            }
            if(windowBody!=null)LayoutChatPanel();
            SyncFooterSaveState();
        }

        private void BuildHotkeyPage() { BuildCaptureHotkeyPage(); }

        private void BuildSavePage()
        {
            Panel page = NewPage();
            PageHeader(page, UiText.T("截图与保存"), "决定截图存到哪里、叫什么名字、存成什么格式");
            int x = 32;
            int w = 588;
            int num = 24;
            int num2 = 540;
            DshGlassCard dshGlassCard = NewCard(page, x, 100, w, 132);
            DshLayout dshLayout = new DshLayout(dshGlassCard, num, 20, num2);
            dshLayout.FieldLabel("保存目录");
            txtDir = new DshTextInput();
            txtDir.Placeholder = "留空则使用「图片\\SnapGrab」";
            int num3 = 76;
            int num4 = 8;
            txtDir.SetBounds(num, dshLayout.Y, num2 - (num3 * 2 + num4 * 2), 38);
            dshGlassCard.Controls.Add(txtDir);
            DshButton dshButton = new DshButton("浏览…");
            dshButton.SetBounds(num + num2 - (num3 * 2 + num4 * 2) + num4, dshLayout.Y, num3, 38);
            dshButton.Click += OnBrowseClick;
            dshGlassCard.Controls.Add(dshButton);
            DshButton dshButton2 = new DshButton("默认位置");
            dshButton2.SetBounds(num + num2 - num3, dshLayout.Y, num3, 38);
            dshButton2.Click += OnDefaultDirClick;
            dshGlassCard.Controls.Add(dshButton2);
            dshLayout.Gap(38);
            DshButton dshButton3 = new DshButton(UiText.T("打开保存文件夹"));
            dshButton3.SetBounds(num, dshLayout.Y + 12, 144, 30);
            dshButton3.Click += OnOpenDirClick;
            dshGlassCard.Controls.Add(dshButton3);
            DshGlassCard dshGlassCard2 = NewCard(page, x, 248, w, 168);
            DshLayout dshLayout2 = new DshLayout(dshGlassCard2, num, 20, num2);
            dshLayout2.FieldLabel("文件名模板");
            txtPattern = new DshTextInput();
            dshLayout2.Put(txtPattern, 38);
            dshLayout2.Gap(8);
            dshLayout2.Hint("可用占位符：{yyyy}  {MM}  {dd}  {HH}  {mm}  {ss}  {fff}  {w}  {h}");
            dshLayout2.Gap(14);
            chkDateFolder = new DshToggle("按日期建子文件夹（如 2026-10-06）");
            chkDateFolder.SetBounds(num, dshLayout2.Y, num2, 26);
            dshGlassCard2.Controls.Add(chkDateFolder);
            DshGlassCard dshGlassCard3 = NewCard(page, x, 432, w, 160);
            DshLayout dshLayout3 = new DshLayout(dshGlassCard3, num, 20, num2);
            dshLayout3.FieldLabel("格式与质量");
            cboFormat = new DshSelect();
            cboFormat.Items.Add("PNG（无损，推荐）");
            cboFormat.Items.Add("JPG（体积更小）");
            cboFormat.SelectedIndexChanged += delegate
            {
                UpdateQualityEnabled();
            };
            cboFormat.SetBounds(num, dshLayout3.Y, 230, 38);
            dshGlassCard3.Controls.Add(cboFormat);
            DshLabel dshLabel = new DshLabel("JPG 质量");
            dshLabel.SetBounds(num + 248, dshLayout3.Y, 64, 38);
            dshGlassCard3.Controls.Add(dshLabel);
            numQuality = new DshNumber();
            numQuality.Minimum = 1;
            numQuality.Maximum = 100;
            numQuality.Value = 92;
            numQuality.SetBounds(num + 316, dshLayout3.Y, 80, 38);
            dshGlassCard3.Controls.Add(numQuality);
            lblQualityHint = new DshLabel("仅 JPG 有效");
            lblQualityHint.Small = true;
            lblQualityHint.Tertiary = true;
            lblQualityHint.SetBounds(num + 408, dshLayout3.Y, num2 - 408, 38);
            dshGlassCard3.Controls.Add(lblQualityHint);
            dshLayout3.Gap(52);
            chkClipboard = new DshToggle("截图后自动复制到剪贴板（可直接 Ctrl+V 粘贴）");
            chkClipboard.SetBounds(num, dshLayout3.Y, num2, 26);
            dshGlassCard3.Controls.Add(chkClipboard);
        }

        private void BuildAppearancePage()
        {
            Panel page = NewPage();
            PageHeader(page, UiText.T("外观"), "选择明亮或深色主题，保留扁平玻璃按钮与统一圆角");
            DshGlassCard card = NewCard(page, 32, 100, 588, 280);
            DshLabel label = new DshLabel(UiText.T("界面主题"));
            label.SetBounds(24, 22, 540, 26);
            card.Controls.Add(label);
            DshSelect theme = new DshSelect();appearanceTheme=theme;
            theme.Items.Add(UiText.T("明亮 · 雾白与浅蓝灰"));
            theme.Items.Add(UiText.T("深色 · 午夜蓝灰"));
            theme.SetBounds(24, 60, 540, 38);
            theme.SelectedIndex = cfg.UiTheme == "dark" ? 1 : 0;
            card.Controls.Add(theme);
            DshLabel hint = new DshLabel(UiText.T("切换立即预览；点击「保存设置」后，下次启动沿用此主题。"));
            hint.Small = true;
            hint.Tertiary = true;
            hint.SetBounds(24, 116, 540, 42);
            card.Controls.Add(hint);
            DshButton diagnostics=new DshButton(UiText.T("显示环境诊断"));diagnostics.SetBounds(24,176,320,36);
            diagnostics.Click+=delegate { ShowDisplayDiagnostics(); };card.Controls.Add(diagnostics);
            DshButton imageSettings=new DshButton("图片编辑服务设置");imageSettings.SetBounds(24,224,320,36);imageSettings.Click+=delegate {ImageEditSettings.Show(this,cfg);};card.Controls.Add(imageSettings);
            theme.SelectedIndexChanged += delegate
            {
                string previewTheme = theme.SelectedIndex == 1 ? "dark" : "light";
                SuspendLayout();
                try { Dsh.ApplyTheme(previewTheme == "light", this); }
                finally { ResumeLayout(false); }
                Invalidate(true);
                SetStatus("已预览" + (previewTheme == "light" ? "明亮" : "深色") + "主题，点击「保存设置」记住选择。");
            };
        }

        private void BuildLanguagePage()
        {
            Panel page=NewPage();
            PageHeader(page,UiText.T("语言"),UiText.T("选择界面语言，单独保存后重启生效。"));
            DshGlassCard card=NewCard(page,32,100,588,210);
            DshLabel languageLabel=new DshLabel(UiText.T("界面语言"));languageLabel.SetBounds(24,22,540,26);card.Controls.Add(languageLabel);
            DshSelect language=new DshSelect();appearanceLanguage=language;language.Items.Add("简体中文");language.Items.Add("English");language.Items.Add("Español");
            language.SetBounds(24,60,540,38);language.SelectedIndex=UiText.LanguageIndex(cfg.UiLanguage);card.Controls.Add(language);
            language.SelectedIndexChanged+=delegate {SetStatus("语言选择尚未保存，点击保存本页设置；保存后重启生效。");};
            DshLabel hint=new DshLabel(UiText.T("保存后重启生效；未翻译内容会回退英文或中文。"));
            hint.Small=true;hint.Tertiary=true;hint.SetBounds(24,116,540,64);card.Controls.Add(hint);
        }

        private void BuildStartupPage()
        {
            Panel page = NewPage();
            PageHeader(page, UiText.T("启动与后台"), "决定它怎么启动、怎么待命、占用多少资源");
            DshGlassCard dshGlassCard = NewCard(page, 32, 100, 588, 424);
            int x = 24;
            int width = 540;
            DshLayout dshLayout = new DshLayout(dshGlassCard, x, 20, width);
            dshLayout.Section("启动与驻留");
            chkAutostart = new DshToggle(UiText.T("开机自动启动（登录 Windows 后自动在右下角运行）"));
            chkAutostart.SetBounds(x, dshLayout.Y, width, 26);
            dshGlassCard.Controls.Add(chkAutostart);
            dshLayout.Gap(30);
            chkShowOnStart = new DshToggle(UiText.T("启动时显示本窗口"));
            chkShowOnStart.SetBounds(x, dshLayout.Y, width, 26);
            dshGlassCard.Controls.Add(chkShowOnStart);
            dshLayout.Gap(30);
            chkMinimizeToTray = new DshToggle(UiText.T("点击关闭按钮时最小化到托盘（不退出程序）"));
            chkMinimizeToTray.SetBounds(x, dshLayout.Y, width, 26);
            dshGlassCard.Controls.Add(chkMinimizeToTray);
            dshLayout.Gap(30);
            Panel panel = new Panel();
            panel.BackColor = Dsh.HairlineSoft;
            panel.SetBounds(x, 174, width, 1);
            dshGlassCard.Controls.Add(panel);
            DshLayout dshLayout2 = new DshLayout(dshGlassCard, x, panel.Top + 20, width);
            chkTrim = new DshToggle("空闲时回收内存，降低后台占用（打游戏 / 跑本地模型时更友好）");
            chkTrim.SetBounds(x, dshLayout2.Y, width, 26);
            dshGlassCard.Controls.Add(chkTrim);
            dshLayout2.Gap(30);
            dshLayout2.FieldLabel("进程优先级");
            cboPriority = new DshSelect();
            cboPriority.Items.Add(UiText.T("最低（idle）"));
            cboPriority.Items.Add(UiText.T("低于正常（省 CPU）"));
            cboPriority.Items.Add(UiText.T("正常（推荐）"));
            cboPriority.Items.Add(UiText.T("高于正常（极限响应）"));
            cboPriority.SetBounds(x, dshLayout2.Y, 240, 38);
            dshGlassCard.Controls.Add(cboPriority);
            dshLayout2.Gap(48);
            dshLayout2.Hint("UI 线程已单独提级，CPU 满载也能秒开遮罩；编码线程走最低优先级。");
        }

        private void BuildAiPage()
        {
            Panel page = NewPage();
            PageHeader(page, UiText.T("AI 助手"), "首次使用：快速配置向导 → 测试看图 → 保存；高级参数在 AI 请求页");
            int num = 24;
            int num2 = 540;
            DshGlassCard dshGlassCard = NewCard(page, 32, 100, 588, 204);
            DshLayout dshLayout = new DshLayout(dshGlassCard, num, 18, num2);
            dshLayout.FieldLabel("模型提供商");
            aiProviderHost = new Panel();
            aiProviderHost.BackColor = Color.Transparent;
            aiProviderHost.SetBounds(num, dshLayout.Y, num2, 72);
            aiProviderHost.AutoScroll = true;
            dshGlassCard.Controls.Add(aiProviderHost);
            dshLayout.Gap(82);
            DshButton dshButton = new DshButton("＋ 新增提供商");
            dshButton.SetBounds(num, dshLayout.Y, 130, 32);
            dshButton.Click += delegate
            {
                OnAddProvider();
            };
            dshGlassCard.Controls.Add(dshButton);
            DshButton dshButton2 = new DshButton("从预设填充…");
            dshButton2.SetBounds(num + 138, dshLayout.Y, 126, 32);
            dshButton2.Text = UiText.T("快速配置向导");
            dshButton2.Click += delegate { ShowAiSetupWizard(); };
            dshGlassCard.Controls.Add(dshButton2);
            DshButton dshButton3 = new DshButton(UiText.T("删除"));
            dshButton3.SetBounds(num + 272, dshLayout.Y, 72, 32);
            dshButton3.Click += delegate
            {
                OnDeleteProvider();
            };
            dshGlassCard.Controls.Add(dshButton3);
            DshButton dshButton4 = new DshButton("打开 AI 对话 →", DshButton.Kind.Primary);
            dshButton4.SetBounds(num + 354, dshLayout.Y, 146, 32);
            dshButton4.Click += delegate
            {
                ToggleChatPanel();
            };
            dshGlassCard.Controls.Add(dshButton4);
            dshLayout.Gap(38);

            int num3 = 336;
            int h = 810 - num3 - 14;
            DshGlassCard dshGlassCard2 = NewCard(page, 32, num3, 588, h);
            providerDetails = dshGlassCard2;
            providerDetailsMotion.Jump(1);
            providerDetailsToggle = new DshButton("收起模型设置");
            providerDetailsToggle.SetBounds(440, 308, 180, 24);
            providerDetailsToggle.Click += delegate { SetProviderDetails(!providerDetailsOpen); };
            page.Controls.Add(providerDetailsToggle);
            providerDetailsTimer = new System.Windows.Forms.Timer();
            providerDetailsTimer.Interval = 15;
            providerDetailsTimer.Tick += delegate
            {
                double seconds = providerDetailsWatch.Elapsed.TotalSeconds;
                providerDetailsWatch.Restart();
                providerDetailsMotion.Advance(seconds);
                ApplyProviderDetailsFrame();
            };
            DshLayout dshLayout2 = new DshLayout(dshGlassCard2, num, 14, num2);
            dshLayout2.FieldLabel("显示名称 / 标识");
            int num4 = (num2 - 12) / 2;
            txtProvName = new DshTextInput();
            txtProvName.Placeholder = "显示名称，例如 百炼-通义千问";
            txtProvName.SetBounds(num, dshLayout2.Y, num4, 36);
            dshGlassCard2.Controls.Add(txtProvName);
            txtProvId = new DshTextInput();
            txtProvId.Placeholder = "标识（小写字母开头），例如 bailian";
            txtProvId.SetBounds(num + num4 + 12, dshLayout2.Y, num4, 36);
            dshGlassCard2.Controls.Add(txtProvId);
            dshLayout2.Gap(48);
            dshLayout2.FieldLabel("API 地址");
            txtProvEndpoint = new DshTextInput();
            txtProvEndpoint.Placeholder = "https://…/v1/chat/completions";
            dshLayout2.Put(txtProvEndpoint, 36);
            dshLayout2.Gap(12);
            dshLayout2.FieldLabel("API 协议");
            cboProvProtocol = new DshSelect();
            cboProvProtocol.Items.Add("OpenAI Chat Completions");
            cboProvProtocol.Items.Add("Anthropic Messages");
            cboProvProtocol.SetBounds(num, dshLayout2.Y, 160, 36);
            dshGlassCard2.Controls.Add(cboProvProtocol);
            DshButton dshButton5 = new DshButton(UiText.T("测试连接"), DshButton.Kind.Primary);
            dshButton5.SetBounds(num + 168, dshLayout2.Y, 106, 36);
            dshButton5.Click += OnTestAiClick;
            dshGlassCard2.Controls.Add(dshButton5);
            DshButton fetch = new DshButton("获取模型");
            fetch.SetBounds(num+282,dshLayout2.Y,118,36);
            fetch.Click += delegate { BeginProviderAction(false,fetch); }; dshGlassCard2.Controls.Add(fetch);
            DshButton vision = new DshButton(UiText.T("测试看图"));
            vision.SetBounds(num+408,dshLayout2.Y,132,36);
            vision.Click += delegate { BeginProviderAction(true,vision); }; dshGlassCard2.Controls.Add(vision);

            lblAiHint = new TextBox();
            lblAiHint.Multiline = true;
            lblAiHint.ReadOnly = true;
            lblAiHint.ScrollBars = ScrollBars.Vertical;
            lblAiHint.BorderStyle = BorderStyle.None;
            lblAiHint.Font = Dsh.Small;
            lblAiHint.BackColor = Dsh.GlassFill;
            lblAiHint.ForeColor = Dsh.TextSecondary;
            lblAiHint.Text = "尚未测试：填入密钥和模型后，点击测试连接。";
            DshInputFrame testResultFrame = new DshInputFrame(lblAiHint);
            testResultFrame.SetBounds(num, dshLayout2.Y + 42, num2, 42);
            dshGlassCard2.Controls.Add(testResultFrame);
            dshLayout2.Gap(88);
            dshLayout2.FieldLabel("API 密钥");
            txtProvKey = new DshTextInput();
            txtProvKey.Inner.UseSystemPasswordChar = true;
            txtProvKey.Placeholder = "sk-…（用 Windows DPAPI 加密后存本机配置）";
            dshLayout2.Put(txtProvKey, 36);
            dshLayout2.Gap(12);
            DshLabel dshLabel2 = new DshLabel("模型列表 / 默认模型");
            dshLabel2.Medium = true;
            dshLabel2.SetBounds(num, dshLayout2.Y, num2 - 250, 20);
            dshGlassCard2.Controls.Add(dshLabel2);
            DshLabel dshLabel3 = new DshLabel("右侧选择默认模型");
            dshLabel3.Small = true;
            dshLabel3.Tertiary = true;
            dshLabel3.SetBounds(num + 350, dshLayout2.Y, num2 - 350, 20);
            dshGlassCard2.Controls.Add(dshLabel3);
            dshLayout2.Gap(24);
            txtProvModels = new DshTextInput();
            txtProvModels.Placeholder = "qwen-vl-max, qwen-vl-plus";
            txtProvModels.SetBounds(num, dshLayout2.Y, 340, 34);
            dshGlassCard2.Controls.Add(txtProvModels);
            cboDefaultModel = new DshSelect();
            cboDefaultModel.SetBounds(num + 350, dshLayout2.Y, num2 - 350, 34);
            dshGlassCard2.Controls.Add(cboDefaultModel);
            txtProvModels.Inner.TextChanged += delegate { RefreshDefaultModels(null); };

            // Both model controls use SetBounds rather than Put: advance past the row.
            dshLayout2.Gap(txtProvModels.Height + 10);
            DshLabel dshLabel4 = new DshLabel("发给模型的图片长边上限");
            dshLabel4.Medium = true;
            dshLabel4.SetBounds(num, dshLayout2.Y, 190, 30);
            dshGlassCard2.Controls.Add(dshLabel4);
            numAiEdge = new DshNumber();
            numAiEdge.Minimum = 512;
            numAiEdge.Maximum = 4096;
            numAiEdge.Value = 1600;
            numAiEdge.SetBounds(num + 196, dshLayout2.Y, 88, 30);
            dshGlassCard2.Controls.Add(numAiEdge);
            DshLabel dshLabel5 = new DshLabel("像素 · 越小越快越省，坐标会自动换算回原图");
            dshLabel5.Small = true;
            dshLabel5.Tertiary = true;
            dshLabel5.SetBounds(num + 292, dshLayout2.Y, Math.Max(60, num2 - 292), 30);
            dshGlassCard2.Controls.Add(dshLabel5);
        }

        private void RebuildProviderList()
        {
            if (aiProviderHost == null)
            {
                return;
            }
            while (aiProviderHost.Controls.Count > 0) aiProviderHost.Controls[0].Dispose();
            providerRows.Clear();
            if (cfg.AiProviders.Count == 0)
            {
                DshLabel dshLabel = new DshLabel("还没有提供商。点「从预设填充…」快速建一个，或「＋ 新增提供商」。");
                dshLabel.Small = true;
                dshLabel.Tertiary = true;
                dshLabel.SetBounds(2, 8, Math.Max(100, aiProviderHost.Width - 26), 26);
                aiProviderHost.Controls.Add(dshLabel);
                FillProviderForm(null);
                return;
            }
            int num = 0;
            foreach (AiProvider aiProvider2 in cfg.AiProviders)
            {
                AiProviderRow aiProviderRow = new AiProviderRow();
                aiProviderRow.SetBounds(0, num, Math.Max(100, aiProviderHost.Width - 26), 44);
                aiProviderRow.Bind(aiProvider2, string.Equals(aiProvider2.Id, cfg.AiActiveProviderId, StringComparison.OrdinalIgnoreCase));
                AiProvider captured = aiProvider2;
                aiProviderRow.EditRequested += delegate { SelectProvider(captured); SetProviderDetails(true); };
                aiProviderRow.Click += delegate
                {
                    SelectProvider(captured);
                };
                aiProviderHost.Controls.Add(aiProviderRow);
                providerRows.Add(aiProviderRow);
                num += 48;
            }
            AiProvider aiProvider = cfg.ActiveProvider();
            if (aiProvider != null)
            {
                FillProviderForm(aiProvider);
            }
        }

        private void SelectProvider(AiProvider p)
        {
            if (p == null)
            {
                return;
            }
            AiProvider current = cfg.ActiveProvider();
            if (current != null && current != p && !CommitProviderForm()) return;
            cfg.AiActiveProviderId = p.Id;
            foreach (AiProviderRow providerRow in providerRows)
            {
                providerRow.SetActive(string.Equals(providerRow.Provider.Id, p.Id, StringComparison.OrdinalIgnoreCase));
            }
            FillProviderForm(p);
            SetProviderDetails(true);
            SetStatus("当前使用：「" + p.Name + "」");
        }

        private void RefreshDefaultModels(string preferred)
        {
            if (cboDefaultModel == null) return;
            string selected = preferred ?? (cboDefaultModel.SelectedIndex >= 0 ? Convert.ToString(cboDefaultModel.Items[cboDefaultModel.SelectedIndex]) : "");
            cboDefaultModel.Items.Clear();
            foreach (string entry in txtProvModels.Text.Split(new char[] { ',', '，', ';', '；', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                if (!cboDefaultModel.Items.Contains(entry.Trim())) cboDefaultModel.Items.Add(entry.Trim());
            int index = cboDefaultModel.Items.IndexOf(selected);
            cboDefaultModel.SelectedIndex = index >= 0 ? index : (cboDefaultModel.Items.Count > 0 ? 0 : -1);
        }

        private void FillProviderForm(AiProvider p)
        {
            if (txtProvName != null)
            {
                if (p == null)
                {
                    txtProvName.Text = "";
                    txtProvId.Text = "";
                    txtProvEndpoint.Text = "";
                    txtProvKey.Text = "";
                    txtProvModels.Text = "";
                    cboProvProtocol.SelectedIndex = 0;
                }
                else
                {
                    txtProvName.Text = p.Name;
                    txtProvId.Text = p.Id;
                    txtProvEndpoint.Text = p.Endpoint;
                    txtProvKey.Text = p.ApiKey;
                    txtProvModels.Text = string.Join(", ", p.Models.ToArray());
                    cboProvProtocol.SelectedIndex = (p.IsAnthropic ? 1 : 0);
                    RefreshDefaultModels(p.FirstModel);
                }
            }
        }

        private bool CommitProviderForm()
        {
            AiProvider aiProvider = cfg.ActiveProvider();
            if (aiProvider == null)
            {
                return true;
            }
            string text = txtProvId.Text.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, "^[a-z][a-z0-9_-]{0,63}$"))
            { SetStatus("提供商标识需以小写字母开头，只含字母、数字、下划线或短横线。"); return false; }
            foreach (AiProvider other in cfg.AiProviders)
                if (other != aiProvider && string.Equals(other.Id, text, StringComparison.OrdinalIgnoreCase))
                { SetStatus("提供商标识已存在，请换一个。"); return false; }
            aiProvider.Id = text;
            cfg.AiActiveProviderId = text;
            aiProvider.Name = txtProvName.Text.Trim();
            if (string.IsNullOrEmpty(aiProvider.Name))
            {
                aiProvider.Name = aiProvider.Id;
            }
            aiProvider.Endpoint = txtProvEndpoint.Text.Trim();
            aiProvider.ApiKey = txtProvKey.Text.Trim();
            aiProvider.Protocol = ((cboProvProtocol.SelectedIndex == 1) ? "anthropic" : "openai");
            aiProvider.Models.Clear();
            string[] array = txtProvModels.Text.Split(new char[] { ',', '，', ';', '；', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string text2 in array)
            {
                string text3 = text2.Trim();
                if (text3.Length > 0 && !aiProvider.Models.Contains(text3))
                {
                    aiProvider.Models.Add(text3);
                }
            }
            aiProvider.SelectedModel = cboDefaultModel.SelectedIndex >= 0 ? Convert.ToString(cboDefaultModel.Items[cboDefaultModel.SelectedIndex]) : "";
            aiProvider.Endpoint = AiProvider.NormalizeEndpoint(aiProvider.Endpoint, aiProvider.IsAnthropic);
            txtProvEndpoint.Text = aiProvider.Endpoint;
            return true;
        }

        private void OnAddProvider()
        {
            if (!CommitProviderForm()) return;
            AiProvider aiProvider = new AiProvider();
            int num = 1;
            while (cfg.AiProviders.Exists(delegate(AiProvider candidate) { return candidate.Id == "provider" + num; })) num++;
            aiProvider.Id = "provider" + num;
            aiProvider.Name = "新提供商 " + num;
            aiProvider.Protocol = "openai";
            cfg.AiProviders.Add(aiProvider);
            cfg.AiActiveProviderId = aiProvider.Id;
            RebuildProviderList();
            SelectProvider(aiProvider);
            SetStatus("已新增提供商，请填写地址、密钥与模型。");
        }

        private void OnDeleteProvider()
        {
            AiProvider aiProvider = cfg.ActiveProvider();
            if (aiProvider == null)
            {
                SetStatus("没有可删除的提供商。");
                return;
            }
            cfg.AiProviders.Remove(aiProvider);
            cfg.AiActiveProviderId = ((cfg.AiProviders.Count > 0) ? cfg.AiProviders[0].Id : "");
            RebuildProviderList();
            SetStatus("已删除提供商。");
        }

        private void OnFillFromPreset()
        {
            if (!CommitProviderForm()) return;
            ThemedDialog dlg = new ThemedDialog();
            try
            {
                dlg.Text = "从预设填充";
                dlg.MinimumSize = new Size(440,312);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(440, 312);
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Dsh.Canvas;
                dlg.Font = Dsh.Body;
                DshLabel dshLabel = new DshLabel("选择要接入的服务，会自动填好地址与模型名（密钥仍需你自己填）：");
                dshLabel.Small = true;
                dshLabel.Tertiary = true;
                dshLabel.SetBounds(20, 18, 400, 20);
                dlg.ContentArea.Controls.Add(dshLabel);
                DshSelect sel = new DshSelect();
                AiPreset[] presets = AiClient.Presets;
                foreach (AiPreset aiPreset in presets)
                {
                    sel.Items.Add(aiPreset.Name);
                }
                sel.SelectedIndex = 0;
                sel.SetBounds(20, 44, 400, 38);
                dlg.ContentArea.Controls.Add(sel);
                DshLabel hint = new DshLabel(AiClient.Presets[0].Hint);
                hint.Small = true;
                hint.Tertiary = true;
                hint.SetBounds(20, 92, 400, 62);
                dlg.ContentArea.Controls.Add(hint);
                sel.SelectedIndexChanged += delegate
                {
                    int selectedIndex2 = sel.SelectedIndex;
                    if (selectedIndex2 >= 0)
                    {
                        hint.Text = AiClient.Presets[selectedIndex2].Hint;
                    }
                };
                DshButton dshButton = new DshButton("填充", DshButton.Kind.Primary);
                dshButton.SetBounds(20, 214, 100, 38);
                dshButton.Click += delegate
                {
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.ContentArea.Controls.Add(dshButton);
                DshButton dshButton2 = new DshButton(UiText.T("取消"));
                dshButton2.SetBounds(128, 214, 100, 38);
                dshButton2.Click += delegate
                {
                    dlg.DialogResult = DialogResult.Cancel;
                    dlg.Close();
                };
                dlg.ContentArea.Controls.Add(dshButton2);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                int selectedIndex = sel.SelectedIndex;
                if (selectedIndex >= 0)
                {
                    AiPreset aiPreset2 = AiClient.Presets[selectedIndex];
                    AiProvider aiProvider = cfg.ActiveProvider();
                    if (aiProvider == null)
                    {
                        aiProvider = new AiProvider();
                        aiProvider.Id = "provider1";
                        cfg.AiProviders.Add(aiProvider);
                        cfg.AiActiveProviderId = aiProvider.Id;
                    }
                    aiPreset2.Fill(aiProvider);
                    SetProviderDetails(true);
                    RebuildProviderList();
                    SelectProvider(aiProvider);
                    SetStatus("已按预设「" + aiPreset2.Name + "」填充，请检查密钥、选择模型并保存设置。");
                }
            }
            finally
            {
                if (dlg != null)
                {
                    ((IDisposable)dlg).Dispose();
                }
            }
        }

        private void BuildFooter()
        {
            int num = 810;
            footerLine = new Panel();
            footerLine.BackColor = Dsh.HairlineSoft;
            footerLine.SetBounds(208, num, WinW - 208, 1);
            base.Controls.Add(footerLine);
            int num2 = 240;
            DshButton dshButton = new DshButton("保存本页设置", DshButton.Kind.Primary);
            dshButton.SetBounds(num2, num + 18, 132, 38);
            dshButton.Click += OnSaveClick;
            base.Controls.Add(dshButton);settingsFooterButtons.Add(dshButton);
            DshButton dshButton2 = new DshButton("最小化到托盘");
            dshButton2.SetBounds(num2 + 144, num + 18, 136, 38);
            dshButton2.Click += delegate
            {
                HideMe();
            };
            base.Controls.Add(dshButton2);settingsFooterButtons.Add(dshButton2);
            DshButton dshButton3 = new DshButton("退出程序", DshButton.Kind.Danger);
            dshButton3.SetBounds(num2 + 294, num + 18, 110, 38);
            dshButton3.Click += delegate
            {
                if(ConfirmDiscardSettingsDrafts())app.ExitApp();
            };
            base.Controls.Add(dshButton3);settingsFooterButtons.Add(dshButton3);
            statusLine = new Panel();
            statusLine.BackColor = Dsh.HairlineSoft;
            statusLine.SetBounds(208, num + 70, WinW - 208, 1);
            base.Controls.Add(statusLine);
            statusBar = new Panel();
            statusBar.SetBounds(208, num + 70 + 1, WinW - 208, 29);
            statusBar.BackColor = Dsh.Canvas;
            base.Controls.Add(statusBar);
            lblStatus = new DshLabel("");
            lblStatus.Small = true;
            lblStatus.Tertiary = true;
            lblStatus.SetBounds(32, 0, WinW - 208 - 64, 29);
            statusBar.Controls.Add(lblStatus);
        }

        private void BuildUi()
        {
            SuspendLayout();
            base.AutoScaleDimensions = new SizeF(96f, 96f);
            base.AutoScaleMode = AutoScaleMode.Dpi;
            base.FormBorderStyle = FormBorderStyle.None;
            base.MaximizeBox = true;
            MinimumSize = new Size(740, 660);
            KeyPreview = true;
            base.MinimizeBox = true;
            base.StartPosition = FormStartPosition.CenterScreen;
            base.ClientSize = new Size(860, 910);
            Text = "截改 AI SnipRefine · 截图与 AI  v" + Program.VersionText;
            BackColor = Dsh.Canvas;
            base.Icon = app.AppIcon;
            titleFont = Dsh.H1;
            boldFont = Dsh.Medium;
            BuildSidebar();
            BuildContent();
            BuildFooter();
            foreach (Control control in Controls)
                if (control.Top >= 810) control.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            BuildPanelToggle();
            BuildChatPanel();
            if (chatPanel != null)
            {
                chatPanel.BringToFront();
            }
            if (btnPanelToggle != null)
            {
                btnPanelToggle.BringToFront();
            }
            LayoutPanelToggle();
            ResumeLayout(false);
        }

        private void BuildPanelToggle()
        {
            panelToggleBar = new ToggleBar();
            panelToggleBar.BackColor = Color.Transparent;
            base.Controls.Add(panelToggleBar);
            btnPanelToggle = new DshPanelToggle();
            btnPanelToggle.Click += delegate
            {
                ToggleChatPanel();
            };
            panelToggleBar.Controls.Add(btnPanelToggle);
        }

        private void LayoutPanelToggle()
        {
            if (btnPanelToggle != null && panelToggleBar != null)
            {
                int width = windowBody == null ? ClientSize.Width : windowBody.ClientSize.Width;
                panelToggleBar.SetBounds(Math.Max(208, width - 52), 0, 52, 48);
                btnPanelToggle.SetBounds(6, 5, 38, 38);
                btnPanelToggle.Active = panelOpen;
                panelToggleBar.Invalidate();
                EnsureAboveContent();
            }
        }

        private void EnsureAboveContent()
        {
            if (panelToggleBar == null || pageHost == null)
            {
                return;
            }
            Rectangle bounds = panelToggleBar.Bounds;
            Rectangle bounds2 = pageHost.Bounds;
            if (bounds.IntersectsWith(bounds2))
            {
                panelToggleBar.BringToFront();
                if (btnPanelToggle != null)
                {
                    btnPanelToggle.BringToFront();
                }
            }
        }

        private void BuildChatPanel()
        {
            chatPanel = new ChatPanel(app, cfg);
            chatPanel.SetBounds(860, 0, 406, 910);
            chatPanel.Visible = false;
            chatPanel.CloseRequested += delegate
            {
                SetChatPanel(false);
            };
            base.Controls.Add(chatPanel);
            chatPanel.BringToFront();
            var transition = new SidebarTransition();
            transition.SetBounds(0, 0, 12, chatPanel.Height);
            transition.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            chatPanel.Controls.Add(transition);
            transition.BringToFront();
            animeTimer = new System.Windows.Forms.Timer();
            animeTimer.Interval = 15;
            animeTimer.Tick += OnAnimeTick;
        }

        public void SetChatPanel(bool open)
        {
            if (chatPanel == null || (open == panelOpen && !panelAnimating)) return;
            panelOpen = open;
            if (open)
            {
                chatPanel.Visible = true;
                chatPanel.RefreshModel();
                chatPanel.BringToFront();
            }
            // No text fade: native child controls cannot share a true parent opacity.
            chatPanel.FadeProgress = 1f;
            if (!GlassSurface.MotionEnabled || !Visible)
            {
                animeTimer.Stop();
                animeWatch.Stop();
                panelMotion.Jump(open ? 1 : 0);
                panelAnimating = false;
                ApplyChatPanelFrame((float)panelMotion.Position);
                chatPanel.Visible = open;
                if (open && Visible) chatPanel.FocusInput();
                return;
            }
            panelMotion.MoveTo(open ? 1 : 0);
            panelAnimating = true;
            animeWatch.Restart();
            animeTimer.Start();
        }

        public void ToggleChatPanel() { SetChatPanel(!panelOpen); }

        private void OnAnimeTick(object sender, EventArgs e)
        {
            double seconds = animeWatch.Elapsed.TotalSeconds;
            animeWatch.Restart();
            panelMotion.Advance(seconds);
            ApplyChatPanelFrame((float)panelMotion.Position);
            if (!panelMotion.Moving)
            {
                animeTimer.Stop();
                animeWatch.Stop();
                panelAnimating = false;
                chatPanel.Visible = panelOpen;
                if (panelOpen) chatPanel.FocusInput();
            }
        }

        internal static float CubicBezier(float[] p, float x)
        {
            if (x <= 0f)
            {
                return 0f;
            }
            if (x >= 1f)
            {
                return 1f;
            }
            float num = 0f;
            float num2 = 1f;
            float num3 = x;
            for (int i = 0; i < 24; i++)
            {
                num3 = (num + num2) / 2f;
                float num4 = Bez(p[0], p[2], num3);
                if (num4 < x)
                {
                    num = num3;
                }
                else
                {
                    num2 = num3;
                }
            }
            return Bez(p[1], p[3], num3);
        }

        private static float Bez(float c1, float c2, float t)
        {
            float num = 1f - t;
            return 3f * num * num * t * c1 + 3f * num * t * t * c2 + t * t * t;
        }

        private bool fullscreen;
        private Rectangle beforeFullscreenBounds;
        private FormWindowState beforeFullscreenState;

        private void ToggleFullscreen()
        {
            if (!fullscreen)
            {
                beforeFullscreenState = WindowState;
                beforeFullscreenBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                fullscreen = true;
                RecreateHandle();
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.FromControl(this).Bounds;
            }
            else
            {
                fullscreen = false;
                RecreateHandle();
                MinimumSize = new Size(740, 660);
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = beforeFullscreenBounds;
                WindowState = beforeFullscreenState;
            }
            LayoutChatPanel();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1) { ShowUserGuide(); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.KeyCode == Keys.F11 || (fullscreen && e.KeyCode == Keys.Escape))
            { ToggleFullscreen(); e.Handled = true; e.SuppressKeyPress = true; return; }
            base.OnKeyDown(e);
        }

        private void ApplyChatPanelFrame(float progress)
        {
            LayoutChatPanel();
        }

        private void LayoutChatPanel()
        {
            int width = windowBody == null ? ClientSize.Width : windowBody.ClientSize.Width;
            int height = windowBody == null ? ClientSize.Height : windowBody.ClientSize.Height;
            if (width <= 0 || height <= 0) return;
            WinW = width;
            int chatWidth = (int)Math.Round(406 * panelMotion.Position);
            // Narrow windows use an overlay drawer; wide windows share the available area.
            // The containing window never changes size when the drawer is toggled.
            int settingsWidth = Math.Max(1, width - 208 - (width >= 1266 ? chatWidth : 0));
            if (sidebar != null) sidebar.SetBounds(0, 0, 208, height);
            if (pageHost != null) pageHost.SetBounds(208, 0, settingsWidth, Math.Max(1, height - 100));
            if (chatPanel != null) chatPanel.SetBounds(width - chatWidth, 0, 406, height);
            if (footerLine != null) footerLine.SetBounds(208, height - 100, settingsWidth, 1);
            if (statusLine != null) statusLine.SetBounds(208, height - 30, settingsWidth, 1);
            if (statusBar != null) statusBar.SetBounds(208, height - 29, settingsWidth, 29);
            if (lblStatus != null) lblStatus.SetBounds(20, 0, Math.Max(1, settingsWidth - 40), 29);
            foreach (Panel page in pages)
            {
                int index=pages.IndexOf(page);
                int viewportWidth=Math.Max(1,page.ClientSize.Width-SystemInformation.VerticalScrollBarWidth);
                int layoutWidth=Math.Max(652,Math.Min(900,viewportWidth));
                int inset=Math.Max(0,(viewportWidth-layoutWidth)/2);
                if(savedSections[index]!=null){savedSections[index].Width=layoutWidth;savedSections[index].Left=inset+page.AutoScrollPosition.X;page.AutoScrollMinSize=new Size(652,100+savedSections[index].Height+16);}
                if(pageSaveButtons[index]!=null){int saveX=inset+Math.Min(layoutWidth-212,Math.Max(220,page.ClientSize.Width-inset-264));pageSaveButtons[index].SetBounds(saveX+page.AutoScrollPosition.X,24+page.AutoScrollPosition.Y,180,30);}
                foreach(Control header in page.Controls)if(header is DshLabel){header.Left=inset+32+page.AutoScrollPosition.X;if(header.Top==30+page.AutoScrollPosition.Y)header.Width=layoutWidth-64;else header.Width=layoutWidth-64;}
                int contentWidth = layoutWidth - 64;
                foreach (Control control in savedSections[pages.IndexOf(page)]==null ? page.Controls : savedSections[pages.IndexOf(page)].Body.Controls)
                {
                    if (control is DshGlassCard) control.Width = contentWidth;
                    else if (control is DshLabel && control.Left == 32 && control.Width >= 588)
                        control.Width = contentWidth;
                }
            }
            if(settingsFooterButtons.Count==3){
                Panel activePage=pages[currentSettingsPage];
                int viewportWidth=Math.Max(1,activePage.ClientSize.Width-SystemInformation.VerticalScrollBarWidth);
                int areaWidth=Math.Max(652,Math.Min(900,viewportWidth));
                int areaLeft=208+Math.Max(0,(viewportWidth-areaWidth)/2)+32;
                bool compact=activePage.ClientSize.Width<600;
                int trayWidth=compact?96:136,exitWidth=compact?70:110;
                settingsFooterButtons[1].Text=compact?"托盘":"最小化到托盘";
                settingsFooterButtons[2].Text=compact?"退出":"退出程序";
                settingsFooterButtons[1].SetBounds(areaLeft,height-82,trayWidth,38);
                settingsFooterButtons[2].SetBounds(areaLeft+trayWidth+12,height-82,exitWidth,38);
                settingsFooterButtons[0].SetBounds(208+pageSaveButtons[currentSettingsPage].Left-activePage.AutoScrollPosition.X,height-82,180,38);
                foreach(DshButton button in settingsFooterButtons)button.BringToFront();
            }
            // Keep the drawer above footer actions after every layout frame.
            if(chatPanel!=null&&chatWidth>0)chatPanel.BringToFront();
            if(panelToggleBar!=null)panelToggleBar.BringToFront();
            LayoutPanelToggle();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (pageHost != null) LayoutChatPanel();
        }

        private void LoadSettings()
        {
            txtHotkey.Text = ShowHotkey(cfg.Hotkey);
            txtQuickCapture.Text = cfg.QuickCaptureHotkey; txtFullscreenCapture.Text = cfg.FullscreenCaptureHotkey;
            pendingHotkey = cfg.Hotkey;
            txtDir.Text = cfg.EffectiveSaveDir();
            txtPattern.Text = cfg.FilenamePattern;
            cboFormat.SelectedIndex = ((cfg.Format == "jpg" || cfg.Format == "jpeg") ? 1 : 0);
            int num = cfg.Quality;
            if (num < numQuality.Minimum)
            {
                num = numQuality.Minimum;
            }
            if (num > numQuality.Maximum)
            {
                num = numQuality.Maximum;
            }
            numQuality.Value = num;
            chkClipboard.Checked = cfg.CopyToClipboard;
            chkDateFolder.Checked = cfg.DateSubfolder;
            chkAutostart.Checked = cfg.StartWithWindows;
            chkShowOnStart.Checked = cfg.ShowWindowOnStart;
            chkMinimizeToTray.Checked = cfg.MinimizeToTray;
            chkTrim.Checked = cfg.TrimWorkingSet;
            cboPriority.SelectedIndex = PriorityIndex(cfg.ProcessPriority);
            UpdateQualityEnabled();
            int num2 = cfg.AiImageMaxEdge;
            if (num2 < numAiEdge.Minimum)
            {
                num2 = numAiEdge.Minimum;
            }
            if (num2 > numAiEdge.Maximum)
            {
                num2 = numAiEdge.Maximum;
            }
            numAiEdge.Value = num2;
            RebuildProviderList();
            if (app.HotkeyRegistered)
            {
                lblHotkeyState.Text = "✔ 已生效：" + app.HotkeyText + "\u3000直接按下这个组合键即可截图";
                lblHotkeyState.ForeColor = Dsh.SuccessText;
            }
            else
            {
                lblHotkeyState.Text = "✘ 启动时注册失败（可能被占用），请在下面重新录制一个组合键";
                lblHotkeyState.ForeColor = Dsh.DangerText;
            }
            string b = cfg.EffectiveSaveDir();
            string text = cfg.ResolveSaveDir();
            if (string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("就绪。截图保存到：" + text);
            }
            else
            {
                SetStatus("注意：设定目录不可写，截图将自动保存到：" + text);
            }
        }

        private static string ShowHotkey(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            return text.Replace("+", " + ");
        }

        private void UpdateQualityEnabled()
        {
            bool flag = cboFormat.SelectedIndex == 1;
            numQuality.Enabled2 = flag;
            lblQualityHint.Text = (flag ? "仅 JPG 有效" : "");
            lblQualityHint.ForeColor = Dsh.TextTertiary;
        }

        private void SetHotkeyHint()
        {
            if (!(lblHotkeyState.ForeColor == Dsh.DangerText))
            {
                lblHotkeyState.Text = "请直接按下新的组合键（必须包含 Ctrl / Shift / Alt 中的至少一个）";
                lblHotkeyState.ForeColor = Dsh.TextPrimary;
            }
        }

        internal void RefreshStatus(string text)
        {
            SetStatus(text);
        }

        private void SetStatus(string text)
        {
            if (lblStatus != null)
            {
                lblStatus.Text = UiText.T(text);
            }
        }

        private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            string text;
            if (Config.FormatHotkey(e.KeyData, out text))
            {
                pendingHotkey = text;
                txtHotkey.Text = ShowHotkey(text);
                lblHotkeyState.Text = "已录制：" + ShowHotkey(text);
                lblHotkeyState.ForeColor = Dsh.TextPrimary;
                SetStatus("已录制新快捷键，记得点击「保存设置」。");
            }
            else
            {
                Keys keys = e.KeyData & Keys.KeyCode;
                if (keys != Keys.None && keys != Keys.ControlKey && keys != Keys.ShiftKey && keys != Keys.Menu && keys != Keys.LWin && keys != Keys.RWin)
                {
                    lblHotkeyState.Text = "该按键不支持（需带 Ctrl/Shift/Alt，且主键为字母/数字/F1-F12/方向键等）";
                    lblHotkeyState.ForeColor = Dsh.DangerText;
                }
            }
        }

        private void OnResetHotkeyClick(object sender, EventArgs e)
        {
            pendingHotkey = "Ctrl+Alt+A";
            txtHotkey.Text = ShowHotkey(pendingHotkey);
            lblHotkeyState.Text = "已恢复默认：Ctrl + Alt + A";
            lblHotkeyState.ForeColor = Dsh.TextPrimary;
        }

        private void OnSaveClick(object sender, EventArgs e) {SaveSettingsPage(currentSettingsPage);}

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "选择截图保存文件夹";
                folderBrowserDialog.ShowNewFolderButton = true;
                try
                {
                    if (!string.IsNullOrEmpty(txtDir.Text) && Directory.Exists(txtDir.Text))
                    {
                        folderBrowserDialog.SelectedPath = txtDir.Text;
                    }
                }
                catch
                {
                }
                if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtDir.Text = folderBrowserDialog.SelectedPath;
                    SetStatus("保存目录已更改，点击「保存设置」生效。");
                }
            }
        }

        private void OnDefaultDirClick(object sender, EventArgs e)
        {
            string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SnapGrab");
            if (string.IsNullOrEmpty(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SnapGrab");
            }
            txtDir.Text = text;
            SetStatus("已填回默认目录，点击「保存设置」生效：" + text);
        }

        private static int PriorityIndex(string text)
        {
            switch (Config.ParsePriority(text))
            {
            case ProcessPriorityClass.Idle:
                return 0;
            case ProcessPriorityClass.BelowNormal:
                return 1;
            case ProcessPriorityClass.Normal:
                return 2;
            default:
                return 3;
            }
        }

        private static string PriorityFromIndex(int index)
        {
            switch (index)
            {
            case 0:
                return "idle";
            case 1:
                return "below_normal";
            case 3:
                return "above_normal";
            default:
                return "normal";
            }
        }

        private void SetAiTestResult(string text)
        {
            if (lblAiHint != null)
            {
                lblAiHint.Text = text;
                lblAiHint.SelectionStart = 0;
                lblAiHint.SelectionLength = 0;
                lblAiHint.ScrollToCaret();
            }
            SetStatus(text);
        }

        private void OnTestAiClick(object sender, EventArgs e)
        {
            if (!CommitProviderForm()) return;
            AiProvider p = cfg.ActiveProvider();
            if (p == null)
            {
                SetAiTestResult("还没有提供商，请先新增或用预设填充一个。");
                return;
            }
            p = p.Snapshot();
            int timeoutMs = cfg.AiTimeoutSeconds * 1000;
            int outputTokens = cfg.AiMaxOutputTokens;
            bool useProxy = cfg.AiUseSystemProxy;
            string validationError = p.ValidationError();
            if (validationError != null) { SetAiTestResult(validationError); return; }
            SetAiTestResult("测试中：「" + p.Name + "」 / " + p.FirstModel + " …");
            DshButton btn = sender as DshButton;
            if (btn != null)
            {
                btn.Enabled = false;
            }
            Thread thread = new Thread((ThreadStart)delegate
            {
                string error = null;
                string text = null;
                try
                {
                    AiClient.UseSystemProxy = useProxy; AiClient.OutputTokens = outputTokens;
                    string responseJson = AiClient.CallProvider(p, "", "回复两个字：可用", null, 0.0, false, timeoutMs);
                    text = AiClient.ExtractContentFor(p, responseJson, out error);
                    if (error == null && string.IsNullOrWhiteSpace(text)) error = "模型返回了空回复，未通过对话测试。";
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                string result;
                if (error != null)
                {
                    result = "连接失败：" + AiTroubleshooting.Explain(error);
                }
                else
                {
                    result = "连接成功，" + p.FirstModel + " 回复：“" + (text ?? "").Trim() + "”";
                }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        SetAiTestResult(result);
                        if (btn != null)
                        {
                            btn.Enabled = true;
                        }
                    });
                }
                catch
                {
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void OnOpenDirClick(object sender, EventArgs e)
        {
            string text = txtDir.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                text = cfg.EffectiveSaveDir();
            }
            if (!Config.IsWritableDir(text))
            {
                text = cfg.ResolveSaveDir();
                SetAiTestResult("设定目录不可写，已改为打开：" + text);
            }
            try
            {
                Process.Start("explorer.exe", "\"" + text + "\"");
            }
            catch (Exception ex)
            {
                SetAiTestResult("无法打开文件夹：" + ex.Message);
            }
        }

        private void OnShotClick(object sender, EventArgs e)
        {
            bool visible = base.Visible;
            try
            {
                if (visible)
                {
                    Hide();
                    Application.DoEvents();
                }
                app.CaptureNow();
            }
            finally
            {
                if (visible && !allowExit)
                {
                    Show();
                    Activate();
                }
            }
        }

        private void HideMe()
        {
            if(!ConfirmDiscardSettingsDrafts())return;
            Hide();
            app.NotifyHiddenToTray();
            app.ReleaseMainForm();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if(!allowExit&&e.CloseReason==CloseReason.UserClosing&&!ConfirmDiscardSettingsDrafts()){e.Cancel=true;return;}
            if (!allowExit && cfg.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideMe();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (allowExit)
            {
                app.RequestExit();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeRecentUi();
                if(settingsDraftTimer!=null){settingsDraftTimer.Stop();settingsDraftTimer.Dispose();settingsDraftTimer=null;}
                if (providerDetailsTimer != null) { providerDetailsTimer.Stop(); providerDetailsTimer.Dispose(); providerDetailsTimer = null; }
                if (animeTimer != null) { animeTimer.Stop(); animeTimer.Dispose(); animeTimer = null; }
                animeWatch.Stop();
                if (titleFont != null)
                {
                    titleFont.Dispose();
                    titleFont = null;
                }
                if (boldFont != null)
                {
                    boldFont.Dispose();
                    boldFont = null;
                }
                if (picIcon != null && picIcon.Image != null)
                {
                    picIcon.Image = null;
                }
                if (brandImage != null)
                {
                    brandImage.Dispose();
                    brandImage = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
