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
				Link
			}

			private Wrappers.Wrapper d_wrapper;
			private HeaderType d_header;
			private Gdk.Pixbuf d_icon;
			private Widget d_widget;
			
			private static Dictionary<Type, Gdk.Pixbuf> s_iconmap;
			
			static WrapperNode()
			{
				s_iconmap = new Dictionary<Type, Gdk.Pixbuf>();
			}
			
			public Gdk.Pixbuf WrapperIcon()
			{
				Gdk.Pixbuf icon;
				Type type = d_wrapper.GetType();
				string stockid;

				if (s_iconmap.TryGetValue(type, out icon))
				{
					return icon;
				}
			
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
			
				s_iconmap[type] = icon;
			
				return icon;
			}
			
			public WrapperNode(Widget widget, Wrappers.Wrapper wrapper)
			{
				d_widget = widget;
				d_wrapper = wrapper;
				
				d_icon = WrapperIcon();
				
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
				foreach (WrapperNode node in Children)
				{
					if (node.Wrapper == child)
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
			
			[PrimaryKey]
			public string FilterName
			{
				get
				{
					return d_wrapper != null ? d_wrapper.Id : null;
				}
			}
			
			[NodeColumn(0)]
			public string Name
			{
				get
				{
					return d_wrapper != null ? d_wrapper.Id : HeaderName;
				}
			}
			
			[NodeColumn(1)]
			public Gdk.Pixbuf Icon
			{
				get
				{
					return d_icon;
				}
			}
			
			[NodeColumn(2)]
			public bool Sensitive
			{
				get
				{
					return !(d_wrapper is Wrappers.Import);
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
					else
					{
						return d_header;
					}
				}
			}
			
			[SortColumn(0)]
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
		private Dictionary<Wrappers.Wrapper, bool> d_selected;
		
		public delegate void WrapperActivatedHandler(object source, Wrappers.Wrapper wrapper);
		public event WrapperActivatedHandler WrapperActivated = delegate {};
		
		public delegate void WrapperFilter(Wrappers.Wrapper wrapper, ref bool ret);
		
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
			
			BorderWidth = 6;
			
			TreeViewColumn col;
			CellRenderer renderer;
			
			col = new TreeViewColumn();
			
			renderer = new CellRendererPixbuf();
			col.PackStart(renderer, false);
			col.SetAttributes(renderer, "pixbuf", 1);

			renderer = new CellRendererText();
			col.PackStart(renderer, true);
			col.SetAttributes(renderer, "text", 0, "sensitive", 2);

			d_treeview.AppendColumn(col);
			
			d_treeview.HeadersVisible = false;
			d_treeview.NodeStore.SortColumn = 0;
			d_treeview.ShowExpanders = false;
			d_treeview.LevelIndentation = 12;
			d_treeview.EnableSearch = false;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			
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
			
			PackStart(hbox, false, false, 0);

			d_entry.Changed += HandleEntryChanged;
			d_entry.KeyPressEvent += HandleEntryKeyPressEvent;
			
			d_treeview.ExpandAll();
			d_treeview.NodeStore.Filter(FilterFunc);
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
			d_treeview.Selection.SelectedForeach(delegate (TreeModel model, TreePath path, TreeIter iter) {
				Console.WriteLine(d_treeview.NodeStore.GetFromIter(iter));
			});
		}

		void OnTreeViewRowActivated(object o, RowActivatedArgs args)
		{
			WrapperNode node = d_treeview.NodeStore.FindPath(args.Path);
			
			if (node != null)
			{
				WrapperActivated(this, node.Wrapper);
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
				if (d_selected == null || !d_selected.ContainsKey(wp.Wrapper))
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
				d_filterStorage(wp.Wrapper, ref ret);
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
		
		private void AddAll(Wrappers.Wrapper wrapper, Dictionary<Wrappers.Wrapper, bool> ret)
		{
			ret[wrapper] = true;
			
			Wrappers.Group grp = wrapper as Wrappers.Group;
						
			if (grp != null)
			{
				foreach (Wrappers.Wrapper child in grp.Children)
				{
					AddAll(child, ret);
				}
			}
		}
		
		private IEnumerable<Wrappers.Group> AllGroups(Wrappers.Group grp)
		{
			foreach (Wrappers.Object child in grp.Children)
			{
				Wrappers.Group cg = child as Wrappers.Group;
				
				if (cg != null)
				{
					foreach (Wrappers.Group g in AllGroups(cg))
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
					selector = Cpg.Selector.Parse(d_searchText);
				}
				catch
				{
				}
				
				if (selector != null)
				{
					selector.Partial = true;

					d_selected = new Dictionary<Wrappers.Wrapper, bool>();
					
					foreach (Wrappers.Group g in AllGroups(d_group))
					{					
						Cpg.Selection[] selections = selector.Select(g, SelectorType.Object, null);
					
						foreach (Cpg.Selection sel in selections)
						{
							AddAll(GLib.Object.GetObject(sel.Object) as Cpg.Object, d_selected);
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

