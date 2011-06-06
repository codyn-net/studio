using System;
using Gtk;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Cpg.Studio.Widgets
{
	[AttributeUsage(AttributeTargets.Property)]
	public class NodeColumnAttribute : System.Attribute
	{
		private int d_index;
		
		public NodeColumnAttribute(int index)
		{
			d_index = index;
		}
		
		public int Index
		{
			get
			{
				return d_index;
			}
		}
	}
	
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute : System.Attribute
	{
	}
	
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=true)]
	public class CustomRendererAttribute : System.Attribute
	{
		private int d_column;
		private int d_renderer;

		public CustomRendererAttribute(int column) : this(column, 0)
		{
		}

		public CustomRendererAttribute(int column, int renderer)
		{
			d_column = column;
			d_renderer = renderer;
		}
		
		public int Column
		{
			get
			{
				return d_column;
			}
		}
		
		public int Renderer
		{
			get
			{
				return d_renderer;
			}
		}
	}
	
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=true)]
	public class SortColumnAttribute : System.Attribute
	{
		private int d_column;
		
		public SortColumnAttribute(int column)
		{
			d_column = column;
		}
		
		public int Column
		{
			get
			{
				return d_column;
			}
		}
	}
	
	public class Node : GLib.Object, IDisposable, IEnumerable<Node>
	{
		public delegate void NodeAddedHandler(Node parent, Node child);
		public delegate void NodeRemovedHandler(Node parent, Node child, int wasAtIndex);
		public delegate void NodeHandler(Node node);
		public delegate bool FilterFunc(Node node);

		private Node d_parent;
		private List<Node> d_children;
		private List<GCHandle> d_gchandles;
		private bool d_visible;
		private int d_childCount;

		public event NodeAddedHandler NodeAddedIntern = delegate {};
		public event NodeAddedHandler NodeAdded = delegate {};
		public event NodeRemovedHandler NodeRemoved = delegate {};
		public event NodeHandler Changed = delegate {};
		public event NodeHandler VisibilityChanged = delegate {};
		
		public Node(Node parent)
		{
			d_parent = parent;
			d_children = new List<Node>();
			d_gchandles = new List<GCHandle>();
			d_visible = true;
			d_childCount = 0;
			
			NodeAdded += delegate {
				++d_childCount;
			};
			
			NodeRemoved += delegate {
				--d_childCount;
			};
		}
				
		public Node() : this(null)
		{
		}
		
		public void Sort(Comparison<Node> sorter)
		{
			d_children.Sort(sorter);
		}
		
		public bool Visible
		{
			get
			{
				return d_visible;
			}
			set
			{
				if (d_visible != value)
				{
					d_visible = value;
					VisibilityChanged(this);
				}
			}
		}

		public override string ToString()
		{
			List<string> ret = new List<string>();

			foreach (PropertyInfo info in GetType().GetProperties())
			{
				object[] attrs = info.GetCustomAttributes(typeof(NodeColumnAttribute), false);
				
				if (attrs.Length > 0)
				{
					NodeColumnAttribute attr = (NodeColumnAttribute)attrs[0];
					ret.Insert(attr.Index, String.Format("{0} = {1}", info.Name, info.GetGetMethod().Invoke(this, new object[] {})));
				}
			}
			
			return String.Format("[Node: {0}]", String.Join(", ", ret.ToArray()));
		}
		
		public IEnumerator<Node> GetEnumerator()
		{
			return Children.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
		
		public Node Parent
		{
			get
			{
				return d_parent;
			}
			protected set
			{
				d_parent = value;
			}
		}
		
		public int IndexOf(Node child)
		{
			int tmp;
			int idx;

			return IndexOf(child, out idx, out tmp) ? idx : -1;
		}
		
		private bool IndexOf(Node child, out int idx, out int internalidx)
		{
			int cnt = 0;
			
			idx = -1;
			internalidx = -1;

			for (int i = 0; i < d_children.Count; ++i)
			{
				if (d_children[i] == child)
				{
					internalidx = i;
					idx = cnt;

					return child.Visible;
				}
				
				if (d_children[i].Visible)
				{
					++cnt;
				}
			}
			
			return false;
		}
		
		public TreeIter Iter
		{
			get
			{
				TreeIter iter = TreeIter.Zero;
				
				GCHandle handle = GCHandle.Alloc(this);
				iter.UserData = (IntPtr)handle;
				
				d_gchandles.Add(handle);
				
				return iter;
			}
		}
		
		public TreePath Path
		{
			get
			{
				TreePath path = new TreePath();
				
				Node child = this;
				Node parent = Parent;
				
				while (parent != null)
				{
					int idx;
					int intidx;

					parent.IndexOf(child, out idx, out intidx);
					path.PrependIndex(idx);
					
					child = parent;
					parent = parent.Parent;
				}
				
				return path;
			}
		}
		
		public override void Dispose()
		{
			foreach (GCHandle handle in d_gchandles)
			{
				handle.Free();
			}
			
			d_gchandles.Clear();

			base.Dispose();
		}
		
		public int Count
		{
			get
			{
				return d_childCount;
			}
		}
		
		public bool Empty
		{
			get
			{
				return d_childCount == 0;
			}
		}
		
		public Node this[int index]
		{
			get
			{
				foreach (Node child in Children)
				{
					if (index == 0)
					{
						return child;
					}
					
					--index;
				}
				
				return null;
			}
		}
		
		public bool Contains(Node child)
		{
			foreach (Node node in Children)
			{
				if (node == child)
				{
					return true;
				}
			}
			
			return false;
		}
		
		public bool Add(Node child)
		{
			TreeIter ret;
			
			return Add(child, out ret);
		}
		
		public bool Add(Node child, out TreeIter iter)
		{
			d_children.Add(child);
			child.Parent = this;
			
			NodeAddedIntern(this, child);
			
			child.VisibilityChanged += HandleChildVisibilityChanged;
			
			if (child.Visible)
			{
				NodeAdded(this, child);
				iter = child.Iter;
				
				return true;
			}
			else
			{
				iter = default(TreeIter);
				return false;
			}
		}

		private void HandleChildVisibilityChanged(Node node)
		{
			if (node.Visible)
			{			
				NodeAdded(this, node);
			}
			else
			{				
				int idx;
				int intidx;
				
				IndexOf(node, out idx, out intidx);
				NodeRemoved(this, node, idx);
			}
		}
		
		public bool Remove(Node child)
		{
			if (child == null)
			{
				return false;
			}

			int idx;
			int intidx;
			bool ret = IndexOf(child, out idx, out intidx);
			
			if (intidx >= 0)
			{
				d_children.RemoveAt(intidx);
				
				if (child.Parent == this)
				{
					child.Parent = null;
				}

				child.VisibilityChanged -= HandleChildVisibilityChanged;
				
				if (ret)
				{
					NodeRemoved(this, child, idx);
				}
				
				child.Dispose();
				
				return true;
			}
			
			return false;
		}
		
		protected Node FindPath(TreePath path, int index)
		{
			int idx = path.Indices[index];

			if (idx < 0 || idx >= Count)
			{
				return null;
			}

			Node child = this[idx];

			if (path.Indices.Length - 1 == index)
			{
				return child;
			}
			else
			{
				return child.FindPath(path, index + 1);
			}
		}
		
		public Node FindPath(string path)
		{
			return FindPath(new TreePath(path));
		}
		
		public Node FindPath(TreePath path)
		{
			return FindPath(path, 0);
		}
		
		public bool FindPath(string path, out TreeIter iter)
		{
			return FindPath(new TreePath(path), out iter);
		}
		
		public bool FindPath(TreePath path, out TreeIter iter)
		{
			Node node = FindPath(path);
				
			if (node != null)
			{
				iter = node.Iter;
				return true;
			}
			else
			{
				iter = TreeIter.Zero;
				return false;
			}
		}
		
		public Node Next(Node child)
		{
			int idx = IndexOf(child);
			
			if (idx >= 0 && idx < Count - 1)
			{
				return this[idx + 1];
			}
			
			return null;
		}

		public Node Previous(Node child)
		{
			int idx = IndexOf(child);
			
			if (idx > 0 && idx <= Count)
			{
				return this[idx - 1];
			}
			
			return null;
		}
		
		public T GetFromIter<T>(TreeIter iter) where T : Node
		{
			if (iter.UserData == IntPtr.Zero)
			{
				return (T)this;
			}
			else
			{
				return Node.FromIter<T>(iter);
			}
		}
		
		public static T FromIter<T>(TreeIter iter) where T : Node
		{
			if (iter.UserData == IntPtr.Zero)
			{
				return null;
			}

			GCHandle handle = (GCHandle)iter.UserData;
			return (T)handle.Target;
		}
		
		public void EmitChanged()
		{
			if (Visible)
			{
				Changed(this);
			}
		}
		
		public void Clear()
		{
			foreach (Node child in d_children.ToArray())
			{
				Remove(child);
			}
		}
		
		public IEnumerable<Node> Children
		{
			get
			{
				foreach (Node child in d_children)
				{
					if (child.Visible)
					{
						yield return child;
					}
				}
			}
			set
			{
				d_children = new List<Node>(value);
			}
		}
		
		public IEnumerable<Node> Descendents
		{
			get
			{
				foreach (Node node in Children)
				{
					yield return node;
					
					foreach (Node d in node.Descendents)
					{
						yield return d;
					}
				}
			}
		}
		
		public void Move(Node a, int position)
		{
			int intidx;
			int orig;

			bool ret = IndexOf(a, out orig, out intidx);
			
			if (!ret || orig == position)
			{
				return;
			}
			
			if (orig >= 0)
			{
				d_children.RemoveAt(intidx);
				
				if (orig < position)
				{
					--position;
				}
				
				// Find the real position
				for (int i = 0; i < d_children.Count; ++i)
				{
					if (position == 0)
					{
						d_children.Insert(i, a);
						break;
					}

					if (d_children[i].Visible)
					{
						--position;
					}
				}

				if (position != 0)
				{
					d_children.Add(a);
				}
			}
		}
		
		public void Filter(FilterFunc func)
		{
			bool checkme = true;
			List<Node> children = new List<Node>(d_children);
			
			foreach (Node child in children)
			{
				child.Filter(func);
				
				if (child.Visible)
				{
					checkme = false;
				}
			}
			
			if (checkme && d_parent != null && func != null)
			{
				Visible = func(this);
			}
			else
			{
				Visible = true;
			}
		}
	}

	public class NodeStore<T> : Node, TreeModelImplementor where T : Node
	{
		private class CustomRenderer
		{
			public int Column;
			public int Renderer;
			public MethodInfo Method;
			
			public CustomRenderer(int column, int renderer, MethodInfo method)
			{
				Column = column;
				Renderer = renderer;
				Method = method;
			}
		}

		private GLib.GType[] d_gtypes;

		private List<MethodInfo> d_valueGetters;
		private List<CustomRenderer> d_customRenderers;

		private Dictionary<Type, MethodInfo> d_primaryKeys;
		private Dictionary<int, MethodInfo> d_sortColumns;
		private Dictionary<int, bool> d_sortable;

		private TreeModelAdapter d_adapter;
		private int d_sortColumn;
		private Node.FilterFunc d_lastFilter;
		
		public delegate void NodeChangedHandler(NodeStore<T> store, Node child);

		public event NodeChangedHandler NodeChanged = delegate {};

		public NodeStore() : base()
		{
			d_valueGetters = new List<MethodInfo>();
			d_customRenderers = new List<CustomRenderer>();

			d_primaryKeys = new Dictionary<Type, MethodInfo>();
			d_sortColumns = new Dictionary<int, MethodInfo>();
			d_sortColumn = -1;
			d_sortable = new Dictionary<int, bool>();

			d_adapter = new TreeModelAdapter(this);
			
			Scan();
			
			Connect(this);
		}
		
		public new void Filter(FilterFunc func)
		{
			base.Filter(func);
			d_lastFilter = func;
		}

		public void Filter(string text)
		{
			string norm = text.ToLowerInvariant();
			bool isempty = norm == String.Empty;
			
			Filter(delegate (Node a) {
				if (isempty)
				{
					return true;
				}
				
				string s = (string)GetPrimary(a, typeof(string));
				
				if (s == null)
				{
					return true;
				}
				
				return s.ToLowerInvariant().Contains(norm);
			});
		}
		
		public void Bind(TreeView view)
		{
			foreach (CustomRenderer renderer in d_customRenderers)
			{
				CustomRenderer rend = renderer;
				TreeViewColumn col = view.GetColumn(rend.Column);
				
				if (col != null)
				{
					if (col.CellRenderers.Length > rend.Renderer)
					{
						col.SetCellDataFunc(col.CellRenderers[rend.Renderer], delegate (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) {
							Node node = GetFromIter<Node>(iter);
							rend.Method.Invoke(node, new object[] {cell});
						});
					}
					else
					{
						Console.Error.WriteLine("Missing renderer {0}", rend.Renderer);
					}
				}
				else
				{
					Console.Error.WriteLine("Missing column {0}", rend.Column);
				}
			}
		}
		
		private void Connect(Node node)
		{
			node.NodeAdded += HandleNodeAdded;
			node.NodeAddedIntern += HandleNodeAddedIntern;
			node.NodeRemoved += HandleNodeRemoved;
			node.Changed += HandleNodeChanged;
			
			foreach (Node child in node)
			{
				Connect(child);
			}
		}
		
		private void HandleNodeChanged(Node node)
		{
			if (d_lastFilter != null && !d_lastFilter(node))
			{
				node.Visible = false;
				return;
			}

			if (IsSorted)
			{
				Sort(node.Parent);
			}

			NodeChanged(this, node);
			d_adapter.EmitRowChanged(node.Path, node.Iter);
		}
		
		private void Disconnect(Node node)
		{
			node.NodeAdded -= HandleNodeAdded;
			node.NodeAddedIntern -= HandleNodeAddedIntern;
			node.NodeRemoved -= HandleNodeRemoved;
			node.Changed -= HandleNodeChanged;

			foreach (Node child in node.Descendents)
			{
				Disconnect(child);
			}
		}
		
		private void RemoveChildren(Node node, TreePath path)
		{
			if (node.Empty)
			{
				return;
			}
			
			TreePath children = path.Copy();
			children.Down();
			
			foreach (Node child in node)
			{
				RemoveChildren(child, children);
				children.Next();
			}
			
			children.Up();
			children.Down();
			
			// Then empty the node
			for (int i = 0; i < node.Count; ++i)
			{
				//Console.WriteLine("Row deleted recurse: {0}, {1}", children, node[i]);
				d_adapter.EmitRowDeleted(children.Copy());
			}
		}

		private void HandleNodeRemoved(Node parent, Node child, int wasAtIndex)
		{
			TreePath path = parent.Path;
			path.AppendIndex(wasAtIndex);
			
			Disconnect(child);

			RemoveChildren(child, path);
			
			//Console.WriteLine("Row deleted: {0}, {1}", path, child);
			d_adapter.EmitRowDeleted(path);
		}
		
		private bool IsSorted
		{
			get
			{
				return d_sortable.ContainsKey(d_sortColumn);
			}
		}
		
		private void AddNodeToModel(Node node, TreePath path)
		{
			// First reorder the child if necessary
			if (path == null && IsSorted)
			{
				Comparison<Node> sorter = Sorter;

				for (int i = 0; i < node.Parent.Count; ++i)
				{
					Node child = node.Parent[i];

					if (node != child && sorter(node, child) < 0)
					{
						node.Parent.Move(node, i);
						break;
					}
				}
			}

			if (path == null)
			{
				path = node.Path;
			}
			
			//Console.WriteLine("Row inserted: {0}, {1}", path, node);
			d_adapter.EmitRowInserted(path.Copy(), node.Iter);
			
			TreePath children = path.Copy();
			children.Down();
			
			if (IsSorted)
			{
				node.Sort(Sorter);
			}
			
			// Then also its children
			foreach (Node child in node)
			{
				AddNodeToModel(child, children);
				children.Next();
			}
		}
		
		private void HandleNodeAddedIntern(Node parent, Node child)
		{
			if (d_lastFilter != null)
			{
				child.Filter(d_lastFilter);
			}
		}

		private void HandleNodeAdded(Node parent, Node child)
		{
			Connect(child);
			AddNodeToModel(child, null);
		}
		
		private void Scan()
		{
			List<GLib.GType> gtypes = new List<GLib.GType>();

			foreach (PropertyInfo info in typeof(T).GetProperties(BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				object[] attrs = info.GetCustomAttributes(typeof(NodeColumnAttribute), false);
				
				if (attrs.Length > 0)
				{
					NodeColumnAttribute attr = (NodeColumnAttribute)attrs[0];
					MethodInfo getter = info.GetGetMethod();

					d_valueGetters.Insert(attr.Index, getter);
					
					d_sortable[attr.Index] = Array.IndexOf(getter.ReturnType.GetInterfaces(), typeof(IComparable)) != -1;
					
					gtypes.Insert(attr.Index, (GLib.GType)info.PropertyType);
				}

				attrs = info.GetCustomAttributes(typeof(PrimaryKeyAttribute), false);
				
				if (attrs.Length > 0)
				{
					MethodInfo getter = info.GetGetMethod();
					d_primaryKeys[info.PropertyType] = getter;
				}
			}
			
			foreach (MethodInfo info in typeof(T).GetMethods(BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				object[] attrs = info.GetCustomAttributes(typeof(CustomRendererAttribute), false);
				
				foreach (object attr in attrs)
				{
					CustomRendererAttribute at = (CustomRendererAttribute)attr;
					d_customRenderers.Add(new CustomRenderer(at.Column, at.Renderer, info));
				}
				
				attrs = info.GetCustomAttributes(typeof(SortColumnAttribute), false);
				
				foreach (object attr in attrs)
				{
					SortColumnAttribute at = (SortColumnAttribute)attr;
					
					d_sortColumns[at.Column] = info;
					d_sortable[at.Column] = true;
				}
			}
			
			d_gtypes = gtypes.ToArray();
		}
		
		public void GetValue(TreeIter iter, int column, ref GLib.Value val)
		{
			val = new GLib.Value(d_valueGetters[column].Invoke(FromIter<Node>(iter), new object[] {}));
		}
		
		public TreeModelFlags Flags
		{
			get
			{
				return TreeModelFlags.ItersPersist;
			}
		}
		
		public int NColumns
		{
			get
			{
				return d_gtypes.Length;
			}
		}
		
		public GLib.GType GetColumnType(int column)
		{
			return d_gtypes[column];
		}
		
		public bool GetIter(out TreeIter iter, TreePath path)
		{
			return FindPath(path, out iter);
		}
		
		public TreePath GetPath(TreeIter iter)
		{
			Node node = GetFromIter<Node>(iter);
			TreePath path = node.Path;
			
			path.Owned = false;
			return path;
		}
		
		public bool IterNext(ref TreeIter iter)
		{
			Node node = GetFromIter<Node>(iter);
			
			if (node == null || node.Parent == null)
			{
				return false;
			}
			
			Node next = node.Parent.Next(node);
			
			if (next != null)
			{
				iter = next.Iter;
				return true;
			}
			else
			{
				return false;
			}
		}
		
		public bool IterChildren(out TreeIter iter, TreeIter parent)
		{
			Node parNode = GetFromIter<Node>(parent);
			
			if (parNode != null && !parNode.Empty)
			{
				iter = parNode[0].Iter;
				return true;
			}
			else
			{
				iter = TreeIter.Zero;
				return false;
			}
		}
		
		public bool IterHasChild(TreeIter iter)
		{
			Node node = GetFromIter<Node>(iter);
			
			return node != null && !node.Empty;
		}
		
		public int IterNChildren(TreeIter iter)
		{
			Node node = GetFromIter<Node>(iter);
			
			return node != null ? node.Count : 0;
		}
		
		public bool IterNthChild(out TreeIter iter, TreeIter parent, int index)
		{
			Node node = GetFromIter<Node>(parent);
			
			if (node != null)
			{
				Node child = node[index];
				
				if (child != null)
				{
					iter = child.Iter;
					return true;
				}
				else
				{
					iter = TreeIter.Zero;
					return false;
				}
			}
			else
			{
				iter = TreeIter.Zero;
				return false;
			}
		}
		
		public bool IterParent(out TreeIter iter, TreeIter child)
		{
			Node node = GetFromIter<Node>(child);
			
			if (node.Parent != null && node.Parent != this)
			{
				iter = node.Parent.Iter;
				return true;
			}
			else
			{
				iter = TreeIter.Zero;
				return false;
			}
		}
		
		public void RefNode(TreeIter iter)
		{
		}
		
		public void UnrefNode(TreeIter iter)
		{
		}
		
		private bool ComparePrimary(Node node, MethodInfo info, object obj)
		{
			object compare = info.Invoke(node, new object[] {});
			return obj.Equals(compare);
		}
		
		private object GetPrimary(Node node, Type type)
		{
			type = PrimaryKeyType(type);
			
			if (type == null)
			{
				return null;
			}
			
			MethodInfo info = d_primaryKeys[type];
			return info.Invoke(node, new object[] {});
		}
		
		private Node FindPrimary(Node parent, object obj)
		{
			Type objType = PrimaryKeyType(obj);
			
			if (objType == null)
			{
				return null;
			}
			
			MethodInfo info = d_primaryKeys[objType];
			
			foreach (Node child in parent)
			{
				if (ComparePrimary(child, info, obj))
				{
					return child;
				}
			}
			
			return null;
		}
		
		public bool FindTree(out TreeIter iter, params object[] tree)
		{
			Node node = FindTree(tree);
			iter = TreeIter.Zero;
			
			if (node != null)
			{
				iter = node.Iter;
				return true;
			}
			else
			{
				return false;
			}
		}
		
		public Node FindTree(params object[] tree)
		{
			int idx = 0;

			Node node = this;
			Node found = null;
			
			while (idx < tree.Length)
			{
				found = FindPrimary(node, tree[idx]);
				
				if (found == null)
				{
					break;
				}
				
				node = found;
				++idx;
			}
			
			return found;
		}
		
		private T[] FindAll(Node parent, MethodInfo primaryInfo, object obj, bool onlyOne)
		{
			List<T> ret = new List<T>();

			foreach (Node child in parent)
			{
				if (ComparePrimary(child, primaryInfo, obj))
				{
					ret.Add((T)child);
					
					if (onlyOne)
					{
						return ret.ToArray();
					}
				}

				ret.AddRange(FindAll(child, primaryInfo, obj, onlyOne));
				
				if (ret.Count > 0 && onlyOne)
				{
					break;
				}
			}
			
			return ret.ToArray();
		}
		
		public T[] FindAll(object obj)
		{
			if (!d_primaryKeys.ContainsKey(obj.GetType()))
			{
				return new T[] {};
			}
			
			MethodInfo info = d_primaryKeys[obj.GetType()];
			return FindAll(this, info, obj, false);
		}
		
		public Type PrimaryKeyType(object obj)
		{
			return PrimaryKeyType(obj.GetType());
		}
		
		public Type PrimaryKeyType(Type type)
		{
			while (type != null && !d_primaryKeys.ContainsKey(type))
			{
				type = type.BaseType;
			}

			return type;
		}
		
		public T Find(object obj)
		{
			Type prim = PrimaryKeyType(obj);

			if (prim == null)
			{
				return null;
			}
			
			MethodInfo info = d_primaryKeys[prim];
			Node[] ret = FindAll(this, info, obj, true);
			
			if (ret.Length > 0)
			{
				return (T)ret[0];
			}
			else
			{
				return null;
			}
		}
		
		public bool Find(object obj, out TreeIter iter)
		{
			Node node = Find(obj);
			
			if (node == null)
			{
				iter = TreeIter.Zero;
				return false;
			}
			else
			{
				iter = node.Iter;
				return true;
			}
		}
		
		public bool Remove(object obj)
		{
			if (obj is Node)
			{
				return base.Remove((Node)obj);
			}

			Node node = Find(obj);
			
			if (node != null && node.Parent != null)
			{
				node.Parent.Remove(node);
				return true;
			}
			else
			{
				return false;
			}
		}
		
		private void Sort()
		{
			if (!IsSorted)
			{
				return;
			}

			Sort(this);
		}
		
		private Comparison<Node> Sorter
		{
			get
			{
				if (!IsSorted)
				{
					return null;
				}

				if (d_sortColumns.ContainsKey(d_sortColumn))
				{
					MethodInfo info = d_sortColumns[d_sortColumn];
					return delegate (Node first, Node second) { return (int)info.Invoke(first, new object[] {second}); };
				}
				else
				{
					MethodInfo info = d_valueGetters[d_sortColumn];
				
					return delegate (Node first, Node second) {
						IComparable o1 = (IComparable)info.Invoke(first, new object[] {});
						IComparable o2 = (IComparable)info.Invoke(second, new object[] {});
					
						return o1.CompareTo(o2);
					};
				}
			}
		}
		
		private void Sort(Node node)
		{
			if (!IsSorted)
			{
				return;
			}
			
			Sort(node, Sorter);
		}
		
		private void Sort(Node node, Comparison<Node> sorter)
		{
			// Sort all the children, deep first
			List<Node> orig = new List<Node>(node.Children);
			
			foreach (Node child in orig)
			{
				Sort(child, sorter);
			}
			
			List<Node> sorted = new List<Node>(orig);
			sorted.Sort(sorter);
			
			int[] order = new int[sorted.Count];
			bool reordered = false;
			
			for (int i = 0; i < sorted.Count; ++i)
			{
				order[i] = sorted.IndexOf(orig[i]);
				
				if (order[i] != i)
				{
					reordered = true;
				}
			}
			
			if (reordered)
			{
				node.Sort(sorter);
				d_adapter.EmitRowsReordered(node.Path, node.Iter, order);
			}
		}
		
		public int SortColumn
		{
			get
			{
				return d_sortColumn;
			}
			set
			{
				if (d_sortColumn != value)
				{
					d_sortColumn = value;
					Sort();
				}
			}
		}
		
		public new T FindPath(string path)
		{
			return (T)base.FindPath(path);
		}
		
		public new T FindPath(TreePath path)
		{
			return (T)base.FindPath(path);
		}

		public T GetFromIter(TreeIter iter)
		{
			return (T)base.GetFromIter<T>(iter);
		}
	}
}

