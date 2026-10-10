using System.Collections.Generic;
using System.Drawing;

namespace SnapGrab
{
    internal class Shape
    {
        public enum K
        {
            Pen,
            Line,
            Arrow,
            Rect,
            Ellipse,
            Text,
            Mosaic,
            Select,
            Redact,
            Blur,
            Number,
            Eraser
        }

        public K Kind;

        public bool Brush;

        public List<PointF> Points = new List<PointF>();

        public RectangleF Rect;

        public RectangleF AiClip;

        public Color Color;

        public float Width;

        public string Text;

        public float FontPx;

        public int Block;

        public Shape Clone()
        {
            Shape shape = (Shape)MemberwiseClone();
            shape.Points = new List<PointF>(Points);
            return shape;
        }
    }
}
