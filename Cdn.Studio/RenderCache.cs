using System;
using System.Collections.Generic;

namespace Cpg.Studio
{
	public class RenderCache : IDisposable
	{
		private Dictionary<long, Cairo.Surface> d_cache;
		public delegate void RenderFunc(Cairo.Context context, double width, double height);

		private Queue<long> d_cacheQueue;
		private Stack<Cairo.Context> d_renderStack;
		private uint d_maxSize;
		private bool d_enabled;
		
		private static bool s_enabled;

		public RenderCache(uint maxSize)
		{
			d_cache = new Dictionary<long, Cairo.Surface>();
			d_renderStack = new Stack<Cairo.Context>();
			d_maxSize = maxSize;
			d_cacheQueue = new Queue<long>();
			d_enabled = true;
		}
		
		public RenderCache() : this(0)
		{
		}
		
		static RenderCache()
		{
			s_enabled = true;
		}
		
		public void Dispose()
		{
			Invalidate();
		}
		
		public bool Enabled
		{
			get
			{
				return s_enabled && d_enabled;
			}
			set
			{
				d_enabled = value;
			}
		}
		
		private void AccountForScaling(Cairo.Context context, ref double width, ref double height)
		{
			// Recalculate width and height to correct for scaling
			double w = width;
			double h = height;

			context.Matrix.TransformDistance(ref w, ref h);
			
			width = System.Math.Ceiling(w);
			height = System.Math.Ceiling(h);
		}
		
		private long UniqueId(int width, int height)
		{
			return ((long)width << 32) + (long)height;
		}
		
		private Cairo.Surface Create(Cairo.Context context, double width, double height, bool cache)
		{
			Cairo.Surface surf;

			surf = context.Target.CreateSimilar(Cairo.Content.ColorAlpha, (int)width, (int)height);

			if (cache)
			{
				long id = UniqueId((int)width, (int)height);
				
				if (d_cache.ContainsKey(id))
				{
					DisposeSurface(d_cache[id]);
				}

				d_cache[id] = surf;
				d_cacheQueue.Enqueue(id);
			}
			
			return surf;
		}
		
		private void DisposeSurface(Cairo.Surface surface)
		{
			((IDisposable)surface).Dispose();
		}
		
		public void Invalidate()
		{
			foreach (KeyValuePair<long, Cairo.Surface> pair in d_cache)
			{
				DisposeSurface(pair.Value);
			}

			d_cache.Clear();
		}
		
		private void Remove(long id)
		{
			DisposeSurface(d_cache[id]);
			d_cache.Remove(id);
		}
		
		public void Render(Cairo.Context context, double width, double height, RenderFunc renderer)
		{
			if (!Enabled)
			{
				context.Save();
				renderer(context, width, height);
				context.Restore();
				
				return;
			}

			double w = width;
			double h = height;
			bool first;

			AccountForScaling(context, ref w, ref h);

			long id = UniqueId((int)w, (int)h);
			Cairo.Surface ret = null;
			bool disposesurf = true;
			
			first = d_renderStack.Count == 0;
			
			if (first)
			{
				d_cache.TryGetValue(id, out ret);
				disposesurf = false;
			}
			
			if (ret == null)
			{
				while (d_cacheQueue.Count >= d_maxSize && d_maxSize != 0)
				{
					Remove(d_cacheQueue.Dequeue());
				}
				
				ret = Create(context, w, h, first);
				disposesurf = !first;
				
				using (Cairo.Context ctx = new Cairo.Context(ret))
				{
					// Apply scaling and rotation here
					ctx.Scale(context.Matrix.Xx, context.Matrix.Yy);
					ctx.LineWidth = context.LineWidth;
				
					d_renderStack.Push(ctx);
				
					renderer(ctx, width, height);

					d_renderStack.Pop();
				}
			}
			
			context.Save();
			
			// Undo scaling here
			context.Scale(1.0 / context.Matrix.Xx, 1.0 / context.Matrix.Yy);
			context.SetSourceSurface(ret, 0, 0);
			context.Paint();
			context.Restore();
			
			if (disposesurf)
			{
				DisposeSurface(ret);
			}
		}
	}
}

