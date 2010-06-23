using System;
using System.Collections.Generic;
using Gtk;
using System.Drawing;
using System.Reflection;

namespace Cpg.Studio
{
	public class Grid : DrawingArea
	{
		struct StackItem
		{
			public Wrappers.Group Group;
			public int GridSize;
			
			public StackItem(Wrappers.Group group, int gridSize)
			{
				this.Group = group;
				this.GridSize = gridSize;
			}
		}
		
		public delegate void ObjectEventHandler(object source, Wrappers.Wrapper obj);
		public delegate void PopupEventHandler(object source, int button, long time); 
		public delegate void ErrorHandler(object source, string error, string message);
		
		public event ObjectEventHandler Activated = delegate {};
		public event ObjectEventHandler ObjectAdded = delegate {};
		public event ObjectEventHandler ObjectRemoved = delegate {};
		public event PopupEventHandler Popup = delegate {};
		public event ObjectEventHandler LeveledUp = delegate {};
		public event ObjectEventHandler LeveledDown = delegate {};
		public event EventHandler SelectionChanged = delegate {};
		public event EventHandler Modified = delegate {};
		public event EventHandler ModifiedView = delegate {};
		public event ErrorHandler Error = delegate {};
		public event EventHandler Cleared = delegate {};
		
		private int d_maxSize;
		private int d_minSize;
		private int d_defaultGridSize;
		private int d_gridSize;
		private Allocation d_mouseRect;
		private PointF d_origPosition;
		private PointF d_buttonPress;
		private PointF d_dragState;
		private bool d_isDragging;
		
		private Wrappers.Wrapper d_focus;
		
		private Wrappers.Network d_network;
		private List<Wrappers.Wrapper> d_hover;
		private List<StackItem> d_objectStack;
		private List<Wrappers.Wrapper> d_selection;
		
		private float[] d_gridBackground;
		private float[] d_gridLine;
		
		private RenderCache d_gridCache;
		
		public Grid(Wrappers.Network network) : base()
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

			LeveledUp += OnLevelUp;
			LeveledDown += OnLevelDown;
			SelectionChanged += OnSelectionChanged;
			
			d_maxSize = 160;
			d_minSize = 10;
			d_defaultGridSize = 50;
			d_gridSize = d_defaultGridSize;
			d_focus = null;
			
			d_gridBackground = new float[] {1f, 1f, 1f};
			d_gridLine = new float[] {0.95f, 0.95f, 0.95f};
			
			d_objectStack = new List<StackItem>();
			d_hover = new List<Wrappers.Wrapper>();
			d_selection = new List<Wrappers.Wrapper>();
			d_mouseRect = new Allocation(0f, 0f, 0f, 0f);
			
			d_gridCache = new RenderCache();
		
			Clear();
		}
		
		public void Clear()
		{
			bool changed = d_objectStack.Count != 1 || d_objectStack[0].Group.Length == 0;
			
			d_objectStack.Clear();
			d_objectStack.Insert(0, new StackItem(new Wrappers.Group(), d_gridSize));
			QueueDraw();

			d_network.Clear();
			d_selection.Clear();
			
			SelectionChanged(this, new EventArgs());
			Cleared(this, new EventArgs());
			
			if (changed)
				Modified(this, new EventArgs());
		}
		
		public void LevelUp(Wrappers.Wrapper obj)
		{
			if (!(obj is Wrappers.Group) || d_objectStack.Count <= 1 || Container.Equals(obj))
				return;

			LeveledUp(this, obj);
		}
		
		private bool LevelDown(Wrappers.Group group, string objectid)
		{
			if (group.Id == objectid)
			{
				LevelDown(group);
				return true;
			}
			
			foreach (Wrappers.Wrapper obj in group.Children)
			{
				if (obj is Wrappers.Group)
				{
					if (LevelDown(obj as Wrappers.Group, objectid))
					{
						return true;
					}
				}
			}
			
			return false;
		}
		
		public void LevelDown(string objectid)
		{
			LevelDown(Root, objectid);			
		}
		
		public void LevelDown(Wrappers.Wrapper obj)
		{
			LeveledDown(this, obj);
		}
		
		private void AddLink(Wrappers.Link link)
		{
			// Check if link has no objects
			if (link.Empty)
				return;
			
			AddObject(link);
		}
		
		public Wrappers.Group Root
		{
			get
			{
				return d_objectStack[d_objectStack.Count - 1].Group;
			}
		}
		
		public Wrappers.Wrapper[] Selection
		{
			get
			{
				return d_selection.ToArray();
			}
		}
		
