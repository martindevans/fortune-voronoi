using System;
using System.Numerics;

namespace FortuneVoronoi
{
    public class Edge
    {
        internal bool Done;

        public Vector2 RightData { get; internal set; }
        public Vector2 LeftData { get; internal set; }

        internal Vector2? VVertexA { get; set; }
        internal Vector2? VVertexB { get; set; }

        internal void AddVertex(Vector2 v)
        {
            if (!VVertexA.HasValue)
                VVertexA = v;
            else if (!VVertexB.HasValue)
                VVertexB = v;
            else
                throw new Exception("Tried to add third vertex!");
        }

        internal bool IsInfinite
        {
            get { return VVertexA == Fortune.VvInfinite && VVertexB == Fortune.VvInfinite; }
        }

        internal bool IsPartlyInfinite
        {
            get { return VVertexA == Fortune.VvInfinite || VVertexB == Fortune.VvInfinite; }
        }

        public Vector2 FixedPoint
        {
            get
            {
                if (!VVertexA.HasValue || !VVertexB.HasValue)
                    throw new InvalidOperationException("Cannot get FixedPoint from edge which is not completely initialised");

                if (IsInfinite)
                    return 0.5f * (LeftData + RightData);

                if (VVertexA != Fortune.VvInfinite)
                    return VVertexA.Value;

                return VVertexB.Value;
            }
        }

        public Vector2 DirectionVector
        {
            get
            {
                if (!VVertexA.HasValue || !VVertexB.HasValue)
                    throw new InvalidOperationException("Cannot get DirectionVector from edge which is not completely initialised");

                if (!IsPartlyInfinite)
                    return Vector2.Normalize(VVertexB.Value - VVertexA.Value);

                if (Math.Abs(LeftData.X - RightData.X) < float.Epsilon)
                {
                    if (LeftData.Y < RightData.Y)
                        return new Vector2(-1, 0);
                    return new Vector2(1, 0);
                }

                var erg = new Vector2(-(RightData.Y - LeftData.Y) / (RightData.X - LeftData.X), 1);
                if (RightData.X < LeftData.X)
                    erg *= -1;
                erg *= 1.0f / erg.Length();
                return erg;
            }
        }

        public float Length
        {
            get
            {
                if (!VVertexA.HasValue || !VVertexB.HasValue)
                    throw new InvalidOperationException("Cannot get Length from edge which is not completely initialised");

                if (IsPartlyInfinite)
                    return float.PositiveInfinity;
                return Vector2.Distance(VVertexA.Value, VVertexB.Value);
            }
        }
    }
}
