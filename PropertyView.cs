using System;
using Gtk;
using System.Text.RegularExpressions;

namespace Cpg.Studio
{
	public class PropertyView : HPaned
	{
		class Node : TreeNode
		{
			Components.Object d_object;
			string d_property;
			
			public Node(Components.Object obj, string name)
			{
				d_object = obj;
				d_property = name;
			}
			
			[TreeNodeValue(Column=0)]
			public string Name
			{
				get
				{
					return d_property;
				}
				set
				{
					d_property = value;
				}
			}
			
			[TreeNodeValue(Column=1)]
			public string Value
			{
				get
				{
					return d_object[d_property];
				}
				set
				{
					d_object[d_property] = value;
				}
			}
			
			[TreeNodeValue(Column=2)]
			public bool Integrated
			{
				get
				{
					return (d_object is Components.Simulated) ? (d_object as Components.Simulated).GetIntegrated(d_property) : false;
				}
				set
				{
					if (d_object is Components.Simulated)
					{
						(d_object as Components.Simulated).SetIntegrated(d_property, value);
					}
				}
			}
		}
		
		class NodeStore : Gtk.NodeStore
		{
			public NodeStore() : base(typeof(Node))
			{
			}
		}
		
		private Components.Object d_object;
		private NodeStore d_store;
		private NodeView d_treeview;
		private Button d_removeButton;
		private bool d_selectProperty;
		private ListStore d_comboStore;
		private ListStore d_actionStore;
		private TreeView d_actionView;
		private Button d_removeActionButton;
		
		public PropertyView(Components.Object obj) : base()
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
			Add1(vbox);
			
			Gtk.Label label = new Label("<b>Actions</b>");
			label.Xalign = 0;
			label.UseMarkup = true;
			
			vbox.PackStart(label, false, true, 0);
			
			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			d_actionStore = new Gtk.ListStore(typeof(Components.Link.Action), typeof(string), typeof(string));
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
				
				Components.Link.Action action = d_actionStore.GetValue(iter, 0) as Components.Link.Action;
				
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
				
				Components.Link.Action action = d_actionStore.GetValue(iter, 0) as Components.Link.Action;
				
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
			
			Components.Link link = d_object as Components.Link;
			link.To.PropertyAdded += DoTargetPropertyAdded;
			link.To.PropertyRemoved += DoTargetPropertyRemoved;
			
			foreach (string prop in link.To.Properties)
			{
				if (prop != "id")
					d_comboStore.AppendValues(prop);
			}
			
			foreach (Components.Link.Action action in link.Actions)
			{
				d_actionStore.AppendValues(action, action.Target, action.Equation);
			}
			
			d_actionView.Selection.Changed += DoActionSelectionChanged;
		}

		private void DoTargetPropertyAdded(Components.Object obj, string prop)
		{
			if (prop != "id")
				d_comboStore.AppendValues(prop);
		}
		
		private void DoTargetPropertyRemoved(Components.Object obj, string prop)
		{
			TreeIter iter;
			
			if (!d_comboStore.GetIterFirst(out iter))
				return;
			
			do
			{
				string val = d_comboStore.GetValue(iter, 0).ToString();
				
				if (val == prop)
				{
					d_comboStore.Remove(ref iter);
					break;
				}				
			} while (d_comboStore.IterNext(ref iter));
		}
		
		public void Initialize(Components.Object obj)
		{
			Clear();
			
			d_object = obj;
			
			if (d_object != null && d_object is Components.Link)
			{
				AddEquationsUI();
			}
			
			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			Add2(vbox);
			
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
			
			column.SetCellDataFunc(renderer, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
				Node node = d_store.GetNode(model.GetPath(piter)) as Node;
				(cell as CellRendererText).Editable = d_object != null && !d_object.IsPermanent(node.Name);
			});
			
			if (d_object != null)
				renderer.Edited += DoNameEdited;

			d_treeview.AppendColumn(column);
			
			renderer = new CellRendererText();
			
			if (d_object != null)
				renderer.Edited += DoValueEdited;
				