		public Wrappers.Link[] Attach()
		{
			if (d_selection.Count == 0)
				return null;
			
			List<Wrappers.Wrapper> selection = new List<Wrappers.Wrapper>();
			
			foreach (Wrappers.Wrapper obj in d_selection)
			{
				if (obj is Wrappers.Wrapper && !(obj is Wrappers.Link))
				{
					selection.Add(obj as Wrappers.Wrapper);
				}
			}

			return Attach(selection.ToArray());
		}
		
		private Wrappers.Link Attach(Wrappers.Wrapper from, Wrappers.Wrapper to)
		{
			Cpg.Link orig = new Cpg.Link("link", from, to);
				
			d_network.Add(orig);

			Wrappers.Link link = new Wrappers.Link(orig, from, to);
			AddLink(link);
			
			return link;
		}
		
		public Wrappers.Link[] Attach(Wrappers.Wrapper[] objs)
		{
			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>(objs);
			
			/* Remove links */
			objects.RemoveAll(delegate (Wrappers.Wrapper obj) { return obj is Wrappers.Link; });

			List<Wrappers.Link> added = new List<Wrappers.Link>();
			
			if (objects.Count == 0)
			{
				return added.ToArray();
			}
			
			int start = objects.Count == 1 ? 0 : 1;
			
			for (int i = start; i < objects.Count; ++i)
			{
				added.Add(Attach(objects[0], objects[i]));
			}
			
			return added.ToArray();
		}
		
		public Wrappers.Group Container
		{
			get
			{
				return d_objectStack[0].Group;
			}
		}
		
		private System.Drawing.Point Position
		{
			get
			{
				return new Point((int)Container.X, (int)Container.Y);
			}
		}
		
		public void Add(Wrappers.Wrapper obj)
		{
			int cx = (int)Math.Round((Container.X + Allocation.Width / 2.0) / (double)d_gridSize);
			int cy = (int)Math.Round((Container.Y + Allocation.Height / 2.0) / (double)d_gridSize);
			
			if (obj is Wrappers.Link)
			{
				AddLink(obj as Wrappers.Link);
			}
			else
			{
				Add(obj, cx, cy, 1, 1);
			}
		}
		
		private void DoObjectAdded(Wrappers.Wrapper obj)
		{
			ObjectAdded(this, obj);
			
			obj.RequestRedraw += new EventHandler(OnRequestRedraw);
			obj.PropertyAdded += delegate (Wrappers.Wrapper src, Cpg.Property prop) { Modified(this, new EventArgs()); };
			obj.PropertyChanged += delegate (Wrappers.Wrapper src, Cpg.Property prop) { Modified(this, new EventArgs()); };
			obj.PropertyRemoved += delegate (Wrappers.Wrapper src, Cpg.Property prop) { Modified(this, new EventArgs()); };
			
			if (d_network.GetChild(obj.Id) != obj)
			{
				d_network.Add(obj);
			}
			
			if (obj is Wrappers.Group)
			{
				foreach (Wrappers.Wrapper child in (obj as Wrappers.Group).Children)
				{
					DoObjectAdded(child);
				}
			}
			
			Modified(this, new EventArgs());
		}
		
		public List<Wrappers.Link> Links
		{
			get
			{
				List<Wrappers.Link> res = new List<Wrappers.Link>();
				
				foreach (Wrappers.Wrapper obj in Container.Children)
				{
					if (obj is Wrappers.Link)
					{
						res.Add(obj as Wrappers.Link);
					}
				}
				
				return res;
			}
		}
		
		public void Add(Wrappers.Wrapper obj, int x, int y, int width, int height)
		{
			obj.Allocation.Assign(x, y, width, height);
			AddObject(obj);
		}
		
		public void AddObject(Wrappers.Wrapper obj)
		{
			Container.Add(obj);
			
			Wrappers.Link link = obj as Wrappers.Link;
			
			if (link != null)
			{
				CheckLinkOffsets(link.From, link.To);
			}
			
			QueueDraw();
		}
		
		public void Remove(Wrappers.Wrapper obj)
		{
			if (!Container.Remove(obj))
			{
				return;
			}

			if (!(obj is Wrappers.Link) && obj is Wrappers.Wrapper)
			{
				/* Remove any associated links */
				Wrappers.Wrapper[] children = Container.Children;
				foreach (Wrappers.Wrapper other in children)
				{
					if (!(other is Wrappers.Link))
					{
						continue;
					}
				
					Wrappers.Link link = other as Wrappers.Link;
					
					if (link.From == obj || link.To == obj)
					{
						Remove(link);
					}
				}
			}
			
			ObjectRemoved(this, obj);
			obj.Removed();

			if (obj is Wrappers.Link)
			{
				/* Reoffset links */
				Wrappers.Link link = obj as Wrappers.Link;
				CheckLinkOffsets(link.From, link.To);
			}
			
			Unselect(obj);

			obj.Dispose();
			
			Modified(this, new EventArgs());
			QueueDraw();
		}
		
