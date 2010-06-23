using System;

namespace Cpg.Studio.Wrappers.Renderers
{	
	public class Box : Renderer
	{
		protected Cairo.LinearGradient d_outer;
		protected Cairo.LinearGradient d_inner;
		protected double[] d_hoverColor;
		
		public Box(Wrappers.Wrapper obj) : base(obj)
		{
			d_object = obj;
			d_hoverColor = new double[] {0.3, 0.6, 0.3, 0.6};
			
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
		
		public override void Draw(Cairo.Context graphics)
		{
			Allocation allocation = d_object != null ? d_object.Allocation : new Allocation(0, 0, 1, 1);
			graphics.Save();
			
			double uw = graphics.LineWidth;
			double marg = uw / 2;

			graphics.Rectangle(-marg, -marg, allocation.Width + marg, allocation.Height + marg);
			
			if (d_object != null && d_object.MouseFocus)
			{
				graphics.LineWidth = uw * 2;
				graphics.SetSourceRGBA(d_hoverColor[0], d_hoverColor[1], d_hoverColor[2], d_hoverColor[3]);
				graphics.StrokePreserve();
			}

			graphics.Source = d_outer;
			graphics.Fill();
			
			graphics.LineWidth = uw * 2;
			marg = uw;
			
			double[] color = LineColor();
			graphics.SetSourceRGB(color[0], color[1], color[2]);
			
			double dd = allocation.Width * 0.1;
			double off = uw * 2 + (dd - (dd % marg));
			graphics.Rectangle(off, off, allocation.Width - off * 2, allocation.Height - off * 2);
			graphics.StrokePreserve();
			
			graphics.Source = d_inner;
			graphics.Fill();
			
			graphics.Restore();
			base.Draw(graphics);
		}
	}
}
