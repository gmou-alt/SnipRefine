using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace SnapGrab
{
    internal sealed class DshContextMenu : ContextMenuStrip
    {
        internal DshContextMenu()
        {
            Renderer=new DshMenuRenderer(); Font=Dsh.Body; BackColor=Dsh.Canvas;
            Padding=new Padding(6); DropShadowEnabled=false; ShowImageMargin=true;
        }
        protected override void OnItemAdded(ToolStripItemEventArgs e)
        {
            base.OnItemAdded(e);
            if(!(e.Item is ToolStripSeparator)) e.Item.Padding=new Padding(6,5,6,5);
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if(Width<2 || Height<2) return;
            Region old=Region;
            using(GraphicsPath path=Dsh.Round(new Rectangle(0,0,Width,Height),12)) Region=new Region(path);
            if(old!=null) old.Dispose();
        }
    }
    internal sealed class DshMenuRenderer : ToolStripProfessionalRenderer
    {
        internal DshMenuRenderer() { RoundedEdges=false; }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            Rectangle r=e.ToolStrip.ClientRectangle;
            if(r.Width<2 || r.Height<2) return;
            e.Graphics.Clear(Dsh.Canvas);
            using(var fill=new LinearGradientBrush(r,Dsh.GlassFill,Dsh.Canvas,LinearGradientMode.Vertical)) e.Graphics.FillRectangle(fill,r);
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Dsh.StrokeRound(e.Graphics,new Rectangle(0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1),12,Color.FromArgb(100,Dsh.PrimaryFill),1);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if(e.Item.Selected && e.Item.Enabled)
                Dsh.FillRound(e.Graphics,new Rectangle(3,1,Math.Max(1,e.Item.Width-6),Math.Max(1,e.Item.Height-2)),7,Color.FromArgb(35,Dsh.PrimaryFill));
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor=e.Item.Enabled ? Dsh.TextPrimary : Dsh.TextTertiary;
            base.OnRenderItemText(e);
        }
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using(Pen pen=new Pen(Color.FromArgb(45,Dsh.PrimaryFill)))
                e.Graphics.DrawLine(pen,12,e.Item.Height/2,e.Item.Width-12,e.Item.Height/2);
        }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias; Rectangle r=e.ImageRectangle;
            using(Pen pen=new Pen(Dsh.PrimaryFill,2))
            { pen.StartCap=pen.EndCap=LineCap.Round; e.Graphics.DrawLines(pen,new Point[] { new Point(r.Left+3,r.Top+8),new Point(r.Left+7,r.Top+12),new Point(r.Left+14,r.Top+4) }); }
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor=Dsh.TextSecondary; base.OnRenderArrow(e); }
    }
    internal static class DshMenuTheme
    {
        private static readonly HashSet<Control> watched=new HashSet<Control>();
        internal static void Install()
        {
            ToolStripManager.Renderer=new DshMenuRenderer();
            Application.Idle+=delegate { foreach(Form form in Application.OpenForms) Watch(form); };
        }
        private static void Watch(Control control)
        {
            if(!watched.Add(control)) return;
            control.Disposed+=delegate { watched.Remove(control); };
            control.ControlAdded+=delegate(object sender,ControlEventArgs e) { Watch(e.Control); };
            foreach(Control child in control.Controls) Watch(child);
            TextBox text=control as TextBox;
            if(text==null || text.ContextMenuStrip!=null) return;
            var menu=new DshContextMenu();
            var undo=new ToolStripMenuItem(UiText.T("撤销"),null,delegate { text.Undo(); });
            var cut=new ToolStripMenuItem("剪切",null,delegate { text.Cut(); });
            var copy=new ToolStripMenuItem(UiText.T("复制"),null,delegate { text.Copy(); });
            var paste=new ToolStripMenuItem("粘贴",null,delegate { text.Paste(); });
            var all=new ToolStripMenuItem("全选",null,delegate { text.SelectAll(); });
            menu.Items.AddRange(new ToolStripItem[] {undo,new ToolStripSeparator(),cut,copy,paste,new ToolStripSeparator(),all});
            menu.Opening+=delegate(object sender,CancelEventArgs e)
            {
                bool masked=text.UseSystemPasswordChar || text.PasswordChar!='\0';
                undo.Enabled=!text.ReadOnly && text.CanUndo;
                cut.Enabled=!text.ReadOnly && !masked && text.SelectionLength>0;
                copy.Enabled=!masked && text.SelectionLength>0;
                bool hasText=false; try { hasText=Clipboard.ContainsText(); } catch { }
                paste.Enabled=!text.ReadOnly && hasText; all.Enabled=text.TextLength>0;
            };
            text.ContextMenuStrip=menu; text.Disposed+=delegate { menu.Dispose(); };
        }
    }
}
