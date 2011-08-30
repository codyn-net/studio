using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class WrappersTree : VBox
	{
		public class WrapperNode : Node
		{
			public enum HeaderType
			{
				State,
				Link,
				Property,
				Action
			}
			
			public struct Column
			{
				public const int Name = 0;
				public const int Icon = 1;
				public const int Sensitive = 2;
				public const int Checked = 3;
				public const int Inconsistent = 4;
			}

			private Wrappers.Wrapper d_wrapper;
			private HeaderType d_header;
			private Gdk.Pixbuf d_icon;
			private Widget d_widget;
			private GLib.Object d_object;
			private bool d_checked;
			private List<WrapperNode> d_inconsistent;

			private static Dictionary<Type, Gdk.Pixbuf> s_iconmap;
			
			public delegate void ToggledHandler(WrapperNode node);
			
			public event ToggledHandler Toggled = delegate {};
			
			static WrapperNode()
			{
				s_iconmap = new Dictionary<Type, Gdk.Pixbuf>();
			}
			
			public Gdk.Pixbuf WrapperIcon()
			{
				Gdk.Pixbuf icon = null;
				Type type = d_wrapper != null ? d_wrapper.GetType() : d_object.GetType();
				string stockid;

				if (s_iconmap.TryGetValue(type, out icon))
				{
					return icon;
				}
				
				if (d_wrapper != null)
				{			
					if (d_wrapper is Wrappers.Group)
					{
						stockid = Stock.GroupState;
					}
					else if (d_wrapper is Wrappers.Link)
					{
						stockid = Stock.Link;
					}
					else
					{
						stockid = Stock.State;
					}
					
					icon = d_widget.RenderIcon(stockid, IconSize.Menu, null);
				}
			
				s_iconmap[type] = icon;
			
				return icon;
			}
			
			public WrapperNode(Widget widget, Cpg.Property property) : this(widget, null, property)
			{
			}
			
			public WrapperNode(Widget widget, Cpg.LinkAction action) : this(widget, null, action)
			{
			}
			
			public WrapperNode(Widget widget, Wrappers.Wrapper wrapper) : this(widget, wrapper, null)
			{
			}
			
			public WrapperNode(Widget widget, Wrappers.Wrapper wrapper, GLib.Object obj)
			{
				d_widget = widget;
				d_wrapper = wrapper;
				d_object = obj;
				
				d_icon = WrapperIcon();
				d_inconsistent = new List<WrapperNode>();
				
				if (d_wrapper != null)
				{
					Wrappers.Group grp = wrapper as Wrappers.Group;
					
					if (grp != null)
					{
						grp.ChildAdded += OnChildAdded;
						grp.ChildRemoved += OnChildRemoved;
						
						foreach (Wrappers.Wrapper wr in grp.Children)
						{
							OnChildAdded(grp, wr);
						}
					}
					
					Wrappers.Link link = wrapper as Wrappers.Link;
					
					if (link != null)
					{
						d_wrapper.WrappedObject.AddNotification("from", OnLinkChanged);
						d_wrapper.WrappedObject.AddNotification("to", OnLinkChanged);
					}
					
					d_wrapper.WrappedObject.AddNotification("id", OnIdChanged);
					
					d_wrapper.PropertyAdded += OnPropertyAdded;
					d_wrapper.PropertyRemoved += OnPropertyRemoved;
					
					foreach (Cpg.Property prop in d_wrapper.WrappedObject.Properties)
					{
						OnPropertyAdded(wrapper, prop);	
					}
				}
				
				if (d_object != null)
				{
					if (d_object is Cpg.Property)
					{
						d_object.AddNotification("name", OnIdChanged);
					}
					else if (d_object is Cpg.LinkAction)
					{
						d_object.AddNotification("target", OnIdChanged);
					}
				}
			}
			
			override public void Dispose()
			{
				base.Dispose();

				if (d_wrapper != null)
				{
					d_wrapper.WrappedObject.RemoveNotification("from", OnLinkChanged);
					d_wrapper.WrappedObject.RemoveNotification("to", OnLinkChanged);
					d_wrapper.WrappedObject.RemoveNotification("id", OnIdChanged);
					
					Wrappers.Group grp = d_wrapper as Wrappers.Group;
					
					if (grp != null)
					{
						grp.ChildAdded -= OnChildAdded;
						grp.ChildRemoved -= OnChildRemoved;
					}
					
					d_wrapper.PropertyAdded -= OnPropertyAdded;
					d_wrapper.PropertyRemoved -= OnPropertyRemoved;
				}
				
				if (d_object != null)
				{
					if (d_object is Cpg.Property)
					{
						d_object.RemoveNotification("name", OnIdChanged);
					}
					else if (d_object is Cpg.LinkAction)
					{
						d_object.RemoveNotification("target", OnIdChanged);
					}
					
					d_object = null;
				}
				
				d_wrapper = null;
			}
			
			private void OnLinkChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			private void OnIdChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			private void OnChildAdded(Wrappers.Group grp, Wrappers.Wrapper child)
			{
				TreeIter iter;
				
				Add(new WrapperNode(d_widget, child), out iter);
			}
			
			private void OnChildRemoved(Wrappers.Group grp, Wrappers.Wrapper child)
			{
				foreach (WrapperNode node in AllChildren)
				{
					if (node.Wrapper == child)
					{
						Remove(node);
						
						break;
					}
				}
			}

			private void OnPropertyAdded(Wrappers.Wrapper wrapper, Cpg.Property property)
			{
				TreeIter iter;
				
				Add(new WrapperNode(d_widget, property), out iter);
			}
			
			private void OnPropertyRemoved(Wrappers.Wrapper wrapper, Cpg.Property property)
			{
				foreach (WrapperNode node in AllChildren)
				{
					if (node.d_object == property)
					{
						Remove(node);
						
						break;
					}
				}
			}
			
			public WrapperNode(HeaderType header)
			{
				d_header = header;
			}
			
			[PrimaryKey()]
			public Wrappers.Wrapper Wrapper
			{
				get
				{
					return d_wrapper;
				}
			}
			
			private string HeaderName
			{
				get
				{
					switch (d_header)
					{
						case HeaderType.Link:
							return "Links";
						case HeaderType.State:
							return "States";
					}
					
					return "None";
				}
			}
			
			private string ObjectName(bool canheader)
			{
				if (d_wrapper != null)
				{
					return d_wrapper.Id;
				}
				else if (d_object != null)
				{
					Cpg.Property prop = d_object as Cpg.Property;
					
					if (prop != null)
					{
						return prop.Name;
					}
					
					Cpg.LinkAction action = d_object as Cpg.LinkAction;
					
					if (action != null)
					{
						return action.Target;
					}
				}
				
				if (canheader)
				{
					return HeaderName;
				}
				else
				{
					return null;
				}
			}
			
			[PrimaryKey]
			public string FilterName
			{
				get
				{
					return ObjectName(false);
				}
			}
			
			[NodeColumn(Column.Name)]
			public string Name
			{
				get
				{
					return ObjectName(true);
				}
			}
			
			[NodeColumn(Column.Icon)]
			public Gdk.Pixbuf Icon
			{
				get
				{
					return d_icon;
				}
			}
			
			[NodeColumn(Column.Sensitive)]
			public bool Sensitive
			{
				get
				{
					return d_wrapper == null || !(d_wrapper is Wrappers.Import);
				}
			}
			
			[PrimaryKey()]
			public Cpg.Property Property
			{
				get
				{
					return d_object != null ? d_object as Cpg.Property : null;
				}
			}
			
			[PrimaryKey()]
			public Cpg.LinkAction Action
			{
				get
				{
					return d_object != null ? d_object as Cpg.LinkAction : null;
				}
			}
			
			private void PropagateChecked(bool val)
			{
				d_inconsistent.Clear();

				if (d_checked != val)
				{
					d_checked = val;

					EmitChanged();
					Toggled(this);
				}
				
				foreach (WrapperNode node in AllChildren)
				{
					node.PropagateChecked(val);
				}
			}
			
			private void UpdateInconsistency(WrapperNode node, bool val)
			{
				if (val != d_checked)
				{
					if (!d_inconsistent.Contains(node))
					{
						d_inconsistent.Add(node);
						EmitChanged();
					}
				}
				else
				{
					if (d_inconsistent.Contains(node))
					{
						d_inconsistent.Remove(node);
						EmitChanged();
					}
				}
				
				if (Parent != null && Parent is WrapperNode)
				{
					((WrapperNode)Parent).UpdateInconsistency(node, val);
				}
			}
			
			[NodeColumn(Column.Checked)]
			public bool Checked
			{
				get
				{
					return d_checked;
				}
				set
				{
					PropagateChecked(value);

					if (Parent != null && Parent is WrapperNode)
					{
						((WrapperNode)Parent).UpdateInconsistency(this, value);
					}
				}
			}
			
			[NodeColumn(Column.Inconsistent)]
			public bool Inconsistent
			{
				get
				{
					return d_inconsistent.Count != 0;
				}
			}
			
			protected HeaderType DerivedHeaderType
			{
				get
				{
					if (d_wrapper != null)
					{
						if (d_wrapper is Wrappers.Link)
						{
							return HeaderType.Link;
						}
						else
						{
							return HeaderType.State;
						}
					}
					else if (d_object != null)
					{
						if (d_object is Cpg.Property)
						{	
							return HeaderType.Property;
						}
						else if (d_object is Cpg.LinkAction)
						{
							return HeaderType.Action;
						}
					}

					return d_header;
				}
			}
			
			[SortColumn(Column.Name)]
			public int Sort(WrapperNode b)
			{
				HeaderType ad = DerivedHeaderType;
				HeaderType bd = b.DerivedHeaderType;
				
				if (ad != bd)
				{
					return ((int)ad).CompareTo((int)bd);
				}
				
				if (d_wrapper == null)
				{
					return -1;
				}
				else if (b.Wrapper == null)
				{
					return 1;
				}
				else
				{
					return Name.CompareTo(b.Name);
				}
			}
		}

		private Widgets.TreeView<WrapperNode> d_treeview;
		private Entry d_entry;
		private string d_searchText;
		private Wrappers.Group d_group;
		private Dictionary<GLib.Object, bool> d_selected;
		
		private CellRendererToggle d_rendererToggle;
		private CellRendererPixbuf d_rendererIcon;
		private CellRendererText d_rendererName;
		
		public delegate void NodeHandler(object source, WrapperNode node);
		public delegate void PopulatePopupHandler(object source, WrapperNode[] nodes, Gtk.Menu menu);

		public event NodeHandler Activated = delegate {};
		public event NodeHandler Toggled = delegate {};
		public event PopulatePopupHandler PopulatePopup = delegate {};
		
		public delegate void WrapperFilter(WrapperNode node, ref bool ret);
		private WrapperFilter d_filterStorage;

		public event WrapperFilter Filter
		{
			add
			{
				d_filterStorage += value;
				d_treeview.NodeStore.Filter(FilterFunc);
			}
			remove
			{
				d_filterStorage -= value;
			}
		}

		public WrappersTree(Wrappers.Group parent) : base(false, 3)
		{
			d_treeview = new Widgets.TreeView<WrapperNode>();
			d_group = parent;
			
			TreeViewColumn col;
			
			col = new TreeViewColumn();
			
			d_rendererToggle = new CellRendererToggle();
			col.PackStart(d_rendererToggle, false);
			col.SetAttributes(d_rendererToggle,
			                  "active", WrapperNode.Column.Checked,
			                  "inconsistent", WrapperNode.Column.Inconsistent);
			
			d_rendererToggle.Toggled += OnRendererToggleToggled;
			
			d_rendererIcon = new CellRendererPixbuf();
			col.PackStart(d_rendererIcon, false);
			col.SetAttributes(d_rendererIcon, "pixbuf", WrapperNode.Column.Icon);

			d_rendererName = new CellRendererText();
			col.PackStart(d_rendererName, true);
			col.SetAttributes(d_rendererName, "text", WrapperNode.Column.Name, "sensitive", WrapperNode.Column.Sensitive);

			d_treeview.AppendColumn(col);
			
			d_treeview.HeadersVisible = false;
			d_treeview.NodeStore.SortColumn = 0;
			d_treeview.ShowExpanders = false;
			d_treeview.LevelIndentation = 6;
			d_treeview.EnableSearch = false;
			d_treeview.SearchColumn = -1;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			
			d_treeview.StartInteractiveSearch += OnTreeViewInteractiveSearch;
			
			d_treeview.RowActivated += OnTreeViewRowActivated;
			d_treeview.PopulatePopup += OnTreeViewPopulatePopup;
			
			// Keep expanded
			d_treeview.Model.RowInserted += delegate(object o, RowInsertedArgs args) {
				TreeRowReference r = new TreeRowReference(d_treeview.Model, args.Path);

				GLib.Idle.Add(delegate {
					if (r.Valid())
					{
						d_treeview.ExpandToPath(r.Path);
					}

					return false;
				});
			};

			d_treeview.NodeStore.NodeAdded += delegate(Node par, Node child) {
				WrapperNode n = (WrapperNode)child;
				n.Toggled += OnNodeToggled;
				
				foreach (WrapperNode c in n.Descendents)
				{
					c.Toggled += OnNodeToggled;
				}
			};
			
			d_treeview.NodeStore.NodeRemoved += delegate(Node par, Node child, int wasAtIndex) {
				WrapperNode n = (WrapperNode)child;
				n.Toggled -= OnNodeToggled;
				
				foreach (WrapperNode c in n.Descendents)
				{
					c.Toggled -= OnNodeToggled;
				}
			};
			
			Build(parent);
			
			d_treeview.Show();
			
			ScrolledWindow wd = new ScrolledWindow();
			wd.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			wd.ShadowType = ShadowType.EtchedIn;
			wd.Add(d_treeview);
			wd.Show();
			
			PackStart(wd, true, true, 0);
			
			HBox hbox = new HBox(false, 3);
			hbox.Show();
			
			d_entry = new Entry();
			d_entry.Show();
			
			Image img = new Image(Gtk.Stock.Find, IconSize.Button);
			img.Show();
			
			hbox.PackStart(img, false, false, 0);
			hbox.PackStart(d_entry, true, true, 0);
			hbox.BorderWidth = 3;
			
			PackStart(hbox, false, false, 0);

			d_entry.Changed += HandleEntryChanged;
			d_entry.KeyPressEvent += HandleEntryKeyPressEvent;
			
			d_treeview.ExpandAll();
			d_treeview.NodeStore.Filter(FilterFunc);
		}

		private void OnNodeToggled(WrapperNode node)
		{
			Toggled(this, node);
		}

		[GLib.ConnectBeforeAttribute]
		private void OnTreeViewInteractiveSearch(object o, StartInteractiveSearchArgs args)
		{
			d_entry.GrabFocus();
		}

		private void OnRendererToggleToggled(object o, ToggledArgs args)
		{
			WrapperNode node = d_treeview.NodeStore.FindPath(args.Path);
			
			if (node != null)
			{
				node.Checked = !node.Checked;
			}
		}
		
		public CellRendererToggle RendererToggle
		{
			get
			{
				return d_rendererToggle;
			}
		}
		
		public CellRendererPixbuf RendererIcon
		{
			get
			{
				return d_rendererIcon;
			}
		}
		
		public CellRendererText RendererName
		{
			get
			{
				return d_rendererName;
			}
		}
		
		public Widgets.TreeView<WrapperNode> TreeView
		{
			get
			{
				return d_treeview;
			}
		}
		
		private void OnTreeViewPopulatePopup(object o, Gtk.Menu menu)
		{
			List<WrapperNode> nodes = new List<WrapperNode>();

			d_treeview.Selection.SelectedForeach(delegate (TreeModel model, TreePath path, TreeIter iter) {
				nodes.Add(d_treeview.NodeStore.GetFromIter(iter));
			});
			
			PopulatePopup(this, nodes.ToArray(), menu);
		}

		void OnTreeViewRowActivated(object o, RowActivatedArgs args)
		{
			WrapperNode node = d_treeview.NodeStore.FindPath(args.Path);
			
			if (node != null)
			{
				Activated(this, node);
			}
		}

		private void HandleEntryKeyPressEvent(object o, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Escape)
			{
				((Entry)o).Text = "";
			}
		}
		
		private bool FilterFunc(Node node)
		{
			WrapperNode wp = (WrapperNode)node;
			
			if (!String.IsNullOrEmpty(d_searchText) && !wp.FilterName.ToLowerInvariant().Contains(d_searchText))
			{
				if (d_selected == null)
				{
					return false;
				}
				else if (wp.Wrapper != null && !d_selected.ContainsKey(wp.Wrapper.WrappedObject))
				{
					return false;
				}
				else if (wp.Property != null && !d_selected.ContainsKey(wp.Property))
				{
					return false;
				}
				else if (wp.Action != null && !d_selected.ContainsKey(wp.Action))
				{
					return false;
				}
			}
			
			Wrappers.Link link = wp.Wrapper as Wrappers.Link;
			
			if (link != null && (link.From != null || link.To != null))
			{
				return false;
			}
			
			bool ret = true;
			
			if (d_filterStorage != null)
			{
				d_filterStorage(wp, ref ret);
			}
			
			return ret;
		}
		
		public Entry Entry
		{
			get
			{
				return d_entry;
			}
		}
		
		private void AddAll(GLib.Object obj, Dictionary<GLib.Object, bool> ret)
		{
			ret[obj] = true;
			
			Cpg.Object o = obj as Cpg.Object;

			Cpg.Group grp = obj as Cpg.Group;
						
			if (grp != null)
			{
				foreach (Cpg.Object child in grp.Children)
				{
					AddAll(child, ret);
				}
				
				foreach (string name in grp.PropertyInterface.Names)
				{
					d_selected[grp.Property(name)] = true;
				}
			}
			
			if (o != null)
			{			
				foreach (Cpg.Property prop in o.Properties)
				{
					d_selected[prop] = true;
				}
			}
			
			Cpg.Link link = obj as Cpg.Link;
			
			if (link != null)
			{
				foreach (Cpg.LinkAction action in link.Actions)
				{
					d_selected[action] = true;
				}
			}
		}
		
		private IEnumerable<Wrappers.Object> AllObjects(Wrappers.Group grp)
		{
			foreach (Wrappers.Object child in grp.Children)
			{
				yield return child;

				Wrappers.Group cg = child as Wrappers.Group;
				
				if (cg != null)
				{
					foreach (Wrappers.Group g in AllObjects(cg))
					{
						yield return g;
					}
				}
			}
			
			yield return grp;
		}
		
		private void HandleEntryChanged(object sender, EventArgs e)
		{
			d_searchText = d_entry.Text.ToLowerInvariant();
			d_selected = null;
			
			if (d_searchText.LastIndexOfAny(new char[] {'/', '"', '.', ':', '('}) != -1)
			{
				Cpg.Selector selector = null;

				try
				{
					selector = Cpg.Selector.Parse(d_group, d_searchText);
				}
				catch
				{
				}
				
				if (selector != null)
				{
					selector.Partial = true;

					d_selected = new Dictionary<GLib.Object, bool>();
					
					foreach (Wrappers.Object o in AllObjects(d_group))
					{					
						Cpg.Selection[] selections = selector.Select(o, SelectorType.Any, null);
					
						foreach (Cpg.Selection sel in selections)
						{
							AddAll(GLib.Object.GetObject(sel.Object), d_selected);
						}
					}
				}
			}

			d_treeview.NodeStore.Filter(FilterFunc);
		}
		
		protected override void OnDestroyed()
		{
			d_group.ChildAdded -= ChildAdded;
			d_group.ChildRemoved -= ChildRemoved;

			base.OnDestroyed();
		}
		
		private void ChildAdded(Wrappers.Group grp, Wrappers.Wrapper wrapper)
		{
			TreeIter iter;
			d_treeview.NodeStore.Add(new WrapperNode(d_treeview, wrapper), out iter);
		}
		
		private void ChildRemoved(Wrappers.Group grp, Wrappers.Wrapper wrapper)
		{
			d_treeview.NodeStore.Remove(wrapper);
		}
		
		private void Build(Wrappers.Group grp)
		{
			foreach (Wrappers.Wrapper child in grp.Children)
			{
				ChildAdded(grp, child);
			}
			
			grp.ChildAdded += ChildAdded;
			grp.ChildRemoved += ChildRemoved;
		}
	}
}

