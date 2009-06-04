using System;
using System.Collections.Generic;
using System.Drawing;

namespace Cpg.Studio.Components
{
	public class Link : Simulated
	{
		public class Action
		{
			Link d_link;
			LinkAction d_action;
			
			public Action(Link link, LinkAction action)
			{
				d_link = link;
				d_action = action;
			}
			
			public Action() : this(null, null)
			{
			}

			public string Target
			{
				get
				{
					return d_action.Target.Name;
				}
				set
				{
					d_action = d_link.UpdateAction(this, value, Equation);
				}
			}
			
			public Cpg.Property Property
			{
				get
				{
					return d_action.Target;
				}
			}
			
			public LinkAction LinkAction
			{
				get
				{
					return d_action;
				}
			}

			public string Equation
			{
				get
				{
					return d_action.Expression.AsString;
				}
				set
				{
					d_action = d_link.UpdateAction(this, d_action.Target.Name, value);
				}
			}
		}
		
		Components.Simulated d_from;
		Components.Simulated d_to;
		
		int d_offset;
		
		double[] d_selectedColor;
		double[] d_normalColor;
		double[] d_hoverColor;
		
		public Link(Cpg.Link obj, Components.Simulated from, Components.Simulated to) : base(obj)
		{
			if (from == null && obj != null)
				From = Components.Simulated.FromCpg(obj.From);
			else
				From = from;
			
			if (to == null && obj != null)
				To = Components.Simulated.FromCpg(obj.To);
			else
				To = to;
			
			d_normalColor = new double[] {0.7, 0.7, 0.7, 0.6};
			d_selectedColor = new double[] {0.6, 0.6, 1, 0.6};
			d_hoverColor = new double[] {0.3, 0.6, 0.3, 0.6};
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
				
				if (d_from != null)
				{
					d_from.Moved += delegate(object sender, EventArgs e) {
						DoRequestRedraw();
					};
				}
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
				if (d_to != null)
					d_to.Unlink(this);
					
				d_to = value;
				
				if (d_to != null)
				{
					d_to.Link(this);
				}
			}
		}
		
		public Action[] Actions
		{
			get
			{
				List<Action> actions = new List<Action>();
				
				foreach (LinkAction action in (d_object as Cpg.Link).Actions)
				{
					actions.Add(new Action(this, action));
				}
				
				return actions.ToArray();
			}
		}
		
		public Action AddAction(string property, string expression)
		{
			Cpg.Property prop = d_to.Object.Property(property);
			
			if (prop != null)
				return new Action(this, (d_object as Cpg.Link).AddAction(prop, expression));
			else
				return null;
		}
		
		public bool RemoveAction(Action action)
		{
			return (d_object as Cpg.Link).RemoveAction(action.LinkAction);
		}
		
		public Cpg.LinkAction UpdateAction(Action action, string property, string expression)
		{
			Cpg.Link link = d_object as Cpg.Link;
			Cpg.Property prop = link.To.Property(property); 

			if (prop == null)
				return action.LinkAction;
				
			if (!RemoveAction(action))
				return action.LinkAction;

			return link.AddAction(prop, expression);
		}
		
