using System;
using System.Collections.Generic;
using System.Drawing;

namespace Cpg.Studio.GtkGui
{
	public class Table : Gtk.Table
	{
		class Placeholder : Gtk.DrawingArea
		{
			public Placeholder() : base()
			{
				Show();
			}
		}
		
		enum Expand
		{
			Right,
			Down
		}
		
		private Expand d_expand;
		private bool d_compacting;
		private Gtk.Widget d_dragging;
		private Gtk.Widget d_swapped;
		private Point d_dragPos;
		
		public Table(uint rows, uint cols, bool homogeneous) : base(rows, cols, homogeneous)
		{
			d_expand = Expand.Right;
			
			AddEvents((int)(Gdk.EventMask.KeyPressMask | Gdk.EventMask.ButtonPressMask));
			CanFocus = true;
			
			Gtk.Drag.DestSet(this, 0, new Gtk.TargetEntry[] {new Gtk.TargetEntry("TableItem", Gtk.TargetFlags.App, 1)}, Gdk.DragAction.Move);
		}
		
		public Table() : this(1, 1, false)
		{
		}
		
		private Gtk.Widget RealChild(Gtk.Widget child)
		{
			while (child != null && child.Parent != this)
				child = child.Parent;
			
			return child;
		}
		
		private void PositionChild(Gtk.Widget child, uint left, uint top)
		{
			child = RealChild(child);

			ChildSetProperty(child, "left-attach", new GLib.Value(left));
			ChildSetProperty(child, "right-attach", new GLib.Value(left + 1));
			ChildSetProperty(child, "top-attach", new GLib.Value(top));
			ChildSetProperty(child, "bottom-attach", new GLib.Value(top + 1));
		}
		
		private void MoveTo(Gtk.Widget from, Gtk.Widget to)
		{
			foreach (string attr in new string[] {"left", "right", "top", "bottom"})
				ChildSetProperty(from, attr + "-attach", ChildGetProperty(to, attr + "-attach"));
		}
		
		private void DoUpdateDragging(int x, int y)
		{
			/* We have the child in d_dragging which was dragged from d_dragPos. We need to
			 * determine the child position under the cursor at x, y and swap accordingly
			 */
			Gtk.Widget[,] mtx = new Gtk.Widget[NColumns, NRows];
			
			object[] columns = new object[NColumns];
			object[] rows = new object[NRows];
			
			foreach (Gtk.Widget child in Children)
			{
				Gdk.Rectangle alloc = child.Allocation;
				
				uint left = (uint)ChildGetProperty(child, "left-attach").Val;
				uint top = (uint)ChildGetProperty(child, "top-attach").Val;
				
				columns[left] = new Point(alloc.X, alloc.X + alloc.Width);
				rows[top] = new Point(alloc.Y, alloc.Y + alloc.Height);
				
				mtx[left, top] = child;
			}
			
			int col = -1;
			
			for (int i = 0; i < NColumns; ++i)
			{
				if (columns[i] == null)
					continue;
				
				Point pt = (Point)columns[i];
				
				if (pt.X < x && pt.Y > x)
				{
					col = i;
					break;
				}
			}
			
			int row = -1;
			
			for (int i = 0; i < NRows; ++i)
			{
				if (rows[i] == null)
					continue;
				
				Point pt = (Point)rows[i];
				
				if (pt.X < y && pt.Y > y)
				{
					row = i;
					break;
				}
			}
			
			if (row == -1 || col == -1)
				return;
			
			Gtk.Widget found = mtx[col, row];
			
			if (found == d_dragging)
				return;
			
			if (d_swapped != null)
				MoveTo(d_swapped, d_dragging);
			
			if (d_swapped != null && d_swapped == found)
			{
				found = d_dragging;
				d_swapped = null;
			}
			else
			{
				PositionChild(d_dragging, (uint)col, (uint)row);
				d_swapped = found;
			}
			
			if (found != null)
				PositionChild(found, (uint)d_dragPos.X, (uint)d_dragPos.Y);
		}
		
