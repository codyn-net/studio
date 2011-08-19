using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Dialogs
{
	public class FindTemplate : Dialog
	{
		private Widgets.WrappersTree d_tree;
		public delegate bool FilterFunc(Wrappers.Wrapper wrapper);

		public FindTemplate(Wrappers.Group grp, FilterFunc func, Gtk.Window parent) : base("Find Template", parent, DialogFlags.DestroyWithParent | DialogFlags.NoSeparator)
		{
			d_tree = new Widgets.WrappersTree(grp);
			d_tree.RendererToggle.Visible = false;

			d_tree.Show();
			
			if (func != null)
			{
				d_tree.Filter += delegate (Widgets.WrappersTree.WrapperNode node, ref bool ret) {
					if (node.Wrapper == null)
					{
						ret = false;
					}
					else
					{
						ret &= func(node.Wrapper);
					}
				};
			}
			
			d_tree.Activated += delegate(object source, Widgets.WrappersTree.WrapperNode wrapper) {
				Respond(ResponseType.Apply);
			};
			
			d_tree.TreeView.Selection.SelectFunction = CantSelectImports;
			
			VBox.PackStart(d_tree, true, true, 0);
			
			TransientFor = parent;

			AddButton(Gtk.Stock.Close, ResponseType.Close);
			AddButton(Gtk.Stock.Apply, ResponseType.Apply);
			
			d_tree.Entry.GrabFocus();

			SetDefaultSize(400, 300);
		}
		
		private bool CantSelectImports(TreeSelection selection, TreeModel model, TreePath path, bool currentsel)
		{
			return !(d_tree.TreeView.NodeStore.FindPath(path).Wrapper is Wrappers.Import);
		}
		
		public IEnumerable<Wrappers.Wrapper> Selection
		{
			get
			{
				List<Wrappers.Wrapper> ret = new List<Wrappers.Wrapper>();

				d_tree.TreeView.Selection.SelectedForeach(delegate (TreeModel model, TreePath path, TreeIter iter)
				{
					Widgets.WrappersTree.WrapperNode node = d_tree.TreeView.NodeStore.GetFromIter(iter);
					
					ret.Add(node.Wrapper);
				});
				
				return ret;
			}
		}
	}
}
