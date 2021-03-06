using System;
using Biorob.Math;

namespace Cdn.Studio.Wrappers.Renderers
{
	// Only used for generating icons for now
	public class Edge : Renderer
	{
		private static double[] s_normalColor;
		private static double[] s_selectedColor;
		private static double[] s_selectedAltColor;
		private static double[] s_hoverColor;
		private static double[] s_iconColor;
		private static double[] s_linkColor;
		private static double s_arrowSize;
		private Point[] d_controlPoints;
		private Allocation d_prevFrom;
		private Allocation d_prevTo;

		static Edge()
		{
			s_normalColor = new double[] {0.7, 0.7, 0.7, 0.6};
			s_selectedColor = new double[] {0.6, 0.6, 1, 0.6};
			s_selectedAltColor = new double[] {1, 0.6, 0.6, 0.6};
			s_hoverColor = new double[] {0.3, 0.6, 0.3, 0.6};
			s_iconColor = new double[] {0.5, 0.5, 0.5, 1.0};
			s_linkColor = new double[] {0.9, 0.9, 0.1, 1.0};
			
			s_arrowSize = 0.15;
		}
		
		public static double ArrowSize
		{
			get
			{
				return s_arrowSize;
			}
		}
		
		public Edge() : base()
		{
		}

		public Edge(Wrappers.Wrapper obj) : base(obj)
		{
		}
		
