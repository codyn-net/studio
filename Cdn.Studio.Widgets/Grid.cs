using System;
using System.Collections.Generic;
using Gtk;
using System.Reflection;
using Biorob.Math;

namespace Cdn.Studio.Widgets
{
	public class Grid : DrawingArea
	{
		public static int DefaultZoom = 50;
		public static int MaxZoom = 160;
		public static int MinZoom = 10;
		private static double AnchorRadius = 0.1;

		public delegate void ObjectEventHandler(object source,Wrappers.Wrapper obj);

		public delegate void PopupEventHandler(object source,int button,long time);

		public delegate void ErrorHandler(object source,string error,string message);

		public delegate void StatusHandler(object source,string status);
		
		public event ObjectEventHandler Activated = delegate {};
		public event PopupEventHandler Popup = delegate {};
		public event EventHandler SelectionChanged = delegate {};
		public event EventHandler ModifiedView = delegate {};
		public event ErrorHandler Error = delegate {};
		public event EventHandler Cleared = delegate {};
		public event ObjectEventHandler ActiveNodeChanged = delegate {};
		public event StatusHandler Status = delegate {};
		
		private Allocation d_mouseRect;
		private System.Drawing.Point d_origPosition;
		private Point d_buttonPress;
		private Point d_dragState;
		private bool d_isDragging;
		private Dictionary<Wrappers.Wrapper, Wrappers.Wrapper.State> d_beforeDragSelection;
		private Wrappers.Wrapper d_focus;
		private Wrappers.Network d_network;
		private Actions d_actions;
		private List<Wrappers.Wrapper> d_hover;
		private Wrappers.Node d_activeNode;
		private List<Wrappers.Wrapper> d_selection;
		private double[] d_gridBackground;
		private double[] d_gridLine;
		private RenderCache d_gridCache;
		private Anchor d_anchor;
		private bool d_isDraggingAnchor;
		private Point d_dragAnchorState;
		private Wrappers.Node d_anchorDragHit;
		private Wrappers.Wrapper d_hiddenLink;
		private bool d_drawLeftBorder;
		private bool d_drawRightBorder;
		private Pango.Layout d_annotationLayout;
		
		private enum SelectionState
		{
			All,
			StatesOnly,
			LinksOnly,
			Num
		}
		
		private SelectionState d_selectionState;
		
		public Grid(Wrappers.Network network, Actions actions) : base()
		{
			AddEvents((int)(Gdk.EventMask.Button1MotionMask |
					  Gdk.EventMask.Button3MotionMask |
					  Gdk.EventMask.ButtonPressMask |
					  Gdk.EventMask.PointerMotionMask |
					  Gdk.EventMask.ButtonReleaseMask |
					  Gdk.EventMask.KeyPressMask |
					  Gdk.EventMask.KeyReleaseMask |
					  Gdk.EventMask.LeaveNotifyMask |
			          Gdk.EventMask.EnterNotifyMask));

			CanFocus = true;

			d_network = network;
			d_actions = actions;

			SelectionChanged += OnSelectionChanged;
			
			d_focus = null;
			
			d_gridBackground = new double[] {1, 1, 1};
			d_gridLine = new double[] {0.95, 0.95, 0.95};

			d_hover = new List<Wrappers.Wrapper>();
			d_selection = new List<Wrappers.Wrapper>();
			d_mouseRect = new Allocation(0, 0, -1, -1);
			d_beforeDragSelection = null;
			
			d_buttonPress = new Point();
			d_dragState = new Point();
			
			d_isDraggingAnchor = false;
			
			d_gridCache = new RenderCache(1);
		
			Clear();
		}
		
		public bool DrawLeftBorder
		{
			get
			{
				return d_drawLeftBorder;
			}
			set
			{
				d_drawLeftBorder = value;
				QueueDraw();
			}
		}
		
		public bool DrawRightBorder
		{
			get
			{
				return d_drawRightBorder;
			}
			set
			{
				d_drawRightBorder = value;
				QueueDraw();
			}
		}

		public void Loaded()
		{
			UpdateAnnotation();
		}
		
		public void Clear()
		{
			SetActiveNode(d_network);

			QueueDraw();

			d_selection.Clear();
			
			SelectionChanged(this, new EventArgs());
			Cleared(this, new EventArgs());
		}
		
		public Wrappers.Wrapper[] Selection
		{
			get
			{
				return d_selection.ToArray();
			}
		}
		
		public Wrappers.Node ActiveNode
		{
			get
			{
				return d_activeNode;
			}
			set
			{
				SetActiveNode(value);
			}
		}

		private void UpdateAnnotation()
		{
			if (d_activeNode == null)
			{
				if (d_annotationLayout != null)
				{
					d_annotationLayout = null;
					QueueDraw();
				}
			}
			else
			{
				var an = d_activeNode.WrappedObject as Annotatable;

				if (an == null || String.IsNullOrEmpty(an.Annotation))
				{
					if (d_annotationLayout != null)
					{
						d_annotationLayout = null;
						QueueDraw();
					}
				}
				else if (d_annotationLayout == null || d_annotationLayout.Text != an.Annotation)
				{
					d_annotationLayout = CreatePangoLayout(an.Annotation);
					QueueDraw();
				}
			}
		}
		
		private System.Drawing.Point Position
		{
			get
			{
				return new System.Drawing.Point((int)ActiveNode.X, (int)ActiveNode.Y);
			}
		}
		
		public int ZoomLevel
		{
			get
			{
				return d_activeNode != null ? d_activeNode.Zoom : DefaultZoom;
			}
			set
			{
				int clipped = System.Math.Max(System.Math.Min(value, MaxZoom), MinZoom);

				if (d_activeNode != null && d_activeNode.Zoom != clipped)
				{
					d_activeNode.Zoom = clipped;
					
					ModifiedView(this, new EventArgs());
					QueueDraw();
				}
			}
		}
		
		public int[] Center
		{
			get
			{
				int cx = (int)System.Math.Round((ActiveNode.X + Allocation.Width / 2.0) / (double)ZoomLevel);
				int cy = (int)System.Math.Round((ActiveNode.Y + Allocation.Height / 2.0) / (double)ZoomLevel);
				
				return new int[] {cx, cy};
			}
		}
		
		public List<Wrappers.Edge> Links
		{
			get
			{
				List<Wrappers.Edge> res = new List<Wrappers.Edge>();
				
				foreach (Wrappers.Wrapper obj in ActiveNode.Children)
				{
					if (obj is Wrappers.Edge)
					{
						res.Add(obj as Wrappers.Edge);
					}
				}
				
				return res;
			}
		}
		