		private Point FindEmpty()
		{
			List<Point> all = new List<Point>();
			
			for (int x = 0; x < NColumns; ++x)
			{
				for (int y = 0; y < NRows; ++y)
					all.Add(new Point(x, y));
			}
			
			if (d_expand == Expand.Down)
			{
				all.Sort(delegate (Point first, Point second) {
					return first.Y == second.Y ? first.X.CompareTo(second.X) : first.Y.CompareTo(second.Y);
				});
			}
			
			foreach (Gtk.Widget child in Children)
			{
				if (child is Placeholder)
					continue;
			
				Point pos = ChildPosition(child);
				
				all.RemoveAll(delegate (Point pt) {
					return pt == pos;
				});
			}
			
			if (all.Count > 0)
				return all[0];
			
			/* Resize is imminent */
			if (d_expand == Expand.Right)
			{
				Resize(NRows, NColumns + 1);
				return new Point((int)NColumns - 1, 0);
			}
			else
			{
				Resize(NRows + 1, NColumns);
				return new Point(0, (int)NRows - 1);
			}
		}
		
		private Point ChildPosition(Gtk.Widget child)
		{
			child = RealChild(child);
			
			if (child == null)
				return new Point();
			
			int left = (int)ChildGetProperty(child, "left-attach").Val;
			int top = (int)ChildGetProperty(child, "top-attach").Val;
			
			return new Point(left, top);
		}
		
		public new void Add(Gtk.Widget child)
		{
			Point pos = FindEmpty();

			if (child.IsNoWindow)
			{
				Gtk.EventBox ev = new Gtk.EventBox();
				child.Show();
				
				child.Destroyed += delegate (object source, EventArgs args) { ev.Destroy(); };
				ev.Add(child);
				
				child = ev;
			}
			
			Attach(child, (uint)pos.X, (uint)pos.X + 1, (uint)pos.Y, (uint)pos.Y + 1, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, 0);
			
			SetDragSource(child);
			child.Show();
		}
		
		private void SetDragSource(Gtk.Widget child)
		{
			Gtk.Drag.SourceSet(child, Gdk.ModifierType.Button1Mask, new Gtk.TargetEntry[] {new Gtk.TargetEntry("TableItem", Gtk.TargetFlags.App, 1)}, Gdk.DragAction.Move);
			
			child.DragBegin += delegate (object source, Gtk.DragBeginArgs args) { DoDragBegin(child, args.Context); };
			child.DragEnd += delegate (object source, Gtk.DragEndArgs args) { DoDragEnd(child, args.Context); };
		}
		
		private void DoDragBegin(Gtk.Widget child, Gdk.DragContext context)
		{
			Gdk.Rectangle alloc = child.Allocation;
			Gdk.Pixbuf icon;
			
			System.Reflection.MethodInfo info = child.GetType().GetMethod("CreateDragIcon");
			
			if (info != null)
				icon = info.Invoke(child, new object[] {}) as Gdk.Pixbuf;
			else
				icon = Gdk.Pixbuf.FromDrawable(child.GdkWindow, child.GdkWindow.Colormap, 0, 0, 0, 0, alloc.Width, alloc.Height);
					
			Gtk.Drag.SetIconPixbuf(context, icon, icon.Width / 2, icon.Height / 2);
			
			d_dragging = child;
			d_swapped = null;
			
			d_dragPos = ChildPosition(child);
		}
		
		private bool EmptyRow(uint idx)
		{
			return (new List<Gtk.Widget>(Children)).Exists(delegate (Gtk.Widget child) {
				return !(child is Placeholder) && (uint)ChildGetProperty(child, "top-attach") == idx;
			});
		}
		
		private bool EmptyColumn(uint idx)
		{
			return (new List<Gtk.Widget>(Children)).Exists(delegate (Gtk.Widget child) {
				return !(child is Placeholder) && (uint)ChildGetProperty(child, "left-attach") == idx;
			});
		}
		
		private bool FindEmptyRow(ref uint idx)
		{
			if (NRows <= 1)
				return false;
			
			for (idx = 0; idx < NRows; ++idx)
			{
				if (EmptyRow(idx))
					return true;
			}
			
			return false;
		}
		
		private bool FindEmptyColumn(ref uint idx)
		{
			if (NColumns <= 1)
				return false;
			
			for (idx = 0; idx < NColumns; ++idx)
			{
				if (EmptyColumn(idx))
					return true;
			}
			
			return false;							
		}

