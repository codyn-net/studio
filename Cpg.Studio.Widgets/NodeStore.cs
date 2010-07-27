using System;
using Gtk;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Cpg.Studio.Widgets
{
	[AttributeUsage(AttributeTargets.Property)]
	public class NodeColumnAttribute : Attribute
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
	public class PrimaryKeyAttribute : Attribute
	{
	}
	
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=true)]
	public class CustomRendererAttribute : Attribute
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
	
	public class Node : GLib.Object, IDisposable, IEnumerable<Node>
	{
		public delegate void NodeAddedHandler(Node parent, Node child);
		public delegate void NodeRemovedHandler(Node parent, Node child, int wasAtIndex);
		public delegate void NodeChangedHandler(Node node);

		private Node d_parent;
		private List<Node> d_children;
		private List<GCHandle> d_gchandles;

		public event NodeAddedHandler NodeAdded = delegate {};
		public event NodeRemovedHandler NodeRemoved = delegate {};
		public event NodeChangedHandler Changed = delegate {};
		
		public Node(Node parent)
		{
			d_parent = parent;
			d_children = new List<Node>();
			d_gchandles = new List<GCHandle>();
		}
		
		public Node() : this(null)
		{
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
			return d_children.GetEnumerator();
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
			return d_children.IndexOf(child);
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
					path.PrependIndex(parent.IndexOf(child));
					
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
				return d_children.Count;
			}
		}
		
		public bool Empty
		{
			get
			{
				return d_children.Count == 0;
			}
		}
		
		public Node this[int index]
		{
			get
			{
				return d_children[index];
			}
		}
		
		public bool Contains(Node child)
		{
			return d_children.Contains(child);
		}
		
		public TreeIter Add(Node child)
		{
			d_children.Add(child);
			child.Parent = this;

			NodeAdded(this, child);
			
			return child.Iter;
		}
		
		public bool Remove(Node child)
		{
			if (child == null)
			{
				return false;
			}

			int idx = d_children.IndexOf(child);
			
			if (idx >= 0)
			{
				d_children.RemoveAt(idx);
				
				if (child.Parent == this)
				{
					child.Parent = null;
				}

				NodeRemoved(this, child, idx);
				
				return true;
			}
			
			return false;
		}
		
		protected Node FindPath(TreePath path, int index)
		{
			int idx = path.Indices[index];

			if (idx < 0 || idx >= d_children.Count)
			{
				return null;
			}

			Node child = d_children[idx];

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
			
			if (idx >= 0 && idx < d_children.Count - 1)
			{
				return d_children[idx + 1];
			}
			
			return null;
		}

		public Node Previous(Node child)
		{
			int idx = IndexOf(child);
			
			if (idx > 0 && idx <= d_children.Count)
			{
				return d_children[idx - 1];
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
			Changed(this);
		}
		
		public void Clear()
		{
			foreach (Node child in d_children.ToArray())
			{
				Remove(child);
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
		private TreeModelAdapter d_adapter;

		public NodeStore() : base()
		{
			d_valueGetters = new List<MethodInfo>();
			d_customRenderers = new List<CustomRenderer>();

			d_primaryKeys = new Dictionary<Type, MethodInfo>();
			d_adapter = new TreeModelAdapter(this);
			
			Scan();
			
			Connect(this);
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
			node.NodeRemoved += HandleNodeRemoved;
			node.Changed += HandleNodeChanged;
			
			foreach (Node child in node)
			{
				Connect(child);
			}
		}

		private void HandleNodeChanged(Node node)
		{
			d_adapter.EmitRowChanged(node.Path, node.Iter);
		}
		
		private void Disconnect(Node node)
		{
			node.NodeAdded -= HandleNodeAdded;
			node.NodeRemoved -= HandleNodeRemoved;
			node.Changed -= HandleNodeChanged;

			foreach (Node child in node)
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
				d_adapter.EmitRowDeleted(children.Copy());
			}
		}

		private void HandleNodeRemoved(Node parent, Node child, int wasAtIndex)
		{
			TreePath path = parent.Path;
			path.AppendIndex(wasAtIndex);
			
			Disconnect(child);

			RemoveChildren(child, path);
			d_adapter.EmitRowDeleted(path);
		}
		
		private void AddNodeToModel(Node node, TreePath path)
		{
			d_adapter.EmitRowInserted(path.Copy(), node.Iter);
			
			TreePath children = path.Copy();
			children.Down();
			
			// Then also its children
			foreach (Node child in node)
			{
				AddNodeToModel(child, children);
				children.Next();
			}
		}

		private void HandleNodeAdded(Node parent, Node child)
		{
			Connect(child);

			AddNodeToModel(child, child.Path);
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

					d_valueGetters.Insert(attr.Index, info.GetGetMethod());
					
					gtypes.Insert(attr.Index, (GLib.GType)info.PropertyType);
				}

				attrs = info.GetCustomAttributes(typeof(PrimaryKeyAttribute), false);
				
				if (attrs.Length > 0)
				{
					d_primaryKeys[info.PropertyType] = info.GetGetMethod();
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
			
			node = node.Parent.Next(node);
			
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
				iter = node[index].Iter;
				return true;
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
			
			if (node.Parent != null)
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
		
		private Node FindPrimary(Node parent, object obj)
		{
			Type objType = obj.GetType();
			
			if (!d_primaryKeys.ContainsKey(objType))
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
		
		private Node[] FindAll(Node parent, MethodInfo primaryInfo, object obj, bool onlyOne)
		{
			List<Node> ret = new List<Node>();

			foreach (Node child in parent)
			{
				if (ComparePrimary(child, primaryInfo, obj))
				{
					ret.Add(child);
					
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
		
		public Node[] FindAll(object obj)
		{
			if (!d_primaryKeys.ContainsKey(obj.GetType()))
			{
				return new Node[] {};
			}
			
			MethodInfo info = d_primaryKeys[obj.GetType()];
			return FindAll(this, info, obj, false);
		}
		
		public Node Find(object obj)
		{
			if (!d_primaryKeys.ContainsKey(obj.GetType()))
			{
				return null;
			}
			
			MethodInfo info = d_primaryKeys[obj.GetType()];
			Node[] ret = FindAll(this, info, obj, true);
			
			if (ret.Length > 0)
			{
				return ret[0];
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
	}
}

