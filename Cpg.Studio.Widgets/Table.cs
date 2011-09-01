using System;
using System.Collections.Generic;
using System.Drawing;

namespace Cpg.Studio.Widgets
{
	public class Table : Gtk.Container
	{
		public enum ExpandType
		{
			Right,
			Down
		}
		
		private ExpandType d_expand;
		private Gtk.Widget d_dragging;
		private Gtk.Widget d_swapped;
		
		private int d_dragRow;
		private int d_dragColumn;

		private int d_swapRow;
		private int d_swapColumn;

		private int d_rows;
		private int d_columns;
		private int d_rowSpacing;
		private int d_columnSpacing;
		
		private int[] d_x;
		private int[] d_y;
		private int[] d_width;
		private int[] d_height;

		private Gtk.Widget[,] d_children;
		
		public Table()
		{
			d_expand = ExpandType.Down;
			d_children = new Gtk.Widget[,] {};
			d_rows = 0;
			d_columns = 0;
			
			WidgetFlags |= Gtk.WidgetFlags.NoWindow;
			
			Gtk.Drag.DestSet(this, 0, new Gtk.TargetEntry[] {new Gtk.TargetEntry("TableItem", Gtk.TargetFlags.App, 1)}, Gdk.DragAction.Move);
		}
		
		public int Columns
		{
			get
			{
				return d_columns;
			}
		}
		
		public int Rows
		{
			get
			{
				return d_rows;
			}
		}
		
		public int ColumnSpacing
		{
			get
			{
				return d_columnSpacing;
			}
			set
			{
				d_columnSpacing = value;
				Reallocate();
			}
		}
		
		public int RowSpacing
		{
			get
			{
				return d_rowSpacing;
			}
			set
			{
				d_rowSpacing = value;
				Reallocate();
			}
		}
		
		public new void Add(Gtk.Widget widget)
		{
			base.Add(widget);
		}
		
		public new void Add(Gtk.Widget widget, int row, int col)
		{
			if (row < 0 || col < 0)
			{
				EmptyCell(out row, out col, true);
			}
			
			Resize(row + 1, col + 1, false);
			
			if (d_children[row, col] != null)
			{
				return;
			}
			
			d_children[row, col] = widget;
			widget.Parent = this;

			ReallocateOne(row, col);
			SetDragSource(widget);
		}

		public override GLib.GType ChildType()
		{
			return Gtk.Widget.GType;
		}
		
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);
			
