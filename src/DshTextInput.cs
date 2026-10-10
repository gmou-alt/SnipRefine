using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshTextInput : DshControl
    {
        private readonly TextBox box;

        private bool focused;

        private bool readOnlyLook;

        private string placeholder = "";

        public TextBox Inner
        {
            get
            {
                return box;
            }
        }

        public override string Text
        {
            get
            {
                return box.Text;
            }
            set
            {
                box.Text = value;
            }
        }

        public bool ReadOnlyLook
        {
            get
            {
                return readOnlyLook;
            }
            set
            {
                readOnlyLook = value;
                Invalidate();
            }
        }

        public string Placeholder
        {
            get
            {
                return placeholder;
            }
            set
            {
                placeholder = value;
                Invalidate();
            }
        }

        public event KeyEventHandler InnerKeyDown
        {
            add
            {
                box.KeyDown += value;
            }
            remove
            {
                box.KeyDown -= value;
            }
        }

        public event EventHandler InnerClick
        {
            add
            {
                box.Click += value;
            }
            remove
            {
                box.Click -= value;
            }
        }

        public event EventHandler InnerEnter
        {
            add
            {
                box.Enter += value;
            }
            remove
            {
                box.Enter -= value;
            }
        }

        public DshTextInput()
        {
            base.Height = 38;
            box = new TextBox();
            box.BorderStyle = BorderStyle.None;
            box.Font = Dsh.Body;
            box.BackColor = Dsh.GlassFillStrong;
            box.ForeColor = Dsh.TextPrimary;
            TextBox textBox = box;
            EventHandler value = delegate
            {
                focused = true;
                Invalidate();
            };
            textBox.GotFocus += value;
            box.LostFocus += delegate
            {
                focused = false;
                Invalidate();
            };
            base.Controls.Add(box);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (box != null)
            {
                int num = 13;
                int preferredHeight = box.PreferredHeight;
                box.SetBounds(num, Math.Max(0, (base.Height - preferredHeight) / 2 - 1), Math.Max(10, base.Width - num * 2), preferredHeight);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            graphics.Clear(ParentBack());
            Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
            if (readOnlyLook)
            {
                Dsh.DrawGlass(graphics, r, 8, true, false);
            }
            else
            {
                Dsh.FillRound(graphics, r, 8, Dsh.GlassFillStrong);
                Dsh.StrokeRound(graphics, r, 8, focused ? Dsh.BorderFocus : Dsh.BorderInput, focused ? 1.4f : 1f);
            }
            if (box.TextLength == 0 && placeholder.Length > 0 && !focused)
            {
                Dsh.DrawText(graphics, placeholder, Dsh.Body, Dsh.TextCaption, new Rectangle(14, 0, base.Width - 28, base.Height), ContentAlignment.MiddleLeft);
            }
        }
    }
}
