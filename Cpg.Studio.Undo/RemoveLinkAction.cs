using System;

namespace Cpg.Studio.Undo
{
	public class RemoveLinkAction : LinkAction, IAction
	{
		public RemoveLinkAction(Wrappers.Link link, Wrappers.Link.Action action) : base(link, action.Target, action.Equation)
		{
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

