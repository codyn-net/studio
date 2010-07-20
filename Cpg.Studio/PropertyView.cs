using System;
using Gtk;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding")]
	[Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding")]
	public class PropertyView : HPaned
	{
		enum Column
		{
			Property = 0
		}

		public delegate void ErrorHandler(object source, Exception exception);
		
		public event ErrorHandler Error = delegate {};
		
		private Wrappers.Wrapper d_object;
		private ListStore d_store;
		private TreeView d_treeview;
		private Button d_removeButton;
		private bool d_selectProperty;
		private ListStore d_comboStore;
		private ListStore d_actionStore;
		private TreeView d_actionView;
		private Button d_removeActionButton;
		private ListStore d_flagsStore;
		private List<KeyValuePair<string, Cpg.PropertyFlags>> d_flaglist;
		private Actions d_actions;
		
		public PropertyView(Actions actions, Wrappers.Wrapper obj) : base()
		{
			d_actions = actions;

			Initialize(obj);
		}
		
		public PropertyView(Actions actions) : this(actions, null)
		{
			d_selectProperty = false;
		}
		
		private void AddEquationsUI()
		{
			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			Add2(vbox);
			
			Gtk.Label label = new Label("<b>Actions</b>");
			label.Xalign = 0;
			label.UseMarkup = true;
			
			vbox.PackStart(label, false, true, 0);
			
			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			d_actionStore = new Gtk.ListStore(typeof(Wrappers.Link.Action), typeof(string), typeof(string));
			d_actionView = new Gtk.TreeView(d_actionStore);
			
			vw.Add(d_actionView);
			
			CellRendererCombo comboRenderer = new CellRendererCombo();
			d_comboStore = new ListStore(typeof(string));
			comboRenderer.Model = d_comboStore;
			comboRenderer.TextColumn = 0;
			comboRenderer.Editable = true;
			comboRenderer.HasEntry = false;
			
			comboRenderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				
				if (!d_actionStore.GetIter(out iter, new TreePath(args.Path)))
				{
					return;
				}
				
				Wrappers.Link.Action action = d_actionStore.GetValue(iter, 0) as Wrappers.Link.Action;
				
				if (action.Target == args.NewText)
				{
					return;
				}

				action.Target = args.NewText;
				d_actionStore.SetValue(iter, 1, action.Target);
			};

			Gtk.TreeViewColumn column = new Gtk.TreeViewColumn("Target", comboRenderer, "text", 1);
			column.MinWidth = 80;
			d_actionView.AppendColumn(column);
			
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;
			
			renderer.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				
				if (!d_actionStore.GetIter(out iter, new TreePath(args.Path)))
				{
					return;
				}
				
				Wrappers.Link.Action action = d_actionStore.GetValue(iter, 0) as Wrappers.Link.Action;
				
				if (action.Equation == args.NewText)
				{
					return;
				}
				
				action.Equation = args.NewText;
				d_actionStore.SetValue(iter, 2, action.Equation);
			};
			
			column = new Gtk.TreeViewColumn("Equation", renderer, "text", 2);
			d_actionView.AppendColumn(column);
			
			vbox.PackStart(vw, true, true, 0);
			
			HBox hbox = new HBox(false, 3);
			vbox.PackStart(hbox, false, false, 0);
			
			d_removeActionButton = new Button();
			d_removeActionButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_removeActionButton.Sensitive = false;
			d_removeActionButton.Clicked += DoRemoveAction;
			hbox.PackStart(d_removeActionButton, false, false ,0);

			Button but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAddAction;
			hbox.PackStart(but, false, false, 0);
			
			Wrappers.Link link = d_object as Wrappers.Link;
			link.To.PropertyAdded += DoTargetPropertyAdded;
			link.To.PropertyRemoved += DoTargetPropertyRemoved;
			
			foreach (Cpg.Property prop in link.To.Properties)
			{
				d_comboStore.AppendValues(prop);
			}
			
			foreach (Wrappers.Link.Action action in link.Actions)
			{
				d_actionStore.AppendValues(action, action.Target, action.Equation);
			}
			
			d_actionView.Selection.Changed += DoActionSelectionChanged;
		}

		private void DoTargetPropertyAdded(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			d_comboStore.AppendValues(prop);
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
				Cpg.Property val = d_comboStore.GetValue(iter, 0) as Cpg.Property;
				
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
				if ((pair.Value & flags) != 0)
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
				
				if ((int)flags != 0)
				{
					d_flaglist.Add(new KeyValuePair<string, Cpg.PropertyFlags>(names[i], flags));
				}
			}
		}
		
		public void Initialize(Wrappers.Wrapper obj)
		{
			Clear();
			
			InitializeFlagsList();
			
			d_object = obj;
			
			if (d_object != null && d_object is Wrappers.Link)
			{
				AddEquationsUI();
			}
			
			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			Add1(vbox);
			
			Gtk.Label label = new Label("<b>Properties</b>");
			label.Xalign = 0;
			label.UseMarkup = true;
			
			vbox.PackStart(label, false, true, 0);
			
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

			HBox hbox = new HBox(false, 3);
			vbox.PackStart(hbox, false, false, 0);
			
			d_removeButton = new Button();
			d_removeButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_removeButton.Sensitive = false;
			d_removeButton.Clicked += DoRemoveProperty;
			hbox.PackStart(d_removeButton, false, false ,0);

			Button but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAddProperty;
			hbox.PackStart(but, false, false, 0);

			d_treeview.Selection.Changed += DoSelectionChanged;
			
			if (d_object != null)
			{
				d_object.PropertyAdded += DoPropertyAdded;
				d_object.PropertyChanged += DoPropertyChanged;
				d_object.PropertyRemoved += DoPropertyRemoved;
			
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
			
			d_actions.Do(new Undo.ModifyProperty(property, newflags));
		}
		
		private void DoValueEdited(object source, EditedArgs args)
		{
			Cpg.Property property = PropertyFromStore(args.Path);
			
			if (args.NewText == property.Expression.AsString)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(property, args.NewText));
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
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			TreeIter iter;

			d_removeButton.Sensitive = d_treeview.Selection.GetSelected(out iter);
		}
		
		private void DoActionSelectionChanged(object source, EventArgs args)
		{
			if (d_actionView.Selection.CountSelectedRows() == 0)
			{
				d_removeActionButton.Sensitive = false;
			}
			else
			{
				d_removeActionButton.Sensitive = true;
			}
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
				// FIXME: if one fails, the others are still removed, but not undoable!
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
			
			List<Cpg.Property> props = new List<Cpg.Property>(link.To.Properties);
		
			// Remove properties that already have actions
			foreach (Wrappers.Link.Action ac in link.Actions)
			{
				if (props.Contains(ac.Property))
				{
					props.Remove(ac.Property);
				}
			}
			
			if (props.Count == 0)
			{
				return;
			}
			
			Wrappers.Link.Action action = link.AddAction(props[0].Name, "");
			TreeIter iter = d_actionStore.AppendValues(action, action.Target, action.Equation);
			
			TreePath path = d_actionStore.GetPath(iter);
			d_actionView.Selection.UnselectAll();
			
			d_actionView.Selection.SelectPath(path);
			d_actionView.SetCursor(path, d_actionView.Columns[0], true);
		}
		
		private void DoRemoveAction(object source, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			
			if (!d_actionView.Selection.GetSelected(out model, out iter))
				return;
			
			Wrappers.Link.Action val = model.GetValue(iter, 0) as Wrappers.Link.Action;	
	
			if ((d_object as Wrappers.Link).RemoveAction(val))
			{
				if (d_actionStore.Remove(ref iter))
				{
					d_actionView.Selection.SelectIter(iter);
				}
			}
		}
		
		private void DoPropertyAdded(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			AddProperty(prop);
		}
		
		private bool FindProperty(string name, out TreePath path, out TreeIter iter)
		{
			TreeModel model = d_treeview.Model;
			
			path = null;
			
			if (!model.GetIterFirst(out iter))
			{
				return false;
			}
			
			do
			{
				Cpg.Property property = PropertyFromStore(iter);

				if (property.Name == name)
				{
					path = model.GetPath(iter);
					return true;
				}
			} while (model.IterNext(ref iter));	
			
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
			
			if (!d_actionStore.GetIterFirst(out iter))
			{
				return;
			}
			
			do
			{
				Wrappers.Link.Action o = d_actionStore.GetValue(iter, 0) as Wrappers.Link.Action;
				
				if (o.LinkAction.Handle == action.Handle)
				{
					d_actionView.Selection.SelectIter(iter);
					return;
				}
			} while (d_actionStore.IterNext(ref iter));
		}
		
		protected override void OnRealized()
		{
			base.OnRealized();
			
			Position = Allocation.Width / 2;
		}

	}
}
