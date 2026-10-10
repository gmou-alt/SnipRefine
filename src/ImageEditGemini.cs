using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
namespace SnapGrab {
 internal static class GeminiImageAdapter {
  internal static byte[] Body(string prompt,Bitmap image,Bitmap mask){
   using(Bitmap guide=new Bitmap(mask.Width,mask.Height)){
    byte[] pixels=ImageEditPatch.ReadPixels(mask);for(int i=0;i<pixels.Length;i+=4){byte value=pixels[i+3]==0 ? (byte)255 : (byte)0;pixels[i]=pixels[i+1]=pixels[i+2]=value;pixels[i+3]=255;}ImageEditPatch.WritePixels(guide,pixels);
    Func<Bitmap,object> part=delegate(Bitmap b){return new Dictionary<string,object>{{"inlineData",new Dictionary<string,object>{{"mimeType","image/png"},{"data",Convert.ToBase64String(ImageEditClient.Png(b))}}}};};
    var body=new Dictionary<string,object>{{"contents",new object[]{new Dictionary<string,object>{{"role","user"},{"parts",new object[]{new Dictionary<string,object>{{"text","First image: edit this image. Second image: permission map; white allows edits, black preserves content. Return only the edited first image with identical framing, square aspect ratio. "+prompt}},part(image),part(guide)}}}}},{"generationConfig",new Dictionary<string,object>{{"responseModalities",new[]{"TEXT","IMAGE"}},{"imageConfig",new Dictionary<string,object>{{"aspectRatio","1:1"}}}}}};
    return Encoding.UTF8.GetBytes(new JavaScriptSerializer {MaxJsonLength=48*1024*1024}.Serialize(body));
   }
  }
  internal static Bitmap Decode(string json){
   var root=new JavaScriptSerializer {MaxJsonLength=48*1024*1024}.DeserializeObject(json) as Dictionary<string,object>;object value;
   if(root==null||!root.TryGetValue("candidates",out value))throw new InvalidOperationException("Gemini did not return an image candidate.");
   var candidates=value as object[];if(candidates==null||candidates.Length!=1)throw new InvalidOperationException("Expected one Gemini image candidate.");
   var candidate=candidates[0] as Dictionary<string,object>;if(candidate==null||!candidate.TryGetValue("content",out value))throw new InvalidOperationException("Gemini response has no image content.");
   var content=value as Dictionary<string,object>;if(content==null||!content.TryGetValue("parts",out value))throw new InvalidOperationException("Gemini response has no image parts.");
   var parts=value as object[];string data=null;if(parts!=null)foreach(object item in parts){var p=item as Dictionary<string,object>;object thought;if(p==null||(p.TryGetValue("thought",out thought)&&object.Equals(thought,true)))continue;if(!p.TryGetValue("inlineData",out value)&&!p.TryGetValue("inline_data",out value))continue;var blob=value as Dictionary<string,object>;if(blob==null||!blob.TryGetValue("data",out value)||!(value is string))continue;object mime;if(!blob.TryGetValue("mimeType",out mime)&&!blob.TryGetValue("mime_type",out mime))throw new InvalidOperationException("Image MIME type missing.");if((string)mime!="image/png"&&(string)mime!="image/jpeg"&&(string)mime!="image/webp")throw new InvalidOperationException("Unsupported image MIME type.");if(data!=null)throw new InvalidOperationException("Multiple result images are not supported.");data=(string)value;}
   if(data==null||data.Length>32*1024*1024)throw new InvalidOperationException("Gemini must return one inline image within the size limit.");
   using(var stream=new MemoryStream(Convert.FromBase64String(data)))using(Bitmap source=new Bitmap(stream)){
    if(source.Width!=source.Height||source.Width<256||source.Width>4096)throw new InvalidOperationException("Returned image must be square, 256–4096 pixels. No changes applied.");
    Bitmap output=new Bitmap(1024,1024,PixelFormat.Format32bppArgb);using(Graphics g=Graphics.FromImage(output)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(source,new Rectangle(0,0,1024,1024));}return output;
   }
  }
 }
}
