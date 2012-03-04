using System;

namespace Cdn.Studio.Undo
{
	public class AddLinkAction : LinkAction, IAction
	{
		public AddLinkAction(Wrappers.Link link, string target, string expression) : base(link, target, expression)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add action `{0}' on `{0}'", Target, Link.FullId);
			}
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

