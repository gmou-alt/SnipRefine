using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;

namespace SnapGrab
{
    internal partial class EditorForm : Form
    {
        internal enum EditorExit
        {
            Cancel,
            Save,
            Copy
        }

        private class EditorButton : Control
        {
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                Color background=BackColor;Control parent=Parent;
                while(parent!=null){if(parent.BackColor.A==255){background=parent.BackColor;break;}parent=parent.Parent;}
                using (SolidBrush brush = new SolidBrush(background))
                    e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            protected override void OnResize(EventArgs e) { base.OnResize(e); GlassSurface.ApplyRoundedRegion(this); }
            protected override void OnPaint(PaintEventArgs e) { OnPaintBackground(e); }
        }

        private class ToolButton : EditorButton
        {
            public Shape.K Kind;

            private bool hover;

            public ToolButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = PanelFillSoft;
                Cursor = Cursors.Hand;
                Font = ToolFont;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                hover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                OnPaintBackground(e);
                Graphics graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool flag = false;
                EditorForm editorForm = FindForm() as EditorForm;
                if (editorForm != null)
                {
                    flag = editorForm.IsDrawingToolActive(Kind);
                }
                Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
                GlassSurface.Button(graphics, r, !Dsh.LightTheme, flag, hover, false, Enabled, true);
                if (flag)
                {
                    Dsh.FillRound(graphics, new Rectangle(0, 8, 3, base.Height - 17), 2, AccentBar);
                }
                Dsh.DrawText(graphics, Text, Font, Enabled ? (flag ? Color.White : Ink) : InkMuted, new Rectangle(0, 0, base.Width, base.Height), ContentAlignment.MiddleCenter);
            }
        }

        private class ColorSwatch : EditorButton
        {
            public Color Value = Color.Red;

            private bool hover;

