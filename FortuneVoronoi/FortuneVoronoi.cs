using System;
using System.Collections.Generic;
using System.Numerics;
using HandyCollections.Heap;

namespace FortuneVoronoi
{
    public static class Fortune
    {
        internal static readonly Vector2 VvInfinite = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        internal static double ParabolicCut(double x1, double y1, double x2, double y2, double ys)
        {
            if (Math.Abs(x1 - x2) < 1e-10 && Math.Abs(y1 - y2) < 1e-10)
                throw new Exception("Identical datapoints are not allowed!");

            if (Math.Abs(y1 - ys) < 1e-10 && Math.Abs(y2 - ys) < 1e-10)
                return (x1 + x2) / 2;
            if (Math.Abs(y1 - ys) < 1e-10)
                return x1;
            if (Math.Abs(y2 - ys) < 1e-10)
                return x2;
            var a1 = 1 / (2 * (y1 - ys));
            var a2 = 1 / (2 * (y2 - ys));
            if (Math.Abs(a1 - a2) < 1e-10)
                return (x1 + x2) / 2;
            var xs1 = 0.5 / (2 * a1 - 2 * a2) * (4 * a1 * x1 - 4 * a2 * x2 + 2 * Math.Sqrt(-8 * a1 * x1 * a2 * x2 - 2 * a1 * y1 + 2 * a1 * y2 + 4 * a1 * a2 * x2 * x2 + 2 * a2 * y1 + 4 * a2 * a1 * x1 * x1 - 2 * a2 * y2));
            var xs2 = 0.5 / (2 * a1 - 2 * a2) * (4 * a1 * x1 - 4 * a2 * x2 - 2 * Math.Sqrt(-8 * a1 * x1 * a2 * x2 - 2 * a1 * y1 + 2 * a1 * y2 + 4 * a1 * a2 * x2 * x2 + 2 * a2 * y1 + 4 * a2 * a1 * x1 * x1 - 2 * a2 * y2));
            xs1 = Math.Round(xs1, 10);
            xs2 = Math.Round(xs2, 10);
            if (xs1 > xs2)
            {
                var h = xs1;
                xs1 = xs2;
                xs2 = h;
            }
            if (y1 >= y2)
                return xs2;
            return xs1;
        }

        internal static Vector2 CircumCircleCenter(Vector2 a, Vector2 b, Vector2 c)
        {
            if (a == b || b == c || a == c)
                throw new Exception("Need three different points!");

            var tx = (a.X + c.X) / 2;
            var ty = (a.Y + c.Y) / 2;

            var vx = (b.X + c.X) / 2;
            var vy = (b.Y + c.Y) / 2;

            float ux, uy, wx, wy;

            if (a.X == c.X)
            {
                ux = 1;
                uy = 0;
            }
            else
            {
                ux = (c.Y - a.Y) / (a.X - c.X);
                uy = 1;
            }

            if (b.X == c.X)
            {
                wx = -1;
                wy = 0;
            }
            else
            {
                wx = (b.Y - c.Y) / (b.X - c.X);
                wy = -1;
            }

            var alpha = (wy * (vx - tx) - wx * (vy - ty)) / (ux * wy - wx * uy);

            return new Vector2(tx + alpha * ux, ty + alpha * uy);
        }

        public static VoronoiGraph ComputeVoronoiGraph(IEnumerable<Vector2> points)
        {
            var pq = new MinHeap<VEvent>();
            var currentCircles = new Dictionary<VDataNode, VCircleEvent>();
            var vg = new VoronoiGraph();
            VNode rootNode = null;
            foreach (var v in points)
            {
                pq.Add(new VDataEvent(v));
            }
            while (pq.Count > 0)
            {
                var ve = pq.RemoveMin();
                VDataNode[] circleCheckList;
                if (ve is VDataEvent)
                {
                    rootNode = VNode.ProcessDataEvent(ve as VDataEvent, rootNode, vg, ve.Y, out circleCheckList);
                }
                else if (ve is VCircleEvent)
                {
                    currentCircles.Remove(((VCircleEvent)ve).NodeN);
                    if (!((VCircleEvent)ve).Valid)
                        continue;
                    rootNode = VNode.ProcessCircleEvent(ve as VCircleEvent, rootNode, vg, out circleCheckList);
                }
                else
                    throw new Exception("Got event of type " + ve.GetType() + "!");
                foreach (var vd in circleCheckList)
                {
                    if (currentCircles.ContainsKey(vd))
                    {
                        currentCircles[vd].Valid = false;
                        currentCircles.Remove(vd);
                    }
                    var vce = VNode.CircleCheckDataNode(vd, ve.Y);
                    if (vce != null)
                    {
                        pq.Add(vce);
                        currentCircles[vd] = vce;
                    }
                }

                var evt = ve as VDataEvent;
                if (evt != null)
                {
                    var dp = evt.DataPoint;
                    foreach (var vce in currentCircles.Values)
                    {
                        if (Vector2.Distance(dp, vce.Center) < vce.Y - vce.Center.Y && Math.Abs(Vector2.Distance(dp, vce.Center) - (vce.Y - vce.Center.Y)) > 1e-10)
                            vce.Valid = false;
                    }
                }
            }
            VNode.CleanUpTree(rootNode as VEdgeNode);
            foreach (var ve in vg.Edges)
            {
                if (ve.Done)
                    continue;
                if (!ve.VVertexB.HasValue)
                {
                    ve.AddVertex(VvInfinite);
                    if (Math.Abs(ve.LeftData.Y - ve.RightData.Y) < 1e-10 && ve.LeftData.X < ve.RightData.X)
                    {
                        var t = ve.LeftData;
                        ve.LeftData = ve.RightData;
                        ve.RightData = t;
                    }
                }
            }

            var minuteEdges = new List<Edge>();
            foreach (var ve in vg.Edges)
            {
                if (!ve.IsPartlyInfinite && ve.VVertexA.Equals(ve.VVertexB))
                {
                    minuteEdges.Add(ve);
                    // prevent rounding errors from expanding to holes
                    foreach (var ve2 in vg.Edges)
                    {
                        if (ve2.VVertexA.Equals(ve.VVertexA))
                            ve2.VVertexA = ve.VVertexA;
                        if (ve2.VVertexB.Equals(ve.VVertexA))
                            ve2.VVertexB = ve.VVertexA;
                    }
                }
            }
            foreach (var ve in minuteEdges)
                vg.MutableEdges.Remove(ve);

            return vg;
        }
    }
}