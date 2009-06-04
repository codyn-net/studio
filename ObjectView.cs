using System;
using Gtk;

namespace Cpg.Studio
{
	public class ObjectView : TreeView
	{
		public delegate void PropertyHandler(ObjectView source, Components.Object obj, string property);

		public event PropertyHandler Toggled = delegate {};
		public event PropertyHandler PropertyAdded = delegate {};

		TreeStore d_store;
		Grid d_grid;
		
		public ObjectView(Grid grid)
		{
			d_grid = grid;
			Build();
		}
		
		private void Build()
		{
			d_store = new TreeStore(typeof(object), typeof(string), typeof(int));
			
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
			
			d_grid.ObjectAdded += HandleObjectAdded;
			d_grid.ObjectRemoved += HandleObjectRemoved;
			d_grid.LeveledUp += HandleLeveledUp;
			d_grid.LeveledDown += HandleLeveledDown;
		}

		private void HandleToggled(object o, ToggledArgs args)
		{
			TreeIter iter;
			
			d_store.GetIter(out iter, new TreePath(args.Path));
			int active = (int)d_store.GetValue(iter, 2);
			
			d_store.SetValue(iter, 2, active == 1 ? 0 : 1);
			
			if (d_store.GetValue(iter, 1) == null)
			{
				ToggleChildren(iter);
			}
			else
			{
				TreeIter parent;
				
				d_store.IterParent(out parent, iter);
				CheckConsistency(parent);
				
				ToggleProperty(iter, (int)d_store.GetValue(iter, 2) == 1);
			}
		}
		
		private void InitStore()
		{
			TreeIter iter;
			
			if (d_store.GetIterFirst(out iter))
			{
				do
				{
					Components.Object o = d_store.GetValue(iter, 0) as Components.Object;
					
					Disconnect(o);
				} while (d_store.IterNext(ref iter));
			}
			
			d_store.Clear();
			
			foreach (Components.Object obj in d_grid.Container.Children)
			{
				AddObject(obj);
			}
		}
		
		public void SetActive(Components.Object obj, string prop, bool active)
		{
			TreeIter parent;
			
			if (!Find(obj, out parent))
				return;
				
			TreeIter child;
			
			if (FindProperty(parent, prop, out child))
			{
				d_store.SetValue(child, 2, active ? 1 : 0);
			}
		}
		
		public bool GetActive(Components.Object obj, string prop)
		{
			TreeIter parent;
			
			if (!Find(obj, out parent))
				return false;
				
			TreeIter child;
			
			if (FindProperty(parent, prop, out child))
			{
				return (int)d_store.GetValue(child, 2) == 1;
			}
			
			return false;
		}
		
		private int DoRowSort(TreeModel model, TreeIter a, TreeIter b)
		{
			object o1 = model.GetValue(a, 0);
			object o2 = model.GetValue(b, 0);
			
			string o3 = model.GetValue(a, 1) as string;
			
			if (o3 == null)
			{
				bool aat = o1 is Components.Link;
				bool bat = o2 is Components.Link;
				
				if (aat && !bat)
					return 1;
				else if (!aat && bat)
					return 0;
				else
					return o1.ToString().CompareTo(o2.ToString());
			}
			else
			{
				return o3.CompareTo(model.GetValue(b, 1) as string); 
			}
		}
		
		private void CheckConsistency(TreeIter parent)
		{
			TreeIter iter;
			
			if (!d_store.IterChildren(out iter, parent))
				return;
			
			int numcheck = 0;
			
			do
			{
				numcheck += (int)d_store.GetValue(iter, 2);
			} while (d_store.IterNext(ref iter));
			
			if (numcheck == 0)
			{
				d_store.SetValue(parent, 2, 0);
			}
			else if (numcheck == d_store.IterNChildren(parent))
			{
				d_store.SetValue(parent, 2, 1);
			}
			else
			{
				d_store.SetValue(parent, 2, 2);
			}
		}
		
		private void Disconnect(Components.Object obj)
		{
			obj.PropertyAdded -= HandlePropertyAdded;
			obj.PropertyRemoved -= HandlePropertyRemoved;
			obj.PropertyChanged -= HandlePropertyChanged;
		}
		
		private bool Find(Components.Object obj, out TreeIter iter)
		{
			if (!d_store.GetIterFirst(out iter))
				return false;
			
			do
			{
				Components.Object other = d_store.GetValue(iter, 0) as Components.Object;
				
				if (other.Equals(obj))
				{
					return true;
				}
			} while (d_store.IterNext(ref iter));
			
			return false;
		}
		
