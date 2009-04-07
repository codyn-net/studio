using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio.Components.Renderers
{
	[Name("Default")]
	public class Default : Renderer
	{
		private SolidBrush[] d_brushes;
		
		public Default(Components.Group group) : base(group)
		{
			d_brushes = new SolidBrush[5];
			
			d_brushes[0] = new SolidBrush(Color.FromArgb(52, 160, 255));
			d_brushes[1] = new SolidBrush(Color.FromArgb(160, 52, 255));
			d_brushes[2] = new SolidBrush(Color.FromArgb(52, 255, 160));
			d_brushes[3] = new SolidBrush(Color.FromArgb(255, 160, 52));
			d_brushes[4] = new SolidBrush(Color.FromArgb(160, 255, 52));
		}
		
		private SolidBrush Darken(SolidBrush other)
		{
			Color o = other.Color;
			Color clr = Color.FromArgb((int)(o.A * 0.6), 
			                           (int)(o.R * 0.6), 
			                           (int)(o.G * 0.6), 
			                           (int)(o.B * 0.6));
			
			return new SolidBrush(clr);
		}
		
		private void DrawRect(Graphics graphics, RectangleF rect, SolidBrush brush)
		{
			graphics.FillRectangle(brush, rect);
			graphics.DrawRectangle(new Pen(Darken(brush)), rect.X, rect.Y, rect.Width, rect.Height);
		}
		
		public override void Draw(Graphics graphics, Font font)
		{
			Allocation alloc = d_group.Allocation;
			
			GraphicsState state = graphics.Save();
			
			float scale = 1f / Utils.TransformScale(graphics.Transform);
			
			float off = scale * 2 + alloc.Width * 0.1f;
			PointF pt = new PointF((alloc.Width - 2 * off) * 0.4f, (alloc.Height - 2 * off) * 0.4f);
			
			DrawRect(graphics, new RectangleF(off, off, pt.X, pt.Y), d_brushes[0]);
			DrawRect(graphics, new RectangleF(off, alloc.Height - pt.Y - off, pt.X, pt.Y), d_brushes[1]);
			DrawRect(graphics, new RectangleF(alloc.Width - pt.X - off, alloc.Height - pt.Y - off, pt.X, pt.Y), d_brushes[2]);
			DrawRect(graphics, new RectangleF(alloc.Width - pt.X - off, off, pt.X, pt.Y), d_brushes[3]);
			
			pt.X = (alloc.Width - 2 * off) * 0.5f;
			pt.Y = (alloc.Height - 2 * off) * 0.5f;
			
			RectangleF r = new RectangleF((alloc.Width - pt.X) / 2, (alloc.Height - pt.Y) / 2, pt.X, pt.Y);
			DrawRect(graphics, r, d_brushes[4]);
			
			graphics.Restore(state);
		}
	}
}
