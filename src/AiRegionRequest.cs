using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
namespace SnapGrab {
 internal static class AiRegionRequest {
  internal static string Encode(Bitmap image,Rectangle region,int maxEdge,out Size encodedSize) {
   if(region.Width<2 || region.Height<2 || !new Rectangle(Point.Empty,image.Size).Contains(region)) throw new ArgumentException("Invalid AI region");
   double scale=maxEdge>0 ? Math.Min(1.0,(double)maxEdge/Math.Max(region.Width,region.Height)) : 1.0;
   encodedSize=new Size(Math.Max(1,(int)Math.Round(region.Width*scale)),Math.Max(1,(int)Math.Round(region.Height*scale)));
   using(Bitmap crop=image.Clone(region,PixelFormat.Format32bppArgb))
   using(Bitmap output=new Bitmap(encodedSize.Width,encodedSize.Height))
   using(Graphics g=Graphics.FromImage(output))
   using(MemoryStream stream=new MemoryStream()) {
    g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
    g.DrawImage(crop,new Rectangle(Point.Empty,encodedSize));output.Save(stream,ImageFormat.Png);
    return "data:image/png;base64,"+Convert.ToBase64String(stream.ToArray());
   }
  }
  internal static void MapAndValidate(AiReply reply,Rectangle region,Size encoded) {
   if(reply==null || reply.Ops==null) throw new InvalidOperationException("Empty AI reply");
   if(reply.Ops.Count>64 || encoded.Width<1 || encoded.Height<1) throw new InvalidOperationException("Invalid AI operation count or image size");
   // Validate every operation before modifying any of them.
   foreach(AiOp op in reply.Ops) {
    if(op==null) throw new InvalidOperationException("Empty operation");
    foreach(float n in new float[]{op.X,op.Y,op.W,op.H,op.X2,op.Y2,op.Width}) if(float.IsNaN(n)||float.IsInfinity(n)) throw new InvalidOperationException("Invalid AI coordinate");
    string k=(op.Kind ?? "").Trim().ToLowerInvariant();
    if(k=="crop") throw new InvalidOperationException("局部编辑不能裁剪整图；请清除 AI 选区后单独裁剪。");
    if(k!="ocr" && k!="rect" && k!="ellipse" && k!="line" && k!="arrow" && k!="text" && k!="mosaic" && k!="redact" && k!="blur") throw new InvalidOperationException("Unsupported regional operation");
    if(op.X<0 || op.Y<0 || op.X>=encoded.Width || op.Y>=encoded.Height || op.W<0 || op.H<0 || op.X+op.W>encoded.Width || op.Y+op.H>encoded.Height) throw new InvalidOperationException("AI 坐标超出框选范围，请缩小任务并重试。");
    if((k=="line"||k=="arrow") && (op.X2<0 || op.Y2<0 || op.X2>encoded.Width || op.Y2>encoded.Height)) throw new InvalidOperationException("AI 线段超出框选范围。");
   }
   float sx=(float)region.Width/encoded.Width,sy=(float)region.Height/encoded.Height;
   foreach(AiOp op in reply.Ops) {
    op.Kind=(op.Kind ?? "").Trim().ToLowerInvariant();
    op.X=region.X+op.X*sx;op.Y=region.Y+op.Y*sy;op.W*=sx;op.H*=sy;
    op.X2=region.X+op.X2*sx;op.Y2=region.Y+op.Y2*sy;
    if(op.Width>0) op.Width*=Math.Min(sx,sy);
   }
  }
 }
}
