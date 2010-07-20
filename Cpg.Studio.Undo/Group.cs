using System;
using System.Collections.Generic;

namespace Cpg.Studio.Undo
{
	public class Group : IAction
	{
		private List<IAction> d_actions;

		public Group(params IAction[] actions)
		{
			d_actions = new List<IAction>(actions);
		}
		
		public List<IAction> Actions
		{
			get
			{
				return d_actions;
			}
		}
		
		public Group(IEnumerable<IAction> actions)
		{
			d_actions = new List<IAction>(actions);
		}
		
		public void Add(IAction action)
		{
			d_actions.Add(action);
		}
		
		public void Undo()
		{
			List<IAction> reversed = new List<IAction>(d_actions);
			reversed.Reverse();

			foreach (IAction action in reversed)
			{
				action.Undo();
			}
		}
		
		public void Redo()
		{
			foreach (IAction action in d_actions)
			{
				action.Redo();
			}
		}
		
		public bool CanMerge(IAction other)
		{
			if (!(other is Group))
			{
				return false;
			}
			
			Group grp = (Group)other;
			
			if (grp.d_actions.Count != d_actions.Count)
			{
				return false;
			}
			
			for (int i = 0; i < d_actions.Count; ++i)
			{
				if (!d_actions[i].CanMerge(grp.d_actions[i]))
				{
					return false;
				}
			}
			
			return true;
		}
		
		public void Merge(IAction other)
		{
			Group grp = (Group)other;
			
			for (int i = 0; i < d_actions.Count; ++i)
			{
				d_actions[i].Merge(grp.d_actions[i]);
			}
		}
		
		public bool Verify()
		{
			foreach (IAction action in d_actions)
			{
				if (!action.Verify())
				{
					return false;
				}
			}
			
			return true;
		}
	}
}
