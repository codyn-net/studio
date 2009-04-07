using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio.Components.Renderers
{
	[Name("Oscillator")]
	public class Oscillator : Renderer
	{
		private float d_height;
		private LinearGradientBrush d_outer;
		private LinearGradientBrush d_inner;
		
		public Oscillator(Group group) : base(group)
		{
			d_height = 0;
		}
		
		private void MakePatterns()
		{
			d_outer = new LinearGradientBrush(new PointF(0, 0), 
			                                  new PointF(0, d_height), 
			                                  Color.FromArgb(127, 172, 227),
			                                  Color.FromArgb(255, 255, 255));
			d_inner = new LinearGradientBrush(new PointF(0, 0),
			                                  new PointF(0, d_height * 0.8f),
			                                  Color.FromArgb(193, 217, 255),
			                                  Color.FromArgb(255, 255, 255));
		}
		
		private void FillCircle(Graphics graphics, Brush brush, PointF pt, float radius)
		{
			graphics.FillEllipse(brush, pt.X, pt.Y, radius * 2, radius * 2);
		}
		
		private void DrawCircle(Graphics graphics, Pen pen, PointF pt, float radius)
		{
			graphics.DrawEllipse(pen, pt.X, pt.Y, radius * 2, radius * 2);
		}
		
		public override void Draw(Graphics graphics, Font font)
		{
			Allocation alloc = d_group.Allocation;
			
			if (alloc.Height != d_height)
			{
				d_height = alloc.Height;
				MakePatterns();
			}
			
			float scale = 1f / Utils.TransformScale(graphics.Transform);
			
			GraphicsState state = graphics.Save();
			float radius = Math.Min(alloc.Width / 2, alloc.Height / 2);
			
			FillCircle(graphics, d_outer, new PointF(0, 0), radius);
			
			float off = scale * 2 + 0.1f * radius * 2;
			float srad = radius * 0.1f - scale * 4;
			FillCircle(graphics, d_inner, new PointF(off, off), srad);
			
			DrawCircle(graphics, new Pen(Color.FromArgb(26, 80, 130)), new PointF(off, off), srad); 
			
			// Draw little sine thingie
			// TODO
			graphics.Restore(state);
		}
	}
}
