using System;
using Gtk;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cpg.Studio.Widgets
{
	public class Window : Gtk.Window
	{
		private ActionGroup d_normalGroup;
		private ActionGroup d_selectionGroup;
		private Pathbar d_pathbar;
		private VBox d_vboxContents;
		private VPaned d_vpaned;
		private Grid d_grid;
		private Entry d_periodEntry;
		private Statusbar d_statusbar;
		private ToggleButton d_simulateButton;
		private HBox d_simulateButtons;
		private Widget d_toolbar;
		private PropertyView d_propertyView;
		private MessageArea d_messageArea;
		private uint d_statusTimeout;
		private uint d_popupMergeId;
		private UIManager d_uimanager;
		private ActionGroup d_popupActionGroup;
		private Dictionary<Wrappers.Wrapper, Dialogs.Property> d_propertyEditors;
		private Serialization.Project d_project;
		private Monitor d_monitor;
		private Simulation d_simulation;
		private string d_prevOpen;
		private Dialogs.Functions d_functionsDialog;
		private ListStore d_integratorStore;
		private ComboBox d_integratorCombo;
		private Widget d_menubar;
		
		private bool d_modified;
		
		private Undo.Manager d_undoManager;
		private Actions d_actions;
		private WindowGroup d_windowGroup;
		private TemplatesMenu d_stateTemplatesMenu;
		private TemplatesMenu d_linkTemplatesMenu;
		
		private Wrappers.Wrapper d_templatePopupObject;
		private Wrappers.Wrapper d_templatePopupTemplate;
		private DateTime d_runElapsed;
		private Progress d_progress;
		private bool d_checkProgress;

		public Window() : base (Gtk.WindowType.Toplevel)
		{
			d_project = new Serialization.Project();

			d_windowGroup = new WindowGroup();
			d_windowGroup.AddWindow(this);
			
			d_project.Network.WrappedObject.CompileError += OnCompileError;

			d_project.Network.WrappedObject.AddNotification("integrator", delegate (object sender, GLib.NotifyArgs args) {
				UpdateCurrentIntegrator();
			});

			d_simulation = new Simulation(d_project.Network);
			
			d_simulation.OnBegin += HandleSimulationBegin;
			d_simulation.OnEnd += HandleSimulationEnd;

			d_undoManager = new Undo.Manager();
			d_undoManager.OnChanged += delegate (object source) {
				UpdateUndoState();	
			};
			
			d_undoManager.OnModified += delegate (object source) {
				UpdateModified();
			};
			
			d_actions = new Actions(d_undoManager);
			
			Build();
			ShowAll();
			
			Clear();
			
			UpdateUndoState();
			UpdateTitle();
			
			d_propertyEditors = new Dictionary<Wrappers.Wrapper, Dialogs.Property>();
		}

		private void HandleSimulationBegin(object o, BeginArgs args)
		{
			d_simulation.OnStepped += HandleSimulationStepped;
			d_runElapsed = DateTime.Now;
			d_checkProgress = true;
			
			d_grid.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
			
			UpdateSensitivity();
		}
		
		private void HandleSimulationEnd(object o, EventArgs args)
		{
			UpdateSensitivity();
			
			d_grid.GdkWindow.Cursor = null;
			
			if (d_progress != null)
			{
				d_progress.Dispose();
				d_progress = null;
			}
		}

		private void HandleSimulationStepped(object o, SteppedArgs args)
		{
			if (d_checkProgress)
			{
				double realTime = (DateTime.Now - d_runElapsed).TotalSeconds;

				if (realTime >= 0.5)
				{
					double span = d_simulation.Range.To - d_simulation.Range.From;
					double todo = span - args.Time;
					
					double estimated = todo / (args.Time - d_simulation.Range.From) * realTime;
					
					if (estimated > 1)
					{
						d_progress = new Progress(d_grid);
					}
					
					d_checkProgress = false;
				}
			}
			
			if (d_progress != null)
			{
				d_progress.Update((args.Time - d_simulation.Range.From) / (d_simulation.Range.To - d_simulation.Range.From));
			}
		}

		private void UpdateUndoState()
		{
			d_normalGroup.GetAction("UndoAction").Sensitive = d_undoManager.CanUndo;
			d_normalGroup.GetAction("RedoAction").Sensitive = d_undoManager.CanRedo;
		}
		
		private void CreateInsertMenu(string path)
		{
			Gtk.MenuItem item = (Gtk.MenuItem)d_uimanager.GetWidget(path);
			Gtk.Menu menu = (Gtk.Menu)item.Submenu;

			Gtk.MenuItem stateItem = new Gtk.MenuItem("State");
			Gtk.Menu stateMenu = new Gtk.Menu();
			stateItem.Submenu = stateMenu;
			
			Gtk.MenuItem defaultStateItem = new Gtk.MenuItem("Default");
			defaultStateItem.Activated += OnAddStateActivated;

			stateMenu.Append(defaultStateItem);
			stateMenu.Append(new Gtk.SeparatorMenuItem());

			d_stateTemplatesMenu = new TemplatesMenu(stateMenu, Network.TemplateGroup, false, FilterStates);
			d_stateTemplatesMenu.Activated += HandleStateTemplatesMenuActivated;

			stateItem.ShowAll();
			menu.Append(stateItem);
			
			Gtk.MenuItem linkItem = new Gtk.MenuItem("Link");
			Gtk.Menu linkMenu = new Gtk.Menu();
			linkItem.Submenu = linkMenu;
			
			Gtk.MenuItem defaultLinkItem = new Gtk.MenuItem("Default");
			defaultLinkItem.Activated += OnAddLinkActivated;

			linkMenu.Append(defaultLinkItem);
			linkMenu.Append(new Gtk.SeparatorMenuItem());

			d_linkTemplatesMenu = new TemplatesMenu(linkMenu, Network.TemplateGroup, false, FilterLinks);
			d_linkTemplatesMenu.Activated += HandleLinkTemplatesMenuActivated;

			linkItem.ShowAll();
			menu.Append(linkItem);
		}

		private void HandleStateTemplatesMenuActivated(object source, Wrappers.Wrapper template)
		{
			Wrappers.Wrapper[] sel = d_grid.Selection;
				
			if (sel.Length == 0 || Array.TrueForAll<Wrappers.Wrapper>(sel, item => item is Wrappers.Link))
			{
				int[] center = d_grid.Center;
				HandleError(delegate () {
					d_actions.AddObject(d_grid.ActiveGroup, template.CopyAsTemplate(), center[0], center[1]);
				}, "An error occurred while adding an object from a template");
			}
			else
			{
				HandleError(delegate () {
					d_actions.ApplyTemplate(template, sel);
				}, "An error occurred while applying the template");
			}
		}
		
		private void HandleLinkTemplatesMenuActivated(object source, Wrappers.Wrapper template)
		{
			Wrappers.Wrapper[] sel = d_grid.Selection;
				
			if (sel.Length == 0 || !Array.TrueForAll<Wrappers.Wrapper>(sel, item => item is Wrappers.Link))
			{
				HandleError(delegate () {
					int[] center = d_grid.Center;
					d_actions.AddLink(d_grid.ActiveGroup, (Wrappers.Link)template, d_grid.Selection, center[0], center[1]);
				}, "An error occurred while adding a link from a template");
			}
			else
			{
				HandleError(delegate () {
					d_actions.ApplyTemplate(template, sel);
				}, "An error occurred while applying the template");
			}
		}
		
		private bool FilterStates(Wrappers.Wrapper wrapper)
		{
			return !(wrapper is Wrappers.Link);
		}
		
		private bool FilterLinks(Wrappers.Wrapper wrapper)
		{
			return wrapper is Wrappers.Link;
		}
		
		private void CreateTemplatesTool()
		{
			Toolbar tb = (Toolbar)d_uimanager.GetWidget("/toolbar");
			SeparatorToolItem sep = new SeparatorToolItem();
			
			sep.Show();
			tb.Add(sep);
			
			MenuToolButton button = new MenuToolButton(Stock.State);
			button.Clicked += OnAddStateActivated;
			button.Show();
			
			TemplatesMenu temp = new TemplatesMenu(Network.TemplateGroup, false, FilterStates);
			button.Menu = temp.Menu;
			temp.Activated += HandleStateTemplatesMenuActivated;
			temp.Menu.ShowAll();

			tb.Add(button);
			
			button = new MenuToolButton(Stock.Link);
			button.Clicked += OnAddLinkActivated;
			button.Show();
			
			temp = new TemplatesMenu(Network.TemplateGroup, false, FilterLinks);
			temp.Activated += HandleLinkTemplatesMenuActivated;
			button.Menu = temp.Menu;
			temp.Menu.ShowAll();

			tb.Add(button);
		}

		private void Build()
		{
			SetDefaultSize(700, 600);
			
			d_uimanager = new UIManager();
			d_normalGroup = new ActionGroup("NormalActions");

			d_normalGroup.Add(new ActionEntry[] {
				new ActionEntry("FileMenuAction", null, "_File", null, null, null),
				new ActionEntry("AddStateMenuAction", null, null, null, null, null),
				new ActionEntry("NewAction", Gtk.Stock.New, null, "<Control>N", "New CPG network", OnFileNew),
				new ActionEntry("OpenAction", Gtk.Stock.Open, null, "<Control>O", "Open CPG network", OnOpenActivated),
				new ActionEntry("RevertAction", Gtk.Stock.RevertToSaved, null, null, "Revert changes", OnRevertActivated),
				new ActionEntry("SaveAction", Gtk.Stock.Save, null, "<Control>S", "Save CPG network", OnSaveActivated),
				new ActionEntry("SaveAsAction", Gtk.Stock.SaveAs, null, "<Control><Shift>S", "Save CPG network", OnSaveAsActivated),

				new ActionEntry("ImportAction", null, "Import", "<Control>i", "Import CPG network objects", OnImportActivated),
				new ActionEntry("ExportAction", null, "Export", "<Control>e", "Export CPG network objects", null),

				new ActionEntry("QuitAction", Gtk.Stock.Quit, null, "<Control>Q", "Quit", OnQuitActivated),

				new ActionEntry("EditMenuAction", null, "_Edit", null, null, null),
				new ActionEntry("UndoAction", Gtk.Stock.Undo, null, "<control>Z", "Undo last action", OnUndoActivated),
				new ActionEntry("RedoAction", Gtk.Stock.Redo, null, "<control><shift>Z", "Redo last action", OnRedoActivated),
				new ActionEntry("PasteAction", Gtk.Stock.Paste, null, "<Control>V", "Paste objects", OnPasteActivated),
				new ActionEntry("GroupAction", Stock.Group, "Group", "<Control>G", "Group objects", OnGroupActivated),
				new ActionEntry("UngroupAction", Stock.Ungroup, "Ungroup", "<Control><Shift>G", "Ungroup object", OnUngroupActivated),
				new ActionEntry("EditGlobalsAction", null, "Globals", "<Control>H", "Edit the network globals", OnEditGlobalsActivated),
				new ActionEntry("EditFunctionsAction", null, "Functions", "<Control>F", "Edit the network custom functions", OnEditFunctionsActivated),

				new ActionEntry("SimulateMenuAction", null, "_Simulate", null, null, null),
				new ActionEntry("StepAction", Gtk.Stock.MediaNext, "Step", "<Control>t", "Execute one simulation step", OnStepActivated),
				new ActionEntry("SimulateAction", Gtk.Stock.MediaForward, "Period", "<Control>p", "(Re)Simulate period", OnSimulateActivated),
				
				new ActionEntry("ViewMenuAction", null, "_View", null, null, null),
				new ActionEntry("CenterAction", Gtk.Stock.JustifyCenter, null, "<Control>h", "Center view", OnCenterViewActivated),
				new ActionEntry("InsertMenuAction", null, "_Apply", null, null, null),
				new ActionEntry("ZoomDefaultAction", Gtk.Stock.Zoom100, null, "<Control>1", null, OnZoomDefaultActivated),
				new ActionEntry("ZoomInAction", Gtk.Stock.ZoomIn, null, "<Control>plus", null, OnZoomInActivated),
				new ActionEntry("ZoomOutAction", Gtk.Stock.ZoomOut, null, "<Control>minus", null, OnZoomOutActivated),

				new ActionEntry("MonitorMenuAction", null, "Monitor", null, null, null),
				new ActionEntry("ControlMenuAction", null, "Control", null, null, null),
				new ActionEntry("PropertiesAction", null, "Properties", null, null, OnPropertiesActivated),
				new ActionEntry("EditGroupAction", null, "Edit group", null, null, OnEditGroupActivated),
				
				new ActionEntry("EditTemplateAction", null, "Edit template", null, null, OnEditTemplateActivated),
				new ActionEntry("RemoveTemplateAction", null, "Unapply template", null, null, OnRemoveTemplateActivated),
			});
			
			d_normalGroup.Add(new ToggleActionEntry[] {
				new ToggleActionEntry("ViewPropertyEditorAction", Gtk.Stock.Properties, "Property Editor", "<Control>F9", "Show/Hide property editor pane", OnViewPropertyEditorActivated, true),
				new ToggleActionEntry("ViewToolbarAction", null, "Toolbar", null, "Show/Hide toolbar", OnViewToolbarActivated, true),
				new ToggleActionEntry("ViewPathbarAction", null, "Pathbar", null, "Show/Hide pathbar", OnViewPathbarActivated, true),
				new ToggleActionEntry("ViewSimulateButtonsAction", null, "Simulate Buttons", null, "Show/Hide simulate buttons", OnViewSimulateButtonsActivated, true),
				new ToggleActionEntry("ViewStatusbarAction", null, "Statusbar", null, "Show/Hide statusbar", OnViewStatusbarActivated, true),
				new ToggleActionEntry("ViewMonitorAction", null, "Monitor", "<Control>m", "Show/Hide monitor window", OnToggleMonitorActivated, false),
				new ToggleActionEntry("ViewControlAction", null, "Control", "<Control>k", "Show/Hide control window", OnToggleControlActivated, false)		
			});
				
			d_uimanager.InsertActionGroup(d_normalGroup, 0);
			
			d_selectionGroup = new ActionGroup("SelectionActions");
			d_selectionGroup.Add(new ActionEntry[] {
				new ActionEntry("CutAction", Gtk.Stock.Cut, null, "<Control>X", "Cut objects", OnCutActivated),
				new ActionEntry("CopyAction", Gtk.Stock.Copy, null, "<Control>C", "Copy objects", OnCopyActivated),
				new ActionEntry("DeleteAction", Gtk.Stock.Delete, null, null, "Delete object", OnDeleteActivated)			
			});
			
			d_uimanager.InsertActionGroup(d_selectionGroup, 0);
			d_uimanager.AddUiFromResource("ui.xml");
			
			AddAccelGroup(d_uimanager.AccelGroup);
			
			VBox vbox = new VBox(false, 0);
			
			CreateInsertMenu("/menubar/InsertMenu");
			CreateInsertMenu("/GridPopup/InsertMenu");
			CreateTemplatesTool();

			d_menubar = d_uimanager.GetWidget("/menubar");
			vbox.PackStart(d_menubar, false, false, 0);
			
			d_toolbar = d_uimanager.GetWidget("/toolbar");
			vbox.PackStart(d_toolbar, false, false, 0);

			d_uimanager.EnsureUpdate();

			d_pathbar = new Pathbar(Network, Network.TemplateGroup);
			d_pathbar.BorderWidth = 1;

			vbox.PackStart(d_pathbar, false, false, 0);
			
			d_vboxContents = new VBox(false, 3);
			vbox.PackStart(d_vboxContents, true, true, 0);
			
			d_grid = new Grid(Network, d_actions);
			d_vpaned = new VPaned();
			d_vpaned.Position = 250;
			d_vpaned.Pack1(d_grid, true, true);
			
			d_vboxContents.PackStart(d_vpaned, true, true, 0);
			
			d_periodEntry = new Entry();
			d_periodEntry.SetSizeRequest(75, -1);
			d_periodEntry.Activated += new EventHandler(OnSimulationRunPeriod);

			d_simulateButtons = new HBox(false, 3);
			d_simulateButtons.BorderWidth = 3;
			BuildButtonBar(d_simulateButtons);
			
			d_vboxContents.PackStart(d_simulateButtons, false, false, 0);
			
			d_statusbar = new Statusbar();
			d_statusbar.Show();
			vbox.PackStart(d_statusbar, false, false, 0);

			OnViewPropertyEditorActivated(d_normalGroup.GetAction("ViewPropertyEditorAction"), new EventArgs());
			OnViewToolbarActivated(d_normalGroup.GetAction("ViewToolbarAction"), new EventArgs());
			OnViewPathbarActivated(d_normalGroup.GetAction("ViewPathbarAction"), new EventArgs());
			OnViewSimulateButtonsActivated(d_normalGroup.GetAction("ViewSimulateButtonsAction"), new EventArgs());
			OnViewStatusbarActivated(d_normalGroup.GetAction("ViewStatusbarAction"), new EventArgs());

			Add(vbox);

			d_pathbar.Update(d_grid.ActiveGroup);

			d_grid.Activated += DoObjectActivated;
			d_grid.Popup += DoPopup;

			d_grid.SelectionChanged += DoSelectionChanged;
			d_grid.FocusInEvent += DoFocusInEvent;
			d_grid.FocusOutEvent += DoFocusOutEvent;
			d_grid.Error += DoError;
			d_grid.ActiveGroupChanged += DoActiveGroupChanged;
			
			d_pathbar.Activated += HandlePathbarActivated;
		}

		private void HandlePathbarActivated(object source, Wrappers.Group grp)
		{
			d_grid.ActiveGroup = grp;
		}
		
		private void DoActiveGroupChanged(object source, Wrappers.Wrapper prev)
		{
			d_pathbar.Update(d_grid.ActiveGroup);
		}
		
		private void UpdateCurrentIntegrator()
		{
			d_integratorStore.Foreach(delegate (TreeModel model, TreePath path, TreeIter piter) {
				Cpg.Integrator intgr = (Cpg.Integrator)model.GetValue(piter, 0);
				
				if (intgr.Id == Network.Integrator.Id)
				{
					d_integratorCombo.Changed -= DoIntegratorChanged;
					d_integratorCombo.SetActiveIter(piter);
					d_integratorCombo.Changed += DoIntegratorChanged;
					
					return true;
				}
				
				return false;
			});
		}
		
		private ComboBox CreateIntegrators()
		{
			ListStore store = new ListStore(typeof(Cpg.Integrator));
			CellRendererText renderer = new CellRendererText();
			ComboBox combo = new ComboBox(store);
			
			combo.PackStart(renderer, true);
			
			combo.SetCellDataFunc(renderer, delegate (CellLayout layout, CellRenderer rd, TreeModel model, TreeIter piter) {
				Cpg.Integrator intgr = (Cpg.Integrator)model.GetValue(piter, 0);
				
				if (intgr != null)
				{
					((CellRendererText)rd).Text = intgr.Name;
				}
			});
			
			Integrator[] integrators = Cpg.Integrators.Create();
			
			foreach (Integrator integrator in integrators)
			{
				if (integrator.Id == "stub")
				{
					continue;
				}

				TreeIter piter = store.AppendValues(integrator);
					
				if (integrator.Id == Network.Integrator.Id)
				{
					combo.SetActiveIter(piter);
				}
			}
			
			store.SetSortFunc(0, delegate (TreeModel model, TreeIter a, TreeIter b) {
				Cpg.Integrator i1 = (Cpg.Integrator)model.GetValue(a, 0);
				Cpg.Integrator i2 = (Cpg.Integrator)model.GetValue(b, 0);
				
				return i1.Name.CompareTo(i2.Name);
			});
			
			store.SetSortColumnId(0, SortType.Ascending);
			
			d_integratorStore = store;
			d_integratorCombo = combo;
			
			combo.Changed += DoIntegratorChanged;
			return combo;
		}
		
		private void DoIntegratorChanged(object sender, EventArgs args)
		{
			ComboBox combo = (ComboBox)sender;
			TreeIter piter;
			
			combo.GetActiveIter(out piter);
			Cpg.Integrator integrator = (Cpg.Integrator)combo.Model.GetValue(piter, 0);

			if (integrator != Network.Integrator)
			{
				d_actions.Do(new Undo.ModifyIntegrator(Network, integrator));
				d_modified = true;
			
				UpdateTitle();
			}
		}
		
		private void BuildButtonBar(HBox hbox)
		{
			hbox.PackStart(new Label("Period:"), false, false, 0);
			hbox.PackStart(d_periodEntry, false, false, 0);
			hbox.PackStart(new Label("(s)"), false, false, 0);
			
			Button but = new Button();
			but.Image = new Image(Gtk.Stock.MediaForward, IconSize.Button);
			but.Label = "Simulate period";
			but.Clicked += new EventHandler(OnSimulationRunPeriod);
			but.TooltipText = "Run new simulation for specified period of time";

			hbox.PackStart(but, false, false, 0);
			
			ComboBox combo = CreateIntegrators();
			combo.Show();
			hbox.PackStart(combo, false, false, 0);
		
			but = new Button();
			but.Image = new Image(Gtk.Stock.MediaNext, IconSize.Button);
			but.Label = "Step";
			but.Clicked += new EventHandler(OnSimulationStep);
			but.TooltipText = "Run one single simulation step";
			
			hbox.PackEnd(but, false, false, 0);

			d_simulateButton = new ToggleButton();
			d_simulateButton.Image = new Image(Gtk.Stock.MediaPlay, IconSize.Button);
			d_simulateButton.Label = "Simulate";
			d_simulateButton.Toggled += new EventHandler(OnSimulationRun);
			d_simulateButton.TooltipText = "Run interactive simulation";

			hbox.PackEnd(d_simulateButton, false, false, 0);

			but = new Button();
			but.Image = new Image(Gtk.Stock.Clear, IconSize.Button);
			but.Label = "Reset";
			but.Clicked += new EventHandler(OnSimulationReset);
			but.TooltipText = "Reset running simulation";
			hbox.PackEnd(but, false, false, 0);
		}

		private void Clear()
		{
			Network.Clear();
			d_grid.Clear();
			
			if (d_messageArea != null)
			{
				d_messageArea.Destroy();
			}
			
			if (d_monitor != null)
			{
				d_monitor.Destroy();
			}
			
			if (d_propertyEditors != null)
			{
				foreach (Dialogs.Property dlg in d_propertyEditors.Values)
				{
					dlg.Destroy();
				}
			}
			
			d_periodEntry.Text = "0:0.01:1";
			d_grid.GrabFocus();
			
			d_modified = false;
			d_undoManager.Clear();
		}
		
		private void UpdateModified()
		{
			if (d_modified == d_undoManager.IsUnmodified)
			{
				d_modified = !d_modified;
				UpdateTitle();
			}
		}
		
		private void UpdateTitle()
		{
			string extra = d_modified ? "*" : "";
			
			if (!String.IsNullOrEmpty(d_project.Filename))
			{
				Title = extra + System.IO.Path.GetFileName(d_project.Filename) + " - CPG Studio";
			}
			else
			{
				Title = extra + "New Network - CPG Studio";
			}
		}
		
		private void UpdateSensitivity()
		{
			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>(d_grid.Selection);
			
			d_selectionGroup.Sensitive = !d_simulation.Running && objects.Count > 0 && d_grid.HasFocus;
			
			bool singleobj = objects.Count == 1;
			bool singlegroup = singleobj && objects[0] is Wrappers.Group;
			int anygroup = objects.FindAll(delegate (Wrappers.Wrapper obj) { return obj is Wrappers.Group; }).Count;
			
			Action ungroup = d_normalGroup.GetAction("UngroupAction");
			ungroup.Sensitive = !d_simulation.Running && anygroup > 0;
			
			if (anygroup > 1)
			{
				ungroup.Label = "Ungroup All";
			}
			else
			{
				ungroup.Label = "Ungroup";
			}
			
			d_normalGroup.GetAction("GroupAction").Sensitive = !d_simulation.Running && objects.Count > 1 && objects.Find(delegate (Wrappers.Wrapper obj) { return !(obj is Wrappers.Link); }) != null;
			d_normalGroup.GetAction("EditGroupAction").Sensitive = !d_simulation.Running && singlegroup;
			
			d_normalGroup.GetAction("PropertiesAction").Sensitive = !d_simulation.Running && singleobj;
			d_normalGroup.GetAction("PasteAction").Sensitive = !d_simulation.Running && d_grid.HasFocus && !Studio.Clipboard.Internal.Empty;
			
			// Disable control for now
			d_normalGroup.GetAction("ControlMenuAction").Visible = false;
			d_normalGroup.GetAction("ViewControlAction").Visible = false;
			
			d_normalGroup.GetAction("RevertAction").Sensitive = !d_simulation.Running && d_project.Filename != null;
			
			d_grid.Sensitive = !d_simulation.Running;
			
			if (d_propertyView != null)
			{
				d_propertyView.Sensitive = d_propertyView.Object != null && !d_simulation.Running;
			}
			
			if (d_propertyEditors != null)
			{			
				foreach (KeyValuePair<Wrappers.Wrapper, Dialogs.Property> pair in d_propertyEditors)
				{
					pair.Value.View.Sensitive = pair.Value.View.Object != null && !d_simulation.Running;
				}
			}
			
			d_simulateButtons.Sensitive = !d_simulation.Running;
			d_toolbar.Sensitive = !d_simulation.Running;
			d_menubar.Sensitive = !d_simulation.Running;
			d_pathbar.Sensitive = !d_simulation.Running;
		}
					
		public Grid Grid
		{
			get
			{
				return d_grid;
			}
		}
					
		public string Period
		{
			get
			{
				return d_periodEntry.Text;
			}
			set
			{
				d_periodEntry.Text = value;
			}
		}
		
		private void PositionWindow(Gtk.Window w)
		{
			int root_x, root_y;
			
			GetPosition(out root_x, out root_y);
			
			int monitor = Screen.GetMonitorAtWindow(GdkWindow);
			Gdk.Rectangle geom = Screen.GetMonitorGeometry(monitor);
			
			int sw = geom.Width;
			int sh = geom.Height;
			
			root_x -= geom.X;
			root_y -= geom.Y;
			
			int mw, mh;
			GetSize(out mw, out mh);
			
			int ww, wh;
			w.GetSize(out ww, out wh);
			
			mw += 10;
			mh += 10;
			ww += 10;
			wh += 10;
			
			int maxx = Utils.Max(new int[] {root_x, sw - (root_x + mw)});
			int maxy = Utils.Max(new int[] {root_y, sh - (root_y + mh)});
			
			int nx, ny;
			
			if (maxx > ww && maxx > maxy)
			{
				nx = root_x > sw - (root_x + mw) ? root_x - ww : root_x + mw;
				ny = root_y;
			}
			else if (maxy > wh && maxy > maxx)
			{
				nx = root_x;
				ny = root_y > sh - (root_y + mh) ? root_y - wh : root_y + mh;
			}
			else
			{
				nx = sw - ww;
				ny = sh - wh;
			}
			
			w.Move(Utils.Max(new int [] {Utils.Min(new int[] {nx, sw - ww}), 0}) + geom.X,
			       Utils.Max(new int [] {Utils.Min(new int[] {ny, sh - wh}), 0}) + geom.Y);
		}
		
		private void DoObjectActivated(object source, Wrappers.Wrapper obj)
		{
			ObjectActivated(obj);
		}
		
		private void ObjectActivated(Wrappers.Wrapper obj)
		{			
			if (d_propertyEditors.ContainsKey(obj))
			{
				d_propertyEditors[obj].Present();
				return;
			}
			
			Dialogs.Property dlg = new Dialogs.Property(this, obj);
			PositionWindow(dlg);
			
			dlg.View.TemplateActivated += HandlePropertyTemplateActivated;
			
			dlg.View.Error += delegate (object s, Exception exception)
			{
				Message(Gtk.Stock.DialogInfo, "Error while editing property", exception);
			};
			
			dlg.Show();
			
			d_propertyEditors[obj] = dlg;
			
			dlg.Response += delegate(object o, ResponseArgs args) {
				d_grid.QueueDraw();
				
				d_propertyEditors.Remove(obj);
				dlg.Destroy();
			};
		}
		
		private string[] CommonProperties(Wrappers.Wrapper[] objects)
		{
			if (objects.Length == 0)
			{
				return new string[] {};
			}
			
			List<string> props = (new List<Cpg.Property>(objects[0].Properties)).ConvertAll<string>(item => item.Name);
			int i = 1;
			
			while (props.Count > 0 && i < objects.Length)
			{
				List<string> pp = (new List<Cpg.Property>(objects[i].Properties)).ConvertAll<string>(item => item.Name);
				
				props.RemoveAll(item => !pp.Contains(item));
				++i;
			}
			
			return props.ToArray();
		}
		
		private void DoPopup(object source, int button, long time)
		{
			if (d_popupMergeId != 0)
			{
				d_uimanager.RemoveUi(d_popupMergeId);
				d_uimanager.RemoveActionGroup(d_popupActionGroup);
			}
			
			d_popupMergeId = d_uimanager.NewMergeId();
			d_popupActionGroup = new ActionGroup("PopupDynamic");
			d_uimanager.InsertActionGroup(d_popupActionGroup, 0);
			
			// Merge monitor
			Wrappers.Wrapper[] selection = d_grid.Selection;
			
			foreach (string prop in CommonProperties(selection))
			{
				string name = "Monitor" + prop;
				string p = (string)prop.Clone();
				
				d_popupActionGroup.Add(new ActionEntry[] {
					new ActionEntry(name + "Action", null, p.Replace("_", "__"), null, null, delegate (object s, EventArgs a) {
						OnStartMonitor(selection, p);
					})
				});
				
				d_uimanager.AddUi(d_popupMergeId, "/GridPopup/MonitorMenu/MonitorPlaceholder", name, name + "Action", UIManagerItemType.Menuitem, false);
			}
			
			Widget menu = d_uimanager.GetWidget("/GridPopup");
			menu.ShowAll();
			(menu as Menu).Popup(null, null, null, (uint)button, (uint)time);
		}
		
		private void DoObjectRemoved(object source, Wrappers.Wrapper obj)
		{
			if (d_propertyEditors.ContainsKey(obj))
			{
				d_propertyEditors[obj].Destroy();
			}
		}
		
		private void ModifiedChanged()
		{
			if (!d_modified)
			{
				d_modified = true;
				
				if (!d_undoManager.IsUnmodified)
				{
					UpdateTitle();
				}
			}
			else if (!d_undoManager.IsUnmodified)
			{
			}
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			if (d_propertyView != null)
			{
				Wrappers.Wrapper[] selection = d_grid.Selection;
				
				if (selection.Length == 1)
				{
					d_propertyView.Initialize(selection[0]);
				}
				else
				{
					d_propertyView.Initialize(null);
				}
			}
			
			UpdateSensitivity();
		}
			
		private void DoFocusOutEvent(object source, Gtk.FocusOutEventArgs evnt)
		{
			UpdateSensitivity();
		}
			
		private void DoFocusInEvent(object source, Gtk.FocusInEventArgs evnt)
		{
			UpdateSensitivity();
		}

		private bool AskUnsavedModified()
		{
			if (!d_modified)
			{
				return true;
			}
			
			MessageDialog dlg = new MessageDialog(this,
			                                      DialogFlags.DestroyWithParent | DialogFlags.Modal,
			                                      MessageType.Warning,
			                                      ButtonsType.None,
			                                      true,
			                                      "<span weight=\"bold\" size=\"larger\">Save changes before closing?</span>");
			dlg.SecondaryText = "There are unsaved changes in the current network, do you want to save these changes first?";

			Gtk.Button button = new Gtk.Button();
			button.Label = "Close without Saving";
			button.Show();
			
			dlg.AddActionWidget(button, Gtk.ResponseType.Cancel);			

			button = new Gtk.Button();
			button.Image = new Gtk.Image(Gtk.Stock.Cancel, Gtk.IconSize.Button);
			button.Label = "Cancel";
			button.Show();
			
			dlg.AddActionWidget(button, Gtk.ResponseType.Close);

			button = new Gtk.Button();
			button.Image = new Gtk.Image(Gtk.Stock.Save, Gtk.IconSize.Button);
			button.Label = "Save";
			button.Show();
			button.CanDefault = true;
			
			dlg.AddActionWidget(button, Gtk.ResponseType.Yes);

			dlg.DefaultResponse = Gtk.ResponseType.Yes;
			dlg.Default = button;

			int resp = dlg.Run();
			
			if (resp == (int)ResponseType.Yes)
			{
				FileChooserDialog d = DoSave();
				
				if (d != null)
				{
					d.Run();
				}
			}
			
			dlg.Destroy();
			return resp != (int)ResponseType.Close && resp != (int)ResponseType.DeleteEvent;
		}
		
		public int PanePosition
		{
			get
			{
				return d_propertyView != null ? d_vpaned.Allocation.Height - d_vpaned.Position : -1;
			}
			set
			{
				ToggleAction action = d_normalGroup.GetAction("ViewPropertyEditorAction") as ToggleAction;
			
				if (value == -1)
				{
					action.Active = false;
				}
				else
				{
					action.Active = true;
					d_vpaned.Position = d_vpaned.Allocation.Height - value;
				}
			}
		}
		
		public bool ShowStatusbar
		{
			get
			{
				return ((ToggleAction)d_normalGroup.GetAction("ViewStatusbarAction")).Active;
			}
			set
			{
				((ToggleAction)d_normalGroup.GetAction("ViewStatusbarAction")).Active = value;
			}
		}
		
		public bool ShowToolbar
		{
			get
			{
				return ((ToggleAction)d_normalGroup.GetAction("ViewToolbarAction")).Active;
			}
			set
			{
				((ToggleAction)d_normalGroup.GetAction("ViewToolbarAction")).Active = value;
			}
		}
		
		public bool ShowPathbar
		{
			get
			{
				return ((ToggleAction)d_normalGroup.GetAction("ViewPathbarAction")).Active;
			}
			set
			{
				((ToggleAction)d_normalGroup.GetAction("ViewPathbarAction")).Active = value;
			}
		}
		
		public bool ShowSimulateButtons
		{
			get
			{
				return ((ToggleAction)d_normalGroup.GetAction("ViewSimulateButtonsAction")).Active;
			}
			set
			{
				((ToggleAction)d_normalGroup.GetAction("ViewSimulateButtonsAction")).Active = value;
			}
		}
		
		/* Callbacks */
		protected override bool OnDeleteEvent(Gdk.Event evt)
		{
			if (!AskUnsavedModified())
			{
				return true;
			}
			
			Gtk.Application.Quit();
			return true;
		}
			
		private void OnFileNew(object obj, EventArgs args)
		{
			if (!AskUnsavedModified())
			{
				return;
			}

			Clear();
			
			d_project.Clear();
			
			UpdateSensitivity();
			UpdateTitle();
		}
		
		private void RestoreSettings()
		{
			Serialization.Project.SettingsType s = d_project.Settings;

			if (s.Allocation != null)
			{
				Allocation alloc = d_project.Settings.Allocation;

				if (!Visible && alloc.X >= 0 && alloc.Y >= 0)
				{
					Move((int)alloc.X, (int)alloc.Y);
				}
				
				Resize((int)alloc.Width, (int)alloc.Height);
			}
			
			PanePosition = s.PanePosition;

			ShowToolbar = s.ToolBar;
			ShowPathbar = s.PathBar;
			ShowSimulateButtons = s.SimulateBar;
			ShowStatusbar = s.StatusBar;
			
			d_periodEntry.Text = s.SimulatePeriod;
			
			// Restore root
			if (!String.IsNullOrEmpty(s.ActiveRoot))
			{
				if (s.ActiveRoot == "templates")
				{
					d_grid.ActiveGroup = Network.TemplateGroup;
				}
			}
			
			// Restore active group
			if (!String.IsNullOrEmpty(s.ActiveGroup))
			{
				Wrappers.Group w = Network.FindObject(s.ActiveGroup) as Wrappers.Group;
				
				if (w != null)
				{
					d_grid.ActiveGroup = w;
				}
			}
			
			// Restore monitors
			Serialization.Project.SettingsType.MonitorsType mons = s.Monitors;
			
			if (mons.Columns > 0 && mons.Rows > 0)
			{
				EnsureMonitor();
				
				if (mons.Allocation != null)
				{
					d_monitor.Resize((int)mons.Allocation.Width, (int)mons.Allocation.Height);
					d_monitor.Move((int)mons.Allocation.X, (int)mons.Allocation.Y);
				}
				
				for (int i = 0; i < mons.Graphs.Count; ++i)
				{
					int col = (int)(i % mons.Columns);
					int row = (int)(i / mons.Columns);
					
					List<Cpg.Property> props = new List<Cpg.Property>();
					
					foreach (string p in mons.Graphs[i].Id)
					{
						Cpg.Property prop = Network.FindProperty(p);
						
						if (prop != null)
						{
							props.Add(prop);
						}
					}
					
					d_monitor.AddHook(props.ToArray(), row, col);
				}
			}
		}
		
		public void DoLoad(string filename)
		{
			if (!AskUnsavedModified())
			{
				return;
			}

			Clear();

			try
			{
				d_project.Load(filename);
			}
			catch (Exception e)
			{
				Message(Gtk.Stock.DialogError, "Error while loading network", e);
				return;
			}
					
			RestoreSettings();

			UpdateTitle();
			
			StatusMessage("Loaded network from " + filename + "...");
		}
		
		private void OnOpenActivated(object sender, EventArgs args)
		{
			FileChooserDialog dlg = new FileChooserDialog("Open",
			                                              this,
			                                              FileChooserAction.Open,
			                                              Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			                                              Gtk.Stock.Open, Gtk.ResponseType.Accept);
		
			if (d_project.Filename != null)
			{
				dlg.SetCurrentFolder(System.IO.Path.GetDirectoryName(d_project.Filename));
			}
			else if (d_prevOpen != null)
			{
				dlg.SetCurrentFolder(d_prevOpen);
			}
			else
			{
				dlg.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
			}
		
			dlg.Response += delegate(object o, ResponseArgs a) {
				d_prevOpen = dlg.CurrentFolder;

				string filename = dlg.Filename;
				dlg.Destroy();
				
				if (a.ResponseId == ResponseType.Accept)
				{
					DoLoad(filename);
				}
			};
			
			dlg.Show();
		}
		
		private void OnRevertActivated(object sender, EventArgs args)
		{
			string filename = d_project.Filename;
			
			Clear();
			d_modified = false;
			
			if (filename != null)
			{
				DoLoad(filename);
			}
		}
		
		private Allocation WindowAllocation(Gtk.Window win)
		{
			int x;
			int y;
			int width;
			int height;

			win.GetPosition(out x, out y);
			win.GetSize(out width, out height);
			
			return new Allocation(x, y, width, height);
		}
		
		private void SaveProjectSettings()
		{
			Serialization.Project.SettingsType s = d_project.Settings;

			s.PathBar = ShowPathbar;
			s.SimulateBar = ShowSimulateButtons;
			s.StatusBar = ShowStatusbar;
			s.ToolBar = ShowToolbar;
			s.PanePosition = PanePosition;
			s.SimulatePeriod = d_periodEntry.Text;
			
			s.Allocation = WindowAllocation(this);
			s.ActiveGroup = d_grid.ActiveGroup.FullId;
			
			if (d_grid.ActiveGroup.TopParent == Network.TemplateGroup)
			{
				s.ActiveRoot = "templates";
			}
			else
			{
				s.ActiveRoot = null;
			}

			s.Monitors.Graphs.Clear();
			s.Monitors.Rows = 0;
			s.Monitors.Columns = 0;
			
			// Save monitor state
			if (d_monitor != null)
			{
				foreach (List<KeyValuePair<Wrappers.Wrapper, Cpg.Property>> monitor in d_monitor.Monitors)
				{
					Serialization.Project.Monitor mon = new Serialization.Project.Monitor();
					
					foreach (KeyValuePair<Wrappers.Wrapper, Cpg.Property> prop in monitor)
					{
						mon.Id.Add(prop.Value.FullName);
					}
					
					s.Monitors.Graphs.Add(mon);
				}
				
				s.Monitors.Rows = d_monitor.Rows;
				s.Monitors.Columns = d_monitor.Columns;
				
				s.Monitors.Allocation = WindowAllocation(d_monitor);
			}
		}
		
		private void DoSave(string filename, bool externalProject)
		{
			d_project.SaveProjectExternally = externalProject;
			
			SaveProjectSettings();
			
			d_project.Save(filename);
			
			d_modified = false;
			d_undoManager.MarkUnmodified();
						
			UpdateSensitivity();
			UpdateTitle();
			
			StatusMessage("Saved network to " + filename + "...");
		}
		
		private FileChooserDialog DoSaveAs()
		{
			FileChooserDialog dlg = new FileChooserDialog("Save As",
			                                              this,
			                                              FileChooserAction.Save,
			                                              Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			                                              Gtk.Stock.Save, Gtk.ResponseType.Accept);

			if (d_project.Filename != null)
			{
				dlg.SetCurrentFolder(System.IO.Path.GetDirectoryName(d_project.Filename));
			}
			else if (d_prevOpen != null)
			{
				dlg.SetCurrentFolder(d_prevOpen);
			}
			
			CheckButton check = new CheckButton("Save project settings in a separate file");
			check.Show();
			
			check.Active = d_project.SaveProjectExternally;
			dlg.ExtraWidget = check;
			
			dlg.DoOverwriteConfirmation = true;
			
			dlg.Response += delegate(object o, ResponseArgs args) {
					d_prevOpen = dlg.CurrentFolder;

					if (args.ResponseId == ResponseType.Accept)
					{
						string filename = dlg.Filename;
						
						try
						{
							DoSave(filename, check.Active);
						}
						catch (Exception e)
						{
							Message(Gtk.Stock.DialogError, "An error occurred while saving the network", e);
						}
					}
					
					dlg.Destroy();		
			};
			
			dlg.Show();
			
			return dlg;
		}
		
		private FileChooserDialog DoSave()
		{
			if (d_project.Filename == null)
			{
				return DoSaveAs();
			}
			
			DoSave(d_project.Filename, d_project.SaveProjectExternally);
			return null;
		}
		
		private void OnSaveActivated(object sender, EventArgs args)
		{
			DoSave();
		}
		
		public void StatusMessage(string message)
		{
			d_statusbar.Push(0, message);
			
			if (d_statusTimeout != 0)
			{
				GLib.Source.Remove(d_statusTimeout);
			}
			
			d_statusTimeout = GLib.Timeout.Add(3000, delegate () {
				d_statusTimeout = 0;
				d_statusbar.Push(0, "");
				return false;
			});
		}
		
		public void Message(string icon, string primary, string secondary, params object[] actions)
		{
			if (d_messageArea != null)
			{
				d_messageArea.Destroy();
			}
			
			if (actions.Length == 0)
			{			
				d_messageArea = MessageArea.Create(icon, primary, secondary, Gtk.Stock.Close, ResponseType.Close);
			}
			else
			{
				d_messageArea = MessageArea.Create(icon, primary, secondary, actions);
			}
			
			d_messageArea.Response += delegate(object source, ResponseType type) {
				if (type == ResponseType.DeleteEvent || type == ResponseType.Close)
				{
					d_messageArea.Destroy();
				}
			};
			
			d_messageArea.Destroyed += delegate(object sender, EventArgs e) {
					d_messageArea = null;
			};
			
			d_vboxContents.PackStart(d_messageArea, false, false, 0);
			d_vboxContents.ReorderChild(d_messageArea, 0);
			
			d_messageArea.Show();
			
			d_messageArea.SetDefaultResponse(ResponseType.Close);
			d_messageArea.GrabFocus();
		}
		
		public void Message(string icon, string primary, Exception exception)
		{
			while (exception.InnerException != null)
			{
				exception = exception.InnerException;
			}
			
			Message(icon, primary, exception.Message);
			
			if (d_messageArea != null)
			{
				d_messageArea.TooltipText = exception.StackTrace;
			}
		}
		
		private void OnSaveAsActivated(object sender, EventArgs args)
		{
			DoSaveAs();
		}

		private void OnQuitActivated(object sender, EventArgs args)
		{
			Gtk.Application.Quit();
		}
		
		private delegate void ErrorThrowingHandler();
		
		private void HandleError(ErrorThrowingHandler handler, string primary)
		{
			try
			{
				handler();
			}
			catch (Exception e)
			{
				Message(Gtk.Stock.DialogError, primary, e);
			}
		}
		
		private void OnPasteActivated(object sender, EventArgs args)
		{
			int[] center = d_grid.Center;
			
			HandleError(delegate () {
				d_actions.Paste(d_grid.ActiveGroup, d_grid.Selection, center[0], center[1]);
			}, "An error occurred while pasting");
		}
		
		private void OnGroupActivated(object sender, EventArgs args)
		{
			HandleError(delegate () {
				Wrappers.Group grp = d_actions.Group(d_grid.ActiveGroup, d_grid.Selection);
				
				if (grp != null)
				{
					d_grid.UnselectAll();
					d_grid.Select(grp);
				}
			}, "An error occurred while grouping");
		}
		
		private void OnUngroupActivated(object sender, EventArgs args)
		{
			HandleError(delegate () {
				d_actions.Ungroup(d_grid.ActiveGroup, d_grid.Selection);
			}, "An error occurred while ungrouping");
		}
		
		private void Select<T>(IEnumerable<T> objs) where T : Wrappers.Wrapper
		{
			d_grid.UnselectAll();

			foreach (Wrappers.Wrapper wrapper in objs)
			{
				d_grid.Select(wrapper);
			}
		}
		
		private void OnAddStateActivated(object sender, EventArgs args)
		{
			int[] center = d_grid.Center;
			
			HandleError(delegate () {
				Select(d_actions.AddState(d_grid.ActiveGroup, center[0], center[1]));
			}, "An error occurred while adding a state");
		}
		
		private void OnAddLinkActivated(object sender, EventArgs args)
		{
			int[] center = d_grid.Center;

			HandleError(delegate () {
				Select(d_actions.AddLink(d_grid.ActiveGroup, d_grid.Selection, center[0], center[1]));
			}, "An error occurred while adding a link");
		}
		
		private void SelectFunction(Wrappers.Function function)
		{
			ShowFunctions();
			d_functionsDialog.Select(function);
		}
		
		private void SelectFunction(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece)
		{
			ShowFunctions();

			d_functionsDialog.Select(function, piece);
		}
		
		private void SelectFromPropertyAction(Undo.Property action)
		{
			if (action.Wrapped == Network)
			{
				ObjectActivated(Network);
			}
			else if (action.Wrapped.TopParent == d_grid.ActiveGroup.TopParent)
			{
				d_grid.CenterView(action.Wrapped);
			}
		}
		
		private void SelectFromObjectAction(Undo.Object action)
		{
			if (action.Wrapped is Wrappers.Function)
			{
				SelectFunction((Wrappers.Function)action.Wrapped);
			}
			else if (action.Wrapped.TopParent == d_grid.ActiveGroup.TopParent)
			{
				if (action is Undo.MoveObject)
				{
					d_grid.ActiveGroup = action.Wrapped.Parent;
					d_grid.UnselectAll();
					d_grid.Select(action.Wrapped);
				}
				else
				{
					d_grid.CenterView(action.Wrapped);
				}
			}
		}
		
		private void SelectFromAddGroupAction(Undo.AddGroup action)
		{
			if (action.Group.TopParent == d_grid.ActiveGroup.TopParent)
			{
				d_grid.CenterView(action.Group);
			}
		}
		
		private void SelectFromUngroupAction(Undo.Ungroup action)
		{
			if (action.Parent.TopParent == d_grid.ActiveGroup.TopParent || action.Parent == d_grid.ActiveGroup.TopParent)
			{
				d_grid.ActiveGroup = action.Parent;
				d_grid.CenterView();
			}
		}
		
		private void SelectFromLinkAction(Undo.LinkAction action)
		{
			if (action.Link.TopParent == d_grid.ActiveGroup.TopParent)
			{
				d_grid.CenterView(action.Link);
			}
		}
		
		private void SelectFromFunctionPolynomialPieceAction(Undo.FunctionPolynomialPiece action)
		{
			SelectFunction(action.WrappedObject, action.Piece);
		}
		
		private void SelectFromAction(Undo.IAction action)
		{
			if (action is Undo.Property)
			{
				SelectFromPropertyAction((Undo.Property)action);
			}
			else if (action is Undo.FunctionPolynomialPiece)
			{
				SelectFromFunctionPolynomialPieceAction((Undo.FunctionPolynomialPiece)action);
			}
			else if (action is Undo.Object)
			{
				SelectFromObjectAction((Undo.Object)action);
			}
			else if (action is Undo.AddGroup)
			{
				SelectFromAddGroupAction((Undo.AddGroup)action);
			}
			else if (action is Undo.Ungroup)
			{
				SelectFromUngroupAction((Undo.Ungroup)action);
			}
			else if (action is Undo.LinkAction)
			{
				SelectFromLinkAction((Undo.LinkAction)action);
			}
		}
		
		private void OnUndoActivated(object sender, EventArgs args)
		{
			SelectFromAction(d_undoManager.PeekUndo());
			SelectFromAction(d_undoManager.Undo());

			d_grid.QueueDraw();
		}
		
		private void OnRedoActivated(object sender, EventArgs args)
		{
			SelectFromAction(d_undoManager.PeekRedo());
			SelectFromAction(d_undoManager.Redo());
			
			d_grid.QueueDraw();
		}

		private void OnEditGlobalsActivated(object sender, EventArgs args)
		{
			ObjectActivated(Network);
		}
		
		private void OnCenterViewActivated(object sender, EventArgs args)
		{
			d_grid.CenterView();
		}
		
		private void OnEditGroupActivated(object sender, EventArgs args)
		{
			Wrappers.Wrapper[] selection = d_grid.Selection;
			
			if (selection.Length != 1 || !(selection[0] is Wrappers.Group))
			{
				return;
			}
			
			d_grid.ActiveGroup = (Wrappers.Group)selection[0];
		}
		
		private void OnPropertiesActivated(object sender, EventArgs args)
		{
			Wrappers.Wrapper[] selection = d_grid.Selection;
			
			if (selection.Length != 0)
			{
				ObjectActivated(selection[0]);
			}
		}
							
		private void OnViewStatusbarActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			d_statusbar.Visible = action.Active;
		}
		
		private void OnViewToolbarActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			d_toolbar.Visible = action.Active;
		}
		
		private void OnViewPathbarActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			d_pathbar.Visible = action.Active;
		}
		
		private void OnViewSimulateButtonsActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			d_simulateButtons.Visible = action.Active;
		}
		
		public Actions Actions
		{
			get
			{
				return d_actions;
			}
		}
		
		private void OnViewPropertyEditorActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			if (action.Active)
			{
				if (d_propertyView == null)
				{
					Wrappers.Wrapper[] selection = d_grid.Selection;
					d_propertyView = new PropertyView(d_actions, selection.Length == 1 ? selection[0] : null);
					d_propertyView.BorderWidth = 3;
					
					d_propertyView.Error += delegate (object source, Exception exception)
					{
						Message(Gtk.Stock.DialogError, "Error while editing property", exception);
					};
					
					d_propertyView.TemplateActivated += HandlePropertyTemplateActivated;
					
					d_vpaned.Pack2(d_propertyView, false, false);
				}
				
				d_propertyView.ShowAll();
			}
			else
			{
				if (d_propertyView != null)
				{
					d_propertyView.Destroy();
				}
				
				d_propertyView = null;
			}
		}
		
		private void HandlePropertyTemplateActivated(object source, Wrappers.Wrapper template)
		{
			PropertyView view = source as PropertyView;
			
			if (view.Object == null)
			{
				return;
			}
			
			d_templatePopupObject = view.Object;
			d_templatePopupTemplate = template;
			
			Widget menu = d_uimanager.GetWidget("/TemplatePopup");
			menu.ShowAll();

			(menu as Menu).Popup(null, null, null, 0, 0);
		}
					
		private void OnZoomInActivated(object sender, EventArgs args)
		{
			d_grid.ZoomIn();
		}
					
		private void OnZoomOutActivated(object sender, EventArgs args)
		{
			d_grid.ZoomOut();
		}
					
		private void OnZoomDefaultActivated(object sender, EventArgs args)
		{
			d_grid.ZoomDefault();
		}
		
		private void EnsureMonitor()
		{
			if (d_monitor != null)
			{
				return;
			}
			
			d_monitor = new Monitor(Network, d_simulation);
			d_monitor.TransientFor = this;
			
			d_monitor.Realize();

			PositionWindow(d_monitor);
			d_monitor.Present();
			
			d_monitor.Destroyed += delegate(object sender, EventArgs e) {
				d_monitor = null;
				(d_normalGroup.GetAction("ViewMonitorAction") as ToggleAction).Active = false;
			};
		}
		
		private void OnToggleMonitorActivated(object sender, EventArgs args)
		{
			ToggleAction toggle = sender as ToggleAction;
			
			if (!toggle.Active && d_monitor != null)
			{
				Gtk.Window ctrl = d_monitor;
				d_monitor = null;
				ctrl.Destroy();
			}
			else if (toggle.Active)
			{
				EnsureMonitor();
				d_monitor.Present();
			}
		}
		
		private void OnToggleControlActivated(object sender, EventArgs args)
		{
		}
		
		private void OnCutActivated(object sender, EventArgs args)
		{
			HandleError(delegate () {
				d_actions.Cut(d_grid.ActiveGroup, d_grid.Selection);
			}, "An error occurred while cutting");
			
			UpdateSensitivity();
		}
		
		private void OnCopyActivated(object sender, EventArgs args)
		{
			HandleError(delegate () {
				d_actions.Copy(d_grid.Selection);
			}, "An error occurred while copying");
			
			UpdateSensitivity();
		}
		
		private void OnDeleteActivated(object sender, EventArgs args)
		{
			HandleError(delegate () {
				d_actions.Delete(d_grid.ActiveGroup, d_grid.Selection);
			}, "An error occurred while deleting");
		}
		
		private void DoError(object sender, string error, string message)
		{
			Message(Gtk.Stock.DialogError, error, message);
		}
		
		private void OnStartMonitor(Wrappers.Wrapper[] objs, string property)
		{
			EnsureMonitor();
			
			foreach (Wrappers.Wrapper obj in objs)
			{
				d_monitor.AddHook(obj.Property(property));
			}
		}

		private void OnSimulationRunPeriod(object sender, EventArgs args)
		{
			Range r = new Range(d_periodEntry.Text);
			
			if (r.From > r.To)
			{
				Message(Gtk.Stock.DialogInfo, "Invalid simulation range", "The start of the simulation range is larger than the end");
			}
			else if (r.Step <= 0)
			{
				Message(Gtk.Stock.DialogInfo, "Invalid step size", "The step size for the simulation is invalid");
			}
			else
			{
				d_simulation.RunPeriod(r.From, r.Step, r.To);
			}
		}
		
		private void OnSimulationStep(object sender, EventArgs args)
		{
			Range r = new Range(d_periodEntry.Text);
			
			if (r.Step > 0)
			{
				d_simulation.Step(r.Step);
			}
			else
			{
				Message(Gtk.Stock.DialogInfo, "Invalid step size", "The step size for the simulation is invalid");
			}
		}
							
		private void OnStepActivated(object sender, EventArgs args)
		{
			OnSimulationStep(sender, args);
		}
							
		private void OnSimulateActivated(object sender, EventArgs args)
		{
			OnSimulationRunPeriod(sender, args);
		}
		
		private void OnSimulationRun(object sender, EventArgs args)
		{
		}
		
		private void OnSimulationReset(object sender, EventArgs args)
		{
			d_simulation.Reset();
		}
		
		private void HandleCompileError(Cpg.CompileError error)
		{
			string title;
			string expression;
			
			title = error.Object.Id;
			
			if (error.Property != null)
			{
				title += "." + error.Property.Name;
				expression = error.Property.Expression.AsString;
			}
			else if (error.LinkAction != null)
			{
				title += "»" + error.LinkAction.Target;
				expression = error.LinkAction.Equation.AsString;
			}
			else if (error.Object is Cpg.Function)
			{
				expression = ((Cpg.Function)error.Object).Expression.AsString;
			}
			else
			{
				expression = "";
			}
		
			d_grid.Select(error.Object);
			
			if (d_propertyView != null)
			{
				if (error.Property != null)
				{
					d_propertyView.Select(error.Property);
				}
				else
				{
					d_propertyView.Select(error.LinkAction);
				}
			}
			else if (error.Object is Cpg.Function)
			{
				ShowFunctions();
			}
		
			Message(Gtk.Stock.DialogError, 
			        "Error while compiling " + title,
			        error.String() + ": " + error.Message + "\n\nExpression: \"" + expression + "\"");
		}
		
		private void OnCompileError(object sender, Cpg.CompileErrorArgs args)
		{
			HandleCompileError(args.Error);
		}
		
		public Wrappers.Network Network
		{
			get
			{
				return d_project.Network;
			}
		}
		
		private void ShowFunctions()
		{
			if (d_functionsDialog == null)
			{
				d_functionsDialog = new Dialogs.Functions(d_actions, this, Network);
				d_windowGroup.AddWindow(d_functionsDialog);

				d_functionsDialog.Response += delegate(object o, ResponseArgs a1) {
					d_functionsDialog.Destroy();
					d_functionsDialog = null;
				};
			}
			
			d_functionsDialog.Present();
		}
		
		private void OnEditFunctionsActivated(object sender, EventArgs args)
		{
			ShowFunctions();
		}
		
		private void OnEditTemplateActivated(object sender, EventArgs args)
		{
			d_grid.UnselectAll();
			d_grid.Select(d_templatePopupTemplate);
		}
		
		private void OnRemoveTemplateActivated(object sender, EventArgs args)
		{
			HandleError(delegate () {
				d_actions.UnapplyTemplate(d_templatePopupObject, d_templatePopupTemplate);
			}, "An error occurred while unapplying the template");
		}
		
		private void OnImportActivated(object sender, EventArgs args)
		{
			Dialogs.Import dlg = new Dialogs.Import(this);
			
			dlg.Response += HandleImportResponse;
			dlg.Present();
		}
		
		private void RepositionImport(IEnumerable<Wrappers.Wrapper> current, IEnumerable<Wrappers.Wrapper> objs)
		{
			Point currentMean = new Point();
			Point objsMean = new Point();

			Utils.MeanPosition(current, out currentMean.X, out currentMean.Y);
			Utils.MeanPosition(objs, out objsMean.X, out objsMean.Y);
			
			currentMean.Floor();
			objsMean.Floor();
			
			Point offset = currentMean - objsMean;
			
			foreach (Wrappers.Wrapper wrapper in objs)
			{
				wrapper.Allocation.Offset(offset);
			}
		}
		
		private void DoImportCopy(string filename, bool importAll)
		{
			Serialization.Project project = new Serialization.Project();

			try
			{
				project.Load(filename);
			}
			catch (Exception e)
			{
				Message(Gtk.Stock.DialogError, "Failed to import network", e);
				return;
			}
			
			List<Cpg.Property> skippedProperties = new List<Cpg.Property>();
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			// Copy globals
			foreach (Cpg.Property property in project.Network.Properties)
			{
				if (!Network.HasProperty(property.Name))
				{
					actions.Add(new Undo.AddProperty(Network, property));
				}
				else
				{
					skippedProperties.Add(property);
				}
			}
			
			List<Wrappers.Function> skippedFunctions = new List<Wrappers.Function>();
			
			// Copy functions
			foreach (Wrappers.Function function in project.Network.Functions)
			{
				if (Network.GetFunction(function.Id) == null)
				{
					actions.Add(new Undo.AddObject(Network.FunctionGroup, function.Copy()));
				}
				else
				{
					skippedFunctions.Add(function);
				}
			}
			
			RepositionImport(Network.TemplateGroup.Children, project.Network.TemplateGroup.Children);
			
			// Copy templates
			foreach (Wrappers.Wrapper obj in project.Network.TemplateGroup.Children)
			{
				actions.Add(new Undo.AddObject(Network.TemplateGroup, obj));
			}
			
			if (importAll)
			{
				RepositionImport(d_grid.ActiveGroup.Children, project.Network.Children);

				// Also copy objects
				foreach (Wrappers.Wrapper obj in project.Network.Children)
				{
					actions.Add(new Undo.AddObject(d_grid.ActiveGroup, obj));
				}
			}
			
			HandleError(delegate () {
				d_actions.Do(new Undo.Group(actions));
			}, "Failed to import network");
			
			if (skippedFunctions.Count != 0 && skippedProperties.Count != 0)
			{
				Message(Gtk.Stock.DialogInfo,
				        "Some functions and globals could not be imported",
				        String.Format("The {0} `{1}' and {2} `{3}' already existed",
				                      skippedFunctions.Count == 1 ? "function" : "functions",
				                      String.Join(", ", Array.ConvertAll<Wrappers.Function, string>(skippedFunctions.ToArray(), item => item.Id)),
				                      skippedProperties.Count == 1 ? "global" : "globals",
				                      String.Join(", ", Array.ConvertAll<Cpg.Property, string>(skippedProperties.ToArray(), item => item.Name))));
			}
			else if (skippedFunctions.Count != 0)
			{
				Message(Gtk.Stock.DialogInfo,
				        "Some functions could not be imported",
				        String.Format("The {0} `{1}' already existed",
				                      skippedFunctions.Count == 1 ? "function" : "functions",
				                      String.Join(", ", Array.ConvertAll<Wrappers.Function, string>(skippedFunctions.ToArray(), item => item.Id))));
			}
			else if (skippedProperties.Count != 0)
			{
				Message(Gtk.Stock.DialogInfo,
				        "Some globals could not be imported",
				        String.Format("The {0} `{1}' already existed",
				                      skippedProperties.Count == 1 ? "global" : "globals",
				                      String.Join(", ", Array.ConvertAll<Cpg.Property, string>(skippedProperties.ToArray(), item => item.Name))));
			}
		}
		
		private void DoImport(string filename, bool importAll)
		{
			// See if it was already imported before
			Wrappers.Import import = Network.GetImportFromPath(filename);
			
			string id = System.IO.Path.GetFileNameWithoutExtension(filename);
			
			if (import == null)
			{
				try
				{
					if (importAll)
					{
						HandleError(delegate () {
							d_actions.Do(new Undo.Import(Network, d_grid.ActiveGroup, id, filename));
						}, "Failed to import network");
					}
					else
					{
						HandleError(delegate () {
							d_actions.Do(new Undo.Import(Network, Network.TemplateGroup, id, filename));
						}, "Failed to import network");
					}
				}
				catch (Exception e)
				{
					Message(Gtk.Stock.DialogError, "Failed to import network", e);
				}
			}
			else
			{
				Message(Gtk.Stock.DialogInfo, "File was already imported", String.Format("The file `{0}' was already imported", System.IO.Path.GetFileName(filename)));
			}
		}
		
		private void DoImport(string[] filenames, bool copyObject, bool importAll)
		{
			foreach (string filename in filenames)
			{
				if (copyObject)
				{
					DoImportCopy(filename, importAll);
				}
				else
				{
					DoImport(filename, importAll);
				}
			}
		}

		private void HandleImportResponse(object o, ResponseArgs args)
		{
			Dialogs.Import dlg = (Dialogs.Import)o;

			if (args.ResponseId == ResponseType.Ok)
			{
				DoImport(dlg.Filenames, dlg.CopyObjects, dlg.ImportAll);
			}
			
			dlg.Destroy();
		}
	}
}
