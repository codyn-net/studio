using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio.Wrappers.Renderers
{
	[Name("Default")]
	public class Default : Group
	{
		private double[][] d_colors;
		
		public Default(Wrappers.Wrapper obj) : base(obj)
		{
			d_colors = new double[5][];
			
			d_colors[0] = new double[] {26 / 125.0, 80 / 125.0, 130 / 125.0};
			d_colors[1] = new double[] {80 / 125.0, 26 / 125.0, 130 / 125.0};
			d_colors[2] = new double[] {26 / 125.0, 130 / 125.0, 80 / 125.0};
			d_colors[3] = new double[] {130 / 125.0, 80 / 125.0, 26 / 125.0};
			d_colors[4] = new double[] {80.0 / 125.0, 130.0 / 125.0, 26.0 / 125.0};
		}
		
		public Default() : this(null)
		{
		}
		
		private double[] Darken(double[] color)
		{
			double[] ret = new double[4];
			
			ret[3] = 0.6;
			
			for (int i = 0; i < color.Length; ++i)
				ret[i] = color[i] * 0.6;
			
			return ret;
		}
		
		private void DrawRect(Cairo.Context graphics, double x, double y, double width, double height, double[] color)
		{
			graphics.Rectangle(x, y, width, height);
			
			if (color.Length == 3)
			{
				graphics.SetSourceRGB(color[0], color[1], color[2]);
			}
			else
			{
				graphics.SetSourceRGBA(color[0], color[1], color[2], color[3]);
			}
			
			graphics.FillPreserve();
			
			double[] darker = Darken(color);
			
			graphics.SetSourceRGBA(darker[0], darker[1], darker[2], darker[3]);
			graphics.Stroke();
		}
		
		public override void Draw(Cairo.Context graphics)
		{
			Allocation alloc = d_group != null ? d_group.Allocation : new Allocation(0, 0, 1, 1);
			
			graphics.Save();
			double uw = graphics.LineWidth;
			
			graphics.LineWidth = uw * 2;
			
			double off = uw * 2 + alloc.Width * 0.1f;
			double w = (alloc.Width - 2 * off) * 0.4;
			double h = (alloc.Height - 2 * off) * 0.4;
			
			DrawRect(graphics, off, off, w, h, d_colors[0]);
			DrawRect(graphics, off, alloc.Height - h - off, w, h, d_colors[1]);
			DrawRect(graphics, alloc.Width - w - off, alloc.Height - h - off, w, h, d_colors[2]);
			DrawRect(graphics, alloc.Width - w - off, off, w, h, d_colors[3]);
			
			w = (alloc.Width - 2 * off) * 0.5f;
			h = (alloc.Height - 2 * off) * 0.5f;
			
			double x = (alloc.Width - w) / 2.0;
			double y = (alloc.Height - h) / 2.0;
			
			DrawRect(graphics, x, y, w, h, d_colors[4]);
			
			graphics.Restore();
		}
	}
}
