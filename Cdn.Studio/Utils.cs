using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Biorob.Math;

namespace Cdn.Studio
{
	public class Utils
	{
		public delegate double SelectHandlerDouble(double first, double second);
		public delegate int SelectHandlerInt(int first, int second);

		public static double Select(IEnumerable<double> list, SelectHandlerDouble handler)
		{
			bool hasitem = false;
			double best = default(double);
			
			foreach (double item in list)
			{
				if (!hasitem)
				{
					best = item;
				}
				else
				{
					best = handler(best, item);
				}
				
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
				{
					best = item;
				}
				else
				{
					best = handler(best, item);
				}
				
				hasitem = true;
			}
			
			return best;
		}
		
		public static double Max(IEnumerable<double> list)
		{
			return Select(list, new SelectHandlerDouble(System.Math.Max));
		}
		
		public static int Max(IEnumerable<int> list)
		{
			return Select(list, new SelectHandlerInt(System.Math.Max));
		}
		
		public static double Min(IEnumerable<double> list)
		{
			return Select(list, new SelectHandlerDouble(System.Math.Min));
		}
		
		public static int Min(IEnumerable<int> list)
		{
			return Select(list, new SelectHandlerInt(System.Math.Min));
		}
		
		public static string Capitalize(string s)
		{
			if (s == String.Empty)
			{
				return s;
			}
			else
			{
				return s.Substring(0, 1).ToUpper() + s.Substring(1);
			}
		}
		
		public static int SortProperties(string first, string second)
		{
			if (first == "id")
			{
				return -1;
			}
			else if (second == "id")
			{
				return 1;
			}
			else
			{
				return first.ToLower().CompareTo(second.ToLower());
			}
		}
		
		public static bool In(object val, ICollection collection)
		{
			if (val == null)
			{
				return false;
			}

			foreach (object t in collection)
			{
				if (t != null && t.Equals(val))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public static Point MeanPosition(IEnumerable<Wrappers.Wrapper> objects)
		{
			Point ret = new Point(0, 0);
			int num = 0;
			
			foreach (Wrappers.Wrapper obj in objects)
			{
				Wrappers.Link link = obj as Wrappers.Link;
				
				if (link != null && (link.From != null || link.To != null))
				{
					continue;
				}

				ret.X += obj.Allocation.X + obj.Allocation.Width / 2.0;
				ret.Y += obj.Allocation.Y + obj.Allocation.Height / 2.0;
				
				num += 1;
			}
			
			if (num != 0)
			{
				ret.X = ret.X / num;
				ret.Y = ret.Y / num;
			}
			
			return ret;
		}
		
		public static IEnumerable<Wrappers.Link> FilterLink(IEnumerable<Wrappers.Wrapper> wrappers)
		{
			foreach (Wrappers.Wrapper wrapper in wrappers)
			{
				Wrappers.Link link = wrapper as Wrappers.Link;
				
				if (link != null)
				{
					yield return link;
				}
			}
		}
		
		[DllImport("libgtk-x11-2.0")]
		private static extern IntPtr gtk_get_current_event();
		
		public static Gdk.Event GetCurrentEvent()
		{
			return Gdk.Event.GetEvent(gtk_get_current_event());
		}
	}
}
