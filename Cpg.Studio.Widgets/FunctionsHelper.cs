using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class FunctionsHelper<NodeType, FunctionType> : VBox where NodeType : GenericFunctionNode, new() where FunctionType : Wrappers.Function
	{
		private Actions d_actions;
		private Wrappers.Group d_group;
		private TreeView d_treeview;
		private NodeStore<NodeType> d_store;
		private bool d_selectFunction;

		public FunctionsHelper(Actions actions, Wrappers.Group grp) : base(false, 3)
		{
			d_actions = actions;
			d_group = grp;

			d_store = new NodeStore<NodeType>();
			d_store.SortColumn = 0;

			d_treeview = new TreeView(new TreeModelAdapter(d_store));
			
			d_treeview.ShowExpanders = false;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			
			d_selectFunction = false;
			
			InitStore();
			
			d_treeview.KeyPressEvent += HandleTreeViewKeyPress;
		}
		
		protected TreeView TreeView
		{
			get
			{
				return d_treeview;
			}
		}
		
		protected NodeStore<NodeType> NodeStore
		{
			get
			{
				return d_store;
			}
		}
		
		protected Wrappers.Group Group
		{
			get
			{
				return d_group;
			}
		}
		
		protected Actions Actions
		{
			get
			{
				return d_actions;
			}
		}
		
		private void InitStore()
		{
			foreach (Wrappers.Function function in d_group.Functions)
			{
				AddFunction(function);
			}
			
			d_group.ChildAdded += HandleFunctionAdded;
			d_group.ChildRemoved += HandleFunctionRemoved;
		}
		
		protected override void OnDestroyed()
		{
			d_group.ChildAdded -= HandleFunctionAdded;
			d_group.ChildRemoved -= HandleFunctionRemoved;
			
			base.OnDestroyed();
		}

		private void HandleFunctionAdded(Wrappers.Group source, Wrappers.Wrapper child)
		{
			if (child is Wrappers.Function)
			{
				AddFunction((Wrappers.Function)child);
			}
		}
		
		private void HandleFunctionRemoved(Wrappers.Group source, Wrappers.Wrapper child)
		{
			if (child is Wrappers.Function)
			{
				RemoveFunction((Wrappers.Function)child);
			}
		}
		
		private void AddFunction(Wrappers.Function function)
		{
			if (function.GetType() != typeof(FunctionType))
			{
				return;
			}
			
			TreeIter iter;

			NodeType node = new NodeType();
			node.Function = function;

			d_store.Add(node, out iter);
			
			if (d_selectFunction)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
			}
		}
		
		private void RemoveFunction(Wrappers.Function function)
		{
			if (function.GetType() != typeof(FunctionType))
			{
				return;
			}

			d_store.Remove((FunctionType)function);
		}
		
		public void Select(Wrappers.Function f)
		{
			TreeIter iter;
			
			if (d_store.Find(f, out iter))
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
			}
		}
		
		public NodeType FromStorage(TreeIter iter)
		{
			return Node.FromIter<NodeType>(iter);
		}
		
		public NodeType FromStorage(TreePath path)
		{
			TreeIter it;
			d_store.GetIter(out it, path);

			return FromStorage(it);
		}
		
		public NodeType FromStorage(string path)
		{
			return FromStorage(new TreePath(path));
		}
		
		public void Add(Wrappers.Function function)
		{
			d_selectFunction = true;
			d_actions.AddObject(d_group, function);
			d_selectFunction = false;
		}
		
		protected void RemoveSelection()
		{
			TreeSelection selection = d_treeview.Selection;
			List<Wrappers.Wrapper> funcs = new List<Wrappers.Wrapper>();

			foreach (TreePath path in selection.GetSelectedRows())
			{
				funcs.Add(FromStorage(path).Function);
			}
			
			d_actions.Delete(d_group, funcs.ToArray());
		}
		
		private void HandleTreeViewKeyPress(object sender, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				RemoveSelection();
			}
		}
	}
}

