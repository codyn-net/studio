using System;
using System.Collections.Generic;

namespace Cpg.Studio
{
	public class Actions
	{
		private Undo.Manager d_undoManager;

		public Actions(Undo.Manager undoManager)
		{
			d_undoManager = undoManager;
		}
		
		public void AddState(Wrappers.Group parent, int x, int y)
		{
			Wrappers.State state = new Wrappers.State();
			state.Allocation = new Allocation(x, y, 1, 1);
			
			d_undoManager.Do(new Undo.AddObject(parent, state));
		}
		
		public void AddLink(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			// Add links between each first selected N-1 objects and selected object N
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);
			
			sel.RemoveAll(item => item is Wrappers.Link);
			
			if (sel.Count < 2)
			{
				return;
			}
			
			Wrappers.Wrapper last = sel[sel.Count - 1];
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			for (int i = 0; i < sel.Count - 1; ++i)
			{
				Wrappers.Link link = new Wrappers.Link(new Cpg.Link("link", sel[i], last));
				actions.Add(new Undo.AddObject(parent, link));
			}
			
			if (actions.Count > 1)
			{
				d_undoManager.Do(new Undo.Group(actions));
			}
			else
			{
				d_undoManager.Do(actions[0]);
			}
		}
		
		public void Delete(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);
			
			if (sel.Count == 0)
			{
				return;
			}
			
			// Get all the links that are connected to selected items
			foreach (Wrappers.Wrapper child in parent.Children)
			{
				if (sel.Contains(child) || !(child is Wrappers.Link))
				{
					continue;
				}
				
				Wrappers.Link link = (Wrappers.Link)child;
				
				if (sel.Contains(link.To) || sel.Contains(link.From))
				{
					sel.Add(link);
				}
			}
			
			// Remove them all!
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper child in sel)
			{
				actions.Add(new Undo.RemoveObject(child));
			}
			
			if (actions.Count == 1)
			{
				d_undoManager.Do(actions[0]);
			}
			else
			{
				d_undoManager.Do(new Undo.Group(actions));
			}
		}
		
		public void Group()
		{
		}
		
		public void Ungroup()
		{
		}
		
		public void Copy()
		{
		}
		
		public void Cut()
		{
		}
		
		public void Paste()
		{
		}
		
		public void Move(List<Wrappers.Wrapper> all, int dx, int dy)
		{
			List<Wrappers.Wrapper> objs = new List<Wrappers.Wrapper>(all);
			objs.RemoveAll(item => item is Wrappers.Link);
			
			if (objs.Count == 0)
			{
				return;
			}
			
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper obj in objs)
			{
				actions.Add(new Undo.MoveObject(obj, dx, dy));
			}
			
			if (actions.Count == 1)
			{
				d_undoManager.Do(actions[0]);
			}
			else
			{
				d_undoManager.Do(new Undo.Group(actions));
			}
		}
	}
}

