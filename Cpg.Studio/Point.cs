using System;

namespace Cpg.Studio
{
	public class Point
	{
		private static double Epsilon = 0.00000001;
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
		
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			
			Point other = obj as Point;
			
			if (other == null)
			{
				return false;
			}
			
			return Math.Abs(X - other.X) < Epsilon && Math.Abs(Y - other.Y) < Epsilon;
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		
		public double this[int idx]
		{
			get
			{
				return idx == 0 ? X : Y;
			}
			set
			{
				if (idx == 0)
				{
					X = value;
				}
				else
				{
					Y = value;
				}
			}
		}
	}
}

