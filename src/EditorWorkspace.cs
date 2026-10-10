using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab {
 internal partial class EditorForm {
  private Panel workspaceTools,workspaceAiCommands,workspaceDivider;
  private void LayoutAiToggle(){
   if(aiCloseButton==null)return;
   aiCloseButton.Text=aiOpen ? "›" : "‹";
   aiCloseButton.AccessibleName=aiOpen ? "收起 AI 面板" : "展开 AI 面板";
   aiCloseButton.SetBounds(Math.Max(WorkspaceLeft,WinW-AiVisibleWidth-27),ViewTop+Math.Max(0,(ViewBottom-ViewTop-64)/2),26,64);
   aiCloseButton.Visible=true;aiCloseButton.BringToFront();
  }
  private bool resizingWorkspace;
  private int resizeStartX,resizeStartWidth;
  private void LayoutWorkspaceDivider(){
   if(aiBody==null)return;
   if(workspaceDivider==null){
    workspaceDivider=new Panel {BackColor=Dsh.SidebarFlat,Cursor=Cursors.VSplit};Controls.Add(workspaceDivider);
    workspaceDivider.Paint+=delegate(object sender,PaintEventArgs e){using(Pen pen=new Pen(Dsh.TextTertiary,2))e.Graphics.DrawLine(pen,3,Math.Max(8,workspaceDivider.Height/2-18),3,Math.Max(8,workspaceDivider.Height/2+18));};
    workspaceDivider.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;resizingWorkspace=true;resizeStartX=Cursor.Position.X;resizeStartWidth=AiPanelWidth;workspaceDivider.Capture=true;};
    workspaceDivider.MouseMove+=delegate {if(!resizingWorkspace)return;requestedAiWidth=resizeStartWidth+resizeStartX-Cursor.Position.X;requestedAiWidth=Math.Max(280,Math.Min(Math.Max(280,WinW-WorkspaceLeft-227),requestedAiWidth));LayoutAiPanel();commitVersion++;Invalidate();};
    workspaceDivider.MouseUp+=delegate {resizingWorkspace=false;workspaceDivider.Capture=false;};
    workspaceDivider.MouseCaptureChanged+=delegate {if(!workspaceDivider.Capture)resizingWorkspace=false;};
    workspaceDivider.DoubleClick+=delegate {requestedAiWidth=0;LayoutAiPanel();commitVersion++;Invalidate();};
   }
   workspaceDivider.Visible=aiOpen && AiVisibleWidth>0;
   workspaceDivider.SetBounds(WinW-AiVisibleWidth-7,ViewTop,7,Math.Max(1,ViewBottom-ViewTop));workspaceDivider.BringToFront();
  }
  private readonly List<DshLabel> workspaceGroupTitles=new List<DshLabel>();
  private static void InvalidateWorkspaceButtons(Control parent){foreach(Control child in parent.Controls){if(child is EditorButton)child.Invalidate();else if(child.HasChildren)InvalidateWorkspaceButtons(child);}}
  private int WorkspaceLeft {get{return 132;}}
  private bool IsWorkspaceAi(Control button){
   foreach(string text in new string[]{"图片编辑设置","替换文字","去字修补","局部重绘","保护画笔","恢复修改","清空保护","AI 框选","清除AI选区","翻译","查看识别/译文","复制OCR","OCR"})if(button.Text==UiText.T(text))return true;
   return button.Text.StartsWith("画笔大小：");
  }
  private void LayoutWorkspaceTools(){
   if(workspaceTools==null){workspaceTools=new Panel {AutoScroll=true,BackColor=PanelFill};Controls.Add(workspaceTools);}
   Point scroll=workspaceTools.AutoScrollPosition;workspaceTools.SuspendLayout();workspaceTools.AutoScrollPosition=Point.Empty;
   workspaceTools.SetBounds(0,toolbarBottom,WorkspaceLeft,Math.Max(1,ViewBottom-toolbarBottom));
   int y=12;List<ToolButton> ordered=new List<ToolButton>();foreach(Shape.K kind in new[]{Shape.K.Pen,Shape.K.Select,Shape.K.Arrow,Shape.K.Text,Shape.K.Mosaic,Shape.K.Eraser,Shape.K.Line,Shape.K.Rect,Shape.K.Ellipse,Shape.K.Redact,Shape.K.Blur,Shape.K.Number})foreach(ToolButton button in toolButtons)if(button.Kind==kind)ordered.Add(button);foreach(ToolButton button in ordered){if(button.Parent!=workspaceTools)workspaceTools.Controls.Add(button);button.SetBounds(8,y,100,30);y+=36;}
   workspaceTools.AutoScrollMinSize=new Size(0,y+12);workspaceTools.ResumeLayout(false);workspaceTools.AutoScrollPosition=new Point(-scroll.X,-scroll.Y);
  }
  private int LayoutWorkspaceAiCommands(int width,int height){
   if(workspaceAiCommands==null){workspaceAiCommands=new Panel {AutoScroll=true,BackColor=Dsh.SidebarFlat};aiBody.Controls.Add(workspaceAiCommands);}
   int visibleHeight=Math.Max(48,Math.Min(284,height-300));
   bool resized=workspaceAiCommands.Width!=width-16||workspaceAiCommands.Height!=visibleHeight;
   Point scroll=workspaceAiCommands.AutoScrollPosition;
   workspaceAiCommands.SuspendLayout();workspaceAiCommands.AutoScrollPosition=Point.Empty;
   workspaceAiCommands.SetBounds(8,44,width-16,visibleHeight);
   string[][] groups={new[]{"AI 框选","替换文字","去字修补","局部重绘","图片编辑设置"},new[]{"清除AI选区","保护画笔","恢复修改","清空保护"},new[]{"OCR","翻译","查看识别/译文","复制OCR"}};
   string[] titles={"图片修改 · 先框选再选择任务","保护与选区","文字识别","标注与整理"};
   while(workspaceGroupTitles.Count<titles.Length){DshLabel label=new DshLabel(UiText.T(titles[workspaceGroupTitles.Count]));label.Small=true;workspaceGroupTitles.Add(label);workspaceAiCommands.Controls.Add(label);}
   int columnWidth=Math.Max(80,(width-42-SystemInformation.VerticalScrollBarWidth)/2),y=4;
   for(int group=0;group<titles.Length;group++){
    workspaceGroupTitles[group].SetBounds(4,y,width-48,22);y+=26;
    List<Control> buttons=new List<Control>();
    if(group<groups.Length){foreach(string text in groups[group])foreach(WideButton button in wideButtons)if(button.Text==UiText.T(text))buttons.Add(button);
     if(group==1)foreach(WideButton button in wideButtons)if(button.Text.StartsWith("画笔大小："))buttons.Add(button);
    }else foreach(GlassButton button in aiQuickButtons)buttons.Add(button);
    int i=0;foreach(Control button in buttons){if(button.Parent!=workspaceAiCommands)workspaceAiCommands.Controls.Add(button);button.Visible=true;button.SetBounds(4+(i%2)*(columnWidth+8),y+(i/2)*38,columnWidth,32);i++;}
    y+=((i+1)/2)*38+8;
   }
   workspaceAiCommands.AutoScrollMinSize=new Size(0,y);workspaceAiCommands.ResumeLayout(false);
   workspaceAiCommands.AutoScrollPosition=resized ? Point.Empty : new Point(-scroll.X,-scroll.Y);
   return 44+visibleHeight+8;
  }
 }
}
