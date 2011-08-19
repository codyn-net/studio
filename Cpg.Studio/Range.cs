using System;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Cpg.Studio
{
	public class Range
	{
		private double d_from;
		private double d_step;
		private double d_to;
		
		public Range(double from, double timestep, double to)
		{
			d_from = from;
			d_step = timestep;
			d_to = to;
		}
		
		public Range(string s)
		{
			Regex r = new Regex(@"\s*[:,]\s*");
			NumberFormatInfo info = NumberFormatInfo.CurrentInfo.Clone() as NumberFormatInfo;
			
			info.NumberDecimalSeparator = ".";
			
			string[] parts = r.Split(s, 3);
			
			try
			{
				if (parts.Length == 1)
				{
					d_from = 0;
					d_to = double.Parse(parts[0], info);
					d_step = d_to;
				}
				else if (parts.Length == 2)
				{
					d_from = double.Parse(parts[0], info);
					d_to = double.Parse(parts[1], info);
					d_step = 1;
				}
				else
				{
					d_from = double.Parse(parts[0], info);
					d_step = double.Parse(parts[1], info);
					d_to = double.Parse(parts[2], info);
				}
			}
			catch (FormatException)
			{
				d_from = 0;
				d_step = 0;
				d_to = 0;
			}
		}
		
		public double From
		{
			get
			{
				return d_from;
			}
		}
		
		public double Step
		{
			get
			{
				return d_step;
			}
		}
		
		public double To
		{
			get
			{
				return d_to;
			}
		}
	}
}
