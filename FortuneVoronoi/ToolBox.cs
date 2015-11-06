using System.Numerics;

namespace FortuneVoronoi
{
	internal static class MathTools
	{
	    public static int ccw(Vector2 p0, Vector2 p1, Vector2 p2, bool plusOneOnZeroDegrees)
	    {
	        var d1 = p1 - p0;
	        var d2 = p2 - p0;

	        if (d1.X * d2.Y > d1.Y * d2.X)
	            return +1;

	        if (d1.X * d2.Y < d1.Y * d2.X)
	            return -1;

	        if ((d1.X * d2.X < 0) || (d1.Y * d2.Y < 0))
	            return -1;

	        if (d1.X * d1.X + d1.Y * d1.Y < d2.X * d2.X + d2.Y * d2.Y && plusOneOnZeroDegrees)
	            return +1;

	        return 0;
	    }
	}
}