using System;
using Gtk;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio.Widgets
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding")]
	[Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding")]
	public class PropertyView : VBox
	{
		private class LinkActionNode : Node
		{
			private LinkAction d_action;

			public LinkActionNode(LinkAction action)
			{
				d_action = action;
				
				d_action.AddNotification("target", OnActionChanged);
				d_action.AddNotification("equation", OnActionChanged);
			}
			
			~LinkActionNode()
			{
				d_action.RemoveNotification("target", OnActionChanged);
				d_action.RemoveNotification("equation", OnActionChanged);
			}
			
			private void OnActionChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[PrimaryKey]
			public LinkAction LinkAction
			{
				get
				{
					return d_action;
				}
			}
			
			[NodeColumn(0)]
			public string Target
			{
				get
				{
					return d_action.Target;
				}
				set
				{
					d_action.Target = value;
				}
			}
			
			[NodeColumn(1)]
			public string Equation
			{
				get
				{
					return d_action.Equation.AsString;
				}
				set
				{
					d_action.Equation.FromString = value;
				}
			}
		}

		enum Column
		{
			Property = 0
		}

		public delegate void ErrorHandler(object source, Exception exception);
		
		public event ErrorHandler Error = delegate {};
		
		private Wrappers.Wrapper d_object;
		private ListStore d_store;
		private TreeView d_treeview;
		private bool d_selectProperty;
		private ListStore d_comboStore;
		private NodeStore<LinkActionNode> d_actionStore;
		private TreeView d_actionView;
		private ListStore d_flagsStore;
		private List<KeyValuePair<string, Cpg.PropertyFlags>> d_flaglist;
		private Actions d_actions;
		private AddRemovePopup d_propertyControls;
		private ListStore d_proxyStore;
		private ComboBox d_proxyCombo;
		private AddRemovePopup d_actionControls;
		private bool d_selectAction;
		private HBox d_extraControl;
		private HPaned d_paned;
		private Entry d_entry;
		
		public PropertyView(Actions actions, Wrappers.Wrapper obj) : base(false, 3)
		{
			d_selectProperty = false;
			d_selectAction = false;

			d_actions = actions;

			Initialize(obj);
		}
		
		public PropertyView(Actions actions) : this(actions, null)
		{
		}
		
		private void AddEquationsUI()
		{
			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			d_paned.Add2(vbox);
			
			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			d_actionStore = new NodeStore<LinkActionNode>();
			d_actionView = new TreeView(new TreeModelAdapter(d_actionStore));
			
			d_actionView.ShowExpanders = false;
			d_actionView.RulesHint = true;
			
			vw.Add(d_actionView);
			
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;			
			renderer.Edited += HandleLinkActionTargetEdited;

			Gtk.TreeViewColumn column = new Gtk.TreeViewColumn("Target", renderer, "text", 0);
			column.MinWidth = 80;
			
			d_actionView.AppendColumn(column);
			
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			renderer.Edited += HandleLinkActionEquationEdited;
			
			column = new Gtk.TreeViewColumn("Equation", renderer, "text", 1);
			d_actionView.AppendColumn(column);
			
			vbox.PackStart(vw, true, true, 0);

			d_actionControls = new AddRemovePopup(d_actionView);
			d_actionControls.AddButton.Clicked += DoAddAction;
			d_actionControls.RemoveButton.Clicked += DoRemoveAction;
			
			UpdateActionSensitivity();
			
			Wrappers.Link link = d_object as Wrappers.Link;
			
			foreach (Cpg.LinkAction action in link.Actions)
			{
				AddLinkAction(action);
			}
			
			d_actionView.Selection.Changed += DoActionSelectionChanged;
		}
		
		private void HandleRenderActionTarget(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter)
		{
			Cpg.LinkAction action = LinkActionFromStore(piter);
			CellRendererText renderer = (CellRendererText)cell;
			
			renderer.Text = action.Target;
		}
		
		private void HandleRenderActionEquation(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter)
		{
			Cpg.LinkAction action = LinkActionFromStore(piter);
			CellRendererText renderer = (CellRendererText)cell;
			
			renderer.Text = action.Equation.AsString;
		}
		
		private void HandleLinkActionTargetEdited(object o, EditedArgs args)
		{
			TreeIter iter;
			
			if (!d_actionStore.GetIter(out iter, new TreePath(args.Path)))
			{
				return;
			}
			
			Cpg.LinkAction action = LinkActionFromStore(iter);
			
			if (action.Target == args.NewText)
			{
				return;
			}

			d_actions.Do(new Undo.ModifyLinkActionTarget((Wrappers.Link)d_object, action.Target, args.NewText));
		}
		
		private void HandleLinkActionEquationEdited(object o, EditedArgs args)
		{
			TreeIter iter;
			
			if (!d_actionStore.GetIter(out iter, new TreePath(args.Path)))
			{
				return;
			}
			
			Cpg.LinkAction action = LinkActionFromStore(iter);
			
			if (action.Equation.AsString == args.NewText)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyLinkActionEquation((Wrappers.Link)d_object, action.Target, args.NewText));
		}

		private void DoTargetPropertyAdded(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			d_comboStore.AppendValues(prop.Name, prop);
		}
		
		private void DoTargetPropertyRemoved(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			TreeIter iter;
			
			if (!d_comboStore.GetIterFirst(out iter))
			{
				return;
			}
			
			do
			{
				Cpg.Property val = d_comboStore.GetValue(iter, 1) as Cpg.Property;
				
				if (val == prop)
				{
					d_comboStore.Remove(ref iter);
					break;
				}				
			} while (d_comboStore.IterNext(ref iter));
		}
		
		private string FlagsToString(Cpg.PropertyFlags flags)
		{
			List<string> parts = new List<string>();

			foreach (KeyValuePair<string, Cpg.PropertyFlags> pair in d_flaglist)
			{
				if ((pair.Value & flags) != 0 && pair.Value != PropertyFlags.Integrated)
				{
					parts.Add(pair.Key);
				}
			}
			
			return String.Join(", ", parts.ToArray());
		}
		
		private void InitializeFlagsList()
		{
			d_flaglist = new List<KeyValuePair<string, Cpg.PropertyFlags>>();
			Type type = typeof(Cpg.PropertyFlags);
			
			string[] names = Enum.GetNames(type);
			Array values = Enum.GetValues(type);
			
			for (int i = 0; i < names.Length; ++i)
			{
				Cpg.PropertyFlags flags = (Cpg.PropertyFlags)values.GetValue(i);
				
				// Don't show 'None' and Integrated is handled separately
				if ((int)flags != 0 && flags != PropertyFlags.Integrated)
				{
					d_flaglist.Add(new KeyValuePair<string, Cpg.PropertyFlags>(names[i], flags));
				}
			}
		}
		
		private void AddGroupUI()
		{
			HBox hbox = new HBox(false, 6);
			hbox.Show();
			
			Label label = new Label("Proxy:");
			label.Show();
			
			hbox.PackStart(label, false, false, 0);
			
			ListStore store = new ListStore(typeof(string), typeof(Wrappers.Wrapper), typeof(bool));
			Wrappers.Group grp = (Wrappers.Group)d_object;
			List<Wrappers.Wrapper> children = new List<Wrappers.Wrapper>(grp.Children);
			
			children.RemoveAll(item => item is Wrappers.Link);
			children.Sort(delegate (Wrappers.Wrapper a, Wrappers.Wrapper b) {
				return a.Id.CompareTo(b.Id);
			});
			
			ComboBox box = new ComboBox(store);
			TreeIter iter;
			
			iter = store.AppendValues("None", null, false);
			
			if (grp.Proxy == null)
			{
				box.SetActiveIter(iter);	
			}

			store.AppendValues(null, null, true);
			
			foreach (Wrappers.Wrapper child in children)
			{
				iter = store.AppendValues(child.Id, child, false);
				
				if (child == grp.Proxy)
				{
					box.SetActiveIter(iter);
				}
			}

			box.RowSeparatorFunc = delegate (TreeModel model, TreeIter it) {
				return (bool)model.GetValue(it, 2);
			};
			
			box.Changed += OnChangeProxy;
			
			box.Show();

			CellRendererText renderer = new CellRendererText();

			box.PackStart(renderer, true);
			box.AddAttribute(renderer, "text", 0);
			
			hbox.PackStart(box, false, false, 0);
			
			d_extraControl.PackEnd(hbox, false, false, 0);
			
			d_proxyStore = store;
			d_proxyCombo = box;
		}
		
		private void HandleProxyChanged(object sender, GLib.NotifyArgs args)
		{
			TreeIter iter;

			if (!d_proxyStore.GetIterFirst(out iter))
			{
				return;
			}
			
			Wrappers.Group grp = (Wrappers.Group)d_object;
			
			do
			{
				Wrappers.Wrapper proxy = (Wrappers.Wrapper)d_proxyStore.GetValue(iter, 1);
				
				if (proxy == grp.Proxy)
				{
					d_proxyCombo.Changed -= OnChangeProxy;
					d_proxyCombo.SetActiveIter(iter);
					d_proxyCombo.Changed += OnChangeProxy;
					return;
				}
			} while (d_proxyStore.IterNext(ref iter));
			
			d_proxyCombo.Active = 0;
		}

		private void OnChangeProxy(object sender, EventArgs e)
		{
			Wrappers.Wrapper proxy;
			TreeIter iter;

			if (!d_proxyCombo.GetActiveIter(out iter))
			{
				proxy = null;
			}
			else
			{
				proxy = (Wrappers.Wrapper)d_proxyStore.GetValue(iter, 1);
			}
			
			d_actions.Do(new Undo.ModifyProxy((Wrappers.Group)d_object, proxy));
		}
		
		private void Disconnect()
		{
			if (d_object == null)
			{
				return;
			}
			
			d_object.PropertyAdded -= DoPropertyAdded;
			d_object.PropertyRemoved -= DoPropertyRemoved;
			
			d_object.WrappedObject.RemoveNotification("id", HandleIdChanged);
			
			foreach (TreeIter iter in ForeachProperty())
			{
				Cpg.Property property = PropertyFromStore(iter);

				property.RemoveNotification("expression", HandlePropertyChanged);
				property.RemoveNotification("flags", HandlePropertyChanged);
				property.RemoveNotification("name", HandlePropertyChanged);
			}
			
			if (d_object is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)d_object;
				
				grp.WrappedObject.RemoveNotification("proxy", HandleProxyChanged);
			}
			else if (d_object is Wrappers.Link)
			{
				Wrappers.Link link = (Wrappers.Link)d_object;
				
				link.ActionAdded -= HandleLinkActionAdded;
				link.ActionRemoved -= HandleLinkActionRemoved;
			}
		}
		
		private void Connect()
		{
			if (d_object == null)
			{
				return;
			}
			
			d_object.PropertyAdded += DoPropertyAdded;
			d_object.PropertyRemoved += DoPropertyRemoved;
			
			d_object.WrappedObject.AddNotification("id", HandleIdChanged);
			
			if (d_object is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)d_object;
				
				grp.WrappedObject.AddNotification("proxy", HandleProxyChanged);
			}
			else if (d_object is Wrappers.Link)
			{
				Wrappers.Link link = (Wrappers.Link)d_object;
				
				link.ActionAdded += HandleLinkActionAdded;
				link.ActionRemoved += HandleLinkActionRemoved;
			}
		}

		private void HandleLinkActionRemoved(object source, Cpg.LinkAction action)
		{
			d_actionStore.Remove(action);
		}

		private void HandleLinkActionAdded(object source, Cpg.LinkAction action)
		{
			AddLinkAction(action);
		}
		
		private void AddLinkAction(Cpg.LinkAction action)
		{
			TreeIter iter;
			
			iter = d_actionStore.Add(new LinkActionNode(action));
			
			if (d_selectAction)
			{
				d_actionView.Selection.UnselectAll();
				d_actionView.Selection.SelectIter(iter);
				
				TreePath path = d_actionStore.GetPath(iter);					
				d_actionView.SetCursor(path, d_actionView.GetColumn(0), true);
			}			
		}
		
		private void AddIdUI()
		{
			HBox hbox = new HBox(false, 6);
			hbox.Show();

			Label lbl = new Label("Id:");
			lbl.Show();
			
			hbox.PackStart(lbl, false, false, 0);
			
			d_entry = new Entry();
			d_entry.Show();
			
			d_entry.Text = d_object.Id;
			
			d_entry.Activated += delegate {
				ModifyId();
			};
			
			d_entry.FocusOutEvent += delegate {
				ModifyId();
			};
			
			d_entry.KeyPressEvent += delegate(object o, KeyPressEventArgs args) {
				if (args.Event.Key == Gdk.Key.Escape)
				{
					d_entry.Text = d_object.Id;
					d_entry.Position = d_entry.Text.Length;
				}
			};
			
			hbox.PackStart(d_entry, false, false, 0);
			d_extraControl.PackStart(hbox, false, false, 0);
		}

		private void ModifyId()
		{ 
			if (d_object.Id == d_entry.Text || d_entry.Text == "")
			{
				d_entry.Text = d_object.Id;
				return;
			}
			
			d_actions.Do(new Undo.ModifyObjectId(d_object, d_entry.Text));
		}
		
		public void Initialize(Wrappers.Wrapper obj)
		{
			Clear();
			
			InitializeFlagsList();
			
			d_object = obj;
			
			d_paned = new HPaned();

			d_paned.Realized += delegate {
				d_paned.Position = Allocation.Width / 2;
			};
			
			if (d_object != null && d_object is Wrappers.Link)
			{
				AddEquationsUI();
			}
			
			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			d_paned.Add1(vbox);

			d_extraControl = new HBox(false, 12);
			d_extraControl.Show();
			PackStart(d_extraControl, false, false, 0);
			
			PackStart(d_paned, true, true, 0);
			
			if (d_object != null)
			{
				AddIdUI();
			}
			
			if (d_object != null && d_object is Wrappers.Group)
			{
				AddGroupUI();
			}

			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			d_store = new ListStore(typeof(Cpg.Property));
			d_treeview = new TreeView(d_store);	
			
			d_treeview.ShowExpanders = false;
			
			vw.Add(d_treeview);
			
			d_treeview.Show();
			vw.Show();

			vbox.PackStart(vw, true, true, 0);
			
			CellRendererText renderer;
			TreeViewColumn column;
			
			// Add column for the name
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			column = new TreeViewColumn("Name", renderer);
			column.Resizable = true;
			column.MinWidth = 75;
			
			if (d_object != null)
			{
				renderer.Edited += DoNameEdited;
			}
			
			column.SetCellDataFunc(renderer, HandleRenderName);
			d_treeview.AppendColumn(column);
			
			// Add column for the value
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			if (d_object != null)
			{
				renderer.Edited += DoValueEdited;
			}
				
			column = new TreeViewColumn("Value", renderer);
			column.Resizable = true;
			
			column.SetCellDataFunc(renderer, HandleRenderValue);
			d_treeview.AppendColumn(column);
			
			// Add column for the integrated
			CellRendererToggle toggle = new CellRendererToggle();
			column = new TreeViewColumn("Integrated", toggle);
			column.Resizable = false;
			
			if (d_object != null)
			{
				toggle.Toggled += DoIntegratedToggled;
			}
			
			column.SetCellDataFunc(toggle, HandleRenderIntegrated);
			d_treeview.AppendColumn(column);			
				
			// Add column for property flags
			CellRendererCombo combo = new CellRendererCombo();
			combo.Editable = true;
			combo.Sensitive = true;
			
			column = new TreeViewColumn("Flags", combo);
			column.Resizable = true;
			column.SetCellDataFunc(combo, HandleRenderFlags);
			
			combo.EditingStarted += DoEditingStarted;
			combo.Edited += DoFlagsEdited;
			combo.HasEntry = false;
			
			d_flagsStore = new ListStore(typeof(string), typeof(Cpg.PropertyFlags));
			combo.Model = d_flagsStore;
			combo.TextColumn = 0;
			
			column.MinWidth = 50;
			d_treeview.AppendColumn(column);

			d_treeview.Selection.Changed += DoSelectionChanged;
			
			Connect();
			
			if (d_object != null)
			{
				InitStore();
				Sensitive = true;
			}
			else
			{
				Sensitive = false;
			}
			
			column = new TreeViewColumn();
			d_treeview.AppendColumn(column);
			
			ShowAll();
			
			d_propertyControls = new AddRemovePopup(d_treeview);
			d_propertyControls.AddButton.Clicked += DoAddProperty;
			d_propertyControls.RemoveButton.Clicked += DoRemoveProperty;
			
			UpdateSensitivity();
		}
		
		private void HandleRenderIntegrated(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter)
		{
			Cpg.Property property = PropertyFromStore(piter);
			CellRendererToggle renderer = (CellRendererToggle)cell;
			
			renderer.Active = (property.Flags & PropertyFlags.Integrated) != PropertyFlags.None;
		}
		
		private void HandleRenderName(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter)
		{
			Cpg.Property property = PropertyFromStore(piter);
			CellRendererText renderer = (CellRendererText)cell;
			
			renderer.Text = property.Name;
		}
		
		private void HandleRenderValue(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter)
		{
			Cpg.Property property = PropertyFromStore(piter);
			CellRendererText renderer = (CellRendererText)cell;
			
			renderer.Text = property.Expression.AsString;
		}
		
		private void HandleRenderFlags(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter)
		{
			Cpg.Property property = PropertyFromStore(piter);
			CellRendererText renderer = (CellRendererText)cell;
			
			renderer.Text = FlagsToString(property.Flags);
		}
		
		private void FillFlagsStore(Cpg.Property property)
		{
			d_flagsStore.Clear();
			Cpg.PropertyFlags flags = property.Flags;
			
			foreach (KeyValuePair<string, Cpg.PropertyFlags> pair in d_flaglist)
			{
				string name = pair.Key;

				if ((flags & pair.Value) != 0)
				{
					name = "• " + name;
				}
				
				d_flagsStore.AppendValues(name, flags);
			}
		}

		private void DoEditingStarted(object o, EditingStartedArgs args)
		{
			FillFlagsStore(PropertyFromStore(args.Path));
		}
		
		private void InitStore()
		{
			foreach (Cpg.Property prop in d_object.Properties)
			{
				AddProperty(prop);
			}
		}
		
		private Cpg.LinkAction LinkActionFromStore(string path)
		{
			return LinkActionFromStore(new TreePath(path));
		}
		
		private Cpg.LinkAction LinkActionFromStore(TreePath path)
		{
			TreeIter iter;
			
			d_store.GetIter(out iter, path);
			return LinkActionFromStore(iter);
		}
		
		private Cpg.LinkAction LinkActionFromStore(TreeIter iter)
		{
			return d_actionStore.GetFromIter<LinkActionNode>(iter).LinkAction;
		}
		
		private Cpg.Property PropertyFromStore(string path)
		{
			return PropertyFromStore(new TreePath(path));
		}
		
		private Cpg.Property PropertyFromStore(TreePath path)
		{
			TreeIter iter;
			
			d_store.GetIter(out iter, path);
			return PropertyFromStore(iter);
		}
		
		private Cpg.Property PropertyFromStore(TreeIter iter)
		{
			return (Cpg.Property)d_store.GetValue(iter, 0);
		}
		
		private void DoIntegratedToggled(object source, ToggledArgs args)
		{
			Cpg.Property property = PropertyFromStore(args.Path);
			PropertyFlags flags = property.Flags;
			CellRendererToggle toggle = (CellRendererToggle)source;
			
			if (!toggle.Active)
			{
				flags |= PropertyFlags.Integrated;
			}
			else
			{
				flags &= ~PropertyFlags.Integrated;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, property, flags));
		}
		
		private void DoFlagsEdited(object source, EditedArgs args)
		{
			Cpg.Property property = PropertyFromStore(args.Path);

			bool wason = false;
			string name = args.NewText;
			
			if (name.StartsWith("• "))
			{
				wason = true;
				name = name.Substring(2);
			}

			Cpg.PropertyFlags flags = (Cpg.PropertyFlags)Enum.Parse(typeof(Cpg.PropertyFlags), name);
			Cpg.PropertyFlags newflags = property.Flags;
			
			if (wason)
			{
				newflags &= ~flags;
			}
			else
			{
				newflags |= flags;
			}
			
			if (newflags == property.Flags)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, property, newflags));
		}
		
		private void DoValueEdited(object source, EditedArgs args)
		{
			Cpg.Property property = PropertyFromStore(args.Path);
			
			if (args.NewText == property.Expression.AsString)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, property, args.NewText));
		}
		
		private void DoNameEdited(object source, EditedArgs args)
		{
			if (String.IsNullOrEmpty(args.NewText))
			{
				return;
			}
			
			Cpg.Property property = PropertyFromStore(args.Path);
			
			if (args.NewText == property.Name)
			{
				return;
			}

			List<Undo.IAction> actions = new List<Undo.IAction>();
			actions.Add(new Undo.RemoveProperty(d_object, property));
			actions.Add(new Undo.AddProperty(d_object, args.NewText, property.Expression.AsString, property.Flags));
			
			try
			{
				d_actions.Do(new Undo.Group(actions));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
				return;
			}
		}
		
		private void UpdateSensitivity()
		{
			TreeIter iter;
			d_propertyControls.RemoveButton.Sensitive = d_treeview.Selection.GetSelected(out iter);
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			UpdateSensitivity();			
		}
		
		private void UpdateActionSensitivity()
		{
			if (d_actionView.Selection.CountSelectedRows() == 0)
			{
				d_actionControls.RemoveButton.Sensitive = false;
			}
			else
			{
				d_actionControls.RemoveButton.Sensitive = true;
			}
		}
		
		private void DoActionSelectionChanged(object source, EventArgs args)
		{
			UpdateActionSensitivity();
		}
		
		private void HandleAddBinding()
		{
			DoAddProperty();
		}
		
		private void HandleDeleteBinding()
		{
			DoRemoveProperty();
		}
		
		private void AddProperty(Cpg.Property prop)
		{
			if (PropertyExists(prop.Name))
			{
				return;
			}
			
			TreeIter iter = d_store.AppendValues(prop);
			
			if (d_selectProperty)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
				
				TreePath path = d_store.GetPath(iter);					
				d_treeview.SetCursor(path, d_treeview.GetColumn(0), true);
			}
			
			prop.AddNotification("expression", HandlePropertyChanged);
			prop.AddNotification("flags", HandlePropertyChanged);
			prop.AddNotification("name", HandlePropertyChanged);
		}
		
		private void HandleIdChanged(object source, GLib.NotifyArgs args)
		{
			d_entry.Text = d_object.Id;
		}
		
		private void HandlePropertyChanged(object source, GLib.NotifyArgs args)
		{
			Cpg.Property prop = (Cpg.Property)source;
			
			TreeIter iter;
			TreePath path;
			
			if (FindProperty(prop.Name, out path, out iter))
			{
				d_store.EmitRowChanged(path, iter);
			}
		}
		
		private bool PropertyExists(string name)
		{
			TreeIter iter;
			return FindProperty(name, out iter);
		}
		
		private void DoAddProperty(object source, EventArgs args)
		{
			DoAddProperty();
		}
		
		private void DoAddProperty()
		{
			int num = 1;
			
			while (PropertyExists("x" + num))
			{
				++num;
			}
			
			d_selectProperty = true;
			d_actions.Do(new Undo.AddProperty(d_object, "x" + num, "0", Cpg.PropertyFlags.None));
			d_selectProperty = false;
		}
		
		private void DoRemoveProperty(object source, EventArgs args)
		{
			DoRemoveProperty();
		}
		
		private void DoRemoveProperty()
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
			{
				Cpg.Property prop = PropertyFromStore(path);
				actions.Add(new Undo.RemoveProperty(d_object, prop));
			}

			try
			{
				d_actions.Do(new Undo.Group(actions));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
			}
		}
		
		private void DoAddAction(object source, EventArgs args)
		{
			Wrappers.Link link = d_object as Wrappers.Link;
			
			List<string> props = new List<string>(Array.ConvertAll<LinkAction, string>(link.Actions, item => item.Target));
			List<string> prefs = new List<string>();
			
			if (link.To != null)
			{
				prefs = new List<string>(Array.ConvertAll<Property, string>(link.To.Properties, item => item.Name));
			}
			else
			{
				prefs = new List<string>();
			}
			
			int i = 0;
			string name;
			
			do
			{
				if (i < prefs.Count)
				{
					name = prefs[i];
				}
				else
				{
					name = String.Format("x{0}", i - prefs.Count + 1);
				}

				++i;
			} while (props.Contains(name));
			
			d_selectAction = true;
			d_actions.Do(new Undo.AddLinkAction(link, name, ""));
			d_selectAction = false;
		}
		
		private void DoRemoveAction(object source, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			
			if (!d_actionView.Selection.GetSelected(out model, out iter))
			{
				return;
			}
			
			Cpg.LinkAction val = LinkActionFromStore(iter);
	
			d_actions.Do(new Undo.RemoveLinkAction((Wrappers.Link)d_object, val));
		}
		
		private void DoPropertyAdded(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			AddProperty(prop);
		}
		
		private IEnumerable<TreeIter> ForeachProperty()
		{
			TreeIter iter;

			if (!d_store.GetIterFirst(out iter))
			{
				return false;
			}
			
			do
			{
				yield return iter;
			} while (d_store.IterNext(ref iter));
		}
		
		private bool FindProperty(string name, out TreePath path, out TreeIter iter)
		{			
			path = null;
			
			if (!d_store.GetIterFirst(out iter))
			{
				return false;
			}
			
			do
			{
				Cpg.Property property = PropertyFromStore(iter);

				if (property.Name == name)
				{
					path = d_store.GetPath(iter);
					return true;
				}
			} while (d_store.IterNext(ref iter));	
			
			return false;
		}
		
		private bool FindProperty(string name, out TreeIter iter)
		{
			TreePath path;
			return FindProperty(name, out path, out iter);
		}
		
		private void DoPropertyChanged(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			TreePath path;
			TreeIter iter;

			if (FindProperty(prop.Name, out path, out iter))
			{
				d_store.EmitRowChanged(path, iter);
			}
		}
		
		private void DoPropertyRemoved(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			TreeIter iter;

			if (FindProperty(prop.Name, out iter))
			{
				d_store.Remove(ref iter);
			}
			
			prop.RemoveNotification(HandlePropertyChanged);
			prop.RemoveNotification(HandlePropertyChanged);
			prop.RemoveNotification(HandlePropertyChanged);
		}
		
		private void Clear()
		{
			Disconnect();

			while (Children.Length > 0)
			{
				Remove(Children[0]);
			}
			
			if (d_store != null)
			{
				d_store.Clear();
			}
				
			d_object = null;
			Sensitive = false;
		}
		
		public void Select(Cpg.Property property)
		{
			TreeIter iter;
			
			if (FindProperty(property.Name, out iter))
			{
				d_treeview.Selection.SelectIter(iter);
			}
		}
		
		public void Select(Cpg.LinkAction action)
		{
			TreeIter iter;
			
			if (d_actionStore.Find(action, out iter))
			{
				d_actionView.Selection.SelectIter(iter);
			}
		}
		
		protected override void OnRealized()
		{
			base.OnRealized();
			
			d_paned.Position = Allocation.Width / 2;
		}

	}
}
