using System;

namespace Cpg.Studio.Widgets
{
	public class Progress : IDisposable
	{
		private Grid d_grid;
		private Cairo.Surface d_canvas;
		private double d_overlayProgress;
		private double d_height;
		private double d_radius;
		private double d_margin;
		private double d_progress;
		private double d_prevProgress;

		public Progress(Grid grid)
		{
			d_grid = grid;
			
			d_grid.ExposeEvent += HandleGridExpose;
			d_grid.ConfigureEvent += HandleGridConfigure;
			d_overlayProgress = 0;
			
			d_height = 20;
			d_radius = 5;
			d_margin = 5;
			d_progress = 0;
			d_prevProgress = 0;
			
			UpdateCanvas();
			
			GLib.Timeout.Add(10, OverlayAnimation);
		}
		
		private void UpdateCanvas()
		{
			using (Cairo.Context context = Gdk.CairoHelper.Create(d_grid.GdkWindow))
			{
				d_canvas = context.Target.CreateSimilar(Cairo.Content.Color, d_grid.Allocation.Width, d_grid.Allocation.Height);
				
				using (Cairo.Context ctx = new Cairo.Context(d_canvas))
				{
					d_grid.Draw(ctx);
				}
			}
			
			d_grid.QueueDraw();
		}

		private void HandleGridConfigure(object o, Gtk.ConfigureEventArgs args)
		{
			UpdateCanvas();
		}
		
		private bool OverlayAnimation()
		{
			d_overlayProgress += 0.1;
			d_grid.QueueDraw();
			
			return d_overlayProgress < 1;
		}
		
		private void RoundedRectangle(Cairo.Context gr, double x, double y, double width, double height, double radius)
		{
			gr.Save ();
			
			if ((radius > height / 2) || (radius > width / 2))
			{
				radius = System.Math.Min(height / 2, width / 2);
			}
			
			gr.MoveTo(x, y + radius);
			
			gr.Arc(x + radius, y + radius, radius, System.Math.PI, -System.Math.PI / 2);
			gr.LineTo(x + width - radius, y);

			gr.Arc(x + width - radius, y + radius, radius, -System.Math.PI / 2, 0);
			gr.LineTo(x + width, y + height - radius);
			
			gr.Arc(x + width - radius, y + height - radius, radius, 0, System.Math.PI / 2);
			gr.LineTo(x + radius, y + height);
			
			gr.Arc(x + radius, y + height - radius, radius, System.Math.PI / 2, System.Math.PI);
			gr.ClosePath();
			
			gr.Restore();
		}
		
		[GLib.ConnectBefore]
		private void HandleGridExpose(object o, Gtk.ExposeEventArgs args)
		{
			using (Cairo.Context graphics = Gdk.CairoHelper.Create(d_grid.GdkWindow))
			{
				graphics.SetSourceSurface(d_canvas, 0, 0);
				graphics.Paint();
				
				graphics.Rectangle(0, 0, d_grid.Allocation.Width, d_grid.Allocation.Height);
				graphics.SetSourceRGBA(0, 0, 0, d_overlayProgress * 0.5);
				graphics.Fill();
				
				if (d_overlayProgress >= 1)
				{
					double w = d_grid.Allocation.Width;
					double h = d_grid.Allocation.Height;

					RoundedRectangle(graphics, d_margin, h - d_margin - d_height, w - d_margin * 2, d_height, d_radius);
					graphics.SetSourceRGB(0.9, 0.9, 0.9);
					graphics.Fill();
					
					double pgs = (w - d_margin * 2 - 2) * d_progress;
					RoundedRectangle(graphics, d_margin + 1, h - d_margin - d_height + 1, pgs, d_height - 2, d_radius - 1);
					graphics.SetSourceRGB(0.4, 0.4, 0.4);
					graphics.Fill();
				}
			}

			args.RetVal = true;
		}
		
		public void Update(double progress)
		{
			d_progress = progress;
			
			if (d_overlayProgress < 1 || (d_progress - d_prevProgress) > 0.01)
			{
				d_prevProgress = d_progress;
				d_grid.QueueDraw();

				while (GLib.MainContext.Pending())
				{
					GLib.MainContext.Iteration();
				}
			}
		}
		
		public void Dispose()
		{
			if (d_canvas != null)
			{
				d_canvas.Destroy();
				d_canvas = null;
			}
			
			if (d_grid != null)
			{
				d_grid.ExposeEvent -= HandleGridExpose;
				d_grid.ConfigureEvent -= HandleGridConfigure;
				d_grid = null;
			}
		}
	}
}

