using System;
using System.Drawing;
namespace SnapGrab
{
    internal enum CaptureMode { Adjustable, Quick, Fullscreen }
    internal static class SelectionGeometry
    {
        // Bits: left=1, right=2, top=4, bottom=8; interior=16.
        internal static int Hit(Rectangle r, Point p, int padding)
        {
            if (r.Width < 3 || r.Height < 3) return 0;
            Rectangle near=r; near.Inflate(padding,padding); if (!near.Contains(p)) return 0;
            int hit=0;
            if (Math.Abs(p.X-r.Left)<=padding) hit|=1;
            else if (Math.Abs(p.X-r.Right)<=padding) hit|=2;
            if (Math.Abs(p.Y-r.Top)<=padding) hit|=4;
            else if (Math.Abs(p.Y-r.Bottom)<=padding) hit|=8;
            return hit!=0 ? hit : r.Contains(p) ? 16 : 0;
        }
        internal static Rectangle Change(Rectangle r, Point start, Point current, int hit, Size bounds)
        {
            int dx=current.X-start.X, dy=current.Y-start.Y;
            if (hit==16) return new Rectangle(Math.Max(0,Math.Min(bounds.Width-r.Width,r.X+dx)),Math.Max(0,Math.Min(bounds.Height-r.Height,r.Y+dy)),r.Width,r.Height);
            int left=r.Left, right=r.Right, top=r.Top, bottom=r.Bottom;
            if ((hit&1)!=0) left=Math.Max(0,Math.Min(bounds.Width,left+dx));
            if ((hit&2)!=0) right=Math.Max(0,Math.Min(bounds.Width,right+dx));
            if ((hit&4)!=0) top=Math.Max(0,Math.Min(bounds.Height,top+dy));
            if ((hit&8)!=0) bottom=Math.Max(0,Math.Min(bounds.Height,bottom+dy));
            return Rectangle.FromLTRB(Math.Min(left,right),Math.Min(top,bottom),Math.Max(left,right),Math.Max(top,bottom));
        }
    }
}
