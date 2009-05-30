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
			public Components.Group Group;
			public int GridSize;
			
			public StackItem(Components.Group group, int gridSize)
			{
				this.Group = group;
				this.GridSize = gridSize;
			}
		}
		
		public delegate void ObjectEventHandler(object source, Components.Object obj);
		public delegate void PopupEventHandler(object source, int button, long time); 
		
		public event ObjectEventHandler Activated = delegate {};
		public event ObjectEventHandler ObjectAdded = delegate {};
		public event ObjectEventHandler ObjectRemoved = delegate {};
		public event PopupEventHandler Popup = delegate {};
		public event ObjectEventHandler LeveledUp = delegate {};
		public event ObjectEventHandler LeveledDown = delegate {};
		public event EventHandler SelectionChanged = delegate {};
		public event EventHandler Modified = delegate {};
		public event EventHandler ModifiedView = delegate {};
		
		private int d_maxSize;
		private int d_minSize;
		private int d_defaultGridSize;
		private int d_gridSize;
		private Allocation d_mouseRect;
		private PointF d_origPosition;
		private PointF d_buttonPress;
		private PointF d_dragState;
		private bool d_isDragging;
		
		private Components.Object d_focus;
		
		private Components.Network d_network;
		private List<Components.Object> d_hover;
		private List<StackItem> d_objectStack;
		private List<Components.Object> d_selection;
		
		private float[] d_gridBackground;
		private float[] d_gridLine;
		
		public Grid() : base()
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
			
			LeveledUp += OnLevelUp;
			LeveledDown += OnLevelDown;
			SelectionChanged += OnSelectionChanged;
			
			d_maxSize = 120;
			d_minSize = 20;
			d_defaultGridSize = 50;
			d_gridSize = d_defaultGridSize;
			d_focus = null;
			
			d_gridBackground = new float[] {1f, 1f, 1f};
			d_gridLine = new float[] {0.9f, 0.9f, 0.9f};
			
			d_objectStack = new List<StackItem>();
			d_hover = new List<Components.Object>();
			d_selection = new List<Components.Object>();
			d_mouseRect = new Allocation(0f, 0f, 0f, 0f);
			
			d_network = new Components.Network();
			
			Clear();
		}
		
		public void Clear()
		{
			bool changed = d_objectStack.Count != 1 || d_objectStack[0].Group.Children.Count == 0;
			
			d_objectStack.Clear();
			d_objectStack.Insert(0, new StackItem(new Components.Group(), d_gridSize));
			QueueDraw();
			
			SelectionChanged(this, new EventArgs());
			
			if (changed)
				Modified(this, new EventArgs());
		}
		
		public void LevelUp(Components.Object obj)
		{
			LeveledUp(this, obj);
		}
		
		public void LevelDown(Components.Object obj)
		{
			LeveledDown(this, obj);
		}
		
		private void AddLink(Components.Link link)
		{
			// Check if link has no objects
			if (link.Empty())
				return;
			
			int offset = -1; 
			
			// Calculate offset
			foreach (Components.Link other in Links)
			{
				if (link.SameObjects(other) && other.Offset > offset)
					offset = other.Offset;
			}
			
			link.Offset = offset + 1;
			AddObject(link);
		}
		
		public Components.Object Root
		{
			get
			{
				return d_objectStack[d_objectStack.Count - 1].Group;
			}
		}
		
		public Components.Object[] Selection
		{
			get
			{
				return d_selection.ToArray();
			}
		}
		
		public void Attach()
		{
			if (d_selection.Count == 0)
				return;
			
			Components.Simulated[] selection = new Components.Simulated[d_selection.Count];
			
			for (int i = 0; i < d_selection.Count; ++i)
				selection[i] = d_selection[i] as Components.Simulated;

			Attach(selection);
		}
		
		public Components.Link[] Attach(Components.Simulated[] objs)
		{
			List<Components.Simulated> objects = new List<Components.Simulated>(objs);
			
			/* Remove links */
			objects.RemoveAll(delegate (Components.Simulated obj) { return obj is Components.Link; });

			List<Components.Link> added = new List<Components.Link>();
			
			if (objects.Count < 2)
				return added.ToArray();
			
			for (int i = 1; i < objects.Count; ++i)
			{
				Cpg.Link orig = new Cpg.Link("", objects[0].Object, objects[i].Object);
				
				d_network.AddObject(orig);

				Components.Link link = new Components.Link(orig, objects[0], objects[i]);
				AddLink(link);
				
				added.Add(link);
			}
			
			return added.ToArray();
		}
		
		private Components.Group Container
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
		
		public void Add(Components.Object obj)
		{
			int cx = (int)Math.Round((Container.X + Allocation.Width / 2.0) / (double)d_gridSize);
			int cy = (int)Math.Round((Container.Y + Allocation.Height / 2.0) / (double)d_gridSize);
			
			if (obj is Components.Link)
			{
				AddLink(obj as Components.Link);
			}
			else
			{
				Add(obj, cx, cy, 1, 1);
			}
		}
		
		private void EnsureUniqueId(Components.Object obj, string id)
		{
			List<string> ids = new List<string>();
			
			foreach (Components.Object o in Container.Children)
			{
				if (o != obj)
					ids.Add(o["id"]);
			}
			
			string newid = id;
			int num = 1;
			
			while (ids.IndexOf(newid) != -1)
			{
				newid = id + " (" + num + ")";
				num++;
			}
			
			if (newid != id)
				obj["id"] = newid;
		}
		
		private void DoObjectAdded(Components.Object obj)
		{
			ObjectAdded(this, obj);
			
			obj.RequestRedraw += new EventHandler(OnRequestRedraw);
			obj.PropertyAdded += delegate (Components.Object src, string name) { Modified(this, new EventArgs()); };
			obj.PropertyChanged += delegate (Components.Object src, string name) { Modified(this, new EventArgs()); };
			obj.PropertyRemoved += delegate (Components.Object src, string name) { Modified(this, new EventArgs()); };
			
			if (obj is Components.Simulated)
				d_network.AddObject((obj as Components.Simulated).Object);
			
			if (obj is Components.Group)
			{
				foreach (Components.Object child in (obj as Components.Group).Children)
					DoObjectAdded(child);
			}
			
			Modified(this, new EventArgs());
		}
		
		public List<Components.Link> Links
		{
			get
			{
				List<Components.Link> res = new List<Components.Link>();
				
				foreach (Components.Object obj in Container.Children)
				{
					if (obj is Components.Link)
						res.Add(obj as Components.Link);
				}
				
				return res;
			}
		}
		
		public void Add(Components.Object obj, int x, int y, int width, int height)
		{
			obj.Allocation.Assign(x, y, width, height);
			AddObject(obj);
		}
		
		private void AddObject(Components.Object obj)
		{
			EnsureUniqueId(obj, obj["id"]);
			
			if (obj is Components.Simulated)
				d_network.AddObject((obj as Components.Simulated).Object);

			Container.Add(obj);
			DoObjectAdded(obj);
			
			QueueDraw();
		}
		
		public void Remove(Components.Object obj)
		{
			if (!Container.Children.Remove(obj))
				return;

			if (!(obj is Components.Link) && obj is Components.Simulated)
			{
				/* Remove any associated links */
				Components.Object[] children = Container.Children.ToArray();
				foreach (Components.Object other in children)
				{
					if (!(other is Components.Link))
						continue;
				
					Components.Link link = other as Components.Link;
					
					if (link.From == obj || link.To == obj)
						Remove(link);
				}
			}
			
			ObjectRemoved(this, obj);
			obj.Removed();

			Unselect(obj);
			
			if (obj is Components.Simulated)
				d_network.RemoveObject((obj as Components.Simulated).Object);
			
			QueueDraw();
		}
		
		public void Unselect(Components.Object obj)
		{
			if (obj == null || !d_selection.Remove(obj))
				return;
			
			obj.Selected = false;
			
			QueueDrawObject(obj);
			SelectionChanged(this, new EventArgs());
		}
		
		public void Select(Components.Object obj)
		{
			if (d_selection.IndexOf(obj) != -1)
				return;
			
			d_selection.Add(obj);
			obj.Selected = true;
			
			QueueDrawObject(obj);
			SelectionChanged(this, new EventArgs());
		}
		
		private void QueueDrawObject(Components.Object obj)
		{
			if (Container.Children.IndexOf(obj) == -1)
				return;
			
			QueueDraw();
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
			return new PointF(Scaled(position.X, predicate), Scaled(position.Y, predicate));
		}
				
		private PointF ScaledPosition(double x, double y, ScaledPredicate predicate)
		{
			return ScaledPosition(new PointF((float)x, (float)y), predicate);
		}
		
		private PointF ScaledPosition(double x, double y)
		{
			return ScaledPosition(x, y, null);
		}
		
		private Components.Object[] SortedObjects()
		{
			List<Components.Object> objects = new List<Components.Object>();
			List<Components.Object> links = new List<Components.Object>();
			
			foreach (Components.Object obj in Container.Children)
			{
				if (obj is Components.Link)
					links.Add(obj);
				else
					objects.Add(obj);
			}
			
			links.Reverse();
			objects.Reverse();
			
			objects.AddRange(links);
			return objects.ToArray();
		}
		
		private List<Components.Object> HitTest(Allocation allocation)
		{
			List<Components.Object> res = new List<Components.Object>();
			Allocation rect = new Allocation(allocation);
			
			rect.X += Container.X;
			rect.Y += Container.Y;
			
			rect.X = Scaled(rect.X);
			rect.Y = Scaled(rect.Y);
			rect.Width = Scaled(rect.Width);
			rect.Height = Scaled(rect.Height);
			
			foreach (Components.Object obj in SortedObjects())
			{				
				if (!(obj is Components.Link))
				{
					if (obj.Allocation.Intersects(rect) && obj.HitTest(rect))
						res.Add(obj);
				}
				else
				{
					if ((obj as Components.Link).HitTest(rect, d_gridSize))
						res.Add(obj);
				}
			}
			
			return res;
		}
		
		private PointF MeanPosition(Components.Object[] objects)
		{
			PointF res = new PointF(0, 0);
			int num = 0;
			
			foreach (Components.Object obj in objects)
			{
				res.X += obj.Allocation.X + obj.Allocation.Width / 2.0f;
				res.Y += obj.Allocation.Y + obj.Allocation.Height / 2.0f;
				
				num += 1;
			}
			
			if (num != 0)
			{
				res.X = res.X / num;
				res.Y = res.Y / num;
			}
			
			return res;
		}
		
		public void CenterView()
		{
			PointF pos = MeanPosition(Container.Children.ToArray());
			
			pos.X *= d_gridSize;
			pos.Y *= d_gridSize;
			
			Container.X = (int)(pos.X - (Allocation.Width / 2.0f));
			Container.Y = (int)(pos.Y - (Allocation.Height / 2.0f));
			
			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private bool Selected(Components.Object obj)
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
			Components.Object first = d_selection.Find(delegate (Components.Object obj) { return !(obj is Components.Link); });
			
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
		
		private PointF ScaledFromDragState(Components.Object obj)
		{
			Allocation rect = d_selection[0].Allocation;
			Allocation aobj = obj.Allocation;
			
			return new PointF(d_dragState.X - (rect.X - aobj.X), d_dragState.Y - (rect.Y - aobj.Y));
		}
		
		private void DoDragRect(Gdk.EventMotion evnt)
		{
			d_mouseRect.Width = (int)evnt.X;
			d_mouseRect.Height = (int)evnt.Y;

			List<Components.Object> objects = HitTest(d_mouseRect.FromRegion());
			
			Components.Object[] selection = new Components.Object[d_selection.Count];
			d_selection.CopyTo(selection, 0);
		
			foreach (Components.Object obj in selection)
			{
				if (objects.IndexOf(obj) == -1)
					Unselect(obj);
			}
			
			foreach (Components.Object obj in objects)
			{
				if (!Selected(obj))
					Select(obj);
			}
			
			QueueDraw();
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
			List<Components.Object> objects = HitTest(new Allocation(x, y, 1, 1));
			
			d_hover.RemoveAll(delegate (Components.Object obj) {
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
				List<Components.Object> objects = HitTest(new Allocation(where.X, where.Y, 1f, 1f));
				
				foreach (Components.Object obj in objects)
				{
					if (!(obj is Components.Group))
						continue;

					Components.Group grp = obj as Components.Group;
					PointF pos = MeanPosition(grp.Children.ToArray());
					
					grp.X = (int)(pos.X * d_defaultGridSize - where.X);
					grp.Y = (int)(pos.Y * d_defaultGridSize - where.Y);
					
					LevelDown(grp);
					return;
				}
			}
			
			if (lowerReached && d_objectStack.Count > 1)
			{
				LevelUp(d_objectStack[1].Group);
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
			foreach (Components.Object obj in d_selection.ToArray())
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
				foreach (Components.Object obj in d_selection)
					obj.Allocation.Offset(dx, dy);
				
				if (d_selection.Count > 0)
					Modified(this, new EventArgs());
			}
			
			QueueDraw();
		}
		
		private void FocusRelease()
		{
			if (d_focus != null)
			{
				Components.Object o = d_focus;
				d_focus = null;
				
				o.KeyFocus = false;
			}
		}
		
		private bool FocusNext(int direction)
		{
			Components.Object pf = d_focus;
			
			FocusRelease();
			
			if (pf == null)
			{
				d_focus = Container.Children[direction == 1 ? 0 : Container.Children.Count - 1];
			}
			else
			{
				int nidx = Container.Children.IndexOf(pf) + direction;
				
				if (nidx >= Container.Children.Count || nidx < 0)
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
		
		private void FocusSet(Components.Object obj)
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
			PointF pt = UnitSize();
			
			graphics.Save();
			graphics.Scale(d_gridSize, d_gridSize);

			float ox = (Container.X / (float)d_gridSize) % 1;
			float oy = (Container.Y / (float)d_gridSize) % 1;
			
			graphics.LineWidth = 1 / (float)d_gridSize;
			
			double offset = graphics.LineWidth / 2;
			
			graphics.SetSourceRGB(d_gridLine[0], d_gridLine[1], d_gridLine[2]);
			
			for (int i = 0; i <= pt.X; ++i)
			{
				graphics.MoveTo(i - ox + offset, offset);
				graphics.LineTo(i - ox + offset, pt.Y +offset);
				graphics.Stroke();
			}
			
			for (int i = 0; i <= pt.Y; ++i)
			{
					graphics.MoveTo(0 + offset, i - oy + offset);
					graphics.LineTo(pt.X + offset, i - oy + offset);
					graphics.Stroke();
			}
			
			graphics.Restore();
		}
		
		private void DrawObject(Cairo.Context graphics, Components.Object obj)
		{
			if (obj == null)
				return;
			
			Rectangle alloc = obj.Allocation;
			graphics.Save();
			
			graphics.Translate(alloc.X, alloc.Y);
			graphics.LineWidth = 1.0f / d_gridSize;
			graphics.SetFontSize(10 / (double)d_gridSize);
			
			obj.Draw(graphics);
			
			graphics.Restore();
		}
		
		private void DrawObjects(Cairo.Context graphics)
		{
			graphics.Save();
			
			graphics.Translate(-Container.X, -Container.Y);
			graphics.Scale(d_gridSize, d_gridSize);

			List<Components.Object> objects = new List<Components.Object>();
			
			foreach (Components.Object obj in Container.Children)
			{
				if (!(obj is Components.Link))
					objects.Add(obj);
				else
					DrawObject(graphics, obj);
			}
			
			foreach (Components.Object obj in objects)
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
		
		public Components.Object[] NormalizeGroup(Components.Object[] objects, out Components.Object[] removed)
		{
			List<Components.Object> orig = new List<Components.Object>(objects);
			List<Components.Object> rm = new List<Components.Object>();
			
			/* Remove any links with objects not within the group */
			orig.RemoveAll(delegate (Components.Object obj) {
				if ((obj is Components.Link))
				{
					Components.Link link = obj as Components.Link;
					
					if (orig.IndexOf(link.From) == -1 || orig.IndexOf(link.To) == -1)
					{
						rm.Add(obj);
						return true;
					}
				}
				
				return false;
			});
			
			/* Add any links that are fully within the group, wrt from and to */
			foreach (Components.Object obj in Container.Children)
			{
				if (!(obj is Components.Link) || orig.IndexOf(obj) != -1 || rm.IndexOf(obj) != -1)
					continue;
				
				Components.Link link = obj as Components.Link;
				
				if (orig.IndexOf(link.From) != -1 && orig.IndexOf(link.To) != -1)
					orig.Add(obj);
			}
			
			removed = rm.ToArray();
			return orig.ToArray();
		}
		
		private bool DoGroupReal(Components.Object[] objects, Components.Simulated main, Type renderer)
		{
			/* Check if any objects have links to objects not in the group, except
			 * for the main object */
			List<Components.Object> objs = new List<Components.Object>(objects);

			foreach (Components.Object obj in Container.Children)
			{
				if (!(obj is Components.Link))
					continue;
				
				Components.Link link = obj as Components.Link;
				
				if (link.To == main as Components.Object || link.From == main as Components.Object)
					continue;
				
				if (objs.IndexOf(link.From) == -1 && objs.IndexOf(link.To) == -1 && objs.IndexOf(link) == -1)
					return false;
			}
			
			PointF pt = MeanPosition(objects);

			Components.Group group = new Components.Group(main);
			ConstructorInfo info = renderer.GetConstructor(new Type[] {typeof(Components.Group) });
			
			group.Renderer = info.Invoke(new object[] { group }) as Components.Renderers.Renderer;

			/* Copy positioning from current group */
			group.X = Container.X;
			group.Y = Container.Y;
			group.Allocation.X = (int)Math.Floor(pt.X);
			group.Allocation.Y = (int)Math.Floor(pt.Y);
			
			/* Reroute links going to main to the group (but not internally) */
			foreach (Components.Object obj in Container.Children)
			{
				if (!(obj is Components.Link))
					continue;
				
				Components.Link link = obj as Components.Link;
				
				if (link.From == group.Main)
					link.From = group;
				
				if (link.To == group.Main)
					link.To = group;
			}
			
			/* Remove objects from the grid and add them to the group */
			foreach (Components.Object obj in objects)
			{
				Container.Remove(obj);
				
				ObjectRemoved(this, obj);
				Unselect(obj);
				
				group.Add(obj);
			}
			
			/* Add group to current */
			Rectangle r = group.Allocation;
			
			Add(group, r.X, r.Y, r.Width, r.Height);
			Select(group);
			
			Modified(this, new EventArgs());
			QueueDraw();
			
			return true;
		}
		
		private void NewGroup(Components.Object[] objects)
		{
			Gtk.Dialog dlg = new Gtk.Dialog("Group properties", Toplevel as Window, 
			                                DialogFlags.DestroyWithParent,
			                                Gtk.Stock.Cancel, Gtk.ResponseType.Close,
			                                Gtk.Stock.Ok, Gtk.ResponseType.Ok);

			dlg.HasSeparator = false;
			GtkGui.GroupProperties widget = new GtkGui.GroupProperties(objects);
			
			dlg.VBox.PackStart(widget, true, true, 0);
			
			dlg.Response += delegate(object o, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Ok)
					DoGroupReal(objects, widget.Main, widget.Klass);
				
				dlg.Destroy();
			};
			
			dlg.ShowAll();
		}
		
		public bool Group(Components.Object[] objects)
		{
			/* Check if everything is in the group */
			Components.Object[] res;
			Components.Object[] removed;

			res = NormalizeGroup(objects, out removed);
			
			/* Check if there were removals and actual objects */
			if (removed.Length != 0)
				return false;
		
			/* Check if there is at least one non-link simulated object */
			bool hassim = false;
			
			foreach (Components.Object obj in res)
			{
				if (obj is Components.Simulated && !(obj is Components.Link))
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
		
		public void Ungroup(Components.Group group)
		{
			/* Reconnect attachments to main object */
			foreach (Components.Object obj in Container.Children)
			{
				if (!(obj is Components.Link))
					continue;
			
				Components.Link link = obj as Components.Link;
				
				if (link.From == group as Components.Simulated)
					link.From = group.Main;
				
				if (link.To == group as Components.Simulated)
					link.To = group.Main;
			}
			
			// TODO
			// CheckLinkOffsets(group.Main);
			
		}
		
		/* Callbacks */
		private void OnRequestRedraw(object source, EventArgs e)
		{
			QueueDrawObject(source as Components.Object);
		}
		
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			base.OnButtonPressEvent(evnt);
			
			GrabFocus();
			
			if (evnt.Button < 1 || evnt.Button > 3)
				return false;
			
			List<Components.Object> objects = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));
			Components.Object first = objects.Count > 0 ? objects[0] : null;

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
					Components.Object[] selection = new Components.Object[d_selection.Count];
					d_selection.CopyTo(selection, 0);
					
					foreach (Components.Object obj in selection)
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
				List<Components.Object> objects = HitTest(new Allocation(evnt.X, evnt.Y, 1, 1));
				Components.Object first = objects.Count > 0 ? objects[0] : null;
				
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
			List<KeyValuePair<Components.Object, PointF>> translation = new List<KeyValuePair<Components.Object, PointF>>(); 
			
			maxx.Add(size.X - 1 + pxn);
			minx.Add(position.X);
			maxy.Add(size.Y - 1 + pyn);
			miny.Add(position.Y);
			
			/* Check boundaries */
			foreach (Components.Object obj in d_selection)
			{
				PointF pt = ScaledFromDragState(obj);
				Allocation alloc = obj.Allocation;
				
				minx.Add(-pt.X + pxn);
				maxx.Add(size.X - pt.X + pxn - alloc.Width);
				miny.Add(-pt.Y + pyn);
				maxy.Add(size.Y - pt.Y + pyn - alloc.Height);
				
				translation.Add(new KeyValuePair<Components.Object, PointF>(obj, pt));
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
			
			foreach (KeyValuePair<Components.Object, PointF> item in translation)
			{
				Allocation alloc = item.Key.Allocation;
				
				if (alloc.X != item.Value.X + position.X || alloc.Y != item.Value.Y + position.Y)
				{
					alloc.Move(item.Value.X + position.X, item.Value.Y + position.Y);
					changed = true;
				}
			}
			
			if (changed)
				ModifiedView(this, new EventArgs());
			
			QueueDraw();
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
			
			foreach (Components.Object obj in d_hover)
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
			else if (evnt.Key == Gdk.Key.Tab || evnt.Key == Gdk.Key.ISO_Left_Tab)
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
				Components.Object obj = d_focus;
				
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

		private void OnLevelDown(object source, Components.Object obj)
		{
			if (Container.Children.IndexOf(obj) == -1 || !(obj is Components.Group))
				return;

			d_objectStack.Insert(0, new StackItem(obj as Components.Group, d_gridSize));
			d_gridSize = d_defaultGridSize;
			
			ModifiedView(this, new EventArgs());
			QueueDraw();			
		}
		
		private void OnLevelUp(object source, Components.Object obj)
		{
			if (!(obj is Components.Group) || d_objectStack.Count <= 1 || Container == (obj as Components.Group))
				return;

			StackItem item = d_objectStack[0];
			d_objectStack.RemoveAt(0);
			
			d_gridSize = item.GridSize;
			
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
			foreach (Components.Object obj in d_hover)
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
			}
			
			return true;
		}
	}
}
