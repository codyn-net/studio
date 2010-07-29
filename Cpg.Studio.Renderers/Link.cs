using System;

namespace Cpg.Studio.Wrappers.Renderers
{
	// Only used for generating icons for now
	public class Link : Renderer
	{
		private static double[] s_normalColor;
		private static double[] s_selectedColor;
		private static double[] s_hoverColor;
		private static double[] s_iconColor;
		private static double s_arrowSize;

		static Link()
		{
			s_normalColor = new double[] {0.7, 0.7, 0.7, 0.6};
			s_selectedColor = new double[] {0.6, 0.6, 1, 0.6};
			s_hoverColor = new double[] {0.3, 0.6, 0.3, 0.6};
			s_iconColor = new double[] {0.5, 0.5, 0.5, 1.0};
			
			s_arrowSize = 0.15;
		}
		
		public static double ArrowSize
		{
			get
			{
				return s_arrowSize;
			}
		}
		
		public Link() : base()
		{
		}

		public Link(Wrappers.Wrapper obj) : base(obj)
		{
		}
		
		public new Wrappers.Link WrappedObject
		{
			get
			{
				return (Wrappers.Link)base.WrappedObject;
			}
		}
		
		private double[] StateColor()
		{
			if (Style == Renderer.DrawStyle.Icon)
			{
				return s_iconColor;
			}
			else if (WrappedObject == null)
			{
				return s_normalColor;
			}
			else if (WrappedObject.Selected)
			{
				return s_selectedColor;
			}
			else if (WrappedObject.MouseFocus)
			{
				return s_hoverColor;
			}
			else
			{
				return s_normalColor;
			}
		}
		
		private Point CalculateControl(Point fr, Point to)
		{
			Point diff = new Point(to.X - fr.X, to.Y - fr.Y);
			Point point = new Point(fr.X + diff.X / 2, fr.Y + diff.Y / 2);
			
			bool same = (diff.X == 0 && diff.Y == 0);
			
			// Offset perpendicular
			double dist = 1 * WrappedObject.Offset;
			double alpha = same ? 0 : Math.Atan(diff.X / -diff.Y);
			
			if (diff.Y >= 0)
			{
				alpha += Math.PI;
			}
		
			return new Point(point.X + Math.Cos(alpha) * dist, point.Y + Math.Sin(alpha) * dist);
		}
		
		public static double EvaluateBezier(double p0, double p1, double p2, double p3, double t)
		{
			return Math.Pow(1 - t, 3) * p0 + 3 * t * Math.Pow(1 - t, 2) * p1 + 3 * Math.Pow(t, 2) * (1 - t) * p2 + Math.Pow(t, 3) * p3;
		}
		
		public static Point EvaluateBezier(Point p0, Point p1, Point p2, Point p3, double t)
		{
			return new Point(
				EvaluateBezier(p0.X, p1.X, p2.X, p3.X, t),
				EvaluateBezier(p0.Y, p1.Y, p2.Y, p3.Y, t)
			);
		}
		
		public Point[] ControlPoints()
		{
			if (WrappedObject.From == null || WrappedObject.To == null)
			{
				return null;
			}

			Allocation a1 = WrappedObject.From.Allocation;
			Allocation a2 = WrappedObject.From.Allocation;
			
			Point fr = new Point(a1.X + a1.Width / 2, a1.Y + a1.Height / 2);
			Point to = new Point(a2.X + a2.Width / 2, a2.Y + a2.Height / 2);
			Point control = CalculateControl(fr, to);
			
			if (fr.X == to.X && fr.Y == to.Y)
			{
				Point pts = new Point(2, WrappedObject.Offset + 0.5f);

				return new Point[] {
					fr,
					new Point(to.X - pts.X, to.Y - pts.Y),
					new Point(to.X + pts.X, to.Y - pts.Y),
					to
				};
			}
			else
			{
				return new Point[] {fr, control, control, to};
			}
		}
		
		public static double DistanceToLine(Point start, Point stop, Point point)
		{
			Point dx = new Point(point.X - start.X, stop.X - start.X);
			Point dy = new Point(point.Y - start.Y, stop.Y - start.Y);
			
			double dot = dx.X * dx.Y + dy.X * dy.Y;
			double len_sq = dx.Y * dx.Y + dy.Y * dy.Y;
			double param = dot / len_sq;
			
			Point res = new Point(0, 0);
			
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
			
			return Math.Sqrt((point.X - res.X) * (point.X - res.X) + (point.Y - res.Y) * (point.Y - res.Y));
		}
		
