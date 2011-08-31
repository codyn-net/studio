using System;
using Gtk;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace Cpg.Studio.Dialogs
{
	[Binding(Gdk.Key.R, Gdk.ModifierType.ControlMask, "ToggleAutoAxis"),
	 Binding(Gdk.Key.L, Gdk.ModifierType.ControlMask, "ToggleLinkAxis")]
	public class Plotting : Gtk.Window
	{
		public class Series : IDisposable
		{
			private Cpg.Monitor d_x;
			private Cpg.Monitor d_y;

			private Plot.Renderers.Line d_renderer;
			
			public event EventHandler Destroyed = delegate {};
			private Dictionary<Wrappers.Group, Wrappers.Wrapper> d_ancestors;
			
			public Series(Cpg.Monitor x, Cpg.Monitor y, Plot.Renderers.Line renderer)
			{
				d_x = x;
				d_y = y;

				d_renderer = renderer;
				
				d_y.Property.AddNotification("name", OnNameChanged);
				d_y.Property.Object.AddNotification("id", OnNameChanged);
				
				d_y.Property.Object.PropertyRemoved += HandlePropertyRemoved;
				
				d_ancestors = new Dictionary<Wrappers.Group, Wrappers.Wrapper>();
				
				ConnectParentsRemoved(Wrappers.Wrapper.Wrap(d_y.Property.Object.Parent) as Wrappers.Group, d_y.Property.Object);
				
				if (d_x != null)
				{
					d_x.Property.Object.PropertyRemoved += HandlePropertyRemoved;
					
					ConnectParentsRemoved(Wrappers.Wrapper.Wrap(d_x.Property.Object.Parent) as Wrappers.Group, d_x.Property.Object);
				}
				
				UpdateName();
			}
			
			private void ConnectParentsRemoved(Wrappers.Group parent, Wrappers.Wrapper obj)
			{
				if (parent == null || obj == null)
				{
					return;
				}

				d_ancestors[parent] = obj;
				
				parent.ChildRemoved += OnChildRemoved;
				
				ConnectParentsRemoved(parent.Parent as Wrappers.Group, parent);
			}
			
			private bool DisconnectParentsRemoved(Wrappers.Group parent)
			{
				Wrappers.Wrapper wrapper;

				if (parent == null)
				{
					return false;
				}
				
				if (!d_ancestors.TryGetValue(parent, out wrapper))
				{
					return DisconnectParentsRemoved(parent.Parent as Wrappers.Group);
				}
				
				parent.ChildRemoved -= OnChildRemoved;
				
				DisconnectParentsRemoved(wrapper as Wrappers.Group);
				d_ancestors.Remove(parent);
				
				return true;
			}
			
			private void OnChildRemoved(Wrappers.Group parent, Wrappers.Wrapper obj)
			{
				Wrappers.Wrapper wrapper;
				
				if (d_ancestors.TryGetValue(parent, out wrapper))
				{
					if (wrapper == obj)
					{
						Destroyed(this, new EventArgs());
					}
					else if (DisconnectParentsRemoved(obj as Wrappers.Group))
					{
						Destroyed(this, new EventArgs());
					}
				}
			}
			
			private void HandlePropertyRemoved(object source, Cpg.PropertyRemovedArgs args)
			{
				Cpg.Property property = args.Property;

				if ((d_x != null && property == d_x.Property) || property == d_y.Property)
				{
					Destroyed(this, new EventArgs());
				}
			}
			
			public void Dispose()
			{
				d_y.Property.RemoveNotification("name", OnNameChanged);
				d_y.Property.Object.RemoveNotification("id", OnNameChanged);
				
				d_y.Property.Object.PropertyRemoved -= HandlePropertyRemoved;
				
				DisconnectParentsRemoved(Wrappers.Wrapper.Wrap(d_y.Property.Object.Parent) as Wrappers.Group);
				
				if (d_x != null)
				{
					d_x.Property.Object.PropertyRemoved -= HandlePropertyRemoved;
					
					DisconnectParentsRemoved(Wrappers.Wrapper.Wrap(d_x.Property.Object.Parent) as Wrappers.Group);
				}				
			}
			
			private void UpdateName()
			{
				d_renderer.Label = d_y.Property.FullNameForDisplay;
			}
			
			private void OnNameChanged(object source, GLib.NotifyArgs args)
			{
				UpdateName();
			}
			
			public Cpg.Monitor X
			{
				get
				{
					return d_x;
				}
			}
			
			public Cpg.Monitor Y
			{
				get
				{
					return d_y;
				}
			}
			
			public Plot.Renderers.Line Renderer
			{
				get
				{
					return d_renderer;
				}
			}
			
			public void Update()
			{
				double[] ydata = d_y.GetData();
				double[] xdata = d_x != null ? d_x.GetData() : d_y.GetSites();
				
				Plot.Point<double>[] data = new Plot.Point<double>[xdata.Length];

				for (int i = 0; i < xdata.Length; ++i)
				{
					data[i] = new Plot.Point<double>(xdata[i], ydata[i]);
				}
				
				d_renderer.Data = data;
			}
		}

		public class Graph : EventBox
		{
			private Plot.Widget d_canvas;
			private Frame d_frame;
			private List<Series> d_plots;
			
			public Graph(Plot.Widget canvas)
			{
				if (canvas != null)
				{
					d_canvas = canvas;
				}
				else
				{
					d_canvas = new Plot.Widget();
				}
				
				d_frame = new Frame();
				d_frame.Show();

				d_frame.ShadowType = ShadowType.EtchedIn;
				
				d_canvas.Show();
				d_frame.Add(d_canvas);
				
				d_plots = new List<Series>();
				
				Add(d_frame);
			}
			
			public Graph() : this(null)
			{
			}
			
			public IEnumerable<Series> Plots
			{
				get
				{
					return d_plots;
				}
			}
			
			public int PlotsCount
			{
				get
				{
					return d_plots.Count;
				}
			}
			
			public void Update()
			{
				foreach (Series series in d_plots)
				{
					series.Update();
				}
			}
			
			public void Add(Series plot)
			{
				d_plots.Add(plot);				
				d_canvas.Graph.Add(plot.Renderer);
				
				plot.Destroyed += OnPlotDestroyed;
			}
			
			public void Remove(Series plot)
			{
				if (!d_plots.Contains(plot))
				{
					return;
				}
				
				d_plots.Remove(plot);
				d_canvas.Graph.Remove(plot.Renderer);
				
				plot.Destroyed -= OnPlotDestroyed;
				
				if (d_plots.Count == 0)
				{
					Destroy();
				}
			}
			
			private void OnPlotDestroyed(object source, EventArgs args)
			{
				Remove((Series)source);
			}

			public Gdk.Pixbuf CreateDragIcon()
			{
				Gdk.Rectangle a = d_frame.Allocation;
				
				Gdk.Pixbuf pix = Gdk.Pixbuf.FromDrawable(d_frame.GdkWindow, d_frame.GdkWindow.Colormap, 0, 0, 0, 0, a.Width, a.Height);
				return pix.ScaleSimple((int)(a.Width * 0.7), (int)(a.Height * 0.7), Gdk.InterpType.Hyper);
			}
			
			public Plot.Widget Canvas
			{
				get
				{
					return d_canvas;
				}
			}
		}
		
		private Simulation d_simulation;
		private Wrappers.Network d_network;
		
		private bool d_linkRulers;

		private HPaned d_hpaned;
		private bool d_configured;
		
		private Widgets.Table d_content;
		private Widgets.WrappersTree d_tree;
		
		private List<Graph> d_graphs;
		Gtk.UIManager d_uimanager;
		
		private bool d_autoaxis;
		private bool d_linkaxis;
		
		private bool d_ignoreAxisChange;
		
		private ActionGroup d_actiongroup;

		public Plotting(Wrappers.Network network, Simulation simulation) : base("Monitor")
		{
			d_simulation = simulation;
			d_graphs = new List<Graph>();

			d_simulation.OnEnd += DoPeriodEnd;
			
			d_linkRulers = true;
			d_ignoreAxisChange = false;
			
			d_network = network;
			d_autoaxis = true;
			d_linkaxis = true;

			Build();
			
			SetDefaultSize(500, 400);
		}
		
		protected override void OnDestroyed()
		{
			Plot.Graph.ResetColors();

			base.OnDestroyed();
		}

		private void Build()
		{
			VBox vboxMain = new VBox(false, 0);
			VBox vboxContent = new VBox(false, 3);
			
			BuildUI();
			
			Toolbar toolbar = (Toolbar)d_uimanager.GetWidget("/toolbar");
			toolbar.IconSize = IconSize.SmallToolbar;
			
			vboxMain.PackStart(toolbar, false, false, 0);
			
			d_hpaned = new HPaned();
			d_hpaned.BorderWidth = 0;
			
			d_tree = new Widgets.WrappersTree(d_network);
			d_tree.RendererToggle.Visible = false;
			d_tree.Show();
			
			d_tree.PopulatePopup += HandleTreePopulatePopup;
			d_tree.Activated += HandleTreeActivated;

			d_hpaned.Pack2(d_tree, false, false);
			
			Widgets.Table table = new Widgets.Table(1, 1, true);
			table.Expand = Widgets.Table.ExpandType.Down;
			table.RowSpacing = 1;
			table.ColumnSpacing = 1;
			d_content = table;
			
			d_hpaned.Pack1(table, true, true);
			vboxContent.PackStart(d_hpaned);
			
			vboxMain.PackStart(vboxContent, true, true, 0);
			
			Add(vboxMain);
			vboxMain.ShowAll();
		}

		private void HandleTreePopulatePopup(object source, Widgets.WrappersTree.WrapperNode[] nodes, Menu menu)
		{
			List<Widgets.WrappersTree.WrapperNode> n = new List<Widgets.WrappersTree.WrapperNode>();
			List<Cpg.Property> properties = new List<Cpg.Property>();
			
			foreach (var node in nodes)
			{
				if (node.Property != null)
				{
					n.Add(node);
					properties.Add(node.Property);
				}
			}

			if (n.Count == 0)
			{
				return;
			}
			
			MenuItem item;
			
			item = new MenuItem("Add");
			item.Show();
			item.Activated += delegate {
				foreach (var node in n)
				{
					Add(node.Property);
				}
			};

			menu.Append(item);
			
			if (n.Count > 1)
			{
				item = new MenuItem("Add Merged");
				item.Show();
				item.Activated += delegate {
					Add(properties);
				};
				
				menu.Append(item);
			}
		}

		private void HandleTreeActivated(object source, Widgets.WrappersTree.WrapperNode node)
		{
			if (node.Property == null)
			{
				return;
			}
			
			Add(node.Property);
		}

		public uint Columns
		{
			get
			{
				return d_content.NColumns;
			}
		}
		
		public uint Rows
		{
			get
			{
				return d_content.NRows;
			}
		}
		
		private void DoLinkRulers(Graph graph)
		{
			foreach (Graph g in d_graphs)
			{
				if (g != graph)
				{
					g.Canvas.Graph.Ruler = graph.Canvas.Graph.Ruler;
				}
			}
		}
		
		private void DoLinkRulersLeave()
		{
			foreach (Graph g in d_graphs)
			{
				g.Canvas.Graph.Ruler = null;
			}
		}
		
		private void MergeWith(Graph source, Graph target)
		{
			List<Series> cp = new List<Series>(source.Plots);
			
			foreach (Series series in cp)
			{
				source.Remove(series);
				target.Add(series);
			}
			
			source.Destroy();
			QueueDraw();
		}
		
		private void MergeTo(Graph source, Point direction)
		{			
			Graph target = (Graph)d_content.Find(source, (int)direction.X, (int)direction.Y);
			
			if (target == null)
			{
				return;
			}
			
			MergeWith(source, target);
		}
		
		private void MakeMergeMenuItem(Graph graph, ActionGroup gp, UIManager manager, Point pos, string stockid, string label, Point dir)
		{
			if (pos.X + dir.X < 0 || pos.X + dir.X >= d_content.NColumns)
			{
				return;
			}
			
			if (pos.Y + dir.Y < 0 || pos.Y + dir.Y >= d_content.NRows)
			{
				return;
			}
			
			Action action = new Action("ActionMerge" + label, label, null, stockid);
			
			action.Activated += delegate {
				MergeTo(graph, dir);
			};
			
			gp.Add(action);
			manager.AddUi(manager.NewMergeId(), "/ui/popup/MainPlaceholder/MergeMenu", label, "ActionMerge" + label, UIManagerItemType.Menuitem, false);
		}
		
		private void MakeUnmergeMenuItems(ActionGroup gp, UIManager manager, Graph graph)
		{
			gp.Add(new Gtk.Action("ActionUnmerge", "Unmerge", null, null));
			
			uint mid = manager.NewMergeId();
			manager.AddUi(mid, "/ui/popup/MainPlaceholder", "Unmerge", "ActionUnmerge", UIManagerItemType.Menu, false);
			
			uint idx = 0;

			foreach (Series series in graph.Plots)
			{
				Series s = series;
				Gtk.Action action = new Gtk.Action("ActionUnmerge" + idx, series.Y.Property.FullNameForDisplay, null, null);
				
				action.Activated += delegate {
					DoUnmerge(graph, s);
				};
				
				gp.Add(action);
				manager.AddUi(mid, "/ui/popup/MainPlaceholder/Unmerge", "Unmerge" + idx, "ActionUnmerge" + idx, UIManagerItemType.Menuitem, false);
				
				++idx;
			}
		}
		
		private void DoUnmerge(Graph graph, Series series)
		{
			Point pt = d_content.GetPosition(graph);

			graph.Remove(series);
			
			d_content.EnsureSize(d_content.NRows + 1, d_content.NColumns);
			Graph g = Add((int)d_content.NRows - 1, (int)d_content.NColumns - 1, series);
			
			Plot.Settings settings = new Plot.Settings();

			settings.Get(graph.Canvas.Graph);
			settings.Set(g.Canvas.Graph);
			
			d_content.SetPosition(g, (int)pt.X, (int)pt.Y + 1);
			
			UpdateAutoScaling();
		}
		
		public IEnumerable<Graph> Graphs
		{
			get
			{
				return d_graphs;
			}
		}
		
		public Graph Add(IEnumerable<Cpg.Property> y)
		{
			List<Cpg.Monitor> mons = new List<Cpg.Monitor>();

			foreach (Cpg.Property p in y)
			{
				mons.Add(new Cpg.Monitor(d_network, p));
			}
			
			return Add(mons);
		}
		
		public Graph Add(Cpg.Property y)
		{
			return Add(new Cpg.Monitor(d_network, y));
		}
		
		private void NewGraphPosition(out int rr, out int rc)
		{
			for (int r = 0; r < d_content.NRows; ++r)
			{
				for (int c = 0; c < d_content.NColumns; ++c)
				{
					if (d_content.At(c, r) == null)
					{
						rr = r;
						rc = c;
						return;
					}
				}
			}
			
			rr = (int)d_content.NRows;
			rc = (int)d_content.NColumns - 1;
		}

		public Graph Add(Cpg.Monitor y)
		{
			return Add(null, y);
		}
		
		public Graph Add(Cpg.Monitor x, Cpg.Monitor y)
		{
			int r;
			int c;
			
			NewGraphPosition(out r, out c);
			return Add(r, c, x, y);
		}
		
		public Graph Add(IEnumerable<Cpg.Monitor> ys)
		{
			int r;
			int c;
			
			NewGraphPosition(out r, out c);
			return Add(r, c, ys);
		}
		
		public Graph Add(int row, int col, Cpg.Monitor x, Cpg.Monitor y)
		{
			Plot.Renderers.Line line = new Plot.Renderers.Line();
			Series s = new Series(x, y, line);
				
			return Add(row, col, new Series[] {s});
		}

		public Graph Add(int row, int col, IEnumerable<Cpg.Monitor> y)
		{
			List<Series> series = new List<Series>();
			
			foreach (Cpg.Monitor m in y)
			{
				Plot.Renderers.Line line = new Plot.Renderers.Line();
				Series s = new Series(null, m, line);
				
				series.Add(s);
			}
			
			return Add(row, col, series);
		}
		
		public Graph Add(int row, int col, Series series)
		{
			return Add(row, col, new Series[] {series});
		}
		
		private delegate void IgnoreHandler();
		
		private void IgnoreAxisChange(IgnoreHandler handler)
		{
			if (!d_ignoreAxisChange)
			{
				d_ignoreAxisChange = true;
				handler();
				d_ignoreAxisChange = false;
			}
			else
			{
				handler();
			}
		}

		public Graph Add(int row, int col, IEnumerable<Series> series)
		{
			Graph graph = (Graph)d_content.At(row, col);
			
			if (graph != null)
			{
				foreach (Series s in series)
				{
					graph.Add(s);
				}

				return graph;
			}
			
			graph = new Graph();
			Cpg.Studio.Settings.PlotSettings.Set(graph.Canvas.Graph);
			
			foreach (Series s in series)
			{
				graph.Add(s);
			}
			
			graph.Show();
			d_graphs.Add(graph);

			d_content.Add(graph, row, col);
			
			graph.MotionNotifyEvent += OnGraphMotionNotify;
			graph.LeaveNotifyEvent += OnGraphLeaveNotify;
			graph.EnterNotifyEvent += OnGraphEnterNotify;
			
			Plot.Graph g = graph.Canvas.Graph;
			
			g.XAxis.Changed += delegate
			{
				if (d_ignoreAxisChange || !d_linkaxis)
				{
					return;
				}
				
				LinkAxes(graph, a => a.XAxis);
			};
			
			g.YAxis.Changed += delegate
			{
				if (d_ignoreAxisChange || !d_linkaxis)
				{
					return;
				}
				
				LinkAxes(graph, a => a.YAxis);
			};
			
			graph.Canvas.PopulatePopup += delegate (object source, Gtk.UIManager manager) {
				OnGraphPopulatePopup(graph, manager);
			};
			
			if (d_linkaxis && !d_autoaxis && d_graphs.Count > 1)
			{
				Graph first = d_graphs[0];
				
				IgnoreAxisChange(() => {
					g.XAxis.Update(first.Canvas.Graph.XAxis);
					g.YAxis.Update(first.Canvas.Graph.YAxis);
				});
			}
			else
			{		
				UpdateAutoScaling();
			}
			
			d_simulation.Resimulate();
			return graph;
		}
		
		private void OnGraphPopulatePopup(Graph graph, Gtk.UIManager manager)
		{
			ActionGroup gp = new ActionGroup("HookActions");
			Gtk.Action action = new Gtk.Action("ActionClose", "Close", "Close graph", Gtk.Stock.Close);
			
			action.Activated += delegate {
				graph.Destroy();
			};
			
			gp.Add(action);
			
			manager.InsertActionGroup(gp, 0);
			
			Point pos = d_content.GetPosition(graph);
			
			gp.Add(new Gtk.Action("ActionMergeMenu", "Merge", null, null));
			manager.AddUi(manager.NewMergeId(),
			              "/ui/popup/MainPlaceholder",
			              "MergeMenu",
			              "ActionMergeMenu",
			              UIManagerItemType.Menu, false);
			
			MakeMergeMenuItem(graph, gp, manager, pos, Gtk.Stock.GotoTop, "Merge Up", new Point(0, -1));
			MakeMergeMenuItem(graph, gp, manager, pos, Gtk.Stock.GotoBottom, "Merge Down", new Point(0, 1));
			MakeMergeMenuItem(graph, gp, manager, pos, Gtk.Stock.GotoLast, "Merge Right", new Point(1, 0));
			MakeMergeMenuItem(graph, gp, manager, pos, Gtk.Stock.GotoFirst, "Merge Left", new Point(-1, 0));
			
			if (graph.PlotsCount > 1)
			{
				MakeUnmergeMenuItems(gp, manager, graph);
			}
			
			manager.AddUi(manager.NewMergeId(),
	              "/ui/popup/MainPlaceholder",
	              "Close",
	              "ActionClose",
	              UIManagerItemType.Auto,
	              false);
		}
		
		[GLib.ConnectBefore]
		private void OnGraphEnterNotify(object o, EnterNotifyEventArgs args)
		{
			Graph graph = (Graph)o;

			if (d_linkRulers && graph.Canvas.Graph.ShowRuler)
			{
				DoLinkRulers(graph);
			}
		}
		
		[GLib.ConnectBefore]
		private void OnGraphLeaveNotify(object o, LeaveNotifyEventArgs args)
		{
			Graph graph = (Graph)o;
			
			if (d_linkRulers && graph.Canvas.Graph.ShowRuler)
			{
				DoLinkRulersLeave();
			}
		}
		
		[GLib.ConnectBefore]
		private void OnGraphMotionNotify(object o, MotionNotifyEventArgs args) {
			Graph graph = (Graph)o;

			if (d_linkRulers && graph.Canvas.Graph.ShowRuler)
			{
				DoLinkRulers(graph);
			}
		}
		
		private void BuildUI()
		{
			d_uimanager = new UIManager();
			d_actiongroup = new ActionGroup("NormalActions");
			
			d_actiongroup.Add(new ToggleActionEntry[] {
				new ToggleActionEntry("ActionAutoAxis", Gtk.Stock.ZoomFit, "Auto Axis", "<Control>r", "Automatically scale axes to fit data", OnAutoAxisToggled, d_autoaxis),
				new ToggleActionEntry("ActionLinkAxis", Cpg.Studio.Stock.Chain, "Link Axis", "<Control>l", "Automatically scale all axes to the same range", OnLinkAxisToggled, d_linkaxis)
			});
			
			d_uimanager.InsertActionGroup(d_actiongroup, 0);
			d_uimanager.AddUiFromResource("monitor-ui.xml");

			AddAccelGroup(d_uimanager.AccelGroup);
		}
		
		public Point Size
		{
			get
			{
				return new Point((int)d_content.NColumns, (int)d_content.NRows);
			}
			set
			{
				if (value.X <= 0)
				{
					value.X = (int)d_content.NColumns;
				}
				
				if (value.Y <= 0)
				{
					value.Y = (int)d_content.NRows;
				}
				
				d_content.Resize((uint)value.X, (uint)value.Y);
			}
		}

		private void DoSelectToggled()
		{
			d_tree.Visible = !d_tree.Visible;
			d_hpaned.QueueDraw();
		}
		
		protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
		{
			bool ret = base.OnConfigureEvent(evnt);
			
			if (d_configured)
			{
				return ret;
			}
			
			d_hpaned.Position = Allocation.Width - 150;
			d_configured = true;
			
			return ret;
		}
		
		private void DoPeriodEnd(object source, EventArgs args)
		{
			foreach (Graph graph in d_graphs)
			{
				graph.Update();
			}
			
			UpdateAutoScaling();
		}
		
		private void UpdateAutoScaling()
		{
			Plot.Range<double> xrange = new Plot.Range<double>();
			Plot.Range<double> yrange = new Plot.Range<double>();
			
			bool first = true;

			foreach (Graph graph in d_graphs)
			{
				Plot.Graph g = graph.Canvas.Graph;
				
				if (d_autoaxis && !d_linkaxis)
				{
					g.XAxisMode = Plot.AxisMode.Auto;
					g.YAxisMode = Plot.AxisMode.Auto;
				}
				else
				{
					g.XAxisMode = Plot.AxisMode.Fixed;
					g.YAxisMode = Plot.AxisMode.Fixed;
				}
					
				if (d_autoaxis && d_linkaxis)
				{
					Plot.Range<double> xr = g.DataXRange;
					Plot.Range<double> yr = g.DataYRange;

					if (first || xr.Min < xrange.Min)
					{
						xrange.Min = xr.Min;
					}
					
					if (first || xr.Max > xrange.Max)
					{
						xrange.Max = xr.Max;
					}
					
					if (first || yr.Min < yrange.Min)
					{
						yrange.Min = yr.Min;
					}
					
					if (first || yr.Max > yrange.Max)
					{
						yrange.Max = yr.Max;
					}
					
					first = false;
				}
			}
			
			IgnoreAxisChange(() => {
				if (d_autoaxis && d_linkaxis)
				{
					foreach (Graph graph in d_graphs)
					{
						Plot.Graph g = graph.Canvas.Graph;
						
						g.XAxis.Update(xrange);
						g.YAxis.Update(Widen(yrange, 0.1));
					}
				}
			});
		}
		
		private Plot.Range<double> Widen(Plot.Range<double> range, double fraction)
		{
			double df = range.Max - range.Min;
			
			if (df == 0)
			{
				return new Plot.Range<double>(-1, 1);
			}
			else
			{
				return new Plot.Range<double>(range.Min - df * fraction, range.Max + df * fraction);
			}
		}
		
		private void OnAutoAxisToggled(object sender, EventArgs args)
		{
			d_autoaxis = ((ToggleAction)sender).Active;
			
			UpdateAutoScaling();
		}
		
		private void OnLinkAxisToggled(object sender, EventArgs args)
		{
			d_linkaxis = ((ToggleAction)sender).Active;
			
			UpdateAutoScaling();
		}
		
		private delegate Plot.Range<double> RangeSelector(Plot.Graph graph);
		
		private void LinkAxes(Graph graph, RangeSelector selector)
		{
			IgnoreAxisChange(() => {
				Plot.Range<double> nr = selector(graph.Canvas.Graph);
	
				foreach (Graph g in d_graphs)
				{
					if (g == graph)
					{
						continue;
					}
					
					selector(g.Canvas.Graph).Update(nr);
				}
			});
		}
		
		private void ToggleLinkAxis()
		{
			ToggleAction action = d_actiongroup.GetAction("ActionLinkAxis") as ToggleAction;
			
			action.Active = !action.Active;
		}
		
		private void ToggleAutoAxis()
		{
			ToggleAction action = d_actiongroup.GetAction("ActionAutoAxis") as ToggleAction;
			
			action.Active = !action.Active;
		}
	}
}
