using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class DshSelect : DshControl
    {
        private readonly ComboBox box;

        private bool hover;

        public ComboBox Inner
        {
            get
            {
                return box;
            }
        }

        public int SelectedIndex
        {
            get
            {
                return box.SelectedIndex;
            }
            set
            {
                box.SelectedIndex = value;
            }
        }

        public ComboBox.ObjectCollection Items
        {
            get
            {
                return box.Items;
            }
        }

        public event EventHandler SelectedIndexChanged
        {
            add
            {
                box.SelectedIndexChanged += value;
            }
            remove
            {
                box.SelectedIndexChanged -= value;
            }
        }

        public DshSelect()
        {
            base.Height = 38;
            Cursor = Cursors.Hand;
            box = new ComboBox();
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.FlatStyle = FlatStyle.Flat;
            box.Font = Dsh.Body;
            box.DrawMode = DrawMode.OwnerDrawFixed;
            box.ItemHeight = 30;
            box.DrawItem += delegate(object sender, DrawItemEventArgs args)
            {
                if (args.Index < 0) return;
                bool selected = (args.State & DrawItemState.Selected) != 0;
                bool closed = (args.State & DrawItemState.ComboBoxEdit) != 0;
                using (SolidBrush brush = new SolidBrush(selected && !closed ? Dsh.Hover : Dsh.GlassFillStrong))
                    args.Graphics.FillRectangle(brush, args.Bounds);
                Rectangle textBounds = args.Bounds;
                textBounds.Inflate(-8, 0);
                // The closed field must align to the visible shell, not the native
                // ComboBox item height (which can exceed a short selector).
                if (closed)
                    textBounds = new Rectangle(textBounds.Left, 4 - box.Top,
                        Math.Max(1, textBounds.Width), Math.Max(1, Height - 8));
                Dsh.DrawText(args.Graphics, Convert.ToString(box.Items[args.Index]), Dsh.Body,
                    Enabled ? Dsh.TextPrimary : Dsh.TextCaption, textBounds, ContentAlignment.MiddleLeft);
            };
            box.SelectedIndexChanged += delegate { Invalidate(); };
            box.GotFocus += delegate { Invalidate(); };
            box.LostFocus += delegate { Invalidate(); };
            box.BackColor = Dsh.GlassFillStrong;
            box.ForeColor = Dsh.TextPrimary;
            box.IntegralHeight = false;
            box.DropDownHeight = 240;
            base.Controls.Add(box);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (box != null)
            {
                int preferredHeight = box.PreferredHeight;
                box.SetBounds(8, (base.Height - preferredHeight) / 2, Math.Max(10, base.Width - 16), preferredHeight);
                // Clip the native square arrow/border; the parent owns the rounded shell.
                Region oldRegion = box.Region;
                // Native preferred height can exceed a short selector. Keep its paint region
                // strictly inside the rounded shell, including the bottom edge.
                Rectangle interior = new Rectangle(10, 4, Math.Max(0, Width - 44), Math.Max(0, Height - 8));
                interior.Offset(-box.Left, -box.Top);
                interior.Intersect(new Rectangle(2, 2, Math.Max(0, box.Width - 28), Math.Max(0, box.Height - 4)));
                box.Region = new Region(interior);
                if (oldRegion != null) oldRegion.Dispose();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            box.Focus();
            box.DroppedDown = true;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = Setup(e.Graphics);
            graphics.Clear(ParentBack());
            Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
            Dsh.FillRound(graphics, r, Dsh.ButtonRadius(r), Dsh.GlassFillStrong);
            Dsh.StrokeRound(graphics, r, Dsh.ButtonRadius(r), hover || box.Focused ? Dsh.BorderFocus : Dsh.BorderInput, 1f);
            int num = base.Width - 17;
            int num2 = base.Height / 2;
            using (Pen pen = new Pen(Dsh.TextSecondary, 1.6f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.DrawLine(pen, num - 4, num2 - 2, num, num2 + 2);
                graphics.DrawLine(pen, num, num2 + 2, num + 4, num2 - 2);
            }
        }
    }
}
