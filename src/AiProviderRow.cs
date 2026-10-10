using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapGrab
{
    internal class AiProviderRow : DshControl
    {
        public AiProvider Provider;

        private bool hover;
        private readonly DshButton editButton;
        public event EventHandler EditRequested;


        private bool active;

        public AiProviderRow()
        {
            base.Height = 44;
            Cursor = Cursors.Hand;
            editButton = new DshButton(UiText.T("编辑"));
            editButton.Click += delegate { if (EditRequested != null) EditRequested(this, EventArgs.Empty); };
            Controls.Add(editButton);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (editButton != null) editButton.SetBounds(Math.Max(0, Width - 74), 7, 64, 30);
        }

        public void Bind(AiProvider p, bool isActive)
        {
            Provider = p;
            active = isActive;
            Invalidate();
        }

        public void SetActive(bool v)
        {
            if (active != v)
            {
                active = v;
                Invalidate();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = Setup(e.Graphics);
            Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
            Color fill = (active ? Dsh.Hover : (hover ? Dsh.Hover : Color.Transparent));
            if (fill.A != 0)
            {
                Dsh.FillRound(g, r, Dsh.ButtonRadius(r), fill);
            }
            Dsh.StrokeRound(g, r, Dsh.ButtonRadius(r), active ? Dsh.BorderInput : Dsh.HairlineSoft, 1f);
            if (active)
            {
                Dsh.FillRound(g, new Rectangle(0, 10, 3, base.Height - 21), 2, Dsh.TextPrimary);
            }
            string text = ((Provider != null) ? Provider.Name : "");
            string text2 = ((Provider == null) ? "" : (string.IsNullOrEmpty(Provider.Endpoint) ? "未填地址" : Provider.Endpoint));
            string text3 = ((Provider != null) ? Provider.FirstModel : "");
            Dsh.DrawText(g, text, Dsh.Medium, Dsh.TextPrimary, new Rectangle(14, 5, Math.Max(40, base.Width - 280), 19), ContentAlignment.MiddleLeft);
            Dsh.DrawText(g, text2, Dsh.Caption, Dsh.TextTertiary, new Rectangle(14, 23, Math.Max(40, base.Width - 280), 17), ContentAlignment.MiddleLeft);
            if (!string.IsNullOrEmpty(text3))
            {
                Dsh.DrawText(g, text3, Dsh.Caption, Dsh.TextTertiary, new Rectangle(Math.Max(0, base.Width - 260), 5, 180, 19), ContentAlignment.MiddleCenter);
            }
            if (active)
            {
                Dsh.DrawText(g, "使用中", Dsh.Caption, Dsh.TextSecondary, new Rectangle(Math.Max(0, base.Width - 260), 23, 180, 17), ContentAlignment.MiddleCenter);
            }
        }
    }
}
