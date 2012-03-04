using System;

namespace Cdn.Studio.Undo
{
	public class RemoveLinkAction : LinkAction, IAction
	{
		public RemoveLinkAction(Wrappers.Link link, Cdn.LinkAction action) : base(link, action.Target, action.Equation.AsString)
		{
		}

		public string Description
		{
			get
			{
				return String.Format("Remove action `{0}' from `{0}'", Target, Link.FullId);
			}
		}

		public void Undo()
		{
			Add();
		}
		
		public void Redo()
		{
			Remove();
		}
	}
}