		public void Unselect(Wrappers.Wrapper obj)
		{
			if (obj == null || !d_selection.Remove(obj))
				return;
			
			obj.Selected = false;
			
			obj.DoRequestRedraw();
			SelectionChanged(this, new EventArgs());
		}
		
		private bool Select(Wrappers.Wrapper sim, Cpg.Object obj)
		{
			if (sim.WrappedObject == obj)
			{
				Wrappers.Group container = Wrappers.Wrapper.Wrap(sim.Parent) as Wrappers.Group;

				if (container != Container)
				{
					LevelUp(Root);
					LevelDown(container);
				}
				
				Select(sim);
				return true;
			}
			
			if (!(sim is Wrappers.Group))
			{
				return false;
			}
			
			Wrappers.Group group = sim as Wrappers.Group;
			
			foreach (Wrappers.Wrapper o in group.Children)
			{
				if (!(o is Wrappers.Wrapper))
				{
					continue;
				}
				
				if (Select(o as Wrappers.Wrapper, obj))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public bool Select(Cpg.Object obj)
		{
			return Select(Container, obj);
		}
		
		public void Select(Wrappers.Wrapper obj)
		{
			if (d_selection.IndexOf(obj) != -1)
				return;
			
			d_selection.Add(obj);
			obj.Selected = true;
			
			obj.DoRequestRedraw();
			SelectionChanged(this, new EventArgs());
		}
		
		private void QueueDrawObject(Wrappers.Wrapper obj)
		{
			if (Array.IndexOf<Wrappers.Wrapper>(Container.Children, obj) != -1)
			{
				return;
			}
			
			using (Cairo.Context graphics = Gdk.CairoHelper.Create(GdkWindow))
			{
				Allocation alloc = obj.Extents(d_gridSize, graphics);
				alloc.Round();
				
				alloc.Offset(-Container.X, -Container.Y);			
				QueueDrawArea((int)alloc.X, (int)alloc.Y, (int)alloc.Width, (int)alloc.Height);				
			};
		}
		
		delegate double ScaledPredicate(double val);
		private float Scaled(double pos, ScaledPredicate predicate)
		{
			return (float)(predicate != null ? predicate(pos / d_gridSize) : pos / d_gridSize);
		}

		private float Scaled(double pos)
		{
			return Scaled(pos, null);
		}
		
		private PointF ScaledPosition(PointF position, ScaledPredicate predicate)
		{
			return new PointF(Scaled(position.X + Container.X, predicate), Scaled(position.Y + Container.Y, predicate));
		}
				
		private PointF ScaledPosition(double x, double y, ScaledPredicate predicate)
		{
			return ScaledPosition(new PointF((float)x, (float)y), predicate);
		}
		
		private PointF ScaledPosition(double x, double y)
		{
			return ScaledPosition(x, y, null);
		}
		
		private Wrappers.Wrapper[] SortedObjects()
		{
			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>();
			List<Wrappers.Wrapper> links = new List<Wrappers.Wrapper>();
			
			foreach (Wrappers.Wrapper obj in Container.Children)
			{
				if (obj is Wrappers.Link)
					links.Add(obj);
				else
					objects.Add(obj);
			}
			
			links.Reverse();
			objects.Reverse();
			
			objects.AddRange(links);
			return objects.ToArray();
		}
		
		private List<Wrappers.Wrapper> HitTest(Allocation allocation)
		{
			List<Wrappers.Wrapper> res = new List<Wrappers.Wrapper>();
			Allocation rect = new Allocation(allocation);
			
			rect.X += Container.X;
			rect.Y += Container.Y;
			
			rect.X = Scaled(rect.X);
			rect.Y = Scaled(rect.Y);
			rect.Width = Scaled(rect.Width);
			rect.Height = Scaled(rect.Height);
			
			foreach (Wrappers.Wrapper obj in SortedObjects())
			{				
				if (!(obj is Wrappers.Link))
				{
					if (obj.Allocation.Intersects(rect) && obj.HitTest(rect))
						res.Add(obj);
				}
				else
				{
					if ((obj as Wrappers.Link).HitTest(rect, d_gridSize))
						res.Add(obj);
				}
			}
			
			return res;
		}
		
		public void CenterView()
		{
			double x, y;
			Utils.MeanPosition(Container.Children, out x, out y);
			
			x *= d_gridSize;
			y *= d_gridSize;
			
			Container.X = (int)(x - (Allocation.Width / 2.0f));
			Container.Y = (int)(y - (Allocation.Height / 2.0f));
			
			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private bool Selected(Wrappers.Wrapper obj)
		{
			return d_selection.IndexOf(obj) != -1;
		}
		
		private void UpdateDragState(PointF position)
		{
			if (d_selection.Count == 0)
			{
				d_isDragging = false;
				return;
			}
			
			/* The drag state contains the relative offset of the mouse to the first
			 * object in the selection. x and y are in unit coordinates */
			Wrappers.Wrapper first = d_selection.Find(delegate (Wrappers.Wrapper obj) { return !(obj is Wrappers.Link); });
			
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
		
		private PointF ScaledFromDragState(Wrappers.Wrapper obj)
		{
			Allocation rect = d_selection[0].Allocation;
			Allocation aobj = obj.Allocation;
			
			return new PointF(d_dragState.X - (rect.X - aobj.X), d_dragState.Y - (rect.Y - aobj.Y));
		}
		
		private void DoDragRect(Gdk.EventMotion evnt)
		{
			Allocation rect = d_mouseRect.FromRegion();
			rect.GrowBorder(2);
			QueueDrawArea((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
			
			d_mouseRect.Width = (int)evnt.X;
			d_mouseRect.Height = (int)evnt.Y;

			List<Wrappers.Wrapper> objects = HitTest(d_mouseRect.FromRegion());
			
			Wrappers.Wrapper[] selection = new Wrappers.Wrapper[d_selection.Count];
			d_selection.CopyTo(selection, 0);
		
			foreach (Wrappers.Wrapper obj in selection)
			{
				if (objects.IndexOf(obj) == -1)
					Unselect(obj);
			}
			
			foreach (Wrappers.Wrapper obj in objects)
			{
				if (!Selected(obj))
					Select(obj);
			}
			
			rect = d_mouseRect.FromRegion();
			rect.GrowBorder(2);
			QueueDrawArea((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
		}
		
		private void DoMoveCanvas(Gdk.EventMotion evnt)
		{
			int dx = (int)(evnt.X - d_buttonPress.X);
			int dy = (int)(evnt.Y - d_buttonPress.Y);
			
			Container.X = (int)(d_origPosition.X - dx);
			Container.Y = (int)(d_origPosition.Y - dy);
			
			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private void DoMouseInOut(double x, double y)
		{
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

			if (objects.Count != 0 && d_hover.IndexOf(objects[0]) == -1)
			{
				objects[0].MouseFocus = true;
				d_hover.Add(objects[0]);
			}
		}
		
		private PointF UnitSize()
		{
			return new PointF(Scaled(Allocation.Width, new ScaledPredicate(Math.Ceiling)),
			                  Scaled(Allocation.Height, new ScaledPredicate(Math.Ceiling)));
		}
		
		private void SetGridSize(int size, System.Drawing.Point where)
		{
			Container.X += (int)(((where.X + Container.X) * (double)size / d_gridSize) - (where.X + Container.X));
			Container.Y += (int)(((where.Y + Container.Y) * (double)size / d_gridSize) - (where.Y + Container.Y));
			
			bool changed = (size != d_gridSize);
			d_gridSize = size;
			
			if (changed)
				ModifiedView(this, new EventArgs());
			
			QueueDraw();
		}
		
		private void DoZoom(bool zoomIn, System.Drawing.Point where)
		{
			int nsize = d_gridSize + (int)Math.Floor(d_gridSize * 0.2 * (zoomIn ? 1 : -1));
			bool upperReached = false;
			bool lowerReached = false;
			
			if (nsize > d_maxSize)
			{
				nsize = d_maxSize;
				upperReached = true;
			}
			else if (nsize < d_minSize)
			{
				nsize = d_minSize;
				lowerReached = true;
			}
			
			if (upperReached)
			{
				List<Wrappers.Wrapper> objects = HitTest(new Allocation(where.X, where.Y, 1f, 1f));
				
				foreach (Wrappers.Wrapper obj in objects)
				{
					if (!(obj is Wrappers.Group))
						continue;

					Wrappers.Group grp = obj as Wrappers.Group;
					double x, y;
					
					Utils.MeanPosition(grp.Children, out x, out y);
					
					grp.X = (int)(x * d_defaultGridSize - where.X);
					grp.Y = (int)(y * d_defaultGridSize - where.Y);
					
					LevelDown(grp);
					return;
				}
			}
			
			if (lowerReached && d_objectStack.Count > 1)
			{
				Wrappers.Group group = d_objectStack[1].Group;
				LevelUp(group);
				
				if (Container == group)
				{
					double x, y;
					Utils.MeanPosition(group.Children, out x, out y);
					
					group.X = (int)(x * d_gridSize - where.X);
					group.Y = (int)(y * d_gridSize - where.Y);
				}
				
				return;
			}
			
			SetGridSize(nsize, where);
		}
		
		private void DoZoom(bool zoomIn)
		{
			DoZoom(zoomIn, new Point((int)(Allocation.Width / 2), (int)(Allocation.Height / 2)));
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
			System.Drawing.Point pt = new Point((int)(Allocation.Width / 2), (int)(Allocation.Height / 2));
			SetGridSize(d_defaultGridSize,pt);
		}
		
		public void DeleteSelected()
		{
			foreach (Wrappers.Wrapper obj in d_selection.ToArray())
				Remove(obj);
		}
		
		private void DoMove(int dx, int dy, bool moveCanvas)
		{
			if (dx == 0 && dy == 0)
				return;
			
			if (moveCanvas)
			{
				Container.X += dx * d_gridSize;
				Container.Y += dy * d_gridSize;
				
				ModifiedView(this, new EventArgs());
			}
			else
			{
				foreach (Wrappers.Wrapper obj in d_selection)
				{
					if (!(obj is Wrappers.Link))
						obj.MoveRel(dx, dy);
				}
				
				if (d_selection.Count > 0)
					Modified(this, new EventArgs());
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
			
			if (Container.Length == 0)
			{
				return false;
			}
			
			if (pf == null)
			{
				d_focus = Container.Children[direction == 1 ? 0 : Container.Length - 1];
			}
			else
			{
				int nidx = Container.IndexOf(pf) + direction;
				
				if (nidx >= Container.Length || nidx < 0)
					return false;
				
				d_focus = Container.Children[nidx];
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
			float ox = Container.X % (float)d_gridSize;
			float oy = Container.Y % (float)d_gridSize;
			
			graphics.Save();
			graphics.Translate(-d_gridSize - ox, -d_gridSize - oy);
			
			d_gridCache.Render(graphics, Allocation.Width + d_gridSize * 2, Allocation.Height + d_gridSize * 2, delegate (Cairo.Context ctx, int width, int height)
			{
				double offset = 0.5;
				ctx.LineWidth = 1;
				
				ctx.SetSourceRGB(d_gridLine[0], d_gridLine[1], d_gridLine[2]);
				
				int i = 0;
				while (i <= width)
				{
					ctx.MoveTo(i - offset, offset);
					ctx.LineTo(i - offset, height + offset);
					
					i += d_gridSize;
				}
				
				i = 0;
				while (i <= height)
				{
					ctx.MoveTo(offset, i - offset);
					ctx.LineTo(width + offset, i - offset);
					
					i += d_gridSize;
				}
				
				ctx.Stroke();
			});
			
			graphics.Restore();
		}
		
		private void DrawObject(Cairo.Context graphics, Wrappers.Wrapper obj)
		{
			if (obj == null)
				return;
			
			Rectangle alloc = obj.Allocation;
			graphics.Save();
			graphics.Translate(alloc.X, alloc.Y);

			obj.Draw(graphics);
			graphics.Restore();
		}
		
		private void DrawObjects(Cairo.Context graphics)
		{
			graphics.Save();
			
			graphics.Translate(-Container.X, -Container.Y);
			graphics.Scale(d_gridSize, d_gridSize);
			graphics.LineWidth = 1.0 / d_gridSize;

			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>();
			
			foreach (Wrappers.Wrapper obj in Container.Children)
			{
				if (!(obj is Wrappers.Link))
					objects.Add(obj);
				else
					DrawObject(graphics, obj);
			}
			
			foreach (Wrappers.Wrapper obj in objects)
				DrawObject(graphics, obj);

			graphics.Restore();
		}
		
		private void DrawSelectionRect(Cairo.Context graphics)
		{
			Allocation rect = d_mouseRect.FromRegion();
			
			if (rect.Width <= 1 || rect.Height <= 1)
				return;
			
			graphics.Rectangle(rect.X + 0.5, rect.Y + 0.5, rect.Width, rect.Height);
			graphics.SetSourceRGBA(0.7, 0.8, 0.7, 0.3);
			graphics.FillPreserve();
			
			graphics.SetSourceRGBA(0, 0.3, 0, 0.6);
			graphics.Stroke();
		}
		
		public List<Wrappers.Wrapper> Normalize(List<Wrappers.Wrapper> objects)
		{
			Wrappers.Wrapper[] objs = new Wrappers.Wrapper[objects.Count];
			objects.CopyTo(objs, 0);

			Wrappers.Wrapper[] ret = Normalize(objs);			
			return new List<Wrappers.Wrapper>(ret);
		}
		
		public Wrappers.Wrapper[] Normalize(Wrappers.Wrapper[] objects)
		{
			Wrappers.Wrapper[] removed;
			
			return Normalize(objects, out removed);
		}
				
		public Wrappers.Wrapper[] Normalize(Wrappers.Wrapper[] objects, out Wrappers.Wrapper[] removed)
		{
			List<Wrappers.Wrapper> orig = new List<Wrappers.Wrapper>(objects);
			List<Wrappers.Wrapper> rm = new List<Wrappers.Wrapper>();
						
			/* Remove any links with objects not within the group */
			orig.RemoveAll(delegate (Wrappers.Wrapper obj) {
				if ((obj is Wrappers.Link))
				{
					Wrappers.Link link = obj as Wrappers.Link;
					
					if (!Utils.In(link.From, orig) || !Utils.In(link.To, orig))
					{
						rm.Add(obj);
						return true;
					}
				}
				
				return false;
			});
			
			/* Add any links that are fully within the group, wrt from and to */
			foreach (Wrappers.Wrapper obj in Container.Children)
			{
				if (!(obj is Wrappers.Link) || Utils.In(obj, orig) || Utils.In(obj, rm))
					continue;
				
				Wrappers.Link link = obj as Wrappers.Link;
				
				if (Utils.In(link.From, orig) && Utils.In(link.To, orig))
				{
					orig.Add(obj);
				}
			}
			
			removed = rm.ToArray();
			return orig.ToArray();
		}
		
		private bool DoGroupReal(Wrappers.Wrapper[] objects, Wrappers.Wrapper main, Type renderer)
		{
			/* Check if any objects have links to objects not in the group, except
			 * for the main object */
			List<Wrappers.Wrapper> objs = new List<Wrappers.Wrapper>(objects);

			foreach (Wrappers.Wrapper obj in Container.Children)
			{
				if (!(obj is Wrappers.Link))
					continue;
				
				Wrappers.Link link = obj as Wrappers.Link;
				
				if (link.To.Equals(main) || link.From.Equals(main))
					continue;
				
				if (Utils.In(link.From, objs) != Utils.In(link.To, objs))
					return false;
			}
			
			double x, y;
			Utils.MeanPosition(objs, out x, out y);

			//TODO
			/*Wrappers.Group group = new Wrappers.Group(main);
			group.RendererType = renderer;

			// Copy positioning from current group
			group.X = Container.X;
			group.Y = Container.Y;
			group.Allocation.X = (int)Math.Floor(x);
			group.Allocation.Y = (int)Math.Floor(y);
			
			// Reroute links going to main to the group (but not internally)
			foreach (Wrappers.Wrapper obj in Container.Children)
			{
				if (!(obj is Wrappers.Link))
					continue;
				
				Wrappers.Link link = obj as Wrappers.Link;
				
				if (link.From.Equals(group.Main) && Array.IndexOf(objects, link.To) == -1)
					link.From = group;
				
				if (link.To.Equals(group.Main) && Array.IndexOf(objects, link.From) == -1)
					link.To = group;
			}
			
			// Remove objects from the grid and add them to the group
			foreach (Wrappers.Wrapper obj in objects)
			{
				Container.Remove(obj);
				
				ObjectRemoved(this, obj);
				Unselect(obj);
				
				group.Add(obj);
			}
			
			// Add group to current
			Rectangle r = group.Allocation;
			
			Add(group, r.X, r.Y, r.Width, r.Height);
			
			group.Rename();
			
			Select(group);
			
			Modified(this, new EventArgs());
			QueueDraw();*/
			
			return true;
		}
		
		private void NewGroup(Wrappers.Wrapper[] objects)
		{
			Gtk.Dialog dlg = new Gtk.Dialog("Group", Toplevel as Window, 
			                                DialogFlags.DestroyWithParent,
			                                Gtk.Stock.Cancel, Gtk.ResponseType.Close,
			                                Gtk.Stock.Ok, Gtk.ResponseType.Ok);

			dlg.HasSeparator = false;
			dlg.BorderWidth = 12;
			
			dlg.SetDefaultSize(300, 100);

			GroupProperties widget = new GroupProperties(objects);
			
			dlg.VBox.PackStart(widget, false, false, 0);
			dlg.VBox.Spacing = 12;
			
			dlg.Response += delegate(object o, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Ok)
				{
					if (!DoGroupReal(objects, widget.Main, widget.Klass))
					{
						Error(this, "Error while grouping", "There are still links connected from outside the group");
					}
				}
				
				dlg.Destroy();
			};
			
			dlg.ShowAll();
		}
		
		public bool Group(Wrappers.Wrapper[] objects)
		{
			/* Check if everything is in the group */
			Wrappers.Wrapper[] res;
			Wrappers.Wrapper[] removed;

			res = Normalize(objects, out removed);
			
			/* Check if there were removals and actual objects */
			if (removed.Length != 0)
				return false;
		
			/* Check if there is at least one non-link simulated object */
			bool hassim = false;
			
			foreach (Wrappers.Wrapper obj in res)
			{
				if (obj is Wrappers.Wrapper && !(obj is Wrappers.Link))
					hassim = true;
			}
			
			if (!hassim)
				return false;
			
			/* Ask for main object etc */
			NewGroup(res);
			return true;
		}
		
		public bool Group()
		{
			return Group(d_selection.ToArray());
		}
		
		private void CheckLinkOffsets()
		{
			foreach (Wrappers.Wrapper obj in Container.Children)
			{
				Wrappers.Link link = obj as Wrappers.Link;
				
				if (link != null)
				{
					CheckLinkOffsets(link.From, link.To);
				}
			}
		}
		
		private void CheckLinkOffsets(Wrappers.Wrapper from, Wrappers.Wrapper to)
		{
			if (from == null)
			{
				foreach (Wrappers.Link l1 in to.Links)
				{
					CheckLinkOffsets(l1.From, l1.To);
				}
			}
			else
			{
				bool flexor = false;
				
				List<Wrappers.Link> first = new List<Wrappers.Link>();
				List<Wrappers.Link> second = new List<Wrappers.Link>();
				
				foreach (Wrappers.Link l1 in to.Links)
				{
					if (l1.From == from)
						first.Add(l1);
				}
				
				foreach (Wrappers.Link l1 in from.Links)
				{
					if (l1.From == to)
					{
						second.Add(l1);
					}
				}
				
				flexor = second.Count > 0;
				int offset = 0;
				
				if (first.Count == 0)
				{
					flexor = true;
					offset -= 1;
				}
				
				if (flexor)
				{
					foreach (Wrappers.Link l1 in second)
					{
						l1.Offset = ++offset;
					}
					
					offset = 1;
				}
				
				foreach (Wrappers.Link l1 in first)
				{
					l1.Offset = offset++;
				}
			}
		}
		
		public void Ungroup(Wrappers.Group group)
		{
			// Reconnect attachments to main object
			// TODO
			/*
			foreach (Wrappers.Wrapper obj in group.Parent.Children)
			{
				if (!(obj is Wrappers.Link))
					continue;
			
				Wrappers.Link link = obj as Wrappers.Link;
				
				if (link.From.Equals(group))
				{
					link.From = group.Main;
				}
				
				if (link.To.Equals(group))
				{
					link.To = group.Main;
				}
			}
			
			if (group.Main != null)
			{
				// Check all links 'to' group.Main
				CheckLinkOffsets(null, group.Main);
			}

			group.Links.Clear();
			
			// Preserve layout and center
			int x = (int)group.Allocation.X;
			int y = (int)group.Allocation.Y;
			
			double xx, yy;
			Utils.MeanPosition(group.Children, out xx, out yy);
			
			// Remove the group
			Remove(group);
			
			// Change id of main to resemble id of the group
			if (group.Main != null)
			{
				string pid = group.Id;
				
				if (pid != String.Empty)
					group.Main.Id = pid;
			}
			
			foreach (Wrappers.Wrapper obj in group.Children)
			{
				if (!(obj is Wrappers.Link))
				{
					obj.Allocation.X = (int)Math.Round((x + (obj.Allocation.X - xx) + 0.00001));
					obj.Allocation.Y = (int)Math.Round((y + (obj.Allocation.Y - yy) + 0.00001));
				}
				
				Add(obj, (int)obj.Allocation.X, (int)obj.Allocation.Y, (int)obj.Allocation.Width, (int)obj.Allocation.Height);
				Select(obj);
			}
			
			ModifiedView(this, new EventArgs());
			QueueDraw();*/
		}
		
		public int GridSize
		{
			get
			{
				return d_gridSize;
			}
			set
			{
				d_gridSize = Math.Max(Math.Min(value, d_maxSize), d_minSize);
				QueueDraw();
			}
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
				return false;
			
			List<Wrappers.Wrapper> objects = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));
			Wrappers.Wrapper first = objects.Count > 0 ? objects[0] : null;

			if (evnt.Type == Gdk.EventType.TwoButtonPress)
			{
				if (first == null)
					return false;
				
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
						Unselect(obj);
				}
			}
			
			if (evnt.Button != 2 && first != null)
			{
				Select(first);
				
				if (evnt.Button == 1)
				{
					PointF res = ScaledPosition(evnt.X, evnt.Y, Math.Floor);
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
			
			if (evnt.Type == Gdk.EventType.ButtonRelease)
			{
				List<Wrappers.Wrapper> objects = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));
				Wrappers.Wrapper first = objects.Count > 0 ? objects[0] : null;
				
				if (first != null)
				{
					PointF pos = ScaledPosition(evnt.X, evnt.Y);
					first.Clicked(pos);
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
					DoDragRect(evnt);
				
				return true;
			}
			
			PointF position = ScaledPosition(evnt.X, evnt.Y, Math.Floor);
			PointF size = UnitSize();
			
			int pxn = (int)Math.Floor((double)(Container.X / d_gridSize));
			int pyn = (int)Math.Floor((double)(Container.Y / d_gridSize));
			
			List<float> maxx = new List<float>();
			List<float> minx = new List<float>();
			List<float> maxy = new List<float>();
			List<float> miny = new List<float>();
			List<KeyValuePair<Wrappers.Wrapper, PointF>> translation = new List<KeyValuePair<Wrappers.Wrapper, PointF>>(); 
			
			maxx.Add(size.X - 1 + pxn);
			minx.Add(position.X);
			maxy.Add(size.Y - 1 + pyn);
			miny.Add(position.Y);
			
			/* Check boundaries */
			foreach (Wrappers.Wrapper obj in d_selection)
			{
				if (obj is Wrappers.Link)
					continue;

				PointF pt = ScaledFromDragState(obj);
				Allocation alloc = obj.Allocation;
				
				minx.Add(-pt.X + pxn);
				maxx.Add(size.X - pt.X + pxn - alloc.Width);
				miny.Add(-pt.Y + pyn);
				maxy.Add(size.Y - pt.Y + pyn - alloc.Height);
				
				translation.Add(new KeyValuePair<Wrappers.Wrapper, PointF>(obj, pt));
			}
			
			if (position.X < Utils.Max(minx))
				position.X = Utils.Max(minx);
			
			if (position.X > Utils.Min(maxx))
				position.X = Utils.Min(maxx);
		
			if (position.Y < Utils.Max(miny))
				position.Y = Utils.Max(miny);
			
			if (position.Y > Utils.Min(maxy))
				position.Y = Utils.Min(maxy);
			
			bool changed = false;
			
			foreach (KeyValuePair<Wrappers.Wrapper, PointF> item in translation)
			{
				Allocation alloc = item.Key.Allocation;
				
				if (alloc.X != item.Value.X + position.X || alloc.Y != item.Value.Y + position.Y)
				{
					item.Key.DoRequestRedraw();
					item.Key.Move(item.Value.X + position.X, item.Value.Y + position.Y);
					item.Key.DoRequestRedraw();

					changed = true;
				}
			}
			
			if (changed)
				ModifiedView(this, new EventArgs());
			
			return true;
		}

		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			base.OnScrollEvent(evnt);
			
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
				obj.MouseFocus = false;
		
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
			
			if (evnt.Key == Gdk.Key.Delete)
			{
				DeleteSelected();
			}
			else if (evnt.Key == Gdk.Key.Home || evnt.Key == Gdk.Key.KP_Home)
			{
				CenterView();
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
					Activated(this, d_selection[0]);
				else
					return false;
			}
			else
			{
				return false;
			}
			
			return true;
		}
		
		public void UnselectAll()
		{
			List<Wrappers.Wrapper> copy = new List<Wrappers.Wrapper>(d_selection);
			
			foreach (Wrappers.Wrapper o in copy)
				Unselect(o);
		}

		private void OnLevelDown(object source, Wrappers.Wrapper obj)
		{
			if (!Container.Contains(obj) || !(obj is Wrappers.Group))
			{
				return;
			}

			d_objectStack.Insert(0, new StackItem(obj as Wrappers.Group, d_gridSize));
			d_gridSize = d_defaultGridSize;
			
			CheckLinkOffsets();
			
			UnselectAll();

			ModifiedView(this, new EventArgs());
			QueueDraw();			
		}
		
		private void OnLevelUp(object source, Wrappers.Wrapper obj)
		{
			StackItem item = d_objectStack[0];
			d_objectStack.RemoveAt(0);
			
			d_gridSize = item.GridSize;
			
			UnselectAll();

			CheckLinkOffsets();
			ModifiedView(this, new EventArgs());
			
			QueueDraw();
			LevelUp(obj);
		}
		
		private void OnSelectionChanged(object source, EventArgs args)
		{
			FocusRelease();
		}
		
		protected override bool OnFocusOutEvent(Gdk.EventFocus evnt)
		{
			foreach (Wrappers.Wrapper obj in d_hover)
				obj.MouseFocus = false;
			
			d_hover.Clear();
			FocusRelease();
			
			return base.OnFocusOutEvent(evnt);
		}
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			using ( Cairo.Context graphics = Gdk.CairoHelper.Create(evnt.Window) )
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
