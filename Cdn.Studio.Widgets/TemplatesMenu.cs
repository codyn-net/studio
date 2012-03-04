using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio
{
	public class TemplatesMenu
	{
		class MenuInfo
		{
			public Gtk.MenuItem Item;
			public Gtk.Menu Menu;
			
			public MenuInfo() : this(null, null)
			{
			}
			
			public MenuInfo(Gtk.MenuItem item, Gtk.Menu menu)
			{
				Item = item;
				Menu = menu;
			}
		}
		
		public delegate void ActivatedHandler(object source, Wrappers.Wrapper template);
		public delegate bool WrapperFilter(Wrappers.Wrapper wrapped);
		
		private WrapperFilter d_filter;
		private bool d_recursive;
		private Dictionary<Wrappers.Wrapper, MenuInfo> d_map;
		private Gtk.Menu d_menu;
		private Gtk.Widget d_widget;
		private PropertyInfo d_menuProperty;
		private Wrappers.Group d_group;
		
		public event ActivatedHandler Activated = delegate {};

		public TemplatesMenu(Gtk.Widget widget, Wrappers.Group grp, bool recursive) : this(widget, new Gtk.Menu(), grp, recursive, null)
		{
		}
		
		public TemplatesMenu(Gtk.Widget widget, Wrappers.Group grp, bool recursive, WrapperFilter filter) : this(widget, new Gtk.Menu(), grp, recursive, filter)
		{
		}
		
		public TemplatesMenu(Gtk.Widget widget, Gtk.Menu menu, Wrappers.Group grp, bool recursive) : this(widget, menu, grp, recursive, null)
		{
		}
		
		public TemplatesMenu(Gtk.Widget widget, Gtk.Menu menu, Wrappers.Group grp, bool recursive, WrapperFilter filter)
		{
			d_menu = menu;
			d_filter = filter;
			d_recursive = recursive;
			d_map = new Dictionary<Wrappers.Wrapper, MenuInfo>();
			d_group = grp;
			
			d_map[grp] = new MenuInfo(null, d_menu);
			
			d_widget = widget;
			
			if (d_widget != null)
			{
				d_menuProperty = d_widget.GetType().GetProperty("Menu");
			}
			
			Traverse(grp, d_menu);
			
			if (d_menuProperty != null)
			{
				grp.ChildRemoved += delegate {
					if (d_menu.Children.Length == 0)
					{
						d_menuProperty.SetValue(d_widget, null, null);
						grp.ChildAdded += HideShowMenu;
					}
				};
				
				if (d_menu.Children.Length > 0)
				{
					d_menuProperty.SetValue(d_widget, d_menu, null);
				}
				else
				{
					grp.ChildAdded += HideShowMenu;
				}
			}
		}

		private void HideShowMenu(Wrappers.Group source, Wrappers.Wrapper child)
		{
			if (d_menu.Children.Length > 0)
			{
				d_menuProperty.SetValue(d_widget, d_menu, null);
				d_group.ChildAdded -= HideShowMenu;
			}
		}
		
		public Gtk.Menu Menu
		{
			get
			{
				return d_menu;
			}
		}
		
		private void Traverse(Wrappers.Group grp, Gtk.Menu sub)
		{
			foreach (Wrappers.Wrapper child in grp.Children)
			{
				AddTemplate(sub, child);
			}
			
			sub.ShowAll();
			
			grp.ChildAdded += HandleChildAdded;
			grp.ChildRemoved += HandleChildRemoved;
		}
		
		private void AddTemplate(Gtk.Menu menu, Wrappers.Wrapper template)
		{
			if (!(d_filter == null || d_filter(template) || (template is Wrappers.Group && d_recursive)))
			{
				return;
			}
			
			string lbl = template.FullId.Replace("_", "__");

			Gtk.MenuItem item = new Gtk.MenuItem(lbl);
			item.Show();
			
			item.Activated += delegate {
				if (item.Submenu == null)
				{
					Activated(this, template);
				}
			};

			menu.Append(item);
			
			d_map[template] = new MenuInfo(item, menu);

			if (d_recursive && template is Wrappers.Group)
			{
				Gtk.Menu sub = new Gtk.Menu();
				item.Submenu = sub;

				Traverse((Wrappers.Group)template, sub);
			}
			
			template.WrappedObject.AddNotification("id", HandleIdChanged);
		}
		
		private void HandleIdChanged(object source, GLib.NotifyArgs args)
		{
			Wrappers.Wrapper wrapped = Wrappers.Wrapper.Wrap((Cpg.Object)source);
			Gtk.MenuItem item = d_map[wrapped].Item;
			
			item.Remove(item.Child);
			Gtk.Label lbl = new Gtk.Label(wrapped.Id.Replace("_", "__"));
			lbl.Show();

			item.Add(lbl);
		}

		private void HandleChildAdded(Wrappers.Group source, Wrappers.Wrapper child)
		{
			Gtk.Menu sub;
			
			if (child is Wrappers.Import)
			{
				d_map[child] = new TemplatesMenu.MenuInfo(null, d_map[source].Menu);

				foreach (Wrappers.Wrapper wrapper in (child as Wrappers.Group).Children)
				{
					HandleChildAdded(child as Wrappers.Group, wrapper);
				}
				
				return;
			}
			
			if (d_map[source].Item == null)
			{
				sub = d_map[source].Menu;
			}
			else
			{
				sub = (Gtk.Menu)d_map[source].Item.Submenu;
			}
			
			AddTemplate(sub, child);
		}
		
		private void RemoveFromMap(Wrappers.Wrapper child)
		{
			child.WrappedObject.RemoveNotification("id", HandleIdChanged);

			d_map.Remove(child);
			
			if (d_recursive && child is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)child;
				
				foreach (Wrappers.Wrapper w in grp.Children)
				{
					RemoveFromMap(w);
				}
			}
		}
		
		private void HandleChildRemoved(Wrappers.Group source, Wrappers.Wrapper child)
		{
			if (!d_map.ContainsKey(child))
			{
				return;
			}

			Gtk.Menu sub = d_map[child].Menu;
			sub.Remove(d_map[child].Item);
			
			if (d_recursive && child is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)child;
				
				grp.ChildAdded -= HandleChildAdded;
				grp.ChildRemoved -= HandleChildRemoved;
			}
			
			RemoveFromMap(child);
		}
	}
}

