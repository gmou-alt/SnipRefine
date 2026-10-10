using System;using System.Drawing;using System.Windows.Forms;
namespace SnapGrab {internal partial class EditorForm {
 private Panel workspaceProperties;private DshLabel propertyHint;private WideButton mosaicProperty;
 private void LayoutToolProperties(){
  if(workspaceProperties==null){workspaceProperties=new Panel {BackColor=PanelFill,AutoScroll=true};Controls.Add(workspaceProperties);propertyHint=new DshLabel("工具参数");workspaceProperties.Controls.Add(propertyHint);
   mosaicProperty=new WideButton {Text="块大小："+mosaicBlock+"px"};mosaicProperty.Click+=delegate {int[] values={4,8,12,16,24};int index=Array.IndexOf(values,mosaicBlock);mosaicBlock=values[(index+1)%values.Length];mosaicProperty.Text=UiText.T("块大小：")+mosaicBlock+"px";Invalidate();};workspaceProperties.Controls.Add(mosaicProperty);
  }
  workspaceProperties.SuspendLayout();workspaceProperties.AutoScrollPosition=Point.Empty;workspaceProperties.SetBounds(WorkspaceLeft,toolbarBottom-44,Math.Max(1,WinW-WorkspaceLeft),44);
  Shape.K kind=tool==Shape.K.Select&&selectedShape!=null ? selectedShape.Kind : tool;
  bool colorVisible=!aiRegionTool&&!maskTool&&(kind==Shape.K.Pen||kind==Shape.K.Line||kind==Shape.K.Arrow||kind==Shape.K.Rect||kind==Shape.K.Ellipse||kind==Shape.K.Text||kind==Shape.K.Number||kind==Shape.K.Redact);
  bool widthVisible=colorVisible&&kind!=Shape.K.Text&&kind!=Shape.K.Number&&kind!=Shape.K.Redact;
  bool effect=!aiRegionTool&&!maskTool&&EffectTool(tool),brush=effect&&effectBrush;
  string caption=aiRegionTool||maskTool ? "AI 参数见右侧" : tool==Shape.K.Select&&selectedShape!=null ? "选中标注参数" : tool==Shape.K.Eraser ? "擦除标注" : effect ? "效果参数" : tool==Shape.K.Select ? "拖动/缩放标注" : "工具参数";
  propertyHint.Text=UiText.T(caption);propertyHint.SetBounds(8,6,110,30);int x=122;
  foreach(ColorSwatch button in swatches){if(button.Parent!=workspaceProperties)workspaceProperties.Controls.Add(button);button.Visible=colorVisible;if(colorVisible){button.SetBounds(x,10,20,20);x+=26;}}
  foreach(ZoomButton button in zoomButtons)if(button.Tag2!=null){if(button.Parent!=workspaceProperties)workspaceProperties.Controls.Add(button);button.Visible=widthVisible;if(widthVisible){button.SetBounds(x,6,30,28);x+=34;}}
  if(effectBrushButton!=null){if(effectBrushButton.Parent!=workspaceProperties)workspaceProperties.Controls.Add(effectBrushButton);effectBrushButton.Visible=effect;if(effect){effectBrushButton.SetBounds(x,6,110,30);x+=118;}}
  if(effectSizeButton!=null){if(effectSizeButton.Parent!=workspaceProperties)workspaceProperties.Controls.Add(effectSizeButton);effectSizeButton.Visible=!aiRegionTool&&!maskTool&&(brush||tool==Shape.K.Eraser);if(effectSizeButton.Visible){effectSizeButton.SetBounds(x,6,140,30);x+=148;}}
  mosaicProperty.Visible=effect&&tool==Shape.K.Mosaic;if(mosaicProperty.Visible){mosaicProperty.SetBounds(x,6,126,30);x+=134;}
  workspaceProperties.AutoScrollMinSize=new Size(x+8,0);workspaceProperties.ResumeLayout(false);
 }
}}
