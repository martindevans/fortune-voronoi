using System;
using System.Numerics;

namespace FortuneVoronoi
{
    internal abstract class VNode
    {
        private VNode _left;
        private VNode _right;

        public VNode Left
        {
            get { return _left; }
            set
            {
                _left = value;
                value.Parent = this;
            }
        }

        public VNode Right
        {
            get { return _right; }
            set
            {
                _right = value;
                value.Parent = this;
            }
        }

        public VNode Parent { get; set; }

        public void Replace(VNode childOld, VNode childNew)
        {
            if (Left == childOld)
                Left = childNew;
            else if (Right == childOld)
                Right = childNew;
            else
                throw new ArgumentException("Child not found!", nameof(childOld));
            childOld.Parent = null;
        }

        public static VDataNode FirstDataNode(VNode root)
        {
            var c = root;
            while (c.Left != null)
                c = c.Left;
            return (VDataNode)c;
        }

        public static VDataNode LeftDataNode(VDataNode current)
        {
            VNode c = current;

            //1. Up
            do
            {
                if (c.Parent == null)
                    return null;
                if (c.Parent.Left == c)
                {
                    c = c.Parent;
                }
                else
                {
                    c = c.Parent;
                    break;
                }
            } while (true);

            //2. One Left
            c = c.Left;

            //3. Down
            while (c.Right != null)
                c = c.Right;
            return (VDataNode)c; // Cast statt 'as' damit eine Exception kommt
        }
        public static VDataNode RightDataNode(VDataNode current)
        {
            VNode c = current;

            //1. Up
            do
            {
                if (c.Parent == null)
                    return null;
                if (c.Parent.Right == c)
                {
                    c = c.Parent;
                }
                else
                {
                    c = c.Parent;
                    break;
                }
            } while (true);

            //2. One Right
            c = c.Right;

            //3. Down
            while (c.Left != null)
                c = c.Left;
            return (VDataNode)c; // Cast statt 'as' damit eine Exception kommt
        }

        public static VEdgeNode EdgeToRightDataNode(VDataNode current)
        {
            VNode c = current;
            //1. Up
            do
            {
                if (c.Parent == null)
                    throw new Exception("No Left Leaf found!");
                if (c.Parent.Right == c)
                {
                    c = c.Parent;
                }
                else
                {
                    c = c.Parent;
                    break;
                }
            } while (true);
            return (VEdgeNode)c;
        }

        public static VDataNode FindDataNode(VNode root, double ys, double x)
        {
            var c = root;
            do
            {
                var node = c as VDataNode;
                if (node != null)
                    return node;

                c = ((VEdgeNode)c).Cut(ys, x) < 0 ? c.Left : c.Right;
            } while (true);
        }

        /// <summary>
        /// Will return the new root (unchanged except in start-up)
        /// </summary>
        public static VNode ProcessDataEvent(VDataEvent e, VNode root, VoronoiGraph vg, double ys, out VDataNode[] circleCheckList)
        {
            if (root == null)
            {
                root = new VDataNode(e.DataPoint);
                circleCheckList = new VDataNode[] { (VDataNode)root };
                return root;
            }
            //1. Find the node to be replaced
            VNode c = FindDataNode(root, ys, e.DataPoint.X);
            //2. Create the subtree (ONE Edge, but two VEdgeNodes)
            var ve = new VoronoiEdge
            {
                LeftData = ((VDataNode)c).DataPoint,
                RightData = e.DataPoint,
                VVertexA = null,
                VVertexB = null
            };
            vg.MutableEdges.Add(ve);

            VNode subRoot;
            if (Math.Abs(ve.LeftData.Y - ve.RightData.Y) < 1e-10)
            {
                if (ve.LeftData.X < ve.RightData.X)
                {
                    subRoot = new VEdgeNode(ve, false)
                    {
                        Left = new VDataNode(ve.LeftData),
                        Right = new VDataNode(ve.RightData)
                    };
                }
                else
                {
                    subRoot = new VEdgeNode(ve, true)
                    {
                        Left = new VDataNode(ve.RightData),
                        Right = new VDataNode(ve.LeftData)
                    };
                }
                circleCheckList = new VDataNode[] { (VDataNode)subRoot.Left, (VDataNode)subRoot.Right };
            }
            else
            {
                subRoot = new VEdgeNode(ve, false)
                {
                    Left = new VDataNode(ve.LeftData),
                    Right = new VEdgeNode(ve, true)
                    {
                        Left = new VDataNode(ve.RightData),
                        Right = new VDataNode(ve.LeftData)
                    }
                };
                circleCheckList = new VDataNode[] { (VDataNode)subRoot.Left, (VDataNode)subRoot.Right.Left, (VDataNode)subRoot.Right.Right };
            }

            //3. Apply subtree
            if (c.Parent == null)
                return subRoot;
            c.Parent.Replace(c, subRoot);
            return root;
        }

