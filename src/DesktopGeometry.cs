using System;
using System.Drawing;
namespace SnapGrab
{
    internal static class DesktopGeometry
    {
        internal static Rectangle Bounds(Rectangle[] displays)
        {
            if(displays==null || displays.Length==0) throw new ArgumentException("No display bounds");
            Rectangle result=displays[0];
            foreach(Rectangle display in displays) {
                if(display.Width<=0 || display.Height<=0) throw new ArgumentException("Invalid display bounds");
                result=Rectangle.Union(result,display);
            }
            return result;
        }
        internal static Rectangle ToSnapshot(Rectangle screenSelection,Rectangle desktop)
        {
            Rectangle clipped=Rectangle.Intersect(screenSelection,desktop);
            return clipped.Width<=0 || clipped.Height<=0 ? Rectangle.Empty :
                new Rectangle(clipped.X-desktop.X,clipped.Y-desktop.Y,clipped.Width,clipped.Height);
        }
        internal static Rectangle ToDesktop(Rectangle localSelection,Rectangle desktop)
        {
            Rectangle clipped=Rectangle.Intersect(localSelection,new Rectangle(Point.Empty,desktop.Size));
            return clipped.Width<=0 || clipped.Height<=0 ? Rectangle.Empty :
                new Rectangle(clipped.X+desktop.X,clipped.Y+desktop.Y,clipped.Width,clipped.Height);
        }
        internal static int Pixels(int logical,int dpi)
        {
            return Math.Max(1,(int)Math.Round(logical*Math.Max(96,Math.Min(384,dpi))/96.0));
        }
    }
}
