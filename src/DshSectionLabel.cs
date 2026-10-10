using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshSectionLabel : DshControl
    {
        public DshSectionLabel(string text)
        {
            Text = text;
            base.Height = 18;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = Setup(e.Graphics);
            Dsh.DrawText(g, Text, Dsh.Caption, Dsh.TextTertiary, new Rectangle(0, 0, base.Width, base.Height), ContentAlignment.MiddleLeft);
        }
    }
}
