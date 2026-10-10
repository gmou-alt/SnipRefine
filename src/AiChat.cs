using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;

namespace SnapGrab
{
    internal static class AiChat
    {
        internal sealed class Message
        {
            internal readonly string Role, Text;
            internal Message(string role, string text) { Role = role; Text = text; }
        }
        private static readonly List<Message> messages = new List<Message>();
        internal static Message[] Messages { get { lock (history) return messages.ToArray(); } }
        private static readonly List<string> history = new List<string>();

        private static SynchronizationContext ui;

        private static bool busy;
        private static int requestId;
        private static AiRequestCancellation activeCancellation;
        private static Action<AiReply, string> activeDone;

        public static bool Busy
        {
            get
            {
                return busy;
            }
        }

        public static int Count
        {
            get
            {
                lock (history)
                {
                    return history.Count;
                }
            }
        }

        public static string Transcript
        {
            get
            {
                lock (history)
                {
                    return string.Join("\r\n\r\n", history.ToArray());
                }
            }
        }

        public static event EventHandler Changed;

        public static void Init()
        {
            if (ui == null)
            {
                ui = SynchronizationContext.Current;
            }
        }

        public static void Clear()
        {
            lock (history)
            {
                history.Clear(); messages.Clear();
            }
            Notify();
        }

        public static void Append(string who, string text)
        {
            lock (history)
            {
                history.Add(who + "：" + text); messages.Add(new Message(who, text));
            }
        }

        private static void Notify()
        {
            EventHandler h = AiChat.Changed;
            if (h == null)
            {
                return;
            }
            if (ui != null)
            {
                ui.Post(delegate
                {
                    try
                    {
                        h(null, EventArgs.Empty);
                    }
                    catch
                    {
                    }
                }, null);
                return;
            }
            try
            {
                h(null, EventArgs.Empty);
            }
            catch
            {
            }
        }

        public static void Send(TrayApp app, Config cfg, string text, Image image, Action<AiReply, string> done)
        {
            Send(app, cfg, text, image, false, done);
        }

        public static void Cancel()
        {
            if (!busy) return;
            ++requestId;
            busy = false;
            if (activeCancellation != null) activeCancellation.Cancel();
            activeCancellation = null;
            Action<AiReply, string> callback = activeDone; activeDone = null;
            Append("提示", "请求已取消，迟到回复不会加入对话。"); Notify();
            if (callback != null) callback(null, "请求已取消");
        }

        private static void CompleteOnUi(int id, Action<AiReply, string> done, AiReply reply, string error)
        {
            Action complete = delegate
            {
                if (id != requestId) return;
                busy = false; activeCancellation = null; activeDone = null;
                if (error == null) Append("AI", reply == null ? "" : reply.Say);
                else Append("错误", error);
                Notify();
                if (done != null) done(reply, error);
            };
            if (ui != null) ui.Post(delegate { complete(); }, null); else complete();
        }

        public static void Send(TrayApp app, Config cfg, string text, Image image, bool annotation, Action<AiReply, string> done)
        {
            if (busy || string.IsNullOrEmpty(text)) return;
            AiProvider prov = cfg.ActiveProvider();
            if (prov == null || string.IsNullOrEmpty(prov.Endpoint) || prov.Models.Count == 0)
            { if (done != null) done(null, "请先在设置中配置提供商、接口和模型。"); return; }
            prov = prov.Snapshot();
            Bitmap shot;
            try { shot = image == null ? null : new Bitmap(image); }
            catch (Exception ex) { if (done != null) done(null, "无法准备图片：" + ex.Message); return; }
            int timeout = cfg.AiTimeoutSeconds * 1000, tokens = cfg.AiMaxOutputTokens, maxEdge = cfg.AiImageMaxEdge;
            bool proxy = cfg.AiUseSystemProxy, json = cfg.AiJsonMode;
            double temperature = cfg.AiTemperature / 10.0;
            string system = AiClient.SystemPrompt(annotation);
            int id = ++requestId;
            var cancellation = new AiRequestCancellation();
            activeCancellation = cancellation; activeDone = done; busy = true;
            Append("你", text); Notify();
            Thread worker = new Thread((ThreadStart)delegate
            {
                AiReply reply = null; string error = null;
                try
                {
                    AiClient.RequestCancellation = cancellation;
                    AiClient.UseSystemProxy = proxy; AiClient.OutputTokens = tokens;
                    string encoded = null; float scale = 1;
                    using (shot) if (shot != null) encoded = AiClient.EncodeImage(shot, maxEdge, out scale);
                    cancellation.ThrowIfCancelled();
                    string response = AiClient.CallProvider(prov, system, text, encoded, temperature, json && annotation, timeout);
                    string content = AiClient.ExtractContentFor(prov, response, out error);
                    if (error == null)
                    {
                        reply = AiClient.ParseReply(content);
                        if (scale > 0 && Math.Abs(scale - 1) > 0.0001f)
                            foreach (AiOp op in reply.Ops)
                            { op.X /= scale; op.Y /= scale; op.W /= scale; op.H /= scale; op.X2 /= scale; op.Y2 /= scale; }
                        if (string.IsNullOrEmpty(reply.Say)) reply.Say = content ?? "";
                    }
                }
                catch (Exception ex) { error = ex.Message; }
                finally
                {
                    cancellation.Complete(); AiClient.RequestCancellation = null;
                    AiClient.UseSystemProxy = false; AiClient.OutputTokens = 0;
                }
                CompleteOnUi(id, done, reply, error);
            });
            worker.IsBackground = true;
            try { worker.Start(); }
            catch (Exception ex) { if (shot != null) shot.Dispose(); cancellation.Complete(); CompleteOnUi(id, done, null, ex.Message); }
        }

        private static void Finish(Action<AiReply, string> done, string err, string note)
        {
            if (done == null)
            {
                return;
            }
            if (ui != null)
            {
                ui.Post(delegate
                {
                    try
                    {
                        done(null, note);
                    }
                    catch
                    {
                    }
                }, null);
                return;
            }
            try
            {
                done(null, note);
            }
            catch
            {
            }
        }

        private static void Finish(Action<AiReply, string> done, string err, AiReply reply)
        {
            if (done == null)
            {
                return;
            }
            if (ui != null)
            {
                ui.Post(delegate
                {
                    try
                    {
                        done(reply, err);
                    }
                    catch
                    {
                    }
                }, null);
                return;
            }
            try
            {
                done(reply, err);
            }
            catch
            {
            }
        }
    }
}
