using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshLabel : DshControl
    {
        private ContentAlignment align = ContentAlignment.MiddleLeft;

        private bool small;

        private bool tertiary;

        private bool medium;

        private bool onDark;

        private Color? ink;

        public ContentAlignment TextAlign
        {
            get
            {
                return align;
            }
            set
            {
                align = value;
                Invalidate();
            }
        }

        public bool Small
        {
            get
            {
                return small;
            }
            set
            {
                small = true;
                Invalidate();
            }
        }

        public bool Tertiary
        {
            get
            {
                return tertiary;
            }
            set
            {
                tertiary = true;
                Invalidate();
            }
        }

        public bool Medium
        {
            get
            {
                return medium;
            }
            set
            {
                medium = true;
                Invalidate();
            }
        }

        public bool OnDark
        {
            get
            {
                return onDark;
            }
            set
            {
                onDark = true;
                Invalidate();
            }
        }

        public Color Ink
        {
            get
            {
                if (!ink.HasValue)
                {
                    return Dsh.TextPrimary;
                }
                return ink.Value;
            }
            set
            {
                ink = value;
                Invalidate();
            }
        }

        internal void RefreshThemeInk(System.Collections.Generic.Dictionary<int, Color> colors)
        {
            Color replacement;
            if (ink.HasValue && colors.TryGetValue(ink.Value.ToArgb(), out replacement)) ink = replacement;
        }

        public DshLabel(string text)
        {
            Text = UiText.T(text);
            base.Height = 20;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = Setup(e.Graphics);
            Color color = (ink.HasValue ? ink.Value : ((!onDark) ? (tertiary ? Dsh.TextTertiary : Dsh.TextSecondary) : (tertiary ? Dsh.OnDarkFaint : Dsh.OnDarkMuted)));
            Font font = (small ? Dsh.Small : (medium ? Dsh.Medium : Dsh.Body));
            Dsh.DrawText(g, Text, font, color, new Rectangle(0, 0, base.Width, base.Height), align);
        }
    }
}
