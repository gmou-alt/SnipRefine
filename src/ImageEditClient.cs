using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
namespace SnapGrab {
 internal static class ImageEditClient {
  internal static byte[] Png(Bitmap image) {using(MemoryStream s=new MemoryStream()){image.Save(s,ImageFormat.Png);return s.ToArray();}}
  internal static byte[] Multipart(string boundary,string model,string prompt,Bitmap image,Bitmap mask) {
   using(MemoryStream s=new MemoryStream()) {
    Action<string> write=delegate(string t){byte[] b=Encoding.UTF8.GetBytes(t);s.Write(b,0,b.Length);};
    foreach(var field in new Dictionary<string,string>{{"model",model},{"prompt",prompt},{"n","1"},{"size","1024x1024"},{"output_format","png"}}) write("--"+boundary+"\r\nContent-Disposition: form-data; name=\""+field.Key+"\"\r\n\r\n"+field.Value+"\r\n");
    foreach(var file in new Dictionary<string,Bitmap>{{"image",image},{"mask",mask}}) {
     write("--"+boundary+"\r\nContent-Disposition: form-data; name=\""+file.Key+"\"; filename=\""+file.Key+".png\"\r\nContent-Type: image/png\r\n\r\n");byte[] b=Png(file.Value);s.Write(b,0,b.Length);write("\r\n");
    }
    write("--"+boundary+"--\r\n");return s.ToArray();
   }
  }
  internal static Bitmap Decode(string json) {
   JavaScriptSerializer parser=new JavaScriptSerializer {MaxJsonLength=48*1024*1024};
   var root=parser.DeserializeObject(json) as Dictionary<string,object>;object value;
   if(root==null || !root.TryGetValue("data",out value)) throw new InvalidOperationException("图片编辑响应缺少 data");
   object[] data=value as object[];if(data==null||data.Length!=1) throw new InvalidOperationException("图片编辑响应应包含一张图片");
   var item=data[0] as Dictionary<string,object>;
   if(item==null || !item.TryGetValue("b64_json",out value) || !(value is string)) throw new InvalidOperationException("需要返回 b64_json 图片；当前不下载服务返回的外部 URL");
   string encoded=(string)value;if(encoded.Length>32*1024*1024) throw new InvalidOperationException("返回图片过大");
   using(MemoryStream stream=new MemoryStream(Convert.FromBase64String(encoded)))
   using(Bitmap image=new Bitmap(stream)) {if(image.Width!=1024||image.Height!=1024) throw new InvalidOperationException("返回图片尺寸不匹配，未应用");return image.Clone(new Rectangle(0,0,1024,1024),PixelFormat.Format32bppArgb);}
  }
  internal static Bitmap Edit(string endpoint,string key,string model,string prompt,Bitmap image,Bitmap mask,int timeout,bool proxy,AiRequestCancellation cancel,string protocol="openai") {
   if(protocol!="openai"&&protocol!="gemini")throw new InvalidOperationException("Unsupported image editing protocol.");
   PrivacyGuard.CheckEndpoint(endpoint);AiClient.ConfigureTls();
   string boundary="SnipRefine"+Guid.NewGuid().ToString("N");bool gemini=protocol=="gemini";byte[] body=gemini ? GeminiImageAdapter.Body(prompt,image,mask) : Multipart(boundary,model,prompt,image,mask);
   HttpWebRequest request=(HttpWebRequest)WebRequest.Create(endpoint);request.Method="POST";request.ServicePoint.Expect100Continue=false;request.AllowAutoRedirect=false;request.ContentType=gemini ? "application/json" : "multipart/form-data; boundary="+boundary;request.Accept="application/json";request.UserAgent="SnipRefine";request.Timeout=timeout;request.ReadWriteTimeout=timeout;request.ContentLength=body.Length;if(!proxy)request.Proxy=null;
   if(!string.IsNullOrEmpty(key))request.Headers[gemini ? "x-goog-api-key" : "Authorization"]=gemini ? key : "Bearer "+key;
   cancel.Attach(request);
   try {
    cancel.ThrowIfCancelled();using(Stream s=cancel.Await(request.GetRequestStreamAsync(),timeout)) {s.Write(body,0,body.Length);}
    using(HttpWebResponse response=(HttpWebResponse)cancel.Await(request.GetResponseAsync(),timeout))
    using(Stream s=response.GetResponseStream())
    using(MemoryStream output=new MemoryStream()) {
     byte[] buffer=new byte[8192];int read;
     while((read=s.Read(buffer,0,buffer.Length))>0) {cancel.ThrowIfCancelled();if(output.Length+read>48*1024*1024)throw new InvalidOperationException("图片响应过大");output.Write(buffer,0,read);}
     cancel.ThrowIfCancelled();return gemini ? GeminiImageAdapter.Decode(Encoding.UTF8.GetString(output.ToArray())) : Decode(Encoding.UTF8.GetString(output.ToArray()));
    }
   } catch(WebException ex) {var failed=ex.Response as HttpWebResponse;string message=failed==null ? "图片编辑请求失败或超时，请检查网络与配置" : "图片编辑服务返回 HTTP "+(int)failed.StatusCode+"，请检查密钥、模型权限及额度";if(ex.Response!=null)ex.Response.Close();throw new InvalidOperationException(message);}
   finally {cancel.Complete();}
  }
 }
}