		public void Unselect(Wrappers.Wrapper obj)
		{
			if (obj == null || !d_selection.Remove(obj))
			{
				return;
			}
			
			obj.Selected = false;

			SelectionChanged(this, new EventArgs());
		}
		
		private void SetActiveNode(Wrappers.Node grp)
		{
			if (d_activeNode == grp)
			{
				return;
			}
			
			if (d_activeNode != null)
			{
				d_activeNode.ChildAdded -= HandleActiveNodeChildAdded;
				d_activeNode.ChildRemoved -= HandleActiveNodeChildRemoved;
				
				foreach (Wrappers.Wrapper child in d_activeNode.Children)
				{
					Disconnect(child);
				}
			}

			if (grp != null)
			{
				grp.ChildAdded += HandleActiveNodeChildAdded;
				grp.ChildRemoved += HandleActiveNodeChildRemoved;
				
				foreach (Wrappers.Wrapper child in grp.Children)
				{
					Connect(child);
				}
			}
			
			UnselectAll();

			Wrappers.Node prev = d_activeNode;
			d_activeNode = grp;

			UpdateAnnotation();
			QueueDraw();
			
			ActiveNodeChanged(this, prev);
		}
		
		private void Connect(Wrappers.Wrapper child)
		{
			child.RequestRedraw += OnRequestRedraw;
		}
		
		private void Disconnect(Wrappers.Wrapper child)
		{
			child.RequestRedraw -= OnRequestRedraw;
		}

		private void HandleActiveNodeChildRemoved(Wrappers.Node source, Wrappers.Wrapper child)
		{
			Disconnect(child);

			Unselect(child);			
			QueueDraw();
		}

		private void HandleActiveNodeChildAdded(Wrappers.Node source, Wrappers.Wrapper child)
		{
			Connect(child);

			QueueDraw();
		}
		
		public void Select(Wrappers.Wrapper obj)
		{
			Select(obj, false);
		}
		
		public void Select(Wrappers.Wrapper obj, bool alt)
		{
			if (obj.Parent == null)
			{
				return;
			}
			
			if (obj.Parent is Wrappers.Network)
			{
				Wrappers.Network net = obj.Parent as Wrappers.Network;
				
				if (obj == net.TemplateNode)
				{
					return;
				}
			}

			if (d_selection.Contains(obj))
			{
				if (alt != obj.SelectedAlt)
				{
					if (!alt)
					{
						obj.SelectedAlt = false;
						obj.Selected = true;
					}
					else
					{
						obj.Selected = false;
						obj.SelectedAlt = true;
					}
				}

				return;
			}

			ScrollInView(obj);
			
			d_selection.Add(obj);
			
			if (alt)
			{
				obj.SelectedAlt = true;
			}
			else
			{
				obj.Selected = true;
			}

			SelectionChanged(this, new EventArgs());
		}
		
		/*public bool Select(Wrappers.Wrapper obj)
		{
			if (d_selection.IndexOf(obj) != -1)
			{
				return true;
			}
			
			d_selection.Add(obj);
			obj.Selected = true;
			
			obj.DoRequestRedraw();
			SelectionChanged(this, new EventArgs());
		}*/
		
		private void QueueDrawObject(Wrappers.Wrapper obj)
		{
			if (!ActiveNode.Contains(obj))
			{
				return;
			}
			
			using (Cairo.Context graphics = Gdk.CairoHelper.Create(GdkWindow))
			{
				Allocation prev = obj.LastExtents;
				Allocation alloc = obj.Extents(ZoomLevel, graphics);
				
				if (prev != null)
				{
					alloc = alloc.Extend(prev);
				}

				alloc.Round();
				
				alloc.Offset(-ActiveNode.X, -ActiveNode.Y);
				QueueDrawArea((int)alloc.X, (int)alloc.Y, (int)alloc.Width, (int)alloc.Height);				
				
				((IDisposable)graphics.Target).Dispose();
			}
		}
		
		delegate double ScaledPredicate(double val);
		
		private double Scaled(double pos, ScaledPredicate predicate)
		{
			return (predicate != null ? predicate(pos / ZoomLevel) : pos / ZoomLevel);
		}

		private double Scaled(double pos)
		{
			return Scaled(pos, null);
		}
		
		private Point ScaledPosition(Point position, ScaledPredicate predicate)
		{
			return new Point(Scaled(position.X + ActiveNode.X, predicate), Scaled(position.Y + ActiveNode.Y, predicate));
		}
				
		private Point ScaledPosition(double x, double y, ScaledPredicate predicate)
		{
			return ScaledPosition(new Point(x, y), predicate);
		}
		
		private Point ScaledPosition(double x, double y)
		{
			return ScaledPosition(x, y, null);
		}
		
		private Wrappers.Wrapper[] SortedObjects()
		{
			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>();
			List<Wrappers.Wrapper> links = new List<Wrappers.Wrapper>();
			
			foreach (Wrappers.Wrapper obj in ActiveNode.Children)
			{
				if (obj is Wrappers.Edge)
				{
					links.Add(obj);
				}
				else
				{
					objects.Add(obj);
				}
			}
			
			links.Reverse();
			objects.Reverse();
			
			objects.AddRange(links);
			return objects.ToArray();
		}
		
		private List<Wrappers.Wrapper> HitTest(Allocation allocation)
		{
			List<Wrappers.Wrapper> res = new List<Wrappers.Wrapper>();
			Allocation rect = allocation.Copy();
			
			rect.X += ActiveNode.X;
			rect.Y += ActiveNode.Y;
			
			rect.X = Scaled(rect.X);
			rect.Y = Scaled(rect.Y);
			rect.Width = Scaled(rect.Width);
			rect.Height = Scaled(rect.Height);
			
			foreach (Wrappers.Wrapper obj in SortedObjects())
			{				
				if (LinkFilter(obj))
				{
					if (obj.Allocation.Intersects(rect) && obj.HitTest(rect))
					{
						res.Add(obj);
					}
				}
				else
				{
					if ((obj as Wrappers.Edge).HitTest(rect, ZoomLevel))
					{
						res.Add(obj);
					}
				}
			}
			
			return res;
		}
		
