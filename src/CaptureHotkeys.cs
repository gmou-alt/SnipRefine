using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class TrayApp
    {
        private HotkeyWindow quickHot, fullHot;
        private void RegisterExtraCaptureHotkeys()
        {
            string[] text={cfg.QuickCaptureHotkey,cfg.FullscreenCaptureHotkey}; HotkeyWindow[] windows={quickHot,fullHot};
            for(int i=0;i<2;i++)
            {
                uint mods,key; string display;
                if(!Config.TryParseHotkey(text[i],out mods,out key,out display) || !windows[i].Register(mods,key))
                    Balloon((i==0 ? "快速截图" : "全屏截图")+"快捷键 "+text[i]+" 未生效，请在快捷键页更换；按钮仍可使用。",ToolTipIcon.Warning);
            }
        }
        internal bool ApplyCaptureHotkeys(string adjustable,string quick,string full,out string error)
        {
            error=null; string[] values={adjustable,quick,full}, names={"框选截图","快速截图","全屏截图"};
            uint[] mods=new uint[3], keys=new uint[3], oldMods=new uint[3], oldKeys=new uint[3];
            string[] display=new string[3]; bool[] wasRegistered=new bool[3];
            HotkeyWindow[] windows={hot,quickHot,fullHot};
            for(int i=0;i<3;i++)
            {
                if(!Config.TryParseHotkey(values[i],out mods[i],out keys[i],out display[i])) { error=names[i]+"快捷键无效；普通键需带修饰键，PrintScreen 可单独使用。"; return false; }
                for(int j=0;j<i;j++) if(mods[i]==mods[j] && keys[i]==keys[j]) { error="三种截图不能使用相同的快捷键。"; return false; }
                wasRegistered[i]=windows[i].Registered; windows[i].Current(out oldMods[i],out oldKeys[i]);
            }
            foreach(HotkeyWindow window in windows) window.Unregister();
            for(int i=0;i<3;i++)
            {
                if(windows[i].Register(mods[i],keys[i])) continue;
                foreach(HotkeyWindow window in windows) window.Unregister();
                bool restored=true;
                for(int j=0;j<3;j++) if(wasRegistered[j] && !windows[j].Register(oldMods[j],oldKeys[j])) restored=false;
                hotkeyRegistered=hot.Registered;
                error=names[i]+"快捷键 "+display[i]+" 被占用。"+(restored ? "原快捷键已保留。" : "部分原快捷键恢复失败，请重新设置。"); return false;
            }
            cfg.Hotkey=display[0]; cfg.QuickCaptureHotkey=display[1]; cfg.FullscreenCaptureHotkey=display[2];
            hotkeyRegistered=true; hotkeyText=display[0]; miHotkey.Text="框选热键："+display[0];
            return true;
        }
    }
    internal partial class MainForm
    {
        private DshTextInput txtQuickCapture,txtFullscreenCapture;
        private DshToggle fullscreenEditor;
        internal bool RecordingCaptureHotkey { get { return (txtHotkey != null && txtHotkey.ContainsFocus) || (txtQuickCapture != null && txtQuickCapture.ContainsFocus) || (txtFullscreenCapture != null && txtFullscreenCapture.ContainsFocus); } }
        private void BuildCaptureHotkeyPage()
        {
            Panel page=NewPage(); PageHeader(page,"截图快捷键","快速截图、可调整框选和全屏截图，分别设置入口");
            txtHotkey=CaptureKeyCard(page,100,"框选截图","框选后拖动位置或边缘调整大小，确认后进入编辑器",cfg.Hotkey,CaptureMode.Adjustable);
            txtHotkey.InnerKeyDown+=OnHotkeyKeyDown;
            txtQuickCapture=CaptureKeyCard(page,245,"快速截图 · 直接保存","拖动框选，松开鼠标立即保存，按设置复制；不进编辑器",cfg.QuickCaptureHotkey,CaptureMode.Quick);
            txtFullscreenCapture=CaptureKeyCard(page,390,"全屏截图","截取全部显示器；默认进入编辑器，可在下方切换为直接保存",cfg.FullscreenCaptureHotkey,CaptureMode.Fullscreen);
            lblHotkeyState=new DshLabel("录制快捷键后点击底部「保存设置」。PrintScreen 被系统占用时可更换组合键。");
            fullscreenEditor=new DshToggle(UiText.T("全屏截图后进入编辑器（关闭则直接保存）"));
            fullscreenEditor.Checked=cfg.FullscreenOpenEditor; fullscreenEditor.SetBounds(32,536,588,30); page.Controls.Add(fullscreenEditor);
            lblHotkeyState.SetBounds(32,578,588,56); page.Controls.Add(lblHotkeyState);
            var reset=new DshButton("恢复三组默认"); reset.SetBounds(32,646,164,34); page.Controls.Add(reset);
            reset.Click+=delegate { pendingHotkey="Ctrl+Alt+A"; txtHotkey.Text=pendingHotkey; txtQuickCapture.Text="Ctrl+Alt+S"; txtFullscreenCapture.Text="PrintScreen"; lblHotkeyState.Text="已填入默认快捷键，请保存设置。"; };
        }
        private DshTextInput CaptureKeyCard(Panel page,int y,string title,string hint,string key,CaptureMode mode)
        {
            var card=NewCard(page,32,y,588,132);
            var label=new DshLabel(title); label.Medium=true; label.SetBounds(24,12,540,26); card.Controls.Add(label);
            var detail=new DshLabel(hint); detail.Small=true; detail.SetBounds(24,40,540,30); card.Controls.Add(detail);
            var input=new DshTextInput(); input.Text=key; input.ReadOnlyLook=true; input.Inner.ReadOnly=true; input.Inner.ShortcutsEnabled=false;
            input.SetBounds(24,78,350,36); card.Controls.Add(input);
            if(mode!=CaptureMode.Adjustable) input.InnerKeyDown+=delegate(object sender,KeyEventArgs e) {
                e.Handled=true; e.SuppressKeyPress=true; string value;
                if(Config.FormatHotkey(e.KeyData,out value)) { input.Text=value; lblHotkeyState.Text="已录制 "+title+"："+value+"，请保存设置。"; }
            };
            var capture=new DshButton(title,DshButton.Kind.Primary); capture.SetBounds(386,78,178,36); card.Controls.Add(capture);
            capture.Click+=delegate {
                bool visible=Visible;
                try { if(visible) { Hide(); Application.DoEvents(); } app.CaptureNow(mode); }
                finally { if(visible && !allowExit) { Show(); Activate(); } }
            };
            return input;
        }
    }
}
