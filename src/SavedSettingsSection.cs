using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
namespace SnapGrab {
 internal sealed class SavedSettingsSection : Panel {
  internal readonly Panel Body;
  internal readonly DshLabel Receipt;
  private readonly Panel clip,summary;
  private readonly Timer timer;
  private readonly Stopwatch watch=new Stopwatch();
  private readonly SidebarMotion motion=new SidebarMotion();
  private readonly int openHeight;
  internal bool ManagedWidth;
  internal bool Open {get;private set;}
  internal event EventHandler OpenChanged;
  internal Panel Summary {get{return summary;}}
  internal SavedSettingsSection(int height){
   SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);BackColor=Dsh.Canvas;openHeight=Math.Max(140,height);
   Body=new Panel {BackColor=Dsh.Canvas};clip=new Panel {BackColor=Dsh.Canvas};clip.Controls.Add(Body);Controls.Add(clip);
   summary=new Panel {BackColor=Dsh.Canvas};Receipt=new DshLabel("");Receipt.SetBounds(24,16,520,60);summary.Controls.Add(Receipt);
   DshButton edit=new DshButton("继续编辑");edit.SetBounds(24,88,160,34);edit.Click+=delegate {SetOpen(true);};summary.Controls.Add(edit);Controls.Add(summary);
   timer=new Timer {Interval=15};timer.Tick+=delegate {double seconds=watch.Elapsed.TotalSeconds;watch.Restart();motion.Advance(seconds);ApplyFrame();};
   motion.Jump(1);Open=true;ApplyFrame();
  }
  internal static SavedSettingsSection Wrap(Panel parent,int offset){
   int height=140;foreach(Control control in parent.Controls)if(control.Top>=offset)height=Math.Max(height,control.Bottom-offset+16);
   int minWidth=parent.AutoScrollMinSize.Width>0 ? parent.AutoScrollMinSize.Width : Math.Max(1,parent.ClientSize.Width);
   SavedSettingsSection section=new SavedSettingsSection(height);section.SetBounds(0,offset,Math.Max(minWidth,parent.ClientSize.Width),height);section.Anchor=AnchorStyles.Top|AnchorStyles.Left;
   Control[] children=new Control[parent.Controls.Count];parent.Controls.CopyTo(children,0);
   foreach(Control child in children)if(child.Top>=offset){int top=child.Top-offset;parent.Controls.Remove(child);section.Body.Controls.Add(child);child.Top=top;}
   parent.Controls.Add(section);
   parent.SizeChanged+=delegate {if(!section.IsDisposed&&!section.ManagedWidth)section.Width=Math.Max(minWidth,parent.ClientSize.Width);};
   section.SizeChanged+=delegate {parent.AutoScrollMinSize=new Size(minWidth,offset+section.Height+16);};
   return section;
  }
  internal void Saved(string message){Receipt.Text="已保存 · "+DateTime.Now.ToString("HH:mm:ss")+"\n"+message;SetOpen(false);}
  internal void SetOpen(bool open){
   Open=open;motion.MoveTo(open ? 1 : 0);
   if(OpenChanged!=null)OpenChanged(this,EventArgs.Empty);
   if(!GlassSurface.MotionEnabled||!Visible){motion.Jump(open ? 1 : 0);ApplyFrame();return;}
   summary.Visible=false;clip.Visible=true;watch.Restart();timer.Start();ApplyFrame();
  }
  private void ApplyFrame(){
   int height=(int)Math.Round(openHeight*motion.Position);
   clip.Visible=height>0;clip.SetBounds(0,0,Math.Max(1,Width),height);Body.SetBounds(0,0,Math.Max(1,Width),openHeight);
   summary.SetBounds(0,0,Math.Max(1,Width),136);Receipt.Width=Math.Max(1,Width-48);
   bool collapsed=!Open&&!motion.Moving;summary.Visible=collapsed;Height=collapsed ? 136 : Math.Max(1,height);
   if(!motion.Moving){timer.Stop();watch.Stop();}
  }
  protected override void OnResize(EventArgs e){base.OnResize(e);if(clip!=null)ApplyFrame();}
  protected override void Dispose(bool disposing){if(disposing){timer.Stop();timer.Dispose();watch.Stop();}base.Dispose(disposing);}
 }
}
