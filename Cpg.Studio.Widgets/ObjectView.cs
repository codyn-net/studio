using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class ObjectView : TreeView
	{
		public delegate void PropertyHandler(ObjectView source, Wrappers.Wrapper obj, Cpg.Property property);

		public event PropertyHandler Toggled = delegate {};
		public event PropertyHandler PropertyAdded = delegate {};

		private TreeStore d_store;
		private Wrappers.Network d_network;
		
		private enum ObjectType
		{
			Object = 1,
			Property = 2
		}
		
		private enum Column
		{
			ObjectType = 0,
			Object = 1,
			Property = 2,
			Activity = 3
		}
		
		private enum Activity
		{
			Active = 0,
			Inactive = 1,
			Inconsistent = 2
		}
		
		public ObjectView(Wrappers.Network network)
		{
			d_network = network;

			Build();
	
			d_network.ChildAdded += HandleChildAdded;
			d_network.ChildRemoved += HandleChildRemoved;
		}
		
		private void Build()
		{
			d_store = new TreeStore(typeof(ObjectType), typeof(Wrappers.Wrapper), typeof(Cpg.Property), typeof(Activity));
			
			d_store.SetSortFunc(1, DoRowSort);
			d_store.SetSortColumnId(1, SortType.Ascending);
			
			Model = d_store;
			HeadersVisible = false;
			
			InitStore();
			ExpandAll();
			
			CellRendererToggle toggle = new CellRendererToggle();
			TreeViewColumn column = new TreeViewColumn();
			
			column.PackStart(toggle, false);

			AppendColumn(column);
			
			toggle.Toggled += HandleToggled;
			
			column.SetCellDataFunc(toggle, (TreeCellDataFunc)OnToggleCellData);
			
			CellRendererText renderer = new CellRendererText();
			column.PackStart(renderer, true);
			column.SetCellDataFunc(renderer, (TreeCellDataFunc)OnTextCellData);
		}
		
		private void HandleCleared(object source, EventArgs args)
		{
			InitStore();
		}
		
		private T FromStore<T>(Gtk.TreeIter iter, Column column)
		{
			return (T)d_store.GetValue(iter, (int)column);
		}
		
		private void ToStore<T>(Gtk.TreeIter iter, Column column, T val)
		{
			d_store.SetValue(iter, (int)column, val);
		}

		private void HandleToggled(object o, ToggledArgs args)
		{
			TreeIter iter;
			
			d_store.GetIter(out iter, new TreePath(args.Path));
			Activity active = FromStore<Activity>(iter, Column.Activity);
			
			bool isactive = active == Activity.Inactive ? true : false;
			
			ToStore(iter, Column.Activity, isactive ? Activity.Active : Activity.Inactive);
			
			if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Object)
			{
				ToggleChildProperties(iter);
			}
			else
			{
				TreeIter parent;
				
				d_store.IterParent(out parent, iter);
				ToggleProperty(iter, FromStore<Wrappers.Wrapper>(parent, Column.Object), FromStore<Cpg.Property>(iter, Column.Property), isactive ? Activity.Active : Activity.Inactive);
			}
		}
		
		private void Disconnect(Wrappers.Wrapper obj)
		{
			obj.PropertyAdded -= HandlePropertyAdded;
			obj.PropertyRemoved -= HandlePropertyRemoved;
			
			if (obj is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)obj;
				
				grp.ChildAdded -= HandleChildAdded;
				grp.ChildRemoved -= HandleChildRemoved;
			}
		}
		
		private void HandlePropertyNameChanged(object source, GLib.NotifyArgs args)
		{
			Cpg.Property prop = (Cpg.Property)source;
			TreeIter parent;
			
			if (Find(prop.Object, out parent))
			{
				TreeIter iter;

				if (FindProperty(parent, prop, out iter))
				{
					TreePath path = d_store.GetPath(iter);
					d_store.EmitRowChanged(path, iter);
				}
			}
		}
		
		private void Disconnect(Cpg.Property prop)
		{
			prop.RemoveNotification("name", HandlePropertyNameChanged);
		}
		
		private void Disconnect(TreeIter iter, bool getChildren)
		{
			ObjectType type = FromStore<ObjectType>(iter, Column.ObjectType);

			if (type == ObjectType.Object)
			{
				Wrappers.Wrapper obj = FromStore<Wrappers.Wrapper>(iter, Column.Object);
				Disconnect(obj);
			}
			else
			{
				Cpg.Property prop = FromStore<Cpg.Property>(iter, Column.Property);
				Disconnect(prop);
			}
			
			TreeIter child;

			if (getChildren)
			{
				if (!d_store.IterChildren(out child, iter))
				{
					return;
				}
			}
			else
			{
				child = iter;
			}
			
			do
			{
				Disconnect(child);
			} while (d_store.IterNext(ref child));
		}
		
		private void Disconnect(TreeIter iter)
		{
			Disconnect(iter, true);
		}
		
		private void Disconnect()
		{
			TreeIter child;

			if (d_store.GetIterFirst(out child))
			{
				Disconnect(child, false);
			}			
		}
		
		private void InitStore()
		{
			Disconnect();
			d_store.Clear();
			
			foreach (Wrappers.Wrapper child in d_network.Children)
			{
				AddObject(child);
			}
		}
		
		private void ToggleProperty(TreeIter iter, Wrappers.Wrapper obj, Cpg.Property property, Activity activity)
		{
			if (activity != FromStore<Activity>(iter, Column.Activity))
			{
				ToStore(iter, Column.Activity, activity);
			}
				
			TreeIter parent;
				
			d_store.IterParent(out parent, iter);
			CheckConsistency(parent);

			Toggled(this, obj, property);
		}
		
		public void SetActive(Cpg.Property prop, bool active)
		{
			TreeIter parent;
			
			if (!Find(prop.Object, out parent))
			{
				return;
			}
				
			TreeIter child;
			
			if (FindProperty(parent, prop, out child))
			{
				ToggleProperty(child, prop.Object, prop, active ? Activity.Active : Activity.Inactive);
			}
		}
		
		public bool GetActive(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			TreeIter parent;
			
			if (!Find(obj, out parent))
			{
				return false;
			}
				
			TreeIter child;
			
			if (FindProperty(parent, prop, out child))
			{
				return FromStore<Activity>(child, Column.Activity) == Activity.Active;
			}
			
			return false;
		}
		
		private int DoRowSort(TreeModel model, TreeIter a, TreeIter b)
		{
			if (FromStore<ObjectType>(a, Column.ObjectType) == ObjectType.Object)
			{
				Wrappers.Wrapper o1 = FromStore<Wrappers.Wrapper>(a, Column.Object);
				Wrappers.Wrapper o2 = FromStore<Wrappers.Wrapper>(b, Column.Object);

				bool aat = o1 is Wrappers.Link;
				bool bat = o2 is Wrappers.Link;
				
				if (aat && !bat)
				{
					return 1;
				}
				else if (!aat && bat)
				{
					return 0;
				}
				else
				{
					if (o1 == null || o2 == null)
					{
						return 0;
					}

					return o1.ToString().CompareTo(o2.ToString());
				}
			}
			else
			{
				Cpg.Property p1 = FromStore<Cpg.Property>(a, Column.Property);
				Cpg.Property p2 = FromStore<Cpg.Property>(b, Column.Property);
				
				if (p1 == null || p2 == null)
				{
					return 0;
				}

				return p1.Name.CompareTo(p2.Name); 
			}
		}
		
		private void CheckConsistency(TreeIter parent)
		{
			TreeIter iter;
			
			if (!d_store.IterChildren(out iter, parent))
			{
				return;
			}
			
			Activity activity = Activity.Inactive;
			bool first = true;
			
			do
			{
				if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Property)
				{
					Activity ac = FromStore<Activity>(iter, Column.Activity);
					
					if (first)
					{
						activity = ac;
						first = false;
					}
					else if (ac != activity)
					{
						activity = Activity.Inconsistent;
						break;
					}
				}
			} while (d_store.IterNext(ref iter));
			
			ToStore(parent, Column.Activity, activity);
		}
		
		private bool Find(Wrappers.Wrapper obj, out TreeIter iter)
		{
			return Find(obj.Parent, obj, out iter);
		}
		
		private bool Find(Wrappers.Group parent, Wrappers.Wrapper obj, out TreeIter iter)
		{
			if (!d_store.GetIterFirst(out iter))
			{
				return false;
			}

			Stack<Wrappers.Wrapper> parents = new Stack<Wrappers.Wrapper>();
			
			parents.Push(obj);
			
			while (parent != null && parent.Parent != null)
			{
				parents.Push(parent);
				parent = parent.Parent;
			}
		
			while (true)
			{
				if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Object)
				{
					Wrappers.Wrapper wrap = FromStore<Wrappers.Wrapper>(iter, Column.Object);
					
					if (wrap == parents.Peek())
					{
						parents.Pop();
						
						if (parents.Count == 0)
						{
							return true;
						}
						
						TreeIter child;

						if (!d_store.IterChildren(out child, iter))
						{
							break;
						}

						iter = child;

						continue;
					}
				}

				if (!d_store.IterNext(ref iter))
				{
					break;
				}
			}
			
			return false;
		}
		
		private void RemoveObject(Wrappers.Wrapper obj)
		{
			RemoveObject(obj.Parent, obj);
		}
		
		private void RemoveObject(Wrappers.Group parent, Wrappers.Wrapper obj)
		{
			TreeIter iter;
			
			if (Find(parent, obj, out iter))
			{
				d_store.Remove(ref iter);
				Disconnect(obj);
			}
		}
		
		private void HandleObjectIdChanged(object source, GLib.NotifyArgs args)
		{
			Wrappers.Wrapper wrapped = Wrappers.Wrapper.Wrap((Cpg.Object)source);
			TreeIter iter;
			
			if (Find(wrapped, out iter))
			{
				TreePath path = d_store.GetPath(iter);
				d_store.EmitRowChanged(path, iter);
			}
		}
		
		private void Connect(Wrappers.Wrapper obj, TreeIter iter)
		{
			TreeRowReference iterPath = new TreeRowReference(d_store, d_store.GetPath(iter));

			foreach (Cpg.Property property in obj.Properties)
			{
				AddProperty(iter, obj, property);
			}
			
			obj.PropertyAdded += HandlePropertyAdded;
			obj.PropertyRemoved += HandlePropertyRemoved;
			
			obj.WrappedObject.AddNotification("id", HandleObjectIdChanged);
			
			if (obj is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)obj;
				
				foreach (Wrappers.Wrapper child in grp.Children)
				{
					AddObject(child, iter);
				}
				
				grp.ChildAdded += HandleChildAdded;
				grp.ChildRemoved += HandleChildRemoved;
			}
			
			ExpandToPath(iterPath.Path);
		}
		
		private void AddObject(Wrappers.Wrapper obj, TreeIter parent)
		{
			TreeIter iter;
			
			TreeRowReference parentPath = new TreeRowReference(d_store, d_store.GetPath(parent));

			iter = d_store.AppendValues(parent, ObjectType.Object, obj, null, Activity.Inactive);
			Connect(obj, iter);
			
			ExpandToPath(parentPath.Path);
		}
		
		private void AddObject(Wrappers.Wrapper obj)
		{
			TreeIter parent;
			
			if (obj.Parent != null && Find(obj.Parent, out parent))
			{
				AddObject(obj, parent);
			}
			else
			{
				TreeIter iter;
				
				iter = d_store.AppendValues(ObjectType.Object, obj, null, Activity.Inactive);
				
				Connect(obj, iter);
			}
		}

		private void HandleChildRemoved(Wrappers.Group source, Wrappers.Wrapper child)
		{
			RemoveObject(source, child);
		}

		private void HandleChildAdded(Wrappers.Group source, Wrappers.Wrapper child)
		{
			AddObject(child);
		}

		private bool FindProperty(TreeIter parent, Cpg.Property prop, out TreeIter iter)
		{
			if (!d_store.IterChildren(out iter, parent))
			{
				return false;
			}
			
			do
			{
				if (FromStore<ObjectType>(iter, Column.ObjectType) != ObjectType.Property)
				{
					continue;
				}

				Cpg.Property property = FromStore<Cpg.Property>(iter, Column.Property);
				
				if (property == prop)
				{
					return true;
				}
			} while (d_store.IterNext(ref iter));
			
			return false;
		}

		private void HandlePropertyRemoved(Wrappers.Wrapper source, Cpg.Property property)
		{
			TreeIter parent;
			
			if (Find(source, out parent))
			{
				TreeIter prop;

				if (FindProperty(parent, property, out prop))
				{
					d_store.Remove(ref prop);
				}
				
				CheckConsistency(parent);
			}
		}

		private void HandlePropertyAdded(Wrappers.Wrapper source, Cpg.Property prop)
		{
			TreeIter parent;
			
			if (Find(source, out parent))
			{
				AddProperty(parent, source, prop);
				CheckConsistency(parent);
			}
		}
		
		private void AddProperty(TreeIter parent, Wrappers.Wrapper obj, Cpg.Property prop)
		{
			TreeRowReference path = new TreeRowReference(d_store, d_store.GetPath(parent));

			d_store.AppendValues(parent, ObjectType.Property, null, prop, Activity.Inactive);
			
			prop.AddNotification("name", HandlePropertyNameChanged);
			
			ExpandToPath(path.Path);
			PropertyAdded(this, obj, prop);
		}
		
		private void OnToggleCellData(TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererToggle renderer = cell as CellRendererToggle;
			
			if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Object)
			{
				cell.CellBackgroundGdk = Style.Base(Gtk.StateType.Active);
			}
			else
			{
				cell.CellBackgroundGdk = Style.Base(this.State);
			}
			
			Activity activity = FromStore<Activity>(iter, Column.Activity);
			
			renderer.Active = activity != Activity.Inactive;
			renderer.Inconsistent = activity == Activity.Inconsistent;
		}
		
		private void OnTextCellData(TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText renderer = cell as CellRendererText;
			
			if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Object)
			{
				Wrappers.Wrapper wrapper = FromStore<Wrappers.Wrapper>(iter, Column.Object);

				string s = wrapper.ToString() + " (" + wrapper.GetType().Name + ")";
				
				if (wrapper is Wrappers.Link)
				{
					Wrappers.Link link = (Wrappers.Link)wrapper;
					s += " " + link.From.ToString() + " » " + link.To.ToString();
				}
				
				renderer.Markup = "<b>" + System.Security.SecurityElement.Escape(s) + "</b>";
				renderer.CellBackgroundGdk = Style.Base(StateType.Active);
				renderer.ForegroundGdk = Style.Text(StateType.Active);
			}
			else
			{
				Cpg.Property property = FromStore<Cpg.Property>(iter, Column.Property);
				
				renderer.Text = property.Name;
				cell.CellBackgroundGdk = Style.Base(this.State);
				renderer.ForegroundGdk = Style.Text(this.State);
			}
		}

		private void HandleObjectAdded(object source, Wrappers.Wrapper obj)
		{
			AddObject(obj);	
		}

		private void HandleObjectRemoved(object source, Wrappers.Wrapper obj)
		{
			RemoveObject(obj);
		}
		
		private void ToggleChildProperties(TreeIter parent)
		{
			Activity activity = FromStore<Activity>(parent, Column.Activity);
			
			TreeIter iter;
			
			if (!d_store.IterChildren(out iter, parent))
			{
				return;
			}
			
			Wrappers.Wrapper obj = FromStore<Wrappers.Wrapper>(parent, Column.Object);
			
			do
			{
				if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Property)
				{
					ToggleProperty(iter, obj, FromStore<Cpg.Property>(iter, Column.Property), activity);
				}
			} while (d_store.IterNext(ref iter));
		}
		
		protected override void OnDestroyed()
		{		
			Disconnect();
			
			d_network.ChildAdded -= HandleChildAdded;
			d_network.ChildRemoved -= HandleChildRemoved;
			
			base.OnDestroyed();
		}
	}
}
