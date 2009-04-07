using System;
using Gtk;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cpg.Studio.GtkGui;

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
		
		private bool d_modified;
		private string d_filename;

		public Window() : base (Gtk.WindowType.Toplevel)
		{
			Build();
			ShowAll();
			
			Clear();
			
			d_modified = false;
			UpdateTitle();
		}
		
		private void Build()
		{
			SetDefaultSize(700, 600);
			
			UIManager manager = new UIManager();
			d_normalGroup = new ActionGroup("NormalActions");

			d_normalGroup.Add(new ActionEntry[] {
				new ActionEntry("FileMenuAction", null, "_File", null, null, null),
				new ActionEntry("NewAction", Gtk.Stock.New, null, null, "New CPG Network", new EventHandler(OnFileNew)),
				new ActionEntry("OpenAction", Gtk.Stock.Open, null, null, "Open CPG network", new EventHandler(OnOpenActivated)),
				new ActionEntry("RevertAction", Gtk.Stock.RevertToSaved, null, null, "Revert changes", new EventHandler(OnRevertActivated)),
				new ActionEntry("SaveAction", Gtk.Stock.Save, null, null, "Save CPG file", new EventHandler(OnSaveActivated)),
				new ActionEntry("SaveAsAction", Gtk.Stock.SaveAs, null, "<Control><Shift>S", "Save CPG file", new EventHandler(OnSaveAsActivated)),

				new ActionEntry("ImportAction", null, "Import", null, "Import CPG network objects", new EventHandler(OnImportActivated)),
				new ActionEntry("ExportAction", null, "Export", "<Control>e", "Export CPG network objects", new EventHandler(OnExportActivated)),

				new ActionEntry("ExportGridAction", null, "Export grid", null, "Export grid view", new EventHandler(OnExportGridActivated)),

				new ActionEntry("QuitAction", Gtk.Stock.Quit, null, null, "Quit", new EventHandler(OnQuitActivated)),

				new ActionEntry("EditMenuAction", null, "_Edit", null, null, null),
				new ActionEntry("PasteAction", Gtk.Stock.Paste, null, null, "Paste objects", new EventHandler(OnPasteActivated)),
				new ActionEntry("GroupAction", null, "Group", "<Control>g", "Group objects", new EventHandler(OnGroupActivated)),
				new ActionEntry("UngroupAction", null, "Ungroup", "<Control>u", "Ungroup object", new EventHandler(OnUngroupActivated)),
				new ActionEntry("ApplySettingsAction", null, "Apply settings", null, "Apply settings from a flat format", new EventHandler(OnApplySettingsActivated)),

				new ActionEntry("AddStateAction", Studio.Stock.State, null, null, "Add state", new EventHandler(OnAddStateActivated)),
				new ActionEntry("AddLinkAction", Studio.Stock.Link, null, null, "Link objects", new EventHandler(OnAddLinkActivated)),
				new ActionEntry("AddSensorAction", Studio.Stock.Sensor, null, null, "Add sensor", new EventHandler(OnAddSensorActivated)),
				new ActionEntry("AddRelayAction", Studio.Stock.Relay, null, null, "Add relay", new EventHandler(OnAddRelayActivated)),

				new ActionEntry("ViewMenuAction", null, "_View", null, null, null),
				new ActionEntry("CenterAction", Gtk.Stock.JustifyCenter, null, "<Control>h", "Center view", new EventHandler(OnCenterViewActivated)),
				new ActionEntry("InsertMenuAction", null, "_Insert", null, null, null),

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
				
			manager.InsertActionGroup(d_normalGroup, 0);
			
			d_selectionGroup = new ActionGroup("SelectionActions");
			d_selectionGroup.Add(new ActionEntry[] {
				new ActionEntry("CutAction", Gtk.Stock.Cut, null, null, "Cut objects", new EventHandler(OnCutActivated)),
				new ActionEntry("CopyAction", Gtk.Stock.Copy, null, null, "Copy objects", new EventHandler(OnCopyActivated)),
				new ActionEntry("DeleteAction", Gtk.Stock.Delete, null, null, "Delete object", new EventHandler(OnDeleteActivated))			
			});
			
			manager.InsertActionGroup(d_selectionGroup, 0);
			manager.AddUiFromResource("ui.xml");
			
			BuildTemplates(manager);
			
			AddAccelGroup(manager.AccelGroup);
			
			VBox vbox = new VBox(false, 0);

			vbox.PackStart(manager.GetWidget("/menubar"), false, false, 0);
			vbox.PackStart(manager.GetWidget("/toolbar"), false, false, 0);
			
			d_hboxPath = new HBox(false, 3);
			vbox.PackStart(d_hboxPath, false, false, 0);
			
			d_vboxContents = new VBox(false, 3);
			vbox.PackStart(d_vboxContents, true, true, 0);
			
			d_grid = new Grid();
			d_vpaned = new VPaned();
			d_vpaned.Position = 300;
			d_vpaned.Pack1(d_grid, true, true);
			
			OnPropertyEditorActivated(d_normalGroup.GetAction("PropertyEditorAction"), new EventArgs());
			
			d_vboxContents.PackStart(d_vpaned, true, true, 0);
			
			d_periodEntry = new Entry();
			d_periodEntry.SetSizeRequest(75, -1);
			d_periodEntry.Activated += new EventHandler(OnSimulationRunPeriod);

			HBox hbox = new HBox(false, 6);			
			BuildButtonBar(hbox);
			
			d_vboxContents.PackStart(hbox, false, false, 0);
			
			d_statusbar = new Statusbar();
			d_statusbar.Show();
			vbox.PackStart(d_statusbar, false, false, 3);
			
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

			hbox.PackStart(but, false, false, 0);
		
			but = new Button();
			but.Image = new Image(Gtk.Stock.MediaNext, IconSize.Button);
			but.Label = "Step";
			but.Clicked += new EventHandler(OnSimulationStep);
			hbox.PackEnd(but, false, false, 0);

			d_simulateButton = new ToggleButton();
			d_simulateButton.Image = new Image(Gtk.Stock.MediaPlay, IconSize.Button);
			d_simulateButton.Label = "Simulate";
			d_simulateButton.Toggled += new EventHandler(OnSimulationRun);
			hbox.PackEnd(d_simulateButton, false, false, 0);

			but = new Button();
			but.Image = new Image(Gtk.Stock.Clear, IconSize.Button);
			but.Label = "Reset";
			but.Clicked += new EventHandler(OnSimulationReset);
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
					action.Activated += delegate (object source, EventArgs args) { ImportFromFile(file); };
					
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
		}
		
		private void Clear()
		{
			d_grid.Clear();
			
			while (d_hboxPath.Children.Length > 0)
				d_hboxPath.Remove(d_hboxPath.Children[0]);
			
			PushPath(null, delegate() { 
				d_grid.LevelUp(d_grid.Root);
			});
			
			d_grid.GrabFocus();
		}
		
		private void UpdateTitle()
		{
			string extra = d_modified ? "*" : "";
			
			if (!String.IsNullOrEmpty(d_filename))
				Title = extra + System.IO.Path.GetFileName(d_filename) + " - CPG Studio";
			else
				Title = extra + "New Network - CPG Studio";
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
			
			//d_normalGroup.GetAction("Properties").Sensitive = singleobj;
			d_normalGroup.GetAction("PasteAction").Sensitive = d_grid.HasFocus;
		}
		
		private void DoObjectActivated(object source, Components.Object obj)
		{
			/*
			
			TODO
			
			if @property_editors.include?(object)
				@property_editors[object].present
				return
			end
		
			dlg = PropertyEditor.new(self, object)
			position_window(dlg)
			dlg.show

			@property_editors[object] = dlg
		
			dlg.signal_connect('response') do |dlg, res|
				@grid.queue_draw
				@property_editors.delete(object)
				dlg.destroy
			end*/
		}
		
		private void DoPopup(object source, int button, long time)
		{
			// TODO
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
			/*
			
			TODO
			
			if @property_editors.include?(obj)
				@property_editors[obj].destroy
			end*/
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
		
		private void ImportFromFile(string file)
		{
			// TODO
		}
		
		/* Callbacks */
		protected override bool OnDeleteEvent(Gdk.Event evt)
		{
			Gtk.Application.Quit();
			return true;
		}
			
		private void OnFileNew(object obj, EventArgs args)
		{
			
		}
		
		private void OnOpenActivated(object sender, EventArgs args)
		{
		}
		
		private void OnRevertActivated(object sender, EventArgs args)
		{
		}
		
		private void OnSaveActivated(object sender, EventArgs args)
		{
		}
		
		private void OnSaveAsActivated(object sender, EventArgs args)
		{
		}
		
		private void OnImportActivated(object sender, EventArgs args)
		{
		}
		
		private void OnExportActivated(object sender, EventArgs args)
		{
		}
		
		private void OnExportGridActivated(object sender, EventArgs args)
		{
		}
		
		private void OnQuitActivated(object sender, EventArgs args)
		{
			Gtk.Application.Quit();
		}
		
		private void OnPasteActivated(object sender, EventArgs args)
		{
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
		
		private void OnAddSensorActivated(object sender, EventArgs args)
		{
		}
		
		private void OnAddRelayActivated(object sender, EventArgs args)
		{
		}
		
		private void OnApplySettingsActivated(object sender, EventArgs args)
		{
		}
		
		private void OnCenterViewActivated(object sender, EventArgs args)
		{
			d_grid.CenterView();
		}
		
		private void OnEditGroupActivated(object sender, EventArgs args)
		{
		}
		
		private void OnPropertiesActivated(object sender, EventArgs args)
		{
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
		
		private void OnToggleMonitorActivated(object sender, EventArgs args)
		{
		}
		
		private void OnToggleControlActivated(object sender, EventArgs args)
		{
		}
		
		private void OnCutActivated(object sender, EventArgs args)
		{
		}
		
		private void OnCopyActivated(object sender, EventArgs args)
		{
		}
		
		private void OnDeleteActivated(object sender, EventArgs args)
		{
			d_grid.DeleteSelected();
		}
		
		private void OnSimulationRunPeriod(object sender, EventArgs args)
		{
		}
		
		private void OnSimulationStep(object sender, EventArgs args)
		{
		}
		
		private void OnSimulationRun(object sender, EventArgs args)
		{
		}
		
		private void OnSimulationReset(object sender, EventArgs args)
		{
		}
	}
}