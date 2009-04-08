using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace Cpg.Studio.Components
{
	public class Link : Simulated
	{
		Components.Simulated d_from;
		Components.Simulated d_to;
		int d_offset;
		Brush d_selectedBrush;
		Brush d_normalBrush;

		public Link(Cpg.Link obj, Components.Simulated from, Components.Simulated to) : base(obj)
		{
			d_from = from != null ? from : Components.Simulated.FromCpg(obj.From);
			d_to = to != null ? to : Components.Simulated.FromCpg(obj.To);
			
			d_selectedBrush = new SolidBrush(Color.FromArgb(150, 150, 150, 255));
			d_normalBrush = new SolidBrush(Color.FromArgb(150, 180, 180, 180));
		}
		
		public Link(Cpg.Link obj) : this(obj, null, null)
		{
		}
		
		public Link() : this(null, null, null)
		{
		}
		
		public Components.Object[] Objects
		{
			get
			{
				if (d_object != null)
					return new Components.Object[] {d_from, d_to};
				else
					return new Components.Object[] {};
			}
		}
		
		public int Offset
		{
			get
			{
				return d_offset;
			}
			set
			{
				d_offset = value;
			}
		}
		
		public bool Empty()
		{
			return d_object == null;
		}
		
		public bool SameObjects(Components.Link other)
		{
			return (other.d_from == d_from && other.d_to == d_to);
		}
		
		public Components.Simulated From
		{
			get
			{
				return d_from;
			}
			set
			{
				d_from = value;
			}
		}
		
		public Components.Simulated To
		{
			get
			{
				return d_to;
			}
			set
			{
				d_to = value;
			}
		}
		
		private bool RectHittest(PointF p1, PointF p2, PointF p3, PointF p4, Allocation rect, float gridSize)
		{
			Allocation other = new Allocation(0, 0, 1f / gridSize, 1f / gridSize);
			
			for (int i = 0; i < 5; ++i)
			{
				other.X = EvaluateBezier(p1.X, p2.X, p3.X, p4.X, (float)i / 5);
				other.Y = EvaluateBezier(p1.Y, p2.Y, p3.Y, p4.Y, (float)i / 5);
				
				if (rect.Intersects(other))
					return true;
			}
			
			return false;
		}
		
		private float DistanceToLine(PointF start, PointF stop, PointF point)
		{
			PointF dx = new PointF(point.X - start.X, stop.X - start.X);
			PointF dy = new PointF(point.Y - start.Y, stop.Y - start.Y);
			
			float dot = dx.X * dx.Y + dy.X * dy.Y;
			float len_sq = dx.Y * dx.Y + dy.Y * dy.Y;
			float param = dot / len_sq;
			
			PointF res = new PointF(0, 0);
			
			if (param < 0)
			{
				res = start;
			}
			else if (param > 1)
			{
				res = stop;
			}
			else
			{
				res.X = start.X + param * dx.Y;
				res.Y = start.Y + param * dy.Y;
			}
			
			return (float)Math.Sqrt((point.X - res.X) * (point.X - res.X) + (point.Y - res.Y) * (point.Y - res.Y));
		}
		
		public bool HitTest(Allocation rect, int gridSize)
		{
			Allocation a1 = d_from.Allocation;
			Allocation a2 = d_to.Allocation;
			
			PointF from = new PointF(a1.X + a1.Width / 2, a1.Y + a1.Height / 2);
			PointF to = new PointF(a2.X + a2.Width / 2, a2.Y + a2.Height / 2);
			
			PointF control = CalculateControl(from, to);
			
			// Piece wise linearization
			int num = 5;
			List<float> dist = new List<float>();
			
			PointF prevp = from;
			PointF p2;
			PointF p3;
			
			if (from.X == to.X && from.Y == to.Y)
			{
				p2 = new PointF(to.X - 2, to.Y - ((d_offset + 1) + 0.5f));
				p3 = new PointF(to.X + 2, to.Y - ((d_offset + 1) + 0.5f));
			}
			else
			{
				p2 = control;
				p3 = control;
			}
			
			if (rect.Width > 1 || rect.Height > 1)
			{
				return RectHittest(from, p2, p3, to, rect, gridSize);
			}
			
			for (int i = 1; i < num; ++i)
			{
				float px = EvaluateBezier(from.X, p2.X, p3.X, to.X, (float)i / num);
				float py = EvaluateBezier(from.Y, p2.Y, p3.Y, to.Y, (float)i / num);
				
				dist.Add(DistanceToLine(prevp, new PointF(px, py), new PointF(rect.X, rect.Y)));
				
				prevp = new PointF(px, py);
			}

			return (Utils.Min(dist) < 10.0f / gridSize);
		}
		
		private PointF CalculateControl(PointF from, PointF to)
		{
			PointF diff = new PointF(to.X - from.X, to.Y - from.Y);
			PointF point = new PointF(from.X + diff.X / 2, from.Y + diff.Y / 2);
			
			bool same = (diff.X == 0 && diff.Y == 0);
			
			// Offset perpendicular
			float dist = 1 * (d_offset + 1);
			float alpha = same ? 0 : (float)Math.Atan(diff.X / -diff.Y);
			
			if (diff.Y >= 0)
				alpha += (float)Math.PI;
		
			return new PointF(point.X + (float)Math.Cos(alpha) * dist, point.Y + (float)Math.Sin(alpha) * dist);
		}
		
		public float EvaluateBezier(float p0, float p1, float p2, float p3, float t)
		{
			return (float)Math.Pow(1 - t, 3) * p0 + 3 * t * (float)Math.Pow(1 - t, 2) * p1 + 3 * (float)Math.Pow(t, 2) * (1 - t) * p2 + (float)Math.Pow(t, 3) * p3;
		}
		
		public override void Draw(Graphics graphics, Font font)
		{
			if (d_from == null || d_to == null)
				return;
		
			Allocation a1 = d_from.Allocation;
			Allocation a2 = d_to.Allocation;
			
			PointF from = new PointF(a1.X + a1.Width / 2, a1.Y + a1.Height / 2);
			PointF to = new PointF(a2.X + a2.Width / 2, a2.Y + a2.Height / 2);
			
			PointF control = CalculateControl(from, to);
			Pen pen = new Pen(Selected ? d_selectedBrush : d_normalBrush, 2 / Utils.TransformScale(graphics.Transform));
			
			PointF pts = new PointF(0, 0);
			
			if (from.X == to.X && from.Y == to.Y)
			{
				// Draw pretty one
				pts.X = 2;
				pts.Y = (d_offset + 1) + 0.5f;
				
				graphics.DrawBezier(pen, from.X, from.Y, to.X - pts.X, to.Y - pts.Y, to.X + pts.X, to.Y - pts.Y, to.X, to.Y);
			}
			else
			{
				graphics.DrawBezier(pen, from.X, from.Y, control.X, control.Y, control.X, control.Y, to.X, to.Y);
			}
			
			PointF xy = new PointF(0, 0);
			float pos;
			
			// Draw the arrow, first move to the center, then rotate, then draw the arrow
			if (from.X == to.X && from.Y == to.Y)
			{
				xy.X = EvaluateBezier(to.X, to.X - pts.X, to.X + pts.X, to.X, 0.5f);
				xy.Y = EvaluateBezier(to.Y, to.Y - pts.Y, to.Y + pts.Y, to.Y, 0.5f);
				
				pos = -0.5f * (float)Math.PI;
			}
			else
			{
				xy.X = EvaluateBezier(from.X, control.X, control.X, to.X, 0.5f);
				xy.Y = EvaluateBezier(from.Y, control.Y, control.Y, to.Y, 0.5f);
				
				PointF diff = new PointF(to.X - from.X, to.Y - from.Y);
				
				if (diff.X == 0)
				{
					pos = (diff.Y < 0 ? 1.5f : 0.5f) * (float)Math.PI; 
				}
				else
				{
					pos = (float)Math.Atan(diff.Y / diff.X);
					
					if (diff.X < 0)
						pos += (float)Math.PI;
					else if (diff.Y < 0)
						pos += 2 * (float)Math.PI;
				}
				
				pos += 0.5f * (float)Math.PI;
			}
			
			graphics.TranslateTransform(xy.X, xy.Y);
			graphics.RotateTransform(pos / (float)Math.PI * 180);
			
			GraphicsPath path = new GraphicsPath();
			float size = 0.15f;
			path.AddLines(new PointF[] { new PointF(0, 0), new PointF(-size, 0), new PointF(0, -size), new PointF(size, 0) });
			path.CloseFigure();
			
			graphics.FillPath(Selected ? d_selectedBrush : d_normalBrush, path);
		}

	}
}
