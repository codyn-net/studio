using System;

namespace Cdn.Studio.Undo
{
	public class AddEdgeAction : EdgeAction, IAction
	{
		public AddEdgeAction(Wrappers.Edge link, string target, string expression) : base(link, target, expression)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add action `{0}' on `{0}'", Target, Edge.FullId);
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

