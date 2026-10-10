using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshNumber : DshControl
    {
        private readonly TextBox box;

        private bool focused;

        private bool upHot;

        private bool downHot;

        private int min;

        private int max = 100;

        private int val = 90;

        public int Minimum
        {
            get
            {
                return min;
            }
            set
            {
                min = value;
            }
        }

        public int Maximum
        {
            get
            {
                return max;
            }
            set
            {
                max = value;
            }
        }

        public bool Enabled2
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;
                box.Enabled = value;
                Invalidate();
            }
        }

        public int Value
        {
            get
            {
                return val;
            }
            set
            {
                int num = value;
                if (num < min)
                {
                    num = min;
                }
                if (num > max)
                {
                    num = max;
                }
                val = num;
                if (box.Text != num.ToString())
                {
                    box.Text = num.ToString();
                }
                Invalidate();
            }
        }

        public event EventHandler ValueChanged;

        public DshNumber()
        {
            base.Height = 38;
            box = new TextBox();
            box.BorderStyle = BorderStyle.None;
            box.Font = Dsh.Body;
            box.BackColor = Dsh.GlassFillStrong;
            box.ForeColor = Dsh.TextPrimary;
            box.TextAlign = HorizontalAlignment.Center;
            box.Text = val.ToString();
            TextBox textBox = box;
            EventHandler value = delegate
            {
                focused = true;
                Invalidate();
            };
            textBox.GotFocus += value;
            box.LostFocus += delegate
            {
                Commit();
                focused = false;
                Invalidate();
            };
            box.KeyDown += OnBoxKeyDown;
            base.Controls.Add(box);
        }

        private void OnBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                Value = val + 1;
                Raise();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                Value = val - 1;
                Raise();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Return)
            {
                Commit();
                e.Handled = true;
            }
        }

        private void Commit()
        {
            int result;
            if (int.TryParse(box.Text.Trim(), out result))
            {
                Value = result;
            }
            box.Text = val.ToString();
            Raise();
        }

        private void Raise()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (box != null)
            {
                int preferredHeight = box.PreferredHeight;
                box.SetBounds(6, Math.Max(0, (base.Height - preferredHeight) / 2 - 1), Math.Max(10, base.Width - 28), preferredHeight);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool flag = e.X >= base.Width - 24 && e.Y < base.Height / 2;
            bool flag2 = e.X >= base.Width - 24 && e.Y >= base.Height / 2;
            if (flag != upHot || flag2 != downHot)
            {
                upHot = flag;
                downHot = flag2;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            upHot = (downHot = false);
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.X >= base.Width - 24)
            {
                if (e.Y < base.Height / 2)
                {
                    Value = val + 1;
                }
                else
                {
                    Value = val - 1;
                }
                Raise();
            }
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            graphics.Clear(ParentBack());
            Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
            if (base.Enabled)
            {
                Dsh.FillRound(graphics, r, Dsh.ButtonRadius(r), Dsh.GlassFillStrong);
                Dsh.StrokeRound(graphics, r, Dsh.ButtonRadius(r), focused ? Dsh.BorderFocus : Dsh.BorderInput, focused ? 1.4f : 1f);
            }
            else
            {
                Dsh.FillRound(graphics, r, Dsh.ButtonRadius(r), Dsh.Pressed);
                Dsh.StrokeRound(graphics, r, Dsh.ButtonRadius(r), Dsh.HairlineSoft, 1f);
            }
            int num = base.Width - 12;
            Color color = ((!base.Enabled) ? Dsh.TextCaption : (upHot ? Dsh.TextPrimary : Dsh.TextSecondary));
            Color color2 = ((!base.Enabled) ? Dsh.TextCaption : (downHot ? Dsh.TextPrimary : Dsh.TextSecondary));
            using (Pen pen = new Pen(color, 1.5f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.DrawLine(pen, num - 3, base.Height / 2 - 5, num, base.Height / 2 - 8);
                graphics.DrawLine(pen, num, base.Height / 2 - 8, num + 3, base.Height / 2 - 5);
            }
            using (Pen pen2 = new Pen(color2, 1.5f))
            {
                pen2.StartCap = LineCap.Round;
                pen2.EndCap = LineCap.Round;
                graphics.DrawLine(pen2, num - 3, base.Height / 2 + 5, num, base.Height / 2 + 8);
                graphics.DrawLine(pen2, num, base.Height / 2 + 8, num + 3, base.Height / 2 + 5);
            }
        }
    }
}
