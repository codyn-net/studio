using System;

namespace Cpg.Studio.Wrappers.Renderers
{
	[Name("Group")]
	public class Group : Renderer
	{
		private double[][] d_colors;

		protected Wrappers.Group d_group;

		public Group(Wrappers.Wrapper obj) : base (obj)
		{
			d_group = obj as Wrappers.Group;
			d_colors = new double[5][];
			
			d_colors[0] = new double[] {26 / 125.0, 80 / 125.0, 130 / 125.0};
			d_colors[1] = new double[] {80 / 125.0, 26 / 125.0, 130 / 125.0};
			d_colors[2] = new double[] {26 / 125.0, 130 / 125.0, 80 / 125.0};
			d_colors[3] = new double[] {130 / 125.0, 80 / 125.0, 26 / 125.0};
			d_colors[4] = new double[] {80.0 / 125.0, 130.0 / 125.0, 26.0 / 125.0};
		}
		
		public Group() : this(null)
		{
		}
		
		private double[] Darken(double[] color)
		{
			double[] ret = new double[4];
			
			ret[3] = 0.6;
			
			for (int i = 0; i < color.Length; ++i)
			{
				ret[i] = color[i] * 0.6;
			}
			
			return ret;
		}
		
		private void SetColor(Cairo.Context graphics, double[] color)
		{
			if (color.Length == 3)
			{
				graphics.SetSourceRGB(color[0], color[1], color[2]);
			}
			else
			{
				graphics.SetSourceRGBA(color[0], color[1], color[2], color[3]);
			}
		}
		
		private void DrawArrow(Cairo.Context graphics, double x, double y, double width, double height, double[] color)
		{
			graphics.MoveTo(x + width, y);
			graphics.LineTo(x, y + height);
			
			if (Detail == "ungroup")
			{
				graphics.LineTo(x, y);
			}
			else
			{
				graphics.LineTo(x + width, y + height);
			}
			
			graphics.ClosePath();
			SetColor(graphics, color);
			
			graphics.FillPreserve();
			
			double[] darker = Darken(color);
			
			graphics.SetSourceRGBA(darker[0], darker[1], darker[2], darker[3]);
			graphics.Stroke();}
		
		private void DrawRect(Cairo.Context graphics, double x, double y, double width, double height, double[] color)
		{
			graphics.Rectangle(x, y, width, height);
			
			SetColor(graphics, color);
			
			graphics.FillPreserve();
			
			double[] lineColor;
			
			if (WrappedObject != null && WrappedObject.LinkFocus)
			{
				lineColor = new double[] {0.8, 0.8, 0.3, 1.0};
			}
			else
			{
				lineColor = Darken(color);
			}
			
			graphics.SetSourceRGBA(lineColor[0], lineColor[1], lineColor[2], lineColor[3]);
			graphics.Stroke();
		}
		
		public override void Draw(Cairo.Context context)
		{
			Allocation alloc = d_group != null ? d_group.Allocation : new Allocation(0, 0, 1, 1);
			
			Cache.Render(context, alloc.Width, alloc.Height, delegate (Cairo.Context graphics, double width, double height) {
				double uw = graphics.LineWidth;
				
				double off;
				
				if (Style == DrawStyle.Normal)
				{
					graphics.LineWidth = uw * 2;
					off = uw * 2 + alloc.Width * 0.1f;
				}
				else
				{
					off = 0;
				}
				
				double w = (alloc.Width - 2 * off) * 0.4;
				double h = (alloc.Height - 2 * off) * 0.4;
				
				if (Detail == "group" || Detail == "ungroup")
				{
					DrawArrow(graphics, off, off, w, h, d_colors[0]);
					DrawArrow(graphics, off, alloc.Height - off, w, -h, d_colors[1]);
					DrawArrow(graphics, alloc.Width - off, alloc.Height - off, -w, -h, d_colors[2]);
					DrawArrow(graphics, alloc.Width - off, off, -w, h, d_colors[3]);
				}
				else
				{
					DrawRect(graphics, off, off, w, h, d_colors[0]);
					DrawRect(graphics, off, alloc.Height - h - off, w, h, d_colors[1]);
					DrawRect(graphics, alloc.Width - w - off, alloc.Height - h - off, w, h, d_colors[2]);
					DrawRect(graphics, alloc.Width - w - off, off, w, h, d_colors[3]);
				}
				
				double w2 = (alloc.Width - 2 * off) * 0.5f;
				double h2 = (alloc.Height - 2 * off) * 0.5f;
				
				double x = (alloc.Width - w2) / 2.0;
				double y = (alloc.Height - h2) / 2.0;
				
				DrawRect(graphics, x, y, w2, h2, d_colors[4]);
			});
		}
	}
}