		public new Wrappers.Edge WrappedObject
		{
			get
			{
				return (Wrappers.Edge)base.WrappedObject;
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
			else if (WrappedObject.LinkFocus)
			{
				return s_linkColor;
			}
			else if (WrappedObject.SelectedAlt)
			{
				return s_selectedAltColor;
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
		
		private static Point CalculateControl(Point fr, Point to, int offset)
		{
			Point diff = new Point(to.X - fr.X, to.Y - fr.Y);
			Point point = new Point(fr.X + diff.X / 2, fr.Y + diff.Y / 2);
			
			bool same = (diff.X == 0 && diff.Y == 0);
			
			// Offset perpendicular
			double dist = 1 * offset;
			double alpha = same ? 0 : System.Math.Atan(diff.X / -diff.Y);
			
			if (diff.Y >= 0)
			{
				alpha += System.Math.PI;
			}
		
			return new Point(point.X + System.Math.Cos(alpha) * dist, point.Y + System.Math.Sin(alpha) * dist);
		}
		
		public static double EvaluateBezier(double p0, double p1, double p2, double p3, double t)
		{
			return System.Math.Pow(1 - t, 3) * p0 + 3 * t * System.Math.Pow(1 - t, 2) * p1 + 3 * System.Math.Pow(t, 2) * (1 - t) * p2 + System.Math.Pow(t, 3) * p3;
		}
		
		public static Point EvaluateBezier(Point p0, Point p1, Point p2, Point p3, double t)
		{
			return new Point(
				EvaluateBezier(p0.X, p1.X, p2.X, p3.X, t),
				EvaluateBezier(p0.Y, p1.Y, p2.Y, p3.Y, t)
			);
		}
		
		public void ResetCache()
		{
			d_prevFrom = null;
			d_prevTo = null;
		}
		
		public Point[] PolynomialControlPoints()
		{
			Point[] ret = ControlPoints();
			
			if (ret == null)
			{
				return null;
			}
			
			return new Point[] {
				new Point(-ret[0].X + 3 * ret[1].X - 3 * ret[2].X + ret[3].X,
				          -ret[0].Y + 3 * ret[1].Y - 3 * ret[2].Y + ret[3].Y),
				new Point(3 * ret[0].X - 6 * ret[1].X + 3 * ret[2].X,
				          3 * ret[0].Y - 6 * ret[1].Y + 3 * ret[2].Y),
				new Point(-3 * ret[0].X + 3 * ret[1].X,
				          -3 * ret[0].Y + 3 * ret[1].Y),
				new Point(ret[0].X, ret[0].Y)
			};
		}
		
		public static Point[] ControlPoints(Allocation a1, Allocation a2, int offset)
		{
			Point fr = new Point(a1.X + a1.Width / 2, a1.Y + a1.Height / 2);
			Point to = new Point(a2.X + a2.Width / 2, a2.Y + a2.Height / 2);

			Point control = CalculateControl(fr, to, offset);
			
			if (fr.X == to.X && fr.Y == to.Y)
			{
				Point pts = new Point(2, offset + 0.5f);

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
		
		public Point[] ControlPoints()
		{
			if (WrappedObject.Input == null || WrappedObject.Output == null)
			{
				return null;
			}

			Allocation a1 = WrappedObject.Input.Allocation;
			Allocation a2 = WrappedObject.Output.Allocation;
			
			if (d_prevFrom != null && d_prevTo != null && a1.Equals(d_prevFrom) && a2.Equals(d_prevTo))
			{
				return d_controlPoints;
			}
			
			d_prevFrom = a1.Copy();
			d_prevTo = a2.Copy();

			d_controlPoints = ControlPoints(a1, a2, WrappedObject.Offset);
			return d_controlPoints;
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
			
			return System.Math.Sqrt((point.X - res.X) * (point.X - res.X) + (point.Y - res.Y) * (point.Y - res.Y));
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
			
			if (WrappedObject != null && WrappedObject.LinkFocus)
			{
				graphics.LineWidth *= 2;
			}
			else if (WrappedObject != null && WrappedObject.KeyFocus)
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
				if (WrappedObject.Selected)
				{
					graphics.LineWidth *= 4;
				}
				else
				{
					graphics.LineWidth *= 3;
				}
			}
			else if (WrappedObject != null && WrappedObject.Selected)
			{
				graphics.LineWidth *= 3;
			}
		}
		
		private static void DrawArrow(Cairo.Context graphics, double x, double y, double pos)
		{
			graphics.MoveTo(x, y);
			graphics.Rotate(pos);
			graphics.RelMoveTo(0, (pos + 0.5 * System.Math.PI < System.Math.PI ? -1 : 1) * s_arrowSize / 2);

			graphics.RelLineTo(-s_arrowSize, 0);
			graphics.RelLineTo(s_arrowSize, -s_arrowSize);
			graphics.RelLineTo(s_arrowSize, s_arrowSize);
			graphics.RelLineTo(-s_arrowSize, 0);
			
			graphics.Fill();
		}
		
		private static void Draw(Cairo.Context graphics, Point[] points)
		{
			graphics.MoveTo(points[0].X, points[0].Y);
			graphics.CurveTo(points[1].X, points[1].Y, points[2].X, points[2].Y, points[3].X, points[3].Y);

			graphics.Stroke();
			
			Point xy;
			double pos;
			
			// Draw the arrow, first move to the center, then rotate, then draw the arrow
			xy = EvaluateBezier(points[0], points[1], points[2], points[3], 0.5);
			
			if (points[0].Equals(points[3]))
			{
				pos = 0.5 * System.Math.PI;
			}
			else
			{
				Point diff = new Point(points[3].X - points[0].X, points[3].Y - points[0].Y);
				
				if (diff.X == 0)
				{
					pos = (diff.Y < 0 ? 1.5 : 0.5) * System.Math.PI; 
				}
				else
				{
					pos = System.Math.Atan(diff.Y / diff.X);
					
					if (diff.X < 0)
					{
						pos += System.Math.PI;
					}
					else if (diff.Y < 0)
					{
						pos += 2 * System.Math.PI;
					}
				}
				
				pos += 0.5 * System.Math.PI;
			}
			
			DrawArrow(graphics, xy.X, xy.Y, pos);
		}
		
		public static void Draw(Cairo.Context graphics, Allocation a1, Allocation a2, int offset)
		{
			Draw(graphics, ControlPoints(a1, a2, offset));
		}
		
		private void DrawObjects(Cairo.Context graphics)
		{
			FromState(graphics, true);
			Draw(graphics, ControlPoints());
		}
		
		private void DrawStandalone(Cairo.Context graphics)
		{
			FromState(graphics, false);

			Allocation alloc = WrappedObject != null ? WrappedObject.Allocation.Copy() : new Allocation(0, 0, 1, 1);
			
			alloc.X = 0;
			alloc.Y = 0;

			alloc.GrowBorder(-graphics.LineWidth);
			
			double radius = System.Math.Min(alloc.Width, alloc.Height) / 2;

			double cx = alloc.X + alloc.Width / 2;
			double cy = alloc.Y + alloc.Height / 2;
			
			double angle = System.Math.PI * 1.8;

			graphics.NewPath();
			graphics.Arc(cx, cy, radius, 0, angle);
			graphics.Stroke();
			
			DrawArrow(graphics, cx + radius * System.Math.Cos(angle), cy + radius * System.Math.Sin(angle), angle - System.Math.PI);
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

