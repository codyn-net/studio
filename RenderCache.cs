using System;
using System.Collections.Generic;

namespace Cpg.Studio
{
	public class RenderCache
	{
		public delegate void RenderFunction (Cairo.Context context, int width, int height);
		
		private Cairo.Surface d_buffer;
		private int d_width;
		private int d_height;
		
		public RenderCache()
		{
			d_width = -1;
			d_height = -1;
		}
		
		private void RecreateBuffer(Cairo.Context context, int width, int height, RenderFunction function)
		{
			d_buffer = context.Target.CreateSimilar(Cairo.Content.ColorAlpha, width, height);			
			
			using (Cairo.Context ctx = new Cairo.Context(d_buffer))
			{
				// Keep original transform
				function(ctx, width, height);

				d_width = width;
				d_height = height;
			}
		}
		
		public void Clear()
		{
			d_buffer = null;
		}
		
		public void Render(Cairo.Context context, int width, int height, RenderFunction function)
		{
			if (d_buffer == null || d_width != width || d_height != height)
			{
				RecreateBuffer(context, width, height, function);
			}
			
			if (d_buffer != null)
			{
				context.Save();
				context.SetSourceSurface(d_buffer, 0, 0);
				context.Paint();
				context.Restore();
			}
		}
	}
}
