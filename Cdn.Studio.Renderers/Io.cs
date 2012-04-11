using System;

namespace Cdn.Studio.Wrappers.Renderers
{
	public class Io : Node
	{
		public Io(Wrappers.Wrapper obj) : base(obj)
		{
		}
		
		public Io() : this(null)
		{
		}
		
		protected override void MakePatterns()
		{
			d_outer = new Cairo.LinearGradient(0, 1, 0, 0);
			d_outer.AddColorStopRgb(0, new Cairo.Color(0.9, 0.8, 0.5));
			d_outer.AddColorStopRgb(d_object != null ? d_object.Allocation.Height : 1, new Cairo.Color(1, 1, 1));
			
			d_inner = new Cairo.LinearGradient(0, 1, 0, 0);
			d_inner.AddColorStopRgb(0, new Cairo.Color(0.9, 0.8, 0.2));
			d_inner.AddColorStopRgb((d_object != null ? d_object.Allocation.Height : 1) * 0.8, new Cairo.Color(1, 1, 1));
		}
		
		private void DrawColumn(Cairo.Context graphics, double x, double y, double width, double lheight, int numlines)
		{
			for (int i = 0; i < numlines; ++i)
			{
				graphics.MoveTo(x, y);
				graphics.LineTo(x + width, y);
				graphics.Stroke();

				y += lheight;
			}
		}
		
		protected override void DrawInner(Cairo.Context graphics, double uw, double x, double y, double width, double height)
		{
			base.DrawInner(graphics, uw, x, y, width, height);
			
			uw = uw / 2;
			
			double margin = uw * 5;
			int columns = 3;
			
			double cwidth = (width - (columns + 1) * margin) / columns;
			double cheight = height - 2 * margin;
			
			double lheight = uw * 6;
			int numlines = (int)System.Math.Ceiling(cheight / lheight);
			
			graphics.SetSourceRGB(0, 0, 0);
			
			graphics.LineWidth = uw;
			
			for (int i = 0; i < columns; ++i)
			{
				double xx = x + (i + 1) * margin + i * cwidth;
				DrawColumn(graphics, xx, y + margin, cwidth, lheight, numlines);
			}
		}
	}
}