		private void RemoveRow(uint idx)
		{
			foreach (Gtk.Widget child in Children)
			{
				Point pos = ChildPosition(child);
				
				if (pos.Y == idx && child is Placeholder)
				{
					child.Destroy();
				}
				else if (pos.Y >= idx)
				{
					ChildSetProperty(child, "top-attach", new GLib.Value(idx - 1));
					ChildSetProperty(child, "bottom-attach", new GLib.Value(idx));
				}
			}
			
			Resize(NRows - 1, NColumns);
			QueueDraw();
		}
		
		private void RemoveColumn(uint idx)
		{
			foreach (Gtk.Widget child in Children)
			{
				Point pos = ChildPosition(child);
				
				if (pos.X == idx && child is Placeholder)
				{
					child.Destroy();
				}
				else if (pos.X > idx)
				{
					ChildSetProperty(child, "left-attach", new GLib.Value(idx - 1));
					ChildSetProperty(child, "right-attach", new GLib.Value(idx));
				}
			}
			
			Resize(NRows, NColumns - 1);
			QueueDraw();
		}

		private void Compact()
		{
			d_compacting = true;
		
			uint idx = 0;
			while (FindEmptyRow(ref idx))
				RemoveRow(idx);
			
			while (FindEmptyColumn(ref idx))
				RemoveColumn(idx);
		
			d_compacting = false;
		}
		
		private void DoDragEnd(Gtk.Widget child, Gdk.DragContext context)
		{
			d_dragging = null;
			Compact();
		}
		
		protected override void OnRemoved(Gtk.Widget widget)
		{
			base.OnRemoved(widget);
			
			if (!d_compacting)
				Compact();
		}

		private void FindEmptyRowCol(uint last, string what, out Gtk.Widget child, out Placeholder placeholder)
		{
			placeholder = null;
			child = null;
			
			foreach (Gtk.Widget widget in Children)
			{
				if ((uint)ChildGetProperty(widget, what + "-attach").Val == last)
				{
					if (widget is Placeholder)
					{
						placeholder = widget as Placeholder;
					}
					else
					{
						child = widget;
						break;
					}
				}
			}
		}
		
		private bool RemoveEmptyRow()
		{
			uint last = NRows - 1;
			Gtk.Widget child;
			Placeholder placeholder;
			
			FindEmptyRowCol(last, "top", out child, out placeholder);
			
			if (child != null)
				return false;
		
			if (placeholder != null)
				placeholder.Destroy();
		
			RemoveRow(last);
			
			return true;
		}
		
		private bool RemoveEmptyColumn()
		{
			uint last = NColumns - 1;
			Gtk.Widget child;
			Placeholder placeholder;
			
			FindEmptyRowCol(last, "left", out child, out placeholder);
			
			if (child != null)
				return false;
		
			if (placeholder != null)
				placeholder.Destroy();
		
			RemoveColumn(last);
			
			return true;
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
		{
			if ((evnt.State & Gdk.ModifierType.ControlMask) == 0)
				return false;
			
			switch (evnt.Key)
			{
				case Gdk.Key.Down:
					return RemoveEmptyRow();
				case Gdk.Key.Up:
					Resize(NRows + 1, NColumns);
					Attach(new Placeholder(), 0, 1, NRows - 1, NRows);
					QueueDraw();
					
					return true;
				case Gdk.Key.Left:
					Resize(NRows, NColumns + 1);
					Attach(new Placeholder(), NColumns - 1, NColumns, 0, 1);
					QueueDraw();
					
					return true;
				case Gdk.Key.Right:
					return RemoveEmptyColumn();
			}
			
			return false;
		}
		
		protected override bool OnDragMotion(Gdk.DragContext context, int x, int y, uint time_)
		{
			base.OnDragMotion(context, x, y, time_);
			
			if (d_dragging == null)
			{
				Gdk.Drag.Status(context, 0, time_);
				return false;
			}
			else
			{
				DoUpdateDragging(x, y);
				Gdk.Drag.Status(context, Gdk.DragAction.Move, time_);
				return true;
			}
		}
		
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			GrabFocus();
			return false;
		}

		protected override bool OnDragDrop(Gdk.DragContext context, int x, int y, uint time_)
		{
			if (d_dragging == null)
				return false;
			
			Gtk.Drag.Finish(context, true, false, time_);
			return true;
		}
	}
}
