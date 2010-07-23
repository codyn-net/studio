using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class Pathbar : HBox
	{
		public delegate void ActivateHandler(object source, Wrappers.Group grp);
		public event ActivateHandler Activated = delegate {};

		private Dictionary<Wrappers.Group, ToggleButton> d_pathWidgets;
		private List<Wrappers.Group> d_pathGroups;

		private Wrappers.Group d_active;

		public Pathbar() : base(false, 0)
		{
			d_pathWidgets = new Dictionary<Wrappers.Group, ToggleButton>();
			d_pathGroups = new List<Wrappers.Group>();

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
			
			but.Sensitive = !active;
			but.Relief = active ? ReliefStyle.Half : ReliefStyle.None;
			
			if (active)
			{
				d_active = grp;
			}
		}
		
		public void Update(Wrappers.Group grp)
		{
			if (d_active == grp)
			{
				return;
			}

			// Check if the group is already in the path
			if (d_pathWidgets.ContainsKey(grp))
			{
				SetActive(d_active, false);
				SetActive(grp, true);

				d_active = grp;
			}
			else
			{
				// Construct a new path
				Clear();

				List<Wrappers.Group> groups = Collect(grp);
				
				for (int i = 0; i < groups.Count; ++i)
				{
					if (i != 0)
					{
						Arrow ar = new Arrow(ArrowType.Right, ShadowType.None);
						PackStart(ar, false, false, 0);
					}
					
					ToggleButton but = new ToggleButton(groups[i].ToString());
					PackStart(but, false, false, 0);
					
					but.Data["WrappedGroup"] = groups[i];
					d_pathWidgets[groups[i]] = but;
					d_pathGroups.Add(groups[i]);
					
					but.Toggled += HandleGroupToggled;
					
					bool last = (i == groups.Count - 1);
					
					SetActive(groups[i], last);					
					groups[i].ChildRemoved += HandleChildRemoved;
				}
				
				ShowAll();
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
				}
				
				d_pathGroups.RemoveRange(start, d_pathGroups.Count - start);
				
				Widget[] children = Children;
				
				for (int i = start * 2 - 1; i < children.Length; ++i)
				{
					children[i].Destroy();
				}
			}
		}

		private void HandleGroupToggled(object sender, EventArgs e)
		{
			Widget w = (Widget)sender;

			SetActive(d_active, false);
			SetActive((Wrappers.Group)w.Data["WrappedGroup"], true);
			
			Activated(this, d_active);
		}
	}
}

