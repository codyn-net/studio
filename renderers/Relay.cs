using System;

namespace Cpg.Studio.Components.Renderers
{	
	public class Relay : Box
	{		
		public Relay(Components.Object obj) : base(obj)
		{
		}
		
		public Relay() : base()
		{
		}

		protected override void MakePatterns()
		{
			d_outer = new Cairo.LinearGradient(0, 1, 0, 0);
			d_outer.AddColorStopRgb(0, new Cairo.Color(200 / 255.0, 200 / 255.0, 100 / 255.0));
			d_outer.AddColorStopRgb(d_object != null ? d_object.Allocation.Height : 1, new Cairo.Color(1, 1, 1));
			
			d_inner= new Cairo.LinearGradient(0, 1, 0, 0);
			d_inner.AddColorStopRgb(0, new Cairo.Color(150 / 255.0, 150 / 255.0, 100 / 255.0));
			d_inner.AddColorStopRgb((d_object != null ? d_object.Allocation.Height : 1) * 0.8, new Cairo.Color(1, 1, 1));
		}

		protected override double[] LineColor()
		{
			return new double[] {130 / 255.0, 130 / 255.0, 30 / 255.0};
		}
	}
}
