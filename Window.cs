using System;
using Gtk;

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
		
		public Window() : base (Gtk.WindowType.Toplevel)
		{
			Build();
			ShowAll();
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
			d_vpaned.Position = 0;
			d_vpaned.Pack1(d_grid, true, true);
			
			//do_property_editor(@normal_group.get_action("PropertyEditorAction"))
			
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
			
			/*
			@grid.signal_connect('activated') do |grid, obj|
				do_object_activated(obj)
			end
		
			@grid.signal_connect('popup') do |grid, button, time|
				do_popup(button, time)
			end
		
			@grid.signal_connect('object_removed') do |grid, obj|
				if @property_editors.include?(obj)
					@property_editors[obj].destroy
				end
			end
		
			@grid.signal_connect('level_down') do |grid, obj|
				handle_level_down(obj)
			end
		
			@grid.signal_connect('level_up') do |grid, obj|
				handle_level_up(obj)
			end
			
			@grid.signal_connect('modified') do |grid|
				if !@modified
					@modified = true
					update_title
				end
			end
			
			@grid.signal_connect('selection_changed') do |grid|
				if @propertyview
					if @grid.selection.length == 1
						@propertyview.init(@grid.selection.first)
					else
						@propertyview.init(nil)
					end
				end
				
				update_sensitivity
			end
			
			@grid.signal_connect('focus-out-event') do |grid, event|
				update_sensitivity
			end
			
			@grid.signal_connect('focus-in-event') do |grid, event|
				update_sensitivity
			end */
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
		
		private void BuildTemplates(UIManager manager)
		{
		}
		
		/* Callbacks */
		protected override bool OnDeleteEvent(Gdk.Event evt)
		{
			Console.Out.WriteLine("Quitting");
			Gtk.Application.Quit();
			return true;
		}
			
		private void OnFileNew(object obj, EventArgs args)
		{
				new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, "Hello World").Run();
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
		}
		
		private void OnUngroupActivated(object sender, EventArgs args)
		{
		}
		
		private void OnAddStateActivated(object sender, EventArgs args)
		{
		}
		
		private void OnAddLinkActivated(object sender, EventArgs args)
		{
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
		}
		
		private void OnEditGroupActivated(object sender, EventArgs args)
		{
		}
		
		private void OnPropertiesActivated(object sender, EventArgs args)
		{
		}
		
		private void OnPropertyEditorActivated(object sender, EventArgs args)
		{
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