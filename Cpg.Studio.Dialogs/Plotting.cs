using System;
using Gtk;
using System.Collections.Generic;
using System.Reflection;
using Biorob.Math;

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
			
			private Cpg.Property d_xprop;
			private Cpg.Property d_yprop;

			private Plot.Renderers.Line d_renderer;
			
			public event EventHandler Destroyed = delegate {};
			private Dictionary<Wrappers.Group, Wrappers.Wrapper> d_ancestors;
			
			public event EventHandler ActiveChanged = delegate {};
			
			public Series(Cpg.Property x, Cpg.Property y, Plot.Renderers.Line renderer)
			{
				d_renderer = renderer;
				
				d_xprop = x;
				d_yprop = y;
				
				d_renderer.ActiveChanged += delegate {
					ActiveChanged(this, new EventArgs());
				};
				
				d_ancestors = new Dictionary<Wrappers.Group, Wrappers.Wrapper>();
				
				if (y != null)
				{
					y.AddNotification("name", OnNameChanged);
					y.Object.AddNotification("id", OnNameChanged);
				
					y.Object.PropertyRemoved += HandlePropertyRemoved;
				
					ConnectParentsRemoved(Wrappers.Wrapper.Wrap(y.Object.Parent) as Wrappers.Group, y.Object);
				}
				
				if (x != null)
				{
					x.AddNotification("name", OnNameChanged);
					x.Object.AddNotification("id", OnNameChanged);

					x.Object.PropertyRemoved += HandlePropertyRemoved;
					
					ConnectParentsRemoved(Wrappers.Wrapper.Wrap(x.Object.Parent) as Wrappers.Group, x.Object);
				}
				
				UpdateName();
			}
			
			public Series(Cpg.Monitor x, Cpg.Monitor y, Plot.Renderers.Line renderer) : this(x != null ? x.Property : null,
			                                                                                 y != null ? y.Property : null,
			                                                                                 renderer)
			{
				d_x = x;
				d_y = y;
			}
			
			public bool Vector
			{
				get { return d_renderer is Plot.Renderers.Vector; }
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

				if (property == d_xprop || property == d_yprop)
				{
					Destroyed(this, new EventArgs());
				}
			}
			
			public void Dispose()
			{
				if (d_yprop != null)
				{
					d_yprop.RemoveNotification("name", OnNameChanged);
					d_yprop.Object.RemoveNotification("id", OnNameChanged);
				
					d_yprop.Object.PropertyRemoved -= HandlePropertyRemoved;
				
					DisconnectParentsRemoved(Wrappers.Wrapper.Wrap(d_yprop.Object.Parent) as Wrappers.Group);
				}
				
				if (d_xprop != null)
				{
					d_xprop.RemoveNotification("name", OnNameChanged);
					d_xprop.Object.RemoveNotification("id", OnNameChanged);
				
					d_xprop.Object.PropertyRemoved -= HandlePropertyRemoved;
				
					DisconnectParentsRemoved(Wrappers.Wrapper.Wrap(d_xprop.Object.Parent) as Wrappers.Group);
				}				
			}
			
			private void UpdateName()
			{
				if (d_yprop != null)
				{
					d_renderer.YLabel = d_yprop.FullNameForDisplay;
				}
				else
				{
					d_renderer.YLabel = null;
				}
				
				if (d_xprop != null)
				{
					d_renderer.XLabel = d_xprop.FullNameForDisplay;
				}
				else
				{
					d_renderer.XLabel = null;
				}
			}
			
			private void OnNameChanged(object source, GLib.NotifyArgs args)
			{
				UpdateName();
			}
			
			public Cpg.Property XProp
			{
				get { return d_xprop; }
			}
			
			public Cpg.Property YProp
			{
				get { return d_yprop; }
			}
			
			public Cpg.Monitor X
			{
				get { return d_x; }
			}
			
			public Cpg.Monitor Y
			{
				get { return d_y; }
			}
			
			public Plot.Renderers.Line Renderer
			{
				get { return d_renderer; }
			}
			
			public void Update(List<Point> xy, List<double> alpha)
			{
				Plot.Renderers.Vector vector = d_renderer as Plot.Renderers.Vector;
				
				if (vector == null)
				{
					return;
				}
				
				vector.Data = xy;
				vector.Alpha = alpha;
			}
			
			public void Update()
			{
				if (d_y == null)
				{
					return;
				}

				double[] ydata = d_y.GetData();
				double[] xdata = d_x != null ? d_x.GetData() : d_y.GetSites();

				Point[] data = new Point[xdata.Length];

				for (int i = 0; i < xdata.Length; ++i)
				{
					data[i] = new Point(xdata[i], ydata[i]);
				}
				
				d_renderer.Data = data;
			}
		}

		public class Graph : EventBox, Widgets.IDragIcon
		{
			private Plot.Widget d_canvas;
			private List<Series> d_plots;
			private bool d_isTime;
			
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
				
				d_canvas.Show();
				
				Frame frame = new Frame();
				frame.ShadowType = ShadowType.EtchedIn;
				frame.Add(d_canvas);
				frame.Show();
				
				Add(frame);
				
				d_plots = new List<Series>();
				
				AddEvents((int)(Gdk.EventMask.AllEventsMask));
			}
			
			public Graph() : this(null)
			{
			}
			
			public bool IsTime
			{
				get { return d_isTime; }
			}
			
			public bool OnlyVectors
			{
				get
				{
					foreach (Series series in d_plots)
					{
						if (!series.Vector)
						{
							return false;
						}
					}
					
					return true;
				}
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
			
			public Series Find(Plot.Renderers.Renderer renderer)
			{
				foreach (Series series in d_plots)
				{
					if (series.Renderer == renderer)
					{
						return series;
					}
				}
				
				return null;
			}
			
			public void Add(Series plot)
			{
				d_plots.Add(plot);				
				d_canvas.Graph.Add(plot.Renderer);
				
				plot.Destroyed += OnPlotDestroyed;
				
				d_isTime = plot.Y != null && plot.X == null;
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
				Gdk.Rectangle a = Allocation;
				
				int w = (int)(a.Width * 0.5);
				int h = (int)(a.Height * 0.5);
				
				Gdk.Pixmap pix = new Gdk.Pixmap(GdkWindow, w, h);
				pix.DrawRectangle(Style.WhiteGC, true, new Gdk.Rectangle(0, 0, w, h));

				Plot.Export.Gdk ex = new Plot.Export.Gdk(pix);
				
				ex.Do(() => {
					ex.Export(d_canvas.Graph, new Plot.Rectangle(0, 0, w, h), (ctx, g, d) => {
						ctx.Rectangle(0, 0, w, h);
						ctx.SetSourceRGBA(1, 1, 1, 0.5);
						ctx.Fill();
					});
				});
				
				return Gdk.Pixbuf.FromDrawable(pix, pix.Colormap, 0, 0, 0, 0, w, h);
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
		
		private Expander d_expanderTime;
		private Expander d_expanderPhase;
		
		private bool d_ignoreExpanderChanged;
		private Widgets.WrappersTree d_phaseTreeY;
		private Widgets.WrappersTree d_phaseTreeX;
		private Widget d_phaseWidget;
		private CheckButton d_vector;
		private List<Series> d_vectors;
		
		private Dictionary<Series, Point> d_initialConditions;

		public Plotting(Wrappers.Network network, Simulation simulation) : base("Monitor")
		{
			d_simulation = simulation;
			d_graphs = new List<Graph>();
			
			d_initialConditions = new Dictionary<Series, Point>();

			d_simulation.OnBegin += DoPeriodBegin;
			d_simulation.OnEnd += DoPeriodEnd;
			
			d_linkRulers = true;
			d_ignoreAxisChange = false;
			
			d_network = network;
			d_autoaxis = true;
			d_linkaxis = true;
			
			d_vectors = new List<Series>();

			Build();
			
			SetDefaultSize(500, 400);
		}
		
		private List<double> VectorMakeGrid(Plot.Ticks ticks, Biorob.Math.Range range, int factor)
		{
			List<double> ret = new List<double>();
			List<double> tt = new List<double>(ticks);
			
			for (int vi = 0; vi < tt.Count; ++vi)
			{
				double rest = 0;
				double v = tt[vi];

				for (int i = 0; i < factor; ++i)
				{
					double p = v + rest;
					
					if (p > range.Min && p < range.Max)
					{
						ret.Add(v + rest);
					}

					rest += (ticks.CalculatedTickSize - rest) / (factor - i);
				}
			}
			
			return ret;
		}
		
		private List<Point> VectorGrid(Graph graph)
		{
			Plot.Graph g = graph.Canvas.Graph;
			
			int factor = 2;
			
			List<double> xticks = VectorMakeGrid(g.XTicks, g.XAxis, factor);
			List<double> yticks = VectorMakeGrid(g.YTicks, g.YAxis, factor);

			List<Point> ret = new List<Point>();
			
			for (int x = 0; x < xticks.Count; ++x)
			{
				for (int y = 0; y < yticks.Count; ++y)
				{
					ret.Add(new Point(xticks[x], yticks[y]));
				}
			}
			
			return ret;
		}
		
		private void SimulateVectorFields()
		{
			if (d_vectors.Count == 0)
			{
				return;
			}
			
			// Record the current state
			IntegratorState s = d_simulation.Network.Integrator.State;
			
			List<KeyValuePair<Cpg.Property, double>> state = new List<KeyValuePair<Cpg.Property, double>>();
			
			foreach (LinkAction action in s.IntegratedLinkActions())
			{
				state.Add(new KeyValuePair<Cpg.Property, double>(action.TargetProperty, action.TargetProperty.Value));
			}
			
			foreach (LinkAction action in s.DirectLinkActions())
			{
				state.Add(new KeyValuePair<Cpg.Property, double>(action.TargetProperty, action.TargetProperty.Value));
			}
			
			double step = d_simulation.Range.Step;
			
			foreach (Graph graph in d_graphs)
			{
				List<Point> grid = null;

				foreach (Series series in graph.Plots)
				{
					if (!(series.Renderer is Plot.Renderers.Vector))
					{
						continue;
					}
					
					if (grid == null)
					{
						grid = VectorGrid(graph);
					}
					
					// We are going to go over the grid, setting initial conditions
					// Then we simulate exactly one step and compute the difference
					List<double> alpha = new List<double>(grid.Count);
					
					for (int i = 0; i < grid.Count; ++i)
					{
						series.XProp.Value = grid[i].X;
						series.YProp.Value = grid[i].Y;

						d_simulation.Step(step);
						
						double dy = series.YProp.Value - grid[i].Y;
						double dx = series.XProp.Value - grid[i].X;
						
						alpha.Add(System.Math.Atan2(dy, dx));

						// Restore the state
						foreach (KeyValuePair<Cpg.Property, double> ss in state)
						{
							ss.Key.Value = ss.Value;
						}
					}
					
					series.Update(grid, alpha);
				}
			}
		}

		private void DoPeriodBegin(object o, BeginArgs args)
		{
			ApplyInitialConditions();
			
			// We do this first
			SimulateVectorFields();
		}
		
		private void ApplyInitialConditions()
		{
			foreach (KeyValuePair<Series, Point> pair in d_initialConditions)
			{
				Series series = pair.Key;

				if (series.X != null)
				{
					series.X.Property.Value = pair.Value.X;
				}
				
				if (series.Y != null)
				{
					series.Y.Property.Value = pair.Value.Y;
				}
			}
		}
		
		protected override void OnDestroyed()
		{
			base.OnDestroyed();
		}

		private void Build()
		{
			VBox vboxMain = new VBox(false, 0);
			vboxMain.Show();

			VBox vboxContent = new VBox(false, 3);
			vboxContent.Show();
			
			BuildUI();
			
			Toolbar toolbar = (Toolbar)d_uimanager.GetWidget("/toolbar");
			toolbar.IconSize = IconSize.SmallToolbar;
			toolbar.Show();
			
			vboxMain.PackStart(toolbar, false, false, 0);
			
			d_hpaned = new HPaned();
			d_hpaned.BorderWidth = 0;
			d_hpaned.Show();
			
			d_tree = new Widgets.WrappersTree(d_network);
			d_tree.RendererToggle.Visible = false;
			d_tree.Show();

			d_tree.Filter += FilterFunctions;
			
			VBox vboxExpand = new VBox(false, 3);
			vboxExpand.Show();
			vboxExpand.BorderWidth = 6;
			
			d_tree.PopulatePopup += HandleTreePopulatePopup;
			d_tree.Activated += HandleTreeActivated;
			
			d_expanderTime = new Expander("Time");
			d_expanderTime.Show();
			d_expanderTime.Expanded = true;
			
			d_expanderTime.AddNotification("expanded", delegate {
				ExpandedChanged(d_expanderTime, d_expanderPhase);
			});
			
			vboxExpand.PackStart(d_expanderTime, false, false, 0);
			vboxExpand.PackStart(d_tree, true, true, 0);
			
			d_expanderPhase = new Expander("Phase");
			d_expanderPhase.Show();
			
			d_expanderPhase.AddNotification("expanded", delegate {
				ExpandedChanged(d_expanderPhase, d_expanderTime);
			});

			vboxExpand.PackStart(d_expanderPhase, false, true, 0);
			
			VBox phase = new VBox(false, 6);

			d_phaseTreeX = new Cpg.Studio.Widgets.WrappersTree(d_network);
			d_phaseTreeX.RendererToggle.Visible = false;
			d_phaseTreeX.Filter += FilterFunctions;
			d_phaseTreeX.Label = "X:";
			d_phaseTreeX.Show();
			d_phaseTreeX.Filter += FilterIntegratedOnly;
			
			phase.PackStart(d_phaseTreeX, true, true, 0);
			
			d_phaseTreeY = new Cpg.Studio.Widgets.WrappersTree(d_network);
			d_phaseTreeY.RendererToggle.Visible = false;
			d_phaseTreeY.Filter += FilterFunctions;
			d_phaseTreeY.Label = "Y:";
			d_phaseTreeY.Show();
			d_phaseTreeY.Filter += FilterIntegratedOnly;
			
			phase.PackStart(d_phaseTreeY, true, true, 0);
			
			HBox hhbox = new HBox(false, 0);
			hhbox.Show();
			
			Button button = new Button(Gtk.Stock.Add);
			button.Show();
			
			button.Clicked += OnPhaseAddClicked;

			hhbox.PackEnd(button, false, false, 0);
			
			d_vector = new CheckButton("Vector field");
			d_vector.Show();
			
			hhbox.PackStart(d_vector, false, false, 0);
			
			phase.PackStart(hhbox, false, false, 0);
			
			vboxExpand.PackStart(phase);
			d_phaseWidget = phase;

			d_hpaned.Pack2(vboxExpand, false, false);
			
			d_content = new Widgets.Table();
			d_content.Expand = Widgets.Table.ExpandType.Down;
			d_content.Show();
			
			d_content.CreateGraph += CreateGraph;
			
			d_hpaned.Pack1(d_content, true, true);
			vboxContent.PackStart(d_hpaned);
			
			vboxMain.PackStart(vboxContent, true, true, 0);
			
			Add(vboxMain);
		}
		
		private void OnPhaseAddClicked(object source, EventArgs args)
		{
			Widgets.WrappersTree.WrapperNode[] xnodes = d_phaseTreeX.SelectedNodes;
			Widgets.WrappersTree.WrapperNode[] ynodes = d_phaseTreeY.SelectedNodes;
			
			Cpg.Property[] xprops;
			Cpg.Property[] yprops;
			
			xprops = Array.ConvertAll<Widgets.WrappersTree.WrapperNode, Cpg.Property>(Array.FindAll(xnodes, a => a.Property != null), a => a.Property);
			yprops = Array.ConvertAll<Widgets.WrappersTree.WrapperNode, Cpg.Property>(Array.FindAll(ynodes, a => a.Property != null), a => a.Property);
			
			if (xprops.Length == 0 || yprops.Length == 0)
			{
				return;
			}
			
			List<Cpg.Property> xx = new List<Cpg.Property>();
			List<Cpg.Property> yy = new List<Cpg.Property>();
			
			// If there are as many x properties as y properties, make x/y pairs
			// otherwise make phase plots for all combinations of x/y
			if (xprops.Length == yprops.Length)
			{
				xx = new List<Cpg.Property>(xprops);
				yy = new List<Cpg.Property>(yprops);
			}
			else
			{
				foreach (Cpg.Property x in xprops)
				{
					foreach (Cpg.Property y in yprops)
					{
						xx.Add(x);
						yy.Add(y);
					}
				}
			}
			
			if (d_vector.Active)
			{			
				List<Series> series = new List<Series>();
			
				for (int i = 0; i < xx.Count; ++i)
				{
					series.Add(CreateVectorSeries(xx[i], yy[i]));
				}

				Add(-1, -1, series);
			}
			else
			{			
				Add(xx, yy);
			}
		}
		
		private void FilterIntegratedOnly(Widgets.WrappersTree.WrapperNode node, ref bool ret)
		{
			if (node.Property != null && !node.Property.Integrated)
			{
				ret = false;
			}
		}
		
		private void FilterFunctions(Widgets.WrappersTree.WrapperNode node, ref bool ret)
		{
			if (node.Wrapper is Wrappers.Function)
			{
				ret = false;
			}
			else if (node.Property != null && node.Property.Object is Cpg.Function)
			{
				ret = false;
			}
		}
		
		private void ExpandedChanged(Expander e1, Expander e2)
		{
			if (d_ignoreExpanderChanged)
			{
				return;
			}

			d_ignoreExpanderChanged = true;
			e2.Expanded = !e1.Expanded;
			
			d_tree.Visible = d_expanderTime.Expanded;
			d_phaseWidget.Visible = d_expanderPhase.Expanded;

			d_ignoreExpanderChanged = false;
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

		private void HandleTreeActivated(object source, Widgets.WrappersTree.WrapperNode[] nodes)
		{
			List<Cpg.Property> properties = new List<Cpg.Property>();

			foreach (Widgets.WrappersTree.WrapperNode node in nodes)
			{
				if (node.Property != null)
				{
					properties.Add(node.Property);
				}
			}
			
			Add(properties);
		}
		
		public bool IndexOf(Graph graph, out int r, out int c)
		{
			return d_content.IndexOf(graph, out r, out c);
		}

		public int Columns
		{
			get
			{
				return d_content.Columns;
			}
		}
		
		public int Rows
		{
			get
			{
				return d_content.Rows;
			}
		}
		
		public IEnumerable<Graph> SameGraphs(Graph graph)
		{
			return graph.IsTime ? TimeGraphs : PhaseGraphs;
		}
		
		private void DoLinkRulers(Graph graph)
		{
			foreach (Graph g in SameGraphs(graph))
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
			
			if (source.IsTime != target.IsTime)
			{
				return;
			}
			
			foreach (Series series in cp)
			{
				source.Remove(series);
				target.Add(series);
			}
			
			source.Destroy();
			QueueDraw();
		}
		
		private void MergeTo(Graph source, int dr, int dc)
		{
			Graph target = (Graph)d_content.Find(source, dr, dc);
			
			if (target == null)
			{
				return;
			}
			
			MergeWith(source, target);
		}
		
		private void MakeMergeMenuItem(Graph graph, ActionGroup gp, UIManager manager, int r, int c, string stockid, string label, int dr, int dc)
		{
			if (r + dr < 0 || r + dr >= d_content.Rows ||
			    c + dc < 0 || c + dc >= d_content.Columns)
			{
				return;
			}
			
			Gtk.Action action = new Gtk.Action("ActionMerge" + label, label, null, stockid);
			
			action.Activated += delegate {
				MergeTo(graph, dr, dc);
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
			int r;
			int c;

			if (!d_content.IndexOf(graph, out r, out c))
			{
				return;
			}

			graph.Remove(series);
			
			Graph g = Add(-1, -1, series);
			
			Plot.Settings settings = new Plot.Settings();

			settings.Get(graph.Canvas.Graph);
			settings.Set(g.Canvas.Graph);
						
			UpdateAutoScaling();
		}
		
		public IEnumerable<Graph> Graphs
		{
			get
			{
				return d_graphs;
			}
		}
		
		public Series CreateVectorSeries(Cpg.Property x, Cpg.Property y)
		{
			Plot.Renderers.Vector rend = new Plot.Renderers.Vector();
			rend.MarkerStyle = Plot.Renderers.Line.MarkerType.FilledCircle;
			rend.PixelLength = 10;

			return new Series(x, y, rend);
		}
		
		public Graph Add(IEnumerable<Cpg.Property> x, IEnumerable<Cpg.Property> y)
		{
			List<Cpg.Monitor> mony = new List<Cpg.Monitor>();

			foreach (Cpg.Property p in y)
			{
				mony.Add(new Cpg.Monitor(d_network, p));
			}
			
			List<Cpg.Monitor> monx = new List<Cpg.Monitor>();

			foreach (Cpg.Property p in x)
			{
				monx.Add(new Cpg.Monitor(d_network, p));
			}
			
			return Add(monx, mony);
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

		public Graph Add(Cpg.Monitor y)
		{
			return Add(null, y);
		}
		
		public Graph Add(Cpg.Monitor x, Cpg.Monitor y)
		{
			return Add(-1, -1, x, y);
		}
		
		public Graph Add(IEnumerable<Cpg.Monitor> xs, IEnumerable<Cpg.Monitor> ys)
		{
			return Add(-1, -1, xs, ys);
		}
		
		public Graph Add(IEnumerable<Cpg.Monitor> ys)
		{
			return Add(-1, -1, null, ys);
		}
		
		public Graph Add(int row, int col, Cpg.Monitor x, Cpg.Monitor y)
		{				
			return Add(row, col, new Series[] {CreateLineSeries(x, y)});
		}
		
		public Series CreateLineSeries(Cpg.Monitor x, Cpg.Monitor y)
		{
			return new Series(x, y, new Plot.Renderers.Line());
		}

		public Graph Add(int row, int col, IEnumerable<Cpg.Monitor> x, IEnumerable<Cpg.Monitor> y)
		{
			List<Series> series = new List<Series>();
			
			if (x == null)
			{			
				foreach (Cpg.Monitor m in y)
				{
					series.Add(CreateLineSeries(null, m));
				}
			}
			else
			{
				IEnumerator<Cpg.Monitor> xe = x.GetEnumerator();
				IEnumerator<Cpg.Monitor> ye = y.GetEnumerator();
				
				while (xe.MoveNext() && ye.MoveNext())
				{
					series.Add(CreateLineSeries(xe.Current, ye.Current));
				}
			}
			
			Graph ret = Add(row, col, series);
			d_simulation.Resimulate();
			
			return ret;
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
		
		private Graph CreateGraph()
		{
			Graph graph = new Graph();
			
			graph.Show();

			Cpg.Studio.Settings.PlotSettings.Set(graph.Canvas.Graph);
			d_graphs.Add(graph);
			
			graph.Destroyed += delegate {
				d_graphs.Remove(graph);
				
				foreach (Series series in graph.Plots)
				{				
					if (d_initialConditions.ContainsKey(series))
					{
						d_initialConditions.Remove(series);
					}
				}
				
				UpdateAutoScaling();
			};

			graph.MotionNotifyEvent += OnGraphMotionNotify;
			graph.LeaveNotifyEvent += OnGraphLeaveNotify;
			graph.EnterNotifyEvent += OnGraphEnterNotify;
			graph.KeyPressEvent += OnGraphKeyPress;
			graph.ButtonPressEvent += OnGraphButtonPress;
			graph.KeyReleaseEvent += OnGraphKeyRelease;
			
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

			return graph;
		}
		
		[GLib.ConnectBefore]
		private void OnGraphButtonPress(object o, ButtonPressEventArgs args)
		{
			Graph graph = (Graph)o;
			
			if (graph.IsTime)
			{
				return;
			}
			
			Plot.Graph g = graph.Canvas.Graph;
			
			if (args.Event.Button == 1 && args.Event.Type == Gdk.EventType.TwoButtonPress)
			{
				Point pt = new Point(args.Event.X, args.Event.Y);
				Point axis = g.PixelToAxis(pt);
				
				if (g.SnapRulerToAxis && !g.SnapRulerToData)
				{
					int factor = g.SnapRulerToAxisFactor;
					
					if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0)
					{
						factor *= 2;
					}
					
					axis = g.SnapToAxis(axis, factor);
				}
				
				foreach (Series series in graph.Plots)
				{
					if (series.Renderer.Active && !(series.Renderer is Plot.Renderers.Vector))
					{
						d_initialConditions[series] = axis.Copy();
					}
				}

				d_simulation.Resimulate();
			}
		}

		[GLib.ConnectBefore]
		private void OnGraphKeyPress(object o, KeyPressEventArgs args)
		{
			Graph graph = (Graph)o;
			
			if (graph.IsTime)
			{
				return;
			}
			
			if (args.Event.Key == Gdk.Key.Alt_L ||
			    args.Event.Key == Gdk.Key.Alt_R)
			{
				Plot.Graph g = graph.Canvas.Graph;

				g.SnapRulerToAxis = false;
				g.SnapRulerToData = true;
				g.RulerTracksData = true;
			}
		}
		
		[GLib.ConnectBefore]
		private void OnGraphKeyRelease(object o, KeyReleaseEventArgs args)
		{
			Graph graph = (Graph)o;
			
			if (graph.IsTime)
			{
				return;
			}

			if (args.Event.Key == Gdk.Key.Alt_L ||
			    args.Event.Key == Gdk.Key.Alt_R)
			{
				Plot.Graph g = graph.Canvas.Graph;

				g.SnapRulerToAxis = true;
				g.SnapRulerToData = false;
				g.RulerTracksData = false;
			}
		}

		public Graph Add(int row, int col, IEnumerable<Series> series)
		{
			Graph graph = (Graph)d_content[row, col];
			
			foreach (Series s in series)
			{
				if (s.Vector)
				{
					d_vectors.Add(s);
					
					s.Destroyed += delegate(object sender, EventArgs e) {
						d_vectors.Remove((Series)sender);
					};
				}
			}
			
			if (graph != null)
			{
				foreach (Series s in series)
				{
					graph.Add(s);
				}

				return graph;
			}
			
			graph = CreateGraph();
			
			foreach (Series s in series)
			{
				graph.Add(s);
			}
			
			if (graph.OnlyVectors)
			{
				graph.Canvas.Graph.XAxisMode = Plot.AxisMode.Fixed;
				graph.Canvas.Graph.YAxisMode = Plot.AxisMode.Fixed;
				
				graph.Canvas.Graph.UpdateAxis(new Biorob.Math.Range(-1, 1),
				                              new Biorob.Math.Range(-1, 1));
			}

			d_content.Add(graph, row, col);
			
			Plot.Graph g = graph.Canvas.Graph;
			
			if (!graph.IsTime)
			{
				g.AutoMargin.X = graph.Canvas.Graph.AutoMargin.Y;
				g.KeepAspect = true;
				
				g.SnapRulerToAxis = true;
				g.SnapRulerToData = false;
				g.RulerTracksData = false;
			}
			
			List<Graph> graphs = new List<Graph>(SameGraphs(graph));
			
			if (d_linkaxis && !d_autoaxis && graphs.Count > 1)
			{
				Graph first = graphs[0];
				
				IgnoreAxisChange(() => {
					g.UpdateAxis(first.Canvas.Graph.XAxis,
					             first.Canvas.Graph.YAxis);
				});
			}
			else
			{		
				UpdateAutoScaling();
			}
			
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
			
			int r;
			int c;
			
			d_content.IndexOf(graph, out r, out c);
			
			gp.Add(new Gtk.Action("ActionMergeMenu", "Merge", null, null));
			manager.AddUi(manager.NewMergeId(),
			              "/ui/popup/MainPlaceholder",
			              "MergeMenu",
			              "ActionMergeMenu",
			              UIManagerItemType.Menu, false);
			
			MakeMergeMenuItem(graph, gp, manager, r, c, Gtk.Stock.GotoTop, "Merge Up", -1, 0);
			MakeMergeMenuItem(graph, gp, manager, r, c, Gtk.Stock.GotoBottom, "Merge Down", 1, 0);
			MakeMergeMenuItem(graph, gp, manager, r, c, Gtk.Stock.GotoLast, "Merge Right", 0, 1);
			MakeMergeMenuItem(graph, gp, manager, r, c, Gtk.Stock.GotoFirst, "Merge Left", 0, -1);
			
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
			
			graph.Canvas.GrabFocus();

			if (d_linkRulers && graph.Canvas.Graph.ShowRuler)
			{
				DoLinkRulers(graph);
			}
		}
		
		[GLib.ConnectBefore]
		private void OnGraphLeaveNotify(object o, LeaveNotifyEventArgs args)
		{
			if (d_linkRulers)
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
			
			d_actiongroup.Add(new ActionEntry[] {
				new ActionEntry("ActionReset", Gtk.Stock.RevertToSaved, "Reset", null, "Reset Settings", OnResetActivated)
			});
			
			d_uimanager.InsertActionGroup(d_actiongroup, 0);
			d_uimanager.AddUiFromResource("plotting-ui.xml");

			AddAccelGroup(d_uimanager.AccelGroup);
		}
		
		public Point Size
		{
			get
			{
				return new Point(d_content.Columns, d_content.Rows);
			}
			set
			{
				if (value.X <= 0)
				{
					value.X = d_content.Columns;
				}
				
				if (value.Y <= 0)
				{
					value.Y = d_content.Rows;
				}
				
				d_content.Resize((int)value.X, (int)value.Y);
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
		
		private IEnumerable<Graph> TimeGraphs
		{
			get
			{
				foreach (Graph graph in d_graphs)
				{
					if (graph.IsTime)
					{
						yield return graph;
					}
				}
			}
		}
		
		private IEnumerable<Graph> PhaseGraphs
		{
			get
			{
				foreach (Graph graph in d_graphs)
				{
					if (!graph.IsTime)
					{
						yield return graph;
					}
				}
			}
		}
		
		private void UpdateAutoScaling()
		{
			UpdateAutoScaling(TimeGraphs);
			UpdateAutoScaling(PhaseGraphs);
		}
		
		private void UpdateAutoScaling(IEnumerable<Graph> graphs)
		{
			Biorob.Math.Range xrange = new Biorob.Math.Range();
			Biorob.Math.Range yrange = new Biorob.Math.Range();
			
			bool first = true;
			
			foreach (Graph graph in graphs)
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
					foreach (Series series in graph.Plots)
					{
						if (series.Vector)
						{
							continue;
						}

						Biorob.Math.Range xr = series.Renderer.XRange;
						Biorob.Math.Range yr = series.Renderer.YRange;
	
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
			}
			
			IgnoreAxisChange(() => {
				if (d_autoaxis && d_linkaxis)
				{
					foreach (Graph graph in graphs)
					{
						if (!graph.OnlyVectors)
						{
							Plot.Graph g = graph.Canvas.Graph;
							Point margin = g.AutoMargin;
						
							g.UpdateAxis(xrange.Widen(margin.X),
						                 yrange.Widen(margin.Y));
						}
					}
				}
			});
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
		
		private delegate Biorob.Math.Range RangeSelector(Plot.Graph graph);
		
		private void LinkAxes(Graph graph, RangeSelector selector)
		{
			IgnoreAxisChange(() => {
				Biorob.Math.Range nr = selector(graph.Canvas.Graph);
				
				foreach (Graph g in SameGraphs(graph))
				{
					if (g == graph)
					{
						continue;
					}
					
					selector(g.Canvas.Graph).Update(nr);
				}
			});
		}
		
		private void OnResetActivated(object source, EventArgs args)
		{
			foreach (Graph graph in d_graphs)
			{
				Plot.Graph g = graph.Canvas.Graph;

				Cpg.Studio.Settings.PlotSettings.Set(g);
				
				if (!graph.IsTime)
				{
					g.AutoMargin.X = g.AutoMargin.Y;
					g.KeepAspect = true;
					
					g.SnapRulerToAxis = true;
					g.SnapRulerToData = false;
					g.RulerTracksData = false;
				}
			}
			
			UpdateAutoScaling();
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
		
		public bool InitialConditions(Series series, out Point pt)
		{
			return d_initialConditions.TryGetValue(series, out pt);
		}
		
		public void SetInitialConditions(Series series, Point pt)
		{
			if (double.IsNaN(pt.X) || double.IsNaN(pt.Y))
			{
				return;
			}
			
			d_initialConditions[series] = pt;
		}
	}
}
