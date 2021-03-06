using System;
using Gtk;
using System.Collections.Generic;
using System.Text;

namespace Cdn.Studio.Widgets
{
	public class Pathbar : HBox
	{
		public delegate void ActivateHandler(object source, Wrappers.Node grp);
		public event ActivateHandler Activated = delegate {};

		private Dictionary<Wrappers.Node, ToggleButton> d_pathWidgets;
		private List<Wrappers.Node> d_pathGroups;

		private Wrappers.Node d_active;
		private List<Wrappers.Node> d_roots;

		public Pathbar(params Wrappers.Node[] roots) : base(false, 0)
		{
			d_pathWidgets = new Dictionary<Wrappers.Node, ToggleButton>();
			d_pathGroups = new List<Wrappers.Node>();
			d_roots = new List<Wrappers.Node>(roots);

			d_active = null;
		}
		
		private List<Wrappers.Node> Collect(Wrappers.Node grp)
		{
			List<Wrappers.Node> ret = new List<Wrappers.Node>();

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
			
			foreach (Wrappers.Node grp in d_pathGroups)
			{
				grp.ChildRemoved -= HandleChildRemoved;
			}
			
			d_pathWidgets.Clear();
			d_pathGroups.Clear();

			d_active = null;
		}
		
		private void SetActive(Wrappers.Node grp, bool active)
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
		
		private string RootName(Wrappers.Node root)
		{
			string ret = root.ToString();
			
			if (ret.Length > 0 && char.IsLetter(ret[0]))
			{
				ret = ret.Substring(0, 1).ToUpper() + ret.Substring(1);
			}
			
			return ret;
		}
		
		public void Update(Wrappers.Node grp)
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

				List<Wrappers.Node> groups = Collect(grp);
				
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
						List<Wrappers.Node> roots = new List<Wrappers.Node>(d_roots);
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

		private void HandleChildRemoved(Wrappers.Node source, Wrappers.Wrapper child)
		{
			Wrappers.Node grp = child as Wrappers.Node;
			
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
			List<Wrappers.Node> roots = new List<Wrappers.Node>(d_roots);
			roots.Remove(d_active);
			
			Menu menu = new Menu();
			menu.Show();

			foreach (Wrappers.Node root in d_roots)
			{
				MenuItem item = new MenuItem(RootName(root));
				item.Show();
				
				Wrappers.Node grp = root;
				
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
			Wrappers.Node selected = (Wrappers.Node)w.Data["WrappedGroup"];
			
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

