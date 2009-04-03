using System;
using Gtk;
using System.Text.RegularExpressions;

namespace Cpg.Studio.GtkGui
{
	public class PropertyView : VBox
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
						(d_object as Components.Simulated).SetIntegrated(d_property, value);
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
		private Entry d_entry;
		private ToolButton d_removeButton;
		
		public PropertyView(Components.Object obj) : base(false, 3)
		{
			Init(obj);
			ShowAll();
		}
		
		public PropertyView() : this(null)
		{
		}
		
		private void Init(Components.Object obj)
		{
			Clear();
			
			d_object = obj;
			
			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			
			d_store = new NodeStore();
			d_treeview = new NodeView(d_store);			
			
			vw.Add(d_treeview);
			
			d_treeview.Show();
			vw.Show();
			
			PackEnd(vw, true, true, 0);
			
			TreeViewColumn column = new TreeViewColumn("Name", new CellRendererText(), new object[] {"text", 0});
			column.Resizable = true;
			column.MinWidth = 75;
			d_treeview.AppendColumn(column);
			
			CellRendererText renderer = new CellRendererText();
			
			if (d_object != null)
				renderer.Edited += DoValueEdited;
				
			column = new TreeViewColumn("Value", renderer);
			column.Resizable = true;
			column.SetCellDataFunc(renderer, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
				(cell as CellRendererText).Text = (d_store.GetNode(model.GetPath(piter)) as Node).Name;
				(cell as CellRendererText).Editable = d_object != null && !(d_object.IsReadOnly(model.GetValue(piter, 0).ToString()));
			});
			
			column.MinWidth = 100;
			d_treeview.AppendColumn(column);
			
			if (d_object != null && d_object is Components.Simulated)
			{
				CellRendererToggle toggle = new CellRendererToggle();
				
				toggle.Toggled += delegate (object source, ToggledArgs args) {
					Node node = d_store.GetNode(new TreePath(args.Path)) as Node;
					node.Integrated = (source as CellRendererToggle).Active;
				};
				
				column = new TreeViewColumn("Int", toggle);
				column.Resizable = true;
				column.SetCellDataFunc(toggle, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
					(cell as CellRendererToggle).Active = (d_object as Components.Simulated).GetIntegrated(model.GetValue(piter, 0).ToString());
				});
			}
			
			if (d_object != null)
			{
				HBox hbox = new HBox(false, 3);
				PackStart(hbox, false, false, 0);
				
				d_entry = new Entry();
				hbox.PackStart(d_entry, true, true, 0);
				
				d_entry.KeyPressEvent += DoEntryKeyPress;
				d_treeview.KeyPressEvent += DoTreeViewKeyPress;
				
				ToolButton but = new ToolButton(Gtk.Stock.Add);
				but.Clicked += DoAddProperty;
				hbox.PackStart(but, false, false ,0);
				
				d_removeButton = new ToolButton(Gtk.Stock.Remove);
				d_removeButton.Sensitive = false;
				d_removeButton.Clicked += DoRemoveProperty;
				hbox.PackStart(d_removeButton, false, false ,0);
				
				d_treeview.NodeSelection.Changed += DoSelectionChanged;
				
				d_object.PropertyAdded += DoPropertyAdded;
				d_object.PropertyChanged += DoPropertyChanged;
				d_object.PropertyRemoved += DoPropertyRemoved;
				
				InitStore();
				Sensitive = true;
			}
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
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			NodeSelection selection = source as NodeSelection;
			Node node = selection.SelectedNode as Node;
			
			d_removeButton.Sensitive = node != null && !d_object.IsPermanent(node.Name);
		}
		
		private void DoEntryKeyPress(object source, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return)
			{
				DoAddProperty(source, new EventArgs());
				(source as Entry).SelectRegion(0, -1);
			}
		}
		
		private void DoTreeViewKeyPress(object source, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
				DoRemoveProperty(source, new EventArgs());
		}
		
		private void AddProperty(string name)
		{
			Node node = new Node(d_object, name); 
			d_store.AddNode(node);
		}
		
		private void DoAddProperty(object source, EventArgs args)
		{
			string s = d_entry.Text;
			
			if (s == String.Empty || !Regex.IsMatch(s, "^[a-zA-Z][a-zA-Z0-9_]*$"))
				return;
			
			if (d_object.HasProperty(s))
				return;
			
			d_object[s] = "";
		}
		
		private void DoRemoveProperty(object source, EventArgs args)
		{
			NodeSelection selection = d_treeview.NodeSelection;
			
			Node[] nodes = new Node[selection.SelectedNodes.Length];
			selection.SelectedNodes.CopyTo(nodes, 0);
			
			foreach (Node node in nodes)
			{
				if (!d_object.IsPermanent(node.Name))
					d_store.RemoveNode(node);
			}
		}
		
		private void DoPropertyAdded(Components.Object obj, string name)
		{
			AddProperty(name);
		}
		
		private TreePath FindProperty(string name)
		{
			TreeModel model = d_store as TreeModel;
			TreePath ret = null;
			
			model.Foreach(delegate (TreeModel mod, TreePath path, TreeIter piter)
			{
				if ((d_store.GetNode(path) as Node).Name == name)
				{
					ret = path;
					return false;
				}
				
				return true;
			});
			
			return ret;
		}
		
		private void DoPropertyChanged(Components.Object obj, string name)
		{
			TreePath path = FindProperty(name);
			
			if (path != null)
			{
				TreeModel model = d_store as TreeModel;
				TreeIter piter;
			
				model.GetIter(out piter, path);
				model.EmitRowChanged(path, piter);
			}
		}
		
		private void DoPropertyRemoved(Components.Object obj, string name)
		{
			TreePath path = FindProperty(name);
			
			if (path != null)
				d_store.RemoveNode(d_store.GetNode(path));
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
	}
}
