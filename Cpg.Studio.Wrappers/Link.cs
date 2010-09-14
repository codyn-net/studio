using System;
using System.Collections.Generic;

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
		private List<Point> d_fromAnchors;
		private List<Point> d_toAnchors;
		
		protected Link(Cpg.Link obj) : this(obj, null, null)
		{
		}
		
		public Link() : this(new Cpg.Link("link", null, null), null, null)
		{
		}
		
		public Link(Cpg.Link obj, Wrappers.Wrapper from, Wrappers.Wrapper to) : base(obj)
		{
			Renderer = new Renderers.Link(this);

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
			
			obj.AddNotification("to", OnToChanged);
			obj.AddNotification("from", OnFromChanged);
			
			obj.ActionAdded += HandleActionAdded;
			obj.ActionRemoved += HandleActionRemoved;
			
			CalculateAnchors();
		}
		
		protected override bool ToState(Graphical.State field, bool val)
		{
			bool ret = base.ToState(field, val);
			
			if (field == Graphical.State.LinkFocus)
			{
				if (d_from != null)
				{
					d_from.LinkFocus = val;
				}
				if (d_to != null)
				{
					d_to.LinkFocus = val;
				}
			}
			
			return ret;
		}

		public new Renderers.Link Renderer
		{
			get
			{
				return (Renderers.Link)base.Renderer;
			}
			set
			{
				base.Renderer = value;
			}
		}

		private void HandleActionRemoved(object o, ActionRemovedArgs args)
		{
			ActionRemoved(this, args.Action);
		}

		private void HandleActionAdded(object o, ActionAddedArgs args)
		{
			ActionAdded(this, args.Action);
		}
		
		private void RecalculateLinkOffsets(Wrappers.Wrapper o1, Wrappers.Wrapper o2)
		{
			if (o1 == null || o2 == null)
			{
				return;
			}
			
			// See how many links there are between o1 and o2
			List<Wrappers.Link> d1 = new List<Wrappers.Link>();
			
			// From o1 to o2
			foreach (Wrappers.Link l in o2.Links)
			{
				if (l.From == o1)
				{
					d1.Add(l);
				}
			}
			
			List<Wrappers.Link> d2 = new List<Wrappers.Link>();
			
			// From o2 to o1
			foreach (Wrappers.Link l in o1.Links)
			{
				if (l.From == o2)
				{
					d2.Add(l);
				}
			}
			
			int baseOffset = (d1.Count == 0 || d2.Count == 0) ? 0 : 1;
			
			for (int i = 0; i < d1.Count; ++i)
			{
				d1[i].Offset = i + baseOffset;
			}
			
			for (int i = 0; i < d2.Count; ++i)
			{
				d2[i].Offset = i + baseOffset;
			}
		}
		
		private void UpdateFrom()
		{
			if (d_from != null)
			{
				d_from.Moved -= OnFromMoved;
			}
			
			Wrappers.Wrapper oldFrom = d_from;
			d_from = WrappedObject.From;
			
			RecalculateLinkOffsets(oldFrom, d_to);
			
			if (d_from != null)
			{
				d_from.Moved += OnFromMoved;
				
				RecalculateLinkOffsets(d_from, d_to);
				
				d_from.LinkFocus = LinkFocus;
			}
		}
		
		private void UpdateTo()
		{
			if (d_to != null)
			{
				d_to.Moved -= OnToMoved;
				d_to.Unlink(this);
			}
			
			Wrappers.Wrapper oldTo = d_to;			
			d_to = WrappedObject.To;
			
			RecalculateLinkOffsets(d_from, oldTo);
			
			if (d_to != null)
			{
				d_to.Moved += OnToMoved;
				d_to.Link(this);
				
				RecalculateLinkOffsets(d_from, d_to);
				
				d_to.LinkFocus = LinkFocus;
			}
		}
		
		private void OnToChanged(object source, GLib.NotifyArgs args)
		{
			UpdateTo();
			CalculateAnchors();
		}
		
		private void OnFromChanged(object source, GLib.NotifyArgs args)
		{
			UpdateFrom();
			CalculateAnchors();
		}
		
		private void OnToMoved(object source, EventArgs args)
		{
			CalculateAnchors();
		}
		
		private void OnFromMoved(object source, EventArgs args)
		{
			DoRequestRedraw();
			CalculateAnchors();
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
			get
			{
				return d_offset;
			}
			set
			{
				d_offset = value;

				Renderer.ResetCache();
				CalculateAnchors();
			}
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
		
		public Cpg.LinkAction AddAction(string target, Cpg.Expression expression)
		{
			Cpg.LinkAction action = new Cpg.LinkAction(target, expression);
			
			if (AddAction(action))
			{
				return action;
			}
			else
			{
				return null;
			}
		}
		
		public bool AddAction(Cpg.LinkAction action)
		{
			return WrappedObject.AddAction(action);
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
				return From == null || To == null;
			}
		}

		private bool RectHittest(Point p1, Point p2, Point p3, Point p4, Allocation rect, double gridSize)
		{
			Allocation other = new Allocation(0, 0, 1.0 / gridSize, 1.0 / gridSize);
			
			for (int i = 0; i < 5; ++i)
			{
				other.X = Renderers.Link.EvaluateBezier(p1.X, p2.X, p3.X, p4.X, i / 5.0);
				other.Y = Renderers.Link.EvaluateBezier(p1.Y, p2.Y, p3.Y, p4.Y, i / 5.0);
				
				if (rect.Intersects(other))
				{
					return true;
				}
			}
			
			return false;
		}
		
		private double EvaluatePolynomial(Point[] polys, double t, int idx)
		{
			return polys[0][idx] * t * t * t +
			       polys[1][idx] * t * t +
			       polys[2][idx] * t +
			       polys[3][idx];
		}
		
		private void CalculateAnchor(Point[] polys, int idx, double x, double y1, double y2, bool isFrom)
		{
			// Find roots at f(t) = x => f(t) - x = 0
			Biorob.Math.Solvers.Cubic fx = new Biorob.Math.Solvers.Cubic(
				polys[0][idx],
				polys[1][idx],
				polys[2][idx],
				polys[3][idx] - x
			);
			
			foreach (double t in fx.Roots)
			{
				// Check if t is within 0 -> 1
				if (t < 0 || t > 1 || (isFrom != (t < 0.5)))
				{
					continue;
				}
				
				// Check if the intersection at !idx is within y1 -> y2
				double val = EvaluatePolynomial(polys, t, idx == 0 ? 1 : 0);
				
				if (val >= y1 && val <= y2)
				{
					Point point = new Point();

					point[idx] = x;
					point[idx == 0 ? 1 : 0] = val;
					
					if (isFrom)
					{
						d_fromAnchors.Add(point);
					}
					else
					{
						d_toAnchors.Add(point);
					}
				}
			}
		}
		
		private List<Point> CalculateAnchors(Point[] polys, bool isFrom)
		{
			List<Point> ret = new List<Point>();
			Wrappers.Wrapper obj;
			
			if (isFrom)
			{
				obj = d_from;
			}
			else
			{
				obj = d_to;
			}

			Allocation alloc = obj.Allocation;
			
			CalculateAnchor(polys, 0, alloc.X, alloc.Y, alloc.Y + alloc.Height, isFrom);
			CalculateAnchor(polys, 0, alloc.X + alloc.Height, alloc.Y, alloc.Y + alloc.Height, isFrom);
			CalculateAnchor(polys, 1, alloc.Y, alloc.X, alloc.X + alloc.Width, isFrom);
			CalculateAnchor(polys, 1, alloc.Y + alloc.Height, alloc.X, alloc.X + alloc.Width, isFrom);
			
			return ret;
		}
		
		public List<Point> FromAnchors
		{
			get
			{
				return d_fromAnchors;
			}
		}
		
		public List<Point> ToAnchors
		{
			get
			{
				return d_toAnchors;
			}
		}
		
		private void CalculateAnchors()
		{
			Point[] polys = Renderer.PolynomialControlPoints();
			
			d_fromAnchors = new List<Point>();
			d_toAnchors = new List<Point>();
			
			if (polys == null)
			{
				return;
			}
			
			// Calculate intersection points for from and to at each of the four sides of the alloc rect
			d_fromAnchors.AddRange(CalculateAnchors(polys, true));
			d_toAnchors.AddRange(CalculateAnchors(polys, false));
		}
		
		public bool HitTest(Allocation rect, int gridSize)
		{
			Point[] points = Renderer.ControlPoints();
			
			if (points == null)
			{
				return Allocation.Intersects(rect) && base.HitTest(rect);
			}
			
			// Piece wise linearization
			int num = 5;
			List<double> dist = new List<double>();
			
			if (rect.Width * gridSize > 1.5 || rect.Height * gridSize > 1.5)
			{
				return RectHittest(points[0], points[1], points[2], points[3], rect, gridSize);
			}

			Point prevp = points[0];

			for (int i = 1; i <= num; ++i)
			{
				Point pt = Renderers.Link.EvaluateBezier(points[0], points[1], points[2], points[3], (double)i / num);
				dist.Add(Renderers.Link.DistanceToLine(prevp, pt, new Point(rect.X, rect.Y)));
				
				prevp = pt;
			}

			return (Utils.Min(dist) < 10.0f / gridSize);
		}
		
		protected override string Label
		{
			get
			{
				if (Empty)
				{
					return base.Label;
				}
				else
				{
					return null;
				}
			}
		}

		public override Allocation Extents(double scale, Cairo.Context graphics)
		{
			if (Empty)
			{
				return base.Extents(scale, graphics);
			}
			
			Point[] points = Renderer.ControlPoints();
			
			List<double> xx = new List<double>();
			List<double> yy = new List<double>();
			
			for (int i = 0; i < points.Length; ++i)
			{
				xx.Add(points[i].X * scale);
				yy.Add(points[i].Y * scale);
			}
			
			double ssize = Renderers.Link.ArrowSize * scale;
			double minx = Utils.Min(xx) - ssize;
			double maxx = Utils.Max(xx) + ssize;
			double miny = Utils.Min(yy) - ssize;
			double maxy = Utils.Max(yy) + ssize;

			return new Allocation(minx, miny, Math.Max(maxx - minx, ssize * 2), Math.Max(maxy - miny, ssize * 2));
		}
		
		protected override void DrawSelection(Cairo.Context graphics)
		{
			// Handled by renderer
		}
		
		protected override void DrawFocus(Cairo.Context graphics)
		{
			// Handled by renderer
		}
	}
}
