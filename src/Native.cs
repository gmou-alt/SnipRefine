using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SnapGrab
{
    internal static class Native
    {
        public const int WM_HOTKEY = 786;

        public const uint MOD_ALT = 1u;

        public const uint MOD_CONTROL = 2u;

        public const uint MOD_SHIFT = 4u;

        public const uint MOD_WIN = 8u;

        public const uint MOD_NOREPEAT = 16384u;

        private const int THREAD_PRIORITY_ABOVE_NORMAL = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr handle);

        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

        public static void BoostUiThread()
        {
            try
            {
                SetThreadPriority(GetCurrentThread(), 1);
            }
            catch
            {
            }
        }

        public static void TrimWorkingSet()
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    EmptyWorkingSet(process.Handle);
                }
            }
            catch
            {
            }
        }

        public static void TrySetDpiAware()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(new IntPtr(-4)))
                {
                    return;
                }
            }
            catch
            {
            }
            try
            {
                if(SetProcessDpiAwareness(2)==0) return;
            }
            catch { }
            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }
    }
}
