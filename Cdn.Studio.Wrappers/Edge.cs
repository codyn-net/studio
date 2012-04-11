using System;
using System.Collections.Generic;
using Biorob.Math;

namespace Cdn.Studio.Wrappers
{
	public class Edge : Object
	{		
		public delegate void ActionEventHandler(object source,Cdn.EdgeAction action);

		public event ActionEventHandler ActionAdded = delegate {};
		public event ActionEventHandler ActionRemoved = delegate {};
		
		private Wrappers.Node d_input;
		private Wrappers.Node d_output;
		private int d_offset;
		private List<Point> d_fromAnchors;
		private List<Point> d_toAnchors;
		
		protected Edge(Cdn.Edge obj) : this(obj, null, null)
		{
		}
		
		public Edge() : this(new Cdn.Edge("edge", null, null), null, null)
		{
		}
		
		public Edge(Cdn.Edge obj, Wrappers.Wrapper from, Wrappers.Wrapper to) : base(obj)
		{
			Renderer = new Renderers.Edge(this);

			if (obj != null && from != null)
			{
				obj.Input = from;
			}

			UpdateFrom();
			
			if (obj != null && to != null)
			{
				obj.Output = to;
			}

			UpdateTo();
			
			obj.AddNotification("output", OnToChanged);
			obj.AddNotification("input", OnFromChanged);
			
			obj.ActionAdded += HandleActionAdded;
			obj.ActionRemoved += HandleActionRemoved;
			
			CalculateAnchors();
		}

