using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio
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
			Property = 1,
			Activity = 2
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
		}
		
		private void Build()
		{
			d_store = new TreeStore(typeof(ObjectType), typeof(object), typeof(Activity));
			
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
		
		private void Disconnect(TreeIter iter, bool getChildren)
		{
			if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Object)
			{
				Wrappers.Wrapper obj = FromStore<Wrappers.Wrapper>(iter, Column.Object);

				obj.PropertyAdded -= HandlePropertyAdded;
				obj.PropertyRemoved -= HandlePropertyRemoved;
				
				if (obj is Wrappers.Group)
				{
					Wrappers.Group grp = (Wrappers.Group)obj;
					
					grp.ChildAdded -= HandleChildAdded;
					grp.ChildRemoved -= HandleChildRemoved;
				}
			}
			
			TreeIter child;

			if (getChildren)
			{
				d_store.IterChildren(out child, iter);
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

			d_store.GetIterFirst(out child);
			Disconnect(child, false);
		}
		
		private void InitStore()
		{
			Disconnect();
			d_store.Clear();
			
			AddObject(d_network);
		}
		
		private void ToggleProperty(TreeIter iter, Wrappers.Wrapper obj, Cpg.Property property, Activity activity)
		{
			if (activity != FromStore<Activity>(iter, Column.Activity))
			{
				ToStore(iter, Column.Activity, activity);
				
				TreeIter parent;
				
				d_store.IterParent(out parent, iter);
				CheckConsistency(parent);

				Toggled(this, obj, property);
			}
		}
		
		public void SetActive(Wrappers.Wrapper obj, Cpg.Property prop, bool active)
		{
			TreeIter parent;
			
			if (!Find(obj, out parent))
			{
				return;
			}
				
			TreeIter child;
			
			if (FindProperty(parent, prop, out child))
			{
				ToggleProperty(child, obj, prop, active ? Activity.Active : Activity.Inactive);
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
			object o1 = model.GetValue(a, 0);
			object o2 = model.GetValue(b, 0);
			
			string o3 = (model.GetValue(a, 1) as Cpg.Property).Name;
			
			if (o3 == null)
			{
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
					return o1.ToString().CompareTo(o2.ToString());
				}
			}
			else
			{
				return o3.CompareTo((model.GetValue(b, 1) as Cpg.Property).Name); 
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
			if (!d_store.GetIterFirst(out iter))
			{
				return false;
			}

			Queue<Wrappers.Wrapper> parents = new Queue<Wrappers.Wrapper>();
			
			Wrappers.Group parent = obj.Parent;
			
			while (parent != null)
			{
				parents.Enqueue(parent);
				parent = parent.Parent;
			}
			
			parents.Enqueue(obj);
		
			while (true)
			{
				if (FromStore<ObjectType>(iter, Column.ObjectType) == ObjectType.Object)
				{
					Wrappers.Wrapper wrap = FromStore<Wrappers.Wrapper>(iter, Column.Object);
					
					if (wrap.Equals(parents.Peek()))
					{
						parents.Dequeue();
						
						if (parents.Count == 0)
						{
							return true;
						}
						
						d_store.IterChildren(out iter, iter);
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
			TreeIter iter;
			
			if (Find(obj, out iter))
			{
				d_store.Remove(ref iter);
			}
		}
		
		private void Connect(Wrappers.Wrapper obj, TreeIter iter)
		{
			foreach (Cpg.Property property in obj.Properties)
			{
				AddProperty(iter, obj, property);
			}
			
			obj.PropertyAdded += HandlePropertyAdded;
			obj.PropertyRemoved += HandlePropertyRemoved;
			
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
			
			ExpandRow(d_store.GetPath(iter), false);
		}
		
		private void AddObject(Wrappers.Wrapper obj, TreeIter parent)
		{
			TreeIter iter;

			iter = d_store.AppendValues(parent, ObjectType.Object, obj, Activity.Inactive);
			Connect(obj, iter);
		}
		
		private void AddObject(Wrappers.Wrapper obj)
		{
			TreeIter parent;
			
			if (obj.Parent != null)
			{
				if (Find(obj.Parent, out parent))
				{
					AddObject(obj, parent);
				}
			}
			else
			{
				TreeIter iter;

				iter = d_store.AppendValues(ObjectType.Object, obj, Activity.Inactive);
				
				Connect(obj, iter);
			}
		}

		private void HandleChildRemoved(Wrappers.Group source, Wrappers.Wrapper child)
		{
			RemoveObject(child);
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
			d_store.AppendValues(parent, ObjectType.Property, prop, Activity.Inactive);
			
			ExpandRow(d_store.GetPath(parent), false);
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
	}
}
