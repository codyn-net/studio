using System;
using Gtk;

namespace Cpg.Studio
{
	public class ObjectView : TreeView
	{
		public delegate void PropertyHandler(ObjectView source, Wrappers.Wrapper obj, Cpg.Property property);

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
			d_store = new TreeStore(typeof(object), typeof(object), typeof(int));
			
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
			d_grid.Cleared += HandleCleared;
		}
		
		private void HandleCleared(object source, EventArgs args)
		{
			InitStore();
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
					Wrappers.Wrapper o = d_store.GetValue(iter, 0) as Wrappers.Wrapper;
					
					Disconnect(o);
				} while (d_store.IterNext(ref iter));
			}
			
			d_store.Clear();
			
			foreach (Wrappers.Wrapper obj in d_grid.Container.Children)
			{
				AddObject(obj);
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
				d_store.SetValue(child, 2, active ? 1 : 0);
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
				return (int)d_store.GetValue(child, 2) == 1;
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
		
		private void Disconnect(Wrappers.Wrapper obj)
		{
			obj.PropertyAdded -= HandlePropertyAdded;
			obj.PropertyRemoved -= HandlePropertyRemoved;
			obj.PropertyChanged -= HandlePropertyChanged;
		}
		
		private bool Find(Wrappers.Wrapper obj, out TreeIter iter)
		{
			if (!d_store.GetIterFirst(out iter))
				return false;
			
			do
			{
				Wrappers.Wrapper other = d_store.GetValue(iter, 0) as Wrappers.Wrapper;
				
				if (other.Equals(obj))
				{
					return true;
				}
			} while (d_store.IterNext(ref iter));
			
			return false;
		}
		
		private void RemoveObject(Wrappers.Wrapper obj)
		{
			TreeIter parent;
			
			if (Find(obj, out parent))
			{
				d_store.Remove(ref parent);
			}
		}
		
		private void AddObject(Wrappers.Wrapper obj)
		{
			if (!(obj is Wrappers.Wrapper))
			{
				return;
			}
			
			TreeIter parent;
			
			parent = d_store.AppendValues(obj, null, 0);
			
			foreach (Cpg.Property property in obj.Properties)
			{
				AddProperty(parent, obj, property);
			}
			
			CheckConsistency(parent);
			
			obj.PropertyAdded += HandlePropertyAdded;
			obj.PropertyRemoved += HandlePropertyRemoved;
			obj.PropertyChanged += HandlePropertyChanged;
			
			ExpandRow(d_store.GetPath(parent), false);
		}

		private void HandlePropertyChanged(Wrappers.Wrapper source, Cpg.Property prop)
		{
			TreeIter parent;
			
			if (!Find(source, out parent))
			{
				return;
			}
			
			d_store.EmitRowChanged(d_store.GetPath(parent), parent);
		}

		private bool FindProperty(TreeIter parent, Cpg.Property prop, out TreeIter iter)
		{
			if (!d_store.IterChildren(out iter, parent))
			{
				return false;
			}
			
			do
			{
				Cpg.Property property = d_store.GetValue(iter, 1) as Cpg.Property;
				
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
			
			if (Find(source as Wrappers.Wrapper, out parent))
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
			
			if (Find(source as Wrappers.Wrapper, out parent))
			{
				AddProperty(parent, source, prop);
				CheckConsistency(parent);
			}
		}
		
		private void AddProperty(TreeIter parent, Wrappers.Wrapper obj, Cpg.Property prop)
		{
			d_store.AppendValues(parent, obj, prop, 0);
			
			ExpandRow(d_store.GetPath(parent), false);
			PropertyAdded(this, obj, prop);
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
			Cpg.Property st = model.GetValue(iter, 1) as Cpg.Property;
			
			CellRendererText renderer = cell as CellRendererText;
			
			if (st == null)
			{
				string s = (o as Wrappers.Wrapper).ToString() + " (" + o.GetType().Name + ")";
				
				if (o is Wrappers.Link)
				{
					Wrappers.Link link = o as Wrappers.Link;
					s += " " + link.From.ToString() + " » " + link.To.ToString();
				}
				
				renderer.Markup = "<b>" + System.Security.SecurityElement.Escape(s) + "</b>";
				renderer.CellBackgroundGdk = Style.Base(StateType.Active);
				renderer.ForegroundGdk = Style.Text(StateType.Active);
			}
			else
			{
				renderer.Text = st.Name;
				cell.CellBackgroundGdk = Style.Base(this.State);
				renderer.ForegroundGdk = Style.Text(this.State);
			}
		}

		private void HandleLeveledDown(object source, Wrappers.Wrapper obj)
		{
			InitStore();
		}

		private void HandleLeveledUp(object source, Wrappers.Wrapper obj)
		{
			InitStore();
		}

		private void HandleObjectAdded(object source, Wrappers.Wrapper obj)
		{
			AddObject(obj);	
		}

		private void HandleObjectRemoved(object source, Wrappers.Wrapper obj)
		{
			RemoveObject(obj);
		}
		
		private void ToggleProperty(TreeIter iter, bool active)
		{
			d_store.SetValue(iter, 2, active ? 1 : 0);
			
			TreeIter parent;
			d_store.IterParent(out parent, iter);
			
			Toggled(this, d_store.GetValue(parent, 0) as Wrappers.Wrapper, d_store.GetValue(iter, 1) as Cpg.Property);
		}
		
		private void ToggleChildren(TreeIter parent)
		{
			int active = (int)d_store.GetValue(parent, 2);
			
			TreeIter iter;
			
			if (!d_store.IterChildren(out iter, parent))
			{
				return;
			}
			
			do
			{
				if ((int)d_store.GetValue(iter, 2) != active)
				{
					ToggleProperty(iter, active == 1);
				}
			} while (d_store.IterNext(ref iter));
		}
		
		protected override void OnDestroyed()
		{
			base.OnDestroyed();
			
			d_grid.ObjectAdded -= HandleObjectAdded;
			d_grid.ObjectRemoved -= HandleObjectRemoved;
			d_grid.LeveledUp -= HandleLeveledUp;
			d_grid.LeveledDown -= HandleLeveledDown;
		}
	}
}
