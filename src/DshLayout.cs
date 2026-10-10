using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshLayout
    {
        private readonly Control host;

        private readonly int x;

        private readonly int width;

        private int y;

        public int Y
        {
            get
            {
                return y;
            }
        }

        public int Width
        {
            get
            {
                return width;
            }
        }

        public int X
        {
            get
            {
                return x;
            }
        }

        public DshLayout(Control host, int x, int y, int width)
        {
            this.host = host;
            this.x = x;
            this.y = y;
            this.width = width;
        }

        public void Gap(int px)
        {
            y += px;
        }

        public DshSectionLabel Section(string text)
        {
            DshSectionLabel dshSectionLabel = new DshSectionLabel(text);
            dshSectionLabel.SetBounds(x, y, width, 18);
            host.Controls.Add(dshSectionLabel);
            y += 28;
            return dshSectionLabel;
        }

        public DshLabel FieldLabel(string text)
        {
            DshLabel dshLabel = new DshLabel(text);
            dshLabel.Medium = true;
            dshLabel.SetBounds(x, y, width, 20);
            host.Controls.Add(dshLabel);
            y += 28;
            return dshLabel;
        }

        public DshLabel Hint(string text)
        {
            DshLabel dshLabel = new DshLabel(text);
            dshLabel.Small = true;
            dshLabel.Tertiary = true;
            dshLabel.SetBounds(x, y, width, 18);
            host.Controls.Add(dshLabel);
            y += 18;
            return dshLabel;
        }

        public T Put<T>(T c, int height) where T : Control
        {
            c.SetBounds(x, y, width, height);
            host.Controls.Add(c);
            y += height;
            return c;
        }

        public void EndField()
        {
            y += 14;
        }
    }
}
