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
				get	{ return d_action; }
			}
			
			[NodeColumn(0)]
			public string Target
			{
				get { return d_action.Target; }
			}
			
			[NodeColumn(1)]
			public string Equation
			{
				get { return d_action.Equation.AsString; }
			}
		}
		
		private class PropertyNode : Node
		{
			private Property d_property;

			public PropertyNode(Property property)
			{
				d_property = property;
				
				d_property.AddNotification("name", OnPropertyChanged);
				d_property.AddNotification("expression", OnPropertyChanged);
				d_property.AddNotification("flags", OnPropertyChanged);
			}
			
			~PropertyNode()
			{
				d_property.RemoveNotification("name", OnPropertyChanged);
				d_property.RemoveNotification("expression", OnPropertyChanged);
				d_property.RemoveNotification("flags", OnPropertyChanged);
			}
			
			private void OnPropertyChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[PrimaryKey]
			public Property Property
			{
				get	{ return d_property; }
			}
			
			[NodeColumn(0), PrimaryKey]
			public string Name
			{
				get { return d_property.Name; }
			}
			
			[NodeColumn(1)]
			public string Expression
			{
				get { return d_property.Expression.AsString; }
			}
			
			[NodeColumn(2)]
			public bool Integrated
			{
				
				get { return (d_property.Flags & PropertyFlags.Integrated) != 0; }
			}
			
			[NodeColumn(3)]
			public string Flags
			{
				
				get 
				{
					// Ignore integrated
					PropertyFlags filt = d_property.Flags & ~PropertyFlags.Integrated;
					return Property.FlagsToString(filt);
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
		private NodeStore<PropertyNode> d_store;
		private TreeView d_treeview;
		private bool d_selectProperty;
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

		
		private void HandleLinkActionTargetEdited(object o, EditedArgs args)
		{
			LinkActionNode node = d_actionStore.FindPath(args.Path);
			
			if (node.LinkAction.Target == args.NewText.Trim())
			{
				return;
			}

			d_actions.Do(new Undo.ModifyLinkActionTarget((Wrappers.Link)d_object, node.LinkAction.Target, args.NewText.Trim()));
		}
		
		private void HandleLinkActionEquationEdited(object o, EditedArgs args)
		{
			LinkActionNode node = d_actionStore.FindPath(args.Path);
			
			if (node.LinkAction.Equation.AsString == args.NewText.Trim())
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyLinkActionEquation((Wrappers.Link)d_object, node.LinkAction.Target, args.NewText.Trim()));
		}
		
		private void InitializeFlagsList()
		{
			d_flaglist = new List<KeyValuePair<string, Cpg.PropertyFlags>>();
			Type type = typeof(Cpg.PropertyFlags);

			Array values = Enum.GetValues(type);
			
			for (int i = 0; i < values.Length; ++i)
			{
				Cpg.PropertyFlags flags = (Cpg.PropertyFlags)values.GetValue(i);
				
				// Don't show 'None' and Integrated is handled separately
				if ((int)flags != 0 && flags != PropertyFlags.Integrated)
				{
					d_flaglist.Add(new KeyValuePair<string, Cpg.PropertyFlags>(Property.FlagsToString(flags), flags));
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
			
			d_entry.WidthChars = 15;
			
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
			
			Wrappers.Wrapper[] templates = d_object.AppliedTemplates;
			
			if (templates.Length != 0)
			{
				string text = String.Join(", ", Array.ConvertAll<Wrappers.Wrapper, string>(templates, item => item.ToString()));

				lbl = new Label("« (" + text + ")");
				lbl.Show();

				hbox.PackStart(lbl, false, false, 0);
			}
			
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
			
			d_store = new NodeStore<PropertyNode>();
			d_treeview = new TreeView(new TreeModelAdapter(d_store));
			
			d_treeview.RulesHint = true;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			
			d_store.NodeChanged += HandleNodeChanged;
			
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
			
			column = new TreeViewColumn("Name", renderer, "text", 0);
			column.Resizable = true;
			column.MinWidth = 75;
			
			if (d_object != null)
			{
				renderer.Edited += DoNameEdited;
			}
			
			d_treeview.AppendColumn(column);
			
			// Add column for the value
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			if (d_object != null)
			{
				renderer.Edited += DoValueEdited;
			}
				
			column = new TreeViewColumn("Value", renderer, "text", 1);
			column.Resizable = true;

			d_treeview.AppendColumn(column);
			
			// Add column for the integrated
			CellRendererToggle toggle = new CellRendererToggle();
			column = new TreeViewColumn("Integrated", toggle, "active", 2);
			column.Resizable = false;
			
			if (d_object != null)
			{
				toggle.Toggled += DoIntegratedToggled;
			}

			d_treeview.AppendColumn(column);			
				
			// Add column for property flags
			CellRendererCombo combo = new CellRendererCombo();
			combo.Editable = true;
			combo.Sensitive = true;
			
			column = new TreeViewColumn("Flags", combo, "text", 3);
			column.Resizable = true;
			
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

		private void HandleNodeChanged(NodeStore<PropertyNode> store, Node child)
		{
			UpdateSensitivity();
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
			FillFlagsStore(d_store.FindPath(args.Path).Property);
		}
		
		private void InitStore()
		{
			foreach (Cpg.Property prop in d_object.Properties)
			{
				AddProperty(prop);
			}
		}
		
		private void DoIntegratedToggled(object source, ToggledArgs args)
		{
			PropertyNode node = d_store.FindPath(args.Path);
			PropertyFlags flags = node.Property.Flags;
			CellRendererToggle toggle = (CellRendererToggle)source;
			
			if (!toggle.Active)
			{
				flags |= PropertyFlags.Integrated;
			}
			else
			{
				flags &= ~PropertyFlags.Integrated;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, node.Property, flags));
		}
		
		private void DoFlagsEdited(object source, EditedArgs args)
		{
			PropertyNode node = d_store.FindPath(args.Path);

			bool wason = false;
			string name = args.NewText;
			
			if (String.IsNullOrEmpty(name))
			{
				return;
			}
			
			if (name.StartsWith("• "))
			{
				wason = true;
				name = name.Substring(2);
			}

			Cpg.PropertyFlags flags = Property.FlagsFromString(name);
			Cpg.PropertyFlags newflags = node.Property.Flags;
			
			if (wason)
			{
				newflags &= ~flags;
			}
			else
			{
				newflags |= flags;
			}
			
			if (newflags == node.Property.Flags)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, node.Property, newflags));
		}
		
		private void DoValueEdited(object source, EditedArgs args)
		{
			PropertyNode node = d_store.FindPath(args.Path);
			
			if (args.NewText.Trim() == node.Property.Expression.AsString)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, node.Property, args.NewText.Trim()));
		}
		
		private void DoNameEdited(object source, EditedArgs args)
		{
			if (String.IsNullOrEmpty(args.NewText))
			{
				return;
			}
			
			PropertyNode node = d_store.FindPath(args.Path);
			
			if (args.NewText.Trim() == node.Property.Name)
			{
				return;
			}

			List<Undo.IAction> actions = new List<Undo.IAction>();

			actions.Add(new Undo.RemoveProperty(d_object, node.Property));
			actions.Add(new Undo.AddProperty(d_object, args.NewText.Trim(), node.Property.Expression.AsString, node.Property.Flags));
			
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
		
		enum Sensitivity
		{
			None,
			Revert,
			Remove
		}
		
		private void UpdateSensitivity()
		{
			Sensitivity sens = Sensitivity.None;
			
			foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
			{
				PropertyNode node = d_store.FindPath(path);
				
				if (d_object.GetPropertyTemplate(node.Property, true) != null)
				{
					if (sens != Sensitivity.None)
					{
						sens = Sensitivity.None;
						break;
					}
				}
				else if (d_object.GetPropertyTemplate(node.Property, false) != null)
				{
					if (sens == Sensitivity.Remove)
					{
						sens = Sensitivity.None;
						break;
					}
					else
					{
						sens = Sensitivity.Revert;
					}
				}
				else
				{
					sens = Sensitivity.Remove;
				}
			}
			
			d_propertyControls.RemoveButton.Sensitive = (sens != Sensitivity.None);

			if (sens == Sensitivity.Revert)
			{
				 d_propertyControls.RemoveButton.Image = new Image(Gtk.Stock.RevertToSaved, IconSize.Button);
			}
			else
			{
				d_propertyControls.RemoveButton.Image = new Image(Gtk.Stock.Remove, IconSize.Button);
			}
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
			
			TreeIter iter = d_store.Add(new PropertyNode(prop));
			
			if (d_selectProperty)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
				
				TreePath path = d_store.GetPath(iter);					
				d_treeview.SetCursor(path, d_treeview.GetColumn(0), true);
			}			
		}
		
		private void HandleIdChanged(object source, GLib.NotifyArgs args)
		{
			d_entry.Text = d_object.Id;
		}
		
		private bool PropertyExists(string name)
		{
			return d_store.Find(name) != null;
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
				PropertyNode node = d_store.FindPath(path);
				
				Wrappers.Wrapper temp = d_object.GetPropertyTemplate(node.Property, false);
				
				if (temp != null)
				{
					Cpg.Property tempProp = temp.Property(node.Property.Name);

					actions.Add(new Undo.ModifyProperty(d_object, node.Property, tempProp.Expression.AsString));
					actions.Add(new Undo.ModifyProperty(d_object, node.Property, tempProp.Flags));
				}
				else
				{
					actions.Add(new Undo.RemoveProperty(d_object, node.Property));
				}
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
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_actionView.Selection.GetSelectedRows())
			{
				LinkActionNode node = d_actionStore.FindPath(path);

				actions.Add(new Undo.RemoveLinkAction((Wrappers.Link)d_object, node.LinkAction));
			}
			
			d_actions.Do(new Undo.Group(actions));
		}
		
		private void DoPropertyAdded(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			AddProperty(prop);
		}
		
		private void DoPropertyRemoved(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			d_store.Remove(prop);
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
			
			if (d_store.Find(property, out iter))
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