            public ColorSwatch()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                hover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics graphics = e.Graphics;
                // Opaque controls must erase the previous selection outline themselves.
                OnPaintBackground(e);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
                Dsh.FillRound(graphics, r, Dsh.ButtonRadius(r), Value);
                bool flag = false;
                EditorForm editorForm = FindForm() as EditorForm;
                if (editorForm != null)
                {
                    flag = editorForm.CurrentColor().ToArgb() == Value.ToArgb();
                }
                Color color = Dsh.LightTheme ? Color.FromArgb(45, 60, 82) : Color.FromArgb(235, 242, 250);
                Dsh.StrokeRound(graphics, r, Dsh.ButtonRadius(r), color, flag ? 3f : (hover ? 2f : 1.5f));
            }
        }

        private class WideButton : EditorButton
        {
            public Func<bool> ActiveState;
            private bool hover;

            private bool down;

            public WideButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = PanelFillSoft;
                Cursor = Cursors.Hand;
                Font = ToolFont;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                hover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = false;
                down = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                down = true;
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                down = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                OnPaintBackground(e);
                Graphics graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
                bool active=ActiveState!=null && ActiveState();
                GlassSurface.Button(graphics, r, !Dsh.LightTheme, active, hover, down, Enabled, true);
                Dsh.DrawText(graphics, Text, Font, Enabled ? (active ? Color.White : Ink) : InkMuted, new Rectangle(0, 0, base.Width, base.Height), ContentAlignment.MiddleCenter);
            }
        }

        private class ZoomButton : EditorButton
        {
            public object Tag2;

            private bool hover;

            public ZoomButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = PanelFillSoft;
                Cursor = Cursors.Hand;
                Font = ToolFont;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                hover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                OnPaintBackground(e);
                Graphics graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
                GlassSurface.Button(graphics, r, !Dsh.LightTheme, false, hover, false, Enabled, true);
                Dsh.DrawText(graphics, Text, Font, Enabled ? Ink : InkMuted, new Rectangle(0, 0, base.Width, base.Height), ContentAlignment.MiddleCenter);
            }
        }

        private class BarButton : EditorButton
        {
            public bool Primary;

            private bool hover;

            private bool down;

            public BarButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = PanelFillSoft;
                Cursor = Cursors.Hand;
                Font = ToolFont;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                hover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = false;
                down = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                down = true;
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                down = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                OnPaintBackground(e);
                Graphics graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
                GlassSurface.Button(graphics, r, !Dsh.LightTheme, Primary, hover, down, Enabled, true);
                Color color = Enabled ? (Primary ? Color.White : Ink) : InkMuted;
                Dsh.DrawText(graphics, Text, Font, color, new Rectangle(0, 0, base.Width, base.Height), ContentAlignment.MiddleCenter);
            }
        }

        private const int ToolbarH = 46;

        private const int ActionH = 50;

        private const int PadX = 10;

        private const int TitleH = 30;

        private const int AiPanelW = 336;

        private static Color CanvasDark { get { return Dsh.Canvas; } }

        private static Color PanelFill { get { return Dsh.SidebarTop; } }

        private static Color PanelFillSoft { get { return Dsh.SidebarFlat; } }

        private static Color PanelEdge { get { return Dsh.SidebarEdge; } }

        private static Color PanelHairline { get { return Dsh.SidebarEdge; } }

        private static Color Ink { get { return Dsh.TextPrimary; } }

        private static Color InkMuted { get { return Dsh.TextSecondary; } }

        private static Color InkFaint { get { return Dsh.TextTertiary; } }

        private static Color HoverFill { get { return Dsh.Hover; } }

        private static Color ActiveFill { get { return Dsh.PrimaryFill; } }

        private static Color GuideFill { get { return Dsh.GlassHighlight; } }

        private static Color AccentBar { get { return Dsh.PrimaryFill; } }

        private readonly Bitmap src;

        private readonly TrayApp app;

        private readonly Config cfg;

        private readonly List<Shape> shapes = new List<Shape>();

        private readonly List<UndoStep> undoSteps = new List<UndoStep>();

        private readonly List<UndoStep> undone = new List<UndoStep>();

        private UndoStep pendingGroup;

        private Shape draft;
        private TextBox annotationInput;
        private PointF annotationAnchor;

        private Bitmap composite;

        private int commitVersion;

        private int builtVersion = -1;

        private float builtZoom = -1f;

        private Point compOrigin;

        private float zoom = 1f;

        private bool zoomInitialized;

        private Point pan;

        private bool panning;

        private Point panStart;

        private Point panOrigin;

        private Shape.K tool;

        private Color color = Color.FromArgb(232, 59, 59);

        private float strokeW = 3f;

        private int mosaicBlock = 8;

        private float fontPx = 22f;

        private RectangleF cropRect = RectangleF.Empty;

        private bool hasCrop;

        private bool exporting;

        private Bitmap exportBase;

        private bool spaceDown;

        private bool mouseIn;

        private string status = "";

        private int statusVersion;

        private readonly List<ToolButton> toolButtons = new List<ToolButton>();

        private readonly List<ColorSwatch> swatches = new List<ColorSwatch>();

        private readonly List<WideButton> wideButtons = new List<WideButton>();

        private readonly List<ZoomButton> zoomButtons = new List<ZoomButton>();

        private readonly List<BarButton> barButtons = new List<BarButton>();

        private bool aiOpen;

        private Panel aiBody;

        private TextBox aiLogBox;

        private TextBox aiInput;

        private DshInputFrame aiInputFrame;

        private Label aiStatusLabel;

        private readonly List<string> aiHistory = new List<string>();

        private Bitmap srcMutable;

        private bool disposed;
        private bool aiBusy;
        private int editRevision;
        private int aiRequestId;
        private Label aiTitle;
        private GlassButton aiSendButton;
        private GlassButton aiCloseButton;
        private readonly List<GlassButton> aiQuickButtons = new List<GlassButton>();
        private int toolbarBottom = 112;
        private readonly SidebarMotion aiMotion = new SidebarMotion();
        private readonly Stopwatch aiMotionWatch = new Stopwatch();
        private System.Windows.Forms.Timer aiMotionTimer;
        private int AiVisibleWidth { get { return (int)Math.Round(AiPanelWidth * aiMotion.Position); } }

        private int requestedAiWidth;
        private int AiPanelWidth { get { return Math.Max(280, Math.Min(Math.Max(280, WinW - WorkspaceLeft - 227), requestedAiWidth == 0 ? Math.Min(360, Math.Max(280, WinW / 3)) : requestedAiWidth)); } }

        public EditorExit ExitMode { get; private set; }

        public int SourceW
        {
            get
            {
                return Src.Width;
            }
        }

        public int SourceH
        {
            get
            {
                return Src.Height;
            }
        }

        private int WinW
        {
            get
            {
                return base.ClientSize.Width;
            }
        }

        private int WinH
        {
            get
            {
                return base.ClientSize.Height;
            }
        }

        private int ViewTop
        {
            get
            {
                return toolbarBottom;
            }
        }

        private int ViewBottom
        {
            get
            {
                return WinH - 50;
            }
        }

        private Rectangle ViewRect
        {
            get
            {
                return new Rectangle(WorkspaceLeft, ViewTop, Math.Max(1, WinW - AiVisibleWidth - WorkspaceLeft - (AiVisibleWidth > 0 ? 7 : 0)), Math.Max(1, ViewBottom - ViewTop));
            }
        }

        private static Font ToolFont
        {
            get
            {
                return Dsh.F(12.5f, FontStyle.Regular);
            }
        }

        private Bitmap Src
        {
            get
            {
                if (srcMutable == null)
                {
                    return src;
                }
                return srcMutable;
            }
        }

        public bool TestProcessKey(Keys keyData)
        {
            Message msg = Message.Create(base.Handle, 256, (IntPtr)(long)keyData, IntPtr.Zero);
            bool flag = ProcessCmdKey(ref msg, keyData);
            if (!flag)
            {
                Control activeControl = base.ActiveControl;
                if (activeControl != null && activeControl.IsHandleCreated)
                {
                    MethodInfo method = typeof(Control).GetMethod("ProcessKeyMessage", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(activeControl, new object[1] { msg });
                    }
                }
            }
            return flag;
        }

        internal Shape.K CurrentTool()
        {
            return tool;
        }

        internal Color CurrentColor()
        {
            return color;
        }

        public EditorForm(TrayApp owner, Bitmap image, Config config)
        {
            app = owner;
            cfg = config;
            src = image;
            color = ParseColor(cfg.DrawColor);
            strokeW = Math.Max(1, Math.Min(20, cfg.DrawWidth));
            mosaicBlock = Math.Max(4, Math.Min(24, cfg.MosaicBlock));
            SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            base.FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(640,450);
            base.StartPosition = FormStartPosition.Manual;
            base.KeyPreview = true;
            Text = UiText.T("SnipRefine 编辑器");
            BackColor = CanvasDark;
            DoubleBuffered = true;
            BuildChrome();
            OpenAiPanel();
        }

        private void OpenAiPanel() { SetAiPanel(true); }

        private void CloseAiPanel() { SetAiPanel(false); }

        private void SetAiPanel(bool open)
        {
            aiOpen = open;
            LayoutAiToggle();
            if (aiBody == null) BuildAiPanel();
            if (aiMotionTimer == null)
            {
                aiMotionTimer = new System.Windows.Forms.Timer();
                aiMotionTimer.Interval = 15;
                aiMotionTimer.Tick += AiMotionTick;
            }
            if (!open) Focus();
            if (!Visible || !IsHandleCreated || !GlassSurface.MotionEnabled)
            {
                aiMotionTimer.Stop();
                aiMotion.Jump(open ? 1 : 0);
                LayoutAiPanel();
                aiBody.Visible = open;
                if (open && Visible) aiInput.Focus();
            }
            else
            {
                aiMotion.MoveTo(open ? 1 : 0);
                aiBody.Visible = true;
                aiMotionWatch.Restart();
                aiMotionTimer.Start();
            }
            Invalidate();
        }

        private void AiMotionTick(object sender, EventArgs e)
        {
            double seconds = aiMotionWatch.Elapsed.TotalSeconds;
            aiMotionWatch.Restart();
            aiMotion.Advance(seconds);
            PositionAiPanel();
            Invalidate(ViewRect);
            if (!aiMotion.Moving)
            {
                aiMotionTimer.Stop();
                aiMotionWatch.Stop();
                aiBody.Visible = aiOpen;
                if (aiOpen) aiInput.Focus();
                Invalidate();
            }
        }

        private void PositionAiPanel()
        {
            if (aiBody != null) aiBody.SetBounds(WinW - AiVisibleWidth, ViewTop, AiPanelWidth, Math.Max(1, ViewBottom - ViewTop));
            LayoutWorkspaceDivider();
            LayoutAiToggle();
        }

        private void BuildAiPanel()
        {
            aiBody = new Panel();
            aiBody.SetBounds(0, 0, 0, 0);
            aiBody.BackColor = Dsh.SidebarFlat;
            base.Controls.Add(aiBody);
            aiLogBox = new TextBox();
            aiLogBox.Multiline = true;
            aiLogBox.ReadOnly = true;
            aiLogBox.ScrollBars = ScrollBars.Vertical;
            aiLogBox.BorderStyle = BorderStyle.None;
            aiLogBox.BackColor = Dsh.SidebarFlat;
            aiLogBox.ForeColor = Dsh.TextPrimary;
            aiLogBox.Font = Dsh.F(12.5f, FontStyle.Regular);
            aiBody.Controls.Add(aiLogBox);
            aiStatusLabel = new Label();
            aiStatusLabel.BorderStyle = BorderStyle.None;
            aiStatusLabel.BackColor = Dsh.SidebarFlat;
            aiStatusLabel.ForeColor = Dsh.TextSecondary;
            aiStatusLabel.Font = Dsh.F(11.5f, FontStyle.Regular);
            aiStatusLabel.Text = "";
            aiBody.Controls.Add(aiStatusLabel);
            aiInput = new TextBox();
            aiInput.Multiline = true;
            aiInput.BorderStyle = BorderStyle.FixedSingle;
            aiInput.BackColor = Dsh.GlassFillStrong;
            aiInput.ForeColor = Dsh.TextPrimary;
            aiInput.Font = Dsh.F(12.5f, FontStyle.Regular);
            aiInput.KeyDown += AiInputKeyDown;
            aiInputFrame = new DshInputFrame(aiInput);
            aiBody.Controls.Add(aiInputFrame);
            aiTitle = new Label();
            aiTitle.Text = UiText.T("AI 修改与识别");
            aiTitle.Font = Dsh.F(13f, FontStyle.Bold);
            aiTitle.ForeColor = aiLogBox.ForeColor;
            aiBody.Controls.Add(aiTitle);
            aiCloseButton = new GlassButton();
            aiCloseButton.Font = Dsh.F(16f, FontStyle.Bold);
            aiCloseButton.Text = UiText.T("收起");
            aiCloseButton.Click += delegate { SetAiPanel(!aiOpen); };
            Controls.Add(aiCloseButton);
            string[] captions = { "标注重点", "遮挡隐私", "裁剪主体", "识别文字" };
            string[] prompts = { "请用矩形或箭头标注截图中的重点，并简要解释。", "请用不透明实心遮挡 redact 隐藏截图中的手机号、邮箱等隐私信息；不要使用模糊或马赛克。", "请裁剪截图，保留主要内容。", AiClient.OcrPrompt() };
            for (int i = 0; i < captions.Length; i++)
            {
                string prompt = prompts[i];
                GlassButton button = new GlassButton();
                button.Text = UiText.T(captions[i]);
                button.Font = ToolFont;
                button.Click += delegate { AiSend(prompt); };
                aiQuickButtons.Add(button);
                aiBody.Controls.Add(button);
            }
            aiSendButton = new GlassButton();
            aiSendButton.Text = UiText.T("发送");
            aiSendButton.Click += delegate { AiSend(null); };
            aiBody.Controls.Add(aiSendButton);
            aiSendButton.Primary = true;
            BuildAiReviewControls();
            aiLogBox.Text = UiText.T("先用 AI 框选，再选择替换文字、去字修补或局部重绘。图片修改需独立图片编辑服务；下方发送和识别使用文本/看图模型。\r\n\r\n发送会将当前图片和要求传到所选模型。结果可预览、撤销，再自行保存或复制。");
            AiSetStatus(UiText.T("Enter 发送 · Shift+Enter 换行"));
            LayoutAiPanel();
        }

        private void LayoutAiPanel()
        {
            if (aiBody == null || aiTitle == null) return;
            int w = AiPanelWidth, h = Math.Max(1, ViewBottom - ViewTop);
            PositionAiPanel();
            aiTitle.SetBounds(12, 12, w - 24, 24);
            UpdateAiUploadTitle();
            LayoutAiToggle();
            int reviewTop=LayoutWorkspaceAiCommands(w,h);
            bool tiny=h<300;
            LayoutAiReviewControls(w);
            aiCompareButton.Visible=aiAcceptButton.Visible=aiUndoButton.Visible=aiCancelButton.Visible=!tiny;
            aiCompareButton.Top=aiAcceptButton.Top=reviewTop;
            aiUndoButton.Top=aiCancelButton.Top=reviewTop+32;
            int logTop=tiny ? reviewTop : reviewTop+72;
            aiLogBox.SetBounds(12, logTop, w - 24, Math.Max(1, h - 114 - logTop));
            aiStatusLabel.SetBounds(12, h - 106, w - 24, 34);
            aiInputFrame.SetBounds(12, h - 66, w - 92, 54);
            aiSendButton.SetBounds(w - 74, h - 66, 62, 54);
        }

        private void AiInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && aiBusy) { e.Handled=true; e.SuppressKeyPress=true; CancelAiRequest(); return; }
            if (e.KeyCode == Keys.Return && !e.Shift)
            {
                e.SuppressKeyPress = true;
                AiSend(null);
            }
        }

        private string ResolveAiEndpoint()
        {
            AiPreset aiPreset = AiClient.FindPreset(cfg.AiPreset);
            string aiEndpoint = cfg.AiEndpoint;
            if (!string.IsNullOrEmpty(aiEndpoint))
            {
                return aiEndpoint;
            }
            return aiPreset.Endpoint;
        }

        private string ResolveAiModel()
        {
            AiPreset aiPreset = AiClient.FindPreset(cfg.AiPreset);
            string aiModel = cfg.AiModel;
            if (!string.IsNullOrEmpty(aiModel))
            {
                return aiModel;
            }
            return aiPreset.Model;
        }

        private void AiAppend(string who, string text)
        {
            aiHistory.Add(who + "：" + text);
            if (aiLogBox != null)
            {
                aiLogBox.Text = string.Join("\r\n\r\n", aiHistory.ToArray());
                aiLogBox.SelectionStart = aiLogBox.TextLength;
                aiLogBox.ScrollToCaret();
            }
        }

        private void AiSetStatus(string s)
        {
            if (aiStatusLabel != null)
            {
                aiStatusLabel.Text = UiText.T(s);
            }
        }

        internal void BeginChatImageEdit(string prompt)
        {
            SetAiPanel(true); aiInput.Text = prompt;
            AiSetStatus(UiText.T("请确认修改要求后点击发送；修改可预览、撤销。"));
            aiInput.Focus();
        }

        private void AiSend(string forcedPrompt)
        {
            if (!aiOpen || aiInput == null) return;
            if (aiBusy) { AiSetStatus(UiText.T("正在等模型回复…")); return; }
            requestingTranslation=forcedPrompt!=null && forcedPrompt.Contains("翻译");
            string text = forcedPrompt ?? aiInput.Text.Trim();
            if (text.Length == 0) return;
            AiProvider provider = cfg.ActiveProvider();
            if (provider == null || string.IsNullOrEmpty(provider.Endpoint) || string.IsNullOrEmpty(provider.FirstModel))
            {
                AiSetStatus(UiText.T("请到设置的 AI 助手页配置 API 地址和视觉模型。"));
                return;
            }
            CommitOpenTextInput();
            compareBefore = false;
            ClearAiComparison();
            Rectangle requestRegion=Rectangle.Intersect(aiRegion,new Rectangle(Point.Empty,Src.Size));
            if(aiRegionDrawing || (aiRegionTool && requestRegion.IsEmpty)) { AiSetStatus(UiText.T("请先框选至少 2 × 2 像素的区域，或清除 AI 选区后发送整图")); return; }
            Bitmap snapshot = RenderResult();
            aiBefore = (Bitmap)snapshot.Clone();
            aiCancellation = new AiRequestCancellation();
            string prompt = EditorAiSession.BuildPrompt(requestRegion.IsEmpty ? aiHistory : new List<string>(), text);
            int revision = editRevision;
            int regionRevision=aiRegionRevision;
            int request = ++aiRequestId;
            aiBusy = true;
            aiSendButton.Enabled = false;
            foreach (GlassButton button in aiQuickButtons) button.Enabled = false;
            if (forcedPrompt == null) { requestingTranslation=false; aiInput.Clear(); }
            AiAppend("你", text);
            StartAiProgress(provider.FirstModel);
            EditorAiSession.Send(provider, cfg, snapshot, prompt, delegate(AiReply reply, string error)
            {
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (disposed || IsDisposed || request != aiRequestId) return;
                        aiBusy = false;
                        StopAiProgress();
                        if (error != null && aiInput.TextLength == 0) aiInput.Text = text;
                        aiCancellation = null;
                        aiSendButton.Enabled = true;
                        foreach (GlassButton button in aiQuickButtons) button.Enabled = true;
                        CommitOpenTextInput();
                        if (error == null && (revision != editRevision || regionRevision!=aiRegionRevision || draft != null || selectionDragging))
                        {
                            if (reply != null && !string.IsNullOrEmpty(reply.Say)) AiAppend("AI", reply.Say);
                            AiAppend("提示", "等待期间截图已被修改，本次操作未应用。请重新发送要求。");
                            ClearAiComparison();
                            AiSetStatus(UiText.T("截图已变更，请重新发送"));
                            return;
                        }
                        if (error == null && reply != null && (cfg.AiReviewOperations || !requestRegion.IsEmpty) && !ReviewRegionReply(reply,requestRegion)) { ClearAiComparison(); AiSetStatus(UiText.T("本次修改已放弃")); return; }
                        activeAiRegion=requestRegion;
                        try { AiHandleReply(reply, error); } finally { activeAiRegion=Rectangle.Empty; }
                    });
                }
                catch (InvalidOperationException) { }
            }, aiCancellation,requestRegion);
        }

        private int nextNumber = 1;
        private bool aiApplying;
        private PointF dragAnchor;

        private void AiHandleReply(AiReply reply, string err)
        {
            if (err != null) { ClearAiComparison(); AiAppend("错误", AiTroubleshooting.Explain(err)); AiSetStatus(UiText.T("要求已保留；检查配置后重试")); return; }
            if (reply == null) { ClearAiComparison(); AiSetStatus(UiText.T("没有拿到有效回复")); return; }
            if(requestingTranslation) { latestTranslation=reply.Say ?? ""; requestingTranslation=false; }
            else latestTranslation="";
            latestOcr = "";
            if (reply.Ops != null) foreach (AiOp operation in reply.Ops) if (operation.Kind == "ocr" && !string.IsNullOrEmpty(operation.Text)) latestOcr += operation.Text + "\r\n";
            if (!string.IsNullOrEmpty(reply.Say)) AiAppend("AI", reply.Say);
            UndoStep checkpoint = new UndoStep();
            checkpoint.Kind = UndoStep.T.Ai;
            checkpoint.HadSource = srcMutable != null;
            checkpoint.Snapshot = srcMutable == null ? null : (Bitmap)srcMutable.Clone();
            foreach (Shape shape in shapes) checkpoint.Items.Add(shape.Clone());
            int before = undoSteps.Count;
            int revision = editRevision;
            aiApplying = true;
            try
            {
                AiApplyOps(reply.Ops);
                if (revision != editRevision)
                {
                    RemoveAiIntermediateSteps(before);
                    ClearRedoHistory();
                    undoSteps.Add(checkpoint);
                    checkpoint = null;
                    aiAppliedRevision = editRevision;
                    AiSetStatus(UiText.T("已应用修改 · 可撤销 · 确认后保存或复制"));
                }
                else { ClearAiComparison(); AiSetStatus(UiText.T("已回复，未修改图片")); }
            }
            catch (Exception ex)
            {
                SwapSnapshotState(checkpoint);
                RemoveAiIntermediateSteps(before);
                AiAppend("错误", "修改未完成，已恢复截图：" + ex.Message);
                ClearAiComparison();
                AiSetStatus(UiText.T("操作失败，已恢复"));
            }
            finally { aiApplying = false; if (checkpoint != null) checkpoint.DisposeSnap(); BoundHistory(); }
            Invalidate();
        }

        private void RemoveAiIntermediateSteps(int count)
        {
            while (undoSteps.Count > count)
            {
                UndoStep step = undoSteps[undoSteps.Count - 1];
                undoSteps.RemoveAt(undoSteps.Count - 1);
                step.DisposeSnap();
            }
        }

        private int AiApplyOps(List<AiOp> ops)
        {
            int num = 0;
            int num2 = 0;
            RectangleF rectangleF = RectangleF.Empty;
            if (ops == null) return 0;
            if (ops.Count > 64) throw new InvalidOperationException("单次 AI 修改最多 64 条操作，请缩小任务范围。");
            foreach (AiOp item in ops)
            {
                if (item == null) throw new InvalidOperationException("AI 返回了空操作。");
                foreach (float value in new float[] { item.X, item.Y, item.W, item.H, item.X2, item.Y2, item.Width })
                    if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException("AI 返回了无效坐标。");
                string kind = (item.Kind ?? "").Trim().ToLowerInvariant();
                if (kind != "rect" && kind != "ellipse" && kind != "line" && kind != "arrow" && kind != "text" && kind != "mosaic" && kind != "redact" && kind != "blur" && kind != "crop" && kind != "ocr")
                    throw new InvalidOperationException("不支持的 AI 操作：" + kind);
            }
            BeginGroup();
            try
            {
                foreach (AiOp op in ops)
                {
                    string text = (op.Kind ?? "").Trim().ToLowerInvariant();
                    Color col = AiClient.ColorOf(op.Color, color);
                    float num3 = ((op.Width > 0f) ? Math.Max(1f, Math.Min(20f, op.Width)) : strokeW);
                    Shape shape = null;
                    switch (text)
                    {
                    case "rect":
                    case "rectangle":
                        shape = MakeRectShape(Shape.K.Rect, op, col, num3);
                        break;
                    case "ellipse":
                    case "circle":
                        shape = MakeRectShape(Shape.K.Ellipse, op, col, num3);
                        break;
                    case "arrow":
                        shape = new Shape();
                        shape.Kind = Shape.K.Arrow;
                        shape.Color = col;
                        shape.Width = num3;
                        shape.Points.Add(new PointF(op.X, op.Y));
                        shape.Rect = new RectangleF(op.X, op.Y, op.X2 - op.X, op.Y2 - op.Y);
                        break;
                    case "line":
                        shape = new Shape();
                        shape.Kind = Shape.K.Line;
                        shape.Color = col;
                        shape.Width = num3;
                        shape.Points.Add(new PointF(op.X, op.Y));
                        shape.Rect = new RectangleF(op.X, op.Y, op.X2 - op.X, op.Y2 - op.Y);
                        break;
                    case "text":
                        if (!string.IsNullOrEmpty(op.Text))
                        {
                            shape = new Shape();
                            shape.Kind = Shape.K.Text;
                            shape.Color = col;
                            shape.Width = num3;
                            shape.FontPx = fontPx;
                            shape.Text = op.Text;
                            shape.Rect = new RectangleF(op.X, op.Y, 0f, 0f);
                        }
                        break;
                    case "mosaic":
                        shape = new Shape();
                        shape.Kind = Shape.K.Mosaic;
                        shape.Color = col;
                        shape.Width = num3;
                        shape.Block = ((op.Block > 0) ? op.Block : mosaicBlock);
                        shape.Rect = ClampRect(op.X, op.Y, op.W, op.H);
                        break;
                    case "redact":
                    case "blur":
                        shape = new Shape { Kind = text == "redact" ? Shape.K.Redact : Shape.K.Blur, Color = col, Width=num3, Rect=ClampRect(op.X,op.Y,op.W,op.H) };
                        break;
                    case "crop":
                    {
                        RectangleF rectangleF2 = ClampRect(op.X, op.Y, op.W, op.H);
                        if (rectangleF2.Width > 2f && rectangleF2.Height > 2f)
                        {
                            rectangleF = rectangleF2;
                            num2++;
                        }
                        continue;
                    }
                    case "ocr":
                        if (!string.IsNullOrEmpty(op.Text))
                        {
                            AiAppend("OCR", op.Text);
                        }
                        continue;
                    default:
                        continue;
                    }
                    if (shape != null)
                    {
                        if (shape.Kind == Shape.K.Rect || shape.Kind == Shape.K.Ellipse)
                        {
                            shape.Rect = ClampRect(op.X, op.Y, op.W, op.H);
                        }
                        if (shape.Kind == Shape.K.Text || shape.Kind == Shape.K.Arrow || shape.Kind == Shape.K.Line || !(shape.Rect.Width < 2f) || !(shape.Rect.Height < 2f))
                        {
                            if(!activeAiRegion.IsEmpty) shape.AiClip=activeAiRegion;
                            AddShapeTracked(shape);
                            num++;
                        }
                    }
                }
            }
            finally
            {
                CommitGroup();
            }
            if (num > 0)
            {
                EnsureComposite();
                Invalidate();
            }
            if (num2 > 0)
            {
                CommitCrop(rectangleF);
                AiAppend("提示", (num2 > 1) ? string.Concat("模型给了多处裁剪，只执行了最后一处（", Rectangle.Round(rectangleF), "）") : ("已按模型建议裁剪到 " + Rectangle.Round(rectangleF)));
            }
            return num;
        }

        private Shape MakeRectShape(Shape.K k, AiOp op, Color col, float wdt)
        {
            Shape shape = new Shape();
            shape.Kind = k;
            shape.Color = col;
            shape.Width = wdt;
            shape.Rect = ClampRect(op.X, op.Y, op.W, op.H);
            return shape;
        }

        private RectangleF ClampRect(float x, float y, float w, float h)
        {
            float num = Src.Width;
            float num2 = Src.Height;
            if (x < 0f)
            {
                w += x;
                x = 0f;
            }
            if (y < 0f)
            {
                h += y;
                y = 0f;
            }
            if (x > num)
            {
                x = num;
            }
            if (y > num2)
            {
                y = num2;
            }
            if (w < 0f)
            {
                w = 0f;
            }
            if (h < 0f)
            {
                h = 0f;
            }
            if (x + w > num)
            {
                w = num - x;
            }
            if (y + h > num2)
            {
                h = num2 - y;
            }
            return new RectangleF(x, y, w, h);
        }

        public static Size SuggestedWindowSize(Size image)
        {
            Rectangle workArea=Screen.FromPoint(Cursor.Position).WorkingArea;
            int num = (int)((double)workArea.Width * 0.92);
            int num2 = (int)((double)workArea.Height * 0.92);
            int num3 = 126;
            int num4 = Math.Max(1000, image.Width + 380);
            int num5 = Math.Max(560, image.Height + num3 + 60);
            if (num4 > num)
            {
                num4 = num;
            }
            if (num5 > num2)
            {
                num5 = num2;
            }
            return new Size(num4, num5);
        }

        private void BuildChrome()
        {
            AddTool(UiText.T("画笔"), Shape.K.Pen);
            AddTool(UiText.T("橡皮擦"), Shape.K.Eraser);
            AddTool(UiText.T("选择"), Shape.K.Select);
            AddTool(UiText.T("直线"), Shape.K.Line);
            AddTool(UiText.T("箭头"), Shape.K.Arrow);
            AddTool(UiText.T("矩形"), Shape.K.Rect);
            AddTool(UiText.T("椭圆"), Shape.K.Ellipse);
            AddTool(UiText.T("文字"), Shape.K.Text);
            AddTool(UiText.T("马赛克"), Shape.K.Mosaic);
            AddTool(UiText.T("遮挡"), Shape.K.Redact);
            AddTool(UiText.T("模糊"), Shape.K.Blur);
            AddTool(UiText.T("序号"), Shape.K.Number);
            BuildExperienceControls();
            LayoutChrome();
        }

        private bool IsDrawingToolActive(Shape.K kind)
        {
            return !aiRegionTool && !maskTool && tool == kind;
        }

        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
            // Parent invalidation does not repaint opaque child controls.
            // All toolbar state is read from the editor when the child repaints.
            InvalidateWorkspaceButtons(this);
        }

        private void AddTool(string text, Shape.K kind)
        {
            ToolButton toolButton = new ToolButton();
            toolButton.Text = text;
            toolButton.Kind = kind;
            toolButton.Click += delegate
            {
                aiRegionTool=false;maskTool=false;
                tool = kind;
                RefreshEffectBrushLabel();
                if(workspaceTools!=null)LayoutChrome();
                if (kind != Shape.K.Mosaic)
                {
                    hasCrop = false;
                    cropRect = RectangleF.Empty;
                }
                SetStatus(kind==Shape.K.Eraser ? "拖动擦除手工标注，保留原图 · Ctrl+Z 撤销" : (kind == Shape.K.Mosaic) ? "马赛克：框选打码，或点击马赛克画笔涂刷" : "");
                Invalidate();
            };
            toolButtons.Add(toolButton);
            base.Controls.Add(toolButton);
        }

        private void EnsureSwatches()
        {
            if (swatches.Count > 0)
            {
                return;
            }
            string[] array = new string[6] { "red", "yellow", "green", "blue", "black", "white" };
            string[] array2 = array;
            foreach (string name in array2)
            {
                ColorSwatch colorSwatch = new ColorSwatch();
                colorSwatch.Value = ParseColor(name);
                colorSwatch.Click += delegate(object sender, EventArgs e)
                {
                    color = ((ColorSwatch)sender).Value;
                    foreach (ColorSwatch swatch in swatches) swatch.Invalidate();
                    EditSelectedStyle();
                    Invalidate();
                };
                swatches.Add(colorSwatch);
                base.Controls.Add(colorSwatch);
            }
            for (int num = 0; num < 4; num++)
            {
                int num2 = (new int[4] { 2, 3, 5, 8 })[num];
                ZoomButton zoomButton = new ZoomButton();
                zoomButton.Text = num2.ToString();
                zoomButton.Tag2 = "W" + num2;
                zoomButton.Click += delegate(object sender, EventArgs e)
                {
                    strokeW = ((ZoomButton)sender).Tag2.ToString()[1] - 48;
                    if (strokeW < 1f)
                    {
                        strokeW = 1f;
                    }
                    EditSelectedStyle();
                    Invalidate();
                };
                zoomButtons.Add(zoomButton);
                base.Controls.Add(zoomButton);
            }
            WideButton wideButton = new WideButton();
            wideButton.Text = UiText.T("撤销");
            wideButton.Click += delegate
            {
                DoUndo();
            };
            wideButtons.Add(wideButton);
            base.Controls.Add(wideButton);
            WideButton wideButton2 = new WideButton();
            wideButton2.Text = UiText.T("重做");
            wideButton2.Click += delegate
            {
                DoRedo();
            };
            wideButtons.Add(wideButton2);
            base.Controls.Add(wideButton2);
            ZoomButton zoomButton2 = new ZoomButton();
            zoomButton2.Text = "－";
            zoomButton2.Click += delegate
            {
                ZoomBy(0.8f);
            };
            zoomButtons.Add(zoomButton2);
            base.Controls.Add(zoomButton2);
            ZoomButton zoomButton3 = new ZoomButton();
            zoomButton3.Text = "＋";
            zoomButton3.Click += delegate
            {
                ZoomBy(1.25f);
            };
            zoomButtons.Add(zoomButton3);
            base.Controls.Add(zoomButton3);
            ZoomButton zoomButton4 = new ZoomButton();
            zoomButton4.Text = UiText.T("适应");
            zoomButton4.Click += delegate
            {
                FitToWindow();
            };
            zoomButtons.Add(zoomButton4);
            base.Controls.Add(zoomButton4);
            ZoomButton zoomButton5 = new ZoomButton();
            zoomButton5.Text = "1:1";
            zoomButton5.Click += delegate
            {
                SetZoom(1f, true);
            };
            zoomButtons.Add(zoomButton5);
            base.Controls.Add(zoomButton5);
            WideButton wideButton3 = new WideButton();
            wideButton3.Text = UiText.T("裁剪");
            wideButton3.Click += delegate
            {
                ApplyCrop();
            };
            wideButtons.Add(wideButton3);
            base.Controls.Add(wideButton3);
            WideButton wideButton4 = new WideButton();
            wideButton4.Text = UiText.T("重置");
            wideButton4.Click += delegate
            {
                ResetAll();
            };
            wideButtons.Add(wideButton4);
            base.Controls.Add(wideButton4);
            WideButton wideButton6 = new WideButton();
            wideButton6.Text = "OCR";
            wideButton6.Click += delegate
            {
                if (!aiOpen)
                {
                    OpenAiPanel();
                }
                AiSend(AiClient.OcrPrompt());
            };
            wideButtons.Add(wideButton6);
            base.Controls.Add(wideButton6);
            AddSavedFileActions();
            BarButton barButton = new BarButton();
            barButton.Text = UiText.T("复制并保存");
            barButton.Primary = false;
            barButton.Click += delegate
            {
                AcceptAndClose(true);
            };
            barButtons.Add(barButton);
            base.Controls.Add(barButton);
            BarButton barButton2 = new BarButton();
            barButton2.Text = UiText.T("仅复制");
            barButton2.Click += delegate
            {
                AcceptAndClose(false);
            };
            barButtons.Add(barButton2);
            base.Controls.Add(barButton2);
            BarButton saveOnly = new BarButton { Text = UiText.T("保存"), Primary = true };
            saveOnly.Click += delegate { SaveEditedImage(false); }; barButtons.Add(saveOnly); Controls.Add(saveOnly);
            BarButton saveAs = new BarButton { Text = UiText.T("另存为") };
            saveAs.Click += delegate { SaveEditedImage(true); }; barButtons.Add(saveAs); Controls.Add(saveAs);
            BarButton barButton3 = new BarButton();
            barButton3.Text = UiText.T("放弃");
            barButton3.Click += delegate
            {
                CancelAndClose();
            };
            barButtons.Add(barButton3);
            base.Controls.Add(barButton3);
        }

        private void LayoutChrome()
        {
            EnsureSwatches();
            int x = 10, y = 37;
            foreach (WideButton button in wideButtons)
                if(!IsWorkspaceAi(button)&&!IsWorkspaceBrush(button))PlaceToolbarControl(button,TextRenderer.MeasureText(button.Text,ToolFont).Width+22,32,ref x,ref y);
            foreach(ZoomButton button in zoomButtons)if(button.Tag2==null)PlaceToolbarControl(button,button.Text==UiText.T("适应")||button.Text=="1:1"?52:32,32,ref x,ref y);
            foreach(BarButton button in barButtons)PlaceToolbarControl(button,TextRenderer.MeasureText(button.Text,ToolFont).Width+26,32,ref x,ref y);
            toolbarBottom=y+39;
            if(annotationInput!=null){annotationInput.SetBounds(WorkspaceLeft+8,toolbarBottom+4,Math.Max(60,Math.Min(420,WinW-WorkspaceLeft-24)),30);toolbarBottom+=42;}
            toolbarBottom+=44;
            LayoutToolProperties();
            LayoutWorkspaceTools();
            LayoutAiPanel();
        }

        private void PlaceToolbarControl(Control control, int width, int height, ref int x, ref int y)
        {
            if (x + width > WinW - 10 && x > 10) { x = 10; y += 36; }
            control.SetBounds(x, y + (32 - height) / 2, width, height);
            x += width + 6;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChrome();
            if (zoomInitialized)
            {
                commitVersion++;
            }
        }

        private RectangleF ImageContentRect()
        {
            int num3 = Math.Max(1, (int)Math.Ceiling((float)Src.Width * zoom));
            int num4 = Math.Max(1, (int)Math.Ceiling((float)Src.Height * zoom));
            // Input may arrive before painting or immediately after resize/zoom.
            // Derive the same geometry as EnsureComposite without relying on its cache.
            Rectangle view = ViewRect;
            int num = Math.Max(view.Width, num3), num2 = Math.Max(view.Height, num4);
            float x = view.Left + (view.Width - num) / 2 + (num - num3) / 2f + pan.X;
            float y = view.Y + (view.Height - num2) / 2 + (num2 - num4) / 2f + pan.Y;
            return new RectangleF(x, y, num3, num4);
        }

        private RectangleF ContentOnScreen()
        {
            return ImageContentRect();
        }

        private void EnsureZoomInit()
        {
            if (!zoomInitialized)
            {
                zoomInitialized = true;
                zoom = Math.Min(1f, FitScale());
                pan = Point.Empty;
                commitVersion++;
            }
        }

        private float FitScale()
        {
            float val = (float)ViewRect.Width / (float)Src.Width;
            float val2 = (float)ViewRect.Height / (float)Src.Height;
            return Math.Max(0.05f, Math.Min(val, val2));
        }

        private void FitToWindow()
        {
            EnsureZoomInit();
            SetZoom(FitScale(), false);
            pan = Point.Empty;
            ClampPan();
            commitVersion++;
            Invalidate();
        }

        private void SetZoom(float z, bool centerOnView)
        {
            EnsureZoomInit();
            if (centerOnView)
            {
                Point screenPt = new Point(ViewRect.Left + ViewRect.Width / 2, ViewRect.Y + ViewRect.Height / 2);
                float num = Math.Max(0.1f, Math.Min(8f, z)) / zoom;
                if (!(Math.Abs(num - 1f) < 0.0001f))
                {
                    ZoomAt(num, screenPt);
                }
            }
            else
            {
                zoom = Math.Max(0.1f, Math.Min(8f, z));
                ClampPan();
                commitVersion++;
                Invalidate();
            }
        }

        private void ZoomBy(float k)
        {
            SetZoom(zoom * k, true);
        }

        private void ZoomAt(float k, Point screenPt)
        {
            EnsureZoomInit();
            RectangleF rectangleF = ImageContentRect();
            float num = ((float)screenPt.X - rectangleF.X) / zoom;
            float num2 = ((float)screenPt.Y - rectangleF.Y) / zoom;
            float num3 = zoom;
            zoom = Math.Max(0.1f, Math.Min(8f, zoom * k));
            if (!(Math.Abs(zoom - num3) < 0.0001f))
            {
                pan = Point.Empty;
                RectangleF rectangleF2 = ImageContentRect();
                float num4 = (float)screenPt.X - num * zoom;
                float num5 = (float)screenPt.Y - num2 * zoom;
                pan = new Point((int)Math.Round(num4 - rectangleF2.X), (int)Math.Round(num5 - rectangleF2.Y));
                ClampPan();
                commitVersion++;
                Invalidate();
            }
        }

        private void ClampPan()
        {
            float num = (float)Src.Width * zoom;
            float num2 = (float)Src.Height * zoom;
            int num3 = (int)(Math.Max(0f, (num - (float)ViewRect.Width) / 2f) + (float)ViewRect.Width / 2f);
            int num4 = (int)(Math.Max(0f, (num2 - (float)ViewRect.Height) / 2f) + (float)ViewRect.Height / 2f);
            if (num3 < 40)
            {
                num3 = 40;
            }
            if (num4 < 40)
            {
                num4 = 40;
            }
            if (pan.X > num3)
            {
                pan.X = num3;
            }
            if (pan.X < -num3)
            {
                pan.X = -num3;
            }
            if (pan.Y > num4)
            {
                pan.Y = num4;
            }
            if (pan.Y < -num4)
            {
                pan.Y = -num4;
            }
        }

        private PointF ScreenToImage(Point p)
        {
            RectangleF rectangleF = ImageContentRect();
            return new PointF(((float)p.X - rectangleF.X) / zoom, ((float)p.Y - rectangleF.Y) / zoom);
        }

        private PointF ClampToImage(PointF p)
        {
            if (p.X < 0f)
            {
                p.X = 0f;
            }
            if (p.Y < 0f)
            {
                p.Y = 0f;
            }
            if (p.X > (float)Src.Width - 0.01f)
            {
                p.X = (float)Src.Width - 0.01f;
            }
            if (p.Y > (float)Src.Height - 0.01f)
            {
                p.Y = (float)Src.Height - 0.01f;
            }
            return p;
        }

        private bool InView(Point p)
        {
            return ViewRect.Contains(p);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            EnsureZoomInit();
            if (WinW > 0 && WinH > 0)
            {
                Graphics graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                graphics.Clear(CanvasDark);
                GraphicsState canvasState = graphics.Save();
                graphics.SetClip(ViewRect);
                DrawChecker(graphics);
                if (compareBefore && aiBefore != null)
                {
                    float fit = Math.Min((float)ViewRect.Width / aiBefore.Width, (float)ViewRect.Height / aiBefore.Height);
                    float width = aiBefore.Width * fit, height = aiBefore.Height * fit;
                    graphics.DrawImage(aiBefore, ViewRect.Left + (ViewRect.Width-width)/2, ViewRect.Top + (ViewRect.Height-height)/2, width, height);
                }
                else
                {
                EnsureComposite();
                if (composite != null)
                {
                    Point point = compOrigin;
                    graphics.DrawImage(composite, point.X + pan.X, point.Y + pan.Y);
                }
                if (draft != null && mouseIn)
                {
                    RectangleF rectangleF = ImageContentRect();
                    GraphicsState gstate = graphics.Save();
                    graphics.TranslateTransform(rectangleF.X, rectangleF.Y);
                    graphics.ScaleTransform(zoom, zoom);
                    DrawShape(graphics, draft, true);
                    graphics.Restore(gstate);
                }
                DrawAiRegion(graphics);
                DrawSelection(graphics);
                DrawAnnotationPreview(graphics);
                DrawBrushCursor(graphics);
                if (hasCrop && cropRect.Width > 1f && cropRect.Height > 1f)
                {
                    DrawCropGuide(graphics);
                }
                RectangleF rectangleF2 = ImageContentRect();
                using (Pen pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1f))
                {
                    graphics.DrawRectangle(pen, rectangleF2.X - 0.5f, rectangleF2.Y - 0.5f, rectangleF2.Width + 1f, rectangleF2.Height + 1f);
                }
                }
                graphics.Restore(canvasState);
                DrawTitleBar(graphics);
                DrawToolbarBg(graphics);
                DrawActionBarBg(graphics);
            }
        }

        private void DrawChecker(Graphics g)
        {
            Rectangle viewRect = ViewRect;
            using (SolidBrush brush = new SolidBrush(Dsh.LightTheme ? Color.FromArgb(226,229,234) : Color.FromArgb(28,32,39)))
            {
                g.FillRectangle(brush, viewRect);
            }
        }

        private void DrawTitleBar(Graphics g)
        {
            Rectangle rect = new Rectangle(0, 0, WinW, 30);
            using (SolidBrush brush = new SolidBrush(PanelFillSoft))
            {
                g.FillRectangle(brush, rect);
            }
            string text = UiText.T(ImageSaveState())+" · " + UiText.T(CurrentToolLabel()) + "  ·  " + Src.Width + " × " + Src.Height + "  ·  " + Math.Round(zoom * 100f) + "%";
            int statusSpace = status.Length > 0 ? Math.Max(120,WinW / 2 - 24) : 0;
            Dsh.DrawText(g, text, ToolFont, Ink, new Rectangle(14, 0, Math.Max(1,WinW - statusSpace - 40), 30), ContentAlignment.MiddleLeft);
            if (status.Length > 0)
            {
                int width = statusSpace;
                Dsh.DrawText(g, status, ToolFont, InkMuted, new Rectangle(WinW - width - 14, 0, width, 30), ContentAlignment.MiddleRight);
            }
            Dsh.HairLine(g, 0, 29, WinW, PanelHairline);
        }

        private void DrawToolbarBg(Graphics g)
        {
            Rectangle rect = new Rectangle(0, 30, WinW, ViewTop - 30);
            using (SolidBrush brush = new SolidBrush(PanelFill))
            {
                g.FillRectangle(brush, rect);
            }
            if (annotationInput != null)
                Dsh.DrawText(g, "输入文字", ToolFont, Ink, new Rectangle(12, annotationInput.Top, 104, annotationInput.Height), ContentAlignment.MiddleLeft);
            Dsh.HairLine(g, 0, ViewTop - 1, WinW, PanelHairline);
        }

        private void DrawActionBarBg(Graphics g)
        {
            int y = WinH - 50;
            using (SolidBrush brush = new SolidBrush(PanelFillSoft))
            {
                g.FillRectangle(brush, new Rectangle(0, y, WinW, 50));
            }
            Dsh.HairLine(g, 0, y, WinW, PanelHairline);
            string text = UiText.T("滚轮缩放 · 空格或中键拖动平移 · Ctrl+Z 撤销 · Ctrl+S 保存 · Esc 放弃");
            Dsh.DrawText(g, text, ToolFont, InkFaint, new Rectangle(14, y, Math.Max(0, WinW - 28), 50), ContentAlignment.MiddleLeft);
        }

        private void DrawCropGuide(Graphics g)
        {
            RectangleF a = ImageRectToScreen(cropRect);
            using (Pen pen = new Pen(Color.White, 1.5f))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(pen, a.X, a.Y, a.Width, a.Height);
            }
        }

        private RectangleF ImageRectToScreen(RectangleF img)
        {
            RectangleF rectangleF = ContentOnScreen();
            return new RectangleF(rectangleF.X + img.X * zoom, rectangleF.Y + img.Y * zoom, img.Width * zoom, img.Height * zoom);
        }

        private void EnsureComposite()
        {
            if (composite != null && builtVersion == commitVersion && Math.Abs(builtZoom - zoom) < 0.0001f)
            {
                compOrigin = new Point(ViewRect.Left + (ViewRect.Width - composite.Width) / 2, (ViewRect.Height - composite.Height) / 2 + ViewRect.Y);
                return;
            }
            int num = Math.Max(1, ViewRect.Width);
            int num2 = Math.Max(1, ViewRect.Height);
            int num3 = Math.Max(1, (int)Math.Ceiling((float)Src.Width * zoom));
            int num4 = Math.Max(1, (int)Math.Ceiling((float)Src.Height * zoom));
            int num5 = Math.Max(num, num3);
            int num6 = Math.Max(num2, num4);
            Bitmap bitmap = composite;
            Bitmap image = new Bitmap(num5, num6, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.InterpolationMode = ((zoom >= 1f) ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic);
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                int num7 = (num5 - num3) / 2;
                int num8 = (num6 - num4) / 2;
                graphics.DrawImage(Src, new Rectangle(num7, num8, num3, num4));
                GraphicsState gstate = graphics.Save();
                graphics.TranslateTransform(num7, num8);
                graphics.ScaleTransform(zoom, zoom);
                Bitmap bitmap2 = new Bitmap(Math.Max(1, Src.Width), Math.Max(1, Src.Height), PixelFormat.Format32bppArgb);
                Bitmap bitmap3 = exportBase;
                try
                {
                    using (Graphics graphics2 = Graphics.FromImage(bitmap2))
                    {
                        graphics2.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics2.DrawImage(Src, 0, 0, Src.Width, Src.Height);
                    }
                    foreach (Shape shape in shapes)
                    {
                        if (shape.Kind == Shape.K.Mosaic || shape.Kind == Shape.K.Blur)
                        {
                            exportBase = bitmap2;
                            DrawShape(graphics, shape, false);
                            exportBase = null;
                        }
                        else
                        {
                            DrawShape(graphics, shape, false);
                        }
                        using (Graphics graphics3 = Graphics.FromImage(bitmap2))
                        {
                            graphics3.SmoothingMode = SmoothingMode.AntiAlias;
                            exportBase = bitmap2;
                            DrawShape(graphics3, shape, false);
                            exportBase = null;
                        }
                    }
                }
                finally
                {
                    exportBase = bitmap3;
                    bitmap2.Dispose();
                }
                graphics.Restore(gstate);
                compOrigin = new Point(ViewRect.Left + (num - num5) / 2, (num2 - num6) / 2 + ViewRect.Y);
            }
            composite = image;
            builtZoom = zoom;
            builtVersion = commitVersion;
            if (bitmap != null)
            {
                bitmap.Dispose();
            }
        }

        private Point CompositeToImageOffset()
        {
            Math.Max(1, ViewRect.Width);
            Math.Max(1, ViewRect.Height);
            int width = composite.Width;
            int height = composite.Height;
            int num = Math.Max(1, (int)Math.Ceiling((float)Src.Width * zoom));
            int num2 = Math.Max(1, (int)Math.Ceiling((float)Src.Height * zoom));
            return new Point(compOrigin.X + (width - num) / 2, compOrigin.Y + (height - num2) / 2);
        }

        private void DrawShape(Graphics g, Shape s, bool preview)
        {
            GraphicsState state=g.Save();
            try {
                if(!s.AiClip.IsEmpty) g.SetClip(s.AiClip,CombineMode.Intersect);
                if(s.Kind==Shape.K.Eraser){
                    using(Region region=BrushRegion(s)){g.SetClip(region,CombineMode.Intersect);if(preview){using(SolidBrush backing=new SolidBrush(Dsh.LightTheme ? Color.FromArgb(226,229,234) : Color.FromArgb(28,32,39)))g.FillRectangle(backing,s.Rect);g.CompositingMode=CompositingMode.SourceOver;}else g.CompositingMode=CompositingMode.SourceCopy;g.DrawImage(Src,new Rectangle(0,0,Src.Width,Src.Height),0,0,Src.Width,Src.Height,GraphicsUnit.Pixel);}
                }else if(s.Brush && EffectTool(s.Kind)){
                    using(Region brushRegion=BrushRegion(s)){
                        g.SetClip(brushRegion,CombineMode.Intersect);
                        if(preview){using(SolidBrush hint=new SolidBrush(Color.FromArgb(120,80,140,210)))g.FillRectangle(hint,s.Rect);}
                        else DrawShapeCore(g,s,false);
                    }
                }else DrawShapeCore(g,s,preview);
            } finally {g.Restore(state);}
        }

        private void DrawShapeCore(Graphics g, Shape s, bool preview)
        {
            if (s.Kind == Shape.K.Redact)
            {
                if (preview) { using (Pen guide = new Pen(s.Color,1.5f/Math.Max(0.1f,zoom))) { guide.DashStyle=DashStyle.Dash; g.DrawRectangle(guide,s.Rect.X,s.Rect.Y,s.Rect.Width,s.Rect.Height); } }
                else { using (SolidBrush fill = new SolidBrush(Color.FromArgb(255,s.Color))) g.FillRectangle(fill,s.Rect); }
                return;
            }
            if (s.Kind == Shape.K.Blur)
            {
                if (preview) { using (Pen guide = new Pen(s.Color,1.5f/Math.Max(0.1f,zoom))) { guide.DashStyle=DashStyle.Dash; g.DrawRectangle(guide,s.Rect.X,s.Rect.Y,s.Rect.Width,s.Rect.Height); } }
                else DrawSoftBlur(g,s);
                return;
            }
            if (s.Kind == Shape.K.Mosaic)
            {
                if (preview)
                {
                    using (Pen guide = new Pen(s.Color, Math.Max(1, 1.5f / zoom)))
                    { guide.DashStyle = DashStyle.Dash; g.DrawRectangle(guide, s.Rect.X, s.Rect.Y, s.Rect.Width, s.Rect.Height); }
                    return;
                }
                DrawMosaic(g, s);
                return;
            }
            if (s.Kind == Shape.K.Text)
            {
                if (string.IsNullOrEmpty(s.Text))
                {
                    return;
                }
                using (Font font = Dsh.F(s.FontPx, FontStyle.Bold))
                {
                    using (SolidBrush brush = new SolidBrush(s.Color))
                    {
                        using (GraphicsPath graphicsPath = new GraphicsPath())
                        {
                            graphicsPath.AddString(s.Text, font.FontFamily, 1, font.Size * 96f / 72f, new PointF(s.Rect.X, s.Rect.Y), StringFormat.GenericTypographic);
                            using (Pen pen = new Pen(Color.FromArgb(112, 0, 0, 0), Math.Max(2f, s.Width + 2f)))
                            {
                                pen.LineJoin = LineJoin.Round;
                                g.DrawPath(pen, graphicsPath);
                            }
                            g.FillPath(brush, graphicsPath);
                            return;
                        }
                    }
                }
            }
            using (Pen pen2 = new Pen(s.Color, s.Width))
            {
                pen2.StartCap = LineCap.Round;
                pen2.EndCap = LineCap.Round;
                pen2.LineJoin = LineJoin.Round;
                switch (s.Kind)
                {
                case Shape.K.Pen:
                    if (s.Points.Count == 1)
                    {
                        PointF pointF = s.Points[0];
                        g.FillEllipse(new SolidBrush(s.Color), pointF.X - s.Width / 2f, pointF.Y - s.Width / 2f, s.Width, s.Width);
                    }
                    else if (s.Points.Count > 1)
                    {
                        g.DrawLines(pen2, s.Points.ToArray());
                    }
                    break;
                case Shape.K.Line:
                    g.DrawLine(pen2, s.Rect.Left, s.Rect.Top, s.Rect.Right, s.Rect.Bottom);
                    break;
                case Shape.K.Arrow:
                    DrawArrow(g, pen2, s);
                    break;
                case Shape.K.Rect:
                    g.DrawRectangle(pen2, s.Rect.X, s.Rect.Y, s.Rect.Width, s.Rect.Height);
                    break;
                case Shape.K.Ellipse:
                    g.DrawEllipse(pen2, s.Rect.X, s.Rect.Y, s.Rect.Width, s.Rect.Height);
                    break;
                }
            }
        }

        private void DrawArrow(Graphics g, Pen pen, Shape s)
        {
            PointF start = new PointF(s.Rect.Left, s.Rect.Top);
            PointF tip = new PointF(s.Rect.Right, s.Rect.Bottom);
            double dx = tip.X-start.X, dy = tip.Y-start.Y;
            double length = Math.Sqrt(dx*dx+dy*dy);
            if (length < .01) return;
            float ux=(float)(dx/length), uy=(float)(dy/length);
            float headLength=Math.Min(Math.Max(10f,s.Width*3.6f),(float)length*.8f);
            float halfWidth=headLength*.45f;
            PointF center=new PointF(tip.X-ux*headLength,tip.Y-uy*headLength);
            PointF left=new PointF(center.X-uy*halfWidth,center.Y+ux*halfWidth);
            PointF right=new PointF(center.X+uy*halfWidth,center.Y-ux*halfWidth);
            // Keep the shaft's end inside the triangle; a round cap at the tip
            // protrudes beyond the polygon and makes the arrowhead look circular.
            using (Pen shaft=(Pen)pen.Clone())
            {
                shaft.Width=Math.Min(pen.Width,headLength*.65f);
                shaft.EndCap=LineCap.Flat;
                g.DrawLine(shaft,start,new PointF(tip.X-ux*headLength*.8f,tip.Y-uy*headLength*.8f));
            }
            using (SolidBrush brush=new SolidBrush(s.Color))
                g.FillPolygon(brush,new PointF[] {tip,left,right});
        }

        private void DrawMosaic(Graphics g, Shape s)
        {
            if (!(s.Rect.Width < 2f) && !(s.Rect.Height < 2f))
            {
                RectangleF rect = s.Rect;
                int block = Math.Max(2, s.Block);
                int sw = Math.Max(1, (int)Math.Round(rect.Width));
                int sh = Math.Max(1, (int)Math.Round(rect.Height));
                GraphicsState gstate = g.Save();
                try
                {
                    g.PageUnit = GraphicsUnit.Pixel;
                    g.SmoothingMode = SmoothingMode.None;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    Bitmap srcBmp = UnderlyingBitmap();
                    PixelateFrom(g, srcBmp, (int)Math.Round(rect.X), (int)Math.Round(rect.Y), sw, sh, block);
                }
                catch (Exception ex)
                {
                    Diag.Log("马赛克绘制失败: " + ex.Message);
                }
                g.Restore(gstate);
            }
        }

        private Bitmap UnderlyingBitmap()
        {
            return exportBase ?? Src;
        }

        private static void PixelateFrom(Graphics g, Bitmap srcBmp, int dx, int dy, int sw, int sh, int block)
        {
            int num = Math.Max(1, (int)Math.Ceiling((double)sw / (double)block));
            int num2 = Math.Max(1, (int)Math.Ceiling((double)sh / (double)block));
            for (int i = 0; i < num2; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    int num3 = j * block;
                    int num4 = i * block;
                    int num5 = Math.Min(sw, num3 + block);
                    int num6 = Math.Min(sh, num4 + block);
                    if (num5 <= num3 || num6 <= num4)
                    {
                        continue;
                    }
                    long num7 = 0L;
                    long num8 = 0L;
                    long num9 = 0L;
                    int num10 = 0;
                    for (int k = num4; k < num6; k++)
                    {
                        for (int l = num3; l < num5; l++)
                        {
                            int num11 = dx + l;
                            int num12 = dy + k;
                            if (num11 >= 0 && num12 >= 0 && num11 < srcBmp.Width && num12 < srcBmp.Height)
                            {
                                Color pixel = srcBmp.GetPixel(num11, num12);
                                num7 += pixel.R;
                                num8 += pixel.G;
                                num9 += pixel.B;
                                num10++;
                            }
                        }
                    }
                    if (num10 != 0)
                    {
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(num7 / num10), (int)(num8 / num10), (int)(num9 / num10))))
                        {
                            g.FillRectangle(brush, dx + num3, dy + num4, num5 - num3, num6 - num4);
                        }
                    }
                }
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (Capture) return;
            maskDrawing=false;
            if(aiRegionDrawing) {aiRegionDrawing=false;SetAiRegion(Rectangle.Empty);}
            if (selectionDragging) CancelSelectionDrag();
            if (draft != null || panning)
            {
                draft = null;
                panning = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (compareBefore) return;
            EnsureZoomInit();
            Focus();
            if (InView(e.Location) && (e.Button == MouseButtons.Middle || (spaceDown && e.Button == MouseButtons.Left)))
            {
                panning = true;
                panStart = e.Location;
                panOrigin = pan;
                Capture = true;
                Cursor = Cursors.SizeAll;
            }
            else
            {
                if (e.Button != MouseButtons.Left || !InView(e.Location))
                {
                    return;
                }
                PointF p = ScreenToImage(e.Location);
                if(maskTool && !aiBusy) {maskDrawing=true;maskLast=ClampToImage(p);PaintMask(maskLast,maskLast);Capture=true;return;}
                if(aiRegionTool) { BeginAiRegion(ClampToImage(p));return; }
                if (tool == Shape.K.Select) { BeginSelectionDrag(ClampToImage(p)); return; }
                if (tool!=Shape.K.Eraser && !(effectBrush&&EffectTool(tool)) && (Control.ModifierKeys & Keys.Shift)==0 && TryBeginDirectDrag(ClampToImage(p))) return;
                if (tool == Shape.K.Number)
                {
                    Shape number = new Shape { Kind=Shape.K.Text, Text=(nextNumber++).ToString(), FontPx=22, Color=color, Width=strokeW, Rect=new RectangleF(ClampToImage(p),SizeF.Empty) };
                    AddShapeTracked(number); Invalidate(); return;
                }
                if (tool == Shape.K.Text)
                {
                    StartTextInput(ClampToImage(p));
                    return;
                }
                draft = new Shape();
                draft.Kind = tool;
                draft.Color = color;
                draft.Width = strokeW;
                draft.Block = mosaicBlock;
                draft.Brush=tool==Shape.K.Eraser||(effectBrush&&EffectTool(tool));
                if(draft.Brush)draft.Width=effectBrushSize;
                PointF item = ClampToImage(p);
                dragAnchor = item;
                Capture = true;
                if (tool == Shape.K.Pen || draft.Brush)
                {
                    draft.Points.Add(item);
                    if(draft.Brush)UpdateEffectStroke(draft,item);
                }
                else
                {
                    draft.Rect = new RectangleF(item.X, item.Y, 0f, 0f);
                }
                mouseIn = true;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if(maskDrawing) {PointF point=ClampToImage(ScreenToImage(e.Location));PaintMask(maskLast,point);maskLast=point;return;}
            if(aiRegionDrawing) { MoveAiRegion(ClampToImage(ScreenToImage(e.Location)));return; }
            if (selectionDragging) { MoveSelection(ClampToImage(ScreenToImage(e.Location))); return; }
            EnsureZoomInit();
            mouseIn = InView(e.Location);
            brushCursor=e.Location;
            if (panning)
            {
                pan = new Point(panOrigin.X + (e.X - panStart.X), panOrigin.Y + (e.Y - panStart.Y));
                ClampPan();
                Invalidate();
            }
            else
            {
                if (draft == null)
                {
                    UpdateToolCursor(ScreenToImage(e.Location));
                    if(tool==Shape.K.Eraser||(effectBrush&&EffectTool(tool)))Invalidate(ViewRect);
                    return;
                }
                PointF pointF = default(PointF);
                pointF = ClampToImage(ScreenToImage(e.Location));
                if(draft.Brush){UpdateEffectStroke(draft,pointF);Invalidate();return;}
                if (draft.Kind == Shape.K.Pen)
                {
                    if (draft.Points.Count == 0 || Math.Abs(draft.Points[draft.Points.Count - 1].X - pointF.X) > 0.6f || Math.Abs(draft.Points[draft.Points.Count - 1].Y - pointF.Y) > 0.6f)
                    {
                        draft.Points.Add(pointF);
                        Invalidate();
                    }
                    return;
                }
                if (draft.Kind == Shape.K.Mosaic)
                {
                    float x = Math.Min(dragAnchor.X, pointF.X);
                    float y = Math.Min(dragAnchor.Y, pointF.Y);
                    float width = Math.Abs(pointF.X - dragAnchor.X);
                    float height = Math.Abs(pointF.Y - dragAnchor.Y);
                    draft.Rect = new RectangleF(x, y, width, height);
                    Invalidate();
                    return;
                }
                PointF item = dragAnchor;
                if (draft.Kind == Shape.K.Line || draft.Kind == Shape.K.Arrow)
                {
                    if (draft.Points.Count == 0)
                    {
                        draft.Points.Add(item);
                    }
                    draft.Rect = new RectangleF(item.X, item.Y, pointF.X - item.X, pointF.Y - item.Y);
                }
                else
                {
                    draft.Rect = ConstrainedBox(item,pointF,Src.Size,(Control.ModifierKeys&Keys.Shift)!=0 && (draft.Kind==Shape.K.Rect||draft.Kind==Shape.K.Ellipse),(Control.ModifierKeys&Keys.Alt)!=0 && (draft.Kind==Shape.K.Rect||draft.Kind==Shape.K.Ellipse));
                }
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (selectionDragging && e.Button == MouseButtons.Left) { MoveSelection(ClampToImage(ScreenToImage(e.Location))); FinishSelectionDrag(); return; }
            if (panning && (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left))
            {
                panning = false;
                Capture = false;
                Cursor = Cursors.Default;
            }
            else
            {
                if (draft == null || e.Button != MouseButtons.Left)
                {
                    return;
                }
                OnMouseMove(e);
                Shape shape = draft;
                draft = null;
                Capture = false;
                if ((shape.Kind == Shape.K.Pen) ? (shape.Points.Count > 0) : ((shape.Kind != Shape.K.Mosaic) ? (Math.Abs(shape.Rect.Width) >= 2f || Math.Abs(shape.Rect.Height) >= 2f) : (shape.Rect.Width >= 3f && shape.Rect.Height >= 3f)))
                {
                    AddShapeTracked(shape);
                    selectedShape=shape.Kind==Shape.K.Eraser ? null : shape;
                    commitVersion++;
                    RectangleF rectangleF = ((shape.Kind == Shape.K.Line || shape.Kind == Shape.K.Arrow) ? RectFromPoints(shape.Rect.Left, shape.Rect.Top, shape.Rect.Right, shape.Rect.Bottom) : shape.Rect);
                    if (!shape.Brush && rectangleF.Width >= 8f && rectangleF.Height >= 8f)
                    {
                        cropRect = rectangleF;
                        hasCrop = true;
                        SetStatus("已框出区域 · 点「裁剪」可裁掉其余部分");
                    }
                }
                Invalidate();
                try
                {
                    Refresh();
                }
                catch
                {
                }
            }
        }

        private static RectangleF RectFromPoints(float x1, float y1, float x2, float y2)
        {
            return new RectangleF(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (Control.ModifierKeys == Keys.Control)
            {
                ZoomBy((e.Delta > 0) ? 1.15f : 0.86956525f);
            }
            else
            {
                ZoomAt((e.Delta > 0) ? 1.15f : 0.86956525f, e.Location);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (base.ActiveControl is TextBoxBase && (IsTextEditingKey(keyData) || keyData == Keys.Escape))
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
            if(NudgeSelected(keyData))return true;
            if (keyData == Keys.Delete && selectedShape != null && !compareBefore) { DeleteSelected(); return true; }
            switch (keyData)
            {
            case Keys.Z | Keys.Control:
                DoUndo();
                return true;
            case Keys.Y | Keys.Control:
                DoRedo();
                return true;
            case Keys.S | Keys.Control | Keys.Shift:
                SaveEditedImage(true);
                return true;
            case Keys.S | Keys.Control:
                SaveEditedImage(false);
                return true;
            case Keys.C | Keys.Control:
                AcceptAndClose(false);
                return true;
            case Keys.Escape:
                if(aiRegionTool || aiRegionDrawing) {aiRegionDrawing=false;Capture=false;aiRegionTool=false;SetAiRegion(Rectangle.Empty);AiSetStatus(UiText.T("已清除 AI 选区"));return true;}
                if (aiBusy) { CancelAiRequest(); return true; }
                CancelAndClose();
                return true;
            case Keys.D0 | Keys.Control:
            case Keys.NumPad0 | Keys.Control:
                FitToWindow();
                return true;
            case Keys.D1 | Keys.Control:
            case Keys.NumPad1 | Keys.Control:
                SetZoom(1f, true);
                return true;
            default:
            {
                int num = ((Control.ModifierKeys == Keys.Shift) ? 40 : 10);
                switch (keyData)
                {
                case Keys.Left:
                    pan.X += num;
                    commitVersion++;
                    Invalidate();
                    return true;
                case Keys.Right:
                    pan.X -= num;
                    commitVersion++;
                    Invalidate();
                    return true;
                case Keys.Up:
                    pan.Y += num;
                    commitVersion++;
                    Invalidate();
                    return true;
                case Keys.Down:
                    pan.Y -= num;
                    commitVersion++;
                    Invalidate();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
                }
            }
            }
        }

        private static bool IsTextEditingKey(Keys keyData)
        {
            if ((keyData & Keys.Control) != Keys.Control)
            {
                return false;
            }
            Keys keys = keyData & Keys.KeyCode;
            if (keys != Keys.C && keys != Keys.X && keys != Keys.V && keys != Keys.A)
            {
                return keys == Keys.Z;
            }
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space)
            {
                spaceDown = true;
                Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space)
            {
                spaceDown = false;
                Cursor = Cursors.Default;
            }
        }

        private void DrawAnnotationPreview(Graphics graphics)
        {
            if (annotationInput == null) return;
            RectangleF content = ImageContentRect();
            Shape preview = new Shape { Kind = Shape.K.Text, Color = color, Width = strokeW, FontPx = fontPx,
                Text = annotationInput.Text, Rect = new RectangleF(annotationAnchor.X, annotationAnchor.Y, 0, 0) };
            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(content.X, content.Y);
            graphics.ScaleTransform(zoom, zoom);
            DrawShape(graphics, preview, true);
            graphics.Restore(state);
            using (Font font = Dsh.F(fontPx, FontStyle.Bold))
            {
                SizeF size = graphics.MeasureString(string.IsNullOrEmpty(preview.Text) ? " " : preview.Text, font);
                RectangleF bounds = ImageRectToScreen(new RectangleF(annotationAnchor.X, annotationAnchor.Y,
                    Math.Max(28 / zoom, size.Width), Math.Max(fontPx * 1.5f, size.Height)));
                using (Pen guide = new Pen(Color.FromArgb(180, 220, 235, 255), 1))
                { guide.DashStyle = DashStyle.Dash; graphics.DrawRectangle(guide, bounds.X - 2, bounds.Y - 2, bounds.Width + 4, bounds.Height + 4); }
            }
        }

        private void StartTextInput(PointF imgPt)
        {
            CommitOpenTextInput();
            TextBox tb = new TextBox();
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = Dsh.F(12f, FontStyle.Regular);
            tb.BackColor = Dsh.GlassFillStrong;
            tb.ForeColor = Dsh.TextPrimary;
            annotationInput = tb;
            annotationAnchor = imgPt;
            hasCrop = false;
            base.Controls.Add(tb);
            LayoutChrome();
            commitVersion++;
            SetStatus("图片上透明预览 · Enter 确认 · Esc 取消");
            tb.TextChanged += delegate { Invalidate(ViewRect); };
            tb.Focus();
            EventHandler commit = delegate
            {
                if (tb.Tag is EventHandler)
                {
                    tb.Tag = null;
                    string text = tb.Text;
                    annotationInput = null;
                    base.Controls.Remove(tb);
                    tb.Dispose();
                    LayoutChrome();
                    commitVersion++;
                    if (!string.IsNullOrEmpty(text))
                    {
                        Shape s = new Shape
                        {
                            Kind = Shape.K.Text,
                            Color = color,
                            Width = strokeW,
                            FontPx = fontPx,
                            Text = text,
                            Rect = new RectangleF(imgPt.X, imgPt.Y, 0f, 0f)
                        };
                        AddShapeTracked(s);
                        commitVersion++;
                    }
                    Invalidate();
                }
            };
            tb.Tag = commit;
            tb.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Return)
                {
                    e.SuppressKeyPress = true;
                    commit(null, null);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    tb.Text = "";
                    commit(null, null);
                }
            };
            tb.LostFocus += delegate
            {
                if (tb.Parent != null)
                {
                    commit(null, null);
                }
            };
        }

        private void BeginGroup()
        {
            if (pendingGroup == null)
            {
                pendingGroup = new UndoStep();
                pendingGroup.Kind = UndoStep.T.Shapes;
            }
        }

        private void CommitGroup()
        {
            UndoStep undoStep = pendingGroup;
            pendingGroup = null;
            if (undoStep != null && undoStep.Items.Count != 0)
            {
                ClearRedoHistory();
                undoSteps.Add(undoStep);
                BoundHistory();
            }
        }

        private void AddShapeTracked(Shape s)
        {
            editRevision++;
            shapes.Add(s);
            if (pendingGroup != null)
            {
                pendingGroup.Items.Add(s.Clone());
            }
            else
            {
                UndoStep undoStep = new UndoStep();
                undoStep.Kind = UndoStep.T.Shapes;
                undoStep.Items.Add(s.Clone());
                ClearRedoHistory();
                undoSteps.Add(undoStep);
                BoundHistory();
            }
            commitVersion++;
        }

        private void DoUndo()
        {
            if (selectionDragging) FinishSelectionDrag();
            selectedShape = null;
            compareBefore = false;
            if (undoSteps.Count == 0)
            {
                SetStatus("没有可撤销的操作");
                return;
            }
            UndoStep undoStep = undoSteps[undoSteps.Count - 1];
            undoSteps.RemoveAt(undoSteps.Count - 1);
            if (undoStep.Kind == UndoStep.T.Shapes)
            {
                for (int i = 0; i < undoStep.Items.Count; i++)
                {
                    if (shapes.Count <= 0)
                    {
                        break;
                    }
                    shapes.RemoveAt(shapes.Count - 1);
                }
            }
            else
            {
                SwapSnapshotState(undoStep);
            }
            editRevision++;
            undone.Add(undoStep);
            commitVersion++;
            BoundHistory();
            hasCrop = false;
            cropRect = RectangleF.Empty;
            SetStatus((undoStep.Kind == UndoStep.T.Shapes) ? ("已撤销 " + undoStep.Items.Count + " 个标注") : (undoStep.Kind == UndoStep.T.Edit ? "已撤销标注编辑" : (undoStep.Kind == UndoStep.T.Ai ? "已撤销 AI 修改" : (undoStep.Kind == UndoStep.T.Reset ? "已撤销重置" : "已撤销裁剪"))));
            Invalidate();
        }

        private void DoRedo()
        {
            if (selectionDragging) FinishSelectionDrag();
            selectedShape = null;
            compareBefore = false;
            if (undone.Count == 0)
            {
                SetStatus("没有可重做的操作");
                return;
            }
            UndoStep undoStep = undone[undone.Count - 1];
            undone.RemoveAt(undone.Count - 1);
            if (undoStep.Kind == UndoStep.T.Shapes)
            {
                foreach (Shape item in undoStep.Items)
                {
                    shapes.Add(item.Clone());
                }
            }
            else
            {
                SwapSnapshotState(undoStep);
                hasCrop = false;
                cropRect = RectangleF.Empty;
                SetStatus(undoStep.Kind == UndoStep.T.Edit ? "已重做标注编辑" : (undoStep.Kind == UndoStep.T.Ai ? "已重做 AI 修改" : (undoStep.Kind == UndoStep.T.Reset ? "已重做重置" : "已重做裁剪")));
            }
            undoSteps.Add(undoStep);
            BoundHistory();
            editRevision++;
            commitVersion++;
            Invalidate();
        }

        // 编辑器与历史步骤交换底图所有权和标注，避免快照重复释放。
        private void SwapSnapshotState(UndoStep step)
        {
            selectedShape = null;
            Bitmap currentSource = srcMutable;
            bool currentHadSource = srcMutable != null;
            List<Shape> currentShapes = new List<Shape>();
            foreach (Shape shape in shapes) currentShapes.Add(shape.Clone());
            srcMutable = null;
            RestoreSource(step.Snapshot, step.HadSource);
            shapes.Clear();
            foreach (Shape shape in step.Items) shapes.Add(shape.Clone());
            step.Snapshot = currentSource;
            step.HadSource = currentHadSource;
            step.Items = currentShapes;
        }

        // Keep the closest undo/redo state even when a single image exceeds the budget.
        private void BoundHistory()
        {
            if (aiApplying) return;
            long bytes = 0;
            foreach (UndoStep step in undoSteps) bytes += HistoryBytes(step);
            foreach (UndoStep step in undone) bytes += HistoryBytes(step);
            while (undoSteps.Count + undone.Count > 1 &&
                (undoSteps.Count + undone.Count > 40 || bytes > 96L * 1024 * 1024))
            {
                List<UndoStep> list = undoSteps.Count > 1 ? undoSteps :
                    (undone.Count > 1 ? undone : (undoSteps.Count > 0 ? undoSteps : undone));
                UndoStep oldest = list[0];
                bytes -= HistoryBytes(oldest);
                list.RemoveAt(0);
                oldest.DisposeSnap();
            }
        }

        private static long HistoryBytes(UndoStep step)
        {
            long bytes = step.Snapshot == null ? 0 : (long)step.Snapshot.Width * step.Snapshot.Height * 4;
            foreach (Shape shape in step.Items)
                bytes += 128L + shape.Points.Count * 8L + (shape.Text == null ? 0 : shape.Text.Length * 2L);
            return bytes;
        }

        private void ClearRedoHistory()
        {
            foreach (UndoStep step in undone) step.DisposeSnap();
            undone.Clear();
        }

        private void RestoreSource(Bitmap snap, bool hadSource)
        {
            SetAiRegion(Rectangle.Empty);
            if (srcMutable != null)
            {
                srcMutable.Dispose();
                srcMutable = null;
            }
            if (hadSource && snap != null)
            {
                srcMutable = snap;
            }
            zoomInitialized = false;
            EnsureZoomInit();
            FitToWindow();
        }

        private void ApplyCrop()
        {
            if (hasCrop && cropRect.Width > 2f && cropRect.Height > 2f)
            {
                CommitCrop(cropRect);
            }
            else
            {
                SetStatus("先用任意工具拖出一块区域，再点「裁剪」");
            }
        }

        private void CommitCrop(RectangleF r)
        {
            Rectangle rect = Rectangle.Round(r);
            rect.Intersect(new Rectangle(0, 0, Src.Width, Src.Height));
            if (rect.Width < 2 || rect.Height < 2)
            {
                SetStatus("裁剪区域太小");
                return;
            }
            Bitmap bitmap2;
            using (Bitmap rendered = RenderResult())
            {
                bitmap2 = rendered.Clone(rect, PixelFormat.Format32bppArgb);
            }
            UndoStep undoStep = new UndoStep();
            undoStep.Kind = UndoStep.T.Crop;
            foreach (Shape shape in shapes) undoStep.Items.Add(shape.Clone());
            undoStep.HadSource = srcMutable != null;
            undoStep.Snapshot = srcMutable;
            srcMutable = null;
            ClearRedoHistory();
            undoSteps.Add(undoStep);
            ReplaceSourceAndRefit(bitmap2);
            shapes.Clear();
            BoundHistory();
            hasCrop = false;
            cropRect = RectangleF.Empty;
            SetStatus("已裁剪为 " + bitmap2.Width + " × " + bitmap2.Height + "（可 Ctrl+Z 撤销）");
        }

        private void OnSourceReplaced(Bitmap bmp)
        {
            SetAiRegion(Rectangle.Empty);
            if (srcMutable != null)
            {
                srcMutable.Dispose();
            }
            editRevision++;
            srcMutable = bmp;
            zoomInitialized = false;
            commitVersion++;
        }

        private void ReplaceSourceAndRefit(Bitmap bmp)
        {
            OnSourceReplaced(bmp);
            EnsureZoomInit();
            FitToWindow();
        }

        private void ResetAll()
        {
            SetAiRegion(Rectangle.Empty);
            editRevision++;
            UndoStep undoStep = new UndoStep();
            undoStep.Kind = UndoStep.T.Reset;
            undoStep.Items = new List<Shape>();
            foreach (Shape shape in shapes)
            {
                undoStep.Items.Add(shape.Clone());
            }
            undoStep.HadSource = srcMutable != null;
            undoStep.Snapshot = srcMutable;
            srcMutable = null;
            ClearRedoHistory();
            undoSteps.Add(undoStep);
            shapes.Clear();
            selectedShape = null;
            hasCrop = false;
            cropRect = RectangleF.Empty;
            zoomInitialized = false;
            EnsureZoomInit();
            commitVersion++;
            BoundHistory();
            SetStatus("已重置（可 Ctrl+Z 撤销）");
            Invalidate();
        }

        private void DisposeUndoStacks()
        {
            for (int i = 0; i < undoSteps.Count; i++)
            {
                undoSteps[i].DisposeSnap();
            }
            for (int j = 0; j < undone.Count; j++)
            {
                undone[j].DisposeSnap();
            }
            undoSteps.Clear();
            ClearRedoHistory();
            if (pendingGroup != null)
            {
                pendingGroup.DisposeSnap();
                pendingGroup = null;
            }
        }

        private void SetStatus(string text)
        {
            status = text;
            statusVersion++;
            Invalidate();
        }

        public Bitmap RenderResult()
        {
            Bitmap bitmap = new Bitmap(Src.Width, Src.Height, PixelFormat.Format32bppArgb);
            bool flag = exporting;
            Bitmap bitmap2 = exportBase;
            try
            {
                exporting = true;
                exportBase = null;
                Bitmap bitmap3 = new Bitmap(Src.Width, Src.Height, PixelFormat.Format32bppArgb);
                try
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap3))
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(Src, 0, 0, Src.Width, Src.Height);
                    }
                    using (Graphics graphics2 = Graphics.FromImage(bitmap))
                    {
                        graphics2.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics2.DrawImage(Src, 0, 0, Src.Width, Src.Height);
                        for (int i = 0; i < shapes.Count; i++)
                        {
                            Shape shape = shapes[i];
                            if (shape.Kind == Shape.K.Mosaic || shape.Kind == Shape.K.Blur)
                            {
                                exportBase = bitmap3;
                                DrawShape(graphics2, shape, false);
                                exportBase = null;
                            }
                            else
                            {
                                DrawShape(graphics2, shape, false);
                            }
                            using (Graphics graphics3 = Graphics.FromImage(bitmap3))
                            {
                                graphics3.SmoothingMode = SmoothingMode.AntiAlias;
                                graphics3.CompositingMode = CompositingMode.SourceOver;
                                exportBase = bitmap3;
                                DrawShape(graphics3, shape, false);
                                exportBase = null;
                            }
                        }
                        return bitmap;
                    }
                }
                finally
                {
                    bitmap3.Dispose();
                }
            }
            finally
            {
                exporting = flag;
                exportBase = bitmap2;
            }
        }

        private void AcceptAndClose(bool alsoSaveFile)
        {
            if (aiBusy) { AiSetStatus(UiText.T("AI 正在修改，请等待完成后确认；也可放弃截图。")); return; }
            if (compareBefore) { AiSetStatus(UiText.T("当前显示修改前，请先切回修改后再确认输出。")); return; }
            CommitOpenTextInput();
            ExitMode = (alsoSaveFile ? EditorExit.Save : EditorExit.Copy);
            base.DialogResult = DialogResult.OK;
            Close();
        }

        private void CommitOpenTextInput()
        {
            TextBox textBox = base.ActiveControl as TextBox;
            if (textBox != null && textIsEditorInput(textBox))
            {
                EventHandler eventHandler = textBox.Tag as EventHandler;
                if (eventHandler != null)
                {
                    eventHandler(null, EventArgs.Empty);
                }
            }
        }

        private bool textIsEditorInput(TextBox tb)
        {
            if (tb != null)
            {
                return tb.Tag is EventHandler;
            }
            return false;
        }

        private void CancelAndClose()
        {
            aiRequestId++;
            aiBusy = false;
            ExitMode = EditorExit.Cancel;
            base.DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (composite != null)
            {
                composite.Dispose();
                composite = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                ResetImageEditMask();
                if (aiMotionTimer != null) { aiMotionTimer.Stop(); aiMotionTimer.Dispose(); }
                aiMotionWatch.Stop();
                aiRequestId++;
                if (selectionCheckpoint != null) selectionCheckpoint.DisposeSnap();
                ClearAiComparison();
                if (aiCancellation != null) aiCancellation.Cancel();
                DisposeUndoStacks();
                if (composite != null)
                {
                    composite.Dispose();
                    composite = null;
                }
                if (exportBase != null)
                {
                    exportBase.Dispose();
                    exportBase = null;
                }
                if (srcMutable != null && !object.ReferenceEquals(srcMutable, src)) src.Dispose();
                Bitmap bitmap = ((srcMutable != null) ? srcMutable : src);
                srcMutable = null;
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
            }
            base.Dispose(disposing);
        }

        public static Color ParseColor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Color.FromArgb(232, 59, 59);
            }
            switch (name.Trim().ToLowerInvariant())
            {
            case "red":
                return Color.FromArgb(232, 59, 59);
            case "yellow":
                return Color.FromArgb(255, 193, 7);
            case "green":
                return Color.FromArgb(24, 169, 91);
            case "blue":
                return Color.FromArgb(45, 140, 240);
            case "black":
                return Color.FromArgb(17, 17, 19);
            case "white":
                return Color.White;
            default:
                return Color.FromArgb(232, 59, 59);
            }
        }
    }
}