		public void ScrollInView(Wrappers.Wrapper obj)
		{
			if (obj.Parent == null)
			{
				return;
			}
			
			if (d_activeNode != obj.Parent)
			{
				CenterView(obj);
				return;
			}
			
			double x;
			double y;
			double dx;
			double dy;
			
			if (!LinkFilter(obj))
			{
				Wrappers.Edge link = (Wrappers.Edge)obj;
				
				x = System.Math.Min(link.Input.Allocation.X, link.Output.Allocation.X);
				dx = System.Math.Max(link.Input.Allocation.X + link.Input.Allocation.Width,
				                     link.Output.Allocation.X + link.Output.Allocation.Width) - x;
				
				y = System.Math.Min(link.Input.Allocation.Y, link.Output.Allocation.Y);
				dy = System.Math.Max(link.Input.Allocation.Y + link.Input.Allocation.Height,
				                     link.Output.Allocation.Y + link.Output.Allocation.Height) - y;
			}
			else
			{
				x = obj.Allocation.X;
				y = obj.Allocation.Y;
				
				dx = obj.Allocation.Width;
				dy = obj.Allocation.Height;
			}
			
			int px = (int)(x * ZoomLevel);
			int py = (int)(y * ZoomLevel);
			
			int pdx = (int)(dx * ZoomLevel);
			int pdy = (int)(dy * ZoomLevel);
			
			/* If object is not in the view, scroll to center it */
			if (px < ActiveNode.X ||
			    py < ActiveNode.Y ||
			    px + pdx > Allocation.Width + ActiveNode.X ||
			    py + pdy > Allocation.Height + ActiveNode.Y)
			{
				ActiveNode.X = (int)(px + (pdx - Allocation.Width) / 2.0f);
				ActiveNode.Y = (int)(py + (pdy - Allocation.Height) / 2.0f);

				ModifiedView(this, new EventArgs());
				QueueDraw();
			}
		}
		
