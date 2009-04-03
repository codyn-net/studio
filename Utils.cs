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
		
		public static int Max(IEnumerable<int> list)
		{
			return Select<int>(list, new SelectHandler<int>(Math.Max));
		}
		
		public static int Min(IEnumerable<int> list)
		{
			return Select<int>(list, new SelectHandler<int>(Math.Min));
		}
		
		public static Rectangle RectRegion(System.Drawing.Rectangle rect)
		{
			if (rect.Width < rect.X)
			{
				int tmp = rect.Width;
				rect.Width = rect.X;
				rect.X = tmp;
			}
			
			if (rect.Height < rect.Y)
			{
				int tmp = rect.Height;
				rect.Height = rect.Y;
				rect.Y = tmp;
			}
			
			return new Rectangle(rect.X, rect.Y, rect.Width - rect.X, rect.Height - rect.Y);
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
