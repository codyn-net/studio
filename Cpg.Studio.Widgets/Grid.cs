using System;
using System.Collections.Generic;
using Gtk;
using System.Drawing;
using System.Reflection;

namespace Cpg.Studio.Widgets
{
	public class Grid : DrawingArea
	{
		public static int DefaultZoom = 50;
		public static int MaxZoom = 160;
		public static int MinZoom = 10;
		private static int AnchorRadius = 6;

		public delegate void ObjectEventHandler(object source, Wrappers.Wrapper obj);
		public delegate void PopupEventHandler(object source, int button, long time); 
		public delegate void ErrorHandler(object source, string error, string message);
		
		public event ObjectEventHandler Activated = delegate {};
		public event PopupEventHandler Popup = delegate {};
		public event EventHandler SelectionChanged = delegate {};
		public event EventHandler ModifiedView = delegate {};
		public event ErrorHandler Error = delegate {};
		public event EventHandler Cleared = delegate {};
		public event ObjectEventHandler ActiveGroupChanged = delegate {};
		
		private Allocation d_mouseRect;
		private System.Drawing.Point d_origPosition;
		private Point d_buttonPress;
		private Point d_dragState;
		private bool d_isDragging;
		private List<Wrappers.Wrapper> d_beforeDragSelection;
		
		private Wrappers.Wrapper d_focus;
		
		private Wrappers.Network d_network;
		private Actions d_actions;

		private List<Wrappers.Wrapper> d_hover;
		private Wrappers.Group d_activeGroup;
		private List<Wrappers.Wrapper> d_selection;
		
		private double[] d_gridBackground;
		private double[] d_gridLine;
		
		private RenderCache d_gridCache;
		private Anchor d_anchor;
		
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
			d_mouseRect = new Allocation(0, 0, 0, 0);
			d_beforeDragSelection = null;
			
			d_buttonPress = new Point();
			d_dragState = new Point();
			
			d_gridCache = new RenderCache();
		
			Clear();
		}
		
