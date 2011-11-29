using System;
using Gtk;
using System.Collections.Generic;
using System.Text;

namespace Cpg.Studio.Widgets
{
	public class Pathbar : HBox
	{
		public delegate void ActivateHandler(object source, Wrappers.Group grp);
		public event ActivateHandler Activated = delegate {};

		private Dictionary<Wrappers.Group, ToggleButton> d_pathWidgets;
		private List<Wrappers.Group> d_pathGroups;

		private Wrappers.Group d_active;
		private List<Wrappers.Group> d_roots;

		public Pathbar(params Wrappers.Group[] roots) : base(false, 0)
		{
			d_pathWidgets = new Dictionary<Wrappers.Group, ToggleButton>();
			d_pathGroups = new List<Wrappers.Group>();
			d_roots = new List<Wrappers.Group>(roots);

			d_active = null;
		}
		
		private List<Wrappers.Group> Collect(Wrappers.Group grp)
		{
			List<Wrappers.Group> ret = new List<Wrappers.Group>();

			do
			{
				ret.Add(grp);
				grp = grp.Parent;
			} while (grp != null);
			
			ret.Reverse();
			return ret;
		}
		
		private void Clear()
		{
			while (Children.Length != 0)
			{
				Remove(Children[0]);
			}
			
			foreach (Wrappers.Group grp in d_pathGroups)
			{
				grp.ChildRemoved -= HandleChildRemoved;
			}
			
			d_pathWidgets.Clear();
			d_pathGroups.Clear();

			d_active = null;
		}
		
		private void SetActive(Wrappers.Group grp, bool active)
		{
			if (grp == null)
			{
				return;
			}

			ToggleButton but = d_pathWidgets[grp];
			
			but.Toggled -= HandleGroupToggled;
			but.Active = active;
			but.Toggled += HandleGroupToggled;
			
			but.Sensitive = !active || d_roots.Contains(grp);
			but.Relief = active ? ReliefStyle.Half : ReliefStyle.None;
			
			if (active)
			{
				d_active = grp;
			}
		}
		
		private string RootName(Wrappers.Group root)
		{
			string ret = root.ToString();
			
			if (ret.Length > 0 && char.IsLetter(ret[0]))
			{
				ret = ret.Substring(0, 1).ToUpper() + ret.Substring(1);
			}
			
			return ret;
		}
		
		public void Update(Wrappers.Group grp)
		{
			if (d_active == grp)
			{
				return;
			}

			// Check if the group is already in the path
			if (grp != null && d_pathWidgets.ContainsKey(grp))
			{
				SetActive(d_active, false);
				SetActive(grp, true);

				d_active = grp;
			}
			else
			{
				// Construct a new path
				Clear();

				if (grp == null)
				{
					return;
				}

				List<Wrappers.Group> groups = Collect(grp);
				
				for (int i = 0; i < groups.Count; ++i)
				{
					if (i != 0)
					{
						Arrow ar = new Arrow(ArrowType.Right, ShadowType.None);
						ar.Show();
						PackStart(ar, false, false, 0);
					}
					
					ToggleButton but = new ToggleButton();
					but.Show();

					HBox hbox = new HBox(false, 0);
					hbox.Show();

					Label lbl = new Label(i == 0 ? RootName(groups[i]) : groups[i].ToString());
					lbl.Show();
					
					if (i == 0)
					{
						List<Wrappers.Group> roots = new List<Wrappers.Group>(d_roots);
						roots.Remove(groups[0]);
						
						if (roots.Count != 0)
						{
							Arrow arrow = new Arrow(ArrowType.Down, ShadowType.None);
							arrow.Show();
							hbox.PackStart(arrow, false, false, 0);
						}
					}
					
					hbox.PackStart(lbl, false, false, 0);
					but.Add(hbox);
					
					PackStart(but, false, false, 0);
					
					but.Data["WrappedGroup"] = groups[i];
					d_pathWidgets[groups[i]] = but;
					d_pathGroups.Add(groups[i]);
					
					but.Toggled += HandleGroupToggled;
					
					bool last = (i == groups.Count - 1);
					
					SetActive(groups[i], last);					
					groups[i].ChildRemoved += HandleChildRemoved;
				}
			}
		}

		private void HandleChildRemoved(Wrappers.Group source, Wrappers.Wrapper child)
		{
			Wrappers.Group grp = child as Wrappers.Group;
			
			if (grp == null)
			{
				return;
			}

			if (d_pathWidgets.ContainsKey(grp))
			{
				int start = d_pathGroups.IndexOf(grp);
				
				for (int i = start; i < d_pathGroups.Count; ++i)
				{
					d_pathGroups[i].ChildRemoved -= HandleChildRemoved;
					d_pathWidgets.Remove(d_pathGroups[i]);
					
					if (d_pathGroups[i] == d_active)
					{
						d_active = null;
					}
				}
				
				d_pathGroups.RemoveRange(start, d_pathGroups.Count - start);
				
				Widget[] children = Children;
				
				for (int i = start * 2 - 1; i < children.Length; ++i)
				{
					children[i].Destroy();
				}
			}
		}
		
		private void PopupRoots()
		{
			List<Wrappers.Group> roots = new List<Wrappers.Group>(d_roots);
			roots.Remove(d_active);
			
			Menu menu = new Menu();
			menu.Show();

			foreach (Wrappers.Group root in d_roots)
			{
				MenuItem item = new MenuItem(RootName(root));
				item.Show();
				
				Wrappers.Group grp = root;
				
				item.Activated += delegate {
					Update(grp);
					
					Activated(this, d_active);
				};

				menu.Append(item);
			}
			
			menu.Popup(null, null, null, 1, 0);
		}

		private void HandleGroupToggled(object sender, EventArgs e)
		{
			Widget w = (Widget)sender;
			
			ToggleButton toggle = (ToggleButton)w;
			Wrappers.Group selected = (Wrappers.Group)w.Data["WrappedGroup"];
			
			if (selected == d_active)
			{
				if (!toggle.Active)
				{
					toggle.Active = true;
					return;
				}

				if (d_roots.Contains(selected) && d_roots.Count > 1)
				{
					PopupRoots();
				}

				return;
			}

			SetActive(d_active, false);
			SetActive(selected, true);
			
			Activated(this, d_active);
		}
	}
}

