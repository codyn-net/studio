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
		
		public event ObjectEventHandler Activated;
		public event ObjectEventHandler ObjectAdded;
		public event ObjectEventHandler ObjectRemoved;
		public event ObjectEventHandler LevelUp;
		public event ObjectEventHandler LevelDown;
		public event EventHandler SelectionChanged;
		public event EventHandler Modified;
		public event EventHandler ModifiedView;
		
		private int d_maxSize;
		private int d_minSize;
		private int d_defaultGridSize;
		private int d_gridSize;
		
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
		}
		
		private Components.Group Container
		{
			get
			{
				return d_objectStack.Peek().Group;
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
		
		public void Add(Components.Object obj, int x, int y, int width, int height)
		{
			if (obj.Grid != null)
				obj.Grid.Remove(obj);

			obj.Grid = this;
			obj.Allocation.Assign(x, y, width, height);
			
			EnsureUniqueId(obj, obj["id"]);
			
			Container.Add(obj);
			ObjectAdded(this, obj);
			
			Modified(this, new EventArgs());
		}
		
		public void Remove(Components.Object obj)
		{
			if (!Container.Children.Remove(obj))
				return;
				
			ObjectRemoved(this, obj);
			obj.Grid = null;

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
		
		/* Callbacks */
		private void OnRequestRedraw(object source, EventArgs e)
		{
			QueueDrawObject(source as Components.Object);
		}
	}
}
