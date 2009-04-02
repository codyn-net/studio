using System;
using System.Collections.Generic;
using Gtk;
using System.Drawing;
using System.Drawing.Drawing2D;

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
		
		public event ObjectEventHandler Activated;
		public event ObjectEventHandler ObjectAdded;
		public event ObjectEventHandler ObjectRemoved;
		public event PopupEventHandler Popup;
		public event ObjectEventHandler LevelUp;
		public event ObjectEventHandler LevelDown;
		public event EventHandler SelectionChanged;
		public event EventHandler Modified;
		public event EventHandler ModifiedView;
		
		private int d_maxSize;
		private int d_minSize;
		private int d_defaultGridSize;
		private int d_gridSize;
		private Rectangle d_mouseRect;
		private Point d_origPosition;
		private Point d_buttonPress;
		private Point d_dragState;
		private bool d_isDragging;
		
		private Components.Object d_focus;
		
		private Components.Network d_network;
		private List<Components.Object> d_hover;
		private List<StackItem> d_objectStack;
		private List<Components.Object> d_selection;
		
		private SolidBrush d_gridBackground;
		private SolidBrush d_gridLine;
		
		public Grid() : base()
		{
			AddEvents((int)(Gdk.EventMask.Button1MotionMask |
					  Gdk.EventMask.Button3MotionMask |
					  Gdk.EventMask.ButtonPressMask |
					  Gdk.EventMask.PointerMotionMask |
					  Gdk.EventMask.ButtonReleaseMask |
					  Gdk.EventMask.KeyPressMask |
					  Gdk.EventMask.KeyReleaseMask |
					  Gdk.EventMask.LeaveNotifyMask));
			
			CanFocus = true;
			
			LevelUp += OnLevelUp;
			LevelDown += OnLevelDown;
			SelectionChanged += OnSelectionChanged;
			
			d_maxSize = 120;
			d_minSize = 20;
			d_defaultGridSize = 50;
			d_gridSize = d_defaultGridSize;
			d_focus = null;
			
			d_gridBackground = new SolidBrush(Color.FromArgb(255, 255, 255));
			d_gridLine = new SolidBrush(Color.FromArgb(230, 230, 230));
			
			d_objectStack = new List<StackItem>();
			d_hover = new List<Components.Object>();
			d_selection = new List<Components.Object>();
			
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
			
			if (changed && Modified != null)
				Modified(this, new EventArgs());
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
			
			link.Offset = offset;
			AddObject(link);
		}
		
		public void Attach()
		{
			Attach(d_selection.ToArray() as Components.Simulated[]);
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

				Components.Link link = new Components.Link(orig);
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
		
		private Point Position
		{
			get
			{
				return new Point((int)Container.X, (int)Container.Y);
			}
		}
		
		public void Add(Components.Object obj)
		{
			int cx = (int)Math.Round((Container.Allocation.X + Allocation.Width / 2.0) / (double)d_gridSize);
			int cy = (int)Math.Round((Container.Allocation.Y + Allocation.Height / 2.0) / (double)d_gridSize);
			
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
			obj.Allocation = new System.Drawing.Rectangle(x, y, width, height);
			AddObject(obj);
		}
		
		private void AddObject(Components.Object obj)
		{
			EnsureUniqueId(obj, obj["id"]);
			
			Container.Add(obj);
			DoObjectAdded(obj);
		}
		
		public void Remove(Components.Object obj)
		{
			if (!Container.Children.Remove(obj))
				return;

			if (!(obj is Components.Link) && obj is Components.Simulated)
			{
				/* Remove any associated links */
				foreach (Components.Object other in Container.Children)
				{
					if (!(other is Components.Link))
						continue;
				
					Components.Link link = other as Components.Link;
					
					if (link.From == other || link.To == other)
						Remove(link);
				}
			}
			
			ObjectRemoved(this, obj);
			obj.Removed();

			Unselect(obj);
			
			if (obj is Components.Simulated)
				d_network.RemoveObject((obj as Components.Simulated).Object);
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
		private int Scaled(double pos, ScaledPredicate predicate)
		{
			return (int)predicate(pos / d_gridSize);
		}

		private int Scaled(double pos)
		{
			return Scaled(pos, new ScaledPredicate(Math.Floor));
		}
		
		private Point ScaledPosition(Point position, ScaledPredicate predicate)
		{
			return new Point(Scaled(position.X, predicate), Scaled(position.Y, predicate));
		}
		
		private Point ScaledPosition(Point position)
		{
			return ScaledPosition(position, new ScaledPredicate(Math.Floor));
		}
		
		private Point ScaledPosition(int x, int y)
		{
			return ScaledPosition(new Point(x, y));
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
		
		private List<Components.Object> HitTest(Rectangle rect)
		{
			List<Components.Object> res = new List<Components.Object>();
			
			rect.X += Container.Allocation.X;
			rect.Y += Container.Allocation.Y;
			
			rect.X = Scaled(rect.X);
			rect.Y = Scaled(rect.Y);
			rect.Width = Scaled(rect.Width);
			rect.Height = Scaled(rect.Height);
			
			foreach (Components.Object obj in SortedObjects())
			{
				if (!(obj is Components.Link))
				{
					if (rect.IntersectsWith(obj.Allocation) && obj.HitTest(rect))
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
		
		private double[] MeanPosition(Components.Object[] objects)
		{
			double[] res = new double[] {0, 0};
			int num = 0;
			
			foreach (Components.Object obj in objects)
			{
				res[0] += obj.Allocation.X + obj.Allocation.Width / 2.0;
				res[1] += obj.Allocation.Y + obj.Allocation.Height / 2.0;
				
				num += 1;
			}
			
			if (num != 0)
			{
				res[0] = res[0] / num;
				res[1] = res[1] / num;
			}
			
			return res;
		}
		
		public void CenterView()
		{
			double[] pos = MeanPosition(Container.Children.ToArray());
			
			pos[0] *= d_gridSize;
			pos[1] *= d_gridSize;
			
			Container.X = (int)(pos[0] - (Allocation.Width / 2.0));
			Container.Y = (int)(pos[1] - (Allocation.Height / 2.0));
			
			ModifiedView(this, new EventArgs());
			QueueDraw();
		}
		
		private bool Selected(Components.Object obj)
		{
			return d_selection.IndexOf(obj) != -1;
		}
		
		private void UpdateDragState(Point position)
		{
			// TODO
		}
		
		private Point ScaledFromDragState(Components.Object obj)
		{
			Rectangle rect = d_selection[0].Allocation;
			Rectangle aobj = obj.Allocation;
			
			return new Point(d_dragState.X - (rect.X - aobj.X), d_dragState.Y - (rect.Y - aobj.Y));
		}
		
		private void DoDragRect(Gdk.EventMotion evnt)
		{
			d_mouseRect.Width = (int)evnt.X;
			d_mouseRect.Height = (int)evnt.Y;
			
			List<Components.Object> objects = HitTest(d_mouseRect);
		
			foreach (Components.Object obj in d_selection)
			{
				if (objects.IndexOf(obj) == -1)
					Unselect(obj);
			}
			
			foreach (Components.Object obj in objects)
			{
				if (!Selected(obj))
					Select(obj);
			}
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
		
		private void DoMouseInOut(Gdk.EventMotion evnt)
		{
			List<Components.Object> objects = HitTest(new Rectangle((int)evnt.X, (int)evnt.Y, 1, 1));
			
			if (objects.Count == 0)
				return;
			
			foreach (Components.Object obj in d_hover)
			{
				if (obj != objects[0])
				{
					obj.MouseExit();
					d_hover.Remove(obj);
				}				
			}
			
			if (d_hover.IndexOf(objects[0]) != -1)
			{
				objects[0].MouseEnter();
				d_hover.Add(objects[0]);
			}
		}
		
		private Point UnitSize()
		{
			return new Point(Scaled(Allocation.Width, new ScaledPredicate(Math.Ceiling)),
			                 Scaled(Allocation.Height, new ScaledPredicate(Math.Ceiling)));
		}
		
		private void DoZoom(bool zoomIn, Point where)
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
				List<Components.Object> objects = HitTest(new Rectangle(where.X, where.Y, 1, 1));
				
				foreach (Components.Object obj in objects)
				{
					if (!(obj is Components.Group))
						continue;

					Components.Group grp = obj as Components.Group;
					double[] pos = MeanPosition(grp.Children.ToArray());
					
					grp.X = (int)(pos[0] * d_defaultGridSize - where.X);
					grp.Y = (int)(pos[1] * d_defaultGridSize - where.Y);
					
					LevelDown(this, grp);
					return;
				}
			}
			
			if (lowerReached && d_objectStack.Count > 1)
			{
				LevelUp(this, d_objectStack[1].Group);
				return;
			}
			
			Container.X += (int)(((where.X + Container.X) * (double)nsize / d_gridSize) - (where.X + Container.X));
			Container.Y += (int)(((where.Y + Container.Y) * (double)nsize / d_gridSize) - (where.Y + Container.Y));
			
			bool changed = (nsize != d_gridSize);
			d_gridSize = nsize;
			
			if (changed)
				ModifiedView(this, new EventArgs());
			
			QueueDraw();
		}
		
		private void DoZoom(bool zoomIn)
		{
			DoZoom(zoomIn, new Point((int)(Allocation.Width / 2), (int)(Allocation.Height / 2)));
		}
		
		private void DeleteSelected()
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
		}
		
		private void FocusRelease()
		{
			if (d_focus != null)
			{
				Components.Object o = d_focus;
				d_focus = null;
				
				o.Focus = false;
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
			obj.Focus = true;
		}
		
		private void DrawBackground(Graphics graphics)
		{
			graphics.FillRectangle(d_gridBackground, graphics.ClipBounds);
		}
		
		private void DrawGrid(Graphics graphics)
		{
			Point pt = UnitSize();
			
			GraphicsState state = graphics.Save();
			graphics.ScaleTransform(d_gridSize, d_gridSize);
			
			float ox = (Container.X / (float)d_gridSize) % 1;
			float oy = (Container.Y / (float)d_gridSize) % 1;
			
			Pen pen = new Pen(d_gridLine, 1 / (float)d_gridSize);
			
			for (int i = 1; i <= pt.X; ++i)
				graphics.DrawLine(pen, new PointF(i - ox, 0), new PointF(i - ox, pt.Y)); 
			
			for (int i = 1; i <= pt.Y; ++i)
				graphics.DrawLine(pen, new PointF(0, i - oy), new PointF(pt.X, i - oy));
			
			graphics.Restore(state);
		}
		
		private void DrawObject(Graphics graphics, Components.Object obj)
		{
			if (obj == null)
				return;
			
			Rectangle alloc = obj.Allocation;
			
			GraphicsState state = graphics.Save();
			
			graphics.TranslateTransform((float)alloc.X, (float)alloc.Y);
			obj.Draw(graphics);
			
			graphics.Restore(state);
		}
		
		private void DrawObjects(Graphics graphics)
		{
			GraphicsState state = graphics.Save();

			graphics.TranslateTransform(-Container.X, -Container.Y);
			graphics.ScaleTransform(d_gridSize, d_gridSize);

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

			graphics.Restore(state);
		}
		
		private void DrawSelectionRect(Graphics graphics)
		{
			if (d_mouseRect.Width <= 1 || d_mouseRect.Height <= 1)
				return;
			
			graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 0, 0, 255)), d_mouseRect);
			graphics.DrawRectangle(new Pen(Color.Blue, 2), d_mouseRect);
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
			
			List<Components.Object> objects = HitTest(new Rectangle((int)evnt.X, (int)evnt.Y, 1, 1));
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
					foreach (Components.Object obj in d_selection)
						Unselect(obj);
				}
			}
			
			if (evnt.Button != 2 && first != null)
			{
				Select(first);
				
				if (evnt.Button == 1)
				{
					Point res = ScaledPosition((int)evnt.X, (int)evnt.Y);
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
				d_mouseRect = new Rectangle((int)evnt.X, (int)evnt.Y, 1, 1);
			}				
			
			return true;
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
		{
			base.OnButtonReleaseEvent(evnt);
			
			d_isDragging = false;
			
			if (evnt.Type == Gdk.EventType.ButtonRelease)
			{
				List<Components.Object> objects = HitTest(new Rectangle((int)evnt.X, (int)evnt.Y, 1, 1));
				Components.Object first = objects.Count > 0 ? objects[0] : null;
				
				if (first != null)
				{
					Point pos = ScaledPosition((int)evnt.X, (int)evnt.Y);
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
				DoMouseInOut(evnt);
				DoDragRect(evnt);
				
				return true;
			}
			
			Point position = ScaledPosition((int)evnt.X, (int)evnt.Y);
			Point size = UnitSize();
			
			int pxn = (int)Math.Floor((double)(Container.X / d_gridSize));
			int pyn = (int)Math.Floor((double)(Container.Y / d_gridSize));
			
			List<int> maxx = new List<int>();
			List<int> minx = new List<int>();
			List<int> maxy = new List<int>();
			List<int> miny = new List<int>();
			List<KeyValuePair<Components.Object, Point>> translation = new List<KeyValuePair<Components.Object, Point>>(); 
			
			maxx.Add(size.X - 1 + pxn);
			minx.Add(position.X);
			maxy.Add(size.Y - 1 + pyn);
			miny.Add(position.Y);
			
			/* Check boundaries */
			foreach (Components.Object obj in d_selection)
			{
				Point pt = ScaledFromDragState(obj);
				Rectangle alloc = obj.Allocation;
				
				minx.Add(-pt.X + pxn);
				maxx.Add(size.X - pt.X + pxn - alloc.Width);
				miny.Add(-pt.Y + pyn);
				maxy.Add(size.Y - pt.Y + pyn - alloc.Height);
				
				translation.Add(new KeyValuePair<Components.Object, Point>(obj, pt));
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
			
			foreach (KeyValuePair<Components.Object, Point> item in translation)
			{
				Rectangle alloc = item.Key.Allocation;
				
				if (alloc.X != item.Value.X + position.X || alloc.Y != item.Value.Y + position.Y)
				{
					alloc.Offset(item.Value.X + position.X, item.Value.Y + position.Y);
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
			base.OnLeaveNotifyEvent(evnt);
			
			foreach (Components.Object obj in d_hover)
				obj.MouseExit();
		
			d_hover.Clear();
			return true;
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
			else if (evnt.Key == Gdk.Key.plus || evnt.Key == Gdk.Key.KP_Add)
			{
				DoZoom(true);
			}
			else if (evnt.Key == Gdk.Key.minus || evnt.Key == Gdk.Key.KP_Subtract)
			{
				DoZoom(false);
			}
			else if (evnt.Key == Gdk.Key.Tab || evnt.Key == Gdk.Key.ISO_Left_Tab)
			{
				FocusNext((evnt.State & Gdk.ModifierType.ShiftMask) != 0 ? -1 : 1);
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
			
			LevelUp(source, obj);
		}
		
		private void OnSelectionChanged(object source, EventArgs args)
		{
			FocusRelease();
		}
		
		protected override bool OnFocusOutEvent(Gdk.EventFocus evnt)
		{
			foreach (Components.Object obj in d_hover)
				obj.MouseExit();
			
			d_hover.Clear();
			FocusRelease();
			
			return base.OnFocusOutEvent(evnt);
		}
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			Graphics graphics = Gtk.DotNet.Graphics.FromDrawable(GdkWindow);
			
			graphics.Clip = new Region(new Rectangle(evnt.Area.X, evnt.Area.Y, evnt.Area.Width, evnt.Area.Height));
			
			DrawBackground(graphics);
			DrawGrid(graphics);

			DrawObjects(graphics);
			DrawSelectionRect(graphics);
			
			
			return true;
		}
	}
}