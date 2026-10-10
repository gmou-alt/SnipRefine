using System;
namespace SnapGrab
{
    internal static class PrivacyGuard
    {
        internal static void CheckEndpoint(string address)
        {
            Uri uri;
            if(!Uri.TryCreate(address,UriKind.Absolute,out uri) ||
                (uri.Scheme!="https" && !(uri.Scheme=="http" && uri.IsLoopback)))
                throw new InvalidOperationException("远程 AI 接口必须使用 HTTPS；仅本机服务允许 HTTP。");
            if(uri.UserInfo.Length>0 || uri.Query.Length>0 || uri.Fragment.Length>0)
                throw new InvalidOperationException("密钥请单独填写，不能放入接口地址。");
        }
    }
}
