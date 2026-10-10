using System;
using System.Diagnostics;
using System.Windows.Forms;
namespace SnapGrab
{
    internal partial class EditorForm
    {
        private Timer aiProgressTimer;
        private readonly Stopwatch aiElapsed = new Stopwatch();
        private void StartAiProgress(string model)
        {
            if (aiProgressTimer == null)
            {
                aiProgressTimer = new Timer { Interval = 1000 };
                Disposed += delegate { aiProgressTimer.Dispose(); };
            }
            aiProgressTimer.Stop(); aiProgressTimer.Tag = model;
            aiProgressTimer.Tick -= AiProgressTick; aiProgressTimer.Tick += AiProgressTick;
            aiElapsed.Restart(); AiProgressTick(this, EventArgs.Empty); aiProgressTimer.Start();
        }
        private void AiProgressTick(object sender, EventArgs e)
        {
            if (!aiBusy || IsDisposed) { StopAiProgress(); return; }
            AiSetStatus("等待 " + aiProgressTimer.Tag + " · " + (int)aiElapsed.Elapsed.TotalSeconds + " 秒 · 可取消");
        }
        private void StopAiProgress() { aiElapsed.Stop(); if (aiProgressTimer != null) aiProgressTimer.Stop(); }
    }
}