		public void CenterView()
		{
			Point xy;

			xy = Utils.MeanPosition(ActiveNode.Children);
			
			ActiveNode.X = (int)(xy.X * ZoomLevel - (Allocation.Width / 2.0f));
			ActiveNode.Y = (int)(xy.Y * ZoomLevel - (Allocation.Height / 2.0f));
			
			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private bool Selected(Wrappers.Wrapper obj)
		{
			return d_selection.IndexOf(obj) != -1;
		}
		
		private void UpdateDragState(Point position)
		{
			if (d_selection.Count == 0)
			{
				d_isDragging = false;
				return;
			}
			
			/* The drag state contains the relative offset of the mouse to the first
			 * object in the selection. x and y are in unit coordinates */
			Wrappers.Wrapper first = d_selection.Find(delegate (Wrappers.Wrapper obj) {
				return LinkFilter(obj); });
			
			if (first == null)
			{
				d_isDragging = false;
				return;
			}
			
			Allocation alloc = first.Allocation;
			
			d_selection.Remove(first);
			d_selection.Insert(0, first);
			
			d_dragState.X = alloc.X - position.X;
			d_dragState.Y = alloc.Y - position.Y;
			
			d_isDragging = true;
		}
		
		private Point ScaledFromDragState(Wrappers.Wrapper obj)
		{
			Allocation rect = d_selection[0].Allocation;
			Allocation aobj = obj.Allocation;
			
			return new Point(d_dragState.X - (rect.X - aobj.X), d_dragState.Y - (rect.Y - aobj.Y));
		}
		
		private bool LinkFilter(Wrappers.Wrapper wrapped)
		{
			return !(wrapped is Wrappers.Edge) || ((Wrappers.Edge)wrapped).Empty;
		}

		private void DoDragRect(int x, int y, Gdk.ModifierType state)
		{
			Allocation rect = d_mouseRect.FromRegion();
			rect.GrowBorder(2);
			QueueDrawArea((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
			
			d_mouseRect.Width = System.Math.Max(System.Math.Min(x, Allocation.Width), 0);
			d_mouseRect.Height = System.Math.Max(System.Math.Min(y, Allocation.Height), 0);
			
			state &= Gtk.Accelerator.DefaultModMask;
			
			/* Modifier cases:
			   Ctrl: select alternative selection (target)
			   Shift: add to selection */
			List<Wrappers.Wrapper> objects = HitTest(d_mouseRect.FromRegion());

			if ((state & Gdk.ModifierType.ShiftMask) != 0)
			{
				foreach (KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper.State> pair in d_beforeDragSelection)
				{
					if (!objects.Contains(pair.Key))
					{
						pair.Key.StateFlags = pair.Value;
					}
				}
			}

			if (d_selectionState != SelectionState.All)
			{
				objects.RemoveAll(item => (d_selectionState == SelectionState.LinksOnly) == LinkFilter(item));
			}

			List<Wrappers.Wrapper> selection = new List<Wrappers.Wrapper>(d_selection);
			
			foreach (Wrappers.Wrapper obj in selection)
			{
				if (!objects.Contains(obj) && ((state & Gdk.ModifierType.ShiftMask) == 0 || !d_beforeDragSelection.ContainsKey(obj)))
				{
					Unselect(obj);
				}
			}
			
			foreach (Wrappers.Wrapper obj in objects)
			{
				bool isalt = (!(obj is Wrappers.Edge) && ((state & Gdk.ModifierType.ControlMask) != 0));
				
				if (!Selected(obj) || obj.SelectedAlt != isalt)
				{
					Select(obj, isalt);
				}
			}
			
			rect = d_mouseRect.FromRegion();
			rect.GrowBorder(2);

			QueueDrawArea((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
		}
		
		private void DoMoveCanvas(Gdk.EventMotion evnt)
		{
			int dx = (int)(evnt.X - d_buttonPress.X);
			int dy = (int)(evnt.Y - d_buttonPress.Y);
			
			ActiveNode.X = (int)(d_origPosition.X - dx);
			ActiveNode.Y = (int)(d_origPosition.Y - dy);
			
			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private bool LinkAnchorTest(Wrappers.Edge link, bool isFrom, double x, double y)
		{
			List<Point> anchors;
			
			if (isFrom)
			{
				anchors = link.FromAnchors;
			}
			else
			{
				anchors = link.ToAnchors;
			}

			if (anchors == null)
			{
				return false;
			}
			
			double radius = AnchorRadius;
			
			foreach (Point pt in anchors)
			{
				double dx = (pt.X - x);
				double dy = (pt.Y - y);
				
				if (System.Math.Sqrt(dx * dx + dy * dy) < radius)
				{
					d_anchor = new Anchor(link, pt, isFrom);
					link.LinkFocus = true;
					
					QueueDrawAnchor(d_anchor);
					return true;
				}
			}
			
			return false;
		}
		
		private void QueueDrawAnchor(Anchor anchor)
		{
			int px = (int)(anchor.Location.X * ZoomLevel - ActiveNode.X);
			int py = (int)(anchor.Location.Y * ZoomLevel - ActiveNode.Y);
			
			int ar = (int)(AnchorRadius * ZoomLevel * 2);

			QueueDrawArea(px - ar, py - ar, ar * 2, ar * 2);
		}
		
		private bool LinkAnchorTest(double x, double y)
		{
			if (d_anchor != null)
			{
				d_anchor.Edge.LinkFocus = false;

				QueueDrawAnchor(d_anchor);
			}

			d_anchor = null;

			x = Scaled(x + ActiveNode.X);
			y = Scaled(y + ActiveNode.Y);

			foreach (Wrappers.Wrapper obj in ActiveNode.Children)
			{
				Wrappers.Edge link = obj as Wrappers.Edge;
				
				if (link == null)
				{
					continue;
				}
				
				if (LinkAnchorTest(link, true, x, y))
				{
					return true;
				}
				
				if (LinkAnchorTest(link, false, x, y))
				{
					return true;
				}
			}
			
			return false;
		}
		
		private void DoMouseInOut(double x, double y)
		{
			// Link anchor testing always has priority
			if (LinkAnchorTest(x, y))
			{
				foreach (Wrappers.Wrapper obj in d_hover)
				{
					obj.MouseFocus = false;
				}

				d_hover.Clear();
				
				return;
			}
			
			List<Wrappers.Wrapper> objects = HitTest(new Allocation(x, y, 1, 1));
			
			d_hover.RemoveAll(delegate (Wrappers.Wrapper obj) {
				if (objects.Count == 0 || obj != objects[0])
				{
					obj.MouseFocus = false;
					return true;
				}
				else
				{
					return false;
				}
			});

			if (objects.Count != 0 && !d_hover.Contains(objects[0]))
			{
				objects[0].MouseFocus = true;
				d_hover.Add(objects[0]);
			}
		}
		
		private Point UnitSize()
		{
			return new Point(Scaled(Allocation.Width, new ScaledPredicate(System.Math.Ceiling)),
			                  Scaled(Allocation.Height, new ScaledPredicate(System.Math.Ceiling)));
		}
		
		private void ZoomAtPoint(int size, Point where)
		{
			size = System.Math.Max(System.Math.Min(size, MaxZoom), MinZoom);
			
			ActiveNode.X += (int)(((where.X + ActiveNode.X) * (double)size / ZoomLevel) - (where.X + ActiveNode.X));
			ActiveNode.Y += (int)(((where.Y + ActiveNode.Y) * (double)size / ZoomLevel) - (where.Y + ActiveNode.Y));
			
			ZoomLevel = size;
		}
		
		public void CenterView(Wrappers.Wrapper obj)
		{
			if (obj.Parent == null)
			{
				return;
			}

			SetActiveNode(obj.Parent);
			
			ZoomLevel = DefaultZoom;
			
			Point xy;
			
			if (!LinkFilter(obj))
			{
				Wrappers.Edge link = (Wrappers.Edge)obj;
				xy = Utils.MeanPosition(new Wrappers.Wrapper[] {link.Input, link.Output});
			}
			else
			{
				xy = new Point(obj.Allocation.X, obj.Allocation.Y);
			}

			ActiveNode.X = (int)(xy.X * ZoomLevel - Allocation.Width / 2.0f);
			ActiveNode.Y = (int)(xy.Y * ZoomLevel - Allocation.Height / 2.0f);

			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private void DoZoom(bool zoomIn, Point pt)
		{
			int nsize = ZoomLevel + (int)System.Math.Floor(ZoomLevel * 0.2 * (zoomIn ? 1 : -1));

			bool upperReached = false;
			bool lowerReached = false;
			
			if (nsize > MaxZoom)
			{
				nsize = MaxZoom;
				upperReached = true;
			}
			else if (nsize < MinZoom)
			{
				nsize = MinZoom;
				lowerReached = true;
			}
			
			ZoomAtPoint(nsize, pt);
			
			if (upperReached)
			{
				List<Wrappers.Wrapper> objects = HitTest(new Allocation(pt.X, pt.Y, 1, 1));
				
				foreach (Wrappers.Wrapper obj in objects)
				{
					if (!(obj is Wrappers.Node))
					{
						continue;
					}

					Wrappers.Node grp = obj as Wrappers.Node;
					Point xy;
					
					xy = Utils.MeanPosition(grp.Children);
					
					grp.X = (int)(xy.X * MinZoom - pt.X);
					grp.Y = (int)(xy.Y * MinZoom - pt.Y);
					
					SetActiveNode(grp);
					ZoomLevel = MinZoom;
					
					break;
				}
			}
			else if (lowerReached && d_activeNode.Parent != null)
			{
				Wrappers.Node newActive = d_activeNode.Parent;
				
				double x, y;
				x = d_activeNode.Allocation.X + d_activeNode.Allocation.Width / 2;
				y = d_activeNode.Allocation.Y + d_activeNode.Allocation.Height / 2;
					
				newActive.X = (int)(x * MaxZoom - pt.X);
				newActive.Y = (int)(y * MaxZoom - pt.Y);
				
				SetActiveNode(newActive);
				ZoomLevel = MaxZoom;
			}
		}
		
		private void DoZoom(bool zoomIn)
		{
			DoZoom(zoomIn, new Point(Allocation.Width / 2, Allocation.Height / 2));
		}
		
		public void ZoomIn()
		{
			DoZoom(true);
		}
		
		public void ZoomOut()
		{
			DoZoom(false);
		}
		
		public void ZoomDefault()
		{
			Point pt = new Point(Allocation.Width / 2, Allocation.Height / 2);
			ZoomAtPoint(DefaultZoom, pt);
		}
		
		private void DoMove(int dx, int dy, bool moveCanvas)
		{
			if (dx == 0 && dy == 0)
			{
				return;
			}
			
			if (moveCanvas)
			{
				ActiveNode.X += dx * ZoomLevel;
				ActiveNode.Y += dy * ZoomLevel;
				
				ModifiedView(this, new EventArgs());
			}
			else
			{
				d_actions.Move(d_selection, dx, dy);
			}
			
			QueueDraw();
		}
		
		private void FocusRelease()
		{
			if (d_focus != null)
			{
				Wrappers.Wrapper o = d_focus;
				d_focus = null;
				
				o.KeyFocus = false;
			}
		}
		
		private bool FocusNext(int direction)
		{
			Wrappers.Wrapper pf = d_focus;
			
			FocusRelease();
			
			if (ActiveNode.Length == 0)
			{
				return false;
			}
			
			if (pf == null)
			{
				d_focus = ActiveNode.Children[direction == 1 ? 0 : ActiveNode.Length - 1];
			}
			else
			{
				int nidx = ActiveNode.IndexOf(pf) + direction;
				
				if (nidx >= ActiveNode.Length || nidx < 0)
				{
					return false;
				}
				
				d_focus = ActiveNode.Children[nidx];
			}
			
			if (d_focus != null)
			{
				FocusSet(d_focus);
				return true;
			}
			else
			{
				return false;
			}
		}
		
		private void FocusSet(Wrappers.Wrapper obj)
		{
			d_focus = obj;
			obj.KeyFocus = true;
		}
		
		private void DrawBackground(Cairo.Context graphics)
		{
			graphics.Rectangle(0, 0, Allocation.Width, Allocation.Height);
			graphics.SetSourceRGB(d_gridBackground[0], d_gridBackground[1], d_gridBackground[2]);
			graphics.Fill();
		}
		
		private void DrawGrid(Cairo.Context graphics)
		{
			// Calculate x/y offset of the grid lines
			float ox = ActiveNode.X % (float)ZoomLevel;
			float oy = ActiveNode.Y % (float)ZoomLevel;
			
			graphics.Save();
			graphics.Translate(-ZoomLevel - ox, -ZoomLevel - oy);
			
			d_gridCache.Render(graphics, Allocation.Width + ZoomLevel * 2, Allocation.Height + ZoomLevel * 2, delegate (Cairo.Context ctx, double width, double height)
			{
				double offset = 0.5;
				ctx.LineWidth = 1;
				
				ctx.SetSourceRGB(d_gridLine[0], d_gridLine[1], d_gridLine[2]);
				
				int i = 0;
				while (i <= width)
				{
					ctx.MoveTo(i - offset, offset);
					ctx.LineTo(i - offset, height + offset);
					
					i += ZoomLevel;
				}
				
				i = 0;
				while (i <= height)
				{
					ctx.MoveTo(offset, i - offset);
					ctx.LineTo(width + offset, i - offset);
					
					i += ZoomLevel;
				}
				
				ctx.Stroke();
			});
			
			graphics.Restore();
		}
		
		private void DrawObject(Cairo.Context graphics, Wrappers.Wrapper obj)
		{
			if (obj == null)
			{
				return;
			}
			
			Allocation alloc = obj.Allocation;
			graphics.Save();
			graphics.Translate(alloc.X, alloc.Y);

			obj.Draw(graphics);
			graphics.Restore();
		}
		
		private void DrawAnchor(Cairo.Context graphics, Point location)
		{
			graphics.Save();
			double radius = AnchorRadius;
			
			graphics.LineWidth *= 3;
			
			graphics.MoveTo(location.X + radius, location.Y);
			graphics.Arc(location.X, location.Y, radius, 0, System.Math.PI * 2);
			graphics.SetSourceRGB(0.6, 0.6, 0.3);
			graphics.StrokePreserve();

			graphics.SetSourceRGB(0.9, 0.9, 0.1);
			graphics.Fill();
			
			graphics.Restore();
		}
		
		private void DrawAnchor(Cairo.Context graphics, Anchor anchor)
		{
			if (d_isDraggingAnchor)
			{
				return;
			}

			DrawAnchor(graphics, anchor.Location);
		}
		
		private void DrawDraggingAnchor(Cairo.Context graphics)
		{
			if (d_anchor == null && d_anchorDragHit == null || d_dragAnchorState == null)
			{
				return;
			}

			graphics.Save();
			
			Allocation fromAlloc;
			Allocation toAlloc;
			
			if (d_anchor == null)
			{
				toAlloc = d_anchorDragHit.Allocation;
				fromAlloc = d_anchorDragHit.Allocation;
			}
			else if (d_anchor.IsFrom)
			{
				toAlloc = d_anchor.Edge.Output.Allocation;
				fromAlloc = new Allocation(d_dragAnchorState.X, d_dragAnchorState.Y, 1, 1);
			}
			else
			{
				toAlloc = new Allocation(d_dragAnchorState.X, d_dragAnchorState.Y, 1, 1);
				fromAlloc = d_anchor.Edge.Input.Allocation;
			}
			
			graphics.SetSourceRGB(0.8, 0.8, 0.3);
			graphics.LineWidth *= 2;

			graphics.SetDash(new double[] {graphics.LineWidth, graphics.LineWidth}, 0);
			Wrappers.Renderers.Edge.Draw(graphics, fromAlloc, toAlloc, 1);
			
			graphics.Restore();
			
			if (d_dragAnchorState != null)
			{
				DrawAnchor(graphics, new Point(d_dragAnchorState.X + 0.5, d_dragAnchorState.Y + 0.5));
			}
		}
		
		private void DrawObjects(Cairo.Context graphics)
		{
			graphics.Save();
			
			graphics.Translate(-ActiveNode.X, -ActiveNode.Y);
			graphics.Scale(ZoomLevel, ZoomLevel);
			graphics.LineWidth = 1.0 / ZoomLevel;

			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>();
			
			foreach (Wrappers.Wrapper obj in ActiveNode.Children)
			{
				Wrappers.Edge link = obj as Wrappers.Edge;

				if (link == null)
				{
					objects.Add(obj);
				}
				else
				{
					DrawObject(graphics, obj);
				}
			}
			
			if (d_isDraggingAnchor || d_isDragging)
			{
				DrawDraggingAnchor(graphics);
			}
			
			foreach (Wrappers.Wrapper obj in objects)
			{
				DrawObject(graphics, obj);
			}
			
			if (d_anchor != null)
			{
				DrawAnchor(graphics, d_anchor);
			}

			graphics.Restore();
		}
		
		private void DrawSelectionRect(Cairo.Context graphics)
		{
			Allocation rect = d_mouseRect.FromRegion();
			
			if (rect.Width <= 1 || rect.Height <= 1)
			{
				return;
			}
			
			graphics.Rectangle(rect.X + 0.5, rect.Y + 0.5, rect.Width, rect.Height);
			graphics.SetSourceRGBA(0.7, 0.8, 0.7, 0.3);
			graphics.FillPreserve();
			
			graphics.SetSourceRGBA(0, 0.3, 0, 0.6);
			graphics.Stroke();
		}
		
		/* Callbacks */
		private void OnRequestRedraw(object source, EventArgs e)
		{
			QueueDrawObject(source as Wrappers.Wrapper);
		}
		
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			base.OnButtonPressEvent(evnt);
			
			GrabFocus();
			
			if (evnt.Button < 1 || evnt.Button > 3)
			{
				return false;
			}
			
			if (LinkAnchorTest(evnt.X, evnt.Y))
			{
				d_isDraggingAnchor = true;

				d_anchor.Edge.LinkFocus = false;
				
				if (d_anchor.IsFrom)
				{
					d_anchor.Edge.Output.LinkFocus = true;
				}
				else
				{
					d_anchor.Edge.Input.LinkFocus = true;
				}

				UpdateDragAnchor(evnt.X, evnt.Y);
				return true;
			}
			
			List<Wrappers.Wrapper> objects = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));
			Wrappers.Wrapper first = objects.Count > 0 ? objects[0] : null;

			if (evnt.Type == Gdk.EventType.TwoButtonPress)
			{
				if (first == null)
				{
					return false;
				}
				
				Activated(this, first);
				return true;
			}
			
			Gdk.ModifierType state = evnt.State & Gtk.Accelerator.DefaultModMask;
			
			if (evnt.Button != 2)
			{
				if (Selected(first) && (state & Gdk.ModifierType.ShiftMask) != 0)
				{
					Unselect(first);
					first = null;
				}
				
				if (Selected(first) && state == 0)
				{
					if (evnt.Button == 1)
					{
						Point res = ScaledPosition(evnt.X, evnt.Y, System.Math.Floor);
						UpdateDragState(res);
					}
					
					first = null;
				}
				else if (!(Selected(first) || (state & Gdk.ModifierType.ShiftMask) != 0))
				{
					Wrappers.Wrapper[] selection = new Wrappers.Wrapper[d_selection.Count];
					d_selection.CopyTo(selection, 0);
					
					foreach (Wrappers.Wrapper obj in selection)
					{
						Unselect(obj);
					}
				}
			}
			
			if (evnt.Button != 2 && first != null)
			{
				Select(first, (evnt.State & Gdk.ModifierType.ControlMask) != 0);
				
				if (evnt.Button == 1)
				{
					Point res = ScaledPosition(evnt.X, evnt.Y, System.Math.Floor);
					UpdateDragState(res);
				}
			}
			
			if (evnt.Button == 3)
			{
				Popup(this, (int)evnt.Button, evnt.Time);
			}
			else if (evnt.Button == 2)
			{
				d_buttonPress = new Point((int)evnt.X, (int)evnt.Y);
				d_origPosition = Position;
			}
			else
			{
				d_mouseRect = new Allocation(evnt.X, evnt.Y, evnt.X, evnt.Y);
			}				
			
			return true;
		}
		
		private void ReattachFromAnchor()
		{
			Wrappers.Node fr;
			Wrappers.Node to;

			if (d_anchor.IsFrom)
			{
				fr = d_anchorDragHit;
				to = d_anchor.Edge.Output;
			}
			else
			{
				fr = d_anchor.Edge.Input;
				to = d_anchorDragHit;
			}
			
			if (d_anchor.Edge.Input != fr || d_anchor.Edge.Output != to)
			{
				d_actions.Do(new Undo.AttachEdge(d_anchor.Edge, fr, to));
			}
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
		{
			base.OnButtonReleaseEvent(evnt);
			
			d_isDragging = false;
			
			if (d_isDraggingAnchor)
			{
				d_isDraggingAnchor = false;
				
				if (d_anchorDragHit != null)
				{
					d_anchorDragHit.LinkFocus = false;
					
					// Do the actual reconnecting!
					ReattachFromAnchor();
					d_anchorDragHit = null;
				}

				LinkAnchorTest(evnt.X, evnt.Y);
			}
			else if (d_anchorDragHit != null)
			{
				d_hiddenLink.Invisible = false;
				d_anchorDragHit.LinkFocus = false;
				
				int dx = (int)d_hiddenLink.Allocation.X;
				int dy = (int)d_hiddenLink.Allocation.Y;
				
				d_actions.Do(new Undo.Group(new Undo.MoveObject(d_hiddenLink, -dx, -dy),
				                            new Undo.AttachEdge(d_hiddenLink as Wrappers.Edge, d_anchorDragHit, d_anchorDragHit)));

				d_anchorDragHit = null;
				d_hiddenLink = null;
			}
			
			d_mouseRect = new Allocation(0, 0, -1, -1);
			
			if (d_beforeDragSelection != null)
			{
				Status(this, null);
			}

			d_beforeDragSelection = null;
			
			if (evnt.Type == Gdk.EventType.ButtonRelease)
			{
				List<Wrappers.Wrapper> objects = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));

				if (objects.Count > 0)
				{
					Point pos = ScaledPosition(evnt.X, evnt.Y);
					objects[0].Clicked(pos);
				}
			}
			
			QueueDraw();
			return false;
		}
		
		private void UpdateDragAnchor(double x, double y)
		{
			int xpos = (int)Scaled(x + ActiveNode.X, item => System.Math.Floor(item));
			int ypos = (int)Scaled(y + ActiveNode.Y, item => System.Math.Floor(item));
			
			if (d_dragAnchorState == null || xpos != d_dragAnchorState.X || ypos != d_dragAnchorState.Y)
			{
				d_dragAnchorState = new Point(xpos, ypos);
				
				List<Wrappers.Wrapper> objects = HitTest(new Allocation(x, y, 1, 1));
				
				objects.RemoveAll(item => !(item is Wrappers.Node));
				
				if (d_anchorDragHit != null)
				{
					if (d_anchorDragHit != d_anchor.Other)
					{
						d_anchorDragHit.LinkFocus = false;
					}

					d_anchorDragHit = null;
				}
				
				if (objects.Count > 0)
				{
					d_anchorDragHit = objects[0] as Wrappers.Node;
					d_anchorDragHit.LinkFocus = true;
				}
				
				QueueDraw();
			}
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
		{
			base.OnMotionNotifyEvent(evnt);
			
			if ((evnt.State & Gdk.ModifierType.Button2Mask) != 0)
			{
				DoMoveCanvas(evnt);
				return true;
			}
			
			if (!d_isDragging && !d_isDraggingAnchor)
			{
				if (d_mouseRect.Width >= 0 && d_mouseRect.Height >= 0)
				{
					if (d_beforeDragSelection == null)
					{
						d_beforeDragSelection = new Dictionary<Wrappers.Wrapper, Wrappers.Wrapper.State>();
						
						foreach (Wrappers.Wrapper wrapper in d_selection)
						{
							d_beforeDragSelection[wrapper] = wrapper.StateFlags;
						}

						d_selectionState = SelectionState.All;
						
						UpdateSelectStatus(evnt.State);
					}

					DoDragRect((int)evnt.X, (int)evnt.Y, evnt.State);
				}
				else
				{
					DoMouseInOut(evnt.X, evnt.Y);
				}
				
				return true;
			}
			
			if (d_isDraggingAnchor)
			{
				UpdateDragAnchor(evnt.X, evnt.Y);
				return true;
			}
			
			Point position = ScaledPosition(evnt.X, evnt.Y, System.Math.Floor);
			
			List<Wrappers.Wrapper> selection = new List<Wrappers.Wrapper>(d_selection);
			selection.RemoveAll(item => !LinkFilter(item));
			
			int dx = 0;
			int dy = 0;
			int[] translation;

			if (selection.Count != 0)
			{			
				Point pt = ScaledFromDragState(selection[0]);
				translation = new int[] {(int)pt.X, (int)pt.Y};
				
				dx = (int)(position.X + translation[0]);
				dy = (int)(position.Y + translation[1]);
			}
			
			bool inwindow = evnt.X >= 0 && evnt.Y >= 0 && evnt.X <= Allocation.Width && evnt.Y <= Allocation.Height;
			
			if (inwindow && selection.Count > 0 &&
			    (dx != (int)selection[0].Allocation.X ||
			     dy != (int)selection[0].Allocation.Y))
			{
				DoMove((int)(dx - selection[0].Allocation.X),
				       (int)(dy - selection[0].Allocation.Y),
				       false);

				ModifiedView(this, new EventArgs());
				
				if (d_anchorDragHit != null)
				{
					d_anchorDragHit.LinkFocus = false;
					d_anchorDragHit = null;
					
					d_hiddenLink.Invisible = false;
					
					QueueDraw();
				}
				
				if (selection.Count == 1 && selection[0] is Wrappers.Edge)
				{
					List<Wrappers.Wrapper> hits = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));
					hits.RemoveAll(item => !(item is Wrappers.Node));
					
					if (hits.Count > 0)
					{
						d_anchorDragHit = hits[0] as Wrappers.Node;
						d_anchorDragHit.LinkFocus = true;
						
						d_hiddenLink = selection[0];
						d_hiddenLink.Invisible = true;
						
						QueueDraw();
					}
				}
			}
			else if (d_anchorDragHit != null)
			{
				List<Wrappers.Wrapper> hits = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));
				hits.RemoveAll(item => item is Wrappers.Edge);
				
				if (hits.Count == 0)
				{
					d_anchorDragHit.LinkFocus = false;
					d_anchorDragHit = null;
					
					d_hiddenLink.Invisible = false;
					
					QueueDraw();
				}
			}
			
			return true;
		}

		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			base.OnScrollEvent(evnt);
			
