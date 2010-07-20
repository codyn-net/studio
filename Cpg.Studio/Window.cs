using System;
using Gtk;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cpg.Studio
{
	public class Window : Gtk.Window
	{
		private ActionGroup d_normalGroup;
		private ActionGroup d_selectionGroup;
		private HBox d_hboxPath;
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
		private Wrappers.Network d_network;
		private Monitor d_monitor;
		private Simulation d_simulation;
		private string d_prevOpen;
		private Dialogs.Functions d_functionsDialog;
		private ListStore d_integratorStore;
		private ComboBox d_integratorCombo;
		
		private bool d_modified;
		private string d_filename;
		
		private Undo.Manager d_undoManager;
		private Actions d_actions;

		public Window() : base (Gtk.WindowType.Toplevel)
		{
			d_network = new Wrappers.Network();
			
			d_network.WrappedObject.CompileError += OnCompileError;

			d_network.WrappedObject.AddNotification("integrator", delegate (object sender, GLib.NotifyArgs args) {
				UpdateCurrentIntegrator();
			});

			d_simulation = new Simulation(d_network);

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
			
			d_modified = false;
			
			UpdateUndoState();
			UpdateTitle();
			
			d_propertyEditors = new Dictionary<Wrappers.Wrapper, Dialogs.Property>();
		}

		private void UpdateUndoState()
		{
			d_normalGroup.GetAction("UndoAction").Sensitive = d_undoManager.CanUndo;
			d_normalGroup.GetAction("RedoAction").Sensitive = d_undoManager.CanRedo;
			
			// TODO
		}

		private void Build()
		{
			SetDefaultSize(700, 600);
			
			d_uimanager = new UIManager();
			d_normalGroup = new ActionGroup("NormalActions");

			d_normalGroup.Add(new ActionEntry[] {
				new ActionEntry("FileMenuAction", null, "_File", null, null, null),
				new ActionEntry("NewAction", Gtk.Stock.New, null, "<Control>N", "New CPG network", OnFileNew),
				new ActionEntry("OpenAction", Gtk.Stock.Open, null, "<Control>O", "Open CPG network", OnOpenActivated),
				new ActionEntry("RevertAction", Gtk.Stock.RevertToSaved, null, null, "Revert changes", OnRevertActivated),
				new ActionEntry("SaveAction", Gtk.Stock.Save, null, "<Control>S", "Save CPG network", OnSaveActivated),
				new ActionEntry("SaveAsAction", Gtk.Stock.SaveAs, null, "<Control><Shift>S", "Save CPG network", OnSaveAsActivated),

				//new ActionEntry("ImportAction", null, "Import", null, "Import CPG network objects", new EventHandler(OnImportActivated)),
				//new ActionEntry("ExportAction", null, "Export", "<Control>e", "Export CPG network objects", new EventHandler(OnExportActivated)),

				new ActionEntry("QuitAction", Gtk.Stock.Quit, null, "<Control>Q", "Quit", OnQuitActivated),

				new ActionEntry("EditMenuAction", null, "_Edit", null, null, null),
				new ActionEntry("UndoAction", Gtk.Stock.Undo, null, "<control>Z", "Undo last action", OnUndoActivated),
				new ActionEntry("RedoAction", Gtk.Stock.Redo, null, "<control><shift>Z", "Redo last action", OnRedoActivated),
				new ActionEntry("PasteAction", Gtk.Stock.Paste, null, "<Control>V", "Paste objects", OnPasteActivated),
				new ActionEntry("GroupAction", null, "Group", "<Control>g", "Group objects", OnGroupActivated),
				new ActionEntry("UngroupAction", null, "Ungroup", "<Control>u", "Ungroup object", OnUngroupActivated),
				new ActionEntry("EditGlobalsAction", null, "Globals", null, "Edit the network globals", OnEditGlobalsActivated),
				new ActionEntry("EditFunctionsAction", null, "Functions", null, "Edit the network custom functions", OnEditFunctionsActivated),

				new ActionEntry("AddStateAction", Studio.Stock.State, null, null, "Add state", OnAddStateActivated),
				new ActionEntry("AddLinkAction", Studio.Stock.Link, null, null, "Link objects", OnAddLinkActivated),

				new ActionEntry("SimulateMenuAction", null, "_Simulate", null, null, null),
				new ActionEntry("StepAction", Gtk.Stock.MediaNext, "Step", "<Control>t", "Execute one simulation step", OnStepActivated),
				new ActionEntry("SimulateAction", Gtk.Stock.MediaForward, "Period", "<Control>p", "(Re)Simulate period", OnSimulateActivated),
				
				new ActionEntry("ViewMenuAction", null, "_View", null, null, null),
				new ActionEntry("CenterAction", Gtk.Stock.JustifyCenter, null, "<Control>h", "Center view", OnCenterViewActivated),
				new ActionEntry("InsertMenuAction", null, "_Insert", null, null, null),
				new ActionEntry("ZoomDefaultAction", Gtk.Stock.Zoom100, null, "<Control>1", null, OnZoomDefaultActivated),
				new ActionEntry("ZoomInAction", Gtk.Stock.ZoomIn, null, "<Control>plus", null, OnZoomInActivated),
				new ActionEntry("ZoomOutAction", Gtk.Stock.ZoomOut, null, "<Control>minus", null, OnZoomOutActivated),

				new ActionEntry("MonitorMenuAction", null, "Monitor", null, null, null),
				new ActionEntry("ControlMenuAction", null, "Control", null, null, null),
				new ActionEntry("PropertiesAction", null, "Properties", null, null, OnPropertiesActivated),
				new ActionEntry("EditGroupAction", null, "Edit group", null, null, OnEditGroupActivated)
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

			vbox.PackStart(d_uimanager.GetWidget("/menubar"), false, false, 0);
			
			d_toolbar = d_uimanager.GetWidget("/toolbar");
			vbox.PackStart(d_toolbar, false, false, 0);
			
			d_hboxPath = new HBox(false, 0);
			d_hboxPath.BorderWidth = 1;
			vbox.PackStart(d_hboxPath, false, false, 0);
			
			d_vboxContents = new VBox(false, 3);
			vbox.PackStart(d_vboxContents, true, true, 0);
			
			d_grid = new Grid(d_network, d_actions);
			d_vpaned = new VPaned();
			d_vpaned.Position = 300;
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
			
			d_grid.Activated += DoObjectActivated;
			d_grid.Popup += DoPopup;

			d_grid.SelectionChanged += DoSelectionChanged;
			d_grid.FocusInEvent += DoFocusInEvent;
			d_grid.FocusOutEvent += DoFocusOutEvent;
			d_grid.Error += DoError;
			d_grid.ActiveGroupChanged += DoActiveGroupChanged;
			
			BuildButtonPath();
		}

		private void DoActiveGroupChanged(object source, Wrappers.Wrapper obj)
		{
			BuildButtonPath();
		}
		
		private void BuildButtonPath()
		{
			while (d_hboxPath.Children.Length > 0)
			{
				d_hboxPath.Remove(d_hboxPath.Children[0]);
			}
			
			Queue<Wrappers.Group> parents = new Queue<Wrappers.Group>();
			Wrappers.Group parent = d_grid.ActiveGroup;
			
			while (parent != null)
			{
				parents.Enqueue(parent);
				parent = parent.Parent;
			}
			
			while (parents.Count != 0)
			{
				PushPath(parents.Dequeue());
			}
		}
		
		private void UpdateCurrentIntegrator()
		{
			d_integratorStore.Foreach(delegate (TreeModel model, TreePath path, TreeIter piter) {
				Cpg.Integrator intgr = (Cpg.Integrator)model.GetValue(piter, 0);
				
				if (intgr.Id == d_network.Integrator.Id)
				{
					d_integratorCombo.SetActiveIter(piter);
					
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
					
				if (integrator.Id == d_network.Integrator.Id)
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
			
			d_network.Integrator = integrator;
			d_modified = true;
			
			UpdateTitle();
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
		
		private void PushPath(Wrappers.Group obj)
		{
			if (obj != d_network)
			{
				Arrow arrow = new Arrow(ArrowType.Right, ShadowType.None);
				arrow.Show();
				d_hboxPath.PackStart(arrow, false, false, 0);
			}
			
			Button but = new Button(obj.ToString());
			but.Relief = ReliefStyle.None;
			but.Show();
			
			d_hboxPath.PackStart(but, false, false, 0);
			
			but.Clicked += delegate(object source, EventArgs args) {
				d_grid.ActiveGroup = obj;
			};
		}
		
		private void Clear()
		{
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
			
			d_simulation.Range = new Range(d_periodEntry.Text);
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
			
			if (!String.IsNullOrEmpty(d_filename))
			{
				Title = extra + System.IO.Path.GetFileName(d_filename) + " - CPG Studio";
			}
			else
			{
				Title = extra + "New Network - CPG Studio";
			}
		}
		
		private void UpdateSensitivity()
		{
			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>(d_grid.Selection);
			
			d_selectionGroup.Sensitive = objects.Count > 0 && d_grid.HasFocus;
			
			bool singleobj = objects.Count == 1;
			bool singlegroup = singleobj && objects[0] is Wrappers.Group;
			int anygroup = objects.FindAll(delegate (Wrappers.Wrapper obj) { return obj is Wrappers.Group; }).Count;
			
			Action ungroup = d_normalGroup.GetAction("UngroupAction");
			ungroup.Sensitive = anygroup > 0;
			
			if (anygroup > 1)
			{
				ungroup.Label = "Ungroup All";
			}
			else
			{
				ungroup.Label = "Ungroup";
			}
			
			d_normalGroup.GetAction("GroupAction").Sensitive = objects.Count > 1 && objects.Find(delegate (Wrappers.Wrapper obj) { return !(obj is Wrappers.Link); }) != null;
			d_normalGroup.GetAction("EditGroupAction").Sensitive = singlegroup;
			
			d_normalGroup.GetAction("PropertiesAction").Sensitive = singleobj;
			d_normalGroup.GetAction("PasteAction").Sensitive = d_grid.HasFocus;
			
			// Disable control for now
			d_normalGroup.GetAction("ControlMenuAction").Visible = false;
			d_normalGroup.GetAction("ViewControlAction").Visible = false;
			
			d_normalGroup.GetAction("RevertAction").Sensitive = d_filename != null;
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
			
			int sw = Screen.Width;
			int sh = Screen.Height;
			
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
			
			w.Move(Utils.Max(new int [] {Utils.Min(new int[] {nx, sw - ww}), 0}),
			       Utils.Max(new int [] {Utils.Min(new int[] {ny, sh - wh}), 0}));
		}
		
		private void DoObjectActivated(object source, Wrappers.Wrapper obj)
		{			
			if (d_propertyEditors.ContainsKey(obj))
			{
				d_propertyEditors[obj].Present();
				return;
			}
			
			Dialogs.Property dlg = new Dialogs.Property(this, obj);
			PositionWindow(dlg);
			
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
		
		private Cpg.Property[] CommonProperties(Wrappers.Wrapper[] objects)
		{
			if (objects.Length == 0)
			{
				return new Cpg.Property[] {};
			}
			
			Cpg.Property[] props = objects[0].Properties;
			int i = 1;
			
			while (props.Length > 0 && i < objects.Length)
			{
				Cpg.Property[] pp = objects[i].Properties;
				
				// TODO
				props = Array.FindAll(props, delegate (Cpg.Property s) { return Utils.In(s, pp); });
				++i;
			}
			
			return props;
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
			
			foreach (Cpg.Property prop in CommonProperties(selection))
			{
				string name = "Monitor" + prop.Name;
				
				d_popupActionGroup.Add(new ActionEntry[] {
					new ActionEntry(name + "Action", null, prop.Name.Replace("_", "__"), null, null, delegate (object s, EventArgs a) {
						OnStartMonitor(selection, prop);
					})
				});
				
				d_uimanager.AddUi(d_popupMergeId, "/popup/MonitorMenu/MonitorPlaceholder", name, name + "Action", UIManagerItemType.Menuitem, false);
			}
			
			Widget menu = d_uimanager.GetWidget("/popup");
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
				d_vpaned.Position = value;
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
			
			d_filename = null;
			d_modified = false;
			
			UpdateSensitivity();
			UpdateTitle();
		}
		
		public void DoLoadXml(string filename)
		{
			// TODO
			/*if (!AskUnsavedModified())
			{
				return;
			}
			
			Serialization.Main cpg;
			
			try
			{
				Serialization.Loader loader = new Serialization.Loader();
				cpg = loader.Load(filename);
			}
			catch (Exception e)
			{
				Message(Gtk.Stock.DialogError, "Error while loading network", e);

				return;
			}
			
			Clear();
			d_filename = filename;
			
			try
			{				
				d_network.Integrator = cpg.Network.CNetwork.Integrator;
			}
			catch (Exception e)
			{
				Message(Gtk.Stock.DialogError, "Error while loading network", e);
				
				Clear();

				d_modified = false;
				d_filename = null;

				UpdateTitle();
				return;
			}
			
			// Load project settings
			Allocation alloc = cpg.Project.Allocation;

			d_grid.Container.X = cpg.Project.Root.X;
			d_grid.Container.Y = cpg.Project.Root.Y;
						
			// Select container
			if (!String.IsNullOrEmpty(cpg.Project.Container))
				d_grid.LevelDown(cpg.Project.Container);

			d_grid.GridSize = cpg.Project.Zoom;

			if (!String.IsNullOrEmpty(cpg.Project.Period))
			{
				d_periodEntry.Text = cpg.Project.Period;
				d_simulation.Range = new Range(cpg.Project.Period);
			}
			
			if (alloc != null)
			{
				if (!Visible)
				{
					Move((int)alloc.X, (int)alloc.Y);
				}
				
				Resize((int)alloc.Width, (int)alloc.Height);
			}
			
			ToggleAction action = d_normalGroup.GetAction("ViewPropertyEditorAction") as ToggleAction;
			
			if (cpg.Project.PanePosition == -1)
			{
				action.Active = false;
			}
			else
			{
				action.Active = true;

				d_vpaned.Position = d_vpaned.Allocation.Height - cpg.Project.PanePosition;
			}
			
			(d_normalGroup.GetAction("ViewToolbarAction") as ToggleAction).Active = cpg.Project.ShowToolbar;
			(d_normalGroup.GetAction("ViewPathbarAction") as ToggleAction).Active = cpg.Project.ShowPathbar;
			(d_normalGroup.GetAction("ViewSimulateButtonsAction") as ToggleAction).Active = cpg.Project.ShowSimulateButtons;
			(d_normalGroup.GetAction("ViewStatusbarAction") as ToggleAction).Active = cpg.Project.ShowStatusbar;

			d_modified = false;
			UpdateTitle();
			
			StatusMessage("Loaded network from " + filename + "...");*/
		}
		
		private void OnOpenActivated(object sender, EventArgs args)
		{
			FileChooserDialog dlg = new FileChooserDialog("Open",
			                                              this,
			                                              FileChooserAction.Open,
			                                              Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			                                              Gtk.Stock.Open, Gtk.ResponseType.Accept);
		
			if (d_filename != null)
			{
				dlg.SetCurrentFolder(System.IO.Path.GetDirectoryName(d_filename));
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
					DoLoadXml(filename);		
			};
			
			dlg.Show();
		}
		
		private void OnRevertActivated(object sender, EventArgs args)
		{
			string filename = d_filename;
			
			Clear();
			d_modified = false;
			
			if (filename != null)
				DoLoadXml(filename);
		}
		
		private FileChooserDialog DoSave()
		{
			if (d_filename != null)
			{
				DoSaveXml(d_filename);
				return null;
			}
			else
			{
				return DoSaveAs();
			}
		}
		
		private void OnSaveActivated(object sender, EventArgs args)
		{
			DoSave();
		}
		
		private bool ExportXml(string filename, List<Wrappers.Wrapper> objects)
		{
			Serialization.Saver saver;
			
			if (objects == null)
			{
				saver = new Serialization.Saver(this, d_grid.ActiveGroup);
			}
			else
			{
				saver = new Serialization.Saver(this, objects);
			}
			
			try
			{
				saver.Save(filename);
				return true;
			}
			catch (Exception e)
			{
				Message(Gtk.Stock.DialogError, "Error while saving network", e);
				return false;
			}
		}
		
		private bool ExportXml(string filename)
		{
			return ExportXml(filename, null);
		}
		
		public void StatusMessage(string message)
		{
			d_statusbar.Push(0, message);
			
			if (d_statusTimeout != 0)
				GLib.Source.Remove(d_statusTimeout);
			
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
				exception = exception.InnerException;
			
			Message(icon, primary, exception.Message);
			
			if (d_messageArea != null)
				d_messageArea.TooltipText = exception.StackTrace;
		}
		
		private bool DoSaveXml(string filename)
		{
			if (ExportXml(filename))
			{
				StatusMessage("Saved " + filename + "...");
				
				d_modified = false;
				d_filename = filename;
				
				UpdateTitle();
				return true;
			}
			else
			{
				return false;
			}
		}
		
		private FileChooserDialog DoSaveAs()
		{
			FileChooserDialog dlg = new FileChooserDialog("Save As",
			                                              this,
			                                              FileChooserAction.Save,
			                                              Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			                                              Gtk.Stock.Save, Gtk.ResponseType.Accept);

			if (d_filename != null)
			{
				dlg.SetCurrentFolder(System.IO.Path.GetDirectoryName(d_filename));
			}
			else if (d_prevOpen != null)
			{
				dlg.SetCurrentFolder(d_prevOpen);
			}
			
			dlg.Response += delegate(object o, ResponseArgs args) {
					d_prevOpen = dlg.CurrentFolder;

					if (args.ResponseId == ResponseType.Accept)
					{
						string filename = dlg.Filename;
						
						if (DoSaveXml(filename))
						{
							d_filename = filename;
							UpdateSensitivity();
							UpdateTitle();
						}
					}
					
					dlg.Destroy();		
			};
			
			dlg.Show();
			
			return dlg;
		}
		
		
		private void OnSaveAsActivated(object sender, EventArgs args)
		{
			DoSaveAs();
		}
		
		/*private void OnImportActivated(object sender, EventArgs args)
		{
			FileChooserDialog dlg = new FileChooserDialog("Import network",
			                                              this,
			                                              FileChooserAction.Open,
			                                              Gtk.Stock.Cancel, ResponseType.Cancel,
			                                              Gtk.Stock.Open, ResponseType.Accept);

			if (d_filename != null)
			{
				dlg.SetCurrentFolder(System.IO.Path.GetDirectoryName(d_filename));
			}
			else if (d_prevOpen != null)
			{
				dlg.SetCurrentFolder(d_prevOpen);
			}

			dlg.Response += delegate(object o, ResponseArgs a) {
				d_prevOpen = dlg.CurrentFolder;

				if (a.ResponseId == ResponseType.Accept)
				{
					ImportFromFile(dlg.Filename);
				}
				
				dlg.Destroy();
			};
			
			dlg.Show();
		}*/
		
		/*private string ExportToXml(Wrappers.Wrapper[] objects)
		{
			Serialization.Saver saver;
			
			if (objects.Length != 0)
			{
				List<Wrappers.Wrapper> objs = new List<Wrappers.Wrapper>();
				objs.AddRange(objects);
				
				List<Wrappers.Wrapper> normalized = d_grid.Normalize(objs);
				
				if (normalized.Count == 0)
					return null;

				saver = new Serialization.Saver(this, normalized);
			}
			else
			{
				saver = new Serialization.Saver(this, d_grid.Root);
			}
			
			string doc;
			
			try
			{
				doc = saver.Save();
			}
			catch (Exception e)
			{
				Message(Gtk.Stock.DialogError, "Error while exporting network", e);
				return null;
			}
			
			return doc;
		}*/
		
		/*private void OnExportActivated(object sender, EventArgs args)
		{
			FileChooserDialog dlg = new FileChooserDialog("Save As",
			                                              this,
			                                              FileChooserAction.Save,
			                                              Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			                                              Gtk.Stock.Save, Gtk.ResponseType.Accept);

			if (d_filename != null)
			{
				dlg.SetCurrentFolder(System.IO.Path.GetDirectoryName(d_filename));
			}
			else if (d_prevOpen != null)
			{
				dlg.SetCurrentFolder(d_prevOpen);
			}
			
			string doc = ExportToXml(d_grid.Selection);
			
			if (doc == null)
				return;
			
			dlg.Response += delegate(object o, ResponseArgs a) {
				d_prevOpen = dlg.CurrentFolder;

				if (a.ResponseId == ResponseType.Accept)
				{
					string filename = dlg.Filename;
					
					try
					{
						Serialization.Saver.SaveToFile(filename, doc);
						
						StatusMessage("Exported network to " + filename + "...");
					}
					catch (Exception e)
					{
						Message(Gtk.Stock.DialogError, "Error while exporting network", e);
					}
				}
				
				dlg.Destroy();		
			};
			
			dlg.Show();
		}*/
		
		private void OnQuitActivated(object sender, EventArgs args)
		{
			Gtk.Application.Quit();
		}
		
		private void OnPasteActivated(object sender, EventArgs args)
		{
			int[] center = d_grid.Center;
			d_actions.Paste(d_grid.ActiveGroup, d_grid.Selection, center[0], center[1]);
		}
		
		private void OnGroupActivated(object sender, EventArgs args)
		{
			d_actions.Group();
		}
		
		private void OnUngroupActivated(object sender, EventArgs args)
		{
			d_actions.Ungroup();
		}
		
		private void OnAddStateActivated(object sender, EventArgs args)
		{
			int[] center = d_grid.Center;
			d_actions.AddState(d_grid.ActiveGroup, center[0], center[1]);
		}
		
		private void OnAddLinkActivated(object sender, EventArgs args)
		{
			d_actions.AddLink(d_grid.ActiveGroup, d_grid.Selection);
		}
		
		private void OnUndoActivated(object sender, EventArgs args)
		{
			d_undoManager.Undo();
			d_grid.QueueDraw();
		}
		
		private void OnRedoActivated(object sender, EventArgs args)
		{
			d_undoManager.Redo();
			d_grid.QueueDraw();
		}

		private void OnEditGlobalsActivated(object sender, EventArgs args)
		{
			DoObjectActivated(sender, d_network);
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
				DoObjectActivated(sender, selection[0]);
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
			
			d_hboxPath.Visible = action.Active;
		}
		
		private void OnViewSimulateButtonsActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			d_simulateButtons.Visible = action.Active;
		}
		
		private void OnViewPropertyEditorActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			if (action.Active)
			{
				if (d_propertyView == null)
				{
					Wrappers.Wrapper[] selection = d_grid.Selection;
					d_propertyView = new PropertyView(selection.Length == 1 ? selection[0] : null);
					d_propertyView.BorderWidth = 3;
					
					d_propertyView.Error += delegate (object source, Exception exception)
					{
						Message(Gtk.Stock.DialogInfo, "Error while editing property", exception);
					};
					
					d_vpaned.Pack2(d_propertyView, false, false);
				}
				
				d_propertyView.ShowAll();
			}
			else
			{
				if (d_propertyView != null)
					d_propertyView.Destroy();
				
				d_propertyView = null;
			}
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
				return;
			
			d_monitor = new Monitor(d_network, d_simulation);
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
			d_actions.Cut(d_grid.ActiveGroup, d_grid.Selection);
		}
		
		private void OnCopyActivated(object sender, EventArgs args)
		{
			d_actions.Copy(d_grid.Selection);
		}
		
		private void OnDeleteActivated(object sender, EventArgs args)
		{
			d_actions.Delete(d_grid.ActiveGroup, d_grid.Selection);
		}
		
		private void DoError(object sender, string error, string message)
		{
			Message(Gtk.Stock.DialogError, error, message);
		}
		
		private void OnStartMonitor(Wrappers.Wrapper[] objs, Cpg.Property property)
		{
			EnsureMonitor();
			
			foreach (Wrappers.Wrapper obj in objs)
			{
				d_monitor.AddHook(obj, property);
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
				title += "»" + error.LinkAction.Target.Name;
				expression = error.LinkAction.Expression.AsString;
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
			        error.String() + ": " + error.Message + "\nExpression: \"" + expression + "\"");
		}
		
		private void OnCompileError(object sender, Cpg.CompileErrorArgs args)
		{
			HandleCompileError(args.Error);
		}
		
		public Wrappers.Network Network
		{
			get
			{
				return d_network;
			}
		}
		
		private void ShowFunctions()
		{
			if (d_functionsDialog == null)
			{
				d_functionsDialog = new Dialogs.Functions(this, d_network);

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
	}
}
