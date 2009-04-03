using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Cpg.Studio.Components
{
	public class State : Simulated
	{
		private Brush d_outer;
		private Brush d_inner;
		private Pen d_border;
		
		public State(Cpg.State obj) : base(obj)
		{
			d_outer = new LinearGradientBrush(new PointF(0, Allocation.Height), new PointF(0, 0), Color.FromArgb(127, 200, 127), Color.FromArgb(255, 255, 255));
			d_inner = new LinearGradientBrush(new PointF(0, Allocation.Height), new PointF(0, 0), Color.FromArgb(193, 255, 217), Color.FromArgb(255, 255, 255));
			
			d_border = new Pen(Color.FromArgb(26, 80, 130));
			d_border.Width = 0.02f;
		}
		
		public State() : this(new Cpg.State(""))
		{
		}
		
		public override void Draw (Graphics graphics)
		{
			GraphicsState state = graphics.Save();
			
			float scale = Utils.TransformScale(graphics.Transform);
			d_border.Width = 2 / scale;

			graphics.FillRectangle(d_outer, 0, 0, Allocation.Width, Allocation.Height);
			float off = 0.1f * Allocation.Width;
			
			RectangleF rect = new RectangleF(off, off, Allocation.Width - 2 * off, Allocation.Height - 2 * off);
			Console.WriteLine(rect);
			graphics.FillRectangle(d_inner, rect);
			graphics.DrawRectangle(d_border, rect.X + 1, rect.Y + 1, rect.Width, rect.Height);
			
			graphics.Restore(state);
			base.Draw(graphics);
		}

	}
}
