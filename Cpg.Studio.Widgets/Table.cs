using System;
using System.Collections.Generic;
using Cpg.Studio.Dialogs;
using Biorob.Math;

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
		private Plotting.Graph d_dragging;
		private Plotting.Graph d_unmerged;
		private Plot.Renderers.Renderer d_dragRenderer;
		private bool d_dragmerge;
		private Gtk.Widget d_dragHighlight;

		private int d_dragRow;
		private int d_dragColumn;

		private int d_rows;
		private int d_columns;
		private int d_rowSpacing;
		private int d_columnSpacing;
		
		private int[] d_x;
		private int[] d_y;
		private int[] d_width;
		private int[] d_height;
		
		private Point d_lastPress;
		
		private Gdk.Window d_window;

		private Plotting.Graph[,] d_children;
		
		public delegate Plotting.Graph CreateGraphHandler();
		
		public event CreateGraphHandler CreateGraph = delegate { return null; };
		
		public Table()
		{
			d_expand = ExpandType.Down;
			d_children = new Plotting.Graph[,] {};
			d_rows = 0;
			d_columns = 0;
			
			AddEvents((int)Gdk.EventMask.ExposureMask);
			
			Gtk.Drag.DestSet(this, 0, new Gtk.TargetEntry[] {new Gtk.TargetEntry("TableItem", Gtk.TargetFlags.App, 1)}, Gdk.DragAction.Move);
		}
		
		[System.Runtime.InteropServices.DllImport("libgtk-x11-2.0")]
		private static extern void gtk_widget_set_realized(IntPtr widget, bool realized);
		
		protected override void OnRealized()
		{
			Gdk.WindowAttr attr;
			
			attr = new Gdk.WindowAttr();
			attr.X = Allocation.X;
			attr.Y = Allocation.Y;
			attr.Width = Allocation.Width;
			attr.Height = Allocation.Height;
			
			attr.EventMask = (int)Events;
			attr.WindowType = Gdk.WindowType.Child;
			attr.Wclass = Gdk.WindowClass.InputOutput;
			attr.Visual = Visual;
			
			d_window = new Gdk.Window(ParentWindow,
			                          attr,
			                          Gdk.WindowAttributesType.X |
			                          Gdk.WindowAttributesType.Y |
			                          Gdk.WindowAttributesType.Visual);
			
			d_window.Background = Style.Background(Gtk.StateType.Normal);
			d_window.UserData = Handle;
			
			GdkWindow = d_window;
			
			gtk_widget_set_realized(Handle, true);
		}
		
		protected override void OnUnrealized()
		{
			if (d_window != null)
			{
				d_window.UserData = IntPtr.Zero;
				d_window.Destroy();
				d_window = null;
			}
			
			gtk_widget_set_realized(Handle, false);
		}
		
		protected override void OnMapped()
		{
			d_window.Show();
			base.OnMapped();
		}
		
		protected override void OnUnmapped()
		{
			d_window.Hide();

			base.OnUnmapped();
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
		
		public new void Add(Plotting.Graph widget)
		{
			base.Add(widget);
		}
		
		public new void Add(Plotting.Graph widget, int row, int col)
		{
			if (row < 0 || col < 0)
			{
				EmptyCell(out row, out col, true);
			}
			
			Resize(row + 1, col + 1);
			
			if (d_children[row, col] != null)
			{
				return;
			}
			
			d_children[row, col] = widget;
			widget.Parent = this;

			ReallocateOne(row, col);
			ConnectChild(widget);
		}

		public override GLib.GType ChildType()
		{
			return Plotting.Graph.GType;
		}
		
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);
			
			if (IsRealized)
			{
				d_window.MoveResize(allocation);
			}
			
			Reallocate();
		}
		
		private void Calculate(int num, int totalSize, int spacing, out int[] pos, out int[] size)
		{
			pos = new int[num];
			size = new int[num];
			
			int p = 0;
			
			for (int i = 0; i < num; ++i)
			{
				pos[i] = p;

				int left = num - i;
				int space = totalSize - pos[i] - (left - 1) * spacing;
				
				size[i] = space / left;
				
				p += size[i] + spacing;
			}
		}
		
		private void CalculateRows(out int[] y, out int[] height)
		{
			Calculate(d_rows, Allocation.Height, d_rowSpacing, out y, out height);
		}
		
		private void CalculateColumns(out int[] x, out int[] width)
		{
			Calculate(d_columns, Allocation.Width, d_columnSpacing, out x, out width);
		}
		
		private void ReallocateOne(int r, int c)
		{
			if (d_children[r, c] == null)
			{
				return;
			}

			Gdk.Rectangle alloc = new Gdk.Rectangle(d_x[c], d_y[r], d_width[c], d_height[r]);
			d_children[r, c].SizeAllocate(alloc);
			
			QueueDraw();
		}
		
		private void Reallocate()
		{
			CalculateRows(out d_y, out d_height);
			CalculateColumns(out d_x, out d_width);
			
			ForeachCell(false, (r, c, child) => {
				Gdk.Rectangle alloc = new Gdk.Rectangle(d_x[c], d_y[r], d_width[c], d_height[r]);

				child.SizeAllocate(alloc);
				return true;
			});
			
			QueueDraw();
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
		
		private bool CellAtPixel(int val, out int idx, int[] pos, int[] size)
		{
			idx = 0;
			
			for (int i = 0; i < pos.Length; ++i)
			{
				int p = pos[i];

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
			return CellAtPixel(y, out r, d_y, d_height) &&
			       CellAtPixel(x, out c, d_x, d_width);
		}
		
		private void Swap(int r, int c, int or, int oc)
		{
			Plotting.Graph tmp = d_children[r, c];

			d_children[r, c] = d_children[or, oc];
			d_children[or, oc] = tmp;
			
			ReallocateOne(r, c);
			ReallocateOne(or, oc);
		}
		
		private void DimsFrom(ref int num, int d, out int r, out int c)
		{
			num += d;

			r = d_rows;
			c = d_columns;
			
			num -= d;
		}
		
		private bool ExpandFromDrag(int pix, int[] pos, int[] size, ref int num, bool isempty)
		{
			if (!isempty)
			{
				if (pix - pos[num - 1] > 0.8 * size[num - 1])
				{
					int r;
					int c;

					DimsFrom(ref num, 1, out r, out c);
					Resize(r, c);

					return true;
				}
			}
			else if (pix - pos[num - 1] < 0.2 * size[num - 1])
			{
				int r;
				int c;
				
				DimsFrom(ref num, -1, out r, out c);
				
				Resize(r, c);
				return true;
			}
			
			return false;
		}
		
		private bool ExpandFromDrag(int x, int y)
		{
			bool ret = false;

			if (ExpandFromDrag(y, d_y, d_height, ref d_rows, EmptyRow(d_rows - 1)))
			{
				ret = true;
			}
			
			if (ExpandFromDrag(x, d_x, d_width, ref d_columns, EmptyColumn(d_columns - 1)))
			{
				ret = true;
			}
			
			return ret;
		}
		
		private void Shift(int rf, int cf, int rt, int ct, int rd, int cd)
		{
			// Asume rf, rt is the original, shift everything until reaching cf ct
			while (rf != rt || cf != ct)
			{
				d_children[rf, cf] = d_children[rf + rd, cf + cd];
				d_children[rf + rd, cf + cd] = null;
				
				ReallocateOne(rf, cf);

				rf += rd;
				cf += cd;
			}
		}
		
		private bool DoUpdateDragging(int x, int y)
		{
			// We have the child in d_dragging which was dragged from d_dragPos. We need to
			// determine the child position under the cursor at x, y and swap accordingly
			int r;
			int c;
			
			if (!CellAtPixel(x, y, out r, out c))
			{
				return false;
			}
			
			if (ExpandFromDrag(x, y))
			{
				CellAtPixel(x, y, out r, out c);
			}
			
			// Do nothing when what we drag is already in this cell
			Plotting.Graph dragging = d_unmerged != null ? d_unmerged : d_dragging;

			if (d_children[r, c] == dragging)
			{
				return true;
			}
			
			bool colprio = System.Math.Abs(d_dragColumn - c) > System.Math.Abs(d_dragRow - r);
			
			if (d_unmerged == null && d_dragRenderer != null)
			{
				// Here we are going to unmerge the renderer in a new graph in fact
				Plotting.Graph ng = CreateGraph();
				
				ng.Parent = this;
				
				d_dragging.Canvas.Graph.Remove(d_dragRenderer);
				ng.Canvas.Graph.Add(d_dragRenderer);
				
				d_unmerged = ng;
				dragging = d_unmerged;

				ConnectChild(ng);
				
				if (d_children[r, c] != null)
				{
					// Make some space
					if (colprio)
					{
						// Add column
						Resize(d_rows, d_columns + 1);
						
						// Move right
						Shift(r, d_columns - 1, r, c, 0, -1);
					}
					else
					{
						// Add row
						Resize(d_rows, d_columns + 1);
						
						// Move down
						Shift(d_rows - 1, c, r, c, -1, 0);
					}
				}
			}
			else
			{			
				if (colprio)
				{
					// First shift left, then up
					Shift(d_dragRow, d_dragColumn, d_dragRow, c, 0, System.Math.Sign(c - d_dragColumn));
					Shift(d_dragRow, d_dragColumn, r, d_dragColumn, System.Math.Sign(r - d_dragRow), 0);
				}
				else
				{
					// First shift up, then left
					Shift(d_dragRow, d_dragColumn, r, d_dragColumn, System.Math.Sign(r - d_dragRow), 0);
					Shift(d_dragRow, d_dragColumn, d_dragRow, c, 0, System.Math.Sign(c - d_dragColumn));
				}
			}
			
			d_dragRow = r;
			d_dragColumn = c;

			d_children[r, c] = dragging;
			ReallocateOne(r, c);
			
			return true;
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
			int numr = ret.GetUpperBound(0) + 1;
			int numc = ret.GetUpperBound(1) + 1;

			ForeachCell((r, c, child) => {
				if (r < numr && c < numc)
				{
					ret[r, c] = child;
				}

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
		
		public void Resize(int rows, int columns)
		{
			int nonempty = d_rows;

			while (rows < nonempty && nonempty > 0)
			{
				if (!EmptyRow(nonempty - 1))
				{	
					rows = nonempty;
					break;
				}
				
				--nonempty;
			}
			
			nonempty = d_columns;

			while (columns < nonempty && nonempty > 0)
			{
				if (!EmptyColumn(nonempty - 1))
				{	
					columns = nonempty;
					break;
				}
				
				--nonempty;
			}
			
			if (rows != d_rows || columns != d_columns)
			{
				Plotting.Graph[,] nc = new Plotting.Graph[rows, columns];
				
				CopyChildren(nc);
				d_children = nc;
				
				d_rows = rows;
				d_columns = columns;
				
				Reallocate();
			}
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
				Resize(d_rows + 1, d_columns);
				
				r = d_rows - 1;
				c = 0;
			}
			else
			{
				Resize(d_rows, d_columns + 1);
				
				r = d_rows - 1;
				c = d_columns - 1;
			}
			
			return true;
		}
		
		private void ConnectChild(Plotting.Graph graph)
		{
			Gtk.Drag.SourceSet(graph,
			                   Gdk.ModifierType.Button1Mask | Gdk.ModifierType.ControlMask,
			                   new Gtk.TargetEntry[] {new Gtk.TargetEntry("Cpg.Studio.TableItem", Gtk.TargetFlags.App, 1),
			                                          new Gtk.TargetEntry("Plot.Renderer", Gtk.TargetFlags.App, 2)},
			                   Gdk.DragAction.Move);
			
			graph.DragBegin += delegate (object source, Gtk.DragBeginArgs args) { DoDragBegin(graph, args.Context); };
			graph.DragEnd += delegate (object source, Gtk.DragEndArgs args) { DoDragEnd(graph, args.Context); };
			graph.ButtonPressEvent += delegate (object source, Gtk.ButtonPressEventArgs args) {
				d_lastPress = new Point(args.Event.X, args.Event.Y);
			};
		}
		
		protected override void OnAdded(Gtk.Widget widget)
		{
			int r;
			int c;

			EmptyCell(out r, out c, true);
			d_children[r, c] = (Plotting.Graph)widget;

			widget.Parent = this;
			ReallocateOne(r, c);

			ConnectChild((Plotting.Graph)widget);
		}
		
		public static Gdk.Atom DragTarget
		{
			get
			{
				return Gdk.Atom.Intern("Cpg.Studio.TableItem", false);
			}
		}
		
		private Gdk.Pixbuf PixbufForRenderer(Plot.Graph graph, Plot.Renderers.Renderer renderer)
		{
			Plot.Renderers.ILabeled lbl = renderer as Plot.Renderers.ILabeled;
			
			if (lbl == null)
			{
				return null;
			}
			
			Plot.Renderers.IColored col = renderer as Plot.Renderers.IColored;
			
			string s;
			
			if (lbl.LabelMarkup != null)
			{
				s = lbl.LabelMarkup;
			}
			else
			{
				s = System.Security.SecurityElement.Escape(lbl.Label);
			}
			
			if (col != null)
			{
				string hex = String.Format("#{0:x2}{1:x2}{2:x2}", 
			                               (int)(col.Color.R * 255),
			                               (int)(col.Color.G * 255),
			                               (int)(col.Color.B * 255));
				
				s = String.Format("<span color=\"{0}\">{1}</span>", hex, s);
			}
			
			Pango.Layout layout = CreatePangoLayout("");
			layout.SetMarkup(s);
			
			if (graph.Font != null)
			{
				layout.FontDescription = graph.Font;
			}
			
			int w;
			int h;
			
			layout.GetPixelSize(out w, out h);

			int xpadding = 4;
			int ypadding = 3;
			
			w += 2 * xpadding;
			h += 2 * ypadding;
			
			Gdk.Pixmap map = new Gdk.Pixmap(GdkWindow, w, h);
			
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(map))
			{
				ctx.Rectangle(0.5, 0.5, w - 1, h - 1);
				ctx.LineWidth = 1;
				
				ctx.SetSourceRGB(1, 1, 1);
				ctx.FillPreserve();

				graph.AxisLabelColors.Bg.Set(ctx);
				ctx.FillPreserve();
				
				graph.AxisLabelColors.Fg.Set(ctx);
				ctx.Stroke();
				
				ctx.Translate(xpadding + 1, ypadding);

				Pango.CairoHelper.ShowLayout(ctx, layout);
			}
			
			return Gdk.Pixbuf.FromDrawable(map, map.Colormap, 0, 0, 0, 0, w, h);
		}
		
		private void DoDragBegin(Gtk.Widget child, Gdk.DragContext context)
		{
			Gdk.Pixbuf icon;
			
			Plotting.Graph graph = (Plotting.Graph)child;
			
			d_dragRenderer = null;
			d_unmerged = null;
			d_dragHighlight = null;
			
			// Check if this is going to drag a label
			if (graph.Canvas.Graph.LabelHitTest(new Point(d_lastPress), out d_dragRenderer))
			{
				icon = PixbufForRenderer(graph.Canvas.Graph, d_dragRenderer);
			}
			else
			{
				icon = graph.CreateDragIcon();
			}
			
			if (icon != null)
			{					
				Gtk.Drag.SetIconPixbuf(context, icon, icon.Width / 2, icon.Height / 2);
			}
			
			d_dragging = graph;
			
			IndexOf(child, out d_dragRow, out d_dragColumn);

			Gdk.EventMotion evnt = Utils.GetCurrentEvent() as Gdk.EventMotion;

			d_dragmerge = (evnt != null && (evnt.State & Gdk.ModifierType.ShiftMask) != 0);
			
			IndexOf(child, out d_dragRow, out d_dragColumn);
			
			QueueDraw();
		}
		
		private void DoDragEnd(Gtk.Widget child, Gdk.DragContext context)
		{
			d_dragging = null;
			QueueDraw();
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
			
			Plotting.Graph[,] children = new Plotting.Graph[rows.Count, cols.Count];
			
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
		
		private void UnsetDragHighlight()
		{
			if (d_dragHighlight != null)
			{
				Gtk.Drag.Unhighlight(d_dragHighlight);
				d_dragHighlight = null;
			}
		}

		protected override bool OnDragMotion(Gdk.DragContext context, int x, int y, uint time_)
		{
			base.OnDragMotion(context, x, y, time_);
			
			if (d_dragging == null)
			{
				Gdk.Drag.Status(context, 0, time_);
				UnsetDragHighlight();

				return false;
			}
			else if (d_dragmerge)
			{
				int r;
				int c;

				if (CellAtPixel(x, y, out r, out c) && d_children[r, c] != null)
				{
					if (d_dragHighlight != d_children[r, c])
					{
						UnsetDragHighlight();
					}

					if (d_dragging != d_children[r, c])
					{
						d_dragHighlight = d_children[r, c];
						Gtk.Drag.Highlight(d_dragHighlight);
					}

					Gdk.Drag.Status(context, Gdk.DragAction.Move, time_);
				}
				else
				{
					UnsetDragHighlight();
					Gdk.Drag.Status(context, 0, time_);
				}

				return true;
			}
			else
			{
				if (DoUpdateDragging(x, y))
				{
					UnsetDragHighlight();
					Gdk.Drag.Status(context, Gdk.DragAction.Move, time_);
				}
				else
				{
					if (d_dragHighlight != null)
					{
						Gtk.Drag.Unhighlight(d_dragHighlight);
						d_dragHighlight = null;
					}

					Gdk.Drag.Status(context, 0, time_);
				}
				return true;
			}
		}
		
		protected override bool OnDragDrop(Gdk.DragContext context, int x, int y, uint time_)
		{
			UnsetDragHighlight();

			if (d_dragging == null)
			{
				return false;
			}
			
			if (d_dragmerge)
			{
				int r;
				int c;

				if (CellAtPixel(x, y, out r, out c) && d_children[r, c] != null)
				{
					if (d_children[r, c] != d_dragging)
					{
						/* Merge what we drag onto the graph where we drop */
						if (d_dragRenderer != null)
						{
							d_dragging.Canvas.Graph.Remove(d_dragRenderer);
							d_children[r, c].Canvas.Graph.Add(d_dragRenderer);
						}
						else
						{
							List<Plot.Renderers.Renderer> rr = new List<Plot.Renderers.Renderer>(d_dragging.Canvas.Graph.Renderers);

							foreach (Plot.Renderers.Renderer renderer in rr)
							{
								d_dragging.Canvas.Graph.Remove(renderer);
								d_children[r, c].Canvas.Graph.Add(renderer);
							}
						}
						
						if (d_dragging.Canvas.Graph.Count == 0)
						{
							d_dragging.Destroy();
						}
					}

					Gtk.Drag.Finish(context, true, false, time_);
				}
				else
				{
					Gtk.Drag.Finish(context, false, false, time_);
				}
			}
			else
			{			
				Gtk.Drag.Finish(context, true, false, time_);
			}

			return true;
		}
	}
}
