using System;
using System.Collections.Generic;
using Gtk;

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
		private System.Drawing.Rectangle d_mouseRect;
		private System.Drawing.Point d_origPosition;
		private System.Drawing.Point d_buttonPress;
		
		private Components.Object d_focus;
		
		private Components.Network d_network;
		private List<Components.Object> d_hover;
		private Stack<StackItem> d_objectStack;
		private List<Components.Object> d_selection;
		
		private double[] d_gridBackground;
		private double[] d_gridLine;
		
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
			
			d_maxSize = 120;
			d_minSize = 20;
			d_defaultGridSize = 50;
			d_gridSize = d_defaultGridSize;
			d_focus = null;
			
			d_gridBackground = new double[] {1, 1, 1};
			d_gridLine = new double[] {0.9, 0.9, 0.9};
			
			d_objectStack = new Stack<StackItem>();
			d_hover = new List<Components.Object>();
			d_selection = new List<Components.Object>();
			
			d_network = new Components.Network();
			
			Clear();
		}
		
		public void Clear()
		{
			bool changed = d_objectStack.Count != 1 || d_objectStack.Peek().Group.Children.Count == 0;
			
			d_objectStack.Clear();
			d_objectStack.Push(new StackItem(new Components.Group(), d_gridSize));
			QueueDraw();
			
			SelectionChanged(this, new EventArgs());
			
			if (changed)
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
				return d_objectStack.Peek().Group;
			}
		}
		
		private System.Drawing.Point Position
		{
			get
			{
				return new System.Drawing.Point((int)Container.X, (int)Container.Y);
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
			
			if (obj is Components.Simulated)
				d_network.AddObject((obj as Components.Simulated).Object);
			
			if (obj is Components.Group)
			{
				foreach (Components.Object child in (obj as Components.Group).Children)
				{
					DoObjectAdded(child);
				}
			}
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
			ObjectAdded(this, obj);
			
			Modified(this, new EventArgs());
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
		
		private System.Drawing.Point ScaledPosition(System.Drawing.Point position, ScaledPredicate predicate)
		{
			return new System.Drawing.Point(Scaled(position.X, predicate), Scaled(position.Y, predicate));
		}
		
		private System.Drawing.Point ScaledPosition(System.Drawing.Point position)
		{
			return ScaledPosition(position, new ScaledPredicate(Math.Floor));
		}
		
		private System.Drawing.Point ScaledPosition(int x, int y)
		{
			return ScaledPosition(new System.Drawing.Point(x, y));
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
		
		private Components.Object[] HitTest(System.Drawing.Rectangle rect)
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
					if (rect.Contains(obj.Allocation) && obj.HitTest(rect))
						res.Add(obj);
				}
				else
				{
					if ((obj as Components.Link).HitTest(rect, d_gridSize))
						res.Add(obj);
				}
			}
			
			return res.ToArray();
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
		
		private void UpdateDragState(System.Drawing.Point position)
		{
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
			
			Components.Object[] objects = HitTest(new System.Drawing.Rectangle((int)evnt.X, (int)evnt.Y, 1, 1));
			Components.Object first = objects.Length > 0 ? objects[0] : null;

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
					System.Drawing.Point res = ScaledPosition((int)evnt.X, (int)evnt.Y);
					UpdateDragState(res);
				}
			}
			
			if (evnt.Button == 3)
			{
				Popup(this, (int)evnt.Button, evnt.Time);
			}
			else if (evnt.Button == 2)
			{
				d_buttonPress = new System.Drawing.Point((int)evnt.X, (int)evnt.Y);
				d_origPosition = Position;
			}
			else
			{
				d_mouseRect = new System.Drawing.Rectangle((int)evnt.X, (int)evnt.Y, 1, 1);
			}				
			
			return true;
		}

	}
}
