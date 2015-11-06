using System;
using System.Collections.Generic;
using System.Numerics;

namespace FortuneVoronoi
{
	public class VoronoiGraph
	{
		public readonly HashSet<Vector2> Vertices = new HashSet<Vector2>();
		public readonly HashSet<VoronoiEdge> Edges = new HashSet<VoronoiEdge>();
	}

	public class VoronoiEdge
	{
		internal bool Done;
		public Vector2 RightData, LeftData;
	    public Vector2? VVertexA;
        public Vector2? VVertexB;

	    public void AddVertex(Vector2 v)
	    {
	        if (!VVertexA.HasValue)
	            VVertexA = v;
	        else if (!VVertexB.HasValue)
	            VVertexB = v;
	        else
	            throw new Exception("Tried to add third vertex!");
	    }

	    public bool IsInfinite
		{
			get { return VVertexA == Fortune.VVInfinite && VVertexB == Fortune.VVInfinite; }
		}
		public bool IsPartlyInfinite
		{
			get { return VVertexA == Fortune.VVInfinite || VVertexB == Fortune.VVInfinite; }
		}
		public Vector2 FixedPoint
		{
			get
			{
				if(IsInfinite)
					return 0.5f * (LeftData + RightData);
				if(VVertexA != Fortune.VVInfinite)
					return VVertexA.Value;
				return VVertexB.Value;
			}
		}
		public Vector2 DirectionVector
		{
			get
			{
			    if (!IsPartlyInfinite)
			        return (VVertexB.Value - VVertexA.Value) * (1.0f / (float)Math.Sqrt(Vector2.Distance(VVertexA.Value, VVertexB.Value)));
				if(LeftData.X == RightData.X)
				{
				    if (LeftData.Y < RightData.Y)
				        return new Vector2(-1, 0);
				    return new Vector2(1, 0);
				}
                var erg = new Vector2(-(RightData.Y-LeftData.Y)/(RightData.X-LeftData.X),1);
			    if (RightData.X < LeftData.X)
			        erg *= -1;
			    erg *= (1.0f / erg.Length());
				return erg;
			}
		}
		public double Length
		{
			get
			{
				if(IsPartlyInfinite)
					return double.PositiveInfinity;
				return Math.Sqrt(Vector2.Distance(VVertexA.Value,VVertexB.Value));
			}
		}
	}
	
