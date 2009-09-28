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
		private PropertyView d_propertyView;
		private MessageArea d_messageArea;
		private uint d_statusTimeout;
		private uint d_popupMergeId;
		private UIManager d_uimanager;
		private ActionGroup d_popupActionGroup;
		private Dictionary<Components.Object, PropertyDialog> d_propertyEditors;
		private Components.Network d_network;
		private Monitor d_monitor;
		private Simulation d_simulation;
		private string d_prevOpen;
		
		private bool d_modified;
		private string d_filename;

		public Window() : base (Gtk.WindowType.Toplevel)
		{
			d_network = new Components.Network();
			
			d_network.CompileError += OnCompileError;
			d_simulation = new Simulation(d_network);

			Build();
			ShowAll();
			
			Clear();
			
			d_modified = false;
			UpdateTitle();
			
			d_propertyEditors = new Dictionary<Components.Object, PropertyDialog>();
		}
		
		private void Build()
		{
			SetDefaultSize(700, 600);
			
			d_uimanager = new UIManager();
			d_normalGroup = new ActionGroup("NormalActions");

			d_normalGroup.Add(new ActionEntry[] {
				new ActionEntry("FileMenuAction", null, "_File", null, null, null),
				new ActionEntry("NewAction", Gtk.Stock.New, null, "<Control>N", "New CPG network", new EventHandler(OnFileNew)),
				new ActionEntry("OpenAction", Gtk.Stock.Open, null, "<Control>O", "Open CPG network", new EventHandler(OnOpenActivated)),
				new ActionEntry("RevertAction", Gtk.Stock.RevertToSaved, null, null, "Revert changes", new EventHandler(OnRevertActivated)),
				new ActionEntry("SaveAction", Gtk.Stock.Save, null, "<Control>S", "Save CPG network", new EventHandler(OnSaveActivated)),
				new ActionEntry("SaveAsAction", Gtk.Stock.SaveAs, null, "<Control><Shift>S", "Save CPG network", new EventHandler(OnSaveAsActivated)),

				new ActionEntry("ImportAction", null, "Import", null, "Import CPG network objects", new EventHandler(OnImportActivated)),
				new ActionEntry("ExportAction", null, "Export", "<Control>e", "Export CPG network objects", new EventHandler(OnExportActivated)),

				new ActionEntry("QuitAction", Gtk.Stock.Quit, null, "<Control>Q", "Quit", new EventHandler(OnQuitActivated)),

				new ActionEntry("EditMenuAction", null, "_Edit", null, null, null),
				new ActionEntry("PasteAction", Gtk.Stock.Paste, null, "<Control>V", "Paste objects", new EventHandler(OnPasteActivated)),
				new ActionEntry("GroupAction", null, "Group", "<Control>g", "Group objects", new EventHandler(OnGroupActivated)),
				new ActionEntry("UngroupAction", null, "Ungroup", "<Control>u", "Ungroup object", new EventHandler(OnUngroupActivated)),
				new ActionEntry("EditGlobalsAction", null, "Global Constants", null, "Edit the global constants of the network", new EventHandler(OnEditGlobalsActivated)),

				new ActionEntry("AddStateAction", Studio.Stock.State, null, null, "Add state", new EventHandler(OnAddStateActivated)),
				new ActionEntry("AddLinkAction", Studio.Stock.Link, null, null, "Link objects", new EventHandler(OnAddLinkActivated)),
				new ActionEntry("AddRelayAction", Studio.Stock.Relay, null, null, "Add relay", new EventHandler(OnAddRelayActivated)),

				new ActionEntry("SimulateMenuAction", null, "_Simulate", null, null, null),
				new ActionEntry("StepAction", Gtk.Stock.MediaNext, "Step", "<Control>t", "Execute one simulation step", new EventHandler(OnStepActivated)),
				new ActionEntry("SimulateAction", Gtk.Stock.MediaForward, "Period", "<Control>p", "(Re)Simulate period", new EventHandler(OnSimulateActivated)),
				
				new ActionEntry("ViewMenuAction", null, "_View", null, null, null),
				new ActionEntry("CenterAction", Gtk.Stock.JustifyCenter, null, "<Control>h", "Center view", new EventHandler(OnCenterViewActivated)),
				new ActionEntry("InsertMenuAction", null, "_Insert", null, null, null),
				new ActionEntry("ZoomDefaultAction", Gtk.Stock.Zoom100, null, "<Control>1", null, new EventHandler(OnZoomDefaultActivated)),
				new ActionEntry("ZoomInAction", Gtk.Stock.ZoomIn, null, "<Control>plus", null, new EventHandler(OnZoomInActivated)),
				new ActionEntry("ZoomOutAction", Gtk.Stock.ZoomOut, null, "<Control>minus", null, new EventHandler(OnZoomOutActivated)),

				new ActionEntry("MonitorMenuAction", null, "Monitor", null, null, null),
				new ActionEntry("ControlMenuAction", null, "Control", null, null, null),
				new ActionEntry("PropertiesAction", null, "Properties", null, null, new EventHandler(OnPropertiesActivated)),
				new ActionEntry("EditGroupAction", null, "Edit group", null, null, new EventHandler(OnEditGroupActivated))
			});
			
			d_normalGroup.Add(new ToggleActionEntry[] {
				new ToggleActionEntry("PropertyEditorAction", Gtk.Stock.Properties, "Property Editor", "<Control>F9", "Show/Hide property editor pane", new EventHandler(OnPropertyEditorActivated), true),
				new ToggleActionEntry("ViewMonitorAction", null, "Monitor", "<Control>m", "Show/Hide monitor window", new EventHandler(OnToggleMonitorActivated), false),
				new ToggleActionEntry("ViewControlAction", null, "Control", "<Control>k", "Show/Hide control window", new EventHandler(OnToggleControlActivated), false)		
			});
				
			d_uimanager.InsertActionGroup(d_normalGroup, 0);
			
			d_selectionGroup = new ActionGroup("SelectionActions");
			d_selectionGroup.Add(new ActionEntry[] {
				new ActionEntry("CutAction", Gtk.Stock.Cut, null, "<Control>X", "Cut objects", new EventHandler(OnCutActivated)),
				new ActionEntry("CopyAction", Gtk.Stock.Copy, null, "<Control>C", "Copy objects", new EventHandler(OnCopyActivated)),
				new ActionEntry("DeleteAction", Gtk.Stock.Delete, null, null, "Delete object", new EventHandler(OnDeleteActivated))			
			});
			
			d_uimanager.InsertActionGroup(d_selectionGroup, 0);
			d_uimanager.AddUiFromResource("ui.xml");
			
			BuildTemplates(d_uimanager);
			
			AddAccelGroup(d_uimanager.AccelGroup);
			
			VBox vbox = new VBox(false, 0);

			vbox.PackStart(d_uimanager.GetWidget("/menubar"), false, false, 0);
			vbox.PackStart(d_uimanager.GetWidget("/toolbar"), false, false, 0);
			
			d_hboxPath = new HBox(false, 0);
			d_hboxPath.BorderWidth = 1;
			vbox.PackStart(d_hboxPath, false, false, 0);
			
			d_vboxContents = new VBox(false, 3);
			vbox.PackStart(d_vboxContents, true, true, 0);
			
			d_grid = new Grid(d_network);
			d_vpaned = new VPaned();
			d_vpaned.Position = 300;
			d_vpaned.Pack1(d_grid, true, true);
			
			OnPropertyEditorActivated(d_normalGroup.GetAction("PropertyEditorAction"), new EventArgs());
			
			d_vboxContents.PackStart(d_vpaned, true, true, 0);
			
			d_periodEntry = new Entry();
			d_periodEntry.SetSizeRequest(75, -1);
			d_periodEntry.Activated += new EventHandler(OnSimulationRunPeriod);

			HBox hbox = new HBox(false, 3);
			hbox.BorderWidth = 3;
			BuildButtonBar(hbox);
			
			d_vboxContents.PackStart(hbox, false, false, 0);
			
			d_statusbar = new Statusbar();
			d_statusbar.Show();
			vbox.PackStart(d_statusbar, false, false, 0);
			
			Add(vbox);
			
			d_grid.Activated += DoObjectActivated;
			d_grid.Popup += DoPopup;
			d_grid.ObjectRemoved += DoObjectRemoved;
			d_grid.LeveledDown += DoLevelDown;
			d_grid.LeveledUp += DoLevelUp;
			d_grid.Modified += DoModified;
			d_grid.SelectionChanged += DoSelectionChanged;
			d_grid.FocusInEvent += DoFocusInEvent;
			d_grid.FocusOutEvent += DoFocusOutEvent;
			d_grid.Error += DoError;
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
		
		private void TraverseTemplates(UIManager manager, ActionGroup group, uint mid, string path, string parent)
		{
			if (!Directory.Exists(path))
				return;
			
			string pname = parent.Replace("/", "");
			List<string> paths = new List<string>();
			
			paths.AddRange(Directory.GetFiles(path));
			paths.AddRange(Directory.GetDirectories(path));
			
			paths.Sort();
			
			foreach (string file in paths)
			{
				string part = System.IO.Path.GetFileName(file);
				string capitalized = Utils.Capitalize(part);
				
				if (Directory.Exists(file))
				{
					string name = pname + capitalized + "Menu";
					
					group.Add(new Action(name + "Action", capitalized.Replace("_", "__"), "", ""));
					manager.AddUi(mid, "/menubar/InsertMenu/" + parent, name, name + "Action", UIManagerItemType.Menu, false);
					manager.AddUi(mid, "/popup/InsertMenu/" + parent, name, name + "Action", UIManagerItemType.Menu, false);
					
					TraverseTemplates(manager, group, mid, file, parent + "/" + name);
				}
				else if (File.Exists(file) && System.IO.Path.GetExtension(file) == ".cpg")
				{
					string name = Regex.Replace(Regex.Replace(capitalized, ".cpg$", ""), "[.-:]+", "").Replace("_", "__");
					Action action = new Action(pname + name + "Action", name, "", "");
					
					group.Add(action);
					string importFile = file;

					action.Activated += delegate (object source, EventArgs args)
					{
						Console.WriteLine("Import: " + importFile);
						ImportFromFile(importFile); 
					};
					
					manager.AddUi(mid, "/menubar/InsertMenu/" + parent, pname + name, pname + name + "Action", UIManagerItemType.Menuitem, false);
					manager.AddUi(mid, "/popup/InsertMenu/" + parent, pname + name, pname + name + "Action", UIManagerItemType.Menuitem, false);
				}
			}
		}
		
		private void BuildTemplates(UIManager manager)
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			string dname = System.IO.Path.GetDirectoryName(asm.Location);
			string tdir = System.IO.Path.Combine(dname, "templates");
			
			ActionGroup group = new ActionGroup("TemplateActions");
			uint mid = manager.NewMergeId();
			
			manager.InsertActionGroup(group, 0);
			
			TraverseTemplates(manager, group, mid, tdir, "InsertTemplates");
		}
		
		delegate void PathHandler();
		
		private void PushPath(Components.Object obj, PathHandler handler)
		{
			if (obj != null)
			{
				Arrow arrow = new Arrow(ArrowType.Right, ShadowType.None);
				arrow.Show();
				d_hboxPath.PackStart(arrow, false, false, 0);
			}
			
			Button but = new Button(obj != null ? obj.ToString() : "(cpg)");
			but.Relief = ReliefStyle.None;
			but.Show();
			
			d_hboxPath.PackStart(but, false, false, 0);
			
			if (handler != null)
				but.Clicked += delegate(object source, EventArgs args) { handler(); };
			
			if (obj != null)
			{
				obj.PropertyChanged += delegate (Components.Object source, string name) {
					but.Label = obj.ToString();
				};
			}
		}
		
		private void PopPath()
		{
			d_hboxPath.Children[d_hboxPath.Children.Length - 1].Destroy();
			d_hboxPath.Children[d_hboxPath.Children.Length - 1].Destroy();
		}
		
		private void Clear()
		{
			d_grid.Clear();
			
			while (d_hboxPath.Children.Length > 0)
				d_hboxPath.Remove(d_hboxPath.Children[0]);
			
			PushPath(null, delegate() { 
				d_grid.LevelUp(d_grid.Root);
			});
			
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
				foreach (PropertyDialog dlg in d_propertyEditors.Values)
				{
					dlg.Destroy();
				}
			}
			
			d_periodEntry.Text = "0:0.01:1";
			d_grid.GrabFocus();
			
			d_simulation.Range = new Range(d_periodEntry.Text);
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
			List<Components.Object> objects = new List<Components.Object>(d_grid.Selection);
			
			d_selectionGroup.Sensitive = objects.Count > 0 && d_grid.HasFocus;
			
			bool singleobj = objects.Count == 1;
			bool singlegroup = singleobj && objects[0] is Components.Group;
			int anygroup = objects.FindAll(delegate (Components.Object obj) { return obj is Components.Group; }).Count;
			
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
			
			d_normalGroup.GetAction("GroupAction").Sensitive = objects.Count > 1 && objects.Find(delegate (Components.Object obj) { return !(obj is Components.Link); }) != null;
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
		
		private void DoObjectActivated(object source, Components.Object obj)
		{			
			if (d_propertyEditors.ContainsKey(obj))
			{
				d_propertyEditors[obj].Present();
				return;
			}
			
			PropertyDialog dlg = new PropertyDialog(this, obj);
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
		
		private string[] VisibleProperties(Components.Object obj)
		{
			return Array.FindAll(obj.Properties, delegate (string s) { 
				return !obj.IsInvisible(s) && (!(obj is Components.Simulated) || (obj as Components.Simulated).SimulatedProperty(s));
			});
		}
		
		private string[] CommonProperties(Components.Object[] objects)
		{
			if (objects.Length == 0)
			{
				return new string[] {};
			}
			
			string[] props = VisibleProperties(objects[0]);
			int i = 1;
			
			while (props.Length > 0 && i < objects.Length)
			{
				string[] pp = VisibleProperties(objects[i]);
				
				props = Array.FindAll(props, delegate (string s) { return Utils.In(s, pp); });
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
			Components.Object[] selection = d_grid.Selection;
			string[] props = CommonProperties(selection);
			
			for (int i = 0; i < props.Length; ++i)
			{
				if (props[i] == "id")
					continue;
				
				string name = "Monitor" + props[i];
				string prop = props[i];
				
				d_popupActionGroup.Add(new ActionEntry[] {
					new ActionEntry(name + "Action", null, props[i].Replace("_", "__"), null, null, delegate (object s, EventArgs a) {
						OnStartMonitor(selection, prop);
					})
				});
				
				d_uimanager.AddUi(d_popupMergeId, "/popup/MonitorMenu/MonitorPlaceholder", name, name + "Action", UIManagerItemType.Menuitem, false);
			}
			
			Widget menu = d_uimanager.GetWidget("/popup");
			menu.ShowAll();
			(menu as Menu).Popup(null, null, null, (uint)button, (uint)time);
		}
		
		private void DoLevelDown(object source, Components.Object obj)
		{
			PushPath(obj, delegate () { d_grid.LevelUp(obj); });
		}
		
		private void DoLevelUp(object source, Components.Object obj)
		{
			PopPath();
		}
		
		private void DoObjectRemoved(object source, Components.Object obj)
		{
			if (d_propertyEditors.ContainsKey(obj))
			{
				d_propertyEditors[obj].Destroy();
			}
		}
		
		private void DoModified(object source, EventArgs args)
		{
			if (!d_modified)
			{
				d_modified = true;
				UpdateTitle();
			}
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			if (d_propertyView != null)
			{
				Components.Object[] selection = d_grid.Selection;
				
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
		
		private void ImportSuccess(Serialization.Cpg cpg, bool centerPosition)
		{			
			// Merge C network
			d_network.Merge(cpg.Network.CNetwork);

			// Add wrappers to grid, position them if necessary
			List<Components.Object> objects = new List<Components.Object>();
			
			foreach (Serialization.Object obj in cpg.Project.Root.Children)
			{
				objects.Add(obj.Obj);
			}
			
			double x, y;
			Utils.MeanPosition(objects, out x, out y);
			
			double cx = (d_grid.Container.X + (d_grid.Allocation.Width / 2.0)) / (float)d_grid.GridSize;
			double cy = (d_grid.Container.Y + (d_grid.Allocation.Height / 2.0)) / (float)d_grid.GridSize;
			
			foreach (Components.Object obj in objects)
			{
				if (!(obj is Components.Link) && centerPosition)
				{
					obj.Allocation.X = (float)Math.Round(cx + (obj.Allocation.X - x) + 0.00001);
					obj.Allocation.Y = (float)Math.Round(cy + (obj.Allocation.Y - y) + 0.00001);
				}
				
				d_grid.AddObject(obj);
			}
			
			// Compile the network right now
			Cpg.CompileError err = new Cpg.CompileError();
			
			if (!d_network.Compile(err))
			{
				HandleCompileError(err);
			}
		}
		
		private void ImportFail(Exception e)
		{
			Message(Gtk.Stock.DialogError, "Error while importing network", e);
		}

		private void ImportFromXml(string xml)
		{
			try
			{
				Serialization.Loader loader = new Serialization.Loader();
				ImportSuccess(loader.LoadXml(xml), true);
			}
			catch (Exception e)
			{
				ImportFail(e);
			}
		}

		private void ImportFromFile(string file)
		{
			try
			{
				Serialization.Loader loader = new Serialization.Loader();
				ImportSuccess(loader.Load(file), true);
				
				StatusMessage("Imported network from " + file + "...");
			}
			catch (Exception e)
			{
				ImportFail(e);
			}			
		}
		
		private void AskUnsavedModified()
		{
			if (!d_modified)
				return;
			
			MessageDialog dlg = new MessageDialog(this,
			                                      DialogFlags.DestroyWithParent | DialogFlags.Modal,
			                                      MessageType.Warning,
			                                      ButtonsType.YesNo,
			                                      "There are unsaved changes in the current network, do you want to save these changes first?");
		
			int resp = dlg.Run();
			
			if (resp == (int)ResponseType.Yes)
			{
				FileChooserDialog d = DoSave();
				
				if (d != null)
					d.Run();
			}
			
			dlg.Destroy();
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
		
		/* Callbacks */
		protected override bool OnDeleteEvent(Gdk.Event evt)
		{
			AskUnsavedModified();
			Gtk.Application.Quit();
			return true;
		}
			
		private void OnFileNew(object obj, EventArgs args)
		{
			AskUnsavedModified();

			Clear();
			
			d_filename = null;
			d_modified = false;
			
			UpdateSensitivity();
			UpdateTitle();
		}
		
		public void DoLoadXml(string filename)
		{
			AskUnsavedModified();
			
			Serialization.Cpg cpg;
			
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
				ImportSuccess(cpg, false);
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
						
			/* Select container */
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
			
			ToggleAction action = d_normalGroup.GetAction("PropertyEditorAction") as ToggleAction;
			
			if (cpg.Project.PanePosition == -1)
			{
				action.Active = false;
			}
			else
			{
				action.Active = true;

				d_vpaned.Position = d_vpaned.Allocation.Height - cpg.Project.PanePosition;
			}
			
			d_modified = false;
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
		
		private bool ExportXml(string filename, List<Components.Object> objects)
		{
			Serialization.Saver saver;
			
			if (objects == null)
			{
				saver = new Serialization.Saver(this, d_grid.Root);
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
		
		private void OnImportActivated(object sender, EventArgs args)
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
		}
		
		private string ExportToXml(Components.Object[] objects)
		{
			Serialization.Saver saver;
			
			if (objects.Length != 0)
			{
				List<Components.Object> objs = new List<Components.Object>();
				objs.AddRange(objects);
				
				List<Components.Object> normalized = d_grid.Normalize(objs);
				
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
		}
		
		private void OnExportActivated(object sender, EventArgs args)
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
		}
		
		private void OnQuitActivated(object sender, EventArgs args)
		{
			Gtk.Application.Quit();
		}
		
		private void OnPasteActivated(object sender, EventArgs args)
		{
			Clipboard clip = Clipboard.Get(Gdk.Selection.Clipboard);
			
			clip.RequestText(delegate (Clipboard c, string text) {
				ImportFromXml(text);
			});
		}
		
		private void OnGroupActivated(object sender, EventArgs args)
		{
			d_grid.Group();
		}
		
		private void OnUngroupActivated(object sender, EventArgs args)
		{
			Components.Object[] objects = d_grid.Selection;

			foreach (Components.Object obj in objects)
			{
				if (obj is Components.Group)
					d_grid.Ungroup(obj as Components.Group);
			}
		}
		
		private void OnAddStateActivated(object sender, EventArgs args)
		{
			d_grid.Add(new Components.State());
		}
		
		private void OnAddLinkActivated(object sender, EventArgs args)
		{
			d_grid.Attach();
		}
		
		private void OnAddRelayActivated(object sender, EventArgs args)
		{
			d_grid.Add(new Components.Relay());
		}
		
		private void OnEditGlobalsActivated(object sender, EventArgs args)
		{
			DoObjectActivated(sender, new Components.Globals(d_network.Globals));
		}
		
		private void OnCenterViewActivated(object sender, EventArgs args)
		{
			d_grid.CenterView();
		}
		
		private void OnEditGroupActivated(object sender, EventArgs args)
		{
			Components.Object[] selection = d_grid.Selection;
			
			if (selection.Length != 1 || !(selection[0] is Components.Group))
				return;
			
			d_grid.LevelDown(selection[0]);
		}
		
		private void OnPropertiesActivated(object sender, EventArgs args)
		{
			Components.Object[] selection = d_grid.Selection;
			
			if (selection.Length != 0)
				DoObjectActivated(sender, selection[0]);
		}
		
		private void OnPropertyEditorActivated(object sender, EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;
			
			if (action.Active)
			{
				if (d_propertyView == null)
				{
					Components.Object[] selection = d_grid.Selection;
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
			
			d_monitor = new Monitor(d_grid, d_simulation);
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
		
		private bool DoCopy()
		{
			Components.Object[] objects = d_grid.Selection;
			
			if (objects.Length == 0)
				return false;
			
			string doc = ExportToXml(objects);
			
			if (doc == null)
				return false;
			
			Clipboard clip = Clipboard.Get(Gdk.Selection.Clipboard);
			clip.Text = doc;
			
			return true;
		}
		
		private void DoDelete()
		{
			d_grid.DeleteSelected();
		}
		
		private void OnCutActivated(object sender, EventArgs args)
		{
			if (!DoCopy())
				return;
			
			DoDelete();
		}
		
		private void OnCopyActivated(object sender, EventArgs args)
		{
			DoCopy();
		}
		
		private void OnDeleteActivated(object sender, EventArgs args)
		{
			DoDelete();
		}
		
		private void DoError(object sender, string error, string message)
		{
			Message(Gtk.Stock.DialogError, error, message);
		}
		
		private void OnStartMonitor(Components.Object[] objs, string property)
		{
			EnsureMonitor();
			
			foreach (Components.Object obj in objs)
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
				expression = error.Property.ValueExpression.AsString;
			}
			else
			{
				title += "»" + error.LinkAction.Target.Name;
				expression = error.LinkAction.Expression.AsString;
			}
		
			if (d_grid.Select(error.Object) && d_propertyView != null)
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
		
			Message(Gtk.Stock.DialogError, 
			        "Error while compiling " + title,
			        error.String() + ": " + error.Message + "\nExpression: \"" + expression + "\"");
		}
		
		private void OnCompileError(object sender, Cpg.CompileErrorArgs args)
		{
			HandleCompileError(args.Error);
		}
		
		public Components.Network Network
		{
			get
			{
				return d_network;
			}
		}
	}
}
