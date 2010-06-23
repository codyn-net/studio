using System;
using Gtk;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio
{
	public class PropertyView : HPaned
	{
		class Node : TreeNode
		{
			Cpg.Property d_property;
			
			public Node(Cpg.Property property)
			{
				d_property = property;
			}
			
			[TreeNodeValue(Column=0)]
			public string Name
			{
				get
				{
					return d_property.Name;
				}
			}
			
			[TreeNodeValue(Column=1)]
			public string Value
			{
				get
				{
					return d_property.Expression.AsString;
				}
				set
				{
					d_property.Expression.FromString = value;
				}
			}

			[TreeNodeValue(Column=2)]
			public Cpg.PropertyFlags Flags
			{
				get
				{
					return d_property.Flags;
				}
				set
				{
					d_property.Flags = value;
				}
			}
			
			public Cpg.Property Property
			{
				get
				{
					return d_property;
				}
			}
		}
		
		class NodeStore : Gtk.NodeStore
		{
			public NodeStore() : base(typeof(Node))
			{
			}
		}
		
		public delegate void ErrorHandler(object source, Exception exception);
		
		public event ErrorHandler Error = delegate {};
		
		private Wrappers.Wrapper d_object;
		private NodeStore d_store;
		private NodeView d_treeview;
		private Button d_removeButton;
		private bool d_selectProperty;
		private ListStore d_comboStore;
		private ListStore d_actionStore;
		private TreeView d_actionView;
		private Button d_removeActionButton;
		private ListStore d_hintStore;
		private List<KeyValuePair<string, Cpg.PropertyFlags>> d_flaglist;
		
		public PropertyView(Wrappers.Wrapper obj) : base()
		{
			Initialize(obj);
		}
		
		public PropertyView() : this(null)
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
					return;
				
				Wrappers.Link.Action action = d_actionStore.GetValue(iter, 0) as Wrappers.Link.Action;
				
				if (action.Target == args.NewText)
					return;

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
					return;
				
				Wrappers.Link.Action action = d_actionStore.GetValue(iter, 0) as Wrappers.Link.Action;
				
				if (action.Equation == args.NewText)
					return;
				
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
			
			d_store = new NodeStore();
			d_treeview = new NodeView(d_store);	
			
			d_treeview.ShowExpanders = false;
			
			vw.Add(d_treeview);
			
			d_treeview.Show();
			vw.Show();

			vbox.PackStart(vw, true, true, 0);
			
			CellRendererText renderer = new CellRendererText();
			TreeViewColumn column = new TreeViewColumn("Name", renderer, new object[] {"text", 0});
			column.Resizable = true;
			column.MinWidth = 75;
			
			if (d_object != null)
			{
				renderer.Edited += DoNameEdited;
			}

			d_treeview.AppendColumn(column);
			
			renderer = new CellRendererText();
			
			if (d_object != null)
			{
				renderer.Edited += DoValueEdited;
			}
				
			column = new TreeViewColumn("Value", renderer, new object[] {"text", 1});
			column.Resizable = true;
			
			d_treeview.AppendColumn(column);
				
			// Add column for property flags
			CellRendererCombo combo = new CellRendererCombo();
			
			column = new TreeViewColumn("Flags", combo);
			column.Resizable = true;
			column.SetCellDataFunc(combo, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
				Node node = d_store.GetNode(model.GetPath(piter)) as Node;
				
				(cell as CellRendererText).Text = FlagsToString(node.Flags);
				
				(cell as CellRendererText).Editable = node.Name != "id";
				cell.Sensitive = node.Name != "id";
			});
			
			combo.EditingStarted += DoEditingStarted;
			combo.Edited += DoHintEdited;
			combo.HasEntry = false;
			
			d_hintStore = new ListStore(typeof(string), typeof(Cpg.PropertyFlags));
			combo.Model = d_hintStore;
			combo.TextColumn = 0;
			
			column.MinWidth = 50;
			d_treeview.AppendColumn(column);

			HBox hbox = new HBox(false, 3);
			vbox.PackStart(hbox, false, false, 0);

			d_treeview.KeyPressEvent += DoTreeViewKeyPress;
			
			d_removeButton = new Button();
			d_removeButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_removeButton.Sensitive = false;
			d_removeButton.Clicked += DoRemoveProperty;
			hbox.PackStart(d_removeButton, false, false ,0);

			Button but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAddProperty;
			hbox.PackStart(but, false, false, 0);

			d_treeview.NodeSelection.Changed += DoSelectionChanged;
			
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
		
		private void FillHintStore(Node node)
		{
			d_hintStore.Clear();
			Cpg.PropertyFlags flags = node.Flags;
			
			foreach (KeyValuePair<string, Cpg.PropertyFlags> pair in d_flaglist)
			{
				string name = pair.Key;

				if ((flags & pair.Value) != 0)
				{
					name = "• " + name;
				}
				
				d_hintStore.AppendValues(name, flags);
			}
		}

		private void DoEditingStarted(object o, EditingStartedArgs args)
		{
			Node node = (d_store.GetNode(new TreePath(args.Path)) as Node);
			
			FillHintStore(node);
		}
		
		private void InitStore()
		{
			foreach (Cpg.Property prop in d_object.Properties)
			{
				AddProperty(prop);
			}
		}
		
		private void DoHintEdited(object source, EditedArgs args)
		{
			Node node = (d_store.GetNode(new TreePath(args.Path)) as Node);
			bool wason = false;
			string name = args.NewText;
			
			if (name.StartsWith("• "))
			{
				wason = true;
				name = name.Substring(2);
			}

			Cpg.PropertyFlags flags = (Cpg.PropertyFlags)Enum.Parse(typeof(Cpg.PropertyFlags), name);
			
			if (wason)
			{
				node.Flags &= ~flags;
			}
			else
			{
				node.Flags |= flags;
			}
		}
		
		private void DoValueEdited(object source, EditedArgs args)
		{
			(d_store.GetNode(new TreePath(args.Path)) as Node).Value = args.NewText;
		}
		
		private void DoNameEdited(object source, EditedArgs args)
		{
			if (args.NewText == String.Empty)
				return;
			
			Node node = (d_store.GetNode(new TreePath(args.Path)) as Node);
			
			if (args.NewText == node.Name)
				return;

			string oldprop = node.Name;
			//string oldvalue = node.Value;
			
			try
			{
				d_object.RemoveProperty(oldprop);
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
				return;
			}
			
			// TODO
			//node.Name = args.NewText;
			//d_object.SetProperty(node.Name, oldvalue);
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			NodeSelection selection = source as NodeSelection;
			Node node = selection.SelectedNode as Node;
			
			d_removeButton.Sensitive = node != null;
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
		
		private void DoTreeViewKeyPress(object source, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
				DoRemoveProperty(source, new EventArgs());
		}
		
		private void AddProperty(Cpg.Property prop)
		{
			if (PropertyExists(prop.Name))
			{
				return;
			}

			Node node = new Node(prop);
			d_store.AddNode(node);
			
			if (d_selectProperty)
			{
				d_treeview.NodeSelection.UnselectAll();
				d_treeview.NodeSelection.SelectNode(node);
				
				TreeModel model;
				TreeIter iter;
				
				if (d_treeview.Selection.GetSelected(out model, out iter))
				{
					TreePath path = d_treeview.Model.GetPath(iter);
					
					d_treeview.SetCursor(path, d_treeview.GetColumn(0), true);
				}
			}
		}
		
		private bool PropertyExists(string name)
		{
			return FindProperty(name) != null;
		}
		
		private void DoAddProperty(object source, EventArgs args)
		{
			int num = 1;
			
			while (PropertyExists("x" + num))
			{
				++num;
			}
			
			d_selectProperty = true;

			d_object.AddProperty("x" + num, "0");
			d_selectProperty = false;
		}
		
		private void DoRemoveProperty(object source, EventArgs args)
		{
			NodeSelection selection = d_treeview.NodeSelection;
			
			Node[] nodes = new Node[selection.SelectedNodes.Length];
			selection.SelectedNodes.CopyTo(nodes, 0);
			
			foreach (Node node in nodes)
			{
				try
				{
					d_object.RemoveProperty(node.Name);
					d_store.RemoveNode(node);
				}
				catch (GLib.GException err)
				{
					// Display could not remove, or something
					Error(this, err);
				}
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
					d_actionView.Selection.SelectIter(iter);
			}
		}
		
		private void DoPropertyAdded(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			AddProperty(prop);
		}
		
		private Node FindProperty(string name, out TreePath path)
		{
			TreeModel model = d_treeview.Model;
			TreeIter iter;
			
			path = null;
			
			if (!model.GetIterFirst(out iter))
				return null;
			
			do
			{
				path = model.GetPath(iter);
				Node node = d_store.GetNode(path) as Node;
				
				if (node.Name == name)
				{
					return node;
				}
			} while (model.IterNext(ref iter));	
			
			return null;
		}
		
		private Node FindProperty(string name)
		{
			TreePath path;
			return FindProperty(name, out path);
		}
		
		private void DoPropertyChanged(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			TreePath path;
			Node node = FindProperty(prop.Name, out path);
			
			if (node != null)
			{
				TreeIter iter;
				
				if (d_treeview.Model.GetIter(out iter, path))
				{
					d_treeview.Model.EmitRowChanged(path, iter);
				}
			}
		}
		
		private void DoPropertyRemoved(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			Node node = FindProperty(prop.Name);
			
			if (node != null)
			{
				d_store.RemoveNode(node);
			}
		}
		
		private void Clear()
		{
			while (Children.Length > 0)
				Remove(Children[0]);
			
			if (d_store != null)
				d_store.Clear();
				
			d_object = null;
			Sensitive = false;
		}
		
		public void Select(Cpg.Property property)
		{
			Node node = FindProperty(property.Name);
			
			if (node != null)
				d_treeview.NodeSelection.SelectNode(node);
		}
		
		public void Select(Cpg.LinkAction action)
		{
			TreeIter iter;
			
			if (!d_actionStore.GetIterFirst(out iter))
				return;
			
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
		
		protected override void OnRealized ()
		{
			base.OnRealized();
			
			Position = Allocation.Width / 2;
		}

	}
}
