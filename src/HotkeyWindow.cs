using System;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class HotkeyWindow : NativeWindow
    {
        private bool registered;

        private uint curMods;

        private uint curVk;

        public bool Registered
        {
            get
            {
                return registered;
            }
        }

        public event EventHandler Pressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        public bool Register(uint mods, uint vk)
        {
            bool flag = registered;
            uint num = curMods;
            uint vk2 = curVk;
            if (flag)
            {
                Native.UnregisterHotKey(base.Handle, 1);
            }
            registered = false;
            if (Native.RegisterHotKey(base.Handle, 1, mods | 0x4000, vk))
            {
                registered = true;
                curMods = mods;
                curVk = vk;
                return true;
            }
            if (flag)
            {
                if (Native.RegisterHotKey(base.Handle, 1, num | 0x4000, vk2))
                {
                    registered = true;
                    curMods = num;
                    curVk = vk2;
                    Diag.Log("新热键注册失败，已回滚到原热键");
                }
                else
                {
                    Diag.Log("新热键注册失败，且原热键也无法恢复");
                }
            }
            return false;
        }

        public void Current(out uint mods, out uint vk)
        {
            mods = curMods;
            vk = curVk;
        }

        public void Unregister()
        {
            if (registered)
            {
                Native.UnregisterHotKey(base.Handle, 1);
                registered = false;
                curMods = 0u;
                curVk = 0u;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 786)
            {
                try
                {
                    EventHandler pressed = this.Pressed;
                    if (pressed != null)
                    {
                        pressed(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Diag.Log("热键处理异常: " + ex.Message);
                }
            }
            base.WndProc(ref m);
        }

        public void Shutdown()
        {
            Unregister();
            try
            {
                DestroyHandle();
            }
            catch
            {
            }
        }
    }
}