		private bool Standalone
		{
			get
			{
				return WrappedObject == null || WrappedObject.Empty;
			}
		}
		
		private void FromState(Cairo.Context graphics, bool transparent)
		{
			double[] color = StateColor();
			
			if (transparent && color.Length == 4)
			{
				graphics.SetSourceRGBA(color[0], color[1], color[2], color[3]);
			}
			else
			{
				graphics.SetSourceRGB(color[0], color[1], color[2]);
			}
			
			if (WrappedObject != null && WrappedObject.KeyFocus)
			{
				graphics.LineWidth *= 4;
				graphics.SetDash(new double[] {graphics.LineWidth, graphics.LineWidth}, 0);
			}
			else if (Style == Renderer.DrawStyle.Icon)
			{
				graphics.LineWidth *= 3;
			}
			else if (Standalone)
			{
				graphics.LineWidth *= 4;
			}
			else if (WrappedObject != null && WrappedObject.MouseFocus)
			{
				graphics.LineWidth *= 2;
			}
		}
		
		private void DrawArrow(Cairo.Context graphics, double x, double y, double pos)
		{
			graphics.MoveTo(x, y);
			graphics.Rotate(pos);
			graphics.RelMoveTo(0, (pos + 0.5 * Math.PI < Math.PI ? -1 : 1) * s_arrowSize / 2);

			graphics.RelLineTo(-s_arrowSize, 0);
			graphics.RelLineTo(s_arrowSize, -s_arrowSize);
			graphics.RelLineTo(s_arrowSize, s_arrowSize);
			graphics.RelLineTo(-s_arrowSize, 0);
			
			graphics.Fill();
		}
		
		private void DrawObjects(Cairo.Context graphics)
		{
			FromState(graphics, true);

			Point[] points = ControlPoints();
			
			graphics.MoveTo(points[0].X, points[0].Y);
			graphics.CurveTo(points[1].X, points[1].Y, points[2].X, points[2].Y, points[3].X, points[3].Y);

			graphics.Stroke();
			
			Point xy;
			double pos;
			
			// Draw the arrow, first move to the center, then rotate, then draw the arrow
			xy = EvaluateBezier(points[0], points[1], points[2], points[3], 0.5);
			
			if (points[0] == points[3])
			{
				pos = 1.5 * Math.PI;
			}
			else
			{
				Point diff = new Point(points[3].X - points[0].X, points[3].Y - points[0].Y);
				
				if (diff.X == 0)
				{
					pos = (diff.Y < 0 ? 1.5 : 0.5) * Math.PI; 
				}
				else
				{
					pos = Math.Atan(diff.Y / diff.X);
					
					if (diff.X < 0)
					{
						pos += Math.PI;
					}
					else if (diff.Y < 0)
					{
						pos += 2 * Math.PI;
					}
				}
				
				pos += 0.5 * Math.PI;
			}
			
			DrawArrow(graphics, xy.X, xy.Y, pos);
		}
		
		private void DrawStandalone(Cairo.Context graphics)
		{
			FromState(graphics, false);

			Allocation alloc = WrappedObject != null ? WrappedObject.Allocation.Copy() : new Allocation(0, 0, 1, 1);
			
			alloc.X = 0;
			alloc.Y = 0;

			alloc.GrowBorder(-graphics.LineWidth);
			
			double radius = Math.Min(alloc.Width, alloc.Height) / 2;

			double cx = alloc.X + alloc.Width / 2;
			double cy = alloc.Y + alloc.Height / 2;
			
			double angle = Math.PI * 1.8;

			graphics.NewPath();
			graphics.Arc(cx, cy, radius, 0, angle);
			graphics.Stroke();
			
			DrawArrow(graphics, cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle), angle - Math.PI);
		}
		
		public override void Draw(Cairo.Context graphics)
		{
			graphics.Save();

			if (Style == Renderer.DrawStyle.Icon)
			{
				DrawStandalone(graphics);
			}
			else if (Standalone)
			{
				DrawStandalone(graphics);
			}
			else
			{
				DrawObjects(graphics);
			}
			
			graphics.Restore();
		}

		private void DrawIcon(Cairo.Context graphics)
		{
			Allocation alloc = d_object != null ? d_object.Allocation : new Allocation(0, 0, 1, 1);
			
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
		}
	}
}

