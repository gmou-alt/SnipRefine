using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshGlassCard : DshControl
    {
        public DshGlassCard()
        {
            BackColor = Dsh.Canvas;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            graphics.Clear(BackColor);
            if (ClientSize.Width <= 1 || ClientSize.Height <= 1) return;
            Dsh.DrawGlass(graphics, new Rectangle(0, 0, base.Width - 1, base.Height - 1), 16, false, true);
        }
    }
}
