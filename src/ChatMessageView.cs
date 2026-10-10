using System;
using System.Drawing;
using System.Windows.Forms;
namespace SnapGrab
{
    // Plain readable messages; role comes from structured history, never text parsing.
    internal sealed class ChatMessageView : Panel
    {
        private AiChat.Message[] messages = new AiChat.Message[0];
        private bool layingOut;
        internal ChatMessageView()
        {
            AutoScroll = true; DoubleBuffered = true; BackColor = Dsh.Canvas;
        }
        internal void SetMessages(AiChat.Message[] value)
        {
            bool same = value.Length == messages.Length;
            if (same) for (int i=0;i<value.Length;i++) if (!object.ReferenceEquals(value[i],messages[i])) { same=false; break; }
            if (same && Controls.Count > 0) return;
            messages=value; SuspendLayout();
            while (Controls.Count > 0) { Control c=Controls[0]; Controls.Remove(c); c.Dispose(); }
            if(value.Length==0)
            {
                Controls.Add(new Label { Text="有什么想一起完成的？\r\n\r\n输入问题，或添加图片开始对话。\r\n图片修改可标注、打码和裁剪。", AutoSize=false, ForeColor=Dsh.TextSecondary, BackColor=Dsh.Canvas, Font=Dsh.Body, Tag="empty" });
            }
            foreach(AiChat.Message message in value)
            {
                var bubble=new MessageBubble(message.Role=="你");
                var text=new TextBox { Multiline=true, ReadOnly=true, BorderStyle=BorderStyle.None, ScrollBars=ScrollBars.None, Text=message.Text, Font=Dsh.F(13f,FontStyle.Regular), ForeColor=Dsh.TextPrimary, BackColor=bubble.Fill, TabStop=false };
                bubble.Controls.Add(text); Controls.Add(bubble);
            }
            ResumeLayout(); Arrange();
            if(Controls.Count>0) ScrollControlIntoView(Controls[Controls.Count-1]);
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); Arrange(); }
        private void Arrange()
        {
            if(layingOut || ClientSize.Width<40) return;
            layingOut=true;
            try
            {
                int scroll=AutoScrollPosition.Y, y=18, available=Math.Max(40,ClientSize.Width-36);
                foreach(Control control in Controls)
                {
                    MessageBubble bubble=control as MessageBubble;
                    if(bubble==null) { control.SetBounds(18,y+scroll,available,160);y+=180;continue; }
                    int maximum=Math.Max(40,(int)(available*.88));
                    TextBox body=(TextBox)bubble.Controls[0];
                    Size natural=TextRenderer.MeasureText(body.Text.Length==0?" ":body.Text,body.Font,new Size(Math.Max(12,maximum-28),int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPrefix);
                    int width=Math.Min(maximum,Math.Max(64,natural.Width+28));
                    TextBox text=(TextBox)bubble.Controls[0];
                    int textWidth=Math.Max(12,width-28);
                    Size size=TextRenderer.MeasureText(text.Text.Length==0?" ":text.Text,text.Font,new Size(textWidth,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPrefix);
                    int h=Math.Max(24,size.Height+10);
                    bubble.SetBounds(18+(bubble.User?available-width:0),y+scroll,width,h+24);
                    text.SetBounds(14,12,textWidth,h);
                    y+=bubble.Height+16;
                }
                AutoScrollMinSize=new Size(0,y);
            }
            finally { layingOut=false; }
        }
        private sealed class MessageBubble : Panel
        {
            internal readonly bool User;
            internal Color Fill { get { return User ? (Dsh.LightTheme ? Color.FromArgb(223,235,252) : Color.FromArgb(49,72,106)) : Dsh.GlassFillStrong; } }
            internal MessageBubble(bool user)
            {
                User=user; DoubleBuffered=true; BackColor=Dsh.Canvas;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(Parent==null?Dsh.Canvas:Parent.BackColor);
                e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Dsh.FillRound(e.Graphics,new Rectangle(0,0,Width,Height),16,Fill);
                foreach(Control c in Controls) { if(c.BackColor!=Fill)c.BackColor=Fill; if(c.ForeColor!=Dsh.TextPrimary)c.ForeColor=Dsh.TextPrimary; }
            }
        }
    }
}
