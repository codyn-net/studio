using System;
using System.Collections.Generic;

namespace Cdn.Studio.Undo
{
	public class Group : IAction
	{
		private List<IAction> d_actions;

		public Group(params IAction[] actions)
		{
			d_actions = new List<IAction>(actions);
		}
		
		public string Description
		{
			get
			{
				List<string> ret = new List<string>();
				
				foreach (IAction action in d_actions)
				{
					ret.Add(action.Description);
				}
				
				return String.Join(", ", ret.ToArray());
			}
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
			
			List<IAction> undid = new List<IAction>();

			foreach (IAction action in reversed)
			{
				try
				{
					action.Undo();
					
					undid.Add(action);
				}
				catch
				{
					/* If there was an error, reverse already done actions */
					undid.Reverse();

					foreach (IAction ac in undid)
					{
						ac.Redo();
					}

					throw;
				}
			}
		}
		
		public void Redo()
		{
			List<IAction> redid = new List<IAction>();

			foreach (IAction action in d_actions)
			{
				try
				{
					action.Redo();
					
					redid.Add(action);
				}
				catch
				{
					/* If there was an error, reverse alraedy done actions */
					redid.Reverse();

					foreach (IAction ac in redid)
					{
						ac.Undo();
					}

					throw;
				}
			}
		}
		
		public bool CanMerge(IAction other)
		{
			if (!(other is Node))
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
