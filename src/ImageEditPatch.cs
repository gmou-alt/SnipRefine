using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace SnapGrab {
 internal sealed class ImageEditPatch : IDisposable {
  internal Bitmap Image,Mask;
  internal Rectangle Context,Content;
  internal static ImageEditPatch Create(Bitmap original,Rectangle region,Bitmap localMask) {
   if(region.Width<2 || region.Height<2 || !new Rectangle(Point.Empty,original.Size).Contains(region) || localMask.Size!=region.Size) throw new ArgumentException("Invalid image-edit region");
   ImageEditPatch patch=new ImageEditPatch();
   try {
    patch.Context=Rectangle.Intersect(Rectangle.Inflate(region,32,32),new Rectangle(Point.Empty,original.Size));
    double scale=Math.Min(1,1024.0/Math.Max(patch.Context.Width,patch.Context.Height));
    int w=Math.Max(1,(int)Math.Round(patch.Context.Width*scale)),h=Math.Max(1,(int)Math.Round(patch.Context.Height*scale));patch.Content=new Rectangle((1024-w)/2,(1024-h)/2,w,h);
    patch.Image=new Bitmap(1024,1024,PixelFormat.Format32bppArgb);patch.Mask=new Bitmap(1024,1024,PixelFormat.Format32bppArgb);
    using(Graphics g=Graphics.FromImage(patch.Image)){g.Clear(Color.White);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(original,patch.Content,patch.Context,GraphicsUnit.Pixel);}
    using(Graphics g=Graphics.FromImage(patch.Mask))g.Clear(Color.Black);
    byte[] permissions=ReadPixels(localMask),canvas=new byte[1024*1024*4];
    for(int i=3;i<canvas.Length;i+=4)canvas[i]=255;
    int editable=0;
    for(int y=patch.Content.Top;y<patch.Content.Bottom;y++) for(int x=patch.Content.Left;x<patch.Content.Right;x++) {
     int ox=patch.Context.Left+(int)((x-patch.Content.Left)*patch.Context.Width/(double)w),oy=patch.Context.Top+(int)((y-patch.Content.Top)*patch.Context.Height/(double)h);
     if(region.Contains(ox,oy) && permissions[((oy-region.Y)*region.Width+ox-region.X)*4+3]==0){canvas[(y*1024+x)*4+3]=0;editable++;}
    }
    if(editable==0)throw new InvalidOperationException("选区已全部保护，没有可编辑像素");
    WritePixels(patch.Mask,canvas);
    return patch;
   } catch {patch.Dispose();throw;}
  }
  internal Bitmap Merge(Bitmap original,Bitmap generated,Rectangle region,Bitmap localMask) {
   if(generated.Size!=new Size(1024,1024))throw new ArgumentException("Generated image dimensions");
   Bitmap result=(Bitmap)original.Clone();
   using(Bitmap restored=new Bitmap(Context.Width,Context.Height,PixelFormat.Format32bppArgb)) {
    using(Graphics g=Graphics.FromImage(restored)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(generated,new Rectangle(Point.Empty,restored.Size),Content,GraphicsUnit.Pixel);}
    byte[] permissions=ReadPixels(localMask),replacement=ReadPixels(restored);
    BitmapData data=result.LockBits(new Rectangle(Point.Empty,result.Size),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
    try {
     byte[] row=new byte[region.Width*4];
     for(int y=0;y<region.Height;y++) {
      IntPtr address=IntPtr.Add(data.Scan0,(region.Y+y)*data.Stride+region.X*4);Marshal.Copy(address,row,0,row.Length);
      for(int x=0;x<region.Width;x++) if(permissions[(y*region.Width+x)*4+3]==0) Buffer.BlockCopy(replacement,((region.Y+y-Context.Y)*Context.Width+region.X+x-Context.X)*4,row,x*4,4);
      Marshal.Copy(row,0,address,row.Length);
     }
    } finally {result.UnlockBits(data);}
   }
   return result;
  }
  internal static bool ReplacementFits(string text,Size region,float size){
   if(string.IsNullOrWhiteSpace(text)||region.Width<=4||region.Height<=4)return false;
   using(Bitmap probe=new Bitmap(1,1))using(Graphics g=Graphics.FromImage(probe))
   using(Font font=Dsh.F(Math.Max(8,Math.Min(96,size)),FontStyle.Regular)){
    int characters,lines;
    g.MeasureString(text,font,new SizeF(region.Width-4,region.Height-4),StringFormat.GenericDefault,out characters,out lines);
    return characters>=text.Length;
   }
  }
  internal static void DrawReplacement(Bitmap result,Rectangle region,Bitmap mask,string text,float size,Color color){
   byte[] permissions=ReadPixels(mask);
   using(GraphicsPath allowed=new GraphicsPath()){
    for(int y=0;y<region.Height;y++){
     int start=-1;
     for(int x=0;x<=region.Width;x++){
      bool editable=x<region.Width&&permissions[(y*region.Width+x)*4+3]==0;
      if(editable&&start<0)start=x;
      if(!editable&&start>=0){allowed.AddRectangle(new Rectangle(region.X+start,region.Y+y,x-start,1));start=-1;}
     }
    }
    using(Graphics g=Graphics.FromImage(result))using(Font font=Dsh.F(Math.Max(8,Math.Min(96,size)),FontStyle.Regular))using(SolidBrush brush=new SolidBrush(color)){
     g.SetClip(allowed);g.DrawString(text,font,brush,new RectangleF(region.X+2,region.Y+2,Math.Max(1,region.Width-4),Math.Max(1,region.Height-4)));
    }
   }
  }
  internal static byte[] ReadPixels(Bitmap image) {
   byte[] pixels=new byte[image.Width*image.Height*4];BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try {for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*image.Width*4,image.Width*4);}finally{image.UnlockBits(data);}return pixels;
  }
  internal static void WritePixels(Bitmap image,byte[] pixels) {
   BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
   try {for(int y=0;y<image.Height;y++)Marshal.Copy(pixels,y*image.Width*4,IntPtr.Add(data.Scan0,y*data.Stride),image.Width*4);}finally{image.UnlockBits(data);}
  }
  public void Dispose(){if(Image!=null)Image.Dispose();if(Mask!=null)Mask.Dispose();}
 }
}
