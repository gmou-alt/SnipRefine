using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
namespace SnapGrab
{
    internal static class UserUpgradeTests
    {
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        internal static void Guide()
        {
            Check(UserGuide.Search(null).Count == UserGuide.Topics.Length, "Empty search hid topics");
            Check(UserGuide.Search("超时").Count > 0, "Body search failed");
            Check(UserGuide.Search(" OCR ").Count > 0, "Search did not trim or ignore case");
            Check(UserGuide.Search("unmatched-function-xyz").Count == 0, "No-match search leaked entries");
            var titles = new HashSet<string>();
            foreach (HelpTopic topic in UserGuide.Topics)
                Check(titles.Add(topic.Title) && !string.IsNullOrWhiteSpace(topic.Body), "Missing or duplicate guide section");
            Check(new Config().AiReviewOperations, "New settings did not enable review");
        }
        internal static void DialogBackgrounds()
        {
            bool previous = Dsh.Canvas.R > 200;
            try
            {
                foreach (bool light in new bool[] { true, false })
                {
                    Dsh.ApplyTheme(light, null);
                    using (var help = new UserGuideForm())
                    using (var themed = new ThemedDialog())
                    using (var image = new System.Drawing.Bitmap(32,32))
                    using (var preview = new ImagePreviewForm(image))
                    {
                        Check(help.BackColor.A == 255 && themed.BackColor.A == 255 && preview.BackColor.A == 255, "Form background must be opaque");
                        help.Show(); System.Windows.Forms.Application.DoEvents();
                        Check(help.Visible, "Help did not open");
                        var search=(DshTextInput)typeof(UserGuideForm).GetField("search",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(help);
                        var content=(System.Windows.Forms.TextBox)typeof(UserGuideForm).GetField("content",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(help);
                        search.Text="快捷键"; System.Windows.Forms.Application.DoEvents();
                        Check(content.Text.Contains("Ctrl"), "Help search did not display a result");
                        help.Close(); preview.Show(); System.Windows.Forms.Application.DoEvents(); preview.Close();
                    }
                }
            }
            finally { Dsh.ApplyTheme(previous,null); }
        }
        private sealed class QueuedContext : SynchronizationContext
        {
            internal readonly Queue<Action> Queue = new Queue<Action>();
            public override void Post(SendOrPostCallback callback, object state) { Queue.Enqueue(delegate { callback(state); }); }
            internal void Drain() { while (Queue.Count > 0) Queue.Dequeue()(); }
        }
        internal static void Cancellation()
        {
            Type type = typeof(AiChat); BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            FieldInfo ui = type.GetField("ui", flags);
            object previous = ui.GetValue(null);
            var context = new QueuedContext(); ui.SetValue(null, context);
            try
            {
                AiChat.Clear(); int count = 0;
                Action<AiReply, string> callback = delegate(AiReply reply, string error) { count++; Check(error == "请求已取消", "Unexpected cancellation result"); };
                type.GetField("requestId", flags).SetValue(null, 100);
                type.GetField("busy", flags).SetValue(null, true);
                type.GetField("activeCancellation", flags).SetValue(null, new AiRequestCancellation());
                type.GetField("activeDone", flags).SetValue(null, callback);
                type.GetMethod("CompleteOnUi", flags).Invoke(null, new object[] { 100, callback, new AiReply { Say = "STALE_REPLY" }, null });
                AiChat.Cancel(); string cancelled = AiChat.Transcript;
                context.Drain();
                Check(count == 1 && !AiChat.Busy, "Cancel callback or busy state incorrect");
                Check(AiChat.Transcript == cancelled && !cancelled.Contains("STALE_REPLY"), "Queued stale reply applied after cancellation");
                AiChat.Cancel(); Check(count == 1, "Idle cancel called completion twice");
            }
            finally { AiChat.Clear(); ui.SetValue(null, previous); }
        }
    }
}
