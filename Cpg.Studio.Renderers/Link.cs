using System;

namespace Cpg.Studio.Wrappers.Renderers
{
	// Only used for generating icons for now
	public class Link : Renderer
	{
		public Link()
		{
		}
		
		public override void Draw(Cairo.Context graphics)
		{
			Allocation alloc = d_object != null ? d_object.Allocation : new Allocation(0, 0, 1, 1);
			
			graphics.Save();
			graphics.SetSourceRGB(0.3, 0.3, 0.3);
			
			double uw = graphics.LineWidth;
			graphics.LineWidth = uw * 2;
			double ar = uw * 5;
			
			double dh = (alloc.Height + uw) / 2;
			
			graphics.MoveTo(alloc.Width, dh);
			graphics.LineTo(uw * 2 + ar, dh);
			graphics.Stroke();
			
			graphics.MoveTo(uw * 2, dh);
			graphics.RelLineTo(ar, -ar / 2);
			graphics.RelLineTo(0, ar);
			graphics.ClosePath();
			graphics.FillPreserve();
			graphics.Stroke();
			
			graphics.Restore();
		}
	}
}

