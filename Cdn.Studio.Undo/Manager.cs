using System;
using System.Collections.Generic;

namespace Cdn.Studio.Undo
{
	public class Manager
	{
		public delegate void Handler(object source);

		private List<IAction> d_actions;
		private int d_actionPtr;
		private int d_unmodifiedMark;
		
		public event Handler OnChanged = delegate {};
		public event Handler OnModified = delegate {};

		public Manager()
		{
			d_actions = new List<IAction>();
			d_actionPtr = 0;
			d_unmodifiedMark = 0;
		}
		
		public IAction LastAction
		{
			get
			{
				if (d_actions.Count != 0 && d_actionPtr == 0 && !IsUnmodified)
				{
					return d_actions[0];
				}
				else
				{
					return null;
				}
			}
		}
		
		public bool CanUndo
		{
			get
			{
				return d_actions.Count != 0 && d_actionPtr != d_actions.Count;
			}
		}
		
		public bool CanRedo
		{
			get
			{
				return d_actions.Count != 0 && d_actionPtr != 0;
			}
		}
		
		public void MarkUnmodified()
		{
			d_unmodifiedMark = d_actions.Count - d_actionPtr;
		}
		
		private bool TestUnmodified(int offset)
		{
			return d_unmodifiedMark == (d_actions.Count - d_actionPtr + offset);
		}
		
		public bool IsUnmodified
		{
			get
			{
				return TestUnmodified(0);
			}
		}
		
		public void Add(IAction action)
		{
			if (action is Group)
			{
				Group grp = (Group)action;
				
				if (grp.Actions.Count == 1)
				{
					action = grp.Actions[0];
				}
				else if (grp.Actions.Count == 0)
				{
					return;
				}
			}

			if (d_actions.Count - d_unmodifiedMark < d_actionPtr)
			{
				d_unmodifiedMark = -1;
			}
			
			IAction last = LastAction;
			
			if (last != null && last.CanMerge(action))
			{
				last.Merge(action);
			}
			else
			{			
				d_actions.RemoveRange(0, d_actionPtr);
				d_actions.Insert(0, action);
				d_actionPtr = 0;

				OnChanged(this);
			
				if (d_actions.Count == 1)
				{
					OnModified(this);
				}
			}
		}
		
		public void Do(IAction action)
		{
			if (action.Verify())
			{
				action.Redo();
				Add(action);
			}
		}
		
		public void Clear()
		{
			d_actions.Clear();
			d_actionPtr = 0;
			d_unmodifiedMark = 0;

			OnChanged(this);
			OnModified(this);
		}
		
		public IAction Undo()
		{
			if (!CanUndo)
			{
				return null;
			}
			
			IAction action = d_actions[d_actionPtr];
			action.Undo();
			
			d_actionPtr += 1;
			
			OnChanged(this);
			
			if (TestUnmodified(0) || TestUnmodified(1))
			{
				OnModified(this);
			}
			
			return action;
		}
		
		public IAction PeekUndo()
		{
			if (!CanUndo)
			{
				return null;
			}
			
			return d_actions[d_actionPtr];
		}
		
		public IAction Redo()
		{
			if (!CanRedo)
			{
				return null;
			}
			
			d_actionPtr -= 1;
			
			IAction action = d_actions[d_actionPtr];
			action.Redo();
			
			OnChanged(this);
			
			if (TestUnmodified(0) || TestUnmodified(-1))
			{
				OnModified(this);
			}
			
			return action;
		}
		
		public IAction PeekRedo()
		{
			if (!CanRedo)
			{
				return null;
			}
			
			return d_actions[d_actionPtr - 1];
		}
	}
}
