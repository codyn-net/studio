using System;

namespace Cpg.Studio.Wrappers.Renderers
{	
	public class State : Box
	{		
		public State(Wrappers.Wrapper obj) : base(obj)
		{
		}
		
		public State() : base()
		{
		}
		
		protected override void MakePatterns()
		{
			d_outer = new Cairo.LinearGradient(0, 1, 0, 0);
			d_outer.AddColorStopRgb(0, new Cairo.Color(0.5, 0.8, 0.5));
			d_outer.AddColorStopRgb(d_object != null ? d_object.Allocation.Height : 1, new Cairo.Color(1, 1, 1));
			
			d_inner= new Cairo.LinearGradient(0, 1, 0, 0);
			d_inner.AddColorStopRgb(0, new Cairo.Color(0.75, 1, 0.85));
			d_inner.AddColorStopRgb((d_object != null ? d_object.Allocation.Height : 1) * 0.8, new Cairo.Color(1, 1, 1));
		}

		protected override double[] LineColor()
		{
			return new double[] {26 / 255.0, 80 / 255.0, 130 / 255.0};
		}
	}
}