		public override void Removed()
		{
			if (d_to != null)
				d_to.Unlink(this);

			base.Removed();
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
			PointF[] points = ControlPoints();
			
			// Piece wise linearization
			int num = 5;
			List<float> dist = new List<float>();
			
			if (rect.Width * gridSize > 1.5 || rect.Height * gridSize > 1.5)
			{
				return RectHittest(points[0], points[1], points[2], points[3], rect, gridSize);
			}

			PointF prevp = points[0];

			for (int i = 1; i <= num; ++i)
			{
				PointF pt = EvaluateBezier(points[0], points[1], points[2], points[3], (float)i / num);
				dist.Add(DistanceToLine(prevp, pt, new PointF(rect.X, rect.Y)));
				
				prevp = pt;
			}

			return (Utils.Min(dist) < 10.0f / gridSize);
		}
		
		
		private double[] StateColor()
		{
			if (Selected)
			{
				return d_selectedColor;
			}
			else if (MouseFocus)
			{
				return d_hoverColor;
			}
			else
			{
				return d_normalColor;
			}
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
		
		public PointF EvaluateBezier(PointF p0, PointF p1, PointF p2, PointF p3, float t)
		{
			return new PointF(
				EvaluateBezier(p0.X, p1.X, p2.X, p3.X, t),
				EvaluateBezier(p0.Y, p1.Y, p2.Y, p3.Y, t)
			);
		}
		
		private PointF[] ControlPoints()
		{
			if (d_from == null || d_to == null)
				return null;

			Allocation a1 = d_from.Allocation;
			Allocation a2 = d_to.Allocation;
			
			PointF from = new PointF(a1.X + a1.Width / 2, a1.Y + a1.Height / 2);
			PointF to = new PointF(a2.X + a2.Width / 2, a2.Y + a2.Height / 2);
			PointF control = CalculateControl(from, to);
			
			if (from.X == to.X && from.Y == to.Y)
			{
				PointF pts = new PointF(2, (d_offset + 1) + 0.5f);

				return new PointF[] {
					from,
					new PointF(to.X - pts.X, to.Y - pts.Y),
					new PointF(to.X + pts.X, to.Y - pts.Y),
					to
				};
			}
			else
			{
				return new PointF[] {from, control, control, to};
			}
		}
		
		public override void Draw(Cairo.Context graphics)
		{			
			PointF[] points = ControlPoints();
			
			if (points == null)
				return;
			
			graphics.Save();			

			double[] color = StateColor();
			double uw = graphics.LineWidth;
			
			if (KeyFocus)
			{
				graphics.LineWidth *= 4;
				graphics.SetDash(new double[] {graphics.LineWidth, graphics.LineWidth}, 0);
			}
			else if (MouseFocus)
			{
				graphics.LineWidth *= 2;
			}

			graphics.MoveTo(points[0].X, points[0].Y);
			graphics.CurveTo(points[1].X, points[1].Y, points[2].X, points[2].Y, points[3].X, points[3].Y);
			graphics.SetSourceRGBA(color[0], color[1], color[2], color[3]);

			graphics.Stroke();
			
			PointF xy;
			float pos;
			
			float size = 0.15f;

			// Draw the arrow, first move to the center, then rotate, then draw the arrow
			xy = EvaluateBezier(points[0], points[1], points[2], points[3], 0.5f);

			if (points[0] == points[3])
			{
				pos = 1.5f * (float)Math.PI;
			}
			else
			{
				PointF diff = new PointF(points[3].X - points[0].X, points[3].Y - points[0].Y);
				
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
			
			graphics.Translate(xy.X, xy.Y);
			graphics.MoveTo((pos <= Math.PI ? -1 : 1) * size / 2, 0);
			graphics.Rotate(pos);

			graphics.RelLineTo(-size, 0);
			graphics.RelLineTo(size, -size);
			graphics.RelLineTo(size, size);
			graphics.RelLineTo(-size, 0);
			
			graphics.Fill();
			
			string s = ToString();
			
			if (s == String.Empty)
			{
				graphics.Restore();
				return;
			}
		
			graphics.Rotate(0.5 * Math.PI);
			
			if (points[0].X < points[3].X)
				graphics.Rotate(Math.PI);
		
			graphics.Scale(uw, uw);
			Pango.Layout layout = Pango.CairoHelper.CreateLayout(graphics);
			
			layout.FontDescription = Settings.Font;
			layout.SetText(s);
			
			if (MouseFocus)
				graphics.SetSourceRGB(d_hoverColor[0], d_hoverColor[1], d_hoverColor[2]);
			else
				graphics.SetSourceRGB(0.3, 0.3, 0.3);
			
			int width, height;
			
			layout.GetSize(out width, out height);
			width = (int)(width / Pango.Scale.PangoScale);
			height = (int)(height / Pango.Scale.PangoScale);
			
			graphics.MoveTo(-width / 2.0, -height * 1.5);
			
			Pango.CairoHelper.ShowLayout(graphics, layout);

			graphics.Restore();
		}

		public override Allocation Extents(float scale, Cairo.Context graphics)
		{
			PointF[] points = ControlPoints();
			
			if (points == null)
				return new Allocation(0, 0, 0, 0);
			
			List<float> xx = new List<float>();
			List<float> yy = new List<float>();
			
			for (int i = 0; i < points.Length; ++i)
			{
				xx.Add(points[i].X * scale);
				yy.Add(points[i].Y * scale);
			}
			
			float minx = Utils.Min(xx);
			float maxx = Utils.Max(xx);
			float miny = Utils.Min(yy);
			float maxy = Utils.Max(yy);
			
			int width, height;
			MeasureString(graphics, ToString(), out width, out height);

			return new Allocation(minx - height, miny - height, maxx - minx + height * 2, maxy - miny + height * 2);
		}
		
		public override bool CanIntegrate
		{
			get
			{
				return false;
			}
		}
	}
}
