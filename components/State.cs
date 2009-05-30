using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio.Components
{
	public class State : Simulated
	{
		private Cairo.LinearGradient d_outer;
		private Cairo.LinearGradient d_inner;
		private double[] d_hoverColor;
		
		public State(Cpg.State obj) : base(obj)
		{
			d_outer = new Cairo.LinearGradient(0, 1, 0, 0);
			d_outer.AddColorStopRgb(0, new Cairo.Color(0.5, 0.8, 0.5));
			d_outer.AddColorStopRgb(Allocation.Height, new Cairo.Color(1, 1, 1));
			
			d_inner= new Cairo.LinearGradient(0, 1, 0, 0);
			d_inner.AddColorStopRgb(0, new Cairo.Color(0.75, 1, 0.85));
			d_inner.AddColorStopRgb(Allocation.Height * 0.8, new Cairo.Color(1, 1, 1));
			
			d_hoverColor = new double[] {0.3, 0.6, 0.3, 0.6};
		}
		
		public State() : this(new Cpg.State("id"))
		{
		}
		
		protected virtual double[] LineColor()
		{
			return new double[] {26 / 255.0, 80 / 255.0, 130 / 255.0};
		}
		
		public override void Draw(Cairo.Context graphics)
		{
			graphics.Save();
			
			double uw = graphics.LineWidth;
			double marg = uw / 2;

			graphics.Rectangle(marg, marg, Allocation.Width, Allocation.Height);
			
			if (MouseFocus)
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
			
			double dd = Allocation.Width * 0.1;
			double off = uw * 2 + (dd - (dd % marg));
			graphics.Rectangle(off, off, Allocation.Width - off * 2, Allocation.Height - off * 2);
			graphics.StrokePreserve();
			
			graphics.Source = d_inner;
			graphics.Fill();
			
			graphics.Restore();
			base.Draw(graphics);
		}
	}
}