			if (d_isDragging || d_isDraggingAnchor || (evnt.State & Gdk.ModifierType.Button2Mask) != 0)
			{
				return false;
			}
			
			if (evnt.Direction == Gdk.ScrollDirection.Up)
			{
				DoZoom(true, new Point((int)evnt.X, (int)evnt.Y));
				return true;
			}
			
			if (evnt.Direction == Gdk.ScrollDirection.Down)
			{
				DoZoom(false, new Point((int)evnt.X, (int)evnt.Y));
				return true;
			}
			
			return false;
		}
		
		protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
		{
			bool ret = base.OnLeaveNotifyEvent(evnt);
			
			foreach (Wrappers.Wrapper obj in d_hover)
			{
				obj.MouseFocus = false;
			}
		
			d_hover.Clear();
			return ret;
		}
		
		protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
		{
			bool ret = base.OnEnterNotifyEvent(evnt);
			
			DoMouseInOut(evnt.X, evnt.Y);
			return ret;
		}
		
		private void UpdateSelectStatus(Gdk.ModifierType state)
		{
			bool isshift = (state & Gdk.ModifierType.ShiftMask) != 0;
			bool isctrl = (state & Gdk.ModifierType.ControlMask) != 0;
			string msg;

			if (isctrl)
			{
				if (isshift)
				{
					msg = "Add to target selection";
				}
				else
				{
					msg = "Create target selection";
				}
			}
			else
			{
				if (isshift)
				{
					msg = "Add to source selection";
				}
				else
				{
					msg = "Create source selection";
				}
			}
			
			switch (d_selectionState)
			{
			case SelectionState.LinksOnly:
				msg += ", only links";
				break;
			case SelectionState.StatesOnly:
				msg += ", only states";
				break;
			default:
				break;
			}
			
			Status(this, msg);
		}
		
