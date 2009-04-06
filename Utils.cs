using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio
{
	public class Utils
	{
		public delegate T SelectHandler<T>(T first, T second);
		
		public static T Select<T>(IEnumerable<T> list, SelectHandler<T> handler)
		{
			bool hasitem = false;
			T best = default(T);
			
			foreach (T item in list)
			{
				if (!hasitem)
					best = item;
				else
					best = handler(best, item);
			}
			
			return best;
		}
		
		public static float Max(IEnumerable<float> list)
		{
			return Select<float>(list, new SelectHandler<float>(Math.Max));
		}
		
		public static float Min(IEnumerable<float> list)
		{
			return Select<float>(list, new SelectHandler<float>(Math.Min));
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
		
		public static float TransformScale(Matrix matrix)
		{
			return matrix.Elements[0];
		}
	}
}