			Reallocate();
		}
		
		private void Calculate(int num, int totalSize, int spacing, int stopat, out int[] pos, out int[] size)
		{
			pos = new int[d_rows];
			size = new int[d_rows];
			
			int p = 0;
			
			for (int i = 0; i < num; ++i)
			{
				pos[i] = p;

				int left = num - i;
				int space = totalSize - pos[i] - (left - 1) * spacing;
				
				size[i] = space / left;
				
				if (i == stopat)
				{
					break;
				}
				
				p += size[i] + spacing;
			}
		}
		
		private void CalculateRows(out int[] y, out int[] height, int stopat)
		{
			Calculate(d_rows, Allocation.Height, d_rowSpacing, stopat, out y, out height);
		}
		
		private void CalculateColumns(out int[] x, out int[] width, int stopat)
		{
			Calculate(d_columns, Allocation.Width, d_columnSpacing, stopat, out x, out width);
		}
		
		private void ReallocateOne(int r, int c)
		{
			if (d_children[r, c] == null)
			{
				return;
			}

			Gdk.Rectangle alloc = new Gdk.Rectangle(d_x[c] + Allocation.X, d_y[r] + Allocation.Y, d_width[c], d_height[r]);
			d_children[r, c].SizeAllocate(alloc);
		}
		
		private void Reallocate()
		{
			CalculateRows(out d_y, out d_height, d_rows);
			CalculateColumns(out d_x, out d_width, d_columns);
			
			ForeachCell(false, (r, c, child) => {
				Gdk.Rectangle alloc = new Gdk.Rectangle(d_x[c] + Allocation.X, d_y[r] + Allocation.Y, d_width[c], d_height[r]);

				child.SizeAllocate(alloc);
				return true;
			});
		}
		
		protected override void ForAll(bool include_internals, Gtk.Callback callback)
		{
			ForeachCell(false, (r, c, child) => {
				callback(child);
				return true;
			});
		}

		public ExpandType Expand
		{
			get
			{
				return d_expand;
			}
			set
			{
				d_expand = value;
			}
		}
		
		private bool CellAtPixel(int val, out int idx, int offset, int[] pos, int[] size)
		{
			idx = 0;
			
			for (int i = 0; i < pos.Length; ++i)
			{
				int p = offset + pos[i];

				if (val >= p && val <= p + size[i])
				{
					idx = i;
					return true;
				}
			}
			
			return false;
		}
		
		private bool CellAtPixel(int x, int y, out int r, out int c)
		{
			return CellAtPixel(y, out r, Allocation.Y, d_y, d_height) &&
			       CellAtPixel(x, out c, Allocation.X, d_x, d_width);
		}
		
		private void Swap(int r, int c, int or, int oc)
		{
			Gtk.Widget tmp = d_children[r, c];

			d_children[r, c] = d_children[or, oc];
			d_children[or, oc] = tmp;
			
			ReallocateOne(r, c);
			ReallocateOne(or, oc);
		}
		
		private void DoUpdateDragging(int x, int y)
		{
			// We have the child in d_dragging which was dragged from d_dragPos. We need to
			// determine the child position under the cursor at x, y and swap accordingly
			int r;
			int c;
			
			if (!CellAtPixel(x, y, out r, out c))
			{
				return;
			}

			if (d_swapped != null)
			{
				// Swap back
				Swap(d_dragRow, d_dragColumn, d_swapRow, d_swapColumn);
			}
			
			d_swapped = null;
			
			if (d_children[r, c] == d_dragging)
			{
				return;
			}
			
			d_swapRow = r;
			d_swapColumn = c;
			
			d_swapped = d_children[d_swapRow, d_swapColumn];
			
			Swap(d_dragRow, d_dragColumn, d_swapRow, d_swapColumn);
		}
		
		public delegate bool ForeachCellHandler(int r, int c, Gtk.Widget widget);
		
		public bool ForeachCell(ForeachCellHandler handler)
		{
			return ForeachCell(true, handler);
		}
		
		public bool ForeachCell(bool includeempty, ForeachCellHandler handler)
		{
			for (int r = 0; r < d_rows; ++r)
			{
				for (int c = 0; c < d_columns; ++c)
				{
					Gtk.Widget child = d_children[r, c];
					
					if (includeempty || child != null)
					{
						if (!handler(r, c, child))
						{
							return false;
						}
					}
				}
			}
			
			return true;
		}
		
		private void CopyChildren(Gtk.Widget[,] ret)
		{
			ForeachCell((r, c, child) => {
				ret[r, c] = child;
				return true;
			});
		}
		
		public Gtk.Widget Find(Gtk.Widget source, int dr, int dc)
		{
			int r;
			int c;

			if (!IndexOf(source, out r, out c))
			{
				return null;
			}
			
			while (true)
			{
				r += dr;
				c += dc;
				
				if (r >= 0 && c >= 0 && r < d_rows && c < d_columns)
				{
					return null;
				}
				
				if (d_children[r, c] != null)
				{
					return d_children[r, c];
				}
			}
		}
		
		public Gtk.Widget this[int r, int c]
		{
			get
			{
				if (r >= d_rows || c >= d_columns || r < 0 || c < 0)
				{
					return null;
				}

				return d_children[r, c];
			}		
		}
		
		private void Resize(int rows, int columns, bool compact)
		{
			if (compact)
			{
				Compact();
			}

			rows = System.Math.Max(rows, d_rows);
			columns = System.Math.Max(columns, d_columns);
			
			if (rows != d_rows || columns != d_columns)
			{
				Gtk.Widget[,] nc = new Gtk.Widget[rows, columns];
				
				CopyChildren(nc);
				d_children = nc;
				
				d_rows = rows;
				d_columns = columns;
				
				Reallocate();
			}
		}
		
		public void Resize(int rows, int columns)
		{
			Resize(rows, columns, false);
		}
		
		private bool EmptyCell(out int r, out int c, bool resize)
		{
			int or = 0;
			int oc = 0;
			bool ret;
			
			ret = !ForeachCell((rr, cc, child) => {
				if (child == null)
				{
					or = rr;
					oc = cc;
					return false;
				}
				
				return true;
			});
			
			r = or;
			c = oc;
			
			if (ret)
			{
				return true;
			}
			else if (!resize)
			{
				return false;
			}
			
			if (d_expand == ExpandType.Down)
			{
				Resize(d_rows + 1, d_columns, false);
				
				r = d_rows - 1;
				c = 0;
			}
			else
			{
				Resize(d_rows, d_columns + 1, false);
				
				r = d_rows - 1;
				c = d_columns - 1;
			}
			
			return true;
		}
		
		protected override void OnAdded(Gtk.Widget widget)
		{
			int r;
			int c;

			EmptyCell(out r, out c, true);
			d_children[r, c] = widget;

			widget.Parent = this;
			ReallocateOne(r, c);
			
			SetDragSource(widget);
		}
		
		private void SetDragSource(Gtk.Widget child)
		{
			Gtk.Drag.SourceSet(child, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.ControlMask, new Gtk.TargetEntry[] {new Gtk.TargetEntry("TableItem", Gtk.TargetFlags.App, 1)}, Gdk.DragAction.Move);
			
			child.DragBegin += delegate (object source, Gtk.DragBeginArgs args) { DoDragBegin(child, args.Context); };
			child.DragEnd += delegate (object source, Gtk.DragEndArgs args) { DoDragEnd(child, args.Context); };
		}
		
		private void DoDragBegin(Gtk.Widget child, Gdk.DragContext context)
		{
			Gdk.Rectangle alloc = child.Allocation;
			Gdk.Pixbuf icon;
			
			if (child is IDragIcon)
			{
				icon = ((IDragIcon)child).CreateDragIcon();
			}
			else
			{
				icon = Gdk.Pixbuf.FromDrawable(child.GdkWindow, child.GdkWindow.Colormap, alloc.X, alloc.Y, 0, 0, alloc.Width, alloc.Height);
			}
					
			Gtk.Drag.SetIconPixbuf(context, icon, icon.Width / 2, icon.Height / 2);
			
			d_dragging = child;
			d_swapped = null;
			
			IndexOf(child, out d_dragRow, out d_dragColumn);
		}
		
		private void DoDragEnd(Gtk.Widget child, Gdk.DragContext context)
		{
			d_dragging = null;
		}
		
		public bool IndexOf(Gtk.Widget widget, out int r, out int c)
		{
			bool ret;
			int ro = 0;
			int co = 0;

			ret = !ForeachCell((rr, cc, child) => {
				if (child == widget)
				{
					ro = rr;
					co = cc;

					return false;
				}
				
				return true;
			});
			
			r = ro;
			c = co;
			
			return ret;
		}
		
		private bool IsEmpty(int r, int c, int dr, int dc)
		{
			while (r < d_rows && c < d_columns)
			{
				if (d_children[r, c] != null)
				{
					return false;
				}
				
				r += dr;
				c += dc;
			}
			
			return true;
		}
		
		private bool EmptyRow(int r)
		{
			return IsEmpty(r, 0, 0, 1);
		}
		
		private bool EmptyColumn(int c)
		{
			return IsEmpty(0, c, 1, 0);
		}
		
		private void Compact()
		{
			List<int> rows = new List<int>();

			/* Remove empty rows and columns */
			for (int r = 0; r < d_rows; ++r)
			{
				if (!EmptyRow(r))
				{
					rows.Add(r);
				}
			}
			
			List<int> cols = new List<int>();
			
			for (int c = 0; c < d_columns; ++c)
			{
				if (!EmptyColumn(c))
				{
					cols.Add(c);
				}
			}
			
			if (cols.Count == d_columns && rows.Count == d_rows)
			{
				return;
			}
			
			Gtk.Widget[,] children = new Gtk.Widget[rows.Count, cols.Count];
			
			for (int r = 0; r < rows.Count; ++r)
			{
				for (int c = 0; c < cols.Count; ++c)
				{
					children[r, c] = d_children[rows[r], cols[c]];
				}
			}
			
			d_children = children;
			
			d_rows = rows.Count;
			d_columns = cols.Count;
			
			Reallocate();
		}
		
		protected override void OnRemoved(Gtk.Widget widget)
		{
			int r;
			int c;

			if (IndexOf(widget, out r, out c))
			{
				widget.Unparent();
				d_children[r, c] = null;
				
				Compact();
			}
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
		
		protected override bool OnDragDrop(Gdk.DragContext context, int x, int y, uint time_)
		{
			if (d_dragging == null)
			{
				return false;
			}
			
			Gtk.Drag.Finish(context, true, false, time_);
			return true;
		}
	}
}
