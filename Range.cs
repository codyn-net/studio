using System;
using System.Text.RegularExpressions;

namespace Cpg.Studio
{
	public class Range
	{
		private float d_from;
		private float d_step;
		private float d_to;
		
		public Range(string s)
		{
			Regex r = new Regex(@"\s*[:,]\s*");
			
			string[] parts = r.Split(s, 3);
			
			try
			{
				if (parts.Length == 1)
				{
					d_from = 0;
					d_to = float.Parse(parts[0]);
					d_step = d_to;
				}
				else if (parts.Length == 2)
				{
					d_from = float.Parse(parts[0]);
					d_to = float.Parse(parts[1]);
					d_step = 1;
				}
				else
				{
					d_from = float.Parse(parts[0]);
					d_step = float.Parse(parts[1]);
					d_to = float.Parse(parts[2]);
				}
			}
			catch (InvalidOperationException)
			{
				d_from = 0;
				d_step = 0;
				d_to = 0;
			}
		}
		
		public float From
		{
			get
			{
				return d_from;
			}
		}
		
		public float Step
		{
			get
			{
				return d_step;
			}
		}
		
		public float To
		{
			get
			{
				return d_to;
			}
		}
	}
}