        public static VNode ProcessCircleEvent(VCircleEvent e, VNode root, VoronoiGraph vg, out VDataNode[] circleCheckList)
        {
            VEdgeNode eo;
            var b = e.NodeN;
            var a = LeftDataNode(b);
            var c = RightDataNode(b);
            if (a == null || b.Parent == null || c == null || !a.DataPoint.Equals(e.NodeL.DataPoint) || !c.DataPoint.Equals(e.NodeR.DataPoint))
            {
                circleCheckList = new VDataNode[] { };
                return root; // Abbruch da sich der Graph verändert hat
            }
            var eu = (VEdgeNode)b.Parent;
            circleCheckList = new VDataNode[] { a, c };
            //1. Create the new Vertex
            var vNew = new Vector2(e.Center.X, e.Center.Y);
            //			VNew.X = Fortune.ParabolicCut(a.DataPoint.X,a.DataPoint.Y,c.DataPoint.X,c.DataPoint.Y,ys);
            //			VNew.Y = (ys + a.DataPoint.Y)/2 - 1/(2*(ys-a.DataPoint.Y))*(VNew.X-a.DataPoint.X)*(VNew.X-a.DataPoint.X);
            vg.MutableVertices.Add(vNew);
            //2. Find out if a or c are in a distand part of the tree (the other is then b's sibling) and assign the new vertex
            if (eu.Left == b) // c is sibling
            {
                eo = EdgeToRightDataNode(a);

                // replace eu by eu's Right
                eu.Parent.Replace(eu, eu.Right);
            }
            else // a is sibling
            {
                eo = EdgeToRightDataNode(b);

                // replace eu by eu's Left
                eu.Parent.Replace(eu, eu.Left);
            }
            eu.Edge.AddVertex(vNew);
            //			///////////////////// uncertain
            //			if(eo==eu)
            //				return root;
            //			/////////////////////

            //complete & cleanup eo
            eo.Edge.AddVertex(vNew);
            //while(eo.Edge.VVertexB == Fortune.VVUnkown)
            //{
            //    eo.flipped = !eo.flipped;
            //    eo.Edge.AddVertex(Fortune.VVInfinite);
            //}
            //if(eo.flipped)
            //{
            //    Vector T = eo.Edge.LeftData;
            //    eo.Edge.LeftData = eo.Edge.RightData;
            //    eo.Edge.RightData = T;
            //}


            //2. Replace eo by new Edge
            var ve = new VoronoiEdge
            {
                LeftData = a.DataPoint,
                RightData = c.DataPoint
            };
            ve.AddVertex(vNew);
            vg.MutableEdges.Add(ve);

            var ven = new VEdgeNode(ve, false)
            {
                Left = eo.Left,
                Right = eo.Right
            };
            if (eo.Parent == null)
                return ven;
            eo.Parent.Replace(eo, ven);
            return root;
        }
        public static VCircleEvent CircleCheckDataNode(VDataNode n, double ys)
        {
            var l = LeftDataNode(n);
            var r = RightDataNode(n);
            if (l == null || r == null || l.DataPoint == r.DataPoint || l.DataPoint == n.DataPoint || n.DataPoint == r.DataPoint)
                return null;
            if (MathTools.ccw(l.DataPoint, n.DataPoint, r.DataPoint, false) <= 0)
                return null;
            var center = Fortune.CircumCircleCenter(l.DataPoint, n.DataPoint, r.DataPoint);
            var vc = new VCircleEvent
            {
                NodeN = n,
                NodeL = l,
                NodeR = r,
                Center = center,
                Valid = true
            };
            if (vc.Y > ys || Math.Abs(vc.Y - ys) < 1e-10)
                return vc;
            return null;
        }

        public static void CleanUpTree(VEdgeNode ve)
        {
            if (ve == null)
                return;

            while (!ve.Edge.VVertexB.HasValue)
            {
                ve.Edge.AddVertex(Fortune.VvInfinite);
            }
            if (ve.Flipped)
            {
                var t = ve.Edge.LeftData;
                ve.Edge.LeftData = ve.Edge.RightData;
                ve.Edge.RightData = t;
            }
            ve.Edge.Done = true;
            CleanUpTree(ve.Left as VEdgeNode);
            CleanUpTree(ve.Right as VEdgeNode);
        }
    }

    internal class VDataNode : VNode
    {
        public VDataNode(Vector2 dp)
        {
            DataPoint = dp;
        }
        public Vector2 DataPoint;
    }

    internal class VEdgeNode : VNode
    {
        public VEdgeNode(VoronoiEdge e, bool flipped)
        {
            Edge = e;
            Flipped = flipped;
        }
        public readonly VoronoiEdge Edge;
        public readonly bool Flipped;
        public double Cut(double ys, double x)
        {
            if (!Flipped)
                return Math.Round(x - Fortune.ParabolicCut(Edge.LeftData.X, Edge.LeftData.Y, Edge.RightData.X, Edge.RightData.Y, ys), 10);
            return Math.Round(x - Fortune.ParabolicCut(Edge.RightData.X, Edge.RightData.Y, Edge.LeftData.X, Edge.LeftData.Y, ys), 10);
        }
    }
}
