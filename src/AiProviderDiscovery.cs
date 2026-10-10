using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
namespace SnapGrab
{
    internal static class AiProviderDiscovery
    {
        internal static string ModelsUrl(AiProvider provider)
        {
            string endpoint=AiProvider.NormalizeEndpoint(provider.Endpoint,provider.IsAnthropic);
            string suffix=provider.IsAnthropic ? "/messages" : "/chat/completions";
            if (!endpoint.EndsWith(suffix,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("自定义接口无法推导模型列表地址，请手动填写模型。");
            return endpoint.Substring(0,endpoint.Length-suffix.Length)+"/models";
        }
        internal static List<string> ParseModels(string json)
        {
            JavaScriptSerializer serializer=new JavaScriptSerializer { MaxJsonLength=4*1024*1024 };
            Dictionary<string,object> root=serializer.DeserializeObject(json) as Dictionary<string,object>;
            object data; object[] entries; if (root==null || !root.TryGetValue("data",out data) || (entries=AiClient.AsArray(data)) == null) throw new InvalidOperationException("模型列表返回格式不支持，请手动填写模型。");
            List<string> models=new List<string>();
            foreach (object entry in entries)
            {
                Dictionary<string,object> model=entry as Dictionary<string,object>; object id;
                if (model!=null && model.TryGetValue("id",out id) && id is string && !string.IsNullOrWhiteSpace((string)id) && !models.Contains((string)id)) models.Add((string)id);
                if (models.Count>=500) break;
            }
            if (models.Count==0) throw new InvalidOperationException("未返回可用模型，请手动填写。");
            return models;
        }
        internal static List<string> Fetch(AiProvider provider,int timeout,bool useProxy)
        {
            AiClient.ConfigureTls();
            string modelUrl=ModelsUrl(provider); PrivacyGuard.CheckEndpoint(modelUrl);
            HttpWebRequest request=(HttpWebRequest)WebRequest.Create(modelUrl); request.AllowAutoRedirect=false;
            request.Method="GET"; request.Accept="application/json"; request.Timeout=timeout; request.ReadWriteTimeout=timeout;
            if (!useProxy) request.Proxy=null;
            if (provider.IsAnthropic) { request.Headers["x-api-key"]=provider.ApiKey; request.Headers["anthropic-version"]="2023-06-01"; }
            else if (!string.IsNullOrWhiteSpace(provider.ApiKey)) request.Headers["Authorization"]="Bearer "+provider.ApiKey;
            try
            {
                using (WebResponse response=request.GetResponse())
                using (StreamReader reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8))
                { StringBuilder body=new StringBuilder(); char[] buffer=new char[4096]; int count;
                    while ((count=reader.Read(buffer,0,buffer.Length))>0)
                    { if (body.Length+count>4*1024*1024) throw new InvalidOperationException("模型列表过大，请手动填写。"); body.Append(buffer,0,count); }
                    return ParseModels(body.ToString()); }
            }
            catch (WebException ex)
            {
                using (HttpWebResponse response=ex.Response as HttpWebResponse)
                { throw new InvalidOperationException(response==null ? "无法获取模型列表，请检查网络或手动填写。" : "模型列表接口返回 HTTP "+(int)response.StatusCode+"，可手动填写模型。"); }
            }
        }
    }
}
