using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace SnapGrab
{
    internal static class AiCancellationChecks
    {
        private static int passed;
        private static void Check(bool value,string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: "+name); }
        public static int Main()
        {
            AiRequestCancellation early=new AiRequestCancellation(); early.Cancel();
            bool rejected=false;
            try { early.Attach((HttpWebRequest)WebRequest.Create("http://127.0.0.1:1/")); } catch (OperationCanceledException) { rejected=true; }
            Check(rejected,"cancel before attach rejects request");
            TcpListener listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
            int port=((IPEndPoint)listener.LocalEndpoint).Port;
            AiRequestCancellation active=new AiRequestCancellation(); HttpWebRequest request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+port+"/"); request.Proxy=null; active.Attach(request);
            bool aborted=false;
            Thread requester=new Thread(delegate() {
                try { using (WebResponse response=active.Await(request.GetResponseAsync(),5000)) {} }
                catch (OperationCanceledException) { aborted=true; }
                catch (WebException ex) { aborted=ex.Status==WebExceptionStatus.RequestCanceled; }
            }); requester.Start();
            using (TcpClient peer=listener.AcceptTcpClient()) { active.Cancel(); Check(requester.Join(5000),"abort promptly ends active request"); }
            listener.Stop();
            Check(aborted,"cancel aborts active local HTTP request");
            active.Cancel(); active.Complete(); Check(true,"repeated cancel and completion are safe");
            AiRequestCancellation complete=new AiRequestCancellation(); complete.Attach((HttpWebRequest)WebRequest.Create("http://127.0.0.1:1/")); complete.Complete(); complete.Cancel();
            rejected=false; try { complete.Attach((HttpWebRequest)WebRequest.Create("http://127.0.0.1:1/")); } catch (OperationCanceledException) { rejected=true; }
            Check(rejected,"completed cancelled controller cannot reattach");
            for (int i=0;i<100;i++)
            {
                AiRequestCancellation race=new AiRequestCancellation();
                Thread thread=new Thread(delegate() { race.Cancel(); }); thread.Start();
                try { race.Attach((HttpWebRequest)WebRequest.Create("http://127.0.0.1:1/")); } catch (OperationCanceledException) { }
                thread.Join(); race.Complete();
            }
            Check(true,"100 attach/cancel races complete safely");
            AiRequestCancellation timeoutControl=new AiRequestCancellation(); bool timedOut=false;
            try { timeoutControl.Await(new TaskCompletionSource<bool>().Task,20); } catch (WebException ex) { timedOut=ex.Status==WebExceptionStatus.Timeout; }
            Check(timedOut,"bounded wait reports timeout");
            TaskCompletionSource<bool> failed=new TaskCompletionSource<bool>(); failed.SetException(new InvalidOperationException("expected")); bool failurePassed=false;
            try { new AiRequestCancellation().Await(failed.Task,100); } catch (InvalidOperationException) { failurePassed=true; }
            Check(failurePassed,"request failures retain original exception");
            Console.WriteLine("PASS: "+passed+" cancellation checks"); return 0;
        }
    }
}
