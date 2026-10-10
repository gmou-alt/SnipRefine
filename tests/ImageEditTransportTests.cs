using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;
namespace SnapGrab {
 internal static class ImageEditTransportTests {
  internal static void Run(){
   using(Bitmap image=new Bitmap(1024,1024))using(Bitmap mask=new Bitmap(1024,1024)) {
    string json=new JavaScriptSerializer {MaxJsonLength=48*1024*1024}.Serialize(new Dictionary<string,object>{{"data",new object[]{new Dictionary<string,object>{{"b64_json",Convert.ToBase64String(ImageEditClient.Png(image))}}}}});
    string geminiJson=new JavaScriptSerializer {MaxJsonLength=48*1024*1024}.Serialize(new Dictionary<string,object>{{"candidates",new object[]{new Dictionary<string,object>{{"content",new Dictionary<string,object>{{"parts",new object[]{new Dictionary<string,object>{{"inlineData",new Dictionary<string,object>{{"mimeType","image/png"},{"data",Convert.ToBase64String(ImageEditClient.Png(image))}}}}}}}}}}}});
    foreach(string unsafeResponse in new[]{"{\"candidates\":[{\"content\":{\"parts\":[{\"fileData\":{\"fileUri\":\"https://example.com/private.png\"}}]}}]}","{\"error\":{\"message\":\"unit-image-secret\"}}"}){
     bool rejected=false;try{using(Bitmap output=GeminiImageAdapter.Decode(unsafeResponse)){} }catch(InvalidOperationException error){if(error.Message.Contains("unit-image-secret")||error.Message.Contains("private.png"))throw;rejected=true;}if(!rejected)throw new Exception("Unsafe Gemini response accepted");
    }
    foreach(string protocol in new[]{"openai","gemini"})foreach(bool failed in new bool[]{false,true}) {
     TcpListener listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;
     Task server=Task.Run(delegate {
      using(TcpClient client=listener.AcceptTcpClient())using(var stream=client.GetStream()) {
       stream.ReadTimeout=10000;StringBuilder header=new StringBuilder();int b;
       while((b=stream.ReadByte())>=0) {header.Append((char)b);if(header.ToString().EndsWith("\r\n\r\n"))break;if(header.Length>16384)throw new Exception("Oversized header");}
       string text=header.ToString();int size=0;
       foreach(string line in text.Split(new string[]{"\r\n"},StringSplitOptions.None))if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))size=int.Parse(line.Substring(15).Trim());
       byte[] body=new byte[size];int offset=0;while(offset<size){int read=stream.Read(body,offset,size-offset);if(read==0)throw new Exception("Truncated multipart");offset+=read;}
       string payload=Encoding.UTF8.GetString(body);
       if(protocol=="openai") {if(!text.Contains("Bearer unit-image-secret")||!payload.Contains("filename=\"mask.png\"")||!payload.Contains("image-model"))throw new Exception("Adapter request missing auth, mask or model");}
       else {if(!text.ToLowerInvariant().Contains("x-goog-api-key: unit-image-secret")||text.Contains("Bearer")||!payload.Contains("inlineData")||!payload.Contains("permission map")||!payload.Contains("responseModalities"))throw new Exception("Gemini request format or authentication invalid");}
       string response=failed ? "unit-image-secret private service response" : protocol=="gemini" ? geminiJson : json;byte[] bytes=Encoding.UTF8.GetBytes(response);
       byte[] headers=Encoding.ASCII.GetBytes("HTTP/1.1 "+(failed ? "401 Unauthorized" : "200 OK")+"\r\nContent-Type: application/json\r\nContent-Length: "+bytes.Length+"\r\nConnection: close\r\n\r\n");stream.Write(headers,0,headers.Length);stream.Write(bytes,0,bytes.Length);
      }
     });
     try {
      bool rejected=false;
      try {using(Bitmap output=ImageEditClient.Edit("http://127.0.0.1:"+port+"/v1/images/edits","unit-image-secret","image-model","remove text",image,mask,10000,false,new AiRequestCancellation(),protocol)) {if(output.Size!=image.Size)throw new Exception("Adapter response image");}}
      catch(InvalidOperationException ex){if(!failed||ex.Message.Contains("unit-image-secret")||ex.Message.Contains("private service"))throw;rejected=true;}
      if(rejected!=failed)throw new Exception("Service status mismatch");if(!server.Wait(10000))throw new Exception("Local image service timeout");server.GetAwaiter().GetResult();
     } finally {listener.Stop();}
    }
   }
  }
 }
}
