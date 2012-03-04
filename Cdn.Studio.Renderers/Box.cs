using System;

namespace Cpg.Studio.Wrappers.Renderers
{	
	public class Box : Renderer
	{
		protected Cairo.LinearGradient d_outer;
		protected Cairo.LinearGradient d_inner;
		protected double[] d_hoverColor;
		protected double[] d_linkColor;
		protected double d_radius;
		
		public Box(Wrappers.Wrapper obj) : base(obj)
		{
			d_object = obj;
			d_hoverColor = new double[] {0.3, 0.6, 0.3, 0.6};
			d_linkColor = new double[] {0.9, 0.9, 0.1, 1.0};
			d_radius = 0.3;
			
			MakePatterns();
		}
		
		public Box() : this(null)
		{
		}
		
		protected virtual void MakePatterns()
		{
			d_outer = new Cairo.LinearGradient(0, 1, 0, 0);
			d_outer.AddColorStopRgb(0, new Cairo.Color(1, 1, 1));
			d_outer.AddColorStopRgb(d_object != null ? d_object.Allocation.Height : 1, new Cairo.Color(1, 1, 1));
			
			d_inner= new Cairo.LinearGradient(0, 1, 0, 0);
			d_inner.AddColorStopRgb(0, new Cairo.Color(1, 1, 1));
			d_inner.AddColorStopRgb(d_object != null ? d_object.Allocation.Height : 1, new Cairo.Color(1, 1, 1));
		}

		protected virtual double[] LineColor()
		{
			return new double[] {0, 0, 0};
		}
		
		protected virtual void DrawInner(Cairo.Context graphics, double uw, double x, double y, double width, double height)
		{
			if (d_radius > 0)
			{
				DrawRoundedRectangle(graphics, x, y, width, height, d_radius);
			}
			else
			{
				graphics.Rectangle(x, y, width, height);
			}

			graphics.StrokePreserve();
			
			graphics.Source = d_inner;
			graphics.Fill();
		}
		
		private void DrawRoundedRectangle(Cairo.Context graphics, double x, double y, double width, double height, double radius)
		{
			double x1 = x + width;
			double y1 = y + height;

			graphics.MoveTo(x, y + radius);

			graphics.CurveTo(x, y, x, y, x + radius, y);
			graphics.LineTo(x1 - radius, y);

			graphics.CurveTo(x1, y, x1, y, x1, y + radius);
			graphics.LineTo(x1, y1 - radius);
			
			graphics.CurveTo(x1, y1, x1, y1, x1 - radius, y1);
			graphics.LineTo(x + radius, y1);
			
			graphics.CurveTo(x, y1, x, y1, x, y1 - radius);
			graphics.ClosePath();
		}
		
		public override bool StrokeSelection (Cairo.Context graphics, double x, double y, double width, double height)
		{
			if (d_radius > 0)
			{
				DrawRoundedRectangle(graphics, x, y, width, height, d_radius);
				return true;
			}
			else
			{	
				return false;
			}
		}
		
		public override void Draw(Cairo.Context context)
		{
			Allocation allocation = d_object != null ? d_object.Allocation : new Allocation(0, 0, 1, 1);
			
			Cache.Render(context, allocation.Width, allocation.Height, delegate (Cairo.Context graphics, double width, double height)
			{
				graphics.Save();
	
				double uw = graphics.LineWidth;
				double marg = uw / 2;
				
				if (d_radius > 0)
				{
					DrawRoundedRectangle(graphics, -marg, -marg, allocation.Width + marg, allocation.Height + marg, d_radius);
				}
				else
				{	
					graphics.Rectangle(-marg, -marg, allocation.Width + marg, allocation.Height + marg);
				}
				
				graphics.Source = d_outer;
				graphics.FillPreserve();
				
				graphics.ClipPreserve();
				
				if (d_object != null && d_object.LinkFocus)
				{
					graphics.LineWidth = uw * 4;
					graphics.SetSourceRGBA(d_linkColor[0], d_linkColor[1], d_linkColor[2], d_linkColor[3]);
					graphics.Stroke();
				}
				else if (d_object != null && d_object.MouseFocus)
				{
					graphics.LineWidth = uw * 4;
					graphics.SetSourceRGBA(d_hoverColor[0], d_hoverColor[1], d_hoverColor[2], d_hoverColor[3]);
					graphics.Stroke();
				}
				else
				{
					graphics.NewPath();
				}
				
				graphics.LineWidth = uw * 2;
				marg = uw;
				
				double[] color = LineColor();
				graphics.SetSourceRGB(color[0], color[1], color[2]);
				
				double dd = allocation.Width * 0.1;
				double off = uw * 2 + (dd - (dd % marg));
				
				DrawInner(graphics, uw, off, off, allocation.Width - off * 2, allocation.Height - off * 2);
				
				graphics.Restore();
				base.Draw(graphics);
			});
		}
	}
}