		public void Clear()
		{
			SetActiveGroup(d_network);

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
		
		public Wrappers.Group ActiveGroup
		{
			get
			{
				return d_activeGroup;
			}
			set
			{
				SetActiveGroup(value);
			}
		}
		
		private System.Drawing.Point Position
		{
			get
			{
				return new System.Drawing.Point((int)ActiveGroup.X, (int)ActiveGroup.Y);
			}
		}
		
		public int ZoomLevel
		{
			get
			{
				return d_activeGroup != null ? d_activeGroup.Zoom : DefaultZoom;
			}
			set
			{
				int clipped = Math.Max(Math.Min(value, MaxZoom), MinZoom);

				if (d_activeGroup != null && d_activeGroup.Zoom != clipped)
				{
					d_activeGroup.Zoom = clipped;
					
					ModifiedView(this, new EventArgs());
					QueueDraw();
				}
			}
		}
		
		public int[] Center
		{
			get
			{
				int cx = (int)Math.Round((ActiveGroup.X + Allocation.Width / 2.0) / (double)ZoomLevel);
				int cy = (int)Math.Round((ActiveGroup.Y + Allocation.Height / 2.0) / (double)ZoomLevel);
				
				return new int[] {cx, cy};
			}
		}
		
		public List<Wrappers.Link> Links
		{
			get
			{
				List<Wrappers.Link> res = new List<Wrappers.Link>();
				
				foreach (Wrappers.Wrapper obj in ActiveGroup.Children)
				{
					if (obj is Wrappers.Link)
					{
						res.Add(obj as Wrappers.Link);
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
		
		private void SetActiveGroup(Wrappers.Group grp)
		{
			if (d_activeGroup == grp)
			{
				return;
			}
			
			if (d_activeGroup != null)
			{
				d_activeGroup.ChildAdded -= HandleActiveGroupChildAdded;
				d_activeGroup.ChildRemoved -= HandleActiveGroupChildRemoved;
				
				foreach (Wrappers.Wrapper child in d_activeGroup.Children)
				{
					Disconnect(child);
				}
			}

			if (grp != null)
			{
				grp.ChildAdded += HandleActiveGroupChildAdded;
				grp.ChildRemoved += HandleActiveGroupChildRemoved;
				
				foreach (Wrappers.Wrapper child in grp.Children)
				{
					Connect(child);
				}
			}
			
			UnselectAll();

			Wrappers.Group prev = d_activeGroup;
			d_activeGroup = grp;
			
			QueueDraw();
			
			ActiveGroupChanged(this, prev);
		}
		
		private void Connect(Wrappers.Wrapper child)
		{
			child.RequestRedraw += OnRequestRedraw;
		}
		
		private void Disconnect(Wrappers.Wrapper child)
		{
			child.RequestRedraw -= OnRequestRedraw;
		}

		private void HandleActiveGroupChildRemoved(Wrappers.Group source, Wrappers.Wrapper child)
		{
			Disconnect(child);

			Unselect(child);			
			QueueDraw();
		}

		private void HandleActiveGroupChildAdded(Wrappers.Group source, Wrappers.Wrapper child)
		{
			Connect(child);

			QueueDraw();
		}
		
		public void Select(Wrappers.Wrapper obj)
		{
			if (d_selection.Contains(obj))
			{
				return;
			}

			SetActiveGroup(obj.Parent);
			
			d_selection.Add(obj);
			obj.Selected = true;

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
			if (!ActiveGroup.Contains(obj))
			{
				return;
			}
			
			using (Cairo.Context graphics = Gdk.CairoHelper.Create(GdkWindow))
			{
				Allocation alloc = obj.Extents(ZoomLevel, graphics);
				alloc.Round();
				
				alloc.Offset(-ActiveGroup.X, -ActiveGroup.Y);
				QueueDrawArea((int)alloc.X, (int)alloc.Y, (int)alloc.Width, (int)alloc.Height);				
			};
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
			return new Point(Scaled(position.X + ActiveGroup.X, predicate), Scaled(position.Y + ActiveGroup.Y, predicate));
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
			
			foreach (Wrappers.Wrapper obj in ActiveGroup.Children)
			{
				if (obj is Wrappers.Link)
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
			
			rect.X += ActiveGroup.X;
			rect.Y += ActiveGroup.Y;
			
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
					if ((obj as Wrappers.Link).HitTest(rect, ZoomLevel))
					{
						res.Add(obj);
					}
				}
			}
			
			return res;
		}
		
		public void CenterView()
		{
			double x, y;
			Utils.MeanPosition(ActiveGroup.Children, out x, out y);
			
			ActiveGroup.X = (int)(x * ZoomLevel - (Allocation.Width / 2.0f));
			ActiveGroup.Y = (int)(y * ZoomLevel - (Allocation.Height / 2.0f));
			
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
			Wrappers.Wrapper first = d_selection.Find(delegate (Wrappers.Wrapper obj) { return LinkFilter(obj); });
			
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
			return !(wrapped is Wrappers.Link) || ((Wrappers.Link)wrapped).Empty;
		}
		
		private void DoDragRect(int x, int y, Gdk.ModifierType state)
		{
			Allocation rect = d_mouseRect.FromRegion();
			rect.GrowBorder(2);
			QueueDrawArea((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
			
			d_mouseRect.Width = x;
			d_mouseRect.Height = y;

			List<Wrappers.Wrapper> objects = HitTest(d_mouseRect.FromRegion());
			state &= Gtk.Accelerator.DefaultModMask;

			if ((state & Gdk.ModifierType.ShiftMask) != 0)
			{
				objects.AddRange(d_beforeDragSelection);
			}
			
			if ((state & Gdk.ModifierType.ControlMask) != 0)
			{
				objects.RemoveAll(item => ((state & Gdk.ModifierType.Mod1Mask) == 0) == LinkFilter(item));
			}
			
			List<Wrappers.Wrapper> selection = new List<Wrappers.Wrapper>(d_selection);
			
			foreach (Wrappers.Wrapper obj in selection)
			{
				if (!objects.Contains(obj))
				{
					Unselect(obj);
				}
			}
			
			foreach (Wrappers.Wrapper obj in objects)
			{
				if (!Selected(obj))
				{
					Select(obj);
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
			
			ActiveGroup.X = (int)(d_origPosition.X - dx);
			ActiveGroup.Y = (int)(d_origPosition.Y - dy);
			
			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private bool LinkAnchorTest(Wrappers.Link link, Wrappers.Wrapper obj, List<Point> anchors, double x, double y)
		{
			if (anchors == null)
			{
				return false;
			}
			
			double radius = (double)AnchorRadius / ZoomLevel;
			
			foreach (Point pt in anchors)
			{
				double dx = (pt.X - x);
				double dy = (pt.Y - y);
				
				if (System.Math.Sqrt(dx * dx + dy * dy) < radius)
				{
					d_anchor = new Anchor(link, obj, pt);
					link.LinkFocus = true;

					QueueDrawAnchor(d_anchor);
					return true;
				}
			}
			
			return false;
		}
		
		private void QueueDrawAnchor(Anchor anchor)
		{
			int px = (int)(anchor.Location.X * ZoomLevel - ActiveGroup.X);
			int py = (int)(anchor.Location.Y * ZoomLevel - ActiveGroup.Y);

			QueueDrawArea(px - AnchorRadius * 2, py - AnchorRadius * 2, AnchorRadius * 4, AnchorRadius * 4);
		}
		
		private bool LinkAnchorTest(double x, double y)
		{
			x = Scaled(x + ActiveGroup.X);
			y = Scaled(y + ActiveGroup.Y);

			foreach (Wrappers.Wrapper obj in ActiveGroup.Children)
			{
				Wrappers.Link link = obj as Wrappers.Link;
				
				if (link == null)
				{
					continue;
				}
				
				if (LinkAnchorTest(link, link.From, link.FromAnchors, x, y))
				{
					return true;
				}
				
				if (LinkAnchorTest(link, link.To, link.ToAnchors, x, y))
				{
					return true;
				}
			}
			
			return false;
		}
		
		private void DoMouseInOut(double x, double y)
		{
			if (d_anchor != null)
			{
				d_anchor.Link.LinkFocus = false;

				QueueDrawAnchor(d_anchor);
			}

			d_anchor = null;
			
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
			return new Point(Scaled(Allocation.Width, new ScaledPredicate(Math.Ceiling)),
			                  Scaled(Allocation.Height, new ScaledPredicate(Math.Ceiling)));
		}
		
		private void ZoomAtPoint(int size, Point where)
		{
			size = Math.Max(Math.Min(size, MaxZoom), MinZoom);
			
			ActiveGroup.X += (int)(((where.X + ActiveGroup.X) * (double)size / ZoomLevel) - (where.X + ActiveGroup.X));
			ActiveGroup.Y += (int)(((where.Y + ActiveGroup.Y) * (double)size / ZoomLevel) - (where.Y + ActiveGroup.Y));
			
			ZoomLevel = size;
		}
		
		public void CenterView(Wrappers.Wrapper obj)
		{
			if (obj.Parent == null)
			{
				return;
			}

			SetActiveGroup(obj.Parent);
			
			UnselectAll();
			Select(obj);
			
			ZoomLevel = DefaultZoom;
			
			double x;
			double y;
			
			if (!LinkFilter(obj))
			{
				Wrappers.Link link = (Wrappers.Link)obj;
				Utils.MeanPosition(new Wrappers.Wrapper[] {link.From, link.To}, out x, out y);
			}
			else
			{
				x = obj.Allocation.X;
				y = obj.Allocation.Y;
			}

			ActiveGroup.X = (int)(x * ZoomLevel - Allocation.Width / 2.0f);
			ActiveGroup.Y = (int)(y * ZoomLevel - Allocation.Height / 2.0f);

			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private void DoZoom(bool zoomIn, Point pt)
		{
			int nsize = ZoomLevel + (int)Math.Floor(ZoomLevel * 0.2 * (zoomIn ? 1 : -1));

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
					if (!(obj is Wrappers.Group))
					{
						continue;
					}

					Wrappers.Group grp = obj as Wrappers.Group;
					double x, y;
					
					Utils.MeanPosition(grp.Children, out x, out y);
					
					grp.X = (int)(x * MinZoom - pt.X);
					grp.Y = (int)(y * MinZoom - pt.Y);
					
					SetActiveGroup(grp);
					ZoomLevel = MinZoom;
					
					break;
				}
			}
			else if (lowerReached && d_activeGroup.Parent != null)
			{
				Wrappers.Group newActive = d_activeGroup.Parent;
				
				double x, y;
				Utils.MeanPosition(newActive.Children, out x, out y);
					
				newActive.X = (int)(x * MaxZoom - pt.X);
				newActive.Y = (int)(y * MaxZoom - pt.Y);
				
				SetActiveGroup(newActive);
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
				ActiveGroup.X += dx * ZoomLevel;
				ActiveGroup.Y += dy * ZoomLevel;
				
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
			
			if (ActiveGroup.Length == 0)
			{
				return false;
			}
			
			if (pf == null)
			{
				d_focus = ActiveGroup.Children[direction == 1 ? 0 : ActiveGroup.Length - 1];
			}
			else
			{
				int nidx = ActiveGroup.IndexOf(pf) + direction;
				
				if (nidx >= ActiveGroup.Length || nidx < 0)
				{
					return false;
				}
				
				d_focus = ActiveGroup.Children[nidx];
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
			float ox = ActiveGroup.X % (float)ZoomLevel;
			float oy = ActiveGroup.Y % (float)ZoomLevel;
			
			graphics.Save();
			graphics.Translate(-ZoomLevel - ox, -ZoomLevel - oy);
			
			d_gridCache.Render(graphics, Allocation.Width + ZoomLevel * 2, Allocation.Height + ZoomLevel * 2, delegate (Cairo.Context ctx, int width, int height)
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
		
		private void DrawAnchor(Cairo.Context graphics, Anchor anchor)
		{
			graphics.Save();
			double radius = graphics.LineWidth * 5;
			
			graphics.LineWidth *= 3;
			
			graphics.MoveTo(anchor.Location.X + radius, anchor.Location.Y);
			graphics.Arc(anchor.Location.X, anchor.Location.Y, radius, 0, System.Math.PI * 2);
			graphics.SetSourceRGB(0.3, 0.3, 0.3);
			graphics.StrokePreserve();

			graphics.SetSourceRGB(0.8, 0.8, 0.3);
			graphics.Fill();
			
			graphics.Restore();
		}
		
		private void DrawObjects(Cairo.Context graphics)
		{
			graphics.Save();
			
			graphics.Translate(-ActiveGroup.X, -ActiveGroup.Y);
			graphics.Scale(ZoomLevel, ZoomLevel);
			graphics.LineWidth = 1.0 / ZoomLevel;

			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>();
			
			foreach (Wrappers.Wrapper obj in ActiveGroup.Children)
			{
				Wrappers.Link link = obj as Wrappers.Link;

				if (link == null)
				{
					objects.Add(obj);
				}
				else
				{
					DrawObject(graphics, obj);
				}
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
			
			if (evnt.Button != 2)
			{
				if (Selected(first) && (evnt.State & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask)) != 0)
				{
					Unselect(first);
					first = null;
				}
				
				if (!(Selected(first) || (evnt.State & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask)) != 0))
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
				Select(first);
				
				if (evnt.Button == 1)
				{
					Point res = ScaledPosition(evnt.X, evnt.Y, Math.Floor);
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

		protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
		{
			base.OnButtonReleaseEvent(evnt);
			
			d_isDragging = false;
			d_mouseRect = new Allocation(0, 0, 0, 0);
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

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
		{
			base.OnMotionNotifyEvent(evnt);
			
			if ((evnt.State & Gdk.ModifierType.Button2Mask) != 0)
			{
				DoMoveCanvas(evnt);
				return true;
			}
			
			if (!d_isDragging)
			{
				DoMouseInOut(evnt.X, evnt.Y);
				
				if (d_mouseRect.Width != 0 && d_mouseRect.Height != 0)
				{
					if (d_beforeDragSelection == null)
					{
						d_beforeDragSelection = new List<Wrappers.Wrapper>(d_selection);
					}

					DoDragRect((int)evnt.X, (int)evnt.Y, evnt.State);
				}
				
				return true;
			}
			
			Point position = ScaledPosition(evnt.X, evnt.Y, Math.Floor);
			Point size = UnitSize();
			
			int pxn = (int)Math.Floor((double)(ActiveGroup.X / ZoomLevel));
			int pyn = (int)Math.Floor((double)(ActiveGroup.Y / ZoomLevel));
			
			List<double> maxx = new List<double>();
			List<double> minx = new List<double>();
			List<double> maxy = new List<double>();
			List<double> miny = new List<double>();
			
			int[] translation = null;
			
			maxx.Add(size.X - 1 + pxn);
			minx.Add(position.X);
			maxy.Add(size.Y - 1 + pyn);
			miny.Add(position.Y);
			
			/* Check boundaries */
			List<Wrappers.Wrapper> selection = new List<Wrappers.Wrapper>(d_selection);
			selection.RemoveAll(item => !LinkFilter(item));
			
			foreach (Wrappers.Wrapper obj in selection)
			{
				Point pt = ScaledFromDragState(obj);
				Allocation alloc = obj.Allocation;
				
				minx.Add(-pt.X + pxn);
				maxx.Add(size.X - pt.X + pxn - alloc.Width);
				miny.Add(-pt.Y + pyn);
				maxy.Add(size.Y - pt.Y + pyn - alloc.Height);
				
				if (translation == null)
				{
					translation = new int[] {(int)pt.X, (int)pt.Y};
				}
			}
			
			if (position.X < Utils.Max(minx))
			{
				position.X = Utils.Max(minx);
			}
			
			if (position.X > Utils.Min(maxx))
			{
				position.X = Utils.Min(maxx);
			}
		
			if (position.Y < Utils.Max(miny))
			{
				position.Y = Utils.Max(miny);
			}
			
			if (position.Y > Utils.Min(maxy))
			{
				position.Y = Utils.Min(maxy);
			}
			
			int dx = (int)(position.X + translation[0]);
			int dy = (int)(position.Y + translation[1]);
			
			if (dx != (int)selection[0].Allocation.X ||
			    dy != (int)selection[0].Allocation.Y)
			{
				DoMove((int)(dx - selection[0].Allocation.X),
				       (int)(dy - selection[0].Allocation.Y),
				       false);

				ModifiedView(this, new EventArgs());
			}
			
			return true;
		}

		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			base.OnScrollEvent(evnt);
			
			if (d_isDragging || (evnt.State & Gdk.ModifierType.Button2Mask) != 0)
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
		
		protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
		{
			base.OnKeyPressEvent(evnt);
			
			if (evnt.Key == Gdk.Key.Home || evnt.Key == Gdk.Key.KP_Home)
			{
				CenterView();
			}
			else if (evnt.Key == Gdk.Key.Delete)
			{
				d_actions.Delete(d_activeGroup, Selection);
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
					}
					
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
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			using (Cairo.Context graphics = Gdk.CairoHelper.Create(evnt.Window))
			{
				graphics.Rectangle(evnt.Area.X, evnt.Area.Y, evnt.Area.Width, evnt.Area.Height);
				graphics.Clip();
				
				graphics.LineWidth = 1;
				DrawBackground(graphics);
				DrawGrid(graphics);
	
				DrawObjects(graphics);
				DrawSelectionRect(graphics);
				
				graphics.SetSourceRGB(0.6, 0.6, 0.6);
				graphics.MoveTo(0, 0.5);
				graphics.LineTo(Allocation.Width, 0.5);
				graphics.Stroke();
				
				graphics.MoveTo(0, Allocation.Height - 0.5);
				graphics.LineTo(Allocation.Width, Allocation.Height - 0.5);
				graphics.Stroke();
			}
			
			return true;
		}
		
		protected override void OnStyleSet(Gtk.Style previous_style)
		{
			base.OnStyleSet(previous_style);
			
			Cpg.Studio.Settings.Font = Style.FontDescription;
		}

	}
}
