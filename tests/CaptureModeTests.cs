using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
namespace SnapGrab
{
    internal static class CaptureModeTests
    {
        private static void Check(bool value,string text) { if(!value) throw new Exception(text); }
        internal static void Geometry()
        {
            Rectangle r=new Rectangle(20,20,60,40); Size bounds=new Size(100,80); Point anchor=new Point(30,30);
            Check(SelectionGeometry.Change(r,anchor,new Point(500,500),16,bounds)==new Rectangle(40,40,60,40),"Move beyond screen was not clamped");
            Check(SelectionGeometry.Change(r,anchor,new Point(-500,-500),16,bounds)==new Rectangle(0,0,60,40),"Negative move was not clamped");
            Check(SelectionGeometry.Change(r,anchor,new Point(50,30),16,bounds).Width==60,"Move changed width");
            Check(SelectionGeometry.Change(r,anchor,new Point(90,30),1,bounds)==new Rectangle(80,20,0,40),"Resize did not preserve opposite edge");
            Check(SelectionGeometry.Change(r,anchor,new Point(110,30),1,bounds)==new Rectangle(80,20,20,40),"Crossed resize edge did not normalize");
            Check(SelectionGeometry.Change(r,anchor,new Point(500,500),10,bounds)==new Rectangle(20,20,80,60),"Corner resize escaped screen");
            Check(SelectionGeometry.Hit(r,new Point(20,20),8)==5,"Corner handle missed");
            Check(SelectionGeometry.Hit(r,new Point(50,20),8)==4,"Top edge missed");
            Check(SelectionGeometry.Hit(r,new Point(50,40),8)==16,"Interior move missed");
            Check(SelectionGeometry.Hit(r,new Point(0,0),8)==0,"Outside unexpectedly moved");
        }
        internal static void Hotkeys()
        {
            uint modifiers,key; string text;
            Check(Config.TryParseHotkey("PrintScreen",out modifiers,out key,out text) && modifiers==0 && key==44,"Standalone PrintScreen rejected");
            Check(!Config.TryParseHotkey("A",out modifiers,out key,out text),"Unmodified typing key allowed");
            Check(Config.FormatHotkey(Keys.PrintScreen,out text) && text=="PrintScreen","PrintScreen recording rejected");
            Check(Config.TryParseHotkey(new Config().QuickCaptureHotkey,out modifiers,out key,out text),"Quick default invalid");
        }
        private static void Mouse(OverlayForm form,string method,int x,int y)
        {
            typeof(OverlayForm).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[] { new MouseEventArgs(MouseButtons.Left,1,x,y,0) });
        }
        internal static void Overlay()
        {
            using(var image=new Bitmap(100,80))
            using(var adjustable=new OverlayForm())
            {
                adjustable.Adjustable=true; adjustable.Prepare(image,new Rectangle(0,0,100,80),null);
                Mouse(adjustable,"OnMouseDown",10,10); Mouse(adjustable,"OnMouseMove",60,50); Mouse(adjustable,"OnMouseUp",60,50);
                Check(!adjustable.Accepted && adjustable.Selection==new Rectangle(10,10,50,40),"Adjustable mode confirmed on release");
                Mouse(adjustable,"OnMouseDown",30,30); Mouse(adjustable,"OnMouseMove",40,40); Mouse(adjustable,"OnMouseUp",40,40);
                Check(adjustable.Selection==new Rectangle(20,20,50,40),"Selected rectangle did not move");
                typeof(OverlayForm).GetMethod("ConfirmSelection",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(adjustable,null);
                Check(adjustable.Accepted,"Explicit confirmation failed");
            }
            using(var image=new Bitmap(100,80))
            using(var quick=new OverlayForm())
            {
                quick.Prepare(image,new Rectangle(0,0,100,80),null);
                Mouse(quick,"OnMouseDown",10,10); Mouse(quick,"OnMouseUp",60,50);
                Check(quick.Accepted,"Quick mode did not confirm on release");
            }
        }
    }
}
