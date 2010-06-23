using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio
{
	public class Utils
	{
		public delegate double SelectHandlerDouble(double first, double second);
		public delegate float SelectHandlerFloat(float first, float second);
		public delegate int SelectHandlerInt(int first, int second);

		public static double Select(IEnumerable<double> list, SelectHandlerDouble handler)
		{
			bool hasitem = false;
			double best = default(double);
			
			foreach (double item in list)
			{
				if (!hasitem)
					best = item;
				else
					best = handler(best, item);
				
				hasitem = true;
			}
			
			return best;
		}
			
		public static float Select(IEnumerable<float> list, SelectHandlerFloat handler)
		{
			bool hasitem = false;
			float best = default(float);
			
			foreach (float item in list)
			{
				if (!hasitem)
					best = item;
				else
					best = handler(best, item);
				
				hasitem = true;
			}
			
			return best;
		}
				
		public static int Select(IEnumerable<int> list, SelectHandlerInt handler)
		{
			bool hasitem = false;
			int best = default(int);
			
			foreach (int item in list)
			{
				if (!hasitem)
					best = item;
				else
					best = handler(best, item);
				
				hasitem = true;
			}
			
			return best;
		}
		
		public static double Max(IEnumerable<double> list)
		{
			return Select(list, new SelectHandlerDouble(Math.Max));
		}
		
		public static float Max(IEnumerable<float> list)
		{
			return Select(list, new SelectHandlerFloat(Math.Max));
		}
		
		public static int Max(IEnumerable<int> list)
		{
			return Select(list, new SelectHandlerInt(Math.Max));
		}
		
		public static double Min(IEnumerable<double> list)
		{
			return Select(list, new SelectHandlerDouble(Math.Min));
		}
		
		public static float Min(IEnumerable<float> list)
		{
			return Select(list, new SelectHandlerFloat(Math.Min));
		}
		
		public static int Min(IEnumerable<int> list)
		{
			return Select(list, new SelectHandlerInt(Math.Min));
		}
		
		public static string Capitalize(string s)
		{
			if (s == String.Empty)
				return s;
			else
				return s.Substring(0, 1).ToUpper() + s.Substring(1);
		}
		
		public static int SortProperties(string first, string second)
		{
			if (first == "id")
				return -1;
			else if (second == "id")
				return 1;
			else
				return first.ToLower().CompareTo(second.ToLower());
		}
		
		public static bool In(object val, ICollection collection)
		{
			if (val == null)
				return false;

			foreach (object t in collection)
			{
				if (t != null && t.Equals(val))
					return true;
			}
			
			return false;
		}
		
		public static void MeanPosition(ICollection<Wrappers.Wrapper> objects, out double x, out double y)
		{
			x = 0;
			y = 0;
			int num = 0;
			
			foreach (Wrappers.Wrapper obj in objects)
			{
				if (obj is Wrappers.Link)
					continue;

				x += obj.Allocation.X + obj.Allocation.Width / 2.0f;
				y += obj.Allocation.Y + obj.Allocation.Height / 2.0f;
				
				num += 1;
			}
			
			if (num != 0)
			{
				x = x / num;
				y = y / num;
			}
		}
	}
}
