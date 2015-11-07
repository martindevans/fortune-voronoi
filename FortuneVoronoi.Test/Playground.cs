using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FortuneVoronoi.Test
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            var shape = new Vector2[] {
                new Vector2(200, 100),
                new Vector2(200, -200),
                new Vector2(100, -200),
                new Vector2(100, -100),
                new Vector2(-100, -100),
                new Vector2(-100, 100),
            };

            var result = Fortune.ComputeVoronoiGraph(shape);

            //Display result
            StringBuilder pathVoronoiEdges = new StringBuilder();

            var min = new Vector2(float.MaxValue);
            foreach (var voronoiEdge in result.Edges)
            {
                min = Vector2.Min(voronoiEdge.LeftData, min);
                min = Vector2.Min(voronoiEdge.RightData, min);

                float length = voronoiEdge.Length;
                if (float.IsPositiveInfinity(length))
                    length = 1000;

                var b = voronoiEdge.FixedPoint + voronoiEdge.DirectionVector * length;
                pathVoronoiEdges.Append($"<path d=\"M {voronoiEdge.FixedPoint.X} {voronoiEdge.FixedPoint.Y} L {b.X} {b.Y} \" stroke=\"black\"></path>");
            }

            StringBuilder shapeData = new StringBuilder();
            shapeData.Append($"M {shape.First().X} {shape.First().Y}");
            foreach (var point in shape.Skip(1))
                shapeData.Append($"L {point.X} {point.Y} ");
            shapeData.Append("Z");

            Console.WriteLine(
                "<svg width=\"500px\" height=\"500px\"><g transform=\"translate({2} {3})\">{0}<path d=\"{1}\" fill=\"none\" stroke=\"green\"></path></g></svg>",
                pathVoronoiEdges,
                shapeData,
                -min.X + 1,
                -min.Y + 1
            );
        }
    }
}
