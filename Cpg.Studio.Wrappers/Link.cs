using System;
using System.Collections.Generic;
using System.Drawing;

namespace Cpg.Studio.Wrappers
{
	public class Link : Wrapper
	{		
		public delegate void ActionEventHandler(object source, Cpg.LinkAction action);

		public event ActionEventHandler ActionAdded = delegate {};
		public event ActionEventHandler ActionRemoved = delegate {};
		
		private Wrappers.Wrapper d_from;
		private Wrappers.Wrapper d_to;
		
		private int d_offset;
		
		private double[] d_selectedColor;
		private double[] d_normalColor;
		private double[] d_hoverColor;
		
		private float d_arrowSize;
		
		public Link(Cpg.Link obj, Wrappers.Wrapper from, Wrappers.Wrapper to) : base(obj)
		{
			if (obj != null && from != null)
			{
				obj.From = from;
			}

			UpdateFrom();
			
			
			if (obj != null && to != null)
			{
				obj.To = to;
			}

			UpdateTo();
			
			d_normalColor = new double[] {0.7, 0.7, 0.7, 0.6};
			d_selectedColor = new double[] {0.6, 0.6, 1, 0.6};
			d_hoverColor = new double[] {0.3, 0.6, 0.3, 0.6};
			
			d_arrowSize = 0.15f;
			
			obj.AddNotification("to", OnToChanged);
			obj.AddNotification("from", OnFromChanged);
			
			obj.ActionAdded += HandleActionAdded;
			obj.ActionRemoved += HandleActionRemoved;
		}

		private void HandleActionRemoved(object o, ActionRemovedArgs args)
		{
			ActionRemoved(this, args.Action);
		}

		private void HandleActionAdded(object o, ActionAddedArgs args)
		{
			ActionAdded(this, args.Action);
		}
		
		private void UpdateFrom()
		{
			if (d_from != null)
			{
				d_from.Moved -= OnFromMoved;
			}

			d_from = WrappedObject.From;
			
			if (d_from != null)
			{
				d_from.Moved += OnFromMoved;
			}
		}
		
		private void UpdateTo()
		{
			if (d_to != null)
			{
				d_to.Unlink(this);
			}
			
			d_to = WrappedObject.To;
			
			if (d_to != null)
			{
				d_to.Link(this);
			}
		}
		
		private void OnToChanged(object source, GLib.NotifyArgs args)
		{
			UpdateTo();
		}
		
		private void OnFromChanged(object source, GLib.NotifyArgs args)
		{
			UpdateFrom();
		}
		
		private void OnFromMoved(object source, EventArgs args)
		{
			DoRequestRedraw();
		}
		
		public Link(Cpg.Link obj) : this(obj, null, null)
		{
		}
		
		public Link() : this(null, null, null)
		{
		}

		public static implicit operator Cpg.Link(Link obj)
		{
			return obj.WrappedObject;
		}
		
		public Wrappers.Wrapper[] Objects
		{
			get
			{
				if (d_object != null)
				{
					return new Wrappers.Wrapper[] {d_from, d_to};
				}
				else
				{
					return new Wrappers.Wrapper[] {};
				}
			}
		}
		
		public int Offset
		{
			get { return d_offset; }
			set { d_offset = value; }
		}
		
		public bool SameObjects(Wrappers.Link other)
		{
			return (other.d_from == d_from && other.d_to == d_to);
		}
		
		public Wrappers.Wrapper From
		{
			get
			{
				return d_from;
			}
			set
			{
				WrappedObject.From = value;
			}
		}
		
		public Wrappers.Wrapper To
		{
			get
			{
				return d_to;
			}
			set
			{
				WrappedObject.To = value;
			}
		}
		
		public Cpg.LinkAction[] Actions
		{
			get
			{
				return WrappedObject.Actions;
			}
		}
		
		public Cpg.LinkAction AddAction(string property, Cpg.Expression expression)
		{
			Cpg.Property prop = d_to[property];
			
			if (prop != null)
			{
				return WrappedObject.AddAction(prop, expression);
			}
			else
			{
				return null;
			}
		}
		
		public Cpg.LinkAction GetAction(string target)
		{
			return WrappedObject.GetAction(target);
		}
		
		public bool RemoveAction(Cpg.LinkAction action)
		{
			return WrappedObject.RemoveAction(action);
		}
		
		public new Cpg.Link WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Link;
			}
		}
		
		public void Reattach()
		{
			Attach(d_from, d_to);			
		}
		
		public void Attach(Wrappers.Wrapper from, Wrappers.Wrapper to)
		{
			WrappedObject.Attach(from, to);
		}
		
		public override void Removed()
		{
			Attach(null, null);

			base.Removed();
		}
		
		public bool Empty
		{
			get
			{
				return From == null && To == null;
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
				{
					return true;
				}
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
			float dist = 1 * d_offset;
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
			{
				return null;
			}

			Allocation a1 = d_from.Allocation;
			Allocation a2 = d_to.Allocation;
			
			PointF from = new PointF(a1.X + a1.Width / 2, a1.Y + a1.Height / 2);
			PointF to = new PointF(a2.X + a2.Width / 2, a2.Y + a2.Height / 2);
			PointF control = CalculateControl(from, to);
			
			if (from.X == to.X && from.Y == to.Y)
			{
				PointF pts = new PointF(2, (d_offset) + 0.5f);

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
			{
				return;
			}
			
			graphics.Save();			

			double[] color = StateColor();
			
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
			
			graphics.MoveTo(xy.X, xy.Y);
			graphics.Rotate(pos);
			graphics.RelMoveTo(0, (pos + 0.5 * Math.PI < Math.PI ? -1 : 1) * d_arrowSize / 2);

			graphics.RelLineTo(-d_arrowSize, 0);
			graphics.RelLineTo(d_arrowSize, -d_arrowSize);
			graphics.RelLineTo(d_arrowSize, d_arrowSize);
			graphics.RelLineTo(-d_arrowSize, 0);
			
			graphics.Fill();
			graphics.Restore();
		}

		public override Allocation Extents(float scale, Cairo.Context graphics)
		{
			PointF[] points = ControlPoints();
			
			if (points == null)
			{
				return new Allocation(0, 0, 0, 0);
			}
			
			List<float> xx = new List<float>();
			List<float> yy = new List<float>();
			
			for (int i = 0; i < points.Length; ++i)
			{
				xx.Add(points[i].X * scale);
				yy.Add(points[i].Y * scale);
			}
			
			float ssize = d_arrowSize * scale;
			float minx = Utils.Min(xx) - ssize;
			float maxx = Utils.Max(xx) + ssize;
			float miny = Utils.Min(yy) - ssize;
			float maxy = Utils.Max(yy) + ssize;

			return new Allocation(minx, miny, Math.Max(maxx - minx, ssize * 2), Math.Max(maxy - miny, ssize * 2));
		}
	}
}
