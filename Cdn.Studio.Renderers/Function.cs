using System;

namespace Cdn.Studio.Wrappers.Renderers
{	
	public class Function : Box
	{		
		public Function(Wrappers.Wrapper obj) : base(obj)
		{
		}
		
		public Function() : base()
		{
		}
		
		protected override void MakePatterns()
		{
			d_outer = new Cairo.LinearGradient(0, 1, 0, 0);
			d_outer.AddColorStopRgb(0, new Cairo.Color(0.5, 0.5, 0.8));
			d_outer.AddColorStopRgb(d_object != null ? d_object.Allocation.Height : 1, new Cairo.Color(1, 1, 1));
			
			d_inner= new Cairo.LinearGradient(0, 1, 0, 0);
			d_inner.AddColorStopRgb(0, new Cairo.Color(0.75, 0.85, 1));
			d_inner.AddColorStopRgb((d_object != null ? d_object.Allocation.Height : 1) * 0.8, new Cairo.Color(1, 1, 1));
		}
		
		protected virtual void DrawInnerFunction(Cairo.Context graphics, double uw, double x, double y, double width, double height)
		{
			double bmarg = uw * 5;
			
			double quart = (width - 2 * bmarg) / 4;
			double half = (width - 2 * bmarg) / 2;
			
			graphics.MoveTo(x + bmarg, y + height - bmarg);
			graphics.CurveTo(x + bmarg + quart, y + height - bmarg, x + bmarg + quart, y + bmarg, x + bmarg + half, y + bmarg);
			graphics.CurveTo(x + half + quart + bmarg, y + bmarg, x + half + quart + bmarg, y + height - bmarg, x + bmarg + half + half, y + height - bmarg);

			graphics.SetSourceRGB(0.3, 0.3, 0.6);
			graphics.Stroke();			
		}
		
		protected override void DrawInner(Cairo.Context graphics, double uw, double x, double y, double width, double height)
		{
			base.DrawInner(graphics, uw, x, y, width, height);
			
			DrawInnerFunction(graphics, uw, x, y, width, height);
		}

		protected override double[] LineColor()
		{
			return new double[] {0.4, 0.4, 0.4};
		}
	}
}
