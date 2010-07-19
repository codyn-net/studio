using System;

namespace Cpg.Studio.Undo
{
	public class AddLinkAction : LinkAction, IAction
	{
		public AddLinkAction(Wrappers.Link link, string target, string expression) : base(link, target, expression)
		{
		}
		
		public void Undo()
		{
			Remove();
		}
		
		public void Redo()
		{
			Add();
		}
	}
}

