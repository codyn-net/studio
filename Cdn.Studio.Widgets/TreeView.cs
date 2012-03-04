using System;
using System.Reflection;

namespace Cpg.Studio.Widgets
{
	public class TreeView<T> : Gtk.TreeView where T : Node
	{
		public delegate void PopulatePopupHandler(object source, Gtk.Menu menu);

		public event PopulatePopupHandler PopulatePopup = delegate {};

		public TreeView() : base(new Gtk.TreeModelAdapter(new NodeStore<T>()))
		{
		}
		
		protected override void OnDestroyed()
		{
			NodeStore.Clear();
			base.OnDestroyed();
		}
		
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			if (evnt.Button != 3)
			{
				return base.OnButtonPressEvent(evnt);
			}

			Gtk.TreePath path;
			
			if (GetPathAtPos((int)evnt.X, (int)evnt.Y, out path))
			{
				if (!Selection.PathIsSelected(path))
				{
					base.OnButtonPressEvent(evnt);
				}
			}
			
			return DoPopupMenu(evnt);
		}
		
		private bool DoPopupMenu()
		{
			return DoPopupMenu(null);
		}
		
		private bool DoPopupMenu(Gdk.EventButton evnt)
		{
			Gtk.Menu menu = new Gtk.Menu();

			PopulatePopup(this, menu);
			
			if (menu.Children.Length == 0)
			{
				return false;
			}
			
			menu.AttachToWidget(this, null);
			
			if (evnt != null)
			{
				menu.Popup(null, null, null, evnt.Button, evnt.Time);
			}
			else
			{
				menu.Popup();
			}
			
			return true;
		}
		
		protected override bool OnPopupMenu()
		{
			return DoPopupMenu();
		}

		public NodeStore<T> NodeStore
		{
			get
			{
				return ((Gtk.TreeModelAdapter)Model).Implementor as NodeStore<T>;
			}
		}		
	}
}