		private void SelectAll(bool isalt)
		{
			foreach (Wrappers.Wrapper wrapper in d_activeNode.Children)
			{
				Select(wrapper, isalt);
			}
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
		{
			base.OnKeyPressEvent(evnt);
			
			if (evnt.Key == Gdk.Key.Home || evnt.Key == Gdk.Key.KP_Home)
			{
				CenterView();
			}
			else if (evnt.Key == Gdk.Key.Delete)
			{
				try
				{
					d_actions.Delete(d_activeNode, Selection);
				}
				catch (Exception e)
				{
					Error(this, "An error occurred while deleting", e.Message);
				}
			}
			else if (evnt.Key == Gdk.Key.Up || evnt.Key == Gdk.Key.KP_Up)
			{
				DoMove(0, -1, (evnt.State & Gdk.ModifierType.Mod1Mask) != 0);
			}
			else if (evnt.Key == Gdk.Key.Down || evnt.Key == Gdk.Key.KP_Down)
			{
				DoMove(0, 1, (evnt.State & Gdk.ModifierType.Mod1Mask) != 0);
			}
			else if (evnt.Key == Gdk.Key.Left || evnt.Key == Gdk.Key.KP_Left)
			{
				DoMove(-1, 0, (evnt.State & Gdk.ModifierType.Mod1Mask) != 0);
			}
			else if (evnt.Key == Gdk.Key.Right || evnt.Key == Gdk.Key.KP_Right)
			{
				DoMove(1, 0, (evnt.State & Gdk.ModifierType.Mod1Mask) != 0);
			}
			else if ((evnt.Key == Gdk.Key.Tab || evnt.Key == Gdk.Key.ISO_Left_Tab) && (evnt.State & Gdk.ModifierType.ControlMask) == 0)
			{
				FocusNext((evnt.State & Gdk.ModifierType.ShiftMask) != 0 ? -1 : 1);
			}
			else if (evnt.Key == Gdk.Key.KP_Add && (evnt.State & Gdk.ModifierType.ControlMask) != 0)
			{
				ZoomIn();
			}
			else if (evnt.Key == Gdk.Key.KP_Subtract && (evnt.State & Gdk.ModifierType.ControlMask) != 0)
			{
				ZoomOut();
			}
			else if (evnt.Key == Gdk.Key.space)
			{
				Wrappers.Wrapper obj = d_focus;
				
				if (d_focus != null && !Selected(d_focus))
				{
					Select(d_focus);
					FocusSet(obj);
				}
				else if (d_focus != null && Selected(d_focus))
				{
					Unselect(d_focus);
					FocusSet(obj);
				}
			}
			else if (evnt.Key == Gdk.Key.Menu || evnt.Key == Gdk.Key.Multi_key)
			{
				Popup(this, 3, evnt.Time);
			}
			else if (evnt.Key == Gdk.Key.Return || evnt.Key == Gdk.Key.KP_Enter)
			{
				if (d_selection.Count == 1)
				{
					Activated(this, d_selection[0]);
				}
				else
				{
					return false;
				}
			}
			else if ((evnt.Key == Gdk.Key.A || evnt.Key == Gdk.Key.a) && (evnt.State & Gdk.ModifierType.ControlMask) != 0)
			{
				SelectAll((evnt.State & Gdk.ModifierType.Mod1Mask) != 0);
			}
			else
			{
				if (d_beforeDragSelection != null)
				{
					Gdk.ModifierType state = evnt.State;
					
					if (evnt.Key == Gdk.Key.Shift_L || evnt.Key == Gdk.Key.Shift_R)
					{
						state |= Gdk.ModifierType.ShiftMask;
					}
					else if (evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)
					{
						state |= Gdk.ModifierType.ControlMask;
					}
					else if (evnt.Key == Gdk.Key.Alt_L || evnt.Key == Gdk.Key.Alt_R)
					{
						state |= Gdk.ModifierType.Mod1Mask;
						
						d_selectionState += 1;
						
						if ((int)d_selectionState == (int)SelectionState.Num)
						{
							d_selectionState = 0;
						}
					}
					
					UpdateSelectStatus(state);
					
					int x;
					int y;			
					
					GetPointer(out x, out y);
					DoDragRect(x, y, state);
				}

				return false;
			}
			
			return true;
		}

		protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
		{
			base.OnKeyReleaseEvent(evnt);
			
			if (d_beforeDragSelection != null)
			{
				Gdk.ModifierType state = evnt.State;
					
				if (evnt.Key == Gdk.Key.Shift_L || evnt.Key == Gdk.Key.Shift_R)
				{
					state &= ~Gdk.ModifierType.ShiftMask;
				}
				else if (evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)
				{
					state &= ~Gdk.ModifierType.ControlMask;
				}
				else if (evnt.Key == Gdk.Key.Alt_L || evnt.Key == Gdk.Key.Alt_R)
				{
					state &= ~Gdk.ModifierType.Mod1Mask;
				}
				
				UpdateSelectStatus(state);
	
				int x;
				int y;			
				
				GetPointer(out x, out y);
				DoDragRect(x, y, state);
			}

			return false;
		}
		
		public void UnselectAll()
		{
			List<Wrappers.Wrapper> copy = new List<Wrappers.Wrapper>(d_selection);
			
			foreach (Wrappers.Wrapper o in copy)
			{
				Unselect(o);
			}
		}
		
		private void OnSelectionChanged(object source, EventArgs args)
		{
			FocusRelease();
			UpdateAnnotation();
		}
		
		protected override bool OnFocusOutEvent(Gdk.EventFocus evnt)
		{
			foreach (Wrappers.Wrapper obj in d_hover)
			{
				obj.MouseFocus = false;
			}
			
			d_hover.Clear();
			FocusRelease();
			
			return base.OnFocusOutEvent(evnt);
		}

		private void DrawAnnotation(Cairo.Context graphics)
		{
			if (d_annotationLayout == null)
			{
				return;
			}

			graphics.Save();

			Pango.Rectangle ink;
			Pango.Rectangle logical;

			d_annotationLayout.GetPixelExtents(out ink, out logical);

			graphics.LineWidth = 1;

			int margin = 10;
			int padding = 8;
			int borderpad = 4;

			graphics.Rectangle(margin, margin, logical.Width + padding * 2, logical.Height + padding * 2);
			graphics.SetSourceRGBA(1, 1, 1, 0.75);
			graphics.Fill();

			graphics.Rectangle(margin + borderpad + 0.5, margin + borderpad + 0.5, logical.Width + (padding - borderpad) * 2, logical.Height + (padding - borderpad) * 2);
			graphics.SetSourceRGB(0.95, 0.95, 0.95);
			graphics.SetDash(new double[] {5, 5}, 0);
			graphics.Stroke();

			graphics.SetSourceRGB(0.5, 0.5, 0.5);

			graphics.MoveTo(margin + padding, margin + padding);
			Pango.CairoHelper.ShowLayout(graphics, d_annotationLayout);

			graphics.Restore();
		}
		
		public void Draw(Cairo.Context graphics)
		{			
			graphics.LineWidth = 1;

			DrawBackground(graphics);
			DrawGrid(graphics);
			DrawAnnotation(graphics);

			DrawObjects(graphics);
			DrawSelectionRect(graphics);
			
			graphics.SetSourceRGB(0.6, 0.6, 0.6);
			graphics.MoveTo(0, 0.5);
			graphics.RelLineTo(Allocation.Width, 0);
			graphics.Stroke();
			
			graphics.MoveTo(0, Allocation.Height - 0.5);
			graphics.RelLineTo(Allocation.Width, 0);
			graphics.Stroke();
			
			if (d_drawRightBorder)
			{
				graphics.MoveTo(Allocation.Width - 0.5, 0);
				graphics.RelLineTo(0, Allocation.Height);
				graphics.Stroke();
			}
			
			if (d_drawLeftBorder)
			{
				graphics.MoveTo(0.5, 0);
				graphics.RelLineTo(0, Allocation.Height);
				graphics.Stroke();
			}
		}
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			using (Cairo.Context graphics = Gdk.CairoHelper.Create(evnt.Window))
			{
				graphics.Rectangle(evnt.Area.X, evnt.Area.Y, evnt.Area.Width, evnt.Area.Height);
				graphics.Clip();
				
				Draw(graphics);
				
				((IDisposable)graphics.Target).Dispose();
			}
			
			return false;
		}
		
		protected override void OnStyleSet(Gtk.Style previous_style)
		{
			base.OnStyleSet(previous_style);
			
			Cdn.Studio.Settings.Font = Style.FontDescription;
		}

	}
}
