using System;
using System.Collections.Generic;

namespace Cpg.Studio.Clipboard
{
	public class Internal
	{
		private static List<Wrappers.Wrapper> s_objects;
		
		public static bool Empty
		{
			get
			{
				return s_objects == null || s_objects.Count == 0;
			}
		}
		
		public static void Clear()
		{
			s_objects = null;
		}
		
		public static Wrappers.Wrapper[] Objects
		{
			get
			{
				return s_objects.ToArray();
			}
			set
			{
				if (value == null)
				{
					s_objects = null;
				}
				else
				{
					s_objects = new List<Wrappers.Wrapper>(value);
				}
			}
		}
	}
}

