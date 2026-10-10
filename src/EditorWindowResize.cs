using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab {
 internal partial class EditorForm {
  protected override CreateParams CreateParams {
   get {CreateParams parameters=base.CreateParams;parameters.Style|=0x00040000;return parameters;}
  }
  private int WindowHitTest(Point point){
   if(WindowState==FormWindowState.Maximized)return 1;
   int edge=Math.Max(6,(int)Math.Ceiling(6*DeviceScale()));
   bool left=point.X<edge,right=point.X>=ClientSize.Width-edge,top=point.Y<edge,bottom=point.Y>=ClientSize.Height-edge;
   if(top&&left)return 13;if(top&&right)return 14;if(bottom&&left)return 16;if(bottom&&right)return 17;
   if(left)return 10;if(right)return 11;if(top)return 12;if(bottom)return 15;
   return point.Y<TitleH ? 2 : 1;
  }
  private float DeviceScale(){using(Graphics graphics=CreateGraphics())return graphics.DpiX/96f;}
  protected override void WndProc(ref Message message){
   base.WndProc(ref message);
   if(message.Msg!=0x84||message.Result.ToInt32()!=1)return;
   long coordinates=message.LParam.ToInt64();Point screen=new Point((short)(coordinates&65535),(short)((coordinates>>16)&65535));
   message.Result=new IntPtr(WindowHitTest(PointToClient(screen)));
  }
 }
}
