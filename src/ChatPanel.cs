using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal partial class ChatPanel : UserControl
    {
        private const int Pad = 14;

        private const int TitleH = 44;

        private static Color PanelFill { get { return Dsh.Canvas; } }

        private static Color PanelHairline { get { return Dsh.SidebarEdge; } }

        private static Color Ink { get { return Dsh.TextPrimary; } }

        private static Color InkFaint { get { return Dsh.TextTertiary; } }

        private static Color InputFill { get { return Dsh.SidebarTop; } }

        private static Color LogFill { get { return Dsh.GlassFill; } }

        public float FadeProgress = 1f;

        private readonly TrayApp app;

        private readonly Config cfg;

        private ChatMessageView logBox;
        private ChatGlassSurface conversationCard;

        private TextBox inputBox;

        private DshInputFrame inputFrame;

        private Label statusLabel;

        private Label modelLabel;

        private DshToggle chkIncludeShot;

        private readonly List<DshButton> quickButtons = new List<DshButton>();

        private DshButton btnSend;
        private DshButton clearConversation;
        private Timer requestTimer;
        private readonly System.Diagnostics.Stopwatch requestElapsed = new System.Diagnostics.Stopwatch();


        private Panel titleBar;

        private Panel bottomBar;

        public event EventHandler CloseRequested;

        public ChatPanel(TrayApp owner, Config config)
        {
            app = owner;
            cfg = config;
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = PanelFill;
            DoubleBuffered = true;
            BuildUi();
            AiChat.Init();
            AiChat.Changed += OnChatChanged;
            RefreshLog();
        }

        public void FocusInput()
        {
            if (inputBox != null && inputBox.Visible)
            {
                inputBox.Focus();
            }
        }

        public void RefreshModel()
        {
            UpdateModelLabel();
            RefreshAttachmentPreview();
        }

        private void BuildUi()
        {
            titleBar = new Panel();
            titleBar.BackColor = PanelFill;
            base.Controls.Add(titleBar);
            DshLabel dshLabel = new DshLabel("AI 对话");
            dshLabel.Font = Dsh.F(14f, FontStyle.Bold);
            dshLabel.Ink = Ink;
            dshLabel.SetBounds(14, 10, 100, 24);
            titleBar.Controls.Add(dshLabel);
            modelLabel = new Label();
            modelLabel.ForeColor = InkFaint;
            modelLabel.BackColor = Color.Transparent;
            modelLabel.Font = Dsh.F(11f, FontStyle.Regular);
            modelLabel.AutoEllipsis = true;
            modelLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleBar.Controls.Add(modelLabel);
            clearConversation = new DshButton("清空会话", DshButton.Kind.Ghost);
            clearConversation.Click += delegate { if(AiChat.Busy) AiChat.Cancel(); AiChat.Clear(); inputBox.Clear(); DisposeAttachmentImages(); if(app!=null) app.ClearScreenshotCache(); chkIncludeShot.Checked=false; RefreshAttachmentPreview(); RefreshLog(); SetStatus("会话、附件与截图缓存已清空"); };
            titleBar.Controls.Add(clearConversation);

            logBox = new ChatMessageView();
            conversationCard = new ChatGlassSurface();
            logBox.Dock = DockStyle.Fill; conversationCard.Controls.Add(logBox);
            base.Controls.Add(conversationCard);
            bottomBar = new Panel();
            bottomBar.BackColor = PanelFill;
            base.Controls.Add(bottomBar);
            string[] array = new string[3] { "看图建议", "OCR 提取", "图片修改" };
            string[] array2 = new string[3]
            {
                "看看这张截图，有哪些值得注意或需要标注的地方？",
                AiClient.OcrPrompt(),
                "用中文简要描述这张截图的内容。"
            };
            for (int num = 0; num < array.Length; num++)
            {
                DshButton dshButton = new DshButton(array[num]);
                string prompt = array2[num];
                dshButton.Click += delegate
                {
                    if (prompt == array2[2]) OpenImageEditing(); else Send(prompt);
                };
                quickButtons.Add(dshButton);
                bottomBar.Controls.Add(dshButton);
            }
            BuildAttachmentUi();
            chkIncludeShot = new DshToggle("自动带上最新截图");
            chkIncludeShot.Checked = false;
            chkIncludeShot.CheckedChanged += delegate { RefreshAttachmentPreview(); };
            bottomBar.Controls.Add(chkIncludeShot);
            inputBox = new TextBox();
            inputBox.Multiline = true;
            inputBox.BorderStyle = BorderStyle.FixedSingle;
            inputBox.BackColor = InputFill;
            inputBox.ForeColor = Ink;
            inputBox.Font = Dsh.F(12.5f, FontStyle.Regular);
            inputBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Return && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    Send(null);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    if (AiChat.Busy) AiChat.Cancel(); else RaiseClose();
                }
            };
            inputBox.BackColor = Dsh.GlassFillStrong;
            inputFrame = new DshInputFrame(inputBox); inputFrame.GlassLook = true;
            bottomBar.Controls.Add(inputFrame);
            btnSend = new DshButton(UiText.T("发送"), DshButton.Kind.Primary);
            btnSend.Click += delegate
            {
                if (AiChat.Busy) AiChat.Cancel(); else Send(null);
            };
            bottomBar.Controls.Add(btnSend);
            statusLabel = new Label();
            statusLabel.ForeColor = InkFaint;
            statusLabel.BackColor = Color.Transparent;
            statusLabel.Font = Dsh.F(10f, FontStyle.Regular);
            statusLabel.AutoEllipsis = true;
            bottomBar.Controls.Add(statusLabel);
            LayoutUi();
            UpdateModelLabel();
        }

        private void RaiseClose()
        {
            EventHandler closeRequested = this.CloseRequested;
            if (closeRequested != null)
            {
                closeRequested(this, EventArgs.Empty);
            }
        }

        private void UpdateModelLabel()
        {
            if (modelLabel != null)
            {
                AiProvider aiProvider = cfg.ActiveProvider();
                modelLabel.Text = ((aiProvider == null) ? "未选择模型" : (aiProvider.Name + " · " + aiProvider.FirstModel));
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutUi();
        }

        private void LayoutUi()
        {
            if (titleBar == null || bottomBar == null || logBox == null || inputBox == null || btnSend == null || statusLabel == null || chkIncludeShot == null || quickButtons.Count == 0)
            {
                return;
            }
            int width = base.ClientSize.Width;
            int height = base.ClientSize.Height;
            if (width > 0 && height > 0)
            {
                titleBar.SetBounds(0, 0, width, 76);
                modelLabel.SetBounds(14, 47, Math.Max(1, width - 152), 24);
                clearConversation.SetBounds(Math.Max(14,width-124),48,108,26);
                int num = 250;
                int num2 = Math.Max(104, height - num);
                bottomBar.SetBounds(0, num2, width, height - num2);
                int num3 = width - 28;
                int num4 = Math.Max(70, (num3 - 12) / 3);
                int num5 = 14;
                for (int i = 0; i < quickButtons.Count; i++)
                {
                    quickButtons[i].SetBounds(num5, 8, num4, 30);
                    num5 += num4 + 6;
                }
                chkIncludeShot.SetBounds(14, 42, num3, 24);
                LayoutAttachmentUi(width);
                int num6 = 156;
                int num7 = 72;
                inputFrame.SetBounds(14, num6, Math.Max(50, width - 28 - num7 - 8), 62);
                btnSend.SetBounds(width - 14 - num7, num6, num7, 62);
                statusLabel.SetBounds(14, num6 + 64, num3, 18);
                int num8 = 82;
                int num9 = bottomBar.Top - num8 - 6;
                if (num9 < 60)
                {
                    num9 = 60;
                }
                conversationCard.SetBounds(14, num8 + 6, Math.Max(60, width - 28), Math.Max(60, num9 - 12));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.None;
            Dsh.HairLine(graphics, 18, 76, Math.Max(1, base.ClientSize.Width - 36), Color.FromArgb(35, Dsh.PrimaryFill));
            if (bottomBar != null)
            {
                Dsh.HairLine(graphics, 18, bottomBar.Top, Math.Max(1, base.ClientSize.Width - 36), Color.FromArgb(28, Dsh.PrimaryFill));
            }
            if (!(FadeProgress < 0.999f))
            {
                return;
            }
            int num = (int)Math.Round(255f * (1f - Math.Max(0f, Math.Min(1f, FadeProgress))));
            if (num > 0)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(num, PanelFill)))
                {
                    graphics.FillRectangle(brush, base.ClientRectangle);
                }
            }
        }

        private void OnChatChanged(object sender, EventArgs e)
        {
            RefreshLog();
        }

        private void RefreshLog()
        {
            if (logBox != null) logBox.SetMessages(AiChat.Messages);
        }

        private void SetStatus(string s)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = s;
            }
        }

        private void Send(string forced)
        {
            if (AiChat.Busy)
            {
                SetStatus("正在等模型回复…");
                return;
            }
            string text = forced;
            if (text == null)
            {
                text = inputBox.Text.Trim();
                if (text.Length == 0)
                {
                    return;
                }
                inputBox.Text = "";
            }
            UpdateModelLabel();
            bool flag = chkIncludeShot == null || chkIncludeShot.Checked;
            Image shot = selectedAttachment != null ? (Image)selectedAttachment.Clone() : ((flag && app != null) ? app.GetLastShotCopy() : null);
            if (requestTimer == null)
            {
                requestTimer = new Timer { Interval = 1000 };
                requestTimer.Tick += delegate { if (AiChat.Busy) SetStatus("等待模型回复 · " + (int)requestElapsed.Elapsed.TotalSeconds + " 秒 · 可取消"); };
            }
            requestElapsed.Restart(); requestTimer.Start();
            SetStatus(shot == null ? "正在发送文字 · 可取消" : "正在发送图片 · 可取消");
            btnSend.Text = UiText.T("取消"); btnSend.Enabled = true;
            foreach (DshButton button in quickButtons) button.Enabled = false;
            // AiChat clones the attachment synchronously, so this temporary copy can
            // be released immediately rather than retained for the entire request.
            using (shot) AiChat.Send(app, cfg, text, shot, delegate(AiReply reply, string err)
            {
                if (IsDisposed) return;
                requestTimer.Stop(); requestElapsed.Stop();
                btnSend.Text = UiText.T("发送"); btnSend.Enabled = true;
                foreach (DshButton button in quickButtons) button.Enabled = true;
                if (err != null && inputBox.TextLength == 0) inputBox.Text = text;
                SetStatus(err == null ? "回复完成" : err == "请求已取消" ? "请求已取消" : "请求失败 · 要求已保留，可重试");
                RefreshLog();
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AiChat.Changed -= OnChatChanged;
                if (AiChat.Busy) AiChat.Cancel();
                if (requestTimer != null) requestTimer.Dispose();
                DisposeAttachmentImages();
            }
            base.Dispose(disposing);
        }
    }
}
