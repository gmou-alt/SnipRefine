using System;
using System.Collections.Generic;

namespace SnapGrab
{
    public class AiProvider
    {
        public string Id = "";

        public string Name = "";

        public string Endpoint = "";

        public string Protocol = "openai";

        public string ApiKey = "";

        public string SelectedModel = "";

        public List<string> Models = new List<string>();

        public bool IsAnthropic
        {
            get
            {
                return string.Equals(Protocol, "anthropic", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string FirstModel
        {
            get
            {
                if (Models.Contains(SelectedModel)) return SelectedModel;
                if (Models.Count <= 0)
                {
                    return "";
                }
                return Models[0];
            }
        }

        public string Serialize()
        {
            return Esc(Id) + "|" + Esc(Name) + "|" + Esc(Endpoint) + "|" + Esc(Protocol) + "|" + Esc(AiClient.ProtectKey(ApiKey)) + "|" + Esc(string.Join(",", Models.ToArray())) + "|" + Esc(FirstModel);
        }

        public static AiProvider Deserialize(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }
            string[] array = line.Split(new char[] { '|' });
            if (array.Length < 2)
            {
                return null;
            }
            AiProvider aiProvider = new AiProvider();
            aiProvider.Id = ((array.Length > 0) ? Unesc(array[0]) : "");
            aiProvider.Name = ((array.Length > 1) ? Unesc(array[1]) : "");
            aiProvider.Endpoint = ((array.Length > 2) ? Unesc(array[2]) : "");
            aiProvider.Protocol = ((array.Length > 3) ? Unesc(array[3]) : "openai");
            aiProvider.ApiKey = ((array.Length > 4) ? AiClient.UnprotectKey(Unesc(array[4])) : "");
            string text = ((array.Length > 5) ? Unesc(array[5]) : "");
            if (text.Length > 0)
            {
                string[] array2 = text.Split(new char[] { ',' });
                foreach (string text2 in array2)
                {
                    string text3 = text2.Trim();
                    if (text3.Length > 0)
                    {
                        aiProvider.Models.Add(text3);
                    }
                }
            }
            aiProvider.SelectedModel = array.Length > 6 ? Unesc(array[6]) : "";
            if (string.IsNullOrEmpty(aiProvider.Id))
            {
                return null;
            }
            return aiProvider;
        }

        public AiProvider Snapshot()
        {
            AiProvider copy = (AiProvider)MemberwiseClone();
            copy.Models = new List<string>(Models);
            return copy;
        }

        public static string NormalizeEndpoint(string endpoint, bool anthropic)
        {
            string value = (endpoint ?? "").Trim().TrimEnd(new char[] { '/' });
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https")) return value;
            if (uri.Query.Length > 0 || uri.Fragment.Length > 0) return value;
            string path = uri.AbsolutePath.TrimEnd(new char[] { '/' });
            if (path == "" || path == "/v1")
                return value + (path == "" ? "/v1" : "") + (anthropic ? "/messages" : "/chat/completions");
            return value;
        }

        public string ValidationError(bool requireModel = true)
        {
            Uri uri;
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https")) return "API 地址必须是完整的 http:// 或 https:// 地址";
            if (uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
                return "请填写 API 请求地址，密钥单独填写，不要放在地址内";
            if (uri.Scheme == "http" && !uri.IsLoopback) return "远程 AI 服务必须使用 HTTPS，避免密钥与截图明文传输";
            if (Protocol != "openai" && Protocol != "anthropic") return "请选择支持的 API 协议";
            if (requireModel && (Models.Count == 0 || string.IsNullOrWhiteSpace(FirstModel))) return "请填写至少一个模型并选择默认模型";
            if (string.IsNullOrWhiteSpace(ApiKey) && !uri.IsLoopback) return "请填写 API 密钥（本机服务可留空）";
            return null;
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }
            return s.Replace("|", " ").Replace("\r", " ").Replace("\n", " ");
        }

        private static string Unesc(string s)
        {
            if (s != null)
            {
                return s;
            }
            return "";
        }
    }
}
