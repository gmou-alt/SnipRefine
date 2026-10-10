using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab {
 internal partial class MainForm {
  private void OpenImageForEditing(){
   if(app==null)return;
   using(OpenFileDialog dialog=new OpenFileDialog {Title="选择需要修改的图片",Filter="图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif"}){
    if(dialog.ShowDialog(this)!=DialogResult.OK)return;
    try {using(Bitmap file=new Bitmap(dialog.FileName))app.EditChatImage((Bitmap)file.Clone(),"");}
    catch {SetStatus("图片无法打开，请检查格式、文件权限或图片尺寸");}
   }
  }
 }
}