		private void RemoveObject(Components.Object obj)
		{
			TreeIter parent;
			
			if (Find(obj, out parent))
			{
				d_store.Remove(ref parent);
			}
		}
		
		private void AddObject(Components.Object obj)
		{
			if (!(obj is Components.Simulated))
				return;
			
			TreeIter parent;
			
			parent = d_store.AppendValues(obj, null, 0);
			
			foreach (string property in obj.Properties)
			{
				AddProperty(parent, obj, property);
			}
			
			CheckConsistency(parent);
			
			obj.PropertyAdded += HandlePropertyAdded;
			obj.PropertyRemoved += HandlePropertyRemoved;
			obj.PropertyChanged += HandlePropertyChanged;
			
			ExpandRow(d_store.GetPath(parent), false);
		}

		private void HandlePropertyChanged(Components.Object source, string name)
		{
			if (name != "id")
				return;

			TreeIter parent;
			
			if (!Find(source, out parent))
				return;
			
			d_store.EmitRowChanged(d_store.GetPath(parent), parent);
		}

		private bool FindProperty(TreeIter parent, string name, out TreeIter iter)
		{
			if (!d_store.IterChildren(out iter, parent))
				return false;
			
			do
			{
				string property = d_store.GetValue(iter, 1) as string;
				
				if (property == name)
				{
					return true;
				}
			} while (d_store.IterNext(ref iter));
			
			return false;
		}

		private void HandlePropertyRemoved(Components.Object source, string name)
		{
			TreeIter parent;
			
			if (Find(source as Components.Object, out parent))
			{
				TreeIter prop;

				if (FindProperty(parent, name, out prop))
				{
					d_store.Remove(ref prop);
				}
				
				CheckConsistency(parent);
			}
		}

		private void HandlePropertyAdded(Components.Object source, string name)
		{
			TreeIter parent;
			
			if (Find(source as Components.Object, out parent))
			{
				AddProperty(parent, source as Components.Object, name);
				CheckConsistency(parent);
			}
		}
		
		private void AddProperty(TreeIter parent, Components.Object obj, string property)
		{
			d_store.AppendValues(parent, obj, property, 0);
			
			PropertyAdded(this, obj, property);
		}
		
		private void OnToggleCellData(TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererToggle renderer = cell as CellRendererToggle;
			
			if (model.GetValue(iter, 1) == null)
			{
				cell.CellBackgroundGdk = Style.Base(Gtk.StateType.Active);
			}
			else
			{
				cell.CellBackgroundGdk = Style.Base(this.State);
			}
			
			int active = (int)model.GetValue(iter, 2);
			
			renderer.Active = active != 0;
			renderer.Inconsistent = active == 2;
		}
		
		private void OnTextCellData(TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object o = model.GetValue(iter, 0);
			string st = model.GetValue(iter, 1) as string;
			
			CellRendererText renderer = cell as CellRendererText;
			
			if (st == null)
			{
				string s = (o as Components.Object).ToString() + " (" + o.GetType().Name + ")";
				
				if (o is Components.Link)
				{
					Components.Link link = o as Components.Link;
					s += " " + link.From.ToString() + " » " + link.To.ToString();
				}
				
				renderer.Markup = "<b>" + System.Security.SecurityElement.Escape(s) + "</b>";
				renderer.CellBackgroundGdk = Style.Base(StateType.Active);
			}
			else
			{
				renderer.Text = st;
				cell.CellBackgroundGdk = Style.Base(this.State);
			}
		}

		private void HandleLeveledDown(object source, Components.Object obj)
		{
			InitStore();
		}

		private void HandleLeveledUp(object source, Components.Object obj)
		{
			InitStore();
		}

		private void HandleObjectAdded(object source, Components.Object obj)
		{
			AddObject(obj);	
		}

		private void HandleObjectRemoved(object source, Components.Object obj)
		{
			RemoveObject(obj);
		}
		
		private void ToggleProperty(TreeIter iter, bool active)
		{
			d_store.SetValue(iter, 2, active ? 1 : 0);
			
			TreeIter parent;
			d_store.IterParent(out parent, iter);
			
			Toggled(this, d_store.GetValue(parent, 0) as Components.Object, d_store.GetValue(iter, 1) as string);
		}
		
		private void ToggleChildren(TreeIter parent)
		{
			int active = (int)d_store.GetValue(parent, 2);
			
			TreeIter iter;
			
			if (!d_store.IterChildren(out iter, parent))
				return;
			
			do
			{
				if ((int)d_store.GetValue(iter, 2) != active)
				{
					ToggleProperty(iter, active == 1);
				}
			} while (d_store.IterNext(ref iter));
		}
	}
}
