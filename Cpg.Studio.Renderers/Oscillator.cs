using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio.Wrappers.Renderers
{
	[Name("Oscillator")]
	public class Oscillator : Group
	{
		private double d_height;
		private Cairo.LinearGradient d_outer;
		private Cairo.LinearGradient d_inner;
		
		public Oscillator(Wrappers.Wrapper obj) : base(obj)
		{
			d_height = 0;
		}
		
		public Oscillator() : this(null)
		{
		}
		
		private void MakePatterns()
		{
			d_outer = new Cairo.LinearGradient(0, 1, 0, 0);
			d_outer.AddColorStopRgb(0, new Cairo.Color(127 / 255.0, 172 / 255.0, 227 / 255.0));
			d_outer.AddColorStopRgb(d_height, new Cairo.Color(1, 1, 1));
			
			d_inner = new Cairo.LinearGradient(0, 1, 0, 0);
			d_inner.AddColorStopRgb(0, new Cairo.Color(193 / 255.0, 217 / 255.0, 255 / 255.0));
			d_inner.AddColorStopRgb(d_height * 0.8, new Cairo.Color(1, 1, 1));
		}
		
		private void DrawCircle(Cairo.Context graphics, double radius, Allocation allocation)
		{
			graphics.Arc(allocation.Width / 2.0, allocation.Height / 2.0, radius, 0, 2 * Math.PI);
		}
		
		public override void Draw(Cairo.Context graphics)
		{
			Allocation alloc = d_group != null ? d_group.Allocation : new Allocation(0, 0, 1, 1);
			
			if (alloc.Height != d_height)
			{
				d_height = alloc.Height;
				MakePatterns();
			}
			
			graphics.Save();
			double uw = graphics.LineWidth;			
			double radius = Math.Min(alloc.Width / 2, alloc.Height / 2);
			
			graphics.Source = d_outer;
			DrawCircle(graphics, radius, alloc);
			graphics.Fill();
			
			graphics.LineWidth = uw * 2;
			graphics.SetSourceRGB(26 / 255.0, 80 / 255.0, 130 / 255.0);
			DrawCircle(graphics, radius * 0.85 - uw * 2, alloc);
			graphics.StrokePreserve();
			
			graphics.Source = d_inner;
			graphics.Fill();
			
			graphics.LineWidth = uw;
			radius = 0.3 * radius;
			
			graphics.Translate(alloc.Width / 2.0, alloc.Height / 2.0);
			graphics.SetSourceRGB(29 / 255.0, 71 / 255.0, 107 / 255.0);
			graphics.Arc(-radius, 0, radius, Math.PI, 2 * Math.PI);
			graphics.Stroke();
			
			graphics.Arc(radius, 0, radius, 0, Math.PI);
			graphics.Stroke();
			
			graphics.Restore();
		}
	}
}
