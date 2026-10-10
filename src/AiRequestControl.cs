using System;
using System.Net;
using System.Threading.Tasks;
namespace SnapGrab
{
    internal sealed class AiRequestCancellation
    {
        private readonly object gate = new object();
        private readonly TaskCompletionSource<bool> signal = new TaskCompletionSource<bool>();
        private HttpWebRequest active;
        private bool cancelled;
        internal void Attach(HttpWebRequest request)
        {
            lock (gate) { if (cancelled) { request.Abort(); throw new OperationCanceledException("请求已取消"); } active = request; }
        }
        internal void Cancel()
        {
            HttpWebRequest request;
            lock (gate) { cancelled=true; signal.TrySetResult(true); request=active; active=null; }
            if (request!=null) { try { request.Abort(); } catch (WebException) { } }
        }
        internal void ThrowIfCancelled() { lock (gate) { if (cancelled) throw new OperationCanceledException("请求已取消"); } }
        internal T Await<T>(Task<T> operation,int timeout)
        {
            int winner=Task.WaitAny(new Task[] { operation,signal.Task },timeout);
            bool isCancelled; lock (gate) isCancelled=cancelled;
            if (winner!=0 || isCancelled)
            {
                operation.ContinueWith(delegate(Task<T> completed) {
                    if (completed.IsFaulted) { AggregateException observed=completed.Exception; GC.KeepAlive(observed); }
                    else if (completed.Status==TaskStatus.RanToCompletion) { IDisposable value=completed.Result as IDisposable; if (value!=null) value.Dispose(); }
                },TaskScheduler.Default);
                if (winner==-1) { Cancel(); throw new WebException("请求超时",WebExceptionStatus.Timeout); }
                throw new OperationCanceledException("请求已取消");
            }
            return operation.GetAwaiter().GetResult();
        }
        internal void Complete() { lock (gate) active=null; }
    }
}
