using System.Collections.Generic;
using System.Drawing;

namespace SnapGrab
{
    internal class UndoStep
    {
        public enum T
        {
            Shapes,
            Crop,
            Reset,
            Ai,
            Edit
        }

        public T Kind;

        public List<Shape> Items = new List<Shape>();

        public Bitmap Snapshot;

        public bool HadSource;

        public void DisposeSnap()
        {
            if (Snapshot != null)
            {
                Snapshot.Dispose();
                Snapshot = null;
            }
        }
    }
}
