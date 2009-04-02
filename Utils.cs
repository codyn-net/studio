using System;
using System.Collections.Generic;

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
	}
}
