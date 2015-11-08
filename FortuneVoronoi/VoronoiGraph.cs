using System.Collections.Generic;
using System.Numerics;

namespace FortuneVoronoi
{
    public interface IGraph
    {
        IReadOnlyCollection<Vector2> Vertices { get; }

        IReadOnlyCollection<Edge> Edges { get; }
    }

    public class VoronoiGraph
        : IGraph
    {
        internal readonly HashSet<Vector2> MutableVertices = new HashSet<Vector2>();
        public IReadOnlyCollection<Vector2> Vertices
        {
            get { return MutableVertices; }
        }

        internal readonly HashSet<Edge> MutableEdges = new HashSet<Edge>();
        public IReadOnlyCollection<Edge> Edges
        {
            get { return MutableEdges; }
        }
    }
}
