using System;

namespace Cpg.Studio.Wrappers.Renderers
{	
	public class PiecewisePolynomial : Function
	{		
		public PiecewisePolynomial(Wrappers.Wrapper obj) : base(obj)
		{
		}
		
		public PiecewisePolynomial() : base()
		{
		}
		
		protected override void DrawInnerFunction(Cairo.Context graphics, double uw, double x, double y, double width, double height)
		{
			double marg = uw * 3;
			double bmarg = uw * 5;
			
			double quart = (width - 2 * bmarg) / 4;
			double half = (width - 2 * bmarg) / 2;
			
			graphics.MoveTo(x + bmarg, y + height - bmarg);
			graphics.CurveTo(x + bmarg + quart, y + height - bmarg, x + bmarg + quart, y + bmarg, x + bmarg + half, y + bmarg);
			graphics.CurveTo(x + half + quart + bmarg, y + bmarg, x + half + quart + bmarg, y + height - bmarg, x + bmarg + half + half, y + height - bmarg);
			
			graphics.SetSourceRGB(0.3, 0.3, 0.6);
			graphics.Stroke();
			
			graphics.Arc(x + bmarg, y + height - bmarg, marg, 0, System.Math.PI * 2);
			graphics.Fill();
			
			graphics.Arc(x + bmarg + half, y + bmarg, marg, 0, System.Math.PI * 2);
			graphics.Fill();
			
			graphics.Arc(x + bmarg + half + half, y + height - bmarg, marg, 0, System.Math.PI * 2);
			graphics.Fill();
		}	
	}
}
