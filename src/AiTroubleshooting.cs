using System;
namespace SnapGrab
{
    internal static class AiTroubleshooting
    {
        internal static string Explain(string error)
        {
            string value=error ?? "未知错误", lower=value.ToLowerInvariant();
            string hint;
            if(lower.Contains("401") || lower.Contains("403")) hint="检查 API 密钥、账号权限和接口是否属于同一服务。";
            else if(lower.Contains("404")) hint="检查接口地址和模型 ID；文本测试成功不代表支持看图。";
            else if(lower.Contains("429")) hint="检查额度或限流，稍后重试；避免连续重复发送。";
            else if(lower.Contains("timeout") || lower.Contains("超时")) hint="检查网络与代理，或在 AI 请求页增加超时时间。";
            else hint="检查网络、接口协议与模型能力；可先在设置中测试连接和看图。";
            return value+"\r\n"+hint;
        }
    }
}
