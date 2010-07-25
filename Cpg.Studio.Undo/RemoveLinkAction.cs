using System;

namespace Cpg.Studio.Undo
{
	public class RemoveLinkAction : LinkAction, IAction
	{
		public RemoveLinkAction(Wrappers.Link link, Cpg.LinkAction action) : base(link, action.Target, action.Equation.AsString)
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