			column = new TreeViewColumn("Value", renderer, new object[] {"text", 1});
			column.Resizable = true;
			column.SetCellDataFunc(renderer, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
				Node node = d_store.GetNode(model.GetPath(piter)) as Node;
				(cell as CellRendererText).Editable = d_object != null && !d_object.IsReadOnly(node.Name);
			});
			
			column.MinWidth = 100;
			d_treeview.AppendColumn(column);
			
			if (d_object != null && d_object is Components.Simulated)
			{
				CellRendererToggle toggle = new CellRendererToggle();
				
				toggle.Toggled += delegate (object source, ToggledArgs args) {
					Node node = d_store.GetNode(new TreePath(args.Path)) as Node;
					node.Integrated = !node.Integrated;
				};
				
				column = new TreeViewColumn("Integrate", toggle);
				column.Resizable = true;
				column.SetCellDataFunc(toggle, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
					Node node = d_store.GetNode(model.GetPath(piter)) as Node;
					
					(cell as CellRendererToggle).Active = (d_object as Components.Simulated).GetIntegrated(node.Name);
					cell.Sensitive = node.Name != "id";
				});
				
				column.MinWidth = 10;
				column.Expand = false;
				
				d_treeview.AppendColumn(column);
			}
			
			if (d_object != null)
			{
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
				
				d_object.PropertyAdded += DoPropertyAdded;
				d_object.PropertyChanged += DoPropertyChanged;
				d_object.PropertyRemoved += DoPropertyRemoved;
				
				InitStore();
				Sensitive = true;
			}
			
			column = new TreeViewColumn();
			d_treeview.AppendColumn(column);
			
			ShowAll();
		}
		
		private void InitStore()
		{
			foreach (string prop in d_object.Properties)
				AddProperty(prop);
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
			string oldvalue = node.Value;
			
			node.Name = args.NewText;
			d_object.RemoveProperty(oldprop);
			d_object.SetProperty(node.Name, oldvalue);
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			NodeSelection selection = source as NodeSelection;
			Node node = selection.SelectedNode as Node;
			
			d_removeButton.Sensitive = node != null && !d_object.IsPermanent(node.Name);
		}
		
		private void DoActionSelectionChanged(object source, EventArgs args)
		{
			if (d_actionView.Selection.CountSelectedRows() == 0)
				d_removeActionButton.Sensitive = false;
			else
				d_removeActionButton.Sensitive = true;
		}
		
		private void DoTreeViewKeyPress(object source, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
				DoRemoveProperty(source, new EventArgs());
		}
		
		private void AddProperty(string name)
		{
			if (PropertyExists(name))
				return;

			Node node = new Node(d_object, name);
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
				++num;
			
			d_selectProperty = true;
			d_object["x" + num] = "0";
			d_selectProperty = false;
		}
		
		private void DoRemoveProperty(object source, EventArgs args)
		{
			NodeSelection selection = d_treeview.NodeSelection;
			
			Node[] nodes = new Node[selection.SelectedNodes.Length];
			selection.SelectedNodes.CopyTo(nodes, 0);
			
			foreach (Node node in nodes)
			{
				if (!d_object.IsPermanent(node.Name))
				{
					d_object.RemoveProperty(node.Name);
					d_store.RemoveNode(node);
				}
			}
		}
		
		private void DoAddAction(object source, EventArgs args)
		{
			Components.Link link = d_object as Components.Link;
			
			string[] props = link.To.Properties;
			
			if (props.Length == 0 || (props[0] == "id" && props.Length == 1))
				return;
			
			Components.Link.Action action = link.AddAction(props[0] == "id" ? props[1] : props[0], "");
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
			
			Components.Link.Action val = model.GetValue(iter, 0) as Components.Link.Action;	
	
			if ((d_object as Components.Link).RemoveAction(val))
			{
				if (d_actionStore.Remove(ref iter))
					d_actionView.Selection.SelectIter(iter);
			}
		}
		
		private void DoPropertyAdded(Components.Object obj, string name)
		{
			AddProperty(name);
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
					return node;
			} while (model.IterNext(ref iter));	
			
			return null;
		}
		
		private Node FindProperty(string name)
		{
			TreePath path;
			return FindProperty(name, out path);
		}
		
		private void DoPropertyChanged(Components.Object obj, string name)
		{
			TreePath path;
			Node node = FindProperty(name, out path);
			
			if (node != null)
			{
				TreeIter iter;
				
				if (d_treeview.Model.GetIter(out iter, path))
					d_treeview.Model.EmitRowChanged(path, iter);
			}
		}
		
		private void DoPropertyRemoved(Components.Object obj, string name)
		{
			Node node = FindProperty(name);
			
			if (node != null)
				d_store.RemoveNode(node);
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
				Components.Link.Action o = d_actionStore.GetValue(iter, 0) as Components.Link.Action;
				
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
