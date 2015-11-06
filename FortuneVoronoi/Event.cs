using System;
using System.Numerics;

namespace FortuneVoronoi
{
    internal abstract class VEvent : IComparable<VEvent>
    {
        public abstract double Y { get; }
        protected abstract double X { get; }

        #region IComparable Members
        public int CompareTo(VEvent evt)
        {
            var i = Y.CompareTo(evt.Y);
            if (i != 0)
                return i;
            return X.CompareTo(evt.X);
        }
        #endregion
    }

    internal class VDataEvent : VEvent
    {
        public Vector2 DataPoint;

        public VDataEvent(Vector2 dp)
        {
            DataPoint = dp;
        }

        public override double Y
        {
            get
            {
                return DataPoint.Y;
            }
        }

        protected override double X
        {
            get
            {
                return DataPoint.X;
            }
        }

    }

    internal class VCircleEvent : VEvent
    {
        public VDataNode NodeN, NodeL, NodeR;
        public Vector2 Center;

        public override double Y
        {
            get { return Math.Round(Center.Y + Vector2.Distance(NodeN.DataPoint, Center), 10); }
        }

        protected override double X
        {
            get
            {
                return Center.X;
            }
        }

        public bool Valid = true;
    }
}
