using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FortuneVoronoi.Test
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            var result = Fortune.ComputeVoronoiGraph(new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10),
            }.Reverse());


        }
    }
}
