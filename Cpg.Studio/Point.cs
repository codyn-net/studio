using System;

namespace Cpg.Studio
{
	public class Point
	{
		public double X;
		public double Y;
		
		public Point(double x, double y)
		{
			X = x;
			Y = y;
		}
		
		public Point() : this(0, 0)
		{
		}
		
		public override string ToString()
		{
			return String.Format("[Point: x = {0}, y = {1}]", X, Y);
		}
		
		public static bool operator==(Point a, Point b)
		{
			return a.X == b.X && a.Y == b.Y;
		}
		
		public static bool operator!=(Point a, Point b)
		{
			return !(a == b);
		}
		
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}