	// VoronoiVertex or VoronoiDataPoint are represented as Vector

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
			if(Left==childOld)
				Left = childNew;
			else if(Right==childOld)
				Right = childNew;
			else
                throw new ArgumentException("Child not found!", nameof(childOld));
			childOld.Parent = null;
		}

		public static VDataNode FirstDataNode(VNode root)
		{
			var c = root;
			while(c.Left!=null)
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
			while(c.Right!=null)
				c = c.Right;
			return (VDataNode)c; // Cast statt 'as' damit eine Exception kommt
		}
		public static VDataNode RightDataNode(VDataNode current)
		{
			VNode c = current;

			//1. Up
			do
			{
				if(c.Parent==null)
					return null;
				if(c.Parent.Right == c)
				{
					c = c.Parent;
				}
				else
				{
					c = c.Parent;
					break;
				}
			}while(true);

			//2. One Right
			c = c.Right;

			//3. Down
			while(c.Left!=null)
				c = c.Left;
			return (VDataNode)c; // Cast statt 'as' damit eine Exception kommt
		}

		public static VEdgeNode EdgeToRightDataNode(VDataNode current)
		{
			VNode c = current;
			//1. Up
			do
			{
				if(c.Parent==null)
					throw new Exception("No Left Leaf found!");
				if(c.Parent.Right == c)
				{
					c = c.Parent;
				}
				else
				{
					c = c.Parent;
					break;
				}
			} while(true);
			return (VEdgeNode)c;
		}

		public static VDataNode FindDataNode(VNode root, double ys, double x)
		{
			var c = root;
			do
			{
				if(c is VDataNode)
					return (VDataNode)c;

				if(((VEdgeNode)c).Cut(ys,x)<0)
					c = c.Left;
				else
					c = c.Right;
			}while(true);
		}

		/// <summary>
		/// Will return the new root (unchanged except in start-up)
		/// </summary>
		public static VNode ProcessDataEvent(VDataEvent e, VNode root, VoronoiGraph vg, double ys, out VDataNode[] circleCheckList)
		{
			if(root==null)
			{
				root = new VDataNode(e.DataPoint);
				circleCheckList = new VDataNode[] {(VDataNode)root};
				return root;
			}
			//1. Find the node to be replaced
			VNode c = FindDataNode(root, ys, e.DataPoint.X);
			//2. Create the subtree (ONE Edge, but two VEdgeNodes)
		    VoronoiEdge ve = new VoronoiEdge {
		        LeftData = ((VDataNode)c).DataPoint,
		        RightData = e.DataPoint,
		        VVertexA = null,
		        VVertexB = null
            };
		    vg.Edges.Add(ve);

			VNode subRoot;
			if(Math.Abs(ve.LeftData.Y-ve.RightData.Y)<1e-10)
			{
				if(ve.LeftData.X<ve.RightData.X)
				{
					subRoot = new VEdgeNode(ve,false);
					subRoot.Left = new VDataNode(ve.LeftData);
					subRoot.Right = new VDataNode(ve.RightData);
				}
				else
				{
					subRoot = new VEdgeNode(ve,true);
					subRoot.Left = new VDataNode(ve.RightData);
					subRoot.Right = new VDataNode(ve.LeftData);
				}
				circleCheckList = new VDataNode[] {(VDataNode)subRoot.Left,(VDataNode)subRoot.Right};
			}
			else
			{
			    subRoot = new VEdgeNode(ve, false) {
			        Left = new VDataNode(ve.LeftData),
			        Right = new VEdgeNode(ve, true) {
			            Left = new VDataNode(ve.RightData),
			            Right = new VDataNode(ve.LeftData)
			        }
			    };
			    circleCheckList = new VDataNode[] {(VDataNode)subRoot.Left,(VDataNode)subRoot.Right.Left,(VDataNode)subRoot.Right.Right};
			}

			//3. Apply subtree
			if(c.Parent == null)
				return subRoot;
			c.Parent.Replace(c,subRoot);
			return root;
		}

		public static VNode ProcessCircleEvent(VCircleEvent e, VNode root, VoronoiGraph vg, double ys, out VDataNode[] circleCheckList)
		{
		    VEdgeNode eo;
			var b = e.NodeN;
			var a = VNode.LeftDataNode(b);
			var c = VNode.RightDataNode(b);
			if(a==null || b.Parent==null || c==null || !a.DataPoint.Equals(e.NodeL.DataPoint) || !c.DataPoint.Equals(e.NodeR.DataPoint))
			{
				circleCheckList = new VDataNode[]{};
				return root; // Abbruch da sich der Graph verändert hat
			}
			var eu = (VEdgeNode)b.Parent;
			circleCheckList = new VDataNode[] {a,c};
			//1. Create the new Vertex
		    var vNew = new Vector2(e.Center.X, e.Center.Y);
//			VNew.X = Fortune.ParabolicCut(a.DataPoint.X,a.DataPoint.Y,c.DataPoint.X,c.DataPoint.Y,ys);
//			VNew.Y = (ys + a.DataPoint.Y)/2 - 1/(2*(ys-a.DataPoint.Y))*(VNew.X-a.DataPoint.X)*(VNew.X-a.DataPoint.X);
			vg.Vertices.Add(vNew);
			//2. Find out if a or c are in a distand part of the tree (the other is then b's sibling) and assign the new vertex
			if(eu.Left==b) // c is sibling
			{
				eo = VNode.EdgeToRightDataNode(a);

				// replace eu by eu's Right
				eu.Parent.Replace(eu,eu.Right);
			}
			else // a is sibling
			{
				eo = VNode.EdgeToRightDataNode(b);

				// replace eu by eu's Left
				eu.Parent.Replace(eu,eu.Left);
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
		    var ve = new VoronoiEdge {
		        LeftData = a.DataPoint,
		        RightData = c.DataPoint
		    };
		    ve.AddVertex(vNew);
			vg.Edges.Add(ve);

		    var ven = new VEdgeNode(ve, false) {
		        Left = eo.Left,
		        Right = eo.Right
		    };
		    if(eo.Parent == null)
				return ven;
			eo.Parent.Replace(eo,ven);
			return root;
		}
		public static VCircleEvent CircleCheckDataNode(VDataNode n, double ys)
		{
			var l = LeftDataNode(n);
			var r = RightDataNode(n);
			if(l==null || r==null || l.DataPoint==r.DataPoint || l.DataPoint==n.DataPoint || n.DataPoint==r.DataPoint)
				return null;
		    if (MathTools.ccw(l.DataPoint, n.DataPoint, r.DataPoint, false) <= 0)
		        return null;
			var center = Fortune.CircumCircleCenter(l.DataPoint,n.DataPoint,r.DataPoint);
		    var vc = new VCircleEvent {
		        NodeN = n,
		        NodeL = l,
		        NodeR = r,
		        Center = center,
		        Valid = true
		    };
		    if(vc.Y>ys || Math.Abs(vc.Y - ys) < 1e-10)
				return vc;
			return null;
		}

		public static void CleanUpTree(VNode root)
		{
			if(root is VDataNode)
				return;
			var ve = root as VEdgeNode;
			while(!ve.Edge.VVertexB.HasValue)
			{
				ve.Edge.AddVertex(Fortune.VVInfinite);
//				VE.flipped = !VE.flipped;
			}
			if(ve.Flipped)
			{
				var t = ve.Edge.LeftData;
				ve.Edge.LeftData = ve.Edge.RightData;
				ve.Edge.RightData = t;
			}
			ve.Edge.Done = true;
			CleanUpTree(root.Left);
			CleanUpTree(root.Right);
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
			if(!Flipped)
				return Math.Round(x-Fortune.ParabolicCut(Edge.LeftData.X, Edge.LeftData.Y, Edge.RightData.X, Edge.RightData.Y, ys),10);
			return Math.Round(x-Fortune.ParabolicCut(Edge.RightData.X, Edge.RightData.Y, Edge.LeftData.X, Edge.LeftData.Y, ys),10);
		}
	}


	internal abstract class VEvent : IComparable
	{
		public abstract double Y {get;}
		public abstract double X {get;}
		#region IComparable Members

		public int CompareTo(object obj)
		{
			if(!(obj is VEvent))
				throw new ArgumentException("obj not VEvent!");
			int i = Y.CompareTo(((VEvent)obj).Y);
			if(i!=0)
				return i;
			return X.CompareTo(((VEvent)obj).X);
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

		public override double X
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

		public override double X
		{
			get
			{
				return Center.X;
			}
		}

		public bool Valid = true;
	}

	public abstract class Fortune
	{
		public static readonly Vector2 VVInfinite = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		public static readonly Vector2 VVUnknown = new Vector2(float.NaN, float.NaN);
		internal static double ParabolicCut(double x1, double y1, double x2, double y2, double ys)
		{
//			y1=-y1;
//			y2=-y2;
//			ys=-ys;
//			
			if(Math.Abs(x1-x2)<1e-10 && Math.Abs(y1-y2)<1e-10)
			{
//				if(y1>y2)
//					return double.PositiveInfinity;
//				if(y1<y2)
//					return double.NegativeInfinity;
//				return x;
				throw new Exception("Identical datapoints are not allowed!");
			}

			if(Math.Abs(y1-ys)<1e-10 && Math.Abs(y2-ys)<1e-10)
				return (x1+x2)/2;
			if(Math.Abs(y1-ys)<1e-10)
				return x1;
			if(Math.Abs(y2-ys)<1e-10)
				return x2;
			double a1 = 1/(2*(y1-ys));
			double a2 = 1/(2*(y2-ys));
			if(Math.Abs(a1-a2)<1e-10)
				return (x1+x2)/2;
			double xs1 = 0.5/(2*a1-2*a2)*(4*a1*x1-4*a2*x2+2*Math.Sqrt(-8*a1*x1*a2*x2-2*a1*y1+2*a1*y2+4*a1*a2*x2*x2+2*a2*y1+4*a2*a1*x1*x1-2*a2*y2));
			double xs2 = 0.5/(2*a1-2*a2)*(4*a1*x1-4*a2*x2-2*Math.Sqrt(-8*a1*x1*a2*x2-2*a1*y1+2*a1*y2+4*a1*a2*x2*x2+2*a2*y1+4*a2*a1*x1*x1-2*a2*y2));
			xs1=Math.Round(xs1,10);
			xs2=Math.Round(xs2,10);
			if(xs1>xs2)
			{
				double h = xs1;
				xs1=xs2;
				xs2=h;
			}
			if(y1>=y2)
				return xs2;
			return xs1;
		}
		internal static Vector2 CircumCircleCenter(Vector2 A, Vector2 B, Vector2 C)
		{
			if(A==B || B==C || A==C)
				throw new Exception("Need three different points!");
			double tx = (A.X + C.X)/2;
			double ty = (A.Y + C.Y)/2;

			double vx = (B.X + C.X)/2;
			double vy = (B.Y + C.Y)/2;

			double ux,uy,wx,wy;
			
			if(A.X == C.X)
			{
				ux = 1;
				uy = 0;
			}
			else
			{
				ux = (C.Y - A.Y)/(A.X - C.X);
				uy = 1;
			}

			if(B.X == C.X)
			{
				wx = -1;
				wy = 0;
			}
			else
			{
				wx = (B.Y - C.Y)/(B.X - C.X);
				wy = -1;
			}

			double alpha = (wy*(vx-tx)-wx*(vy - ty))/(ux*wy-wx*uy);

		    return new Vector2((float)(tx + alpha * ux), (float)(ty + alpha * uy));
		}	
		public static VoronoiGraph ComputeVoronoiGraph(IEnumerable<Vector2> datapoints)
		{
			var pq = new BinaryPriorityQueue<VEvent>();
			var currentCircles = new Dictionary<VDataNode, VCircleEvent>();
			var vg = new VoronoiGraph();
			VNode rootNode = null;
			foreach(Vector2 v in datapoints)
			{
				pq.Push(new VDataEvent(v));
			}
			while(pq.Count>0)
			{
				var ve = pq.Pop() as VEvent;
				VDataNode[] circleCheckList;
				if(ve is VDataEvent)
				{
					rootNode = VNode.ProcessDataEvent(ve as VDataEvent,rootNode,vg,ve.Y,out circleCheckList);
				}
				else if(ve is VCircleEvent)
				{
					currentCircles.Remove(((VCircleEvent)ve).NodeN);
					if(!((VCircleEvent)ve).Valid)
						continue;
					rootNode = VNode.ProcessCircleEvent(ve as VCircleEvent,rootNode,vg,ve.Y,out circleCheckList);
				}
				else throw new Exception("Got event of type "+ve.GetType()+"!");
				foreach(var vd in circleCheckList)
				{
					if(currentCircles.ContainsKey(vd))
					{
						((VCircleEvent)currentCircles[vd]).Valid=false;
						currentCircles.Remove(vd);
					}
					var vce = VNode.CircleCheckDataNode(vd,ve.Y);
					if(vce!=null)
					{
						pq.Push(vce);
						currentCircles[vd]=vce;
					}
				}
				if(ve is VDataEvent)
				{
					var dp = ((VDataEvent)ve).DataPoint;
				    foreach (var vce in currentCircles.Values)
				    {
				        if (Vector2.Distance(dp, vce.Center) < vce.Y - vce.Center.Y && Math.Abs(Vector2.Distance(dp, vce.Center) - (vce.Y - vce.Center.Y)) > 1e-10)
				            vce.Valid = false;
				    }
				}
			}
			VNode.CleanUpTree(rootNode);
			foreach(var ve in vg.Edges)
			{
				if(ve.Done)
					continue;
				if(!ve.VVertexB.HasValue)
				{
					ve.AddVertex(VVInfinite);
					if(Math.Abs(ve.LeftData.Y-ve.RightData.Y)<1e-10 && ve.LeftData.X<ve.RightData.X)
					{
						Vector2 T = ve.LeftData;
						ve.LeftData = ve.RightData;
						ve.RightData = T;
					}
				}
			}
			
			var minuteEdges = new List<VoronoiEdge>();
			foreach(var ve in vg.Edges)
			{
				if(!ve.IsPartlyInfinite && ve.VVertexA.Equals(ve.VVertexB))
				{
					minuteEdges.Add(ve);
					// prevent rounding errors from expanding to holes
					foreach(VoronoiEdge ve2 in vg.Edges)
					{
						if(ve2.VVertexA.Equals(ve.VVertexA))
							ve2.VVertexA = ve.VVertexA;
						if(ve2.VVertexB.Equals(ve.VVertexA))
							ve2.VVertexB = ve.VVertexA;
					}
				}
			}
			foreach(var ve in minuteEdges)
				vg.Edges.Remove(ve);

			return vg;
		}
		public static VoronoiGraph FilterVG(VoronoiGraph vg, double minLeftRightDist)
		{
			var vgErg = new VoronoiGraph();
			foreach(var ve in vg.Edges)
			{
				if(Math.Sqrt(Vector2.Distance(ve.LeftData,ve.RightData))>=minLeftRightDist)
					vgErg.Edges.Add(ve);
			}
			foreach(var ve in vgErg.Edges)
			{
				vgErg.Vertices.Add(ve.VVertexA.Value);
				vgErg.Vertices.Add(ve.VVertexB.Value);
			}
			return vgErg;
		}
	}
}
