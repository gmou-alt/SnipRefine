using System;
using System.IO;
namespace SnapGrab
{
    // Diagnostics must not persist paths, service errors or user content.
    internal static class Diag
    {
        public static bool Enabled;
        public static void Reset()
        {
            try { foreach(string name in new string[] { "timing.log", "SnapGrab.ini.tmp" }) { string path=Path.Combine(Config.ConfigDir,name); if(File.Exists(path)) File.Delete(path); } }
            catch(IOException) { } catch(UnauthorizedAccessException) { }
        }
        public static void Log(string message) { }
    }
}
