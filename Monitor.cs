using System;
using Gtk;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace Cpg.Studio
{
	public class Monitor : Gtk.Window
	{
		class Container : EventBox
		{
			private Graph d_graph;
			private Frame d_frame;
			private Button d_close;
			private Button d_merge;
			
			public Container(Graph graph)
			{
				HBox hbox = new HBox(false, 0);
				Add(hbox);
				
				d_graph = graph;
				
				d_frame = new Frame();
				d_frame.ShadowType = ShadowType.EtchedIn;
				
				d_frame.Add(d_graph);
				
				hbox.PackStart(d_frame, true, true, 0);
				d_close = Stock.CloseButton();
				
				VBox vbox = new VBox(false, 3);
				vbox.PackStart(d_close, false, false, 0);
				
				d_merge = Stock.SmallButton(Gtk.Stock.Convert);
				vbox.PackStart(d_merge, false, false, 0);
				
				hbox.PackStart(vbox, false, false, 0);
			}
			
			public Button Close
			{
				get
				{
					return d_close;
				}
			}
			
			public Button Merge
			{
				get
				{
					return d_merge;
				}
			}
			
			public Gdk.Pixbuf CreateDragIcon()
			{
				Gdk.Rectangle a = d_frame.Allocation;
				
				Gdk.Pixbuf pix = Gdk.Pixbuf.FromDrawable(d_frame.GdkWindow, null, 0, 0, 0, 0, a.Width, a.Height);
				pix.ScaleSimple((int)(a.Width * 0.7), (int)(a.Height * 0.7), Gdk.InterpType.Hyper);
				
				return pix;
			}
		}
		
		new class State
		{
			public string Property;
			public Graph Graph;
			public Graph.Container Plot;
			public Cpg.Monitor Monitor;
			public Widget Widget;
			
			public State(string property)
			{
				Property = property;
				Graph = null;
				Plot = null;
				Monitor = null;
				Widget = null;
			}
		}
		
		Simulation d_simulation;
		Grid d_grid;
		
		bool d_linkRulers;
		bool d_linkAxis;
		UIManager d_uimanager;
		HPaned d_hpaned;
		bool d_configured;
		
		ObjectView d_objectView;
		ScrolledWindow d_objectViewScrolledWindow;
		Dictionary<Components.Object, List<State>> d_map;
		Cpg.Studio.Table d_content;
		Range d_range;
		
		const int SampleWidth = 2;
		
		Gtk.Label d_timeLabel;
		Gtk.CheckButton d_showRulers;
		
		public Monitor(Grid grid, Simulation simulation) : base("Monitor")
		{
			d_simulation = simulation;
			
			d_simulation.OnStep += DoStep;
			d_simulation.OnPeriodEnd += DoPeriodEnd;
			
			d_linkRulers = true;
			d_linkAxis = true;
			
			d_map = new Dictionary<Components.Object, List<State>>();
			
			d_grid = grid;
			
			Build();
			
			SetDefaultSize(500, 400);
		}

		private void Build()
		{
			VBox vboxMain = new VBox(false, 0);
			VBox vboxContent = new VBox(false,   3);
			
			BuildMenu();
			
			vboxMain.PackStart(d_uimanager.GetWidget("/menubar"), false, false, 0);
			
			d_hpaned = new HPaned();
			CreateObjectView();
			
			Cpg.Studio.Table table = new Cpg.Studio.Table(1, 1, true);
			table.Expand = Cpg.Studio.Table.ExpandType.Down;
			table.RowSpacing = 1;
			table.ColumnSpacing = 1;
			
			d_content = table;
			
			d_hpaned.Pack1(table, true, true);
			vboxContent.PackStart(d_hpaned);
			
			vboxMain.PackStart(vboxContent, true, true, 0);
			
			Add(vboxMain);
			vboxMain.ShowAll();
			
			HBox hbox = new HBox(false, 6);
			ToggleButton but = Stock.ChainButton();
			
			but.Toggled += delegate(object sender, EventArgs e) {
				d_linkRulers = (sender as ToggleButton).Active;
			};
			
			Gtk.Label lbl = new Gtk.Label("Time: ");
			hbox.PackStart(lbl, false, false, 0);
			d_timeLabel = new Gtk.Label("");
			
			d_timeLabel.Xalign = 0;
			hbox.PackStart(d_timeLabel, true, true, 0);
			
			but.Active = d_linkRulers;
			hbox.PackEnd(but, false, false, 0);
			
			d_showRulers = new CheckButton("Show graph rulers");
			d_showRulers.Active = true;
			hbox.PackEnd(d_showRulers, false, false, 0);
			
			vboxContent.PackEnd(hbox, false, false, 3);
			
			d_showRulers.Toggled += delegate(object sender, EventArgs e) {
				foreach (KeyValuePair<Components.Object, Monitor.State> state in Each())
				{
					state.Value.Graph.ShowRuler = (sender as CheckButton).Active;
				}
			};
			
			hbox.ShowAll();
		}
		
		private void CreateObjectView()
		{
			d_objectView = new ObjectView(d_grid);
			
			ScrolledWindow sw = new ScrolledWindow();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.Add(d_objectView);
			
			sw.ShadowType = ShadowType.EtchedIn;
			sw.ShowAll();
			
			d_hpaned.Pack2(sw, false, false);
			
			d_objectView.Toggled += HandleToggled;
			d_objectView.PropertyAdded += HandlePropertyAdded;
			d_objectViewScrolledWindow = sw;
		}

		void HandlePropertyAdded(ObjectView source, Components.Object obj, string property)
		{
			source.SetActive(obj, property, HasHook(obj, property));
		}

		void HandleToggled(ObjectView source, Components.Object obj, string property)
		{
			if (source.GetActive(obj, property))
			{
				AddHook(obj, property);
			}
			else
			{
				RemoveHook(obj, property);
			}
		}
		
		private bool HasHook(Components.Object obj)
		{
			return d_map.ContainsKey(obj);
		}
		
		private bool HasHook(Components.Object obj, string property)
		{
			if (!d_map.ContainsKey(obj))
				return false;
			
			return d_map[obj].Exists(delegate (Monitor.State state) {
				if (state.Property == property)
					return true;
				else
					return false;
			});
		}
		
		private void InstallObject(Components.Object obj)
		{
			d_map[obj] = new List<State>();
			
			obj.PropertyRemoved += HandlePropertyRemoved;
			obj.PropertyChanged += HandlePropertyChanged;
		}
		
		private string PropertyName(Components.Object obj, string property, bool longname)
		{
			string s = obj.ToString() + "." + property;
			
			if (longname && obj is Components.Link)
			{
				Components.Link link = obj as Components.Link;
				s += " (" + link.From.ToString() + " » " + link.To.ToString();
			}
			
			return s;
		}

		private void UpdateTitle(Components.Object obj)
		{
			if (!HasHook(obj))
				return;
			
			foreach (Monitor.State state in d_map[obj])
			{
				state.Plot.Label = PropertyName(obj, state.Property, true);
			}
		}

		private void HandlePropertyChanged(Components.Object source, string name)
		{
			if (name == "id")
			{
				UpdateTitle(source);
			}
		}
		
		private void DoLinkRulers(Graph graph, Gdk.EventMotion evnt)
		{
			foreach (KeyValuePair<Components.Object, Monitor.State> state in Each())
			{
				if (state.Value.Graph != graph)
				{
					state.Value.Graph.Ruler = graph.Ruler;
				}
			}
		}
		
		private void DoLinkRulersLeave(Graph graph, Gdk.EventCrossing evnt)
		{
			foreach (KeyValuePair<Components.Object, Monitor.State> state in Each())
			{
				state.Value.Graph.HasRuler = false;
			}
		}
		
		private Monitor.State FindHook(Components.Object obj, string property)
		{
			if (!HasHook(obj))
				return null;
			
			foreach (Monitor.State state in d_map[obj])
			{
				if (state.Property == property)
					return state;
			}
			
			return null;
		}
		
		private List<KeyValuePair<Components.Object, Monitor.State>> FindForWidget(Widget w)
		{
			List<KeyValuePair<Components.Object, Monitor.State>> ret = new List<KeyValuePair<Components.Object, Monitor.State>>();
			
			foreach (KeyValuePair<Components.Object, List<Monitor.State>> r in d_map)
			{
				foreach (Monitor.State state in r.Value)
				{
					if (state.Widget == w)
					{
						ret.Add(new KeyValuePair<Components.Object, Monitor.State>(r.Key, state));
					}
				}
			}
			
			return ret;
		}
		
		private void RemoveAllHooks(Components.Object obj, string property)
		{
			Monitor.State state = FindHook(obj, property);
			
			if (state == null)
				return;
			
			List<KeyValuePair<Components.Object, Monitor.State>> all = FindForWidget(state.Widget);
			
			foreach (KeyValuePair<Components.Object, Monitor.State> s in all)
			{
				RemoveHook(s.Key, s.Value.Property);
			}
		}
		
		private void MergeWith(Components.Object obj, string property, Widget widget)
		{
			Monitor.State state = FindHook(obj, property);
			
			if (state == null)
				return;
			
			List<KeyValuePair<Components.Object, Monitor.State>> all = FindForWidget(state.Widget);
			Monitor.State toitem = FindForWidget(widget)[0].Value;
			
			foreach (KeyValuePair<Components.Object, Monitor.State> s in all)
			{
				Monitor.State s2 = s.Value;
				
				s2.Plot = toitem.Graph.Add(s2.Plot.Data.ToArray(), s2.Plot.Label, s2.Plot.Color);
				s2.Graph = toitem.Graph;
				s2.Widget = widget;
			}
			
			UpdateTitle(obj);
			widget.Destroy();
		}
		
		private void DoMerge(Components.Object obj, string property, Point direction)
		{
			Monitor.State state = FindHook(obj, property);
			
			if (state == null)
				return;
			
			Gtk.Widget to = d_content.Find(state.Widget, direction.X, direction.Y);
			
			if (to == null)
				return;
			
			MergeWith(obj, property, to);
		}
		
		private void MakeMergeMenuItem(Components.Object obj, string property, Menu menu, Point pos, string stockid, string label, Point dir)
		{
			if (pos.X + dir.X < 0 || pos.X + dir.X >= d_content.NColumns)
				return;
			
			if (pos.Y + dir.Y < 0 || pos.Y + dir.Y >= d_content.NRows)
				return;
			
			ImageMenuItem item = new ImageMenuItem(label);
			item.Image = new Gtk.Image(stockid, IconSize.Menu);
			
			item.Activated += delegate(object sender, EventArgs e) {
				DoMerge(obj, property, dir);
			};
			
			menu.Append(item);
			item.Show();
		}
		
		private void ShowMergeMenu(Components.Object obj, string property)
		{
			Monitor.State state = FindHook(obj, property);
			Menu menu = new Menu();
			
			Point pos = d_content.GetPosition(state.Widget);
			
			MakeMergeMenuItem(obj, property, menu, pos, Gtk.Stock.GotoTop, "Merge Up", new Point(0, -1));
			MakeMergeMenuItem(obj, property, menu, pos, Gtk.Stock.GotoBottom, "Merge Down", new Point(0, 1));
			MakeMergeMenuItem(obj, property, menu, pos, Gtk.Stock.GotoLast, "Merge Right", new Point(1, 0));
			MakeMergeMenuItem(obj, property, menu, pos, Gtk.Stock.GotoFirst, "Merge Left", new Point(-1, 0));
			
			if (menu.Children.Length != 0)
			{
				menu.Popup(null, null, null, 1, 0);
			}
		}
		
		private void UpdateTimeLabel(Graph graph, Gdk.EventMotion evnt)
		{
			if (d_simulation.Range != null)
			{
				double perc = evnt.X / (graph.Allocation.Width - 1);
				
				Range r = d_simulation.Range;
				double t = r.From + (r.To - r.From) * perc;
				d_timeLabel.Text = t.ToString("F3");
			}
		}
		
		private bool AddHookReal(Components.Object obj, string property, Monitor.State state)
		{
			Graph graph = new Graph(SampleWidth, new Graph.Range(-3, 3));
			graph.SetSizeRequest(-1, 50);
			graph.ShowRuler = d_showRulers.Active;
			
			if (d_linkAxis)
			{
				foreach (KeyValuePair<Components.Object, Monitor.State> s in Each())
				{
					graph.YAxis = s.Value.Graph.YAxis;
					break;
				}
			}
			
			Container cont = new Container(graph);
			
			cont.Close.Clicked += delegate(object sender, EventArgs e) {
				RemoveAllHooks(obj, property);
			};
			
			cont.Merge.Clicked += delegate(object sender, EventArgs e) {
				ShowMergeMenu(obj, property);
			};
			
			cont.ShowAll();
			d_content.Add(cont);
			
			graph.MotionNotifyEvent += delegate(object o, MotionNotifyEventArgs args) {
				if (d_linkRulers && graph.ShowRuler)
					DoLinkRulers(graph, args.Event);
				
				UpdateTimeLabel(graph, args.Event);
			};
			
			graph.LeaveNotifyEvent += delegate(object o, LeaveNotifyEventArgs args) {
				if (d_linkRulers && graph.ShowRuler)
					DoLinkRulersLeave(graph, args.Event);
			};
			
			state.Graph = graph;
			state.Widget = cont;
			state.Monitor = new Cpg.Monitor(d_simulation.Network, (obj as Components.Simulated).Object, property);
			state.Plot = graph.Add(new double[] {}, PropertyName(obj, property, true));

			d_map[obj].Add(state);
			d_simulation.Resimulate();			

			return true;
		}

		void HandlePropertyRemoved(Components.Object source, string name)
		{
			RemoveHook(source, name);
		}
		
		public bool AddHook(Components.Object obj, string property)
		{
			if (!(obj is Components.Simulated))
				return false;

			if (!HasHook(obj))
				InstallObject(obj);
			
			if (HasHook(obj, property))
				return false;
			
			if (!AddHookReal(obj, property, new Monitor.State(property)))
				return false;
			
			if (d_objectView != null)
				d_objectView.SetActive(obj, property, true);
			
			return true;
		}
		
		private bool RemoveHookReal(Components.Object obj, Monitor.State state)
		{
			if (state.Graph != null)
			{
				if (state.Graph.Count > 1)
				{
					state.Graph.Remove(state.Plot);
				}
				else
				{				
					state.Widget.Destroy();
				}
			}
			
			state.Monitor.Dispose();
			return true;
		}
		
		private void Disconnect(Components.Object obj)
		{
			obj.PropertyRemoved -= HandlePropertyRemoved;
			obj.PropertyChanged -= HandlePropertyChanged;
		}
		
		private void RemoveHook(Components.Object obj, string property)
		{
			if (!HasHook(obj, property))
				return;
			
			d_map[obj].RemoveAll(delegate (Monitor.State state) {
				return (property == null || property == state.Property) && RemoveHookReal(obj, state);
			});
			
			if (d_map[obj].Count == 0)
			{
				Disconnect(obj);
				d_map.Remove(obj);
			}
			
			if (d_objectView != null)
				d_objectView.SetActive(obj, property, false);
		}
		
		private void BuildMenu()
		{
			d_uimanager = new UIManager();
			ActionGroup ag = new ActionGroup("NormalActions");
			
			ag.Add(new ActionEntry[] {
				new ActionEntry("FileMenuAction", null, "_File", null, null, null),
				new ActionEntry("CloseAction", Gtk.Stock.Close, null, null, null, DoClose),
				new ActionEntry("ViewMenuAction", null, "_View", null, null, null),
			});
			
			ag.Add(new ToggleActionEntry[] {
				new ToggleActionEntry("ViewSelectAction", 
				                       null, 
				                       "Show _object list", 
				                       "<Control>o", 
				                       "Show/hide property selection",
				                       DoSelectToggled,
				                       true)
			});
			
			d_uimanager.InsertActionGroup(ag, 0);
			
			ag = new ActionGroup("MonitorActions");
			
			ag.Add(new ActionEntry[] {
				new ActionEntry("AutoAxisAction", 
				                null, 
				                "Auto scale axis", 
				                "<Control>r", 
				                "Automatically scale axis for data", 
				                DoAutoAxis)
			});
			
			ag.Add(new ToggleActionEntry[] {
				new ToggleActionEntry("LinkedAxisAction",
				                      null,
				                      "Link axis scales",
				                      "<Control>l",
				                      "Link graph axis scales",
				                      DoLinkAxis,
				                      d_linkAxis)
			});
			
			d_uimanager.InsertActionGroup(ag, 0);

			d_uimanager.AddUiFromResource("monitor-ui.xml");

			uint mid = d_uimanager.NewMergeId();

			d_uimanager.AddUi(mid, "/menubar/View/ViewBottom", "AutoAxis", "AutoAxisAction", UIManagerItemType.Menuitem, false);
			d_uimanager.AddUi(mid, "/menubar/View/ViewBottom", "LinkedAxis", "LinkedAxisAction", UIManagerItemType.Menuitem, false);
			
			AddAccelGroup(d_uimanager.AccelGroup);
		}

		void DoClose(object source, EventArgs args)
		{
			Destroy();
		}
		
		private void LinkAxis(bool active)
		{
			d_linkAxis = active;
			
			if (d_linkAxis)
			{
				Graph.Range range = new Graph.Range(0, 0);
				bool isset = false;
				
				foreach (KeyValuePair<Components.Object, Monitor.State> state in Each())
				{
					Graph.Range yaxis = state.Value.Graph.YAxis;
					
					if (!isset || yaxis.Min < range.Min)
						range.Min = yaxis.Min;
					if (!isset ||yaxis.Max > range.Max)
						range.Max = yaxis.Max;
					
					isset = true;
				}
				
				foreach (KeyValuePair<Components.Object, Monitor.State> state in Each())
				{
					state.Value.Graph.YAxis = range;
				}
			}
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
					value.X = (int)d_content.NColumns;
				if (value.Y <= 0)
					value.Y = (int)d_content.NRows;
				
				d_content.Resize((uint)value.X, (uint)value.Y);
			}
		}
		
		void DoAutoAxis(object source, EventArgs args)
		{
			foreach (KeyValuePair<Components.Object, Monitor.State> state in Each())
			{
				state.Value.Graph.AutoAxis();
			}
			
			LinkAxis(d_linkAxis);
		}
		
		private IEnumerable<KeyValuePair<Components.Object, Monitor.State>> Each()
		{
			foreach (KeyValuePair<Components.Object, List<Monitor.State>> pair in d_map)
			{
				foreach (Monitor.State state in pair.Value)
				{
					yield return new KeyValuePair<Components.Object, Monitor.State>(pair.Key, state);
				}
			}
		}
		
		void DoLinkAxis(object source, EventArgs args)
		{
			bool active = (source as ToggleAction).Active;
			
			LinkAxis(active);
		}
		
		void DoSelectToggled(object source, EventArgs args)
		{
			d_objectViewScrolledWindow.Visible = (source as Gtk.ToggleAction).Active;
			d_hpaned.QueueDraw();
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape)
			{
				Destroy();
				return true;
			}
			
			return base.OnKeyPressEvent(evnt);
		}
		
		protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
		{
			bool ret = base.OnConfigureEvent(evnt);
			
			if (d_configured)
				return ret;
			
			d_hpaned.Position = Allocation.Width - 150;
			d_configured = true;
			
			return ret;
		}
		
		void DoStep(object source, double timestep)
		{
			// TODO
		}
		
		private void SetMonitorData(Components.Object obj, Monitor.State state)
		{
			int numpix = state.Graph.Allocation.Width;
			
			// Resample data to be on thingie
			double rstep = (d_range.To - d_range.From) / (double)numpix;
			
			double[] to = new double[numpix / SampleWidth];
			
			for (int i = 0; i < to.Length; ++i)
			{
				to[i] = d_range.From + (i * rstep * SampleWidth);
			}
			
			double[] data = state.Monitor.GetDataResampled(to);
			
			for (int i = 0; i < data.Length; ++i)
			{
				if (double.IsInfinity(data[i]) || double.IsNaN(data[i]))
					data[i] = 0;
			}
			
			int mindw = 10;
			double d = d_range.To - d_range.From;
			double ds = d_range.Step;
			double dw = numpix / (d / ds);
			
			while (dw < mindw)
			{
				ds = ds * 10;
				dw = numpix / (d / ds);
			}

			state.Plot.SetData(data);
			state.Graph.SetTicks((int)dw, d_range.From);			
		}
		
		private void DoPeriodEnd(object source, Simulation.Args args)
		{
			d_range = d_simulation.Range;
			
			foreach (KeyValuePair<Components.Object, Monitor.State> state in Each())
			{
				SetMonitorData(state.Key, state.Value);
			}			
		}
		
		public void SetMonitorPosition(Components.Object obj, string property, Point pt)
		{
			Monitor.State state = FindHook(obj, property);
			
			if (state == null)
				return;
			
			Widget w = d_content.At(pt.X, pt.Y);
			
			if (w != null && w != d_content.RealChild(state.Widget))
			{
				MergeWith(obj, property, w);
			}
			else if (w != null)
			{
				d_content.PositionChild(w, (uint)pt.X, (uint)pt.Y);
			}
		}
	}
}