		public static implicit operator Cdn.Object(Edge obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator Cdn.Edge(Edge obj)
		{
			return obj.WrappedObject;
		}
		
		protected override bool ToState(Graphical.State field, bool val)
		{
			bool ret = base.ToState(field, val);
			
			if (field == Graphical.State.LinkFocus)
			{
				if (d_input != null)
				{
					d_input.LinkFocus = val;
				}
				if (d_output != null)
				{
					d_output.LinkFocus = val;
				}
			}
			
			return ret;
		}

		public new Renderers.Edge Renderer
		{
			get
			{
				return (Renderers.Edge)base.Renderer;
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
			List<Wrappers.Edge> d1 = new List<Wrappers.Edge>();
			
			// From o1 to o2
			foreach (Wrappers.Edge l in o2.Links)
			{
				if (l.Input == o1)
				{
					d1.Add(l);
				}
			}
			
			List<Wrappers.Edge> d2 = new List<Wrappers.Edge>();
			
			// From o2 to o1
			foreach (Wrappers.Edge l in o1.Links)
			{
				if (l.Input == o2)
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
			if (d_input != null)
			{
				d_input.Moved -= OnFromMoved;
			}
			
			Wrappers.Node oldFrom = d_input;
			d_input = Wrappers.Wrapper.Wrap(WrappedObject.Input as Cdn.Node) as Wrappers.Node;
			
			RecalculateLinkOffsets(oldFrom, d_output);
			
			if (d_input != null)
			{
				d_input.Moved += OnFromMoved;
				
				RecalculateLinkOffsets(d_input, d_output);
				
				d_input.LinkFocus = LinkFocus;
				
				if (d_output != null)
				{
					Allocation.Assign(0, 0, 1, 1);
				}
			}
		}
		
		private void UpdateTo()
		{
			if (d_output != null)
			{
				d_output.Moved -= OnToMoved;
				d_output.Unlink(this);
			}
			
			Wrappers.Node oldTo = d_output;
			d_output = Wrappers.Wrapper.Wrap(WrappedObject.Output as Cdn.Node) as Wrappers.Node;
			
			RecalculateLinkOffsets(d_input, oldTo);
			
			if (d_output != null)
			{
				d_output.Moved += OnToMoved;
				d_output.Link(this);
				
				RecalculateLinkOffsets(d_input, d_output);
				
				d_output.LinkFocus = LinkFocus;
				
				if (d_input != null)
				{
					Allocation.Assign(0, 0, 1, 1);
				}
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
		
		public Wrappers.Wrapper[] Objects
		{
			get
			{
				if (d_object != null)
				{
					return new Wrappers.Wrapper[] {d_input, d_output};
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
		
		public bool SameObjects(Wrappers.Edge other)
		{
			return (other.d_input == d_input && other.d_output == d_output);
		}
		
		public Wrappers.Node Input
		{
			get
			{
				return d_input;
			}
			set
			{
				WrappedObject.Input = value;
			}
		}
		
		public Wrappers.Node Output
		{
			get
			{
				return d_output;
			}
			set
			{
				WrappedObject.Output = value;
			}
		}
		
		public Cdn.EdgeAction[] Actions
		{
			get
			{
				return WrappedObject.Actions;
			}
		}
		
		public Cdn.EdgeAction AddAction(string target, Cdn.Expression expression)
		{
			Cdn.EdgeAction action = new Cdn.EdgeAction(target, expression);
			
			if (AddAction(action))
			{
				return action;
			}
			else
			{
				return null;
			}
		}
		
		public bool AddAction(Cdn.EdgeAction action)
		{
			return WrappedObject.AddAction(action);
		}
		
		public Cdn.EdgeAction GetAction(string target)
		{
			return WrappedObject.GetAction(target);
		}
		
		public bool RemoveAction(Cdn.EdgeAction action)
		{
			return WrappedObject.RemoveAction(action);
		}
		
		public new Cdn.Edge WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.Edge;
			}
		}
		
		public void Reattach()
		{
			Attach(d_input, d_output);
		}
		
		public void Attach(Wrappers.Node from, Wrappers.Node to)
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
				return Input == null || Output == null;
			}
		}

		private bool RectHittest(Point p1, Point p2, Point p3, Point p4, Allocation rect, double gridSize)
		{
			Allocation other = new Allocation(0, 0, 1.0 / gridSize, 1.0 / gridSize);
			
			for (int i = 0; i < 5; ++i)
			{
				other.X = Renderers.Edge.EvaluateBezier(p1.X, p2.X, p3.X, p4.X, i / 5.0);
				other.Y = Renderers.Edge.EvaluateBezier(p1.Y, p2.Y, p3.Y, p4.Y, i / 5.0);
				
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
				obj = d_input;
			}
			else
			{
				obj = d_output;
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
				Point pt = Renderers.Edge.EvaluateBezier(points[0], points[1], points[2], points[3], (double)i / num);
				dist.Add(Renderers.Edge.DistanceToLine(prevp, pt, new Point(rect.X, rect.Y)));
				
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
			
			double ssize = Renderers.Edge.ArrowSize * scale;
			double minx = Utils.Min(xx) - ssize;
			double maxx = Utils.Max(xx) + ssize;
			double miny = Utils.Min(yy) - ssize;
			double maxy = Utils.Max(yy) + ssize;

			return new Allocation(minx, miny, System.Math.Max(maxx - minx, ssize * 2), System.Math.Max(maxy - miny, ssize * 2));
		}
		
		protected override void DrawSelection(Cairo.Context graphics)
		{
			// Handled by renderer
		}
		
		protected override void DrawFocus(Cairo.Context graphics)
		{
			// Handled by renderer
		}
		
		public override bool CanDrawAnnotation(Cairo.Context context)
		{
			if (d_input == null || d_output == null)
			{
				return base.CanDrawAnnotation(context);
			}
			else if (d_input == d_output)
			{
				return d_input.CanDrawAnnotation(context);
			}
			
			double fx = context.Matrix.Xx * d_input.Allocation.X;
			double tx = context.Matrix.Xx * d_output.Allocation.X;
			double fy = context.Matrix.Yy * d_input.Allocation.Y;
			double ty = context.Matrix.Yy * d_output.Allocation.Y;
			
			double dist = System.Math.Sqrt(System.Math.Pow(fx - tx, 2) + System.Math.Pow(fy - ty, 2));
			return dist > 2 * RenderAnnotationAtsize;
		}
		
		public override void AnnotationHotspot(Cairo.Context context, double width, double height, int size, out double x, out double y)
		{
			if (d_input == null || d_output == null)
			{
				base.AnnotationHotspot(context, width, height, size, out x, out y);
			}
			else
			{
				Point[] polys = Renderer.PolynomialControlPoints();
				
				x = EvaluatePolynomial(polys, 0.4, 0) * context.Matrix.Xx - size / 2;
				y = EvaluatePolynomial(polys, 0.4, 1) * context.Matrix.Yy - size / 2;
			}
		}
		
		public Wrappers.Edge GetActionTemplate(Cdn.EdgeAction action, bool match_full)
		{
			return (Wrappers.Edge)Wrapper.Wrap(WrappedObject.GetActionTemplate(action, match_full));
		}
	}
}
