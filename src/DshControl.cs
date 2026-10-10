using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SnapGrab
{
    internal abstract class DshControl : Control
    {
        protected DshControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            Font = Dsh.Body;
        }

        protected override void OnTextChanged(System.EventArgs e)
        {base.OnTextChanged(e);Invalidate();}

        protected Graphics Setup(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            return g;
        }

        protected Color ParentBack()
        {
            Control parent = base.Parent;
            int num = 0;
            while (parent != null && num++ < 8)
            {
                if (parent.BackColor != Color.Transparent)
                {
                    return parent.BackColor;
                }
                parent = parent.Parent;
            }
            return Dsh.Canvas;
        }
    }
}